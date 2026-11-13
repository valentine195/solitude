using System;
using System.IO;
using System.Linq;
using UnityEditor;
using SOLITUDE.Composition;
using UnityEngine;
namespace SOLITUDE.Editor
{
    public sealed class WakeupBuildManifest : ScriptableObject
    {
        public const string AssetPath = "Assets/_Project/Editor/WakeupBuildManifest.asset";
        private static WakeupBuildManifest verificationManifest;
        public static WakeupBuildManifest Selected => verificationManifest != null ? verificationManifest : AssetDatabase.LoadAssetAtPath<WakeupBuildManifest>(AssetPath);
        public static bool IsRepairVerification => verificationManifest != null;
        public static string[] RepairScenes => new[] { MechanicalRepairFixtureBuilder.ScenePath, "Assets/_Project/Scenes/Wakeup.unity" };
        public static bool AllowsScenes(string[] selected) => selected != null && selected.SequenceEqual(IsRepairVerification ? RepairScenes : new[] { "Assets/_Project/Scenes/Wakeup.unity" });
        public static IDisposable SelectRepairVerification()
        {
            string root = Directory.GetParent(UnityEngine.Application.dataPath).FullName;
            if (!Environment.GetCommandLineArgs().Contains("-solitudeMechanicalRepairFixture") || File.Exists(Path.Combine(root, ".git")) || Directory.Exists(Path.Combine(root, ".git")) || verificationManifest != null)
                throw new InvalidOperationException("Repair build requires explicit disposable verification opt-in.");
            var original = AssetDatabase.LoadAssetAtPath<WakeupBuildManifest>(AssetPath);
            if (original == null || original.scenes == null || !original.scenes.SequenceEqual(new[] { "Assets/_Project/Scenes/Wakeup.unity" }))
                throw new InvalidOperationException("Shipping manifest must remain Wakeup-only.");
            var settings = EditorBuildSettings.scenes;
            verificationManifest = Instantiate(original);
            // BuildPipeline unloads unused assets before scene callbacks. Keep this
            // nonpersistent selection alive until the enclosing build scope exits.
            verificationManifest.hideFlags = HideFlags.HideAndDontSave;
            verificationManifest.scenes = RepairScenes;
            EditorBuildSettings.scenes = RepairScenes.Select(p => new EditorBuildSettingsScene(p, true)).ToArray();
            return new RepairScope(settings);
        }
        private sealed class RepairScope : IDisposable
        {
            private EditorBuildSettingsScene[] original;
            public RepairScope(EditorBuildSettingsScene[] original) { this.original = original; }
            public void Dispose()
            {
                if (original == null) return;
                EditorBuildSettings.scenes = original; original = null;
                DestroyImmediate(verificationManifest); verificationManifest = null;
            }
        }
        public string[] scenes = { "Assets/_Project/Scenes/Wakeup.unity" };
        public GameRuntimeConfig runtimeConfig;
        public string target = "StandaloneOSX";
        public string architecture = "ARM64";
        public string backend = "Mono2x";
        public string saveWorld = "Wakeup";
        // Context fixtures share this save world but are excluded from player builds.
        public string[] verificationContextScenes = { PhaseFiveFixtureBuilder.Context, PhaseFiveFixtureBuilder.Unrelated };
    }
}
