using System;
using System.Linq;
using NUnit.Framework;
using SOLITUDE.Editor.RoomAuthoring;
using SOLITUDE.World.RoomAuthoring;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SOLITUDE.Tests.RoomAuthoring
{
    public sealed class ShipLayoutConnectionTests
    {
        private string folder;
        private ShipLayoutAuthoring layout;
        private ShipRoomAuthoring negative;
        private ShipRoomAuthoring positive;
        private int generations;

        [SetUp] public void SetUp()
        {
            folder = ShipLayoutDiagnosticTests.CreateSharedAssets();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            layout = RoomAuthoringCreation.CreateLayout(scene, AssetDatabase.LoadAssetAtPath<RoomStyleDefinition>(folder + "/Style.asset"));
            negative = layout.Spaces[0]; positive = RoomAuthoringCreation.CreateSpace(layout);
            negative.AuthoringMetadata = "Negative fixture"; positive.AuthoringMetadata = "Positive fixture";
            generations = 0;
        }

        private void Populate(bool horizontal)
        {
            positive.transform.localPosition = horizontal ? Vector3.up * 4 : Vector3.right * 4;
            foreach (var room in layout.Spaces)
                for (var y = 0; y < (horizontal ? 4 : 8); y++) for (var x = 0; x < (horizontal ? 8 : 4); x++)
                    room.Occupancy.SetTile(new Vector3Int(x, y, 0), room.Style.FloorTile);
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(layout), layout.GenerationResult.Summary);
            RoomAuthoringOperations.GenerationCompleted += Generated;
        }
        private void Generated(ShipRoomAuthoring _) => generations++;
        [TearDown] public void TearDown()
        {
            RoomAuthoringOperations.GenerationCompleted -= Generated;
            RoomAuthoringOperations.CancelActiveEdit(); Selection.activeObject = null;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AssetDatabase.DeleteAsset(folder);
        }
        private LayoutConnectionDefinition Connection(bool horizontal = true, int width = 2, LayoutConnectionType type = LayoutConnectionType.OpenPassage,
            string id = null, Vector2? start = null, string a = null, string b = null) =>
            new LayoutConnectionDefinition(id ?? Guid.NewGuid().ToString("N"), a ?? negative.RoomId, b ?? positive.RoomId,
                start ?? (horizontal ? new Vector2(2, 4) : new Vector2(4, 2)), horizontal, width, type);
        private string[] Snapshot() => ShipLayoutDiagnosticTests.Snapshot(layout)
            .Concat(layout.Connections.Select(JsonUtility.ToJson)).ToArray();

        [TestCase(true, 1, LayoutConnectionType.OpenPassage)][TestCase(true, 2, LayoutConnectionType.OpenPassage)]
        [TestCase(false, 1, LayoutConnectionType.OpenPassage)][TestCase(false, 2, LayoutConnectionType.OpenPassage)]
        [TestCase(true, 1, LayoutConnectionType.Doorway)][TestCase(true, 2, LayoutConnectionType.Doorway)]
        [TestCase(false, 1, LayoutConnectionType.Doorway)][TestCase(false, 2, LayoutConnectionType.Doorway)]
        [TestCase(true, 1, LayoutConnectionType.AirlockOpening)][TestCase(true, 2, LayoutConnectionType.AirlockOpening)]
        [TestCase(false, 1, LayoutConnectionType.AirlockOpening)][TestCase(false, 2, LayoutConnectionType.AirlockOpening)]
        public void AllTypesCutExactUnitSpansAndUndoRestoreWallsAndRecords(bool horizontal, int width, LayoutConnectionType type)
        {
            Populate(horizontal); var ids = layout.Spaces.Select(r => r.RoomId).ToArray();
            var cells = layout.Spaces.Select(r => RoomAuthoringOperations.ReadCells(r)).ToArray();
            var before = Snapshot(); var connection = Connection(horizontal, width, type);
            Assert.IsTrue(RoomAuthoringOperations.AddConnection(layout, connection, out var issue), issue);
            Assert.AreEqual(1, generations); Assert.AreEqual(connection.Id, layout.Connections.Single().Id);
            var topology = RoomAuthoringValidator.TopologyOf(layout);
            var geometry = LayoutBoundaryResolver.Resolve(topology, layout.Connections);
            CollectionAssert.AreEquivalent(connection.Edges(), geometry.Edges.Where(e => e.Kind == LayoutEdgeKind.Opening).Select(e => e.Address));
            Assert.AreEqual(8 - width, geometry.Edges.Count(e => e.Kind == LayoutEdgeKind.Partition));
            Assert.AreEqual(8 - width, geometry.Spans.Where(s => s.First.Kind == LayoutEdgeKind.Partition).Sum(s => s.Length));
            var placement = RoomPlacementBuilder.BuildLayout(geometry, layout.Spaces.ToDictionary(r => r.RoomId, r => r.Style));
            Assert.IsEmpty(placement.Issues, string.Join("\n", placement.Issues.Select(i => i.Reason)));
            Assert.AreEqual(8 - width, placement.Sprites.Count(p => p.Role.StartsWith("Partition")));
            Assert.AreEqual(RoomGenerationStatus.Complete, layout.GenerationResult.Status, layout.GenerationResult.Summary);
            Assert.IsTrue(RoomAuthoringValidator.IsCurrent(layout));
            CollectionAssert.AreEqual(ids, layout.Spaces.Select(r => r.RoomId));
            for (var i = 0; i < cells.Length; i++) CollectionAssert.AreEqual(cells[i], RoomAuthoringOperations.ReadCells(layout.Spaces[i]));
            var added = Snapshot(); Undo.PerformUndo(); CollectionAssert.AreEqual(before, Snapshot());
            Undo.PerformRedo(); CollectionAssert.AreEqual(added, Snapshot());
            Assert.IsTrue(RoomAuthoringOperations.RemoveConnection(layout, connection.Id, out issue), issue);
            Assert.AreEqual(2, generations); Assert.IsEmpty(layout.Connections);
            Assert.AreEqual(8, LayoutBoundaryResolver.Resolve(RoomAuthoringValidator.TopologyOf(layout)).Edges.Count(e => e.Kind == LayoutEdgeKind.Partition));
            Undo.PerformUndo(); CollectionAssert.AreEqual(added, Snapshot());
            Undo.PerformRedo(); Assert.IsEmpty(layout.Connections); Assert.IsTrue(RoomAuthoringValidator.IsCurrent(layout));
            var rebuilt = Snapshot(); RoomAuthoringOperations.Rebuild(layout); CollectionAssert.AreEqual(rebuilt, Snapshot());
        }

        [TestCase("wrong")][TestCase("same")][TestCase("exterior")][TestCase("off-grid")][TestCase("zero")]
        [TestCase("negative")][TestCase("swapped")][TestCase("direction")][TestCase("endpoint")]
        [TestCase("beyond")][TestCase("overflow")][TestCase("nan")][TestCase("unknown-type")]
        public void InvalidPlacementDoesNotMutateAuthoredDataOutputOrUndo(string invalid)
        {
            Populate(true);
            var candidate = invalid switch
            {
                "wrong" => Connection(a: "missing-owner"),
                "same" => Connection(b: negative.RoomId),
                "exterior" => Connection(start: new Vector2(2, 0)),
                "off-grid" => Connection(start: new Vector2(2.5f, 4)),
                "zero" => Connection(width: 0), "negative" => Connection(width: -1),
                "swapped" => Connection(a: positive.RoomId, b: negative.RoomId),
                "direction" => Connection(false, start: new Vector2(2, 4)),
                "endpoint" => Connection(start: new Vector2(0, 4)),
                "beyond" => Connection(width: 8), "overflow" => Connection(width: int.MaxValue),
                "nan" => Connection(start: new Vector2(float.NaN, 4)),
                _ => Connection(type: (LayoutConnectionType)99)
            };
            var before = Snapshot(); var objects = layout.GetComponentsInChildren<Transform>(true);
            var group = Undo.GetCurrentGroup(); var dirty = layout.gameObject.scene.isDirty;
            Assert.IsFalse(RoomAuthoringOperations.AddConnection(layout, candidate, out var issue)); Assert.IsNotEmpty(issue);
            CollectionAssert.AreEqual(before, Snapshot()); CollectionAssert.AreEqual(objects, layout.GetComponentsInChildren<Transform>(true));
            Assert.AreEqual(group, Undo.GetCurrentGroup()); Assert.AreEqual(dirty, layout.gameObject.scene.isDirty); Assert.AreEqual(0, generations);
        }

        [TestCase(false)][TestCase(true)]
        public void DuplicateAndOverlappingSpansRejectWithoutChoosingAWinner(bool duplicate)
        {
            Populate(true); var first = Connection(); Assert.IsTrue(RoomAuthoringOperations.AddConnection(layout, first, out _));
            var before = Snapshot(); var group = Undo.GetCurrentGroup();
            var other = duplicate ? Connection(id: first.Id) : Connection(start: new Vector2(3, 4));
            Assert.IsFalse(RoomAuthoringOperations.AddConnection(layout, other, out var issue)); Assert.That(issue, Does.Contain("overlap").IgnoreCase.Or.Contain("Duplicate"));
            CollectionAssert.AreEqual(before, Snapshot()); Assert.AreEqual(group, Undo.GetCurrentGroup()); Assert.AreEqual(1, generations);
            layout.SetConnections(new[] { first, other });
            var issues = LayoutBoundaryResolver.ConnectionIssues(RoomAuthoringValidator.TopologyOf(layout), layout.Connections);
            Assert.GreaterOrEqual(issues.Length, 2); Assert.AreEqual(2, layout.Connections.Count);
            Assert.AreEqual(RoomGenerationStatus.Refused, RoomAuthoringValidator.Assess(layout, out _).Status);
        }

        [TestCase(true)][TestCase(false)]
        public void NeighboringOverhangingArtworkCannotEnterAperture(bool horizontal)
        {
            Populate(horizontal); var connection = Connection(horizontal);
            Assert.IsTrue(RoomAuthoringOperations.AddConnection(layout, connection, out _));
            var geometry = LayoutBoundaryResolver.Resolve(RoomAuthoringValidator.TopologyOf(layout), layout.Connections);
            var style = negative.Style;
            var module = horizontal ? style.SouthCap : style.EastCap;
            module.OffsetPixels += horizontal ? Vector2.right * 16 : Vector2.up * 16;
            var placement = RoomPlacementBuilder.BuildLayout(geometry, layout.Spaces.ToDictionary(r => r.RoomId, r => r.Style));
            Assert.That(placement.Issues.Any(i => i.Reason.Contains("intrudes into connection " + connection.Id)));
            Assert.AreEqual(connection.Width, geometry.Edges.Count(e => e.Kind == LayoutEdgeKind.Opening));
            Assert.IsTrue(RoomAuthoringOperations.RemoveConnection(layout, connection.Id, out _));
        }

        [Test] public void WideNorthCanvasWithoutDeclaredInkCannotOverhangIntoAnInteriorOpening()
        {
            Populate(true);
            // A north exterior run meets a raised room: a wide panel can overhang sideways into its doorway.
            for (var y = 0; y < 4; y++) for (var x = 0; x < 4; x++) positive.Occupancy.SetTile(new Vector3Int(x, y, 0), null);
            var connection = Connection(width: 1, start: new Vector2(5, 4));
            Assert.IsTrue(RoomAuthoringOperations.AddConnection(layout, connection, out _));
            using var art = new RoomAuthoringTestFixtures(64);
            var wide = art.Module(64, 64, 64); wide.OffsetPixels = Vector2.right * 128;
            negative.Style.NorthWalls = new[] { wide };
            var geometry = LayoutBoundaryResolver.Resolve(RoomAuthoringValidator.TopologyOf(layout), layout.Connections);
            var placement = RoomPlacementBuilder.BuildLayout(geometry, layout.Spaces.ToDictionary(r => r.RoomId, r => r.Style));
            Assert.IsTrue(placement.Issues.Any(i => i.Reason.Contains("intrudes into connection " + connection.Id)));
            Assert.IsFalse(placement.Sprites.Any(p => p.Sprite == wide.Sprite && p.Position.y == 4 &&
                p.Position.x < 6 && p.Position.x + 2 > 5), "Only panels intersecting the opening must be suppressed; clear neighboring panels remain.");
            Assert.AreEqual(1, geometry.Edges.Count(e => e.Kind == LayoutEdgeKind.Opening));
        }

        [Test] public void OwnerPairChangesAndCrossJunctionEndpointsAreRejected()
        {
            Populate(true); var third = RoomAuthoringCreation.CreateSpace(layout);
            third.transform.localPosition = new Vector3(4, 4, 0);
            for (var y = 0; y < 4; y++) for (var x = 4; x < 8; x++)
            { positive.Occupancy.SetTile(new Vector3Int(x, y, 0), null); third.Occupancy.SetTile(new Vector3Int(x - 4, y, 0), third.Style.FloorTile); }
            RoomAuthoringOperations.Rebuild(layout); var before = Snapshot();
            Assert.IsFalse(RoomAuthoringOperations.AddConnection(layout, Connection(width: 4), out _));
            Assert.IsFalse(RoomAuthoringOperations.AddConnection(layout, Connection(width: 2), out var endpoint));
            Assert.That(endpoint, Does.Contain("endpoint")); CollectionAssert.AreEqual(before, Snapshot());
        }

        [Test] public void InspectorAndBoundaryPreviewAreReadOnlyAndExternalTypeChangesAreStale()
        {
            Populate(true); var connection = Connection(); Assert.IsTrue(RoomAuthoringOperations.AddConnection(layout, connection, out _));
            var before = Snapshot(); var group = Undo.GetCurrentGroup(); var editor = UnityEditor.Editor.CreateEditor(layout);
            try { Assert.IsNotNull(editor.CreateInspectorGUI()); } finally { Object.DestroyImmediate(editor); }
            CollectionAssert.AreEqual(before, Snapshot()); Assert.AreEqual(group, Undo.GetCurrentGroup());
            layout.SetConnections(new[] { Connection(type: LayoutConnectionType.Doorway, id: connection.Id) });
            Assert.IsFalse(RoomAuthoringValidator.IsCurrent(layout), "Connection identity, type and address participate in currentness.");
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(layout)); Assert.IsTrue(RoomAuthoringValidator.IsCurrent(layout));
        }
    }
}
