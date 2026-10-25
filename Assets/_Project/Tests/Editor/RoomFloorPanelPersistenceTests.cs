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
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SOLITUDE.Tests.RoomAuthoring
{
    public sealed class RoomFloorPanelPersistenceTests
    {
        private string folder;
        private ShipRoomAuthoring room;
        [SetUp] public void Setup()
        {
            Assert.IsTrue(Environment.GetCommandLineArgs().Contains("-solitudeRoomAuthoringVerification"));
            Undo.ClearAll();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            folder = RoomPersistenceTests.CreateAssets();
            RoomFloorPanelAssetStore.VerificationRoot = folder + "/Panels";
            room = RoomPersistenceTests.CreateRoom(scene, folder);
            room.Style.Rendering = new RoomStyleDefinition.RenderingConfiguration();
            EditorUtility.SetDirty(room.Style); AssetDatabase.SaveAssets();
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(room));
        }
        [TearDown] public void Cleanup()
        {
            RoomFloorPanelAssetStore.PublicationCheckpoint = null;
            Undo.ClearAll(); EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (folder != null) AssetDatabase.DeleteAsset(folder); // This test's unique owned fixture only.
            RoomFloorPanelAssetStore.VerificationRoot = null;
        }
        private void Enable(int seed = 17, int thickness = 1)
        {
            var config = room.FloorPanels.Copy(); config.Enabled = true; config.Seed = seed; config.SeamThickness = thickness;
            Assert.IsTrue(RoomAuthoringOperations.SetFloorPanelSettings(room, config, out var issue), issue);
            Assert.IsTrue(room.GenerationResult.IsSuccessful, room.GenerationDiagnostic);
        }
        private RoomFloorPanelOutput Revision() => AssetDatabase.LoadAssetAtPath<RoomFloorPanelOutput>(
            AssetDatabase.GetAssetPath(room.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>()
                .Single(r => r.name == "Generated Floor Panels").sprite));
        private static string PixelHash(RoomFloorPanelOutput revision) => RoomFloorPanelAssetStore.ContentHash(revision);

        [Test] public void RebuildReusesImmutableReferencesAndNativeRendering()
        {
            Enable(); var revision = Revision(); var hash = PixelHash(revision);
            var sprites = revision.Chunks.Select(c => c.Sprite).ToArray(); var count = RoomFloorPanelAssetStore.RetainedRevisionCount;
            Assert.IsTrue(RoomFloorPanelAssetStore.IsVerified(revision));
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(room)); Assert.AreSame(revision, Revision());
            CollectionAssert.AreEqual(sprites, Revision().Chunks.Select(c => c.Sprite));
            Assert.AreEqual(hash, PixelHash(revision)); Assert.AreEqual(count, RoomFloorPanelAssetStore.RetainedRevisionCount);
            var renderer = room.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>().Single(r => r.name == "Generated Floor Panels");
            Assert.Greater(renderer.sortingOrder, room.Style.Rendering.FloorOrder);
            Assert.Less(renderer.sortingOrder, room.Style.Rendering.ArchitectureOrder);
            Assert.IsEmpty(renderer.GetComponents<Collider2D>());
            Assert.AreEqual(48, RoomAuthoringTestFixtures.FloorCount(room)); Assert.IsTrue(RoomAuthoringValidator.IsCurrent(room));
        }
        [Test] public void SeedAndThicknessUndoRedoRestoreExactPixelsAndDiagnostics()
        {
            Enable(); var old = Revision(); var hash = PixelHash(old); var result = JsonUtility.ToJson(room.GenerationResult);
            Enable(29, 3); var next = Revision(); var nextHash = PixelHash(next); var nextResult = JsonUtility.ToJson(room.GenerationResult);
            Assert.AreNotEqual(hash, nextHash); Assert.AreEqual(2, RoomFloorPanelAssetStore.RetainedRevisionCount);
            Undo.PerformUndo(); Assert.AreSame(old, Revision()); Assert.AreEqual(hash, PixelHash(old));
            Assert.AreEqual(result, JsonUtility.ToJson(room.GenerationResult)); Assert.AreEqual(17, room.FloorPanels.Seed);
            Undo.PerformRedo(); Assert.AreSame(next, Revision()); Assert.AreEqual(nextHash, PixelHash(next));
            Assert.AreEqual(nextResult, JsonUtility.ToJson(room.GenerationResult)); Assert.AreEqual(hash, PixelHash(old));
        }
        [Test] public void SavedSceneAndDuplicatedLayoutKeepHistoricalPixels()
        {
            Enable(); var old = Revision(); var oldHash = PixelHash(old);
            Assert.IsTrue(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(old, out string oldGuid, out long oldFileId));
            var spriteIdentities = old.Chunks.Select(c =>
            {
                Assert.IsTrue(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(c.Sprite, out string guid, out long fileId));
                return guid + ":" + fileId;
            }).ToArray();
            var path = folder + "/Historical.unity"; Assert.IsTrue(EditorSceneManager.SaveScene(room.gameObject.scene, path));
            var copy = Object.Instantiate(room.gameObject); var copySprite = copy.GetComponentsInChildren<SpriteRenderer>().Single(r => r.name == "Generated Floor Panels").sprite;
            // A separate saved scene still owns the original content even after the current copy changes.
            Object.DestroyImmediate(copy); Enable(36, 2);
            Assert.AreEqual(oldHash, PixelHash(old)); Assert.AreSame(old.Chunks[0].Sprite, copySprite);
            Assert.IsFalse(RoomFloorPanelAssetStore.CleanupUnused(old, true, out var refusal)); StringAssert.Contains("saved reference", refusal);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var scene = EditorSceneManager.OpenScene(path); room = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ShipRoomAuthoring>()).Single();
            var reopened = Revision();
            Assert.IsTrue(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(reopened, out string reopenedGuid, out long reopenedFileId));
            Assert.AreEqual(oldGuid, reopenedGuid); Assert.AreEqual(oldFileId, reopenedFileId);
            CollectionAssert.AreEqual(spriteIdentities, reopened.Chunks.Select(c =>
            {
                Assert.IsTrue(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(c.Sprite, out string guid, out long fileId));
                return guid + ":" + fileId;
            }));
            Assert.AreEqual(oldHash, PixelHash(reopened)); Assert.IsTrue(RoomAuthoringValidator.IsCurrent(room));
        }
        [TestCase("allocation")] [TestCase("import")] [TestCase("publication")]
        public void FailedPublicationHasUsefulBaseGeometryAndNoMissingReferences(string checkpoint)
        {
            Enable(); var old = Revision(); var hash = PixelHash(old); var count = RoomFloorPanelAssetStore.RetainedRevisionCount;
            RoomFloorPanelAssetStore.PublicationCheckpoint = name => { if (name == checkpoint) throw new IOException("Injected " + checkpoint); };
            var config = room.FloorPanels.Copy(); config.Seed = 83;
            Assert.IsTrue(RoomAuthoringOperations.SetFloorPanelSettings(room, config, out var issue), issue);
            Assert.AreEqual(RoomGenerationStatus.IncompletePreview, room.GenerationResult.Status);
            Assert.IsTrue(room.GenerationResult.Issues.Any(i => i.Field == "FloorPanels/Publication"));
            Assert.IsFalse(RoomAuthoringValidator.IsCurrent(room)); Assert.AreEqual(48, RoomAuthoringTestFixtures.FloorCount(room));
            Assert.IsTrue(room.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>().All(r => r.sprite != null));
            Assert.IsFalse(room.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>().Any(r => r.name == "Generated Floor Panels"));
            Assert.AreEqual(hash, PixelHash(old)); Assert.AreEqual(count, RoomFloorPanelAssetStore.RetainedRevisionCount);
        }
        [Test] public void DisableAndEmptyClearVisualsButKeepAuthoredInputsAndHistory()
        {
            Enable(); var old = Revision(); var config = room.FloorPanels.Copy(); config.Enabled = false;
            Assert.IsTrue(RoomAuthoringOperations.SetFloorPanelSettings(room, config, out _));
            Assert.IsFalse(room.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>().Any(r => r.name == "Generated Floor Panels"));
            Assert.AreEqual(17, room.FloorPanels.Seed); Assert.IsTrue(old != null);
            Undo.PerformUndo(); Assert.AreSame(old, Revision());
            room.Occupancy.ClearAllTiles(); RoomAuthoringOperations.Rebuild(room);
            Assert.AreEqual(0, room.GeneratedOutput.transform.childCount); Assert.AreEqual(17, room.FloorPanels.Seed);
            Assert.IsTrue(old != null);
        }
        [Test] public void UnsafeGeneratedChildRefusesPanelReplacement()
        {
            Enable(); var old = Revision(); var sentinel = new GameObject("Authored sentinel"); sentinel.transform.SetParent(room.GeneratedOutput.transform, false);
            Assert.IsFalse(RoomAuthoringOperations.Rebuild(room)); Assert.IsTrue(sentinel != null); Assert.AreSame(old, Revision());
            Assert.AreEqual(RoomGenerationStatus.Refused, room.GenerationResult.Status);
        }
        [TestCase(false, "Generated Floor")]
        [TestCase(false, "Generated Floor Panels")]
        [TestCase(true, "Generated Floor")]
        [TestCase(true, "Generated Floor Panels")]
        public void MissingGeneratedChildRecoversWithPanelsAndUndo(bool layoutOwned, string childName)
        {
            if (layoutOwned) Assert.IsNotNull(RoomAuthoringCreation.AdoptIntoNewLayout(room));
            Enable(); var old = Revision(); var pixels = PixelHash(old);
            var input = RoomAuthoringOperations.ReadCells(room);
            Object.DestroyImmediate(room.GeneratedOutput.transform.Find(childName).gameObject);
            var path = folder + "/MissingChild.unity";
            Assert.IsTrue(EditorSceneManager.SaveScene(room.gameObject.scene, path));
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var scene = EditorSceneManager.OpenScene(path);
            room = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ShipRoomAuthoring>()).Single();
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(room), room.GenerationDiagnostic);
            Assert.IsNotNull(room.GeneratedOutput.transform.Find(childName));
            Assert.AreEqual(48, RoomAuthoringTestFixtures.FloorCount(room));
            Assert.AreSame(old, Revision()); Assert.AreEqual(pixels, PixelHash(old));
            CollectionAssert.AreEqual(input, RoomAuthoringOperations.ReadCells(room));
            Assert.IsTrue(RoomAuthoringValidator.IsCurrent(room));
            Undo.PerformUndo(); Assert.IsNull(room.GeneratedOutput.transform.Find(childName));
            Undo.PerformRedo(); Assert.IsNotNull(room.GeneratedOutput.transform.Find(childName));
            Assert.IsTrue(RoomAuthoringValidator.IsCurrent(room));
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(room), room.GenerationDiagnostic);
        }

        [Test] public void MissingAllChildrenCanBeRestoredWithoutChangingPreparedResources()
        {
            Enable(); var old = Revision(); var count = RoomFloorPanelAssetStore.RetainedRevisionCount;
            foreach (var child in room.GeneratedOutput.transform.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(room), room.GenerationDiagnostic);
            Assert.AreSame(old, Revision()); Assert.AreEqual(count, RoomFloorPanelAssetStore.RetainedRevisionCount);
            Assert.AreEqual(48, RoomAuthoringTestFixtures.FloorCount(room));
        }

        [TestCase("component")] [TestCase("nested")] [TestCase("position")] [TestCase("root")]
        public void MissingChildDoesNotAuthorizeDiscardingOtherModifiedContent(string modification)
        {
            Enable(); var floor = room.GeneratedOutput.transform.Find("Generated Floor");
            Object.DestroyImmediate(room.GeneratedOutput.transform.Find("Generated Floor Panels").gameObject);
            Object sentinel = floor;
            if (modification == "component") sentinel = floor.gameObject.AddComponent<BoxCollider2D>();
            if (modification == "nested")
            {
                var go = new GameObject("Nested authored sentinel"); go.transform.SetParent(floor, false); sentinel = go;
            }
            if (modification == "position") floor.localPosition = Vector3.right;
            if (modification == "root") sentinel = room.GeneratedOutput.gameObject.AddComponent<BoxCollider2D>();
            Assert.IsFalse(RoomAuthoringOperations.Rebuild(room)); Assert.IsTrue(sentinel != null);
            Assert.AreEqual(RoomGenerationStatus.Refused, room.GenerationResult.Status);
            Assert.IsNull(room.GeneratedOutput.transform.Find("Generated Floor Panels"));
        }

        [Test] public void DeletedRendererDoesNotHideModifiedHistoricalPixels()
        {
            Enable(); var old = Revision(); var texture = old.Chunks[0].Texture;
            texture.SetPixel(0, 0, Color.magenta); texture.Apply(); EditorUtility.SetDirty(texture);
            Object.DestroyImmediate(room.GeneratedOutput.transform.Find("Generated Floor Panels").gameObject);
            Assert.IsFalse(RoomAuthoringOperations.Rebuild(room));
            Assert.AreEqual((Color32)Color.magenta, texture.GetPixels32()[0]);
        }

        [Test] public void CleanupRequiresExplicitUndoBoundaryAndRejectsSavedOrLoadedReferences()
        {
            Enable(); var old = Revision(); Enable(45); var current = Revision();
            Assert.IsFalse(RoomFloorPanelAssetStore.CleanupUnused(old, false, out _));
            Assert.IsFalse(RoomFloorPanelAssetStore.CleanupUnused(current, true, out var loaded)); StringAssert.Contains("loaded", loaded);
            Assert.IsTrue(RoomFloorPanelAssetStore.CleanupUnused(old, true, out var issue), issue);
            Assert.IsTrue(current != null); Assert.AreEqual(1, RoomFloorPanelAssetStore.RetainedRevisionCount);
        }
        [Test] public void ModifiedHistoricalPixelsAreNeverOverwrittenOrClaimedCurrent()
        {
            Enable(); var revision = Revision(); var texture = revision.Chunks[0].Texture;
            texture.SetPixel(0, 0, Color.magenta); texture.Apply(); EditorUtility.SetDirty(texture);
            Assert.IsFalse(RoomAuthoringValidator.IsCurrent(room));
            Assert.IsFalse(RoomAuthoringOperations.Rebuild(room));
            Assert.AreEqual((Color32)Color.magenta, texture.GetPixels32()[0]);
        }

        [Test] public void MultiSpaceRebuildUsesLocalPositionsAndPreservesUnaffectedRevisions()
        {
            var style = room.Style; var scene = room.gameObject.scene; Object.DestroyImmediate(room.gameObject);
            var layout = RoomAuthoringCreation.CreateLayout(scene, style); room = layout.Spaces[0];
            var other = RoomAuthoringCreation.CreateSpace(layout); other.transform.localPosition = new Vector3(16, -8, 0);
            foreach (var space in layout.Spaces)
            {
                foreach (var cell in RoomAuthoringTestFixtures.Cells()) space.Occupancy.SetTile(new Vector3Int(cell.x, cell.y, 0), style.FloorTile);
                space.SetFloorPanelAuthoring(new RoomFloorPanelSettings { Enabled = true, Seed = 12 }, Array.Empty<RoomFloorReservation>());
            }
            RoomAuthoringOperations.Rebuild(layout); var old = Revision();
            var otherSprite = other.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>().Single(r => r.name == "Generated Floor Panels");
            var otherRevision = AssetDatabase.LoadAssetAtPath<RoomFloorPanelOutput>(AssetDatabase.GetAssetPath(otherSprite.sprite));
            Assert.AreEqual(Vector3.zero, otherSprite.transform.localPosition);
            var pixels = PixelHash(otherRevision);
            var config = room.FloorPanels.Copy(); config.Seed = 75;
            Assert.IsTrue(RoomAuthoringOperations.SetFloorPanelSettings(room, config, out var issue), issue);
            Assert.AreNotSame(old, Revision());
            Assert.AreEqual(pixels, PixelHash(otherRevision));
            Assert.AreSame(otherRevision.Chunks[0].Sprite, other.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>()
                .Single(r => r.name == "Generated Floor Panels").sprite);
        }

        [Test] public void PreparedResourcePlayerBundleBuildPreservesExactPixelsWithoutGeneration()
        {
            Enable(); var revision = Revision(); var hash = PixelHash(revision); var publications = RoomFloorPanelAssetStore.PublicationCount;
            var directory = Path.GetFullPath("Temp/PanelBundle" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
            var manifest = BuildPipeline.BuildAssetBundles(directory, new[] { new AssetBundleBuild
            { assetBundleName = "panels", assetNames = new[] { AssetDatabase.GetAssetPath(revision) } } },
                BuildAssetBundleOptions.UncompressedAssetBundle, BuildTarget.StandaloneOSX);
            Assert.IsNotNull(manifest);
            var bundle = AssetBundle.LoadFromFile(Path.Combine(directory, "panels")); Assert.IsNotNull(bundle);
            try
            {
                var built = bundle.LoadAllAssets<RoomFloorPanelOutput>().Single();
                Assert.AreEqual(hash, PixelHash(built)); Assert.AreEqual(publications, RoomFloorPanelAssetStore.PublicationCount);
                Assert.IsTrue(built.Chunks.All(c => c.Sprite != null && c.Texture != null));
            }
            finally { bundle.Unload(true); }
        }
    }
}
