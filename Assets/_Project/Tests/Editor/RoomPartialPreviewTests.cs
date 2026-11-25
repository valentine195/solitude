using System.Linq;
using NUnit.Framework;
using SOLITUDE.Editor.RoomAuthoring;
using SOLITUDE.World.RoomAuthoring;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SOLITUDE.Tests.RoomAuthoring
{
    public sealed class RoomPartialPreviewTests
    {
        [Test] public void MissingCornerRetainsFloorsAndIndependentWallsAndHasStableLocatedIssue()
        {
            using var f = new RoomAuthoringTestFixtures();
            var room = f.Room();
            var original = f.Style.Northwest.Sprite;
            f.Style.Northwest.Sprite = null;
            Assert.IsFalse(RoomAuthoringOperations.Rebuild(room));
            Assert.AreEqual(RoomGenerationStatus.IncompletePreview, room.GenerationResult.Status);
            Assert.AreEqual(48, RoomAuthoringTestFixtures.FloorCount(room));
            var renderers = room.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>();
            Assert.AreEqual(27, renderers.Length);
            Assert.AreEqual(4, renderers.Count(r => r.name == "North"));
            var issue = room.GenerationResult.Issues.Single();
            Assert.AreEqual(room.RoomId, issue.RoomId);
            Assert.AreEqual("Northwest", issue.Field);
            Assert.IsTrue(issue.HasRegion);
            Assert.AreEqual(new Vector2Int(0, 6), issue.Start);
            var before = RoomUndoRedoTests.Snapshot(room);
            Assert.IsFalse(RoomAuthoringOperations.Rebuild(room));
            CollectionAssert.AreEqual(before, RoomUndoRedoTests.Snapshot(room));
            Assert.IsTrue(RoomAuthoringValidator.IsCurrent(room));
            f.Style.Northwest.Sprite = original;
            Assert.IsFalse(RoomAuthoringValidator.IsCurrent(room));
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(room));
            Assert.IsEmpty(room.GenerationResult.Issues);
            Assert.AreEqual(28, room.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>().Length);
        }

        [Test] public void MissingFloorRetainsArchitectureAndMissingStyleRetainsAuthoredCells()
        {
            using var f = new RoomAuthoringTestFixtures();
            var room = f.Room();
            f.Style.FloorTile = null;
            Assert.IsFalse(RoomAuthoringOperations.Rebuild(room));
            Assert.AreEqual(0, RoomAuthoringTestFixtures.FloorCount(room));
            Assert.AreEqual(28, room.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>().Length);
            var floorIssue = room.GenerationResult.Issues.Single();
            Assert.AreEqual(RoomIssueKind.Floor, floorIssue.Kind);
            Assert.AreEqual("floorTile", floorIssue.Field);
            Assert.IsFalse(floorIssue.HasRegion);
            room.Style = null;
            Assert.IsFalse(RoomAuthoringOperations.Rebuild(room));
            Assert.AreEqual(48, RoomAuthoringOperations.ReadCells(room).Length);
            Assert.AreEqual(0, room.GeneratedOutput.transform.childCount);
            Assert.AreEqual(RoomIssueKind.Style, room.GenerationResult.Issues.Single().Kind);
            Assert.AreEqual(room.RoomId, room.GenerationResult.Issues.Single().RoomId);
            Assert.IsFalse(room.GenerationResult.Issues.Single().HasRegion);
        }

        [Test] public void UncoverableRunHasNoPrefixAndDoesNotSuppressOtherSides()
        {
            using var f = new RoomAuthoringTestFixtures();
            var room = f.Room(7, 6);
            Assert.IsFalse(RoomAuthoringOperations.Rebuild(room));
            Assert.AreEqual(42, RoomAuthoringTestFixtures.FloorCount(room));
            var sprites = room.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>();
            Assert.IsFalse(sprites.Any(r => r.name == "North"));
            Assert.AreEqual(6, sprites.Count(r => r.name == "East"));
            var issue = room.GenerationResult.Issues.Single();
            Assert.AreEqual(RoomIssueKind.Span, issue.Kind);
            Assert.AreEqual(new Vector2Int(0, 6), issue.Start);
            Assert.AreEqual(new Vector2Int(7, 6), issue.End);
        }

        [Test] public void CorrectionRemovesOnlyResolvedIssues()
        {
            using var f = new RoomAuthoringTestFixtures();
            var room = f.Room();
            var corner = f.Style.Northwest.Sprite;
            f.Style.Northwest.Sprite = null;
            f.Style.FloorTile = null;
            Assert.IsFalse(RoomAuthoringOperations.Rebuild(room));
            var floorId = room.GenerationResult.Issues.Single(i => i.Kind == RoomIssueKind.Floor).Id;
            f.Style.Northwest.Sprite = corner;
            Assert.IsFalse(RoomAuthoringOperations.Rebuild(room));
            Assert.AreEqual(floorId, room.GenerationResult.Issues.Single().Id);
            Assert.AreEqual(28, room.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>().Length);
        }

        [Test] public void MissingAssetAndCorrectionUndoRestoreResultIssuesAndOutputTogether()
        {
            using var f = new RoomAuthoringTestFixtures();
            var room = f.Room();
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(room));
            var complete = RoomUndoRedoTests.Snapshot(room);
            var original = f.Style.Northwest.Sprite;
            using (RoomAuthoringOperations.BeginEdit(room, "Remove Corner"))
            {
                Undo.RegisterCompleteObjectUndo(f.Style, "Remove Corner");
                f.Style.Northwest.Sprite = null;
                Assert.IsFalse(RoomAuthoringOperations.Rebuild(room));
            }
            var partial = RoomUndoRedoTests.Snapshot(room);
            Undo.PerformUndo();
            CollectionAssert.AreEqual(complete, RoomUndoRedoTests.Snapshot(room));
            Assert.IsTrue(RoomAuthoringValidator.IsCurrent(room));
            Undo.PerformRedo();
            CollectionAssert.AreEqual(partial, RoomUndoRedoTests.Snapshot(room));
            using (RoomAuthoringOperations.BeginEdit(room, "Restore Corner"))
            {
                Undo.RegisterCompleteObjectUndo(f.Style, "Restore Corner");
                f.Style.Northwest.Sprite = original;
                Assert.IsTrue(RoomAuthoringOperations.Rebuild(room));
            }
            CollectionAssert.AreEqual(complete, RoomUndoRedoTests.Snapshot(room));
            Undo.PerformUndo();
            CollectionAssert.AreEqual(partial, RoomUndoRedoTests.Snapshot(room));
            Undo.PerformRedo();
            CollectionAssert.AreEqual(complete, RoomUndoRedoTests.Snapshot(room));
        }

        [Test] public void LegacyOrExternallyModifiedOutputIsNeverLabeledCurrentByReadOnlyAssessment()
        {
            using var f = new RoomAuthoringTestFixtures();
            var room = f.Room();
            Assert.IsTrue(room.GenerationResult == null || string.IsNullOrEmpty(room.GenerationResult.InputFingerprint),
                "A legacy inline result has no freshness data, even if Unity materializes its default object.");
            var before = EditorJsonUtility.ToJson(room);
            Assert.AreEqual(RoomGenerationStatus.Complete, RoomAuthoringValidator.Assess(room, out _).Status);
            Assert.IsFalse(RoomAuthoringValidator.IsCurrent(room));
            Assert.AreEqual(before, EditorJsonUtility.ToJson(room));
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(room));
            Assert.IsTrue(RoomAuthoringValidator.IsCurrent(room));
            var floor = room.GeneratedOutput.GetComponentInChildren<Tilemap>();
            floor.SetTile(Vector3Int.zero, null);
            before = EditorJsonUtility.ToJson(room);
            Assert.IsFalse(RoomAuthoringValidator.IsCurrent(room));
            Assert.IsNull(RoomAuthoringValidator.Validate(room));
            Assert.AreEqual(before, EditorJsonUtility.ToJson(room));
            Assert.AreEqual(47, RoomAuthoringTestFixtures.FloorCount(room));
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(room));
            Assert.IsTrue(RoomAuthoringValidator.IsCurrent(room));
            Assert.AreEqual(48, RoomAuthoringTestFixtures.FloorCount(room));
        }
    }
}
