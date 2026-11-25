using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using SOLITUDE.Editor.RoomAuthoring;
using SOLITUDE.World.RoomAuthoring;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace SOLITUDE.Tests.RoomAuthoring
{
    public sealed class RoomFloorPanelAuthoringTests
    {
        private string panelFolder;
        [SetUp] public void BeginPanelResourceScope()
        {
            Assert.IsTrue(Environment.GetCommandLineArgs().Contains("-solitudeRoomAuthoringVerification"));
            panelFolder = "Assets/_Project/Tests/PanelAuthoringFixture" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets/_Project/Tests", Path.GetFileName(panelFolder));
            RoomFloorPanelAssetStore.VerificationRoot = panelFolder + "/Panels";
        }
        [TearDown] public void EndPanelResourceScope()
        {
            RoomAuthoringOperations.CancelActiveEdit(); Undo.ClearAll();
            if (panelFolder != null) AssetDatabase.DeleteAsset(panelFolder);
            RoomFloorPanelAssetStore.VerificationRoot = null;
        }
        private static string[] StructuralSprites(ShipRoomAuthoring room) => room.GeneratedOutput
            .GetComponentsInChildren<SpriteRenderer>().Where(r => r.name != "Generated Floor Panels")
            .Select(r => r.name + ":" + r.sprite.GetEntityId() + ":" + JsonUtility.ToJson(r.transform.localPosition) +
                ":" + JsonUtility.ToJson(r.transform.localRotation) + ":" + JsonUtility.ToJson(r.transform.localScale) +
                ":" + r.sortingLayerID + ":" + r.sortingOrder + ":" + r.sharedMaterial.GetEntityId()).OrderBy(s => s).ToArray();

        private static ShipRoomAuthoring Room(RoomAuthoringTestFixtures fixture)
        {
            ShipLayoutStyleTests.Configure(fixture);
            fixture.Style.Rendering = new RoomStyleDefinition.RenderingConfiguration();
            var room = fixture.Room(); Assert.IsTrue(RoomAuthoringOperations.Rebuild(room)); return room;
        }

        private static RoomFloorReservation Reserve(ShipRoomAuthoring room, string id, int x, int y, int w = 1, int h = 1) =>
            new RoomFloorReservation(id, room.RoomId, new Vector2(x, y), new Vector2(w, h));

        private static string Inputs(ShipRoomAuthoring room) => JsonUtility.ToJson(room.FloorPanels) + "|" +
            string.Join("|", room.FloorReservations.Select(r => JsonUtility.ToJson(r)));

        [Test] public void SettingsAreCopiedAndUndoRestoresExactAuthoredAndStructuralState()
        {
            using var f = new RoomAuthoringTestFixtures(32); var room = Room(f);
            var before = RoomUndoRedoTests.Snapshot(room); var structure = StructuralSprites(room);
            var floor = room.GeneratedOutput.GetComponentInChildren<Tilemap>();
            var floorBounds = floor.cellBounds; var floorTiles = floor.GetTilesBlock(floorBounds);
            var settings = new RoomFloorPanelSettings { Enabled = true, Seed = -8123, Density = .75f, SeamThickness = 3 };
            Assert.IsTrue(RoomAuthoringOperations.SetFloorPanelSettings(room, settings, out var issue), issue);
            var authored = Inputs(room); var after = RoomUndoRedoTests.Snapshot(room);
            CollectionAssert.AreEqual(structure, StructuralSprites(room)); Assert.AreEqual(48, RoomAuthoringTestFixtures.FloorCount(room));
            var afterFloor = room.GeneratedOutput.GetComponentInChildren<Tilemap>();
            Assert.AreEqual(floorBounds, afterFloor.cellBounds); CollectionAssert.AreEqual(floorTiles, afterFloor.GetTilesBlock(floorBounds));
            Assert.IsTrue(room.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>().Any(r => r.name == "Generated Floor Panels"));
            Assert.IsTrue(room.GenerationResult.IsSuccessful, room.GenerationDiagnostic);
            settings.Seed = 8; settings.SeamPalette[0] = Color.magenta;
            Assert.AreEqual(authored, Inputs(room), "Caller-owned inputs cannot mutate persisted authoring after commit.");
            Undo.PerformUndo(); Assert.IsFalse(room.FloorPanels.Enabled); Assert.AreEqual(0, room.FloorPanels.Seed);
            CollectionAssert.AreEqual(before, RoomUndoRedoTests.Snapshot(room));
            Undo.PerformRedo(); Assert.AreEqual(authored, Inputs(room)); CollectionAssert.AreEqual(after, RoomUndoRedoTests.Snapshot(room));
            Assert.IsTrue(RoomAuthoringValidator.IsCurrent(room));
        }

        [Test] public void ReservationsRetainIdentityThroughAddUpdateRemoveUndoAndUnionExclusions()
        {
            using var f = new RoomAuthoringTestFixtures(32); var room = Room(f); var cells = RoomAuthoringOperations.ReadCells(room);
            var output = RoomAuthoringValidator.OutputFingerprint(room);
            Assert.IsTrue(RoomAuthoringOperations.UpsertFloorReservation(room, Reserve(room, "bed", 0, 0, 2, 2), out var issue), issue);
            var first = Inputs(room);
            Undo.PerformUndo(); Assert.IsEmpty(room.FloorReservations);
            Undo.PerformRedo(); Assert.AreEqual(first, Inputs(room));
            Assert.IsTrue(RoomAuthoringOperations.UpsertFloorReservation(room, Reserve(room, "console", 1, 0, 2, 2), out issue), issue);
            var domain = RoomFloorPanelDomain.Build(room);
            CollectionAssert.AreEquivalent(new[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0),
                new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(2,1) }, domain.ExcludedCells);
            Assert.AreEqual(42, domain.EligibleCells.Count); Assert.AreEqual(42, domain.PixelRegions.Count); Assert.AreEqual(2, room.FloorReservations.Count);
            for (var y = 0; y < 192; y++) for (var x = 0; x < 256; x++)
                Assert.AreEqual(!(x < 96 && y < 64), domain.ContainsPixel(new Vector2Int(x, y)), "Pixel " + x + "," + y);
            var both = Inputs(room);
            Assert.IsTrue(RoomAuthoringOperations.UpsertFloorReservation(room, Reserve(room, "bed", 4, 3), out issue), issue);
            Assert.AreEqual(2, room.FloorReservations.Count); Assert.AreEqual(new Vector2(4,3), room.FloorReservations.Single(r => r.Id == "bed").Position);
            Undo.PerformUndo(); Assert.AreEqual(both, Inputs(room));
            Assert.IsTrue(RoomAuthoringOperations.RemoveFloorReservation(room, "console", out issue), issue); Assert.AreEqual(first, Inputs(room));
            Undo.PerformUndo(); Assert.AreEqual(both, Inputs(room)); Undo.PerformRedo(); Assert.AreEqual(first, Inputs(room));
            CollectionAssert.AreEqual(cells, RoomAuthoringOperations.ReadCells(room)); Assert.AreEqual(output, RoomAuthoringValidator.OutputFingerprint(room));
        }

        [TestCase(true, LayoutConnectionType.Doorway)] [TestCase(false, LayoutConnectionType.Doorway)]
        [TestCase(true, LayoutConnectionType.AirlockOpening)] [TestCase(false, LayoutConnectionType.AirlockOpening)]
        [TestCase(true, LayoutConnectionType.OpenPassage)] [TestCase(false, LayoutConnectionType.OpenPassage)]
        public void ThresholdExclusionsFollowExactSpanAndOwnerOnBothSides(bool horizontal, LayoutConnectionType type)
        {
            foreach (var offset in new[] { Vector2Int.zero, new Vector2Int(-9, -7) })
            {
                using var f = new RoomAuthoringTestFixtures(32);
                var layout = RoomAuthoringCreation.CreateLayout(f.Scene, f.Style); var negative = layout.Spaces[0];
                var positive = RoomAuthoringCreation.CreateSpace(layout);
                negative.transform.localPosition = new Vector3(offset.x, offset.y);
                positive.transform.localPosition = new Vector3(offset.x, offset.y) + (horizontal ? Vector3.up : Vector3.right) * 3;
                var footprint = RoomAuthoringTestFixtures.Cells(horizontal ? 6 : 3, horizontal ? 3 : 6);
                foreach (var room in layout.Spaces)
                {
                    ShipLayoutStyleTests.Fill(room, footprint);
                    room.SetFloorPanelAuthoring(new RoomFloorPanelSettings { Enabled = true }, Array.Empty<RoomFloorReservation>());
                }
                var start = offset + (horizontal ? new Vector2Int(2, 3) : new Vector2Int(3, 2));
                layout.SetConnections(new[] { new LayoutConnectionDefinition("door", negative.RoomId, positive.RoomId, start, horizontal, 2, type) });
                foreach (var room in layout.Spaces)
                {
                    var domain = RoomFloorPanelDomain.Build(room); Assert.IsEmpty(domain.Issues, string.Join("\n", domain.Issues.Select(i => i.Reason)));
                    var expected = type == LayoutConnectionType.OpenPassage ? Array.Empty<Vector2Int>() : new[] {
                        start - (room == negative ? horizontal ? Vector2Int.up : Vector2Int.right : Vector2Int.zero),
                        start + (horizontal ? Vector2Int.right : Vector2Int.up) - (room == negative ? horizontal ? Vector2Int.up : Vector2Int.right : Vector2Int.zero) };
                    CollectionAssert.AreEquivalent(expected, domain.ExcludedCells);
                    Assert.AreEqual(18 - expected.Length, domain.EligibleCells.Count); Assert.AreEqual(18, RoomAuthoringOperations.ReadCells(room).Length);
                    foreach (var cell in expected)
                    {
                        Assert.IsFalse(domain.ContainsPixel(cell * 32)); Assert.IsFalse(domain.ContainsPixel(cell * 32 + Vector2Int.one * 31));
                    }
                    foreach (var rect in domain.PixelRegions) Assert.IsTrue(domain.ContainsPixel(rect.position));
                    Assert.IsFalse(domain.ContainsPixel(new Vector2Int(-10000, -10000)));
                }
            }
        }

        [TestCase("density")] [TestCase("nan")] [TestCase("version")] [TestCase("thickness")]
        [TestCase("range")] [TestCase("quantum")] [TestCase("palette")] [TestCase("color")]
        public void InvalidSettingsAreRejectedBeforeUndoDirtinessOrOutputChanges(string invalid)
        {
            using var f = new RoomAuthoringTestFixtures(32); var room = Room(f); var settings = new RoomFloorPanelSettings();
            switch (invalid)
            {
                case "density": settings.Density = 2; break;
                case "nan": settings.Density = float.NaN; break;
                case "version": settings.Version = 0; break;
                case "thickness": settings.SeamThickness = 4; break;
                case "range": settings.PreferredMinPixels = new Vector2Int(256, 32); break;
                case "quantum": settings.PreferredMaxPixels = new Vector2Int(127, 128); break;
                case "palette": settings.SeamPalette = Array.Empty<Color>(); break;
                case "color": settings.SeamPalette[0] = new Color(float.PositiveInfinity, 0, 0); break;
            }
            var before = Inputs(room); var output = RoomUndoRedoTests.Snapshot(room); var group = Undo.GetCurrentGroup(); var dirty = room.gameObject.scene.isDirty;
            Assert.IsFalse(RoomAuthoringOperations.SetFloorPanelSettings(room, settings, out var issue));
            Assert.That(issue, Does.Contain(room.RoomId).And.Contain("FloorPanels/Settings"));
            Assert.AreEqual(before, Inputs(room)); CollectionAssert.AreEqual(output, RoomUndoRedoTests.Snapshot(room));
            Assert.AreEqual(group, Undo.GetCurrentGroup()); Assert.AreEqual(dirty, room.gameObject.scene.isDirty);
        }

        [TestCase("owner")] [TestCase("fractional")] [TestCase("zero")] [TestCase("outside")]
        [TestCase("missing-id")] [TestCase("duplicate")] [TestCase("huge")]
        public void InvalidReservationsRejectAtomicallyAndExternalRecordsRemainLocated(string invalid)
        {
            using var f = new RoomAuthoringTestFixtures(32); var room = Room(f);
            var record = invalid == "owner" ? new RoomFloorReservation("bed", "another-space", Vector2.zero, Vector2.one) :
                invalid == "fractional" ? new RoomFloorReservation("bed", room.RoomId, Vector2.one * .5f, Vector2.one) :
                invalid == "zero" ? Reserve(room, "bed", 0, 0, 0, 1) : invalid == "outside" ? Reserve(room, "bed", 7, 5, 2, 1) :
                invalid == "missing-id" ? Reserve(room, "", 0, 0) : invalid == "huge" ? Reserve(room, "bed", 0, 0, 1000000, 1000000) : Reserve(room, "bed", 0, 0);
            var records = invalid == "duplicate" ? new[] { record, record.Copy() } : new[] { record };
            var snapshot = RoomUndoRedoTests.Snapshot(room); var inputs = Inputs(room); var group = Undo.GetCurrentGroup();
            Assert.IsFalse(RoomAuthoringOperations.EditFloorPanelAuthoring(room, room.FloorPanels, records, out var issue));
            Assert.That(issue, Does.Contain(room.RoomId).And.Contain("Reservation/")); Assert.AreEqual(inputs, Inputs(room));
            CollectionAssert.AreEqual(snapshot, RoomUndoRedoTests.Snapshot(room)); Assert.AreEqual(group, Undo.GetCurrentGroup());
            room.SetFloorPanelAuthoring(new RoomFloorPanelSettings { Enabled = true }, records); var external = Inputs(room);
            var domain = RoomFloorPanelDomain.Build(room); Assert.IsFalse(domain.CanGenerate); Assert.IsNotEmpty(domain.Issues);
            Assert.IsTrue(domain.Issues.All(i => i.RoomId == room.RoomId && i.Field.StartsWith("FloorPanels/Reservation/")));
            Assert.IsFalse(RoomAuthoringOperations.Rebuild(room)); Assert.AreEqual(RoomGenerationStatus.IncompletePreview, room.GenerationResult.Status);
            Assert.AreEqual(external, Inputs(room)); Assert.AreEqual(48, RoomAuthoringTestFixtures.FloorCount(room));
            Assert.IsNotEmpty(room.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>());
        }

        [TestCase(false)] [TestCase(true)] public void OrphanedAndEmptyRecordsRemainAuthoredAndUndoRestoresThem(bool adopted)
        {
            using var f = new RoomAuthoringTestFixtures(32); var room = Room(f);
            if (adopted) RoomAuthoringCreation.AdoptIntoNewLayout(room);
            Assert.IsTrue(RoomAuthoringOperations.SetFloorPanelSettings(room, new RoomFloorPanelSettings { Enabled = true, Seed = 91 }, out var issue), issue);
            Assert.IsTrue(RoomAuthoringOperations.UpsertFloorReservation(room, Reserve(room, "bed", 1, 1), out issue), issue);
            var authored = Inputs(room); var before = RoomUndoRedoTests.Snapshot(room);
            Assert.IsFalse(RoomAuthoringOperations.PaintCell(room, new Vector3Int(1, 1, 0), null));
            Assert.AreEqual(47, RoomAuthoringTestFixtures.FloorCount(room)); Assert.AreEqual(authored, Inputs(room));
            Assert.IsTrue(room.GenerationResult.Issues.Any(i => i.Field == "FloorPanels/Reservation/bed" && i.HasRegion && !i.Inactive));
            Undo.PerformUndo(); CollectionAssert.AreEqual(before, RoomUndoRedoTests.Snapshot(room));
            Assert.IsFalse(RoomAuthoringOperations.PaintBox(room, new BoundsInt(0, 0, 0, 8, 6, 1), null));
            Assert.AreEqual(0, RoomAuthoringTestFixtures.FloorCount(room)); Assert.AreEqual(authored, Inputs(room));
            Assert.AreEqual(RoomGenerationStatus.IncompletePreview, room.GenerationResult.Status);
            Assert.IsTrue(room.GenerationResult.Issues.All(i => i.Inactive));
            Undo.PerformUndo(); CollectionAssert.AreEqual(before, RoomUndoRedoTests.Snapshot(room)); Assert.AreEqual(authored, Inputs(room));
        }

        [Test] public void UnsafeOutputAndPendingStrokeRefusePanelEditsAndNoOpCreatesNoUndo()
        {
            using var f = new RoomAuthoringTestFixtures(32); var room = Room(f); var before = Inputs(room);
            var group = Undo.GetCurrentGroup(); Assert.IsTrue(RoomAuthoringOperations.SetFloorPanelSettings(room, room.FloorPanels.Copy(), out var issue), issue);
            Assert.AreEqual(group, Undo.GetCurrentGroup());
            using (var edit = RoomAuthoringOperations.BeginEdit(room, "Pending panel fixture"))
            {
                Assert.IsFalse(RoomAuthoringOperations.SetFloorPanelSettings(room, new RoomFloorPanelSettings { Enabled = true }, out issue));
                Assert.That(issue, Does.Contain("paint stroke")); edit.Cancel();
            }
            var authored = new GameObject("Equipment", typeof(BoxCollider2D)); authored.transform.SetParent(room.GeneratedOutput.transform, false);
            var output = RoomAuthoringValidator.OutputFingerprint(room); group = Undo.GetCurrentGroup();
            Assert.IsFalse(RoomAuthoringOperations.SetFloorPanelSettings(room, new RoomFloorPanelSettings { Enabled = true }, out issue));
            Assert.That(issue, Does.Contain(room.RoomId).And.Contain("generated output"));
            Assert.AreEqual(group, Undo.GetCurrentGroup()); Assert.AreEqual(before, Inputs(room));
            Assert.AreEqual(output, RoomAuthoringValidator.OutputFingerprint(room)); Assert.IsNotNull(authored.GetComponent<BoxCollider2D>());
        }

        [Test] public void UnsupportedNativePixelCoordinatesAreLocalAndNeverAllocatePartialRegions()
        {
            using var f = new RoomAuthoringTestFixtures(32); var layout = RoomAuthoringCreation.CreateLayout(f.Scene, f.Style);
            var room = layout.Spaces[0]; room.transform.localPosition = Vector3.right * 100000000;
            ShipLayoutStyleTests.Fill(room, RoomAuthoringTestFixtures.Cells());
            room.SetFloorPanelAuthoring(new RoomFloorPanelSettings { Enabled = true }, Array.Empty<RoomFloorReservation>());
            var before = Inputs(room); var domain = RoomFloorPanelDomain.Build(room);
            Assert.IsFalse(domain.CanGenerate); Assert.IsEmpty(domain.PixelRegions);
            Assert.That(domain.Issues.Single().Field, Is.EqualTo("FloorPanels/Coordinates")); Assert.AreEqual(before, Inputs(room));
            Assert.AreEqual(48, RoomAuthoringOperations.ReadCells(room).Length);
        }

        [Test] public void LocatedLayoutDiagnosticsAndDomainReadsDoNotMutateState()
        {
            using var f = new RoomAuthoringTestFixtures(32); ShipLayoutStyleTests.Configure(f);
            var layout = RoomAuthoringCreation.CreateLayout(f.Scene, f.Style); var room = layout.Spaces[0];
            room.transform.localPosition = new Vector3(-9, -7, 0); ShipLayoutStyleTests.Fill(room, RoomAuthoringTestFixtures.Cells());
            room.SetFloorPanelAuthoring(new RoomFloorPanelSettings { Enabled = true }, new[] {
                new RoomFloorReservation("orphan", "wrong-owner", Vector2.one, Vector2.one) });
            Assert.IsFalse(RoomAuthoringOperations.Rebuild(layout)); Assert.AreEqual(48, RoomAuthoringTestFixtures.FloorCount(room));
            var located = layout.GenerationResult.Issues.Single(i => i.Field == "FloorPanels/Reservation/orphan");
            Assert.AreEqual(new Vector2Int(-8, -6), located.Start); Assert.AreEqual(new Vector2Int(-7, -5), located.End);
            Assert.AreEqual(new Vector3(-7.5f, -5.5f, 0), RoomAuthoringPreview.IssueBounds(room, located).center);
            Assert.AreEqual(new Vector3(-7.5f, -5.5f, 0), RoomAuthoringPreview.IssueBounds(layout, located).center);
            var roomState = EditorJsonUtility.ToJson(room); var layoutState = EditorJsonUtility.ToJson(layout);
            var output = RoomAuthoringValidator.OutputFingerprint(layout); var group = Undo.GetCurrentGroup(); var dirty = f.Scene.isDirty;
            var domain = RoomFloorPanelDomain.Build(room); domain.Settings.Seed = 77; domain.Settings.SeamPalette[0] = Color.red;
            Assert.AreEqual(0, domain.Settings.Seed); Assert.AreEqual(0, room.FloorPanels.Seed);
            var assessment = RoomAuthoringValidator.Assess(layout, out _);
            Assert.IsTrue(assessment.Issues.Any(i => i.Id == located.Id));
            Assert.AreEqual(roomState, EditorJsonUtility.ToJson(room)); Assert.AreEqual(layoutState, EditorJsonUtility.ToJson(layout));
            Assert.AreEqual(output, RoomAuthoringValidator.OutputFingerprint(layout)); Assert.AreEqual(group, Undo.GetCurrentGroup()); Assert.AreEqual(dirty, f.Scene.isDirty);
        }

        [Test] public void PanelEditPreflightsOtherLayoutMembersBeforeMutation()
        {
            using var f = new RoomAuthoringTestFixtures(32); ShipLayoutStyleTests.Configure(f);
            var layout = RoomAuthoringCreation.CreateLayout(f.Scene, f.Style); var room = layout.Spaces[0];
            var other = RoomAuthoringCreation.CreateSpace(layout); other.transform.localPosition = Vector3.right * 8;
            foreach (var member in layout.Spaces) ShipLayoutStyleTests.Fill(member, RoomAuthoringTestFixtures.Cells());
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(layout), layout.GenerationResult?.Summary);
            var authored = new GameObject("Equipment", typeof(BoxCollider2D)); authored.transform.SetParent(other.GeneratedOutput.transform, false);
            var output = RoomAuthoringValidator.OutputFingerprint(layout); var inputs = Inputs(room); var group = Undo.GetCurrentGroup();
            Assert.IsFalse(RoomAuthoringOperations.SetFloorPanelSettings(room, new RoomFloorPanelSettings { Enabled = true }, out var issue));
            Assert.That(issue, Does.Contain(other.RoomId)); Assert.AreEqual(inputs, Inputs(room));
            Assert.AreEqual(output, RoomAuthoringValidator.OutputFingerprint(layout)); Assert.AreEqual(group, Undo.GetCurrentGroup()); Assert.IsNotNull(authored.GetComponent<BoxCollider2D>());
        }

        [TestCase(false)] [TestCase(true)]
        public void SaveReopenPreservesPanelDataAndLegacyMissingFieldsDefaultDisabled(bool legacy)
        {
            Assert.IsTrue(Environment.GetCommandLineArgs().Contains("-solitudeRoomAuthoringVerification"));
            var folder = RoomPersistenceTests.CreateAssets();
            try
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var room = RoomPersistenceTests.CreateRoom(scene, folder); var id = room.RoomId;
                if (!legacy)
                {
                    room.Style.Rendering = new RoomStyleDefinition.RenderingConfiguration();
                    EditorUtility.SetDirty(room.Style); AssetDatabase.SaveAssets();
                    var settings = new RoomFloorPanelSettings { Enabled = true, Seed = -91, Density = .75f, SeamThickness = 3,
                        PreferredMinPixels = new Vector2Int(16, 24), PreferredMaxPixels = new Vector2Int(64, 96), SeamPalette = new[] { Color.cyan, Color.gray } };
                    Assert.IsTrue(RoomAuthoringOperations.SetFloorPanelSettings(room, settings, out var issue), issue);
                    Assert.IsTrue(RoomAuthoringOperations.UpsertFloorReservation(room, Reserve(room, "bed", 1, 1, 2, 2), out issue), issue);
                    Assert.IsTrue(RoomAuthoringOperations.UpsertFloorReservation(room, Reserve(room, "console", 2, 1, 2, 2), out issue), issue);
                }
                var inputs = Inputs(room); var result = JsonUtility.ToJson(room.GenerationResult); var visual = RoomPersistenceTests.Placements(room);
                var cells = RoomAuthoringOperations.ReadCells(room); var style = AssetDatabase.GetAssetPath(room.Style);
                var path = folder + "/PanelAuthoring.unity"; Assert.IsTrue(EditorSceneManager.SaveScene(scene, path));
                var guid = AssetDatabase.AssetPathToGUID(path);
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                if (legacy)
                {
                    var text = File.ReadAllText(path); Assert.That(text, Does.Contain("  floorPanels:"));
                    text = Regex.Replace(text, @"(?m)^  floorPanels:\r?\n(?:    .*\r?\n)*", "");
                    text = Regex.Replace(text, @"(?m)^  floorReservations: \[\]\r?\n", "");
                    Assert.That(text, Does.Not.Contain("  floorPanels:").And.Not.Contain("  floorReservations:"));
                    File.WriteAllText(path, text); AssetDatabase.ImportAsset(path);
                }
                var applications = 0; void Generated(ShipRoomAuthoring _) => applications++;
                RoomAuthoringOperations.GenerationCompleted += Generated;
                try { scene = EditorSceneManager.OpenScene(path); }
                finally { RoomAuthoringOperations.GenerationCompleted -= Generated; }
                room = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<ShipRoomAuthoring>()).Single();
                Assert.AreEqual(0, applications); Assert.AreEqual(id, room.RoomId); Assert.AreEqual(guid, AssetDatabase.AssetPathToGUID(path));
                Assert.AreEqual(inputs, Inputs(room)); Assert.AreEqual(result, JsonUtility.ToJson(room.GenerationResult));
                CollectionAssert.AreEqual(visual, RoomPersistenceTests.Placements(room)); CollectionAssert.AreEqual(cells, RoomAuthoringOperations.ReadCells(room));
                Assert.AreEqual(style, AssetDatabase.GetAssetPath(room.Style)); Assert.IsFalse(scene.isDirty); Assert.IsTrue(RoomAuthoringValidator.IsCurrent(room)); Assert.IsFalse(scene.isDirty);
                Assert.AreEqual(!legacy, room.FloorPanels.Enabled);
            }
            finally { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); AssetDatabase.DeleteAsset(folder); }
        }
    }
}
