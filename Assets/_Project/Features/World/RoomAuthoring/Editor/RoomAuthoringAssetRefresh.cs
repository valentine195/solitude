using System;
using System.Collections.Generic;
using System.Linq;
using SOLITUDE.World.RoomAuthoring;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SOLITUDE.Editor.RoomAuthoring
{
    // Watches loaded authoring only. Generated output/imports are never a dependency merely by folder or name.
    [InitializeOnLoad]
    public static class RoomAuthoringAssetRefresh
    {
        private sealed class Watch
        {
            public string Assets;
            public string OutputOwnership;
            public bool VerifiedOutput;
            public HashSet<string> Paths;
            public bool Pending;
            public bool ImportPending;
            public double Due;
            public string Issue;
        }

        private const double ScanInterval = .25;
        private const double SettleInterval = .15;
        private const int WritesPerUpdate = 2;
        private static readonly Dictionary<ShipLayoutAuthoring, Watch> watches = new Dictionary<ShipLayoutAuthoring, Watch>();
        private static double nextScan;
        private static bool publishing;

        static RoomAuthoringAssetRefresh()
        {
            EditorApplication.update += Update;
            RoomAuthoringOperations.GenerationCompleted += room => { if (room != null && room.Layout != null) Observe(room.Layout); };
            EditorSceneManager.sceneOpened += (scene, mode) => ObserveScene(scene);
            EditorSceneManager.sceneClosed += scene => Prune();
            Undo.undoRedoPerformed += ObserveLoaded;
            AssemblyReloadEvents.beforeAssemblyReload += () => watches.Clear();
        }

        public static ShipLayoutAuthoring[] LoadedLayouts() => Resources.FindObjectsOfTypeAll<ShipLayoutAuthoring>()
            .Where(layout => layout != null && !EditorUtility.IsPersistent(layout) && layout.gameObject.scene.IsValid() &&
                layout.gameObject.scene.isLoaded && layout.Spaces.Count != 0 && layout.Spaces.All(room => room != null))
            .OrderBy(layout => layout.LayoutId, StringComparer.Ordinal).ToArray();

        private static string OutputOwnership(ShipLayoutAuthoring layout) => string.Join("|", layout.Spaces
            .OrderBy(room => room.RoomId, StringComparer.Ordinal).Select(room => room.RoomId + ":" + RoomAuthoringValidator.OutputFingerprint(room, false)));

        public static void Observe(ShipLayoutAuthoring layout)
        {
            if (layout == null || !layout.gameObject.scene.IsValid() || !layout.gameObject.scene.isLoaded ||
                layout.Spaces.Count == 0 || layout.Spaces.Any(room => room == null)) return;
            var verified = layout.Spaces.All(room => !string.IsNullOrEmpty(room.GenerationResult?.OutputFingerprint) &&
                room.GenerationResult.OutputFingerprint == RoomAuthoringValidator.OutputFingerprint(room));
            watches[layout] = new Watch
            {
                VerifiedOutput = verified,
                Issue = verified ? null : "Generated output is modified or was reopened with outdated asset bounds. Review it and use Explicit Rebuild before automatic artwork refresh.",
                Assets = RoomAuthoringValidator.StyleAssetsFingerprint(layout), OutputOwnership = OutputOwnership(layout),
                Paths = new HashSet<string>(RoomAuthoringValidator.StyleDependencies(layout).Select(AssetDatabase.GetAssetPath)
                    .Where(path => !string.IsNullOrEmpty(path)), StringComparer.Ordinal)
            };
        }

        public static string GetIssue(ShipLayoutAuthoring layout) => layout != null && watches.TryGetValue(layout, out var watch) ? watch.Issue : null;

        public static bool IsPending(ShipLayoutAuthoring layout) => layout != null && watches.TryGetValue(layout, out var watch) && (watch.Pending || watch.ImportPending);

        public static void ObserveScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return;
            foreach (var layout in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<ShipLayoutAuthoring>(true))) Observe(layout);
        }

        private static void ObserveLoaded()
        {
            // Supported binding edits restore their derived output in the same Undo group. Never publish a new Undo
            // operation here: it would clear Redo. External bytes remain current and read-only assessment may be stale.
            foreach (var layout in LoadedLayouts())
            {
                var pending = watches.TryGetValue(layout, out var previous) && (previous.Pending || previous.ImportPending);
                Observe(layout);
                if (pending) { watches[layout].Pending = true; watches[layout].Due = EditorApplication.timeSinceStartup + SettleInterval; }
            }
            Prune();
        }

        private static void Prune()
        {
            foreach (var layout in watches.Keys.Where(layout => layout == null || !layout.gameObject.scene.IsValid() || !layout.gameObject.scene.isLoaded).ToArray())
                watches.Remove(layout);
        }

        public static void AssetsImported(IEnumerable<string> paths)
        {
            if (publishing) return;
            var changed = new HashSet<string>(paths, StringComparer.Ordinal);
            foreach (var watch in watches.Values.Where(watch => watch.Paths.Overlaps(changed)))
                watch.ImportPending = true;
            nextScan = 0;
        }

        private static void Update()
        {
            if (publishing || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            var now = EditorApplication.timeSinceStartup;
            if (now < nextScan) return;
            nextScan = now + ScanInterval;
            Prune();
            foreach (var layout in LoadedLayouts())
                if (!watches.ContainsKey(layout)) Observe(layout);
            var writes = 0;
            foreach (var pair in watches.ToArray())
            {
                var layout = pair.Key; var watch = pair.Value;
                var assets = RoomAuthoringValidator.StyleAssetsFingerprint(layout);
                if (watch.Assets != assets)
                {
                    watch.Assets = assets; watch.Pending = true; watch.Due = now + SettleInterval;
                }
                watch.ImportPending = false;
                if (!watch.Pending || now < watch.Due || writes >= WritesPerUpdate || RoomAuthoringOperations.HasActiveEdit) continue;
                watch.Pending = false;
                if (RoomAuthoringValidator.IsCurrent(layout)) { Observe(layout); continue; }
                var safety = RoomAuthoringOperations.GetSafetyIssue(layout, allowMissingOutput: true);
                if (safety == null && !watch.VerifiedOutput) safety = watch.Issue;
                if (safety == null && OutputOwnership(layout) != watch.OutputOwnership)
                    safety = "Generated output was changed manually. Inspect it and use Explicit Rebuild before automatic artwork refresh.";
                if (safety != null) { watch.Issue = safety; continue; }
                publishing = true;
                try
                {
                    RoomAuthoringOperations.Rebuild(layout);
                    Observe(layout);
                    if (layout.GenerationResult?.Status == RoomGenerationStatus.Refused) watches[layout].Issue = layout.GenerationResult.Summary;
                    SceneView.RepaintAll();
                    writes++;
                }
                finally { publishing = false; }
            }
        }
    }

    public sealed class RoomAuthoringAssetPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom) =>
            RoomAuthoringAssetRefresh.AssetsImported(imported.Concat(deleted).Concat(moved).Concat(movedFrom));
    }
}
