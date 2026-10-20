using System.Linq;
using NUnit.Framework;
using SOLITUDE.Editor.RoomAuthoring;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace SOLITUDE.Tests.RoomAuthoring
{
    public sealed class RoomOwnershipTests
    {
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
            second.transform.position = Vector3.right * 7;
            Assert.That(RoomAuthoringValidator.Validate(first), Does.Contain("overlaps"));
            Assert.IsFalse(RoomAuthoringOperations.Rebuild(first));
            Assert.AreEqual(0, first.GeneratedOutput.transform.childCount);
            Assert.AreSame(f.Style.FloorTile, sentinelMap.GetTile(Vector3Int.zero));
            CollectionAssert.AreEqual(otherOutput, second.GeneratedOutput.GetComponentsInChildren<Transform>());
            second.transform.position = Vector3.right * 8;
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(first));
        }
    }
}
