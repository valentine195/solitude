using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SOLITUDE.Editor.RoomAuthoring;
using SOLITUDE.World.RoomAuthoring;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace SOLITUDE.Tests.RoomAuthoring
{
    public sealed class RoomFloorPanelEditingTests
    {
        private string folder;
        private ShipRoomAuthoring room;
        private ShipLayoutAuthoring layout;
        [SetUp] public void Setup()
        {
            Assert.IsTrue(Environment.GetCommandLineArgs().Contains("-solitudeRoomAuthoringVerification"));
            Undo.ClearAll(); var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            folder = RoomPersistenceTests.CreateAssets(); RoomFloorPanelAssetStore.VerificationRoot = folder + "/Panels";
            room = RoomPersistenceTests.CreateRoom(scene, folder); room.Style.Rendering = new RoomStyleDefinition.RenderingConfiguration();
            EditorUtility.SetDirty(room.Style); AssetDatabase.SaveAssets(); Assert.IsTrue(RoomAuthoringOperations.Rebuild(room));
            layout = RoomAuthoringCreation.AdoptIntoNewLayout(room);
            Assert.IsTrue(RoomAuthoringOperations.SetFloorPanelSettings(room, new RoomFloorPanelSettings { Enabled = true, Seed = 17 }, out var issue), issue);
        }
        [TearDown] public void Cleanup()
        {
            RoomAuthoringOperations.CancelActiveEdit(); RoomAuthoringPreview.ClearSelection(); Selection.activeObject = null;
            Undo.ClearAll(); EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (folder != null) AssetDatabase.DeleteAsset(folder); RoomFloorPanelAssetStore.VerificationRoot = null;
        }
        private static string Partition(ShipRoomAuthoring space, RoomFloorPanelSettings settings = null) =>
            string.Join("|", RoomAuthoringPreview.BuildFloorPanelPreview(space, settings ?? space.FloorPanels, space.FloorReservations).Partition.Rectangles
                .Select(r => r.x + "," + r.y + "," + r.width + "," + r.height));
        private RoomFloorPanelOutput Revision(ShipRoomAuthoring space = null) => AssetDatabase.LoadAssetAtPath<RoomFloorPanelOutput>(AssetDatabase.GetAssetPath(
            (space ?? room).GeneratedOutput.GetComponentsInChildren<SpriteRenderer>().Single(r => r.name == "Generated Floor Panels").sprite));
        private string[] AssetBytes() => Directory.GetFiles(folder, "*", SearchOption.AllDirectories).OrderBy(p => p)
            .Select(p => p + ":" + Convert.ToBase64String(System.Security.Cryptography.SHA256.Create().ComputeHash(File.ReadAllBytes(p)))).ToArray();

        [Test] public void PreviewIsReadOnlyAndReportsDraftPartitionsAndExclusions()
        {
            Assert.IsTrue(EditorSceneManager.SaveScene(room.gameObject.scene, folder + "/Preview.unity"));
            var authored = EditorJsonUtility.ToJson(room); var owner = EditorJsonUtility.ToJson(layout); var style = EditorJsonUtility.ToJson(room.Style);
            var assets = AssetBytes(); var output = RoomUndoRedoTests.Snapshot(room); var group = Undo.GetCurrentGroup(); var count = RoomFloorPanelAssetStore.RetainedRevisionCount;
            var settings = room.FloorPanels.Copy(); settings.Seed = 31;
            var records = new[] { new RoomFloorReservation("draft-bed", room.RoomId, Vector2.one, Vector2.one * 2) };
            Assert.IsTrue(RoomAuthoringPreview.ShowFloorPanelPreview(room, settings, records));
            var preview = RoomAuthoringPreview.GetFloorPanelPreview(room); Assert.IsNotNull(preview);
            Assert.AreEqual(4, preview.Domain.ExcludedCells.Count); Assert.IsNotEmpty(preview.Partition.Rectangles);
            settings.Seed = 1; Assert.AreEqual(31, preview.Partition.Seed);
            Assert.AreEqual(authored, EditorJsonUtility.ToJson(room)); Assert.AreEqual(owner, EditorJsonUtility.ToJson(layout));
            Assert.AreEqual(style, EditorJsonUtility.ToJson(room.Style)); CollectionAssert.AreEqual(assets, AssetBytes());
            CollectionAssert.AreEqual(output, RoomUndoRedoTests.Snapshot(room)); Assert.AreEqual(group, Undo.GetCurrentGroup());
            Assert.AreEqual(count, RoomFloorPanelAssetStore.RetainedRevisionCount); Assert.IsFalse(room.gameObject.scene.isDirty);
            RoomAuthoringEditLifecycle.PrepareForRuntime(room.gameObject.scene); // Read-only preview is explicitly cancelled by save notification below.
            EditorSceneManager.SaveScene(room.gameObject.scene, folder + "/Preview.unity");
            Assert.IsNull(RoomAuthoringPreview.GetFloorPanelPreview(room));
        }
        [Test] public void PreviewCancelsOnCommitUndoAndStaleConnectivityWithoutPublishing()
        {
            Assert.IsTrue(RoomAuthoringPreview.ShowFloorPanelPreview(room, room.FloorPanels, room.FloorReservations));
            Assert.IsNotNull(RoomAuthoringPreview.GetFloorPanelPreview(room));
            Assert.IsTrue(RoomAuthoringOperations.NewFloorPanelSeed(room, out var issue), issue);
            Assert.IsNull(RoomAuthoringPreview.GetFloorPanelPreview(room));
            Assert.IsTrue(RoomAuthoringPreview.ShowFloorPanelPreview(room, room.FloorPanels, room.FloorReservations));
            Undo.PerformUndo(); Assert.IsNull(RoomAuthoringPreview.GetFloorPanelPreview(room));
            Assert.IsTrue(RoomAuthoringPreview.ShowFloorPanelPreview(room, room.FloorPanels, room.FloorReservations));
            var count = RoomFloorPanelAssetStore.RetainedRevisionCount;
            layout.SetConnections(new[] { new LayoutConnectionDefinition("stale", room.RoomId, "missing", Vector2Int.zero, true, 1, LayoutConnectionType.Doorway) });
            Assert.IsNull(RoomAuthoringPreview.GetFloorPanelPreview(room));
            var preview = RoomAuthoringPreview.BuildFloorPanelPreview(room, room.FloorPanels, room.FloorReservations);
            Assert.IsNotEmpty(preview.Partition.Issues); Assert.AreEqual(count, RoomFloorPanelAssetStore.RetainedRevisionCount);
        }
        [Test] public void RegenerateRetainsSeedNewSeedUndoRestoresExactPixelsAndDisableRetainsInputs()
        {
            var old = Revision(); var hash = RoomFloorPanelAssetStore.ContentHash(old); var partition = Partition(room);
            Assert.IsTrue(RoomAuthoringOperations.RegenerateFloorPanels(room, out var issue), issue);
            Assert.AreEqual(17, room.FloorPanels.Seed); Assert.AreSame(old, Revision());
            Assert.IsTrue(RoomAuthoringOperations.NewFloorPanelSeed(room, out issue), issue); Assert.AreEqual(18, room.FloorPanels.Seed);
            Undo.PerformUndo(); Assert.AreEqual(17, room.FloorPanels.Seed); Assert.AreSame(old, Revision());
            Assert.AreEqual(hash, RoomFloorPanelAssetStore.ContentHash(Revision())); Assert.AreEqual(partition, Partition(room));
            Assert.IsTrue(RoomAuthoringOperations.DisableFloorPanels(room, out issue), issue); Assert.IsFalse(room.FloorPanels.Enabled);
            Assert.AreEqual(17, room.FloorPanels.Seed); Assert.AreEqual(48, RoomAuthoringTestFixtures.FloorCount(room));
            Assert.IsFalse(room.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>().Any(r => r.name == "Generated Floor Panels"));
            Undo.PerformUndo(); Assert.AreSame(old, Revision());
        }
        [Test] public void OneSettingsCommitGeneratesOnceAndSeamChangesPreservePartition()
        {
            var before = RoomUndoRedoTests.Snapshot(room); var partition = Partition(room); var calls = 0;
            void Generated(ShipRoomAuthoring _) => calls++;
            RoomAuthoringOperations.GenerationCompleted += Generated;
            try
            {
                var settings = room.FloorPanels.Copy(); settings.SeamThickness = 3; settings.SeamPalette = new[] { Color.cyan, Color.gray };
                Assert.IsTrue(RoomAuthoringOperations.SetFloorPanelSettings(room, settings, out var issue), issue);
                Assert.AreEqual(1, calls); Assert.AreEqual(partition, Partition(room));
                Undo.PerformUndo(); CollectionAssert.AreEqual(before, RoomUndoRedoTests.Snapshot(room));
            }
            finally { RoomAuthoringOperations.GenerationCompleted -= Generated; }
        }
        [Test] public void InvalidReservationFocusIsReadOnlyAndCorrectionRestoresOnlyPanelOutput()
        {
            room.SetFloorPanelAuthoring(room.FloorPanels, new[] { new RoomFloorReservation("orphan", room.RoomId, new Vector2(100, 100), Vector2.one) });
            Assert.IsFalse(RoomAuthoringOperations.Rebuild(room)); Assert.AreEqual(48, RoomAuthoringTestFixtures.FloorCount(room));
            Selection.activeGameObject = room.gameObject;
            var issue = layout.GenerationResult.Issues.Single(i => i.Field == "FloorPanels/Reservation/orphan");
            var before = EditorJsonUtility.ToJson(room); var group = Undo.GetCurrentGroup(); var dirty = room.gameObject.scene.isDirty;
            Assert.IsTrue(RoomAuthoringPreview.SelectIssue(room, issue.Id, frame: false));
            Assert.AreEqual("FloorPanels/Reservation/orphan", RoomAuthoringPreview.FieldLabel(issue));
            Assert.AreEqual(before, EditorJsonUtility.ToJson(room)); Assert.AreEqual(group, Undo.GetCurrentGroup()); Assert.AreEqual(dirty, room.gameObject.scene.isDirty);
            Assert.IsTrue(RoomAuthoringOperations.RemoveFloorReservation(room, "orphan", out var correction), correction);
            Assert.IsTrue(room.GenerationResult.IsSuccessful, room.GenerationDiagnostic); Assert.IsNotNull(Revision());
            Assert.AreEqual(48, RoomAuthoringTestFixtures.FloorCount(room)); Assert.IsNull(RoomAuthoringPreview.SelectedIssueId);
        }
        [Test] public void DisablePreservesExternallyInvalidRecordsAndSupportsUndo()
        {
            var orphan = new RoomFloorReservation("bad", "external-owner", Vector2.one * .5f, Vector2.one);
            room.SetFloorPanelAuthoring(room.FloorPanels, new[] { orphan }); RoomAuthoringOperations.Rebuild(room);
            var record = JsonUtility.ToJson(room.FloorReservations[0]);
            Assert.IsTrue(RoomAuthoringOperations.DisableFloorPanels(room, out var issue), issue);
            Assert.IsFalse(room.FloorPanels.Enabled); Assert.AreEqual(record, JsonUtility.ToJson(room.FloorReservations[0]));
            Assert.AreEqual(48, RoomAuthoringTestFixtures.FloorCount(room));
            Undo.PerformUndo(); Assert.IsTrue(room.FloorPanels.Enabled); Assert.AreEqual(record, JsonUtility.ToJson(room.FloorReservations[0]));
        }
        [Test] public void OneSpaceSeedEditPreservesOtherSpacePartitionsAndResourceReferences()
        {
            var other = RoomAuthoringCreation.CreateSpace(layout); other.transform.localPosition = Vector3.right * 16;
            ShipLayoutStyleTests.Fill(other, RoomAuthoringTestFixtures.Cells());
            other.SetFloorPanelAuthoring(new RoomFloorPanelSettings { Enabled = true, Seed = 37 }, Array.Empty<RoomFloorReservation>());
            RoomAuthoringOperations.Rebuild(layout); var old = Revision(other); var partition = Partition(other);
            Assert.IsTrue(RoomAuthoringOperations.NewFloorPanelSeed(room, out var issue), issue);
            Assert.AreSame(old, Revision(other)); Assert.AreEqual(partition, Partition(other));
        }
        [Test] public void InspectorDraftFieldsDoNotWriteAuthoringOrPublishUntilApply()
        {
            var before = RoomUndoRedoTests.Snapshot(room); var count = RoomFloorPanelAssetStore.RetainedRevisionCount; var group = Undo.GetCurrentGroup();
            var controls = RoomFloorPanelControls.ForSpace(room);
            controls.Q<Toggle>("panelEnabled").value = false; controls.Q<IntegerField>("panelSeed").value = 991;
            CollectionAssert.AreEqual(before, RoomUndoRedoTests.Snapshot(room)); Assert.AreEqual(count, RoomFloorPanelAssetStore.RetainedRevisionCount);
            Assert.AreEqual(group, Undo.GetCurrentGroup()); Assert.IsNotNull(controls.Q<Button>("panelPreview")); Assert.IsNotNull(controls.Q<Button>("panelApply"));
            var inspector = UnityEditor.Editor.CreateEditor(layout);
            try { Assert.IsNotNull(inspector.CreateInspectorGUI().Q<VisualElement>("floorPanelControls")); }
            finally { Object.DestroyImmediate(inspector); }
        }
        [UnityTest] public IEnumerator ReferencedAssetBurstWaitsForStrokeAndReusesIdenticalPanelResources()
        {
            var calls = 0; void Generated(ShipRoomAuthoring _) => calls++;
            RoomAuthoringAssetRefresh.Observe(layout); var old = Revision(); var partition = Partition(room);
            RoomAuthoringOperations.GenerationCompleted += Generated;
            try
            {
                using (var edit = RoomAuthoringOperations.BeginEdit(room, "Pending panel stroke"))
                {
                    RoomAuthoringOperations.PaintCell(room, new Vector3Int(8, 0, 0), room.Style.FloorTile, edit);
                    var texture = new Texture2D(2, 2); var path = folder + "/square.png";
                    texture.LoadImage(File.ReadAllBytes(path)); texture.SetPixel(0, 0, Color.green); File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                    var wait = EditorApplication.timeSinceStartup + .8; while (EditorApplication.timeSinceStartup < wait) yield return null;
                    Assert.AreEqual(0, calls); Assert.AreSame(old, Revision()); edit.Cancel();
                }
                var deadline = EditorApplication.timeSinceStartup + 10; while (calls == 0 && EditorApplication.timeSinceStartup < deadline) yield return null;
                Assert.AreEqual(1, calls, RoomAuthoringAssetRefresh.GetIssue(layout)); Assert.AreSame(old, Revision()); Assert.AreEqual(partition, Partition(room));
                var settle = EditorApplication.timeSinceStartup + .8; while (EditorApplication.timeSinceStartup < settle) yield return null;
                Assert.AreEqual(1, calls, "Generated imports must not recursively rebuild.");
            }
            finally { RoomAuthoringOperations.GenerationCompleted -= Generated; }
        }
    }
}
