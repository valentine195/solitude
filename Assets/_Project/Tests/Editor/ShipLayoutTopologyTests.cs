using System;
using System.Linq;
using NUnit.Framework;
using SOLITUDE.Editor.RoomAuthoring;
using SOLITUDE.World.RoomAuthoring;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SOLITUDE.Tests.RoomAuthoring
{
    public sealed class ShipLayoutTopologyTests
    {
        private static void Fill(ShipRoomAuthoring room, TileBase marker, params Vector2Int[] cells)
        {
            foreach (var cell in cells) room.Occupancy.SetTile(new Vector3Int(cell.x, cell.y, 0), marker);
        }

        private static ShipSpaceCells Space(string id, Vector2Int offset, params Vector2Int[] cells) => new ShipSpaceCells(id, cells, offset);

        [Test] public void TopologyPreservesSameOwnerDifferentOwnerAndExteriorNeighbors()
        {
            var topology = ShipLayoutTopology.Build(new[]
            {
                Space("corridor-a", new Vector2Int(-4, -2), Vector2Int.zero, Vector2Int.right),
                Space("corridor-b", new Vector2Int(-2, -2), Vector2Int.zero)
            });
            CollectionAssert.AreEqual(new[] { "corridor-a" }, topology.OwnersAt(new Vector2Int(-4, -2)));
            CollectionAssert.AreEqual(topology.OwnersAt(new Vector2Int(-4, -2)), topology.OwnersAt(new Vector2Int(-3, -2)));
            CollectionAssert.AreEqual(new[] { "corridor-b" }, topology.OwnersAt(new Vector2Int(-2, -2)));
            Assert.IsEmpty(topology.OwnersAt(new Vector2Int(-3, -1)));
            Assert.AreEqual(2, topology.SpaceIds.Count);
            Assert.IsEmpty(topology.Overlaps);
        }

        [Test] public void OverlapListsEveryOwnerWithoutChoosingAWinnerAndIsOrderIndependent()
        {
            var spaces = new[] { Space("c", Vector2Int.zero, Vector2Int.zero), Space("a", Vector2Int.zero, Vector2Int.zero),
                Space("b", Vector2Int.zero, Vector2Int.zero) };
            var first = ShipLayoutTopology.Build(spaces);
            var second = ShipLayoutTopology.Build(spaces.Reverse());
            CollectionAssert.AreEqual(new[] { Vector2Int.zero }, first.Overlaps);
            CollectionAssert.AreEqual(new[] { "a", "b", "c" }, first.OwnersAt(Vector2Int.zero));
            CollectionAssert.AreEqual(first.Cells, second.Cells);
            CollectionAssert.AreEqual(first.OwnersAt(Vector2Int.zero), second.OwnersAt(Vector2Int.zero));
            Assert.Throws<ArgumentException>(() => ShipLayoutTopology.Build(new[] { spaces[0], spaces[0] }));
        }

        [Test] public void DiagonalContactAndBridgeRemovalRetainSeparateComponents()
        {
            var diagonal = ShipLayoutTopology.Build(new[] { Space("a", Vector2Int.zero, Vector2Int.zero, Vector2Int.one) });
            Assert.AreEqual(2, diagonal.ComponentsOf("a").Count);
            var joined = ShipLayoutTopology.Build(new[] { Space("a", Vector2Int.zero, Vector2Int.zero, Vector2Int.right, Vector2Int.right * 2) });
            Assert.AreEqual(1, joined.ComponentsOf("a").Count);
            var erased = ShipLayoutTopology.Build(new[] { Space("a", Vector2Int.zero, Vector2Int.zero, Vector2Int.right * 2) });
            Assert.AreEqual(2, erased.ComponentsOf("a").Count);
            Assert.Throws<OverflowException>(() => ShipLayoutTopology.Build(new[] { Space("a", Vector2Int.right, new Vector2Int(int.MaxValue, 0)) }));
        }

        [Test] public void OffsetSpacesProduceExactIndependentFloorsAndOneLayoutGeneration()
        {
            using var fixture = new RoomAuthoringTestFixtures(32);
            var layout = RoomAuthoringCreation.CreateLayout(fixture.Scene, fixture.Style, LogicalSpaceType.Corridor);
            layout.transform.position = new Vector3(-20, -10, 0);
            var a = layout.Spaces[0];
            var b = RoomAuthoringCreation.CreateSpace(layout, LogicalSpaceType.Corridor);
            b.transform.localPosition = Vector3.right * 2;
            Fill(a, fixture.Style.FloorTile, Vector2Int.zero, Vector2Int.right);
            Fill(b, fixture.Style.FloorTile, Vector2Int.zero, Vector2Int.up);
            Assert.IsNull(RoomAuthoringOperations.GetSafetyIssue(layout));
            var topology = RoomAuthoringValidator.TopologyOf(layout);
            Assert.AreEqual(a.RoomId, topology.OwnersAt(Vector2Int.right)[0]);
            Assert.AreEqual(b.RoomId, topology.OwnersAt(Vector2Int.right * 2)[0]);
            var generated = 0;
            void Count(ShipRoomAuthoring room) { generated++; }
            RoomAuthoringOperations.GenerationCompleted += Count;
            try { Assert.IsFalse(RoomAuthoringOperations.Rebuild(layout)); }
            finally { RoomAuthoringOperations.GenerationCompleted -= Count; }
            Assert.AreEqual(1, generated);
            Assert.AreEqual(RoomGenerationStatus.IncompletePreview, layout.GenerationResult.Status);
            Assert.AreEqual(2, RoomAuthoringTestFixtures.FloorCount(a));
            Assert.AreEqual(2, RoomAuthoringTestFixtures.FloorCount(b));
            Assert.AreEqual(1, layout.GetComponentsInChildren<SpriteRenderer>().Count(r => r.name == "PartitionEast"));
            Assert.IsTrue(RoomAuthoringValidator.IsCurrent(layout));
            var before = RoomAuthoringValidator.OutputFingerprint(layout);
            Assert.IsFalse(RoomAuthoringOperations.Rebuild(b));
            Assert.AreEqual(before, RoomAuthoringValidator.OutputFingerprint(layout));
            Assert.AreEqual(LogicalSpaceType.Corridor, a.SpaceType);
            Assert.AreEqual(LogicalSpaceType.Corridor, b.SpaceType);
        }

        [Test] public void BridgeErasePreservesIdentityCellsAndUsefulFloorPreviewWithLocatedIssues()
        {
            using var fixture = new RoomAuthoringTestFixtures(32);
            var layout = RoomAuthoringCreation.CreateLayout(fixture.Scene, fixture.Style);
            var a = layout.Spaces[0];
            var b = RoomAuthoringCreation.CreateSpace(layout); b.transform.localPosition = Vector3.right * 10;
            Fill(a, fixture.Style.FloorTile, Vector2Int.zero, Vector2Int.right, Vector2Int.right * 2);
            Fill(b, fixture.Style.FloorTile, Vector2Int.zero);
            RoomAuthoringOperations.Rebuild(layout);
            var id = a.RoomId; var bCells = RoomAuthoringOperations.ReadCells(b);
            Assert.IsFalse(RoomAuthoringOperations.PaintCell(a, Vector3Int.right, null));
            Assert.AreEqual(id, a.RoomId);
            CollectionAssert.AreEquivalent(new[] { Vector2Int.zero, Vector2Int.right * 2 }, RoomAuthoringOperations.ReadCells(a));
            CollectionAssert.AreEqual(bCells, RoomAuthoringOperations.ReadCells(b));
            Assert.AreEqual(2, RoomAuthoringTestFixtures.FloorCount(a));
            var issues = layout.GenerationResult.Issues.Where(i => i.Field.StartsWith("Disconnected/", StringComparison.Ordinal)).ToArray();
            Assert.AreEqual(2, issues.Length);
            Assert.IsTrue(issues.All(i => i.RoomId == id && i.HasRegion));
            Assert.AreEqual(RoomGenerationStatus.IncompletePreview, layout.GenerationResult.Status);
            Undo.PerformUndo();
            Assert.AreEqual(3, RoomAuthoringTestFixtures.FloorCount(a));
            Assert.IsFalse(layout.GenerationResult.Issues.Any(i => i.Field.StartsWith("Disconnected/", StringComparison.Ordinal)));
        }

        [Test] public void SingleDisconnectedSpaceCannotBeCompleteAndKeepsAllFloors()
        {
            using var fixture = new RoomAuthoringTestFixtures(32);
            var layout = RoomAuthoringCreation.CreateLayout(fixture.Scene, fixture.Style);
            var a = layout.Spaces[0];
            Fill(a, fixture.Style.FloorTile, Vector2Int.zero, Vector2Int.one);
            Assert.IsFalse(RoomAuthoringOperations.Rebuild(layout));
            Assert.AreEqual(RoomGenerationStatus.IncompletePreview, layout.GenerationResult.Status);
            Assert.AreEqual(2, RoomAuthoringTestFixtures.FloorCount(a));
            Assert.AreEqual(2, layout.GenerationResult.Issues.Count(i => i.Field.StartsWith("Disconnected/", StringComparison.Ordinal)));
        }

        [Test] public void OverlapNamesAllOwnersAndRetainsEveryOutputAndAuthoredCell()
        {
            using var fixture = new RoomAuthoringTestFixtures(32);
            var layout = RoomAuthoringCreation.CreateLayout(fixture.Scene, fixture.Style);
            var b = RoomAuthoringCreation.CreateSpace(layout); b.transform.localPosition = Vector3.right * 4;
            var c = RoomAuthoringCreation.CreateSpace(layout); c.transform.localPosition = Vector3.right * 8;
            foreach (var room in layout.Spaces) Fill(room, fixture.Style.FloorTile, Vector2Int.zero);
            RoomAuthoringOperations.Rebuild(layout);
            var output = layout.GetComponentsInChildren<Transform>(true);
            var identities = layout.Spaces.Select(room => room.RoomId).ToArray();
            b.transform.localPosition = c.transform.localPosition = Vector3.zero;
            Assert.IsFalse(RoomAuthoringOperations.Rebuild(layout));
            Assert.AreEqual(RoomGenerationStatus.Refused, layout.GenerationResult.Status);
            var issue = layout.GenerationResult.Issues.Single();
            Assert.IsTrue(issue.HasRegion); Assert.AreEqual(Vector2Int.zero, issue.Start);
            foreach (var id in identities) StringAssert.Contains(id, issue.Reason);
            CollectionAssert.AreEqual(output, layout.GetComponentsInChildren<Transform>(true));
            foreach (var room in layout.Spaces) CollectionAssert.AreEqual(new[] { Vector2Int.zero }, RoomAuthoringOperations.ReadCells(room));
        }

        [TestCase("duplicate-member")]
        [TestCase("duplicate-id")]
        [TestCase("shared-map")]
        [TestCase("foreign-layout")]
        [TestCase("foreign-output")]
        [TestCase("unsafe-output")]
        public void MultiSpaceOwnershipRefusesBeforeDeletingOutput(string fault)
        {
            using var fixture = new RoomAuthoringTestFixtures(32);
            var layout = RoomAuthoringCreation.CreateLayout(fixture.Scene, fixture.Style);
            var a = layout.Spaces[0];
            var b = RoomAuthoringCreation.CreateSpace(layout); b.transform.localPosition = Vector3.right * 10;
            Fill(a, fixture.Style.FloorTile, Vector2Int.zero); Fill(b, fixture.Style.FloorTile, Vector2Int.zero);
            RoomAuthoringOperations.Rebuild(layout);
            var retained = layout.GetComponentsInChildren<Transform>(true);
            var data = new SerializedObject(b);
            if (fault == "duplicate-member")
            {
                data = new SerializedObject(layout); data.FindProperty("spaces").GetArrayElementAtIndex(1).objectReferenceValue = a;
            }
            else if (fault == "duplicate-id") data.FindProperty("roomId").stringValue = a.RoomId;
            else if (fault == "shared-map") data.FindProperty("occupancy").objectReferenceValue = a.Occupancy;
            else if (fault == "foreign-layout")
            {
                var other = RoomAuthoringCreation.CreateLayout(fixture.Scene, fixture.Style);
                data.FindProperty("layout").objectReferenceValue = other;
            }
            else if (fault == "foreign-output")
            {
                data = new SerializedObject(b.GeneratedOutput); data.FindProperty("layoutOwner").objectReferenceValue = null;
            }
            else data.FindProperty("generatedOutput").objectReferenceValue = a.GeneratedOutput;
            data.ApplyModifiedPropertiesWithoutUndo();
            Assert.IsFalse(RoomAuthoringOperations.Rebuild(layout));
            Assert.AreEqual(RoomGenerationStatus.Refused, layout.GenerationResult.Status);
            CollectionAssert.AreEqual(retained, layout.GetComponentsInChildren<Transform>(true));
            Assert.IsFalse(RoomAuthoringValidator.IsCurrent(layout));
        }

        [Test] public void MemberOrderAndValidationDoNotChangeCellsOutputOrUndo()
        {
            using var fixture = new RoomAuthoringTestFixtures(32);
            var layout = RoomAuthoringCreation.CreateLayout(fixture.Scene, fixture.Style);
            var a = layout.Spaces[0]; var b = RoomAuthoringCreation.CreateSpace(layout);
            b.transform.localPosition = Vector3.left * 4;
            Fill(a, fixture.Style.FloorTile, Vector2Int.zero); Fill(b, fixture.Style.FloorTile, Vector2Int.zero);
            RoomAuthoringOperations.Rebuild(layout);
            var before = RoomAuthoringValidator.OutputFingerprint(layout);
            var data = new SerializedObject(layout); var members = data.FindProperty("spaces");
            members.MoveArrayElement(0, 1); data.ApplyModifiedPropertiesWithoutUndo();
            var group = Undo.GetCurrentGroup(); var dirty = fixture.Scene.isDirty;
            var serialized = EditorJsonUtility.ToJson(layout);
            RoomAuthoringValidator.Assess(layout, out _);
            Assert.AreEqual(group, Undo.GetCurrentGroup()); Assert.AreEqual(dirty, fixture.Scene.isDirty);
            Assert.AreEqual(serialized, EditorJsonUtility.ToJson(layout));
            Assert.IsTrue(RoomAuthoringValidator.IsCurrent(layout));
            RoomAuthoringOperations.Rebuild(layout);
            Assert.AreEqual(before, RoomAuthoringValidator.OutputFingerprint(layout));
        }

        [Test] public void MissingMemberOutputsRestoreWithoutChangingAuthoredMaps()
        {
            using var fixture = new RoomAuthoringTestFixtures(32);
            var layout = RoomAuthoringCreation.CreateLayout(fixture.Scene, fixture.Style);
            var b = RoomAuthoringCreation.CreateSpace(layout); b.transform.localPosition = Vector3.right * 4;
            foreach (var room in layout.Spaces) Fill(room, fixture.Style.FloorTile, Vector2Int.zero);
            foreach (var room in layout.Spaces) UnityEngine.Object.DestroyImmediate(room.GeneratedOutput.gameObject);
            RoomAuthoringOperations.Rebuild(layout);
            foreach (var room in layout.Spaces)
            {
                Assert.AreEqual(1, RoomAuthoringTestFixtures.FloorCount(room));
                Assert.AreSame(layout, room.GeneratedOutput.LayoutOwner);
                CollectionAssert.AreEqual(new[] { Vector2Int.zero }, RoomAuthoringOperations.ReadCells(room));
            }
            Assert.IsTrue(RoomAuthoringValidator.IsCurrent(layout));
        }

        [Test] public void InvalidBackReferenceCannotDispatchToAnotherMultiSpaceLayout()
        {
            using var fixture = new RoomAuthoringTestFixtures(32);
            var first = RoomAuthoringCreation.CreateLayout(fixture.Scene, fixture.Style);
            var room = first.Spaces[0];
            Fill(room, fixture.Style.FloorTile, Vector2Int.zero);
            RoomAuthoringOperations.Rebuild(first);
            var other = RoomAuthoringCreation.CreateLayout(fixture.Scene, fixture.Style);
            other.transform.position = Vector3.right * 20;
            var otherSpace = RoomAuthoringCreation.CreateSpace(other); otherSpace.transform.localPosition = Vector3.right * 4;
            Fill(other.Spaces[0], fixture.Style.FloorTile, Vector2Int.zero);
            Fill(otherSpace, fixture.Style.FloorTile, Vector2Int.zero);
            RoomAuthoringOperations.Rebuild(other);
            var retainedFirst = room.GeneratedOutput.GetComponentsInChildren<Transform>(true);
            var retainedOther = other.GetComponentsInChildren<Transform>(true);
            var otherResult = EditorJsonUtility.ToJson(other);
            var data = new SerializedObject(room); data.FindProperty("layout").objectReferenceValue = other;
            data.ApplyModifiedPropertiesWithoutUndo();
            Assert.IsFalse(RoomAuthoringOperations.Rebuild(room));
            Assert.AreEqual(RoomGenerationStatus.Refused, room.GenerationResult.Status);
            CollectionAssert.AreEqual(retainedFirst, room.GeneratedOutput.GetComponentsInChildren<Transform>(true));
            CollectionAssert.AreEqual(retainedOther, other.GetComponentsInChildren<Transform>(true));
            Assert.AreEqual(otherResult, EditorJsonUtility.ToJson(other));
        }

        [Test] public void CreateSpaceUndoRedoPreservesExistingOutputAndMembership()
        {
            using var fixture = new RoomAuthoringTestFixtures(32);
            var layout = RoomAuthoringCreation.CreateLayout(fixture.Scene, fixture.Style);
            var first = layout.Spaces[0];
            Fill(first, fixture.Style.FloorTile, Vector2Int.zero);
            RoomAuthoringOperations.Rebuild(layout);
            var retained = first.GeneratedOutput.GetComponentsInChildren<Transform>(true);
            var created = RoomAuthoringCreation.CreateSpace(layout, LogicalSpaceType.Corridor);
            var id = created.RoomId;
            Assert.AreEqual(2, layout.Spaces.Count);
            CollectionAssert.AreEqual(retained, first.GeneratedOutput.GetComponentsInChildren<Transform>(true));
            Undo.PerformUndo();
            Assert.AreEqual(1, layout.Spaces.Count); Assert.AreSame(first, layout.Spaces[0]);
            Assert.IsTrue(RoomAuthoringValidator.IsCurrent(layout));
            Undo.PerformRedo();
            Assert.AreEqual(2, layout.Spaces.Count);
            var restored = layout.Spaces.Single(space => space != first);
            Assert.AreEqual(id, restored.RoomId); Assert.AreSame(layout, restored.Layout);
            Assert.AreEqual(LogicalSpaceType.Corridor, restored.SpaceType);
            Assert.IsEmpty(RoomAuthoringOperations.ReadCells(restored));
            Assert.IsNull(RoomAuthoringOperations.GetSafetyIssue(layout));
            CollectionAssert.AreEqual(retained, first.GeneratedOutput.GetComponentsInChildren<Transform>(true));
        }
    }
}
