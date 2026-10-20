using System.Linq;
using NUnit.Framework;
using SOLITUDE.Editor.RoomAuthoring;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace SOLITUDE.Tests.RoomAuthoring
{
    public sealed class RoomOwnershipTests
    {
        [TestCase("valid", false)]
        [TestCase("invalid", false)]
        [TestCase("empty", false)]
        [TestCase("valid", true)]
        public void DuplicateOutputOwnerRefusesRebuildUntilExtraRootIsRemoved(string state, bool separateScene)
        {
            using var f = new RoomAuthoringTestFixtures();
            var room = f.Room();
            var neighbor = f.Room(); neighbor.transform.position = Vector3.right * 8;
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(room));
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(neighbor));
            var duplicate = UnityEngine.Object.Instantiate(room.GeneratedOutput.gameObject, room.transform);
            var foreignScene = default(Scene);
            try
            {
                Assert.AreSame(room, duplicate.GetComponent<SOLITUDE.World.RoomAuthoring.RoomGeneratedOutput>().Owner);
                if (separateScene)
                {
                    duplicate.transform.SetParent(null);
                    foreignScene = EditorSceneManager.NewPreviewScene();
                    SceneManager.MoveGameObjectToScene(duplicate, foreignScene);
                }
                if (state == "invalid") f.Style.NorthWalls[0].Sprite = null;
                if (state == "empty") room.Occupancy.ClearAllTiles();
                var cells = RoomAuthoringOperations.ReadCells(room);
                var primaryObjects = room.GeneratedOutput.GetComponentsInChildren<Transform>(true);
                var duplicateObjects = duplicate.GetComponentsInChildren<Transform>(true);
                var neighborObjects = neighbor.GeneratedOutput.GetComponentsInChildren<Transform>(true);
                Assert.That(RoomAuthoringValidator.Validate(room), Does.Contain("exactly one output root"));
                Assert.IsFalse(RoomAuthoringOperations.Rebuild(room));
                Assert.That(room.GenerationDiagnostic, Does.Contain("exactly one output root"));
                Assert.Throws<System.InvalidOperationException>(() => RoomGeometryWriter.Clear(room));
                CollectionAssert.AreEqual(cells, RoomAuthoringOperations.ReadCells(room));
                CollectionAssert.AreEqual(primaryObjects, room.GeneratedOutput.GetComponentsInChildren<Transform>(true));
                CollectionAssert.AreEqual(duplicateObjects, duplicate.GetComponentsInChildren<Transform>(true));
                CollectionAssert.AreEqual(neighborObjects, neighbor.GeneratedOutput.GetComponentsInChildren<Transform>(true));
                UnityEngine.Object.DestroyImmediate(duplicate);
                var rebuilt = RoomAuthoringOperations.Rebuild(room);
                Assert.AreEqual(state != "invalid", rebuilt, room.GenerationDiagnostic);
                Assert.AreEqual(state == "valid" ? 28 : state == "invalid" ? 24 : 0, room.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>().Length);
                CollectionAssert.AreEqual(cells, RoomAuthoringOperations.ReadCells(room));
                CollectionAssert.AreEqual(neighborObjects, neighbor.GeneratedOutput.GetComponentsInChildren<Transform>(true));
            }
            finally
            {
                if (foreignScene.IsValid() && foreignScene.isLoaded) EditorSceneManager.ClosePreviewScene(foreignScene);
            }
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void ForeignAuthoredDescendantRefusesCleanupAndPreservesBothRooms(bool nested, bool separateScene)
        {
            using var f = new RoomAuthoringTestFixtures();
            var first = f.Room(); var second = f.Room(); second.transform.position = Vector3.right * 8;
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(first));
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(second));
            var foreignScene = default(Scene);
            try
            {
                var authored = second.Occupancy;
                var firstCells = RoomAuthoringOperations.ReadCells(first);
                var secondCells = RoomAuthoringOperations.ReadCells(second);
                var secondOutput = second.GeneratedOutput.GetComponentsInChildren<Transform>(true);
                var parent = first.GeneratedOutput.transform;
                if (nested)
                {
                    var container = new GameObject("Ordinary Container"); container.transform.SetParent(parent, false);
                    parent = container.transform;
                }
                authored.transform.SetParent(parent, false);
                if (separateScene)
                {
                    foreignScene = EditorSceneManager.NewPreviewScene();
                    SceneManager.MoveGameObjectToScene(second.gameObject, foreignScene);
                }
                var firstOutput = first.GeneratedOutput.GetComponentsInChildren<Transform>(true);
                Assert.That(RoomAuthoringValidator.Validate(first), Does.Contain("referenced authored occupancy"));
                Assert.IsFalse(RoomAuthoringOperations.Rebuild(first));
                Assert.That(first.GenerationDiagnostic, Does.Contain("referenced authored occupancy"));
                Assert.Throws<System.InvalidOperationException>(() => RoomGeometryWriter.Clear(first));
                Assert.AreSame(authored, second.Occupancy);
                CollectionAssert.AreEqual(firstCells, RoomAuthoringOperations.ReadCells(first));
                CollectionAssert.AreEqual(secondCells, RoomAuthoringOperations.ReadCells(second));
                CollectionAssert.AreEqual(firstOutput, first.GeneratedOutput.GetComponentsInChildren<Transform>(true));
                CollectionAssert.AreEqual(secondOutput, second.GeneratedOutput.GetComponentsInChildren<Transform>(true));
                authored.transform.SetParent(second.transform, false);
                Assert.IsTrue(RoomAuthoringOperations.Rebuild(first), first.GenerationDiagnostic);
                Assert.IsTrue(RoomAuthoringOperations.Rebuild(second), second.GenerationDiagnostic);
            }
            finally
            {
                if (foreignScene.IsValid() && foreignScene.isLoaded) EditorSceneManager.ClosePreviewScene(foreignScene);
            }
        }

        [TestCase("identity")]
        [TestCase("foreign")]
        public void AmbiguityRefusesDestructionOfBothRooms(string fault)
        {
            using var f = new RoomAuthoringTestFixtures();
            var first = f.Room(); var second = f.Room(); second.transform.position = Vector3.right * 8;
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(first));
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(second));
            var firstObjects = first.GeneratedOutput.GetComponentsInChildren<Transform>();
            var secondRoot = second.GeneratedOutput;
            var secondObjects = secondRoot.GetComponentsInChildren<Transform>();
            var serialized = new SerializedObject(second);
            if (fault == "identity") serialized.FindProperty("roomId").stringValue = first.RoomId;
            else serialized.FindProperty("generatedOutput").objectReferenceValue = first.GeneratedOutput;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.IsFalse(RoomAuthoringOperations.Rebuild(second));
            Assert.IsNotEmpty(second.GenerationDiagnostic);
            CollectionAssert.AreEqual(firstObjects, first.GeneratedOutput.GetComponentsInChildren<Transform>());
            CollectionAssert.AreEqual(secondObjects, secondRoot.GetComponentsInChildren<Transform>());
            Assert.AreEqual(48, RoomAuthoringOperations.ReadCells(first).Length);
            Assert.AreEqual(48, RoomAuthoringOperations.ReadCells(second).Length);
        }

        [Test] public void OverlapIsDiagnosedAndSameNameSentinelIsPreserved()
        {
            using var f = new RoomAuthoringTestFixtures();
            var first = f.Room(); var second = f.Room(); second.transform.position = Vector3.right * 8;
            var sentinel = new GameObject("Generated Floor", typeof(Tilemap));
            SceneManager.MoveGameObjectToScene(sentinel, f.Scene);
            var sentinelMap = sentinel.GetComponent<Tilemap>(); sentinelMap.SetTile(Vector3Int.zero, f.Style.FloorTile);
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(first));
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(second));
            var otherOutput = second.GeneratedOutput.GetComponentsInChildren<Transform>();
            var retainedOutput = first.GeneratedOutput.GetComponentsInChildren<Transform>();
            second.transform.position = Vector3.right * 7;
            Assert.That(RoomAuthoringValidator.Validate(first), Does.Contain("overlaps"));
            Assert.IsFalse(RoomAuthoringOperations.Rebuild(first));
            CollectionAssert.AreEqual(retainedOutput, first.GeneratedOutput.GetComponentsInChildren<Transform>());
            Assert.AreEqual(SOLITUDE.World.RoomAuthoring.RoomGenerationStatus.Refused, first.GenerationResult.Status);
            Assert.AreSame(f.Style.FloorTile, sentinelMap.GetTile(Vector3Int.zero));
            CollectionAssert.AreEqual(otherOutput, second.GeneratedOutput.GetComponentsInChildren<Transform>());
            second.transform.position = Vector3.right * 8;
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(first));
        }
    }
}
