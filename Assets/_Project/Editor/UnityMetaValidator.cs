#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SOLITUDE.Editor
{
    /// <summary>
    /// Validates the two invariants Unity references depend on:
    /// every project asset/folder has one metadata file, and every versioned
    /// asset/folder metadata pair is committed together with a unique GUID.
    /// </summary>
    public static class UnityMetaValidator
    {
        private static readonly Regex GuidPattern = new(
            @"^guid:\s*([0-9a-fA-F]{32})\s*$",
            RegexOptions.Compiled | RegexOptions.Multiline);

        [MenuItem("SOLITUDE/Validation/Validate Unity Metadata")]
        public static void ValidateFromMenu()
        {
            var errors = CollectErrors();
            if (errors.Count == 0)
            {
                Debug.Log("[UnityMetaValidator] Metadata pairs and GUIDs are valid.");
                return;
            }

            foreach (var error in errors)
                Debug.LogError($"[UnityMetaValidator] {error}");
        }

        /// <summary>
        /// Batch-mode entry point. Use with:
        /// -executeMethod SOLITUDE.Editor.UnityMetaValidator.ValidateForCI
        /// </summary>
        public static void ValidateForCI()
        {
            var errors = CollectErrors();
            if (errors.Count == 0)
            {
                Debug.Log("[UnityMetaValidator] Metadata pairs and GUIDs are valid.");
                return;
            }

            throw new BuildFailedException(
                "Unity metadata validation failed:\n" + string.Join("\n", errors));
        }

        internal static IReadOnlyList<string> CollectErrors()
        {
            string projectRoot = Directory.GetParent(UnityEngine.Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot))
                return new[] { "Could not resolve the Unity project root." };

            var errors = new List<string>();
            ValidateWorkingTree(projectRoot, errors);
            ValidateTrackedPairs(projectRoot, errors);
            ValidateUniqueGuids(projectRoot, errors);
            return errors;
        }

        private static void ValidateWorkingTree(string projectRoot, List<string> errors)
        {
            foreach (string assetPath in AssetDatabase.GetAllAssetPaths())
            {
                if (!assetPath.StartsWith("Assets/", StringComparison.Ordinal) ||
                    assetPath.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                    continue;

                string fullPath = Path.Combine(projectRoot, assetPath);
                if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
                    continue;

                if (!File.Exists(fullPath + ".meta"))
                    errors.Add($"Missing metadata: {assetPath}.meta");
            }

            foreach (string metaPath in Directory.EnumerateFiles(
                         UnityEngine.Application.dataPath, "*.meta", SearchOption.AllDirectories))
            {
                string assetPath = metaPath[..^".meta".Length];
                if (!File.Exists(assetPath) && !Directory.Exists(assetPath))
                    errors.Add($"Orphan metadata: {ToProjectPath(projectRoot, metaPath)}");
            }
        }

        private static void ValidateTrackedPairs(string projectRoot, List<string> errors)
        {
            if (!TryReadTrackedPaths(projectRoot, out var tracked, out string failure))
            {
                errors.Add(failure);
                return;
            }

            foreach (string path in tracked.Where(IsVersionedUnityAsset))
            {
                string fullPath = Path.Combine(projectRoot, path);
                // A pending deletion is valid when the asset and metadata were removed together.
                if (!File.Exists(fullPath) && !Directory.Exists(fullPath) && !File.Exists(fullPath + ".meta"))
                    continue;
                if (!tracked.Contains(path + ".meta"))
                    errors.Add($"Versioned asset is missing versioned metadata: {path}.meta");

                string directory = Path.GetDirectoryName(path)?.Replace('\\', '/');
                while (!string.IsNullOrEmpty(directory) && directory != "Assets")
                {
                    if (!tracked.Contains(directory + ".meta"))
                        errors.Add($"Versioned asset folder is missing versioned metadata: {directory}.meta");
                    directory = Path.GetDirectoryName(directory)?.Replace('\\', '/');
                }
            }

            foreach (string metaPath in tracked.Where(path => path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)))
            {
                string assetPath = metaPath[..^".meta".Length];
                string fullAssetPath = Path.Combine(projectRoot, assetPath);
                if (tracked.Contains(assetPath) && !File.Exists(fullAssetPath) &&
                    !Directory.Exists(fullAssetPath) && !File.Exists(fullAssetPath + ".meta"))
                    continue;
                if (File.Exists(fullAssetPath) && !tracked.Contains(assetPath))
                    errors.Add($"Versioned metadata has an unversioned asset: {metaPath}");
                else if (!File.Exists(fullAssetPath) && !Directory.Exists(fullAssetPath))
                    errors.Add($"Versioned metadata has no asset or folder: {metaPath}");
            }
        }

        private static void ValidateUniqueGuids(string projectRoot, List<string> errors)
        {
            var ownersByGuid = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string metaPath in Directory.EnumerateFiles(
                         UnityEngine.Application.dataPath, "*.meta", SearchOption.AllDirectories))
            {
                string contents = File.ReadAllText(metaPath);
                Match match = GuidPattern.Match(contents);
                string relativePath = ToProjectPath(projectRoot, metaPath);
                if (!match.Success)
                {
                    errors.Add($"Metadata has no valid GUID: {relativePath}");
                    continue;
                }

                string guid = match.Groups[1].Value;
                if (ownersByGuid.TryGetValue(guid, out string existing))
                    errors.Add($"Duplicate GUID {guid}: {existing} and {relativePath}");
                else
                    ownersByGuid.Add(guid, relativePath);
            }
        }

        private static bool TryReadTrackedPaths(
            string projectRoot,
            out HashSet<string> tracked,
            out string failure)
        {
            tracked = new HashSet<string>(StringComparer.Ordinal);
            failure = null;

            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "ls-files -z -- Assets",
                WorkingDirectory = projectRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using Process process = Process.Start(startInfo);
                if (process == null)
                {
                    failure = "Could not start Git to validate versioned metadata.";
                    return false;
                }

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    failure = $"Git metadata query failed: {error.Trim()}";
                    return false;
                }

                foreach (string path in output.Split(new[] { '\0' }, StringSplitOptions.RemoveEmptyEntries))
                    tracked.Add(path.Replace('\\', '/'));
                return true;
            }
            catch (Exception exception)
            {
                failure = $"Git metadata query failed: {exception.Message}";
                return false;
            }
        }

        private static bool IsVersionedUnityAsset(string path) =>
            path.StartsWith("Assets/", StringComparison.Ordinal) &&
            !path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase) &&
            !path.EndsWith("/.DS_Store", StringComparison.Ordinal) &&
            !path.Equals("Assets/.DS_Store", StringComparison.Ordinal);

        private static string ToProjectPath(string projectRoot, string fullPath) =>
            Path.GetRelativePath(projectRoot, fullPath).Replace('\\', '/');
    }
}
#endif
