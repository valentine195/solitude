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
    public sealed class RoomPreviewFeedbackTests
    {
        private UnityEngine.Object previousSelection;
        [SetUp] public void Setup() { previousSelection = Selection.activeObject; RoomAuthoringPreview.ClearSelection(); }
        [TearDown] public void Cleanup() { RoomAuthoringPreview.ClearSelection(); Selection.activeObject = previousSelection; }

        [Test] public void SelectingLocatedIssueUsesExpectedWorldBoundsWithoutEditingOrGenerating()
        {
            using var f = new RoomAuthoringTestFixtures();
            var room = f.Room(7, 6); room.transform.position = new Vector3(-12, 5, 0);
            Assert.IsFalse(RoomAuthoringOperations.Rebuild(room));
            Selection.activeGameObject = room.gameObject;
            var issue = RoomAuthoringPreview.GetFeedback(room).Assessment.Issues.Single();
            var before = RoomUndoRedoTests.Snapshot(room);
            var serialized = EditorJsonUtility.ToJson(room);
            var objects = room.GetComponentsInChildren<Transform>(true);
            var dirty = f.Scene.isDirty;
            var group = Undo.GetCurrentGroup();
            var generated = 0;
            void Applied(ShipRoomAuthoring _) => generated++;
            RoomAuthoringOperations.GenerationCompleted += Applied;
            try
            {
                var bounds = RoomAuthoringPreview.IssueBounds(room, issue);
                Assert.AreEqual(new Vector3(-8.5f, 11, 0), bounds.center);
                Assert.AreEqual(7, bounds.size.x);
                Assert.IsTrue(RoomAuthoringPreview.SelectIssue(room, issue.Id, false));
                Assert.AreEqual(issue.Id, RoomAuthoringPreview.SelectedIssueId);
                Assert.AreEqual(serialized, EditorJsonUtility.ToJson(room));
                CollectionAssert.AreEqual(before, RoomUndoRedoTests.Snapshot(room));
                CollectionAssert.AreEqual(objects, room.GetComponentsInChildren<Transform>(true));
                Assert.AreEqual(dirty, f.Scene.isDirty);
                Assert.AreEqual(group, Undo.GetCurrentGroup());
                Assert.AreEqual(0, generated);
            }
            finally { RoomAuthoringOperations.GenerationCompleted -= Applied; }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MissingFloorOrStyleShowsEveryAuthoredCellWithoutAddingRuntimeObjects(bool missingStyle)
        {
            using var f = new RoomAuthoringTestFixtures();
            var room = f.Room();
            if (missingStyle) room.Style = null; else f.Style.FloorTile = null;
            Assert.IsFalse(RoomAuthoringOperations.Rebuild(room));
            Selection.activeGameObject = room.gameObject;
            var before = room.GetComponentsInChildren<Component>(true);
            var feedback = RoomAuthoringPreview.GetFeedback(room);
            Assert.AreEqual(48, feedback.MissingFloorCells.Length);
            CollectionAssert.AreEqual(RoomAuthoringOperations.ReadCells(room), feedback.MissingFloorCells);
            Assert.AreEqual(RoomGenerationStatus.IncompletePreview, feedback.Assessment.Status);
            Assert.AreEqual(missingStyle ? 0 : 28, room.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>().Length);
            var issue = feedback.Assessment.Issues.Single();
            Assert.AreEqual(missingStyle ? "style" : "style.floorTile", RoomAuthoringPreview.FieldLabel(issue));
            Assert.IsTrue(RoomAuthoringPreview.SelectIssue(room, issue.Id, false));
            Assert.IsFalse(issue.HasRegion);
            CollectionAssert.AreEqual(before, room.GetComponentsInChildren<Component>(true));
            Assert.IsFalse(room.Occupancy.gameObject.activeSelf);
            Assert.IsEmpty(room.GetComponentsInChildren<Collider2D>(true));
            Assert.AreEqual(0, RoomAuthoringTestFixtures.FloorCount(room));
        }

        [Test] public void CorrectionRemovesStaleSelectionButRetainsOtherIssuesAndTargetSwitchClearsIt()
        {
            using var f = new RoomAuthoringTestFixtures();
            var room = f.Room(); var other = f.Room(); other.transform.position = Vector3.right * 20;
            var original = f.Style.Northwest.Sprite;
            f.Style.Northwest.Sprite = null; f.Style.FloorTile = null;
            Assert.IsFalse(RoomAuthoringOperations.Rebuild(room));
            Selection.activeGameObject = room.gameObject;
            var corner = RoomAuthoringPreview.GetFeedback(room).Assessment.Issues.Single(i => i.Kind == RoomIssueKind.Join);
            Assert.IsTrue(RoomAuthoringPreview.SelectIssue(room, corner.Id, false));
            f.Style.Northwest.Sprite = original;
            Assert.IsFalse(RoomAuthoringOperations.Rebuild(room));
            Assert.IsNull(RoomAuthoringPreview.SelectedIssueId);
            Assert.AreEqual(RoomIssueKind.Floor, RoomAuthoringPreview.GetFeedback(room).Assessment.Issues.Single().Kind);
            var floor = RoomAuthoringPreview.GetFeedback(room).Assessment.Issues.Single();
            Assert.IsTrue(RoomAuthoringPreview.SelectIssue(room, floor.Id, false));
            Selection.activeGameObject = other.gameObject;
            Assert.AreSame(other, RoomAuthoringPreview.CurrentRoom);
            Assert.IsNull(RoomAuthoringPreview.SelectedIssueId);
            Assert.IsFalse(RoomAuthoringPreview.SelectIssue(room, floor.Id, false));
            UnityEngine.Object.DestroyImmediate(other.gameObject);
            Assert.IsNull(RoomAuthoringPreview.CurrentRoom);
        }

        [Test] public void ValidateAndSelectionOnSavedScenePreserveCompleteSerializedStateAndCleanDirtiness()
        {
            Assert.IsTrue(Environment.GetCommandLineArgs().Contains("-solitudeRoomAuthoringVerification"));
            var folder = RoomPersistenceTests.CreateAssets();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            try
            {
                var room = RoomPersistenceTests.CreateRoom(scene, folder);
                room.Occupancy.SetTile(new Vector3Int(6, 4, 0), null);
                Assert.IsFalse(RoomAuthoringOperations.Rebuild(room));
                Assert.IsTrue(EditorSceneManager.SaveScene(scene, folder + "/Partial.unity"));
                Selection.activeGameObject = room.gameObject;
                var state = RoomUndoRedoTests.Snapshot(room);
                var serialized = EditorJsonUtility.ToJson(room);
                var group = Undo.GetCurrentGroup();
                var feedback = RoomAuthoringPreview.GetFeedback(room);
                Assert.IsTrue(feedback.IsCurrent);
                Assert.IsNotEmpty(feedback.Assessment.Issues);
                Assert.IsNotNull(RoomAuthoringValidator.Validate(room));
                Assert.IsTrue(RoomAuthoringPreview.SelectIssue(room, feedback.Assessment.Issues.First().Id, false));
                Assert.IsFalse(scene.isDirty);
                Assert.AreEqual(group, Undo.GetCurrentGroup());
                Assert.AreEqual(serialized, EditorJsonUtility.ToJson(room));
                CollectionAssert.AreEqual(state, RoomUndoRedoTests.Snapshot(room));
            }
            finally
            {
                Selection.activeObject = null;
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                AssetDatabase.DeleteAsset(folder);
            }
        }
    }
}
