using System.Linq;
using NUnit.Framework;
using SOLITUDE.Editor.RoomAuthoring;
using SOLITUDE.World.RoomAuthoring;
using UnityEditor;
using UnityEngine;

namespace SOLITUDE.Tests.RoomAuthoring
{
    public sealed class ShipLayoutCreationTests
    {
        private static ShipLayoutAuthoring Create(RoomAuthoringTestFixtures fixture)
        {
            var layout = RoomAuthoringCreation.CreateLayout(fixture.Scene, fixture.Style);
            var room = layout.Spaces[0];
            Assert.IsTrue(RoomAuthoringOperations.PaintBox(room, new BoundsInt(0, 0, 0, 8, 6, 1), fixture.Style.FloorTile));
            return layout;
        }

        [Test] public void CreateLayoutKeepsAuthoritativeSpaceAndExclusiveOutputAssociations()
        {
            using var fixture = new RoomAuthoringTestFixtures();
            var layout = RoomAuthoringCreation.CreateLayout(fixture.Scene, fixture.Style, LogicalSpaceType.Corridor);
            var space = layout.Spaces.Single();
            Assert.IsNotEmpty(layout.LayoutId);
            Assert.IsNotEmpty(space.RoomId);
            Assert.AreNotEqual(layout.LayoutId, space.RoomId);
            Assert.AreSame(layout, space.Layout);
            Assert.AreEqual(LogicalSpaceType.Corridor, space.SpaceType);
            Assert.AreSame(fixture.Style, space.Style);
            Assert.AreSame(space.GeneratedOutput, layout.GeneratedOutput);
            Assert.AreSame(layout, layout.GeneratedOutput.LayoutOwner);
            Assert.AreSame(space, layout.GeneratedOutput.Owner);
            Assert.IsFalse(space.Occupancy.gameObject.activeSelf);
            Assert.AreEqual("EditorOnly", space.Occupancy.tag);
            Assert.IsNull(RoomAuthoringOperations.GetSafetyIssue(layout));
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(layout));
            Assert.AreEqual(RoomGenerationStatus.Empty, layout.GenerationResult.Status);
        }

