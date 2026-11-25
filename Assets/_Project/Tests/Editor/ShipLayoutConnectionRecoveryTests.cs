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
    public sealed class ShipLayoutConnectionRecoveryTests
    {
        private string folder;
        private ShipLayoutAuthoring layout;
        private ShipRoomAuthoring a, b;
        private LayoutConnectionDefinition connection;
        private GameObject sentinel;
        private int generations;

        [SetUp] public void SetUp()
        {
            folder = ShipLayoutDiagnosticTests.CreateSharedAssets();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            layout = RoomAuthoringCreation.CreateLayout(scene, AssetDatabase.LoadAssetAtPath<RoomStyleDefinition>(folder + "/Style.asset"));
            layout.transform.position = new Vector3(-10, -8, 0);
            a = layout.Spaces[0]; b = RoomAuthoringCreation.CreateSpace(layout); b.transform.localPosition = Vector3.up * 4;
            a.AuthoringMetadata = "A recovery fixture"; b.AuthoringMetadata = "B recovery fixture";
            foreach (var room in layout.Spaces)
                for (var y = 0; y < 4; y++) for (var x = 0; x < 8; x++) room.Occupancy.SetTile(new Vector3Int(x, y, 0), room.Style.FloorTile);
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(layout));
            connection = Definition(); Assert.IsTrue(RoomAuthoringOperations.AddConnection(layout, connection, out _));
            sentinel = new GameObject("Generated Floor"); SceneManager.MoveGameObjectToScene(sentinel, scene);
            sentinel.transform.position = new Vector3(25, 30, 0); sentinel.AddComponent<BoxCollider2D>();
            generations = 0; RoomAuthoringOperations.GenerationCompleted += Generated;
        }
        private void Generated(ShipRoomAuthoring _) => generations++;
        private LayoutConnectionDefinition Definition(Vector2? start = null, string owner = null, string id = null, LayoutConnectionType type = LayoutConnectionType.OpenPassage) =>
            new LayoutConnectionDefinition(id ?? connection?.Id ?? Guid.NewGuid().ToString("N"), a.RoomId, owner ?? b.RoomId,
                start ?? new Vector2(2, 4), true, 2, type);
        private string[] Snapshot() => ShipLayoutDiagnosticTests.Snapshot(layout).Concat(layout.Connections.Select(JsonUtility.ToJson)).ToArray();
        private string[] Output() => layout.Spaces.Select(RoomAuthoringValidator.OutputFingerprint).ToArray();
        [TearDown] public void TearDown()
        {
            RoomAuthoringOperations.GenerationCompleted -= Generated; RoomAuthoringOperations.CancelActiveEdit();
            Selection.activeObject = null; RoomAuthoringPreview.ClearSelection();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); AssetDatabase.DeleteAsset(folder);
        }
        private void EraseAllAsOneAction()
        {
            Undo.IncrementCurrentGroup(); var group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Clear fixture occupancy");
            foreach (var room in layout.Spaces) RoomAuthoringOperations.PaintBox(room, new BoundsInt(0, 0, 0, 8, 4, 1), null);
            Undo.CollapseUndoOperations(group); Undo.IncrementCurrentGroup();
        }

        [Test] public void WhollyEmptyLayoutClearsOnlyVerifiedVisualsAndRetainsInactiveConnectionThroughUndoRedo()
        {
            var before = Snapshot(); var record = JsonUtility.ToJson(connection); EraseAllAsOneAction();
            Assert.AreEqual(RoomGenerationStatus.IncompletePreview, layout.GenerationResult.Status, layout.GenerationResult.Summary);
            Assert.IsTrue(layout.GenerationResult.Issues.All(i => i.Inactive && i.Kind == RoomIssueKind.Connection && i.HasRegion));
            Assert.That(layout.GenerationResult.Summary, Does.Contain("Inactive connection retained"));
            Assert.AreEqual(record, JsonUtility.ToJson(layout.Connections.Single()));
            foreach (var room in layout.Spaces) { Assert.IsEmpty(RoomAuthoringOperations.ReadCells(room)); Assert.AreEqual(0, room.GeneratedOutput.transform.childCount); }
            Assert.IsTrue(RoomAuthoringValidator.IsCurrent(layout)); Assert.IsNotNull(sentinel); Assert.AreEqual(new Vector3(25, 30, 0), sentinel.transform.position);
            var empty = Snapshot(); Undo.PerformUndo(); CollectionAssert.AreEqual(before, Snapshot()); Assert.IsTrue(RoomAuthoringValidator.IsCurrent(layout));
            Undo.PerformRedo(); CollectionAssert.AreEqual(empty, Snapshot());
            Assert.IsTrue(RoomAuthoringOperations.RemoveConnection(layout, connection.Id, out _));
            Assert.AreEqual(RoomGenerationStatus.Empty, layout.GenerationResult.Status); Assert.IsEmpty(layout.GenerationResult.Issues);
            Undo.PerformUndo(); CollectionAssert.AreEqual(empty, Snapshot());
        }

        [TestCase("identity")][TestCase("coordinates")][TestCase("unknown-child")][TestCase("root-component")]
        public void UnsafeEmptyLayoutRefusesWithoutDeletingUnverifiedContent(string unsafeCase)
        {
            foreach (var room in layout.Spaces) room.Occupancy.ClearAllTiles();
            if (unsafeCase == "identity")
            { var data = new SerializedObject(b); data.FindProperty("roomId").stringValue = a.RoomId; data.ApplyModifiedPropertiesWithoutUndo(); }
            else if (unsafeCase == "coordinates") b.transform.localPosition += Vector3.right * .5f;
            else if (unsafeCase == "unknown-child") new GameObject("Authored object").transform.SetParent(b.GeneratedOutput.transform, false);
            else b.GeneratedOutput.gameObject.AddComponent<BoxCollider2D>();
            var outputs = Output(); var objects = layout.GetComponentsInChildren<Transform>(true); var record = JsonUtility.ToJson(connection);
            Assert.IsFalse(RoomAuthoringOperations.Rebuild(layout)); Assert.AreEqual(RoomGenerationStatus.Refused, layout.GenerationResult.Status);
            CollectionAssert.AreEqual(outputs, Output()); CollectionAssert.AreEqual(objects, layout.GetComponentsInChildren<Transform>(true));
            Assert.AreEqual(record, JsonUtility.ToJson(layout.Connections.Single())); Assert.AreEqual(0, generations); Assert.IsNotNull(sentinel);
        }

        [Test] public void NonemptyInvalidBoundaryRetainsStaleOutputAndUndoReactivatesOriginalAddress()
        {
            var before = Snapshot(); var output = Output();
            Assert.IsFalse(RoomAuthoringOperations.PaintBox(b, new BoundsInt(2, 0, 0, 2, 1, 1), null));
            Assert.AreEqual(RoomGenerationStatus.Refused, layout.GenerationResult.Status); CollectionAssert.AreEqual(output, Output());
            Assert.IsFalse(RoomAuthoringPreview.GetFeedback(layout).IsCurrent);
            var issue = layout.GenerationResult.Issues.Single(); Assert.IsFalse(issue.Inactive); Assert.IsTrue(issue.HasRegion);
            Assert.That(issue.Reason, Does.Contain("stale").And.Contain("correct or explicitly remove"));
            Assert.AreEqual(JsonUtility.ToJson(connection), JsonUtility.ToJson(layout.Connections.Single()));
            Undo.PerformUndo(); CollectionAssert.AreEqual(before, Snapshot()); Assert.IsTrue(RoomAuthoringValidator.IsCurrent(layout));
        }

        [TestCase(false)][TestCase(true)]
        public void ExplicitCorrectionPreservesIdentityIsAtomicAndUndoesToInvalidRecord(bool removeInstead)
        {
            layout.SetConnections(new[] { Definition(owner: "deleted-owner") });
            Assert.IsFalse(RoomAuthoringOperations.Rebuild(layout)); var before = Snapshot(); var group = Undo.GetCurrentGroup();
            Assert.IsFalse(RoomAuthoringOperations.UpdateConnection(layout, connection.Id, Definition(id: "different-id"), out var issue));
            Assert.That(issue, Does.Contain("identity")); CollectionAssert.AreEqual(before, Snapshot()); Assert.AreEqual(group, Undo.GetCurrentGroup());
            var corrected = removeInstead ? RoomAuthoringOperations.RemoveConnection(layout, connection.Id, out issue) :
                RoomAuthoringOperations.UpdateConnection(layout, connection.Id, Definition(start: new Vector2(3, 4)), out issue);
            Assert.IsTrue(corrected, issue); Assert.AreEqual(1, generations); Assert.IsTrue(RoomAuthoringValidator.IsCurrent(layout));
            if (!removeInstead) { Assert.AreEqual(connection.Id, layout.Connections.Single().Id); Assert.AreEqual(new Vector2(3, 4), layout.Connections.Single().Start); }
            else Assert.IsEmpty(layout.Connections);
            var after = Snapshot(); Undo.PerformUndo(); CollectionAssert.AreEqual(before, Snapshot());
            Undo.PerformRedo(); CollectionAssert.AreEqual(after, Snapshot());
        }

        [Test] public void SeveralInvalidRecordsCanBeCorrectedIndividuallyWithoutPublishingUntilAllAreValid()
        {
            var second = Definition(start: new Vector2(5, 4), owner: "missing-second", id: "second-connection");
            layout.SetConnections(new[] { Definition(owner: "missing-first"), second });
            Assert.IsFalse(RoomAuthoringOperations.Rebuild(layout)); var originalOutput = Output();
            Assert.IsTrue(RoomAuthoringOperations.UpdateConnection(layout, connection.Id, Definition(), out var issue), issue);
            Assert.AreEqual(RoomGenerationStatus.Refused, layout.GenerationResult.Status); CollectionAssert.AreEqual(originalOutput, Output());
            var partiallyCorrected = Snapshot(); var group = Undo.GetCurrentGroup();
            Assert.IsFalse(RoomAuthoringOperations.UpdateConnection(layout, second.Id, Definition(start: new Vector2(3, 4), id: second.Id), out issue));
            Assert.That(issue, Does.Contain("overlap")); CollectionAssert.AreEqual(partiallyCorrected, Snapshot()); Assert.AreEqual(group, Undo.GetCurrentGroup());
            Assert.IsTrue(RoomAuthoringOperations.UpdateConnection(layout, second.Id, Definition(start: new Vector2(5, 4), id: second.Id), out issue), issue);
            Assert.AreEqual(RoomGenerationStatus.Complete, layout.GenerationResult.Status); Assert.IsTrue(RoomAuthoringValidator.IsCurrent(layout));
            Assert.AreEqual(1, generations); Assert.AreEqual(2, layout.Connections.Count);
            Undo.PerformUndo(); CollectionAssert.AreEqual(partiallyCorrected, Snapshot());
        }

        [TestCase(LayoutConnectionType.Doorway)][TestCase(LayoutConnectionType.AirlockOpening)]
        public void MissingFrameIsOnlyALocalAssetIssueAndNeverFillsTheAperture(LayoutConnectionType type)
        {
            Assert.IsTrue(RoomAuthoringOperations.UpdateConnection(layout, connection.Id, Definition(type: type), out _));
            Assert.AreEqual(RoomGenerationStatus.IncompletePreview, layout.GenerationResult.Status);
            var issue = layout.GenerationResult.Issues.Single(); Assert.AreEqual(RoomIssueKind.Asset, issue.Kind);
            Assert.IsTrue(issue.HasRegion); Assert.That(issue.Field, Does.Contain(connection.Id).And.EndWith("/Frame"));
            Assert.IsTrue(RoomAuthoringValidator.IsCurrent(layout)); Assert.AreEqual(32, RoomAuthoringTestFixtures.FloorCount(a)); Assert.AreEqual(32, RoomAuthoringTestFixtures.FloorCount(b));
            var geometry = LayoutBoundaryResolver.Resolve(RoomAuthoringValidator.TopologyOf(layout), layout.Connections);
            Assert.AreEqual(2, geometry.Edges.Count(e => e.Kind == LayoutEdgeKind.Opening));
            Assert.IsTrue(RoomAuthoringOperations.UpdateConnection(layout, connection.Id, Definition(), out _));
            Assert.AreEqual(RoomGenerationStatus.Complete, layout.GenerationResult.Status); Assert.IsEmpty(layout.GenerationResult.Issues);
        }

        [TestCase(false)][TestCase(true)]
        public void InactiveConnectionFocusIsLocatedAndReadOnlyForLayoutAndMember(bool member)
        {
            EraseAllAsOneAction(); var scene = layout.gameObject.scene;
            Assert.IsTrue(EditorSceneManager.SaveScene(scene, folder + "/EmptyConnections.unity"));
            Selection.activeGameObject = member ? a.gameObject : layout.gameObject;
            var issue = layout.GenerationResult.Issues.Single(); var before = Snapshot(); var group = Undo.GetCurrentGroup(); var calls = generations;
            Assert.IsTrue(member ? RoomAuthoringPreview.SelectIssue(a, issue.Id, false) : RoomAuthoringPreview.SelectIssue(layout, issue.Id, false));
            var bounds = member ? RoomAuthoringPreview.IssueBounds(a, issue) : RoomAuthoringPreview.IssueBounds(layout, issue);
            Assert.AreEqual(new Vector3(-7, -4, 0), bounds.center);
            Assert.That(RoomAuthoringPreview.IssueLabel(issue), Does.StartWith("[inactive]"));
            RoomAuthoringPreview.GetFeedback(layout); CollectionAssert.AreEqual(before, Snapshot());
            Assert.AreEqual(group, Undo.GetCurrentGroup()); Assert.AreEqual(calls, generations); Assert.IsFalse(scene.isDirty);
        }

        [Test] public void DeletedOwnerLeavesOriginalConnectionInactiveAfterRemainingOccupancyIsErased()
        {
            var record = JsonUtility.ToJson(connection);
            var data = new SerializedObject(layout); data.FindProperty("spaces").arraySize = 1; data.ApplyModifiedPropertiesWithoutUndo();
            Object.DestroyImmediate(b.gameObject); var previousOutput = RoomAuthoringValidator.OutputFingerprint(a);
            Assert.IsFalse(RoomAuthoringOperations.Rebuild(layout)); Assert.AreEqual(RoomGenerationStatus.Refused, layout.GenerationResult.Status);
            Assert.AreEqual(previousOutput, RoomAuthoringValidator.OutputFingerprint(a)); Assert.AreEqual(record, JsonUtility.ToJson(layout.Connections.Single()));
            RoomAuthoringOperations.PaintBox(a, new BoundsInt(0, 0, 0, 8, 4, 1), null);
            Assert.AreEqual(RoomGenerationStatus.IncompletePreview, layout.GenerationResult.Status); Assert.IsTrue(layout.GenerationResult.Issues.Single().Inactive);
            Assert.AreEqual(record, JsonUtility.ToJson(layout.Connections.Single())); Assert.AreEqual(0, a.GeneratedOutput.transform.childCount);
            Assert.IsTrue(RoomAuthoringValidator.IsCurrent(layout));
        }

        [Test] public void MalformedExternalConnectionRemainsInactiveInSafeEmptyLayoutWithoutBeingNormalized()
        {
            var malformed = Definition(start: new Vector2(2.5f, 4)); layout.SetConnections(new[] { malformed });
            var record = JsonUtility.ToJson(malformed); EraseAllAsOneAction();
            Assert.AreEqual(RoomGenerationStatus.IncompletePreview, layout.GenerationResult.Status);
            var issue = layout.GenerationResult.Issues.Single(); Assert.IsTrue(issue.Inactive); Assert.IsFalse(issue.HasRegion);
            Assert.AreEqual("Connection/" + malformed.Id, RoomAuthoringPreview.FieldLabel(issue));
            Assert.AreEqual(record, JsonUtility.ToJson(layout.Connections.Single())); Assert.IsTrue(RoomAuthoringValidator.IsCurrent(layout));
        }

        [Test] public void InactiveEmptyRecordsPersistAndReturnToOriginalGapWhenOccupancyIsRestored()
        {
            EraseAllAsOneAction(); var id = layout.LayoutId; var record = JsonUtility.ToJson(layout.Connections.Single());
            var path = folder + "/Recovery.unity"; Assert.IsTrue(EditorSceneManager.SaveScene(layout.gameObject.scene, path));
            var reopened = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            layout = reopened.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ShipLayoutAuthoring>(true)).Single(l => l.LayoutId == id);
            a = layout.Spaces[0]; b = layout.Spaces[1];
            Assert.AreEqual(record, JsonUtility.ToJson(layout.Connections.Single())); Assert.AreEqual(RoomGenerationStatus.IncompletePreview, layout.GenerationResult.Status);
            Assert.IsTrue(RoomAuthoringValidator.IsCurrent(layout));
            foreach (var room in layout.Spaces)
                for (var y = 0; y < 4; y++) for (var x = 0; x < 8; x++) room.Occupancy.SetTile(new Vector3Int(x, y, 0), room.Style.FloorTile);
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(layout)); Assert.AreEqual(record, JsonUtility.ToJson(layout.Connections.Single()));
            Assert.AreEqual(2, LayoutBoundaryResolver.Resolve(RoomAuthoringValidator.TopologyOf(layout), layout.Connections).Edges.Count(e => e.Kind == LayoutEdgeKind.Opening));
        }
    }
}
