using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SOLITUDE.Editor.RoomAuthoring;
using SOLITUDE.World.RoomAuthoring;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SOLITUDE.Tests.RoomAuthoring
{
    public sealed class ShipLayoutMedbayTests
    {
        private string folder, baselinePath;
        private ShipLayoutAuthoring layout;
        [SetUp] public void SetUp()
        {
            folder = ShipLayoutSmokeSetup.FolderPrefix + Guid.NewGuid().ToString("N");
            baselinePath = "Assets/_Project/Tests/MedbayBaseline" + Guid.NewGuid().ToString("N") + ".unity";
            var baseline = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Assert.IsTrue(EditorSceneManager.SaveScene(baseline, baselinePath));
            var path = ShipLayoutSmokeSetup.CreateMedbay(folder); var scene = SceneManager.GetSceneByPath(path);
            layout = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ShipLayoutAuthoring>()).Single(l => l.name == "Structural Medbay Layout");
        }
        [TearDown] public void TearDown()
        {
            RoomAuthoringOperations.CancelActiveEdit(); Selection.activeObject = null;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AssetDatabase.DeleteAsset(folder); AssetDatabase.DeleteAsset(baselinePath);
        }
        [Test] public void ScaffoldIsEmptyAndRerunRefusesWithoutChangingScenesOrSentinels()
        {
            Assert.AreEqual(3, layout.Spaces.Count); Assert.AreEqual(LogicalSpaceType.Corridor, layout.Spaces[0].SpaceType);
            Assert.IsEmpty(layout.Connections); Assert.IsTrue(layout.Spaces.All(s => RoomAuthoringOperations.ReadCells(s).Length == 0));
            Assert.IsTrue(RoomAuthoringValidator.IsCurrent(layout)); var scene = layout.gameObject.scene;
            var bytes = File.ReadAllBytes(scene.path); var objects = scene.GetRootGameObjects(); var group = Undo.GetCurrentGroup();
            Assert.Throws<InvalidOperationException>(() => ShipLayoutSmokeSetup.CreateMedbay(folder));
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(scene.path)); CollectionAssert.AreEqual(objects, scene.GetRootGameObjects());
            Assert.AreEqual(group, Undo.GetCurrentGroup()); Assert.IsFalse(scene.isDirty);
        }
        [Test] public void SyntheticBrushAndConnectionWorkflowHasExactGeometryUndoAndReopen()
        {
            // Automated control-path exercise. Native authoring evidence is recorded separately.
            var scene = layout.gameObject.scene;
            var sentinels = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ShipRoomAuthoring>()).Where(r => r.Layout != layout).ToArray();
            var beforeSentinels = sentinels.Select(RoomUndoRedoTests.Snapshot).ToArray();
            foreach (var space in layout.Spaces) space.AuthoringMetadata = space.name + " automated fixture";
            var corridor = layout.Spaces[0]; var a = layout.Spaces[1]; var b = layout.Spaces[2];
            var brush = ScriptableObject.CreateInstance<ShipRoomBrush>(); brush.OccupancyMarker = corridor.Style.FloorTile; brush.CorridorWidth = 3;
            try
            {
                brush.BeginGesture(corridor, GridBrushBase.Tool.Paint);
                foreach (var p in new[] { new Vector3Int(1, 1, 0), new Vector3Int(17, 1, 0), new Vector3Int(17, 10, 0) })
                    brush.Paint(corridor.GetComponent<Grid>(), corridor.Occupancy.gameObject, p);
                brush.EndGesture(); brush.BeginGesture(corridor, GridBrushBase.Tool.Paint);
                foreach (var p in new[] { new Vector3Int(9, 1, 0), new Vector3Int(9, 7, 0) }) brush.Paint(corridor.GetComponent<Grid>(), corridor.Occupancy.gameObject, p);
                brush.EndGesture(); Assert.AreEqual(102, RoomAuthoringOperations.ReadCells(corridor).Length);
                Assert.IsTrue(RoomAuthoringOperations.PaintBox(a, new BoundsInt(0, 3, 0, 8, 6, 1), a.Style.FloorTile));
                Assert.IsTrue(RoomAuthoringOperations.PaintBox(b, new BoundsInt(11, 3, 0, 5, 6, 1), b.Style.FloorTile));
                Assert.IsTrue(RoomAuthoringOperations.PaintBox(b, new BoundsInt(14, 7, 0, 2, 2, 1), null));
                var baseline = LayoutBoundaryResolver.Resolve(RoomAuthoringValidator.TopologyOf(layout));
                var partitions = baseline.Edges.Count(e => e.Kind == LayoutEdgeKind.Partition);
                foreach (var item in new[] { (a, 2, 1, LayoutConnectionType.Doorway), (b, 12, 1, LayoutConnectionType.Doorway), (a, 5, 2, LayoutConnectionType.OpenPassage) })
                {
                    var definition = new LayoutConnectionDefinition(Guid.NewGuid().ToString("N"), corridor.RoomId, item.Item1.RoomId, new Vector2(item.Item2, 3), true, item.Item3, item.Item4);
                    Assert.IsTrue(RoomAuthoringOperations.AddConnection(layout, definition, out var issue), issue);
                }
                var topology = RoomAuthoringValidator.TopologyOf(layout); var geometry = LayoutBoundaryResolver.Resolve(topology, layout.Connections);
                Assert.AreEqual(176, topology.Cells.Count); Assert.AreEqual(partitions - 4, geometry.Edges.Count(e => e.Kind == LayoutEdgeKind.Partition));
                Assert.AreEqual(4, geometry.Edges.Count(e => e.Kind == LayoutEdgeKind.Opening));
                Assert.AreEqual(geometry.Edges.Count(e => e.Kind == LayoutEdgeKind.Exterior || e.Kind == LayoutEdgeKind.Partition), geometry.Spans.Sum(s => s.Length));
                Assert.AreEqual(1, topology.ComponentsOf(corridor.RoomId).Count);
                Assert.IsTrue(geometry.Exterior.Junctions.Any(j => j.Kind == RoomJoinKind.Concave));
                Assert.AreEqual(176, layout.Spaces.Sum(RoomAuthoringTestFixtures.FloorCount));
                Assert.AreEqual(2, layout.GenerationResult.Issues.Length, layout.GenerationResult.Summary);
                Assert.IsTrue(layout.GenerationResult.Issues.All(i => i.Field.EndsWith("/Frame")));
                var snapshot = ShipLayoutDiagnosticTests.Snapshot(layout); var definitions = layout.Connections.Select(JsonUtility.ToJson).ToArray();
                RoomAuthoringOperations.Rebuild(layout); RoomAuthoringOperations.Rebuild(layout); CollectionAssert.AreEqual(snapshot, ShipLayoutDiagnosticTests.Snapshot(layout));
                RoomAuthoringOperations.PaintBox(b, new BoundsInt(12, 3, 0, 1, 1, 1), null);
                Assert.AreEqual(RoomGenerationStatus.Refused, layout.GenerationResult.Status); Assert.IsFalse(RoomAuthoringValidator.IsCurrent(layout));
                Undo.PerformUndo(); CollectionAssert.AreEqual(snapshot, ShipLayoutDiagnosticTests.Snapshot(layout));
                for (var i = 0; i < sentinels.Length; i++) CollectionAssert.AreEqual(beforeSentinels[i], RoomUndoRedoTests.Snapshot(sentinels[i]));
                Assert.IsTrue(EditorSceneManager.SaveScene(scene)); var id = layout.LayoutId;
                scene = EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Single);
                layout = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ShipLayoutAuthoring>()).Single(l => l.LayoutId == id);
                CollectionAssert.AreEqual(snapshot, ShipLayoutDiagnosticTests.Snapshot(layout)); CollectionAssert.AreEqual(definitions, layout.Connections.Select(JsonUtility.ToJson));
                Assert.IsTrue(RoomAuthoringValidator.IsCurrent(layout));
            }
            finally { Object.DestroyImmediate(brush); }
        }
        [Test] public void StyleAssignmentPreservesPaintedMedbayAndWritesConfiguredSortingThroughUndo()
        {
            SyntheticBrushAndConnectionWorkflowHasExactGeometryUndoAndReopen();
            var original = layout.DefaultStyle;
            var alternate = Object.Instantiate(original); alternate.DisplayName = "Compatible Medbay alternate";
            var identity = new SerializedObject(alternate); identity.FindProperty("styleId").stringValue = Guid.NewGuid().ToString("N"); identity.ApplyModifiedPropertiesWithoutUndo();
            alternate.Rendering = new RoomStyleDefinition.RenderingConfiguration { SortingLayerName = "Wall_Panels", FloorOrder = -4, DetailOrder = -2, ArchitectureOrder = 3 };
            alternate.CompatibleTransitions = new[] { original }; original.CompatibleTransitions = new[] { alternate };
            AssetDatabase.CreateAsset(alternate, folder + "/AlternateStyle.asset");
            EditorUtility.SetDirty(original); AssetDatabase.SaveAssets(); RoomAuthoringOperations.Rebuild(layout);
            var before = ShipLayoutDiagnosticTests.Snapshot(layout); var cells = RoomAuthoringValidator.TopologyOf(layout).Cells.ToArray();
            var connections = layout.Connections.Select(JsonUtility.ToJson).ToArray();
            Assert.IsTrue(RoomAuthoringStyleControls.AssignLayoutStyle(layout, alternate, out var issue), issue);
            foreach (var room in layout.Spaces)
            {
                var rendering = room.Style.Rendering;
                Assert.AreEqual(rendering?.FloorOrder ?? 0, room.GeneratedOutput.GetComponentInChildren<UnityEngine.Tilemaps.TilemapRenderer>().sortingOrder);
                Assert.IsTrue(room.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>().All(r => r.sortingOrder == (rendering?.ArchitectureOrder ?? 1)));
            }
            CollectionAssert.AreEqual(cells, RoomAuthoringValidator.TopologyOf(layout).Cells);
            CollectionAssert.AreEqual(connections, layout.Connections.Select(JsonUtility.ToJson));
            Assert.AreEqual(176, layout.Spaces.Sum(RoomAuthoringTestFixtures.FloorCount));
            Undo.PerformUndo(); Assert.AreSame(original, layout.DefaultStyle); CollectionAssert.AreEqual(before, ShipLayoutDiagnosticTests.Snapshot(layout));
            Undo.PerformRedo(); Assert.AreSame(alternate, layout.DefaultStyle); Assert.IsTrue(RoomAuthoringValidator.IsCurrent(layout));
        }

    }
}
