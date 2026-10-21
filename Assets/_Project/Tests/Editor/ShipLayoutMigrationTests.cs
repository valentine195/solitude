using System;
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
    public sealed class ShipLayoutMigrationTests
    {
        private static Vector3[] WorldCells(ShipRoomAuthoring room) => RoomAuthoringOperations.ReadCells(room)
            .Select(cell => room.Occupancy.CellToWorld(new Vector3Int(cell.x, cell.y, 0))).ToArray();

        private static void MakeIrregular(ShipRoomAuthoring room)
        {
            for (var x = 4; x < 8; x++)
                for (var y = 3; y < 6; y++) room.Occupancy.SetTile(new Vector3Int(x, y, 0), null);
        }

        private static string[] SceneSnapshot(Scene scene) => scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .SelectMany(transform => transform.GetComponents<Component>())
            .Select(component => component.GetEntityId() + ":" + EditorJsonUtility.ToJson(component)).ToArray();

        [TestCase(false)]
        [TestCase(true)]
        public void AdoptionPreservesOffsetMapOutputAndReferencesAndUndoRestoresIndependentRoom(bool irregular)
        {
            using var fixture = new RoomAuthoringTestFixtures(32);
            var room = fixture.Room();
            var parent = new GameObject("Existing Parent");
            SceneManager.MoveGameObjectToScene(parent, fixture.Scene);
            room.transform.SetParent(parent.transform, false);
            parent.transform.position = new Vector3(10, -4, 0);
            room.transform.localPosition = new Vector3(20, 8, 0);
            if (irregular) MakeIrregular(room);
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(room));
            var identity = room.RoomId;
            var map = room.Occupancy;
            var style = room.Style;
            var output = room.GeneratedOutput;
            var objects = output.GetComponentsInChildren<Transform>(true);
            var cells = WorldCells(room);
            var before = RoomUndoRedoTests.Snapshot(room);
            var group = Undo.GetCurrentGroup();
            Assert.IsNull(RoomAuthoringCreation.GetAdoptionIssue(room));
            Assert.AreEqual(group, Undo.GetCurrentGroup(), "Preflight is read-only.");
            var layout = RoomAuthoringCreation.AdoptIntoNewLayout(room);
            var layoutId = layout.LayoutId;
            Assert.AreSame(room, layout.Spaces.Single());
            Assert.AreSame(layout, room.Layout);
            Assert.AreSame(layout, output.LayoutOwner);
            Assert.AreSame(output, layout.GeneratedOutput);
            Assert.AreSame(output, room.GeneratedOutput);
            Assert.AreSame(map, room.Occupancy);
            Assert.AreSame(style, room.Style);
            Assert.AreEqual(identity, room.RoomId);
            CollectionAssert.AreEqual(cells, WorldCells(room));
            CollectionAssert.AreEqual(objects, output.GetComponentsInChildren<Transform>(true));
            StringAssert.Contains(identity, output.AdoptionRecord);
            StringAssert.Contains(layoutId, output.AdoptionRecord);
            StringAssert.Contains("Existing Parent/ShipRoom", output.AdoptionRecord);
            Assert.IsTrue(RoomAuthoringValidator.IsCurrent(layout));
            Assert.AreNotSame(room.GenerationResult, layout.GenerationResult);
            var after = RoomUndoRedoTests.Snapshot(room);
            Undo.PerformUndo();
            Assert.IsTrue(layout == null);
            Assert.IsNull(room.Layout);
            Assert.IsNull(output.LayoutOwner);
            Assert.IsTrue(string.IsNullOrEmpty(output.AdoptionRecord));
            Assert.AreSame(parent.transform, room.transform.parent);
            CollectionAssert.AreEqual(before, RoomUndoRedoTests.Snapshot(room));
            CollectionAssert.AreEqual(cells, WorldCells(room));
            CollectionAssert.AreEqual(objects, output.GetComponentsInChildren<Transform>(true));
            Undo.PerformRedo();
            layout = room.Layout;
            Assert.AreEqual(layoutId, layout.LayoutId);
            Assert.AreSame(output, layout.GeneratedOutput);
            Assert.AreSame(layout, output.LayoutOwner);
            CollectionAssert.AreEqual(after, RoomUndoRedoTests.Snapshot(room));
            CollectionAssert.AreEqual(cells, WorldCells(room));
            Assert.IsTrue(RoomAuthoringValidator.IsCurrent(layout));
        }

        [TestCase("empty")]
        [TestCase("incomplete")]
        public void AdoptionUndoRedoRestoresTheEntireResultWithoutSharedMutableSnapshots(string state)
        {
            using var fixture = new RoomAuthoringTestFixtures(32);
            var room = state == "empty" ? fixture.Room(0, 0) : fixture.Room();
            if (state == "incomplete") fixture.Style.FloorTile = null;
            RoomAuthoringOperations.Rebuild(room);
            Assert.AreEqual(state == "empty" ? RoomGenerationStatus.Empty : RoomGenerationStatus.IncompletePreview,
                room.GenerationResult.Status);
            var before = RoomUndoRedoTests.Snapshot(room);
            var layout = RoomAuthoringCreation.AdoptIntoNewLayout(room);
            var after = RoomUndoRedoTests.Snapshot(room);
            var layoutResult = JsonUtility.ToJson(layout.GenerationResult);
            Assert.AreNotSame(room.GenerationResult, layout.GenerationResult);
            if (room.GenerationResult.Issues.Length != 0)
            {
                Assert.AreNotSame(room.GenerationResult.Issues, layout.GenerationResult.Issues);
                Assert.AreNotSame(room.GenerationResult.Issues[0], layout.GenerationResult.Issues[0]);
            }
            Undo.PerformUndo();
            Assert.IsNull(room.Layout);
            CollectionAssert.AreEqual(before, RoomUndoRedoTests.Snapshot(room));
            Undo.PerformRedo();
            layout = room.Layout;
            CollectionAssert.AreEqual(after, RoomUndoRedoTests.Snapshot(room));
            Assert.AreEqual(layoutResult, JsonUtility.ToJson(layout.GenerationResult));
            Assert.IsTrue(RoomAuthoringValidator.IsCurrent(layout));
        }

        [TestCase("unknown-child")]
        [TestCase("unknown-component")]
        [TestCase("nested-content")]
        [TestCase("unsafe-transform")]
        [TestCase("overlap")]
        [TestCase("duplicate-output")]
        [TestCase("shared-map")]
        [TestCase("duplicate-association")]
        [TestCase("stale-output")]
        [TestCase("nested-room")]
        public void UnsafeAdoptionRefusesBeforeAnySceneOrUndoMutation(string fault)
        {
            using var fixture = new RoomAuthoringTestFixtures(32);
            var room = fixture.Room();
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(room));
            if (fault == "unknown-child") new GameObject("Authored Console").transform.SetParent(room.GeneratedOutput.transform, false);
            else if (fault == "unknown-component") room.GeneratedOutput.GetComponentInChildren<SpriteRenderer>().gameObject.AddComponent<BoxCollider2D>();
            else if (fault == "nested-content") new GameObject("Authored Child").transform.SetParent(room.GeneratedOutput.transform.GetChild(0), false);
            else if (fault == "unsafe-transform") room.transform.position = Vector3.right * 0.5f;
            else if (fault == "overlap") fixture.Room();
            else if (fault == "duplicate-output") Object.Instantiate(room.GeneratedOutput.gameObject, room.transform);
            else if (fault == "shared-map")
            {
                var other = fixture.Room();
                var data = new SerializedObject(other);
                data.FindProperty("occupancy").objectReferenceValue = room.Occupancy;
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            else if (fault == "duplicate-association")
            {
                var other = RoomAuthoringCreation.CreateLayout(fixture.Scene, fixture.Style);
                var data = new SerializedObject(other);
                data.FindProperty("spaces").GetArrayElementAtIndex(0).objectReferenceValue = room;
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            else if (fault == "nested-room")
            {
                var other = fixture.Room();
                other.transform.SetParent(room.transform, false);
                other.transform.localPosition = Vector3.right * 20;
            }
            else room.Occupancy.SetTile(Vector3Int.zero, null);
            var before = SceneSnapshot(fixture.Scene);
            var group = Undo.GetCurrentGroup();
            var dirty = fixture.Scene.isDirty;
            Assert.IsNotNull(RoomAuthoringCreation.GetAdoptionIssue(room));
            Assert.Throws<InvalidOperationException>(() => RoomAuthoringCreation.AdoptIntoNewLayout(room));
            CollectionAssert.AreEqual(before, SceneSnapshot(fixture.Scene));
            Assert.AreEqual(group, Undo.GetCurrentGroup());
            Assert.AreEqual(dirty, fixture.Scene.isDirty);
        }

        [Test] public void AdoptionDoesNotGenerateAndBothEntryPointsHaveOneAuthorityWhileLegacyStillWorks()
        {
            using var fixture = new RoomAuthoringTestFixtures();
            var room = fixture.Room();
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(room));
            var applications = 0;
            void Generated(ShipRoomAuthoring _) => applications++;
            RoomAuthoringOperations.GenerationCompleted += Generated;
            try
            {
                var layout = RoomAuthoringCreation.AdoptIntoNewLayout(room);
                Assert.AreEqual(0, applications);
                Assert.IsTrue(RoomAuthoringOperations.Rebuild(room));
                Assert.AreEqual(1, applications);
                Assert.AreNotSame(room.GenerationResult, layout.GenerationResult);
                Assert.IsTrue(RoomAuthoringOperations.Rebuild(layout));
                Assert.AreEqual(2, applications);
                Assert.AreNotSame(room.GenerationResult, layout.GenerationResult);
                Assert.AreEqual(48, RoomAuthoringTestFixtures.FloorCount(room));
                Assert.AreEqual(1, layout.GetComponentsInChildren<RoomGeneratedOutput>(true).Length);
                var legacy = fixture.Room(); legacy.transform.position = Vector3.right * 20;
                Assert.IsTrue(RoomAuthoringOperations.Rebuild(legacy));
                Assert.IsNull(legacy.Layout);
                Assert.IsNull(legacy.GeneratedOutput.LayoutOwner);
            }
            finally { RoomAuthoringOperations.GenerationCompleted -= Generated; }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SavedAdoptionPreservesAssetAndSceneIdentitiesAndReopenDoesNotGenerate(bool irregular)
        {
            Assert.IsTrue(Environment.GetCommandLineArgs().Contains("-solitudeRoomAuthoringVerification"));
            var folder = RoomPersistenceTests.CreateAssets();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var applications = 0;
            void Generated(ShipRoomAuthoring _) => applications++;
            try
            {
                var room = RoomPersistenceTests.CreateRoom(scene, folder);
                room.transform.position = new Vector3(-20, 8, 0);
                if (irregular) MakeIrregular(room);
                RoomAuthoringOperations.Rebuild(room);
                var path = folder + "/Adoption.unity";
                Assert.IsTrue(EditorSceneManager.SaveScene(scene, path));
                var identity = room.RoomId;
                var cells = WorldCells(room);
                var mapId = GlobalObjectId.GetGlobalObjectIdSlow(room.Occupancy);
                var roomId = GlobalObjectId.GetGlobalObjectIdSlow(room);
                var outputId = GlobalObjectId.GetGlobalObjectIdSlow(room.GeneratedOutput);
                var styleGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(room.Style));
                var scriptGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(room)));
                var output = RoomAuthoringValidator.OutputFingerprint(room);
                RoomAuthoringOperations.GenerationCompleted += Generated;
                var layout = RoomAuthoringCreation.AdoptIntoNewLayout(room);
                var layoutId = layout.LayoutId;
                var record = room.GeneratedOutput.AdoptionRecord;
                Assert.IsTrue(EditorSceneManager.SaveScene(scene, path));
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                scene = EditorSceneManager.OpenScene(path);
                room = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<ShipRoomAuthoring>()).Single();
                Selection.activeGameObject = room.gameObject;
                Assert.AreEqual(0, applications);
                Assert.AreEqual(identity, room.RoomId);
                Assert.AreEqual(layoutId, room.Layout.LayoutId);
                Assert.AreEqual(mapId, GlobalObjectId.GetGlobalObjectIdSlow(room.Occupancy));
                Assert.AreEqual(roomId, GlobalObjectId.GetGlobalObjectIdSlow(room));
                Assert.AreEqual(outputId, GlobalObjectId.GetGlobalObjectIdSlow(room.GeneratedOutput));
                Assert.AreEqual(styleGuid, AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(room.Style)));
                Assert.AreEqual(scriptGuid, AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(room))));
                CollectionAssert.AreEqual(cells, WorldCells(room));
                Assert.AreEqual(output, RoomAuthoringValidator.OutputFingerprint(room));
                Assert.AreEqual(record, room.GeneratedOutput.AdoptionRecord);
                Assert.AreSame(room.GeneratedOutput, room.Layout.GeneratedOutput);
                Assert.AreSame(room.Layout, room.GeneratedOutput.LayoutOwner);
                Assert.IsTrue(RoomAuthoringValidator.IsCurrent(room.Layout));
                Assert.IsFalse(scene.isDirty);
            }
            finally
            {
                RoomAuthoringOperations.GenerationCompleted -= Generated;
                Selection.activeObject = null;
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                AssetDatabase.DeleteAsset(folder);
            }
        }
    }
}
