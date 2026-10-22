using System;
using System.Linq;
using NUnit.Framework;
using SOLITUDE.Editor.RoomAuthoring;
using SOLITUDE.World.RoomAuthoring;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SOLITUDE.Tests.RoomAuthoring
{
    public sealed class ShipLayoutEditingTests
    {
        [Test] public void SmokeSetupRefusesExistingDestinationBeforeChangingAnything()
        {
            Assert.IsTrue(Environment.GetCommandLineArgs().Contains("-solitudeRoomAuthoringVerification"));
            var folder = ShipLayoutSmokeSetup.FolderPrefix + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets/_Project/Tests", System.IO.Path.GetFileName(folder));
            var guid = AssetDatabase.AssetPathToGUID(folder);
            var scenes = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt).ToArray();
            var group = Undo.GetCurrentGroup();
            try
            {
                Assert.Throws<InvalidOperationException>(() => ShipLayoutSmokeSetup.Create(folder));
                Assert.AreEqual(guid, AssetDatabase.AssetPathToGUID(folder));
                CollectionAssert.AreEqual(scenes, Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt).ToArray());
                Assert.AreEqual(group, Undo.GetCurrentGroup());
                Assert.IsEmpty(System.IO.Directory.GetFiles(folder));
            }
            finally { AssetDatabase.DeleteAsset(folder); }
        }

        [Test] public void SmokeSetupRefusesUntitledSceneBeforeWritingAssets()
        {
            Assert.IsTrue(Environment.GetCommandLineArgs().Contains("-solitudeRoomAuthoringVerification"));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var folder = ShipLayoutSmokeSetup.FolderPrefix + Guid.NewGuid().ToString("N");
            var group = Undo.GetCurrentGroup(); var dirty = scene.isDirty;
            Assert.Throws<InvalidOperationException>(() => ShipLayoutSmokeSetup.Create(folder));
            Assert.IsFalse(System.IO.Directory.Exists(folder));
            Assert.IsFalse(System.IO.File.Exists(folder + ".meta"));
            Assert.AreEqual(scene, SceneManager.GetActiveScene());
            Assert.AreEqual(group, Undo.GetCurrentGroup()); Assert.AreEqual(dirty, scene.isDirty);
        }

        [Test] public void WalkthroughFixtureSupportsEditingCorrectionAndReadOnlyFocusWithoutTouchingSentinels()
        {
            Assert.IsTrue(Environment.GetCommandLineArgs().Contains("-solitudeRoomAuthoringVerification"));
            var folder = ShipLayoutSmokeSetup.FolderPrefix + Guid.NewGuid().ToString("N");
            var baselinePath = "Assets/_Project/Tests/ShipLayoutSmokeBaseline" + Guid.NewGuid().ToString("N") + ".unity";
            var baseline = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var baselineSentinel = new GameObject("Baseline Gameplay Sentinel");
            baselineSentinel.transform.position = new Vector3(2, 3, 0);
            Assert.IsTrue(EditorSceneManager.SaveScene(baseline, baselinePath));
            var baselineBytes = System.IO.File.ReadAllBytes(baselinePath);
            var scene = default(Scene);
            try
            {
                var path = ShipLayoutSmokeSetup.Create(folder); scene = SceneManager.GetSceneByPath(path);
                var rooms = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<ShipRoomAuthoring>()).ToArray();
                var room = rooms.Single(space => space.name == ShipLayoutSmokeSetup.MainSpaceName);
                var others = rooms.Where(space => space != room).ToArray();
                var original = others.Select(RoomUndoRedoTests.Snapshot).ToArray();
                var objects = others.Select(space => space.GeneratedOutput.GetComponentsInChildren<Transform>(true)).ToArray();
                var sentinel = scene.GetRootGameObjects().Single(root => root.name == "Generated Architecture");
                var sentinelPosition = sentinel.transform.position;
                Assert.IsEmpty(RoomAuthoringOperations.ReadCells(room));
                Assert.IsTrue(RoomAuthoringOperations.PaintBox(room, new BoundsInt(0, 0, 0, 8, 6, 1), room.Style.FloorTile));
                Assert.AreEqual(48, RoomAuthoringTestFixtures.FloorCount(room));
                AssertExteriorRectangle(room, 8, 6);
                var before = RoomUndoRedoTests.Snapshot(room);
                Assert.IsTrue(RoomAuthoringOperations.PaintBox(room, new BoundsInt(8, 0, 0, 4, 6, 1), room.Style.FloorTile));
                Assert.AreEqual(72, RoomAuthoringTestFixtures.FloorCount(room));
                AssertExteriorRectangle(room, 12, 6);
                Undo.PerformUndo(); CollectionAssert.AreEqual(before, RoomUndoRedoTests.Snapshot(room));
                Undo.PerformRedo(); Assert.AreEqual(72, RoomAuthoringTestFixtures.FloorCount(room));
                Assert.IsTrue(RoomAuthoringOperations.PaintBox(room, new BoundsInt(10, 0, 0, 2, 2, 1), null));
                Assert.AreEqual(68, RoomAuthoringTestFixtures.FloorCount(room));
                AssertCapsAndCornersLeaveFloorClear(room);
                Assert.AreEqual(1, room.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>(true)
                    .Count(renderer => renderer.name == "ConcaveSoutheast"));
                Undo.PerformUndo(); Assert.AreEqual(72, RoomAuthoringTestFixtures.FloorCount(room));
                var sprite = room.Style.Northwest.Sprite;
                using (RoomAuthoringOperations.BeginEdit(room, "Withhold Northwest"))
                {
                    Undo.RegisterCompleteObjectUndo(room.Style, "Withhold Northwest");
                    room.Style.Northwest.Sprite = null;
                    Assert.IsFalse(RoomAuthoringOperations.Rebuild(room.Layout));
                }
                Assert.AreEqual(72, RoomAuthoringTestFixtures.FloorCount(room));
                Assert.AreEqual(RoomGenerationStatus.IncompletePreview, room.Layout.GenerationResult.Status);
                var partial = RoomUndoRedoTests.Snapshot(room);
                Selection.activeGameObject = room.gameObject;
                var issue = room.GenerationResult.Issues.First(i => i.Field == "Northwest");
                var dirty = scene.isDirty; var group = Undo.GetCurrentGroup();
                Assert.IsTrue(RoomAuthoringPreview.SelectIssue(room, issue.Id, false));
                CollectionAssert.AreEqual(partial, RoomUndoRedoTests.Snapshot(room));
                Assert.AreEqual(dirty, scene.isDirty); Assert.AreEqual(group, Undo.GetCurrentGroup());
                using (RoomAuthoringOperations.BeginEdit(room, "Restore Northwest"))
                {
                    Undo.RegisterCompleteObjectUndo(room.Style, "Restore Northwest");
                    room.Style.Northwest.Sprite = sprite;
                    Assert.IsTrue(RoomAuthoringOperations.Rebuild(room.Layout));
                }
                Assert.IsTrue(RoomAuthoringValidator.IsCurrent(room.Layout));
                Assert.IsEmpty(room.GenerationResult.Issues);
                for (var i = 0; i < others.Length; i++)
                {
                    CollectionAssert.AreEqual(original[i], RoomUndoRedoTests.Snapshot(others[i]));
                    CollectionAssert.AreEqual(objects[i], others[i].GeneratedOutput.GetComponentsInChildren<Transform>(true));
                }
                Assert.AreEqual(sentinelPosition, sentinel.transform.position);
                Assert.IsTrue(EditorSceneManager.SaveScene(scene, path));
                Assert.IsFalse(baseline.isDirty);
                Assert.AreEqual(new Vector3(2, 3, 0), baselineSentinel.transform.position);
                CollectionAssert.AreEqual(baselineBytes, System.IO.File.ReadAllBytes(baselinePath));
            }
            finally
            {
                Selection.activeObject = null;
                if (scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                AssetDatabase.DeleteAsset(folder);
                AssetDatabase.DeleteAsset(baselinePath);
            }
        }

        [Test] public void SharedEraseUndoRedoPreservesOtherAuthoredChoicesAndRestoresAllDerivedOutput()
        {
            var folder = ShipLayoutDiagnosticTests.CreateSharedAssets();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            try
            {
                var layout = ShipLayoutDiagnosticTests.CreateSharedLayout(scene, folder);
                Assert.AreEqual(RoomGenerationStatus.Complete, layout.GenerationResult.Status, layout.GenerationResult.Summary);
                var a = layout.Spaces[0]; var b = layout.Spaces[1]; var c = layout.Spaces[2];
                var sentinel = new GameObject("Gameplay Sentinel"); sentinel.transform.position = new Vector3(20, 30, 0);
                var decoration = new GameObject("Authored Decoration"); decoration.transform.SetParent(b.transform, false);
                var bCells = RoomAuthoringOperations.ReadCells(b); var cCells = RoomAuthoringOperations.ReadCells(c);
                b.AuthoringMetadata = "Preserve B choice"; c.AuthoringMetadata = "Preserve C choice";
                var bStyle = b.Style; var cStyle = c.Style; var bType = b.SpaceType; var cType = c.SpaceType;
                Assert.IsTrue(RoomAuthoringOperations.Rebuild(layout));
                var before = ShipLayoutDiagnosticTests.Snapshot(layout);
                Assert.IsTrue(layout.GetComponentsInChildren<SpriteRenderer>().Any(r => r.name.StartsWith("Partition")));
                Assert.IsTrue(RoomAuthoringOperations.PaintBox(a, new BoundsInt(3, 0, 0, 1, 6, 1), null));
                Assert.AreEqual(18, RoomAuthoringTestFixtures.FloorCount(a));
                CollectionAssert.AreEqual(bCells, RoomAuthoringOperations.ReadCells(b)); CollectionAssert.AreEqual(cCells, RoomAuthoringOperations.ReadCells(c));
                Assert.AreSame(bStyle, b.Style); Assert.AreSame(cStyle, c.Style);
                Assert.AreEqual(bType, b.SpaceType); Assert.AreEqual(cType, c.SpaceType);
                Assert.AreEqual("Preserve B choice", b.AuthoringMetadata); Assert.AreEqual("Preserve C choice", c.AuthoringMetadata);
                Assert.AreEqual(4, layout.GetComponentsInChildren<SpriteRenderer>().Count(r => r.name.StartsWith("Partition")), "Only B/C shared boundary remains.");
                var after = ShipLayoutDiagnosticTests.Snapshot(layout);
                Undo.PerformUndo(); CollectionAssert.AreEqual(before, ShipLayoutDiagnosticTests.Snapshot(layout));
                Undo.PerformRedo(); CollectionAssert.AreEqual(after, ShipLayoutDiagnosticTests.Snapshot(layout));
                Assert.AreEqual(new Vector3(20, 30, 0), sentinel.transform.position); Assert.AreSame(b.transform, decoration.transform.parent);
                Assert.IsTrue(RoomAuthoringValidator.IsCurrent(layout));
                Assert.IsTrue(RoomAuthoringOperations.Rebuild(c)); CollectionAssert.AreEqual(after, ShipLayoutDiagnosticTests.Snapshot(layout));
            }
            finally { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); AssetDatabase.DeleteAsset(folder); }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SharedCompleteAndPartialScenesReopenWithoutGenerationAndRebuildEquivalently(bool partial)
        {
            Assert.IsTrue(Environment.GetCommandLineArgs().Contains("-solitudeRoomAuthoringVerification"));
            var folder = ShipLayoutDiagnosticTests.CreateSharedAssets();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); var calls = 0;
            void Generated(ShipRoomAuthoring _) => calls++;
            try
            {
                var layout = ShipLayoutDiagnosticTests.CreateSharedLayout(scene, folder);
                if (partial)
                {
                    layout.DefaultStyle.JunctionModules.Single(b => b.Role == LayoutJunctionRole.TeeEast).Artwork.Sprite = null;
                    EditorUtility.SetDirty(layout.DefaultStyle); AssetDatabase.SaveAssets();
                    Assert.IsFalse(RoomAuthoringOperations.Rebuild(layout));
                }
                Assert.AreEqual(partial ? RoomGenerationStatus.IncompletePreview : RoomGenerationStatus.Complete, layout.GenerationResult.Status);
                var expected = ShipLayoutDiagnosticTests.Snapshot(layout); var id = layout.LayoutId;
                var path = folder + "/Shared.unity"; Assert.IsTrue(EditorSceneManager.SaveScene(scene, path));
                RoomAuthoringOperations.GenerationCompleted += Generated;
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); scene = EditorSceneManager.OpenScene(path);
                layout = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<ShipLayoutAuthoring>()).Single();
                Assert.AreEqual(id, layout.LayoutId); Assert.AreEqual(0, calls); Assert.IsFalse(scene.isDirty);
                CollectionAssert.AreEqual(expected, ShipLayoutDiagnosticTests.Snapshot(layout)); Assert.IsTrue(RoomAuthoringValidator.IsCurrent(layout));
                Assert.AreEqual(48, layout.Spaces.Sum(RoomAuthoringTestFixtures.FloorCount));
                var data = new SerializedObject(layout); data.FindProperty("spaces").MoveArrayElement(0, 2); data.ApplyModifiedPropertiesWithoutUndo();
                Assert.AreEqual(!partial, RoomAuthoringOperations.Rebuild(layout.Spaces[0])); CollectionAssert.AreEqual(expected, ShipLayoutDiagnosticTests.Snapshot(layout));
                Assert.AreEqual(!partial, RoomAuthoringOperations.Rebuild(layout)); CollectionAssert.AreEqual(expected, ShipLayoutDiagnosticTests.Snapshot(layout));
            }
            finally
            {
                RoomAuthoringOperations.GenerationCompleted -= Generated;
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); AssetDatabase.DeleteAsset(folder);
            }
        }

        private static void AssertExteriorRectangle(ShipRoomAuthoring room, int width, int height)
        {
            var sprites = room.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>(true);
            Assert.AreEqual(width * 2 + height * 2 + 4, sprites.Length);
            foreach (var side in new[] { "North", "South", "East", "West" })
            {
                var panels = sprites.Where(renderer => renderer.name == side).ToArray();
                Assert.AreEqual(side == "North" || side == "South" ? width : height, panels.Length, side);
                foreach (var panel in panels)
                {
                    var bounds = panel.bounds;
                    if (side == "South")
                    { Assert.AreEqual(-1f, bounds.min.y, .0001f); Assert.AreEqual(0f, bounds.max.y, .0001f); }
                    if (side == "West")
                    { Assert.AreEqual(-1f, bounds.min.x, .0001f); Assert.AreEqual(0f, bounds.max.x, .0001f); }
                    if (side == "East")
                    { Assert.AreEqual(width, bounds.min.x, .0001f); Assert.AreEqual(width + 1, bounds.max.x, .0001f); }
                    if (side == "North")
                    { Assert.AreEqual(height, bounds.min.y, .0001f); Assert.AreEqual(height + 2, bounds.max.y, .0001f); }
                }
            }
            var corners = new[] { "Southwest", "Southeast", "Northwest", "Northeast" };
            var origins = new[] { new Vector2(-1, -1), new Vector2(width, -1), new Vector2(-1, height), new Vector2(width, height) };
            for (var i = 0; i < corners.Length; i++)
            {
                var bounds = sprites.Single(renderer => renderer.name == corners[i]).bounds;
                Assert.AreEqual(origins[i].x, bounds.min.x, .0001f, corners[i]);
                Assert.AreEqual(origins[i].y, bounds.min.y, .0001f, corners[i]);
                Assert.AreEqual(1f, bounds.size.x, .0001f);
                Assert.AreEqual(1f, bounds.size.y, .0001f);
            }
            AssertCapsAndCornersLeaveFloorClear(room);
        }

        private static void AssertCapsAndCornersLeaveFloorClear(ShipRoomAuthoring room)
        {
            var cells = RoomAuthoringOperations.ReadCells(room);
            foreach (var panel in room.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>(true).Where(renderer => renderer.name != "North"))
                foreach (var cell in cells)
                {
                    var origin = room.Occupancy.CellToWorld(new Vector3Int(cell.x, cell.y, 0));
                    var bounds = panel.bounds;
                    var overlapX = Mathf.Min(bounds.max.x, origin.x + 1) - Mathf.Max(bounds.min.x, origin.x);
                    var overlapY = Mathf.Min(bounds.max.y, origin.y + 1) - Mathf.Max(bounds.min.y, origin.y);
                    Assert.IsFalse(overlapX > .0001f && overlapY > .0001f,
                        panel.name + " covers authored floor at " + cell);
                }
        }
    }
}
