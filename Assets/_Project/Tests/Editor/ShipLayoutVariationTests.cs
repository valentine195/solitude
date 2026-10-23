using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SOLITUDE.Editor.RoomAuthoring;
using SOLITUDE.World.RoomAuthoring;
using UnityEditor;
using UnityEngine;

namespace SOLITUDE.Tests.RoomAuthoring
{
    public sealed class ShipLayoutVariationTests
    {
        private static void Variants(RoomAuthoringTestFixtures f)
        {
            ShipLayoutStyleTests.Configure(f);
            var modules = Enumerable.Range(0, 5).Select(i =>
            {
                var m = f.Module(32, 64, 32);
                var pixels = new Color[32 * 64];
                for (var x = 0; x < 32; x++) pixels[x] = new Color((i + 1) / 5f, .2f, .3f);
                m.Sprite.texture.SetPixels(pixels); m.Sprite.texture.Apply();
                m.HasFootprint = true; m.FootprintsPixels = new[] { new RectInt(0, 0, 32, 1) };
                return m;
            }).ToArray();
            f.Style.NorthWalls = modules;
        }

        private static Dictionary<Vector3, Sprite> North(ShipLayoutAuthoring layout, ShipRoomAuthoring owner) =>
            owner.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>().Where(r => r.name == "North")
                .ToDictionary(r => r.transform.position - layout.transform.position, r => r.sprite);

