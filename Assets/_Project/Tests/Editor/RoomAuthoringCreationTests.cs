using System.Linq;
using NUnit.Framework;
using SOLITUDE.Editor.RoomAuthoring;
using SOLITUDE.World.RoomAuthoring;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace SOLITUDE.Tests.RoomAuthoring
{
    public sealed class RoomAuthoringCreationTests
    {
        private Scene scene;
        private Scene previousScene;
        private RoomStyleDefinition style;

        [SetUp] public void SetUp()
        {
            previousScene = SceneManager.GetActiveScene();
            scene = EditorSceneManager.NewPreviewScene();
            style = ScriptableObject.CreateInstance<RoomStyleDefinition>();
        }

        [TearDown] public void TearDown()
        {
            if (scene.IsValid() && scene.isLoaded) EditorSceneManager.ClosePreviewScene(scene);
            if (previousScene.IsValid() && previousScene.isLoaded) SceneManager.SetActiveScene(previousScene);
            Object.DestroyImmediate(style);
        }

        [Test] public void AdjacentRoomsShareStyleWithoutSharingIdentityOrOutput()
        {
            var first = RoomAuthoringCreation.Create(scene, style);
            var second = RoomAuthoringCreation.Create(scene, style);
            second.transform.position = new Vector3(8, 0, 0);
            Assert.AreSame(first.Style, second.Style);
            Assert.IsNotEmpty(first.RoomId);
            Assert.AreNotEqual(first.RoomId, second.RoomId);
            Assert.AreNotSame(first.Occupancy, second.Occupancy);
            Assert.AreNotSame(first.GeneratedOutput, second.GeneratedOutput);
            Assert.AreSame(first, first.GeneratedOutput.Owner);
            Assert.IsNull(RoomAuthoringCreation.GetOwnershipIssue(first));
            Assert.IsNull(RoomAuthoringCreation.GetOwnershipIssue(second));
        }

        [Test] public void StyleChangeAndDeletingOutputDoNotChangeAuthoredCellsOrIdentity()
        {
            var room = RoomAuthoringCreation.Create(scene, style);
            var tile = ScriptableObject.CreateInstance<Tile>();
            try
            {
                room.Occupancy.SetTile(new Vector3Int(2, 3, 0), tile);
                var identity = room.RoomId;
                style.DisplayName = "Renamed";
                room.Style = null;
                Object.DestroyImmediate(room.GeneratedOutput.gameObject);
                Assert.AreEqual(identity, room.RoomId);
                Assert.AreSame(tile, room.Occupancy.GetTile(new Vector3Int(2, 3, 0)));
                Assert.That(RoomAuthoringCreation.GetOwnershipIssue(room), Does.Contain("missing"));
            }
            finally { Object.DestroyImmediate(tile); }
        }

        [Test] public void NewRoomHasNativeGridAndSeparateInactiveAuthoringMask()
        {
            var room = RoomAuthoringCreation.Create(scene);
            Assert.AreEqual(Vector3.one, room.GetComponent<Grid>().cellSize);
            Assert.AreEqual(Vector3.one, room.transform.localScale);
            Assert.IsFalse(room.Occupancy.gameObject.activeSelf);
            Assert.AreEqual("EditorOnly", room.Occupancy.tag);
            Assert.IsFalse(room.Occupancy.transform.IsChildOf(room.GeneratedOutput.transform));
            Assert.IsNull(room.Style);
            Assert.AreEqual(32, RoomStyleDefinition.PixelsPerUnit);
        }

        [Test] public void DuplicateIdentityAndForeignOutputAreDiagnosedWithoutRepair()
        {
            var first = RoomAuthoringCreation.Create(scene, style);
            var second = RoomAuthoringCreation.Create(scene, style);
            var serialized = new SerializedObject(second);
            serialized.FindProperty("roomId").stringValue = first.RoomId;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(RoomAuthoringCreation.GetOwnershipIssue(first), Does.Contain("ambiguous"));
            Assert.AreEqual(first.RoomId, second.RoomId);
            serialized.FindProperty("roomId").stringValue = "distinct";
            serialized.FindProperty("generatedOutput").objectReferenceValue = first.GeneratedOutput;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.IsNotNull(RoomAuthoringCreation.GetOwnershipIssue(second));
            Assert.AreSame(first, first.GeneratedOutput.Owner);
            Assert.IsNotNull(first.GeneratedOutput);
        }

        [Test] public void CreationPreservesExistingSceneObjectsAndRuntimeAssemblyBoundary()
        {
            var sentinel = new GameObject("Generated Architecture", typeof(BoxCollider2D));
            SceneManager.MoveGameObjectToScene(sentinel, scene);
            sentinel.transform.position = new Vector3(7, 11, 0);
            var collider = sentinel.GetComponent<BoxCollider2D>();
            var room = RoomAuthoringCreation.Create(scene, style);
            Assert.IsTrue(scene.isDirty);
            Assert.AreEqual(new Vector3(7, 11, 0), sentinel.transform.position);
            Assert.AreSame(collider, sentinel.GetComponent<BoxCollider2D>());
            Assert.IsNull(RoomAuthoringCreation.GetOwnershipIssue(room));
            var playerAssembly = CompilationPipeline.GetAssemblies(AssembliesType.Player)
                .Single(a => a.name == typeof(ShipRoomAuthoring).Assembly.GetName().Name);
            Assert.That(playerAssembly.sourceFiles, Has.Some.EndsWith("RoomAuthoring/ShipRoomAuthoring.cs"));
            Assert.IsFalse(playerAssembly.allReferences.Any(reference =>
                System.IO.Path.GetFileNameWithoutExtension(reference).StartsWith("UnityEditor")));
        }
    }
}
