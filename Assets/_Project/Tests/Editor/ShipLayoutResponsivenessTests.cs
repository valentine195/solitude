using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SOLITUDE.Editor.RoomAuthoring;
using SOLITUDE.World.RoomAuthoring;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SOLITUDE.Tests.RoomAuthoring
{
    public sealed class ShipLayoutResponsivenessTests
    {
        [Serializable] public sealed class Measurement
        {
            public string fixture, settings, result;
            public int cells, objects, renderers, textures, chunks, revisionsBefore, revisionsAfter;
            public long pixels, rgbaBytes;
            public double[] rebuildMilliseconds, gestureMilliseconds, seedMilliseconds, styleMilliseconds;
            public int[] gestureApplications;
        }
        [Serializable] public sealed class Report
        {
            public string utc, unity, os, cpu, gpu, project, source;
            public int processorCount, systemMemoryMB;
            public string sampling = "One untimed warmup; five equivalent rebuilds; three multi-sample gestures, seed changes and style roundtrips. Stopwatch wall time; no speed threshold.";
            public List<Measurement> measurements = new List<Measurement>();
        }
        public static Report Measure(string source)
        {
            var report = new Report { utc = DateTime.UtcNow.ToString("o"), unity = UnityEngine.Application.unityVersion,
                os = SystemInfo.operatingSystem, cpu = SystemInfo.processorType, gpu = SystemInfo.graphicsDeviceName,
                processorCount = SystemInfo.processorCount, systemMemoryMB = SystemInfo.systemMemorySize,
                project = Directory.GetCurrentDirectory(), source = source };
            var priorStore = RoomFloorPanelAssetStore.VerificationRoot;
            var folder = RoomPersistenceTests.CreateAssets();
            var originalActive = SceneManager.GetActiveScene();
            var undoStart = Undo.GetCurrentGroup(); Undo.IncrementCurrentGroup();
            try
            {
                foreach (var kind in new[] { "Medbay176", "Cryobay83", "Larger32x32" })
                {
                    Scene scene;
                    ShipRoomAuthoring[] rooms;
                    ShipLayoutAuthoring layout;
                    if (kind == "Medbay176")
                    {
                        scene = EditorSceneManager.OpenPreviewScene("Assets/_Project/Features/World/RoomAuthoring/Scenes/MedbayAuthoringDemo.unity");
                        layout = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ShipLayoutAuthoring>()).Single(l => l.LayoutId == "1299084db2ba4b079efd3e7f8a1036c6");
                        rooms = layout.Spaces.ToArray();
                    }
                    else
                    {
                        scene = EditorSceneManager.NewPreviewScene();
                        var room = RoomAuthoringCreation.Create(scene, AssetDatabase.LoadAssetAtPath<RoomStyleDefinition>(folder + "/Style.asset"));
                        var cells = kind == "Cryobay83" ? Enumerable.Range(-5, 9).SelectMany(y => Enumerable.Range(-3, 7).Select(x => new Vector2Int(x, y)))
                            .Concat(Enumerable.Range(-1, 5).SelectMany(y => Enumerable.Range(-7, 4).Select(x => new Vector2Int(x, y)))) : RoomAuthoringTestFixtures.Cells(32, 32);
                        foreach (var c in cells) room.Occupancy.SetTile(new Vector3Int(c.x, c.y, 0), room.Style.FloorTile);
                        layout = RoomAuthoringCreation.AdoptIntoNewLayout(room); rooms = new[] { room };
                    }
                    try
                    {
                        RoomFloorPanelAssetStore.VerificationRoot = kind == "Medbay176" ? null : folder + "/" + kind;
                        foreach (var r in rooms)
                        {
                            // Detached style copy avoids modifying reusable or saved scene artwork.
                            var style = Object.Instantiate(r.Style); style.Rendering = new RoomStyleDefinition.RenderingConfiguration();
                            AssetDatabase.CreateAsset(style, folder + "/" + kind + r.RoomId + ".asset");
                            Assert.IsTrue(RoomAuthoringStyleControls.AssignSpaceStyle(r, style, out var issue), issue);
                            Assert.IsTrue(RoomAuthoringOperations.SetFloorPanelSettings(r, new RoomFloorPanelSettings { Enabled = true, Seed = 17 }, out issue), issue);
                        }
                        void Build() { RoomAuthoringOperations.Rebuild(layout); Assert.AreNotEqual(RoomGenerationStatus.Refused, layout.GenerationResult.Status); }
                        Build();
                        var m = new Measurement { fixture = kind, cells = rooms.Sum(r => RoomAuthoringOperations.ReadCells(r).Length),
                            settings = JsonUtility.ToJson(rooms[0].FloorPanels), revisionsBefore = RoomFloorPanelAssetStore.RetainedRevisionCount };
                        Assert.AreEqual(kind == "Medbay176" ? 176 : kind == "Cryobay83" ? 83 : 1024, m.cells);
                        m.rebuildMilliseconds = Enumerable.Range(0, 5).Select(_ => Time(Build)).ToArray();
                        Assert.AreEqual(m.revisionsBefore, RoomFloorPanelAssetStore.RetainedRevisionCount, "Equivalent builds must reuse immutable revisions.");
                        var r0 = rooms[0]; var cell = RoomAuthoringOperations.ReadCells(r0).Last();
                        var gestures = new List<double>(); var applications = new List<int>();
                        for (var i = 0; i < 3; i++)
                        {
                            int count = 0; void Generated(ShipRoomAuthoring r) { if (r == r0) count++; }
                            RoomAuthoringOperations.GenerationCompleted += Generated;
                            try
                            {
                                gestures.Add(Time(() => {
                                    using var edit = RoomAuthoringOperations.BeginEdit(r0, "Responsiveness multi-sample gesture");
                                    RoomAuthoringOperations.PaintCells(r0, new[] { cell }, null, out _, edit);
                                    RoomAuthoringOperations.PaintCells(r0, new[] { cell }, null, out _, edit);
                                    Assert.AreEqual(0, count); edit.Commit();
                                }));
                                Assert.AreEqual(1, count); applications.Add(count);
                            }
                            finally { RoomAuthoringOperations.GenerationCompleted -= Generated; }
                            Undo.PerformUndo(); Assert.AreEqual(m.cells, rooms.Sum(r => RoomAuthoringOperations.ReadCells(r).Length));
                        }
                        m.gestureMilliseconds = gestures.ToArray(); m.gestureApplications = applications.ToArray();
                        m.seedMilliseconds = Enumerable.Range(0, 3).Select(i => Time(() => {
                            var config = r0.FloorPanels.Copy(); config.Seed = 101 + i;
                            Assert.IsTrue(RoomAuthoringOperations.SetFloorPanelSettings(r0, config, out var issue), issue);
                        })).ToArray();
                        var baseStyle = r0.Style; var alternate = Object.Instantiate(baseStyle);
                        AssetDatabase.CreateAsset(alternate, folder + "/" + kind + "Alternate.asset");
                        m.styleMilliseconds = Enumerable.Range(0, 3).Select(i => Time(() => {
                            Assert.IsTrue(RoomAuthoringStyleControls.AssignSpaceStyle(r0, alternate, out var issue), issue);
                            Assert.IsTrue(RoomAuthoringStyleControls.AssignSpaceStyle(r0, baseStyle, out issue), issue);
                        })).ToArray();
                        var output = rooms.Select(r => r.GeneratedOutput).Distinct().ToArray();
                        m.objects = output.Sum(o => o.GetComponentsInChildren<Transform>(true).Length);
                        m.renderers = output.Sum(o => o.GetComponentsInChildren<Renderer>(true).Length);
                        var revisions = AssetDatabase.FindAssets("t:RoomFloorPanelOutput", new[] { RoomFloorPanelAssetStore.VerificationRoot ?? RoomFloorPanelAssetStore.Root })
                            .Select(g => AssetDatabase.LoadAssetAtPath<RoomFloorPanelOutput>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
                        var active = rooms.Select(r => r.GeneratedOutput.PreparedPanels).Where(p => p != null).Distinct().ToArray();
                        var textures = active.SelectMany(p => p.Chunks).Select(c => c.Texture).Distinct().ToArray();
                        m.textures = textures.Length; m.chunks = active.Sum(p => p.Chunks.Count);
                        m.pixels = textures.Sum(t => (long)t.width * t.height); m.rgbaBytes = m.pixels * 4;
                        m.revisionsAfter = revisions.Length; m.result = layout.GenerationResult.Status.ToString();
                        Assert.IsTrue(revisions.All(RoomFloorPanelAssetStore.IsVerified));
                        report.measurements.Add(m);
                    }
                    finally { EditorSceneManager.ClosePreviewScene(scene); }
                }
            }
            finally
            {
                Undo.RevertAllDownToGroup(undoStart + 1);
                if (originalActive.IsValid() && originalActive.isLoaded) SceneManager.SetActiveScene(originalActive);
                RoomFloorPanelAssetStore.VerificationRoot = priorStore;
                AssetDatabase.DeleteAsset(folder);
            }
            return report;
        }
        private static double Time(Action action) { var watch = Stopwatch.StartNew(); action(); watch.Stop(); return watch.Elapsed.TotalMilliseconds; }
        [Test] public void MeasureThreeFixturesWithEquivalentReuseAndSingleApplyGestures()
        {
            Assert.IsTrue(Environment.GetCommandLineArgs().Contains("-solitudeRoomAuthoringVerification"));
            var report = Measure("Phase4 Step2 current isolated source");
            var path = Environment.GetEnvironmentVariable("SOLITUDE_RESPONSIVENESS_REPORT");
            if (!string.IsNullOrEmpty(path)) File.WriteAllText(path, JsonUtility.ToJson(report, true));
            TestContext.WriteLine(JsonUtility.ToJson(report, true));
        }
    }
}
