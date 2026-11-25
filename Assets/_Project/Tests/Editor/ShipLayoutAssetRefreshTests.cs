using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SOLITUDE.Editor.RoomAuthoring;
using SOLITUDE.World.RoomAuthoring;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace SOLITUDE.Tests.RoomAuthoring
{
    public sealed class ShipLayoutAssetRefreshTests
    {
        private string folder, otherFolder;
        private Scene scene;
        private RoomStyleDefinition style, otherStyle;
        private ShipLayoutAuthoring first, second, unrelated;
        private readonly Dictionary<string, int> generations = new Dictionary<string, int>();
        private int total;

        [SetUp] public void SetUp()
        {
            Assert.IsTrue(Environment.GetCommandLineArgs().Contains("-solitudeRoomAuthoringVerification"));
            folder = RoomPersistenceTests.CreateAssets(); otherFolder = RoomPersistenceTests.CreateAssets();
            var importer = (TextureImporter)AssetImporter.GetAtPath(folder + "/square.png"); importer.isReadable = true; importer.SaveAndReimport();
            style = AssetDatabase.LoadAssetAtPath<RoomStyleDefinition>(folder + "/Style.asset");
            otherStyle = AssetDatabase.LoadAssetAtPath<RoomStyleDefinition>(otherFolder + "/Style.asset");
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            first = Layout(style, 0); second = Layout(style, 20); unrelated = Layout(otherStyle, 40);
            var sentinel = new GameObject("Authored Gameplay Sentinel"); sentinel.transform.position = new Vector3(5, 9, 0);
            generations.Clear(); total = 0; RoomAuthoringOperations.GenerationCompleted += Generated;
        }

        [TearDown] public void TearDown()
        {
            RoomAuthoringOperations.GenerationCompleted -= Generated;
            RoomAuthoringOperations.CancelActiveEdit(); Selection.activeObject = null;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AssetDatabase.DeleteAsset(folder); AssetDatabase.DeleteAsset(otherFolder);
            RoomFloorPanelAssetStore.VerificationRoot = null;
        }

        private ShipLayoutAuthoring Layout(RoomStyleDefinition selected, int x)
        {
            var layout = RoomAuthoringCreation.CreateLayout(scene, selected); layout.transform.position = Vector3.right * x;
            foreach (var cell in RoomAuthoringTestFixtures.Cells()) layout.Spaces[0].Occupancy.SetTile(new Vector3Int(cell.x, cell.y, 0), selected.FloorTile);
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(layout), layout.GenerationResult?.Summary);
            return layout;
        }

        private void Generated(ShipRoomAuthoring room)
        {
            total++;
            var id = room.Layout.LayoutId;
            generations[id] = Count(room.Layout) + 1;
        }
        private int Count(ShipLayoutAuthoring layout) => generations.TryGetValue(layout.LayoutId, out var count) ? count : 0;
        private static string[] Snapshot(ShipLayoutAuthoring layout) => RoomUndoRedoTests.Snapshot(layout.Spaces[0]);
        private static Tilemap Floor(ShipLayoutAuthoring layout) => layout.Spaces[0].GeneratedOutput.GetComponentInChildren<Tilemap>();
        private static void Pixel(string path, Color color)
        {
            var texture = new Texture2D(2, 2);
            texture.LoadImage(File.ReadAllBytes(path)); texture.SetPixel(0, 0, color);
            File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }
        private IEnumerator Until(Func<bool> done)
        {
            var deadline = EditorApplication.timeSinceStartup + 10;
            while (!done() && EditorApplication.timeSinceStartup < deadline) yield return null;
            Assert.IsTrue(done(), "Refresh timed out. " + RoomAuthoringAssetRefresh.GetIssue(first));
        }
        private static IEnumerator Settle()
        {
            var until = EditorApplication.timeSinceStartup + .85;
            while (EditorApplication.timeSinceStartup < until) yield return null;
        }

        [UnityTest] public IEnumerator PixelOnlyImportRefreshesExactlySharedConsumersAndActualFloorTexture()
        {
            var before = Snapshot(unrelated); var cells = RoomAuthoringOperations.ReadCells(first.Spaces[0]); var ids = new[] { first.LayoutId, first.Spaces[0].RoomId };
            Pixel(folder + "/square.png", Color.green);
            Assert.IsFalse(RoomAuthoringValidator.IsCurrent(first));
            yield return Until(() => Count(first) == 1 && Count(second) == 1);
            Assert.AreEqual(0, Count(unrelated)); CollectionAssert.AreEqual(before, Snapshot(unrelated));
            Assert.AreEqual(Color.green, Floor(first).GetSprite(Vector3Int.zero).texture.GetPixel(0, 0));
            Assert.AreEqual(48, RoomAuthoringTestFixtures.FloorCount(first.Spaces[0]));
            CollectionAssert.AreEqual(cells, RoomAuthoringOperations.ReadCells(first.Spaces[0]));
            CollectionAssert.AreEqual(ids, new[] { first.LayoutId, first.Spaces[0].RoomId });
            Assert.IsTrue(RoomAuthoringValidator.IsCurrent(first));
            yield return Settle(); Assert.AreEqual(2, total);
            Assert.AreEqual(new Vector3(5, 9, 0), scene.GetRootGameObjects().Single(g => g.name == "Authored Gameplay Sentinel").transform.position);
        }

        [UnityTest] public IEnumerator TileSpriteRebindingUpdatesExistingFloorWithoutRepainting()
        {
            var tile = (Tile)style.FloorTile; var replacement = ((Tile)otherStyle.FloorTile).sprite;
            tile.sprite = replacement; EditorUtility.SetDirty(tile); AssetDatabase.SaveAssets();
            yield return Until(() => Count(first) == 1 && Count(second) == 1);
            Assert.AreSame(replacement, Floor(first).GetSprite(Vector3Int.zero));
            Assert.AreEqual(48, RoomAuthoringTestFixtures.FloorCount(first.Spaces[0]));
            Assert.AreEqual(0, Count(unrelated)); Assert.IsTrue(RoomAuthoringValidator.IsCurrent(first));
        }

        private void EnablePanels()
        {
            RoomFloorPanelAssetStore.VerificationRoot = folder + "/Panels";
            style.Rendering = new RoomStyleDefinition.RenderingConfiguration();
            EditorUtility.SetDirty(style); AssetDatabase.SaveAssets();
            foreach (var layout in new[] { first, second })
            {
                var config = layout.Spaces[0].FloorPanels.Copy(); config.Enabled = true;
                Assert.IsTrue(RoomAuthoringOperations.SetFloorPanelSettings(layout.Spaces[0], config, out var issue), issue);
                Assert.IsTrue(RoomAuthoringValidator.IsCurrent(layout));
            }
            generations.Clear(); total = 0;
        }

        private void ImportIntegralPivot()
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(folder + "/wall.png");
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom; settings.spritePivot = Vector2.zero;
            importer.SetTextureSettings(settings); importer.SaveAndReimport();
        }

        [UnityTest] public IEnumerator CompatiblePivotImportRefreshesPanelEnabledConsumers()
        {
            EnablePanels(); var room = first.Spaces[0]; var cells = RoomAuthoringOperations.ReadCells(room);
            var panel = room.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>().Single(r => r.name == "Generated Floor Panels").sprite;
            ImportIntegralPivot();
            yield return Until(() => Count(first) == 1 && Count(second) == 1);
            Assert.AreEqual(RoomGenerationStatus.Complete, first.GenerationResult.Status);
            Assert.IsTrue(RoomAuthoringValidator.IsCurrent(first)); Assert.IsTrue(RoomAuthoringValidator.IsCurrent(second));
            Assert.AreEqual(0, Count(unrelated)); CollectionAssert.AreEqual(cells, RoomAuthoringOperations.ReadCells(room));
            Assert.AreSame(panel, room.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>().Single(r => r.name == "Generated Floor Panels").sprite);
            var north = room.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>().Where(r => r.name == "North").OrderBy(r => r.transform.localPosition.x).First();
            Assert.AreEqual(Vector2.zero, north.sprite.pivot);
            Assert.AreEqual(0, north.transform.localPosition.x);
            yield return Settle(); Assert.AreEqual(2, total);
        }

        [Test] public void ExplicitRebuildRecoversCompatiblePivotImportWithPanels()
        {
            EnablePanels(); ImportIntegralPivot();
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(first), first.GenerationResult.Summary);
            Assert.IsTrue(RoomAuthoringValidator.IsCurrent(first));
            Assert.AreEqual(48, RoomAuthoringTestFixtures.FloorCount(first.Spaces[0]));
        }

        [UnityTest] public IEnumerator MetadataAndReferenceBurstPublishesFinalStateOnce()
        {
            var replacement = otherStyle.NorthWalls[0].Sprite;
            for (var i = 0; i < 3; i++)
            {
                style.NorthWalls[0].Sprite = replacement; style.NorthWalls[0].OffsetPixels = Vector2.right * i;
                EditorUtility.SetDirty(style); AssetDatabase.SaveAssets();
            }
            yield return Until(() => Count(first) == 1 && Count(second) == 1);
            var north = first.GetComponentsInChildren<SpriteRenderer>().Where(r => r.name == "North").OrderBy(r => r.transform.position.x).ToArray();
            Assert.AreEqual(4, north.Length); Assert.IsTrue(north.All(r => r.sprite == replacement));
            Assert.AreEqual((replacement.pivot.x + 2) / 32f, north[0].transform.position.x);
            yield return Settle(); Assert.AreEqual(2, total); Assert.AreEqual(0, Count(unrelated));
        }

        [UnityTest] public IEnumerator PendingStrokeCommitConsumesAssetChangeInOneGeneration() { yield return Gesture(true); }
        [UnityTest] public IEnumerator CancelledStrokeStillRefreshesQueuedExternalAssets() { yield return Gesture(false); }
        private IEnumerator Gesture(bool commit)
        {
            var room = first.Spaces[0]; var before = Snapshot(first);
            using var edit = RoomAuthoringOperations.BeginEdit(room, "Pending refresh fixture");
            Assert.IsTrue(RoomAuthoringOperations.PaintBox(room, new BoundsInt(8, 0, 0, 2, 6, 1), style.FloorTile, edit));
            Pixel(folder + "/square.png", Color.green);
            yield return Settle(); Assert.AreEqual(0, total); Assert.IsTrue(RoomAuthoringOperations.IsEditing(room));
            Assert.IsFalse(RoomAuthoringValidator.IsCurrent(first));
            // Saved generated output is still the pre-gesture 48-cell result while authored occupancy is pending.
            Assert.AreEqual(48, RoomAuthoringTestFixtures.FloorCount(room));
            if (commit) Assert.IsTrue(edit.Commit()); else edit.Cancel();
            yield return Until(() => Count(first) == 1 && Count(second) == 1);
            Assert.AreEqual(commit ? 60 : 48, RoomAuthoringTestFixtures.FloorCount(room));
            Assert.AreEqual(commit ? 60 : 48, RoomAuthoringOperations.ReadCells(room).Length);
            Assert.AreEqual(Color.green, Floor(first).GetSprite(Vector3Int.zero).texture.GetPixel(0, 0));
            Assert.IsTrue(RoomAuthoringValidator.IsCurrent(first)); yield return Settle(); Assert.AreEqual(2, total);
        }

        [UnityTest] public IEnumerator UnreferencedPreparedTextureImportsDoNotQueueOrRecurse()
        {
            var outputPath = folder + "/PreparedFloorDetail.png";
            File.Copy(folder + "/square.png", outputPath); AssetDatabase.ImportAsset(outputPath);
            yield return Settle(); Assert.AreEqual(0, total); Assert.IsFalse(RoomAuthoringAssetRefresh.IsPending(first));
            Pixel(folder + "/square.png", Color.green);
            yield return Until(() => Count(first) == 1 && Count(second) == 1);
            Pixel(outputPath, Color.blue); yield return Settle(); Assert.AreEqual(2, total);
        }

        [UnityTest] public IEnumerator SupportedRoleBindingEditUndoRedoRestoresAllConsumersAsOneAction()
        {
            var beforeA = Snapshot(first); var beforeB = Snapshot(second); var beforeOther = Snapshot(unrelated); var original = style.FloorTile;
            Assert.IsTrue(RoomAuthoringOperations.EditStyleBindings(style, selected => selected.FloorTile = otherStyle.FloorTile, out var issue), issue);
            Assert.AreEqual(2, total); var afterA = Snapshot(first); var afterB = Snapshot(second);
            Assert.AreSame(otherStyle.FloorTile, Floor(first).GetTile(Vector3Int.zero));
            Undo.PerformUndo(); Assert.AreSame(original, style.FloorTile);
            CollectionAssert.AreEqual(beforeA, Snapshot(first)); CollectionAssert.AreEqual(beforeB, Snapshot(second));
            yield return Settle(); Assert.AreEqual(2, total); Assert.IsTrue(RoomAuthoringValidator.IsCurrent(first));
            Undo.PerformRedo(); Assert.AreSame(otherStyle.FloorTile, style.FloorTile);
            CollectionAssert.AreEqual(afterA, Snapshot(first)); CollectionAssert.AreEqual(afterB, Snapshot(second));
            yield return Settle(); Assert.AreEqual(2, total); CollectionAssert.AreEqual(beforeOther, Snapshot(unrelated));
            Assert.IsTrue(RoomAuthoringValidator.IsCurrent(first));
        }

        [UnityTest] public IEnumerator UndoOfAssetRefreshLeavesExternalBytesCurrentAndRedoAvailable()
        {
            Pixel(folder + "/square.png", Color.green); yield return Until(() => total == 2);
            // Each affected layout has its own refresh Undo action; the last one is a known loaded consumer.
            Undo.PerformUndo();
            Assert.AreEqual(Color.green, ((Tile)style.FloorTile).sprite.texture.GetPixel(0, 0));
            Assert.IsTrue(!RoomAuthoringValidator.IsCurrent(first) || !RoomAuthoringValidator.IsCurrent(second));
            yield return Settle(); Assert.AreEqual(2, total);
            Undo.PerformRedo(); Assert.IsTrue(RoomAuthoringValidator.IsCurrent(first)); Assert.IsTrue(RoomAuthoringValidator.IsCurrent(second));
            yield return Settle(); Assert.AreEqual(2, total);
        }

        [UnityTest] public IEnumerator ClosedSceneReopensStaleWithoutGeneratingAndExplicitRebuildRecovers()
        {
            var path = folder + "/ClosedRefresh.unity"; Assert.IsTrue(EditorSceneManager.SaveScene(scene, path)); var id = first.LayoutId;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Pixel(folder + "/square.png", Color.green); yield return Settle(); Assert.AreEqual(0, total);
            scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            first = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ShipLayoutAuthoring>()).Single(l => l.LayoutId == id);
            yield return Settle(); Assert.AreEqual(0, total); Assert.IsFalse(RoomAuthoringValidator.IsCurrent(first));
            var before = Snapshot(first); var group = Undo.GetCurrentGroup(); var dirty = scene.isDirty;
            RoomAuthoringValidator.AssessSpaces(first, out _);
            CollectionAssert.AreEqual(before, Snapshot(first)); Assert.AreEqual(group, Undo.GetCurrentGroup()); Assert.AreEqual(dirty, scene.isDirty);
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(first)); Assert.AreEqual(1, total); Assert.IsTrue(RoomAuthoringValidator.IsCurrent(first));
            Assert.AreEqual(48, RoomAuthoringTestFixtures.FloorCount(first.Spaces[0]));
        }

        [UnityTest] public IEnumerator InvalidSpriteImportKeepsFloorAndIndependentArtThenRecoversLocally()
        {
            var path = folder + "/wall.png"; var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.spritePixelsPerUnit = 64; importer.SaveAndReimport();
            yield return Until(() => Count(first) == 1 && Count(second) == 1);
            Assert.AreEqual(RoomGenerationStatus.IncompletePreview, first.GenerationResult.Status);
            Assert.AreEqual(48, RoomAuthoringTestFixtures.FloorCount(first.Spaces[0]));
            Assert.IsTrue(first.GenerationResult.Issues.Any(i => i.HasRegion && i.Field == "North" && i.Reason.Contains("PPU")));
            Assert.IsFalse(first.GetComponentsInChildren<SpriteRenderer>().Any(r => r.name == "North"));
            Assert.IsTrue(first.GetComponentsInChildren<SpriteRenderer>().Any(r => r.name == "East"));
            importer.spritePixelsPerUnit = 32; importer.SaveAndReimport();
            yield return Until(() => Count(first) == 2 && Count(second) == 2);
            Assert.AreEqual(RoomGenerationStatus.Complete, first.GenerationResult.Status); Assert.IsEmpty(first.GenerationResult.Issues);
            Assert.AreEqual(0, Count(unrelated));
        }

        [UnityTest] public IEnumerator SavingDuringQueuedImportDoesNotPublishOrLosePendingRefresh()
        {
            Pixel(folder + "/square.png", Color.green);
            Assert.IsTrue(EditorSceneManager.SaveScene(scene, folder + "/PendingSave.unity")); Assert.AreEqual(0, total);
            yield return Until(() => Count(first) == 1 && Count(second) == 1);
            Assert.IsTrue(RoomAuthoringValidator.IsCurrent(first));
        }

        [UnityTest] public IEnumerator UnrelatedUndoDoesNotAdoptManuallyAddedGeneratedContent()
        {
            var root = scene.GetRootGameObjects().Single(g => g.name == "Authored Gameplay Sentinel");
            Undo.IncrementCurrentGroup(); Undo.RecordObject(root.transform, "Unrelated authored edit");
            root.transform.position = Vector3.one; Undo.FlushUndoRecordObjects(); Undo.IncrementCurrentGroup();
            var manual = new GameObject("Manual Content"); manual.transform.SetParent(first.Spaces[0].GeneratedOutput.transform);
            Undo.PerformUndo();
            Pixel(folder + "/square.png", Color.green);
            yield return Until(() => Count(second) == 1);
            Assert.IsTrue(manual != null); Assert.AreEqual(0, Count(first));
            Assert.That(RoomAuthoringAssetRefresh.GetIssue(first), Does.Contain("Explicit Rebuild"));
        }

        [UnityTest] public IEnumerator ReopeningDoesNotAdoptManuallyAddedGeneratedContent()
        {
            var id = first.LayoutId; var secondId = second.LayoutId;
            var manual = new GameObject("Manual Content"); manual.transform.SetParent(first.Spaces[0].GeneratedOutput.transform);
            var path = folder + "/ManualContent.unity"; Assert.IsTrue(EditorSceneManager.SaveScene(scene, path));
            scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var layouts = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ShipLayoutAuthoring>()).ToArray();
            first = layouts.Single(l => l.LayoutId == id); second = layouts.Single(l => l.LayoutId == secondId);
            Pixel(folder + "/square.png", Color.green);
            yield return Until(() => Count(second) == 1);
            Assert.IsNotNull(first.Spaces[0].GeneratedOutput.transform.Find("Manual Content")); Assert.AreEqual(0, Count(first));
            Assert.That(RoomAuthoringAssetRefresh.GetIssue(first), Does.Contain("Explicit Rebuild"));
        }

        [UnityTest] public IEnumerator ManualGeneratedContentIsPreservedAndAutomaticRefreshReportsWhyItStopped()
        {
            var sentinel = new GameObject("Manually Authored Sentinel"); sentinel.transform.SetParent(first.Spaces[0].GeneratedOutput.transform);
            Pixel(folder + "/square.png", Color.green);
            yield return Until(() => Count(second) == 1 && !string.IsNullOrEmpty(RoomAuthoringAssetRefresh.GetIssue(first)));
            Assert.IsTrue(sentinel != null); Assert.AreEqual(0, Count(first)); Assert.AreEqual(0, Count(unrelated));
            Assert.That(RoomAuthoringAssetRefresh.GetIssue(first), Does.Contain("changed manually"));
            Assert.IsFalse(RoomAuthoringValidator.IsCurrent(first));
        }
    }
}
