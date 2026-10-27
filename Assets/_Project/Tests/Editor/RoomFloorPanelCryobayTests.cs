using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
    public sealed class RoomFloorPanelCryobayTests
    {
        private string host, fixture;
        private Scene scene;
        private string source, metadata, build;
        private static string Digest(byte[] bytes)
        { using var sha = SHA256.Create(); return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
        [SetUp] public void Setup()
        {
            Assert.IsTrue(Environment.GetCommandLineArgs().Contains("-solitudeRoomAuthoringVerification"));
            source = Digest(File.ReadAllBytes(ShipLayoutSmokeSetup.CryobaySource));
            metadata = Digest(File.ReadAllBytes(ShipLayoutSmokeSetup.CryobaySource + ".meta"));
            build = Digest(File.ReadAllBytes("ProjectSettings/EditorBuildSettings.asset"));
            host = "Assets/_Project/Tests/RoomAuthoringFixture" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets/_Project/Tests", Path.GetFileName(host));
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Assert.IsTrue(EditorSceneManager.SaveScene(scene, host + "/Host.unity"));
            fixture = ShipLayoutSmokeSetup.FolderPrefix + Guid.NewGuid().ToString("N");
            RoomFloorPanelAssetStore.VerificationRoot = host + "/Panels";
        }
        [TearDown] public void Cleanup()
        {
            Assert.AreEqual(source, Digest(File.ReadAllBytes(ShipLayoutSmokeSetup.CryobaySource)));
            Assert.AreEqual(metadata, Digest(File.ReadAllBytes(ShipLayoutSmokeSetup.CryobaySource + ".meta")));
            Assert.AreEqual(build, Digest(File.ReadAllBytes("ProjectSettings/EditorBuildSettings.asset")));
            Undo.ClearAll(); EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AssetDatabase.DeleteAsset(fixture); AssetDatabase.DeleteAsset(host); RoomFloorPanelAssetStore.VerificationRoot = null;
        }
        private ShipRoomAuthoring Create()
        {
            var path = ShipLayoutSmokeSetup.CreateCryobay(fixture);
            scene = SceneManager.GetSceneByPath(path);
            return scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ShipRoomAuthoring>()).Single(r => r.name == "Cryobay83");
        }
        [Test] public void SourceCopyUsesExactConnectedFloorCellsAndRefusesScaffoldOverwrite()
        {
            var room = Create(); var actual = new HashSet<Vector2Int>(RoomAuthoringOperations.ReadCells(room));
            var expected = new HashSet<Vector2Int>();
            for (var y = -5; y <= 3; y++) for (var x = -3; x <= 3; x++) expected.Add(new Vector2Int(x, y));
            for (var y = -1; y <= 3; y++) for (var x = -7; x <= -4; x++) expected.Add(new Vector2Int(x, y));
            Assert.AreEqual(83, actual.Count); Assert.IsTrue(actual.SetEquals(expected));
            var seen = new HashSet<Vector2Int>(); var queue = new Queue<Vector2Int>(); queue.Enqueue(actual.First());
            while (queue.Count != 0)
            {
                var cell = queue.Dequeue(); if (!seen.Add(cell)) continue;
                foreach (var step in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                    if (actual.Contains(cell + step) && !seen.Contains(cell + step)) queue.Enqueue(cell + step);
            }
            Assert.AreEqual(83, seen.Count); Assert.AreEqual(83, RoomAuthoringTestFixtures.FloorCount(room));
            Assert.AreEqual(83, room.Layout.Spaces.Sum(r => RoomAuthoringOperations.ReadCells(r).Length));
            Assert.IsEmpty(room.FloorReservations); Assert.IsEmpty(room.Layout.Connections);
            Assert.AreEqual(0, room.Style.Rendering.FloorOrder); Assert.AreEqual(1, room.Style.Rendering.DetailOrder);
            Assert.AreEqual(2, room.Style.Rendering.ArchitectureOrder);
            Assert.IsTrue(room.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>().Any(r => r.name == "Generated Floor Panels"), room.GenerationDiagnostic);
            var bytes = File.ReadAllBytes(scene.path); var guid = AssetDatabase.AssetPathToGUID(scene.path);
            Assert.Throws<InvalidOperationException>(() => ShipLayoutSmokeSetup.CreateCryobay(fixture));
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(scene.path)); Assert.AreEqual(guid, AssetDatabase.AssetPathToGUID(scene.path));
            var image = new Texture2D(2, 2);
            try { image.LoadImage(File.ReadAllBytes(fixture + "/Floor.png")); Assert.AreEqual(1, image.GetPixels32().Distinct().Count()); }
            finally { Object.DestroyImmediate(image); }
        }
        [Test] public void TenSeedEditorGalleryMatchesExactCoveragePreparedPixelsAndRepeatGeneration()
        {
            var room = Create(); var originalCells = RoomAuthoringOperations.ReadCells(room);
            var destination = Path.GetFullPath("work/Reviews/RoomAuthoring/CryobayRuns/" + Guid.NewGuid().ToString("N"));
            var gallery = ShipLayoutSmokeSetup.ExportCryobayGallery(room, destination);
            Assert.AreEqual(10, gallery.seeds.Length); Assert.AreEqual(10, gallery.seeds.Select(s => s.rectangleSha256).Distinct().Count());
            Assert.AreEqual(10, gallery.seeds.Select(s => s.pixelSha256).Distinct().Count());
            var expected = new HashSet<Vector2Int>();
            foreach (var cell in originalCells)
                for (var y = 0; y < 32; y++) for (var x = 0; x < 32; x++) expected.Add(cell * 32 + new Vector2Int(x, y));
            foreach (var record in gallery.seeds)
            {
                var visited = new HashSet<Vector2Int>();
                foreach (var rect in record.rectangles)
                    for (var y = rect.y; y < rect.yMax; y++) for (var x = rect.x; x < rect.xMax; x++)
                    { var pixel = new Vector2Int(x, y); Assert.IsTrue(expected.Contains(pixel)); Assert.IsTrue(visited.Add(pixel), "Overlapping panel interiors"); }
                Assert.IsTrue(visited.SetEquals(expected), "Gaps in panel coverage");
                var partition = RoomFloorPanelPartitioner.Partition(RoomFloorPanelDomain.Build(room, record.settings, room.FloorReservations));
                CollectionAssert.AreEqual(record.rectangles, partition.Rectangles);
                var seams = RoomFloorPanelSeams.Extract(room.RoomId, partition.Rectangles);
                Assert.IsTrue(seams.Success); Assert.AreEqual(seams.Segments.Count, seams.Segments.Select(s => s.Start + ":" + s.End).Distinct().Count());
                var raster = RoomFloorPanelRasterizer.Rasterize(room.RoomId, seams, record.settings); Assert.IsTrue(raster.Success);
                var rasterPixels = new Dictionary<Vector2Int, Color32>();
                foreach (var chunk in raster.Chunks)
                    for (var y = 0; y < chunk.Size; y++) for (var x = 0; x < chunk.Size; x++)
                    {
                        var color = chunk.Pixels[y * chunk.Size + x]; if (color.a == 0) continue;
                        var pixel = chunk.Origin + new Vector2Int(x, y); Assert.IsTrue(expected.Contains(pixel)); rasterPixels.Add(pixel, color);
                    }
                var image = new Texture2D(2, 2);
                try
                {
                    image.LoadImage(File.ReadAllBytes(Path.Combine(destination, record.transparentImage)));
                    Assert.AreEqual(352, image.width); Assert.AreEqual(288, image.height); var pixels = image.GetPixels32();
                    Assert.AreEqual(record.pixelSha256, Digest(pixels.SelectMany(c => new[] { c.r, c.g, c.b, c.a }).ToArray()));
                    Assert.AreEqual(record.seamPixels, rasterPixels.Count); Assert.AreEqual(record.seamPixels, pixels.Count(c => c.a != 0));
                    foreach (var pair in rasterPixels) Assert.AreEqual(pair.Value, pixels[(pair.Key.y + 160) * 352 + pair.Key.x + 224]);
                }
                finally { Object.DestroyImmediate(image); }
            }
            var old = RoomUndoRedoTests.Snapshot(room); var revisionCount = RoomFloorPanelAssetStore.RetainedRevisionCount;
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(room)); Assert.IsTrue(RoomAuthoringOperations.Rebuild(room));
            CollectionAssert.AreEqual(old, RoomUndoRedoTests.Snapshot(room)); Assert.AreEqual(revisionCount, RoomFloorPanelAssetStore.RetainedRevisionCount);
            CollectionAssert.AreEqual(originalCells, RoomAuthoringOperations.ReadCells(room));
            Assert.Throws<InvalidOperationException>(() => ShipLayoutSmokeSetup.ExportCryobayGallery(room, destination));
            var sparse = room.FloorPanels.Copy(); sparse.Density = 0;
            var dense = sparse.Copy(); dense.Density = 1;
            var a = RoomFloorPanelPartitioner.Partition(RoomFloorPanelDomain.Build(room, sparse, room.FloorReservations));
            var b = RoomFloorPanelPartitioner.Partition(RoomFloorPanelDomain.Build(room, dense, room.FloorReservations));
            Assert.Less(a.Rectangles.Count, b.Rectangles.Count);
            var constrained = sparse.Copy(); constrained.PreferredMinPixels = constrained.PreferredMaxPixels = new Vector2Int(64, 64);
            var residual = RoomFloorPanelPartitioner.Partition(RoomFloorPanelDomain.Build(room, constrained, room.FloorReservations));
            Assert.IsTrue(residual.Success); Assert.IsNotEmpty(residual.Advisories);
            TestContext.WriteLine("Cryobay gallery export: " + destination);
            TestContext.WriteLine("Density panels low=" + a.Rectangles.Count + " high=" + b.Rectangles.Count + "; constrained advisories=" + residual.Advisories.Count);
        }
    }
}