        [Test] public void ExtendingRunAtItsBeginningKeepsVariantsAtEveryUnaffectedAddress()
        {
            using var f = new RoomAuthoringTestFixtures(32); Variants(f);
            var layout = RoomAuthoringCreation.CreateLayout(f.Scene, f.Style);
            var room = layout.Spaces[0]; RoomAuthoringCreation.CreateSpace(layout);
            ShipLayoutStyleTests.Fill(room, RoomAuthoringTestFixtures.Cells(24, 4));
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(layout), layout.GenerationResult?.Summary);
            var original = North(layout, room); Assert.AreEqual(24, original.Count); Assert.Greater(original.Values.Distinct().Count(), 1);
            foreach (var c in RoomAuthoringTestFixtures.Cells(3, 4))
                room.Occupancy.SetTile(new Vector3Int(c.x - 3, c.y, 0), f.Style.FloorTile);
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(layout), layout.GenerationResult.Summary);
            var extended = North(layout, room); Assert.AreEqual(27, extended.Count);
            foreach (var pair in original) Assert.AreSame(pair.Value, extended[pair.Key], "Existing address changed: " + pair.Key);
            var before = ShipLayoutStyleTests.Snapshot(layout);
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(room)); CollectionAssert.AreEqual(before, ShipLayoutStyleTests.Snapshot(layout));
            var topology = RoomAuthoringValidator.TopologyOf(layout);
            var sources = topology.Cells.Reverse().ToArray();
            var forward = LayoutBoundaryResolver.Resolve(ShipLayoutTopology.Build(new[] { new ShipSpaceCells(room.RoomId, sources, Vector2Int.zero) }));
            var reverse = LayoutBoundaryResolver.Resolve(ShipLayoutTopology.Build(new[] { new ShipSpaceCells(room.RoomId, sources.Reverse().ToArray(), Vector2Int.zero) }));
            var styles = new Dictionary<string, RoomStyleDefinition> { [room.RoomId] = f.Style };
            var a = RoomPlacementBuilder.BuildLayout(forward, styles); var b = RoomPlacementBuilder.BuildLayout(reverse, styles);
            CollectionAssert.AreEqual(a.Sprites.Select(p => p.Sprite), b.Sprites.Select(p => p.Sprite));
            CollectionAssert.AreEqual(a.Sprites.Select(p => p.Position), b.Sprites.Select(p => p.Position));
        }

        [Test] public void UnrelatedSpaceExtensionAndMemberOrderDoNotChangeExistingVariants()
        {
            using var f = new RoomAuthoringTestFixtures(32); Variants(f);
            var layout = RoomAuthoringCreation.CreateLayout(f.Scene, f.Style);
            var first = layout.Spaces[0]; var other = RoomAuthoringCreation.CreateSpace(layout);
            other.transform.localPosition = Vector3.right * 40;
            ShipLayoutStyleTests.Fill(first, RoomAuthoringTestFixtures.Cells(24, 4)); ShipLayoutStyleTests.Fill(other, RoomAuthoringTestFixtures.Cells(2, 2));
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(layout), layout.GenerationResult?.Summary);
            var original = North(layout, first);
            ShipLayoutStyleTests.Fill(other, RoomAuthoringTestFixtures.Cells(5, 2));
            var data = new SerializedObject(layout); data.FindProperty("spaces").MoveArrayElement(0, 1); data.ApplyModifiedPropertiesWithoutUndo();
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(other), layout.GenerationResult.Summary);
            CollectionAssert.AreEquivalent(original, North(layout, first));
        }

        private static RoomStyleDefinition PersistentVariants(string folder)
        {
            var style = AssetDatabase.LoadAssetAtPath<RoomStyleDefinition>(folder + "/Style.asset");
            style.NorthWalls = Enumerable.Range(0, 5).Select(i =>
            {
                var texture = new Texture2D(32, 64);
                var pixels = new Color[32 * 64];
                for (var x = 0; x < 32; x++) pixels[x] = new Color((i + 1) / 5f, .2f, .3f);
                texture.SetPixels(pixels); texture.Apply();
                var path = folder + "/Variant" + i + ".png";
                System.IO.File.WriteAllBytes(path, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 32; importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Custom; settings.spritePivot = Vector2.zero;
                importer.SetTextureSettings(settings); importer.SaveAndReimport();
                return new RoomStyleDefinition.Module { Sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path),
                    CanvasPixels = new Vector2Int(32, 64), RepeatPixels = 32, HasFootprint = true,
                    FootprintsPixels = new[] { new RectInt(0, 0, 32, 1) } };
            }).ToArray();
            style.StructuralSeed = 19; EditorUtility.SetDirty(style); AssetDatabase.SaveAssets();
            return style;
        }

        [Test] public void SeedUndoAndRestoredStyleSerializationReproduceTheSameVariants()
        {
            Assert.IsTrue(System.Environment.GetCommandLineArgs().Contains("-solitudeRoomAuthoringVerification"));
            var folder = ShipLayoutDiagnosticTests.CreateSharedAssets();
            try
            {
                using var f = new RoomAuthoringTestFixtures(32);
                var style = PersistentVariants(folder);
                Assert.IsTrue(EditorUtility.IsPersistent(style.FloorTile));
                Assert.IsTrue(style.NorthWalls.All(m => EditorUtility.IsPersistent(m.Sprite)));
                var layout = RoomAuthoringCreation.CreateLayout(f.Scene, style);
                var room = layout.Spaces[0]; RoomAuthoringCreation.CreateSpace(layout);
                ShipLayoutStyleTests.Fill(room, RoomAuthoringTestFixtures.Cells(32, 4));
                Assert.IsTrue(RoomAuthoringOperations.Rebuild(layout), layout.GenerationResult?.Summary);
                var original = North(layout, room); var serialized = EditorJsonUtility.ToJson(style);
                var floor = style.FloorTile; var sprites = style.NorthWalls.Select(m => m.Sprite).ToArray();
                Undo.IncrementCurrentGroup(); Undo.RecordObject(style, "Change structural seed"); style.StructuralSeed = 23; Undo.FlushUndoRecordObjects();
                Undo.IncrementCurrentGroup();
                RoomAuthoringValidator.AssessSpaces(layout, out var changed);
                Assert.IsTrue(changed[room].Sprites.Where(p => p.Role == "North").Any(p => original[p.Position] != p.Sprite));
                Undo.PerformUndo(); Assert.AreEqual(19, style.StructuralSeed);
                Assert.IsTrue(RoomAuthoringOperations.Rebuild(layout), layout.GenerationResult.Summary);
                CollectionAssert.AreEquivalent(original, North(layout, room));
                style.StructuralSeed = 23;
                EditorJsonUtility.FromJsonOverwrite(serialized, style);
                Assert.AreEqual(19, style.StructuralSeed); Assert.AreSame(floor, style.FloorTile);
                CollectionAssert.AreEqual(sprites, style.NorthWalls.Select(m => m.Sprite));
                Assert.IsTrue(RoomAuthoringOperations.Rebuild(layout), layout.GenerationResult.Summary);
                CollectionAssert.AreEquivalent(original, North(layout, room));
                Assert.AreEqual(128, RoomAuthoringTestFixtures.FloorCount(room));
            }
            finally { AssetDatabase.DeleteAsset(folder); }
        }
    }
}
