using System;
using System.Linq;
using NUnit.Framework;
using SOLITUDE.Editor.RoomAuthoring;
using SOLITUDE.World.RoomAuthoring;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace SOLITUDE.Tests.RoomAuthoring
{
    public sealed class RoomEditingWorkflowTests
    {
        [Test] public void ResizeEraseUndoEmptyAndRecoverPreserveOtherRoomAndUnownedSentinels()
        {
            using var f = new RoomAuthoringTestFixtures();
            var room = f.Room(0, 0); var other = f.Room(); other.transform.position = Vector3.right * 20;
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(other));
            var otherState = RoomUndoRedoTests.Snapshot(other);
            var otherObjects = other.GetComponentsInChildren<Transform>(true);
            var sentinel = new GameObject("Generated Architecture"); sentinel.transform.SetParent(room.transform, false);
            sentinel.transform.localPosition = new Vector3(3, 9, 0);
            var brush = ScriptableObject.CreateInstance<ShipRoomBrush>(); brush.OccupancyMarker = f.Style.FloorTile;
            void UnrelatedUnchanged()
            {
                CollectionAssert.AreEqual(otherState, RoomUndoRedoTests.Snapshot(other));
                CollectionAssert.AreEqual(otherObjects, other.GetComponentsInChildren<Transform>(true));
                Assert.AreEqual(room.transform, sentinel.transform.parent);
                Assert.AreEqual(new Vector3(3, 9, 0), sentinel.transform.localPosition);
            }
            try
            {
                var grid = room.GetComponent<Grid>(); var target = room.Occupancy.gameObject;
                brush.BoxFill(grid, target, new BoundsInt(0, 0, 0, 8, 6, 1));
                Assert.AreEqual(48, RoomAuthoringOperations.ReadCells(room).Length); UnrelatedUnchanged();
                brush.BoxFill(grid, target, new BoundsInt(8, 0, 0, 4, 6, 1));
                Assert.AreEqual(72, RoomAuthoringOperations.ReadCells(room).Length); UnrelatedUnchanged();
                var valid = RoomUndoRedoTests.Snapshot(room);
                Assert.IsTrue(RoomAuthoringOperations.Rebuild(room));
                Assert.IsTrue(RoomAuthoringOperations.Rebuild(room));
                CollectionAssert.AreEqual(valid, RoomUndoRedoTests.Snapshot(room)); UnrelatedUnchanged();
                brush.Erase(grid, target, new Vector3Int(11, 5, 0));
                Assert.AreEqual(71, RoomAuthoringOperations.ReadCells(room).Length);
                Assert.AreEqual(71, RoomAuthoringTestFixtures.FloorCount(room));
                Assert.That(room.GenerationDiagnostic, Does.Contain("not exactly coverable"));
                var invalid = RoomUndoRedoTests.Snapshot(room); UnrelatedUnchanged();
                Undo.PerformUndo(); CollectionAssert.AreEqual(valid, RoomUndoRedoTests.Snapshot(room)); UnrelatedUnchanged();
                Undo.PerformRedo(); CollectionAssert.AreEqual(invalid, RoomUndoRedoTests.Snapshot(room)); UnrelatedUnchanged();
                Undo.PerformUndo();
                brush.BoxMode = ShipRoomBrush.BoxEditMode.Erase;
                brush.BoxFill(grid, target, new BoundsInt(0, 0, 0, 12, 6, 1));
                Assert.IsEmpty(RoomAuthoringOperations.ReadCells(room));
                Assert.IsEmpty(room.GenerationDiagnostic); Assert.AreEqual(0, room.GeneratedOutput.transform.childCount); UnrelatedUnchanged();
                Undo.PerformUndo(); CollectionAssert.AreEqual(valid, RoomUndoRedoTests.Snapshot(room)); UnrelatedUnchanged();
                Object.DestroyImmediate(room.GeneratedOutput.GetComponentInChildren<Tilemap>().gameObject);
                Assert.IsTrue(RoomAuthoringOperations.Rebuild(room));
                CollectionAssert.AreEqual(valid, RoomUndoRedoTests.Snapshot(room)); UnrelatedUnchanged();
            }
            finally { Object.DestroyImmediate(brush); }
        }

        [Test] public void ExternalOccupancyValidatePreservesCleanSceneAndRebuildRecoversCurrentCells()
        {
            Assert.IsTrue(Environment.GetCommandLineArgs().Contains("-solitudeRoomAuthoringVerification"));
            var folder = RoomPersistenceTests.CreateAssets();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            try
            {
                var room = RoomPersistenceTests.CreateRoom(scene, folder);
                var bounds = new BoundsInt(8, 0, 0, 4, 6, 1);
                room.Occupancy.SetTilesBlock(bounds, Enumerable.Repeat(room.Style.FloorTile, 24).ToArray());
                Assert.IsTrue(EditorSceneManager.SaveScene(scene, folder + "/ExternalEdit.unity"));
                var before = RoomUndoRedoTests.Snapshot(room);
                var objects = room.GetComponentsInChildren<Transform>(true);
                Assert.IsFalse(scene.isDirty);
                Assert.IsNull(RoomAuthoringValidator.Validate(room));
                Assert.IsFalse(RoomAuthoringValidator.IsCurrent(room), "External edits must leave stored output stale.");
                Assert.IsFalse(scene.isDirty);
                CollectionAssert.AreEqual(before, RoomUndoRedoTests.Snapshot(room));
                CollectionAssert.AreEqual(objects, room.GetComponentsInChildren<Transform>(true));
                Assert.IsTrue(RoomAuthoringOperations.Rebuild(room));
                Assert.AreEqual(72, RoomAuthoringOperations.ReadCells(room).Length);
                Assert.AreEqual(72, RoomUndoRedoTests.Snapshot(room).Count(s => s.StartsWith("floor:")));
                var expected = RoomUndoRedoTests.Snapshot(room);
                Object.DestroyImmediate(room.GeneratedOutput.GetComponentInChildren<SpriteRenderer>().gameObject);
                Assert.IsTrue(RoomAuthoringOperations.Rebuild(room));
                CollectionAssert.AreEqual(expected, RoomUndoRedoTests.Snapshot(room));
                var path = folder + "/ExternalEdit.unity";
                Assert.IsTrue(EditorSceneManager.SaveScene(scene, path));
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                scene = EditorSceneManager.OpenScene(path);
                room = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ShipRoomAuthoring>()).Single();
                CollectionAssert.AreEqual(expected, RoomUndoRedoTests.Snapshot(room));
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                AssetDatabase.DeleteAsset(folder);
            }
        }
    }
}