        [Test] public void RebuildLayoutAndSpaceUseTheSameRectangleOutputAndReadOnlyValidation()
        {
            using var fixture = new RoomAuthoringTestFixtures();
            var layout = Create(fixture);
            var room = layout.Spaces[0];
            Assert.AreEqual(48, RoomAuthoringTestFixtures.FloorCount(room));
            Assert.AreEqual(4, room.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>().Count(r =>
                new[] { "Northwest", "Northeast", "Southwest", "Southeast" }.Contains(r.name)));
            var identity = room.RoomId;
            var cells = RoomAuthoringOperations.ReadCells(room);
            var fingerprint = RoomAuthoringValidator.OutputFingerprint(room);
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(layout));
            Assert.AreEqual(fingerprint, RoomAuthoringValidator.OutputFingerprint(room));
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(room));
            Assert.AreEqual(fingerprint, RoomAuthoringValidator.OutputFingerprint(room));
            Assert.AreEqual(identity, room.RoomId);
            CollectionAssert.AreEqual(cells, RoomAuthoringOperations.ReadCells(room));
            Assert.IsTrue(RoomAuthoringValidator.IsCurrent(layout));
            var children = layout.GeneratedOutput.GetComponentsInChildren<Transform>(true);
            var before = EditorJsonUtility.ToJson(layout);
            var group = Undo.GetCurrentGroup();
            var dirty = fixture.Scene.isDirty;
            Assert.AreEqual(RoomGenerationStatus.Complete, RoomAuthoringValidator.Assess(layout, out _).Status);
            CollectionAssert.AreEqual(children, layout.GeneratedOutput.GetComponentsInChildren<Transform>(true));
            Assert.AreEqual(before, EditorJsonUtility.ToJson(layout));
            Assert.AreEqual(group, Undo.GetCurrentGroup());
            Assert.AreEqual(dirty, fixture.Scene.isDirty);
        }

        [Test] public void DefaultStyleAndSpaceMetadataDoNotChangeIdentityOrOccupancy()
        {
            using var fixture = new RoomAuthoringTestFixtures();
            var layout = Create(fixture);
            var space = layout.Spaces[0];
            var cells = RoomAuthoringOperations.ReadCells(space);
            var identity = space.RoomId;
            space.SpaceType = LogicalSpaceType.Corridor;
            space.AuthoringMetadata = "Medbay main route";
            layout.DefaultStyle = null;
            Assert.IsNull(space.Style);
            Assert.IsFalse(RoomAuthoringValidator.IsCurrent(layout));
            Assert.IsFalse(RoomAuthoringOperations.Rebuild(layout));
            Assert.AreEqual(RoomGenerationStatus.IncompletePreview, layout.GenerationResult.Status);
            layout.DefaultStyle = fixture.Style;
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(layout));
            Assert.AreSame(fixture.Style, space.Style);
            Assert.AreEqual(identity, space.RoomId);
            CollectionAssert.AreEqual(cells, RoomAuthoringOperations.ReadCells(space));
        }

        [TestCase("duplicate-member")]
        [TestCase("duplicate-layout-id")]
        [TestCase("misaligned")]
        [TestCase("duplicate-output")]
        [TestCase("foreign-membership")]
        public void InvalidLayoutRefusesBeforeDestructiveWrites(string fault)
        {
            using var fixture = new RoomAuthoringTestFixtures();
            var layout = Create(fixture);
            var room = layout.Spaces[0];
            var cells = RoomAuthoringOperations.ReadCells(room);
            var retained = room.GeneratedOutput.GetComponentsInChildren<Transform>(true);
            if (fault == "duplicate-member")
            {
                var data = new SerializedObject(layout);
                var members = data.FindProperty("spaces");
                members.arraySize = 2;
                members.GetArrayElementAtIndex(1).objectReferenceValue = room;
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            else if (fault == "duplicate-layout-id")
            {
                var other = RoomAuthoringCreation.CreateLayout(fixture.Scene, fixture.Style);
                other.transform.position = Vector3.right * 20;
                var data = new SerializedObject(other);
                data.FindProperty("layoutId").stringValue = layout.LayoutId;
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            else if (fault == "misaligned") layout.transform.position = new Vector3(0.5f, 0, 0);
            else if (fault == "duplicate-output") Object.Instantiate(room.GeneratedOutput.gameObject, room.transform);
            else
            {
                var other = RoomAuthoringCreation.CreateLayout(fixture.Scene, fixture.Style);
                other.transform.position = Vector3.right * 20;
                var data = new SerializedObject(other);
                data.FindProperty("spaces").GetArrayElementAtIndex(0).objectReferenceValue = room;
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            Assert.IsFalse(RoomAuthoringOperations.Rebuild(layout));
            Assert.AreEqual(RoomGenerationStatus.Refused, layout.GenerationResult.Status);
            Assert.IsFalse(RoomAuthoringValidator.IsCurrent(layout));
            CollectionAssert.AreEqual(retained, room.GeneratedOutput.GetComponentsInChildren<Transform>(true));
            CollectionAssert.AreEqual(cells, RoomAuthoringOperations.ReadCells(room));
        }

        [Test] public void SharedAuthoritativeMapRefusesAndPreservesUnrelatedOutput()
        {
            using var fixture = new RoomAuthoringTestFixtures();
            var layout = Create(fixture);
            var other = fixture.Room(); other.transform.position = Vector3.right * 20;
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(other));
            var retained = layout.GeneratedOutput.GetComponentsInChildren<Transform>(true);
            var otherOutput = other.GeneratedOutput.GetComponentsInChildren<Transform>(true);
            var data = new SerializedObject(other);
            data.FindProperty("occupancy").objectReferenceValue = layout.Spaces[0].Occupancy;
            data.ApplyModifiedPropertiesWithoutUndo();
            Assert.IsFalse(RoomAuthoringOperations.Rebuild(layout));
            CollectionAssert.AreEqual(retained, layout.GeneratedOutput.GetComponentsInChildren<Transform>(true));
            CollectionAssert.AreEqual(otherOutput, other.GeneratedOutput.GetComponentsInChildren<Transform>(true));
        }

        [Test] public void InvalidBackReferenceDoesNotDispatchGenerationToAnotherLayout()
        {
            using var fixture = new RoomAuthoringTestFixtures();
            var first = Create(fixture);
            var other = RoomAuthoringCreation.CreateLayout(fixture.Scene, fixture.Style);
            other.transform.position = Vector3.right * 20;
            Assert.IsTrue(RoomAuthoringOperations.PaintBox(other.Spaces[0], new BoundsInt(0, 0, 0, 8, 6, 1), fixture.Style.FloorTile));
            var retained = other.GeneratedOutput.GetComponentsInChildren<Transform>(true);
            var data = new SerializedObject(first.Spaces[0]);
            data.FindProperty("layout").objectReferenceValue = other;
            data.ApplyModifiedPropertiesWithoutUndo();
            Assert.IsFalse(RoomAuthoringOperations.Rebuild(first.Spaces[0]));
            CollectionAssert.AreEqual(retained, other.GeneratedOutput.GetComponentsInChildren<Transform>(true));
        }

        [Test] public void DeletedLayoutOutputCanBeReconstructedWithoutChangingAuthoredCells()
        {
            using var fixture = new RoomAuthoringTestFixtures();
            var layout = Create(fixture);
            var room = layout.Spaces[0];
            var cells = RoomAuthoringOperations.ReadCells(room);
            Object.DestroyImmediate(layout.GeneratedOutput.gameObject);
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(layout));
            Assert.AreSame(layout.GeneratedOutput, room.GeneratedOutput);
            Assert.AreSame(layout, room.GeneratedOutput.LayoutOwner);
            Assert.AreEqual(48, RoomAuthoringTestFixtures.FloorCount(room));
            CollectionAssert.AreEqual(cells, RoomAuthoringOperations.ReadCells(room));
        }

        [Test] public void LegacyUnadoptedRoomStillUsesTheSameGenerationPipeline()
        {
            using var fixture = new RoomAuthoringTestFixtures();
            var room = fixture.Room();
            Assert.IsNull(room.Layout);
            Assert.IsNull(room.GeneratedOutput.LayoutOwner);
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(room));
            Assert.AreEqual(48, RoomAuthoringTestFixtures.FloorCount(room));
            Assert.IsTrue(RoomAuthoringValidator.IsCurrent(room));
        }
    }
}
