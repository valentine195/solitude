using System;
using System.Linq;
using NUnit.Framework;
using SOLITUDE.Editor.RoomAuthoring;
using SOLITUDE.World.RoomAuthoring;
using UnityEditor;
using UnityEngine;

namespace SOLITUDE.Tests.RoomAuthoring
{
    public sealed class ShipLayoutStyleTests
    {
        internal static void Configure(RoomAuthoringTestFixtures f)
        {
            RoomStyleDefinition.Module Art(int width, int height, RectInt ink, Vector2 anchor)
            {
                var m = f.Module(width, height, width);
                var pixels = new Color[width * height];
                for (var y = ink.y; y < ink.yMax; y++) for (var x = ink.x; x < ink.xMax; x++) pixels[y * width + x] = Color.white;
                m.Sprite.texture.SetPixels(pixels); m.Sprite.texture.Apply();
                m.HasFootprint = true; m.FootprintsPixels = new[] { ink }; m.AnchorPixels = anchor;
                return m;
            }
            var s = f.Style; s.FloorEdgeInsetPixels = 1;
            s.NorthWalls = new[] { Art(32, 64, new RectInt(0, 0, 32, 1), Vector2.zero) };
            s.WestCap = Art(32, 32, new RectInt(0, 0, 1, 32), new Vector2(.5f, 0));
            s.EastCap = Art(32, 32, new RectInt(0, 0, 1, 32), new Vector2(.5f, 0));
            s.SouthCap = Art(32, 32, new RectInt(0, 0, 32, 1), new Vector2(0, .5f));
            s.Northwest = Art(32, 32, new RectInt(0, 0, 1, 1), new Vector2(1, 0));
            s.Northeast = Art(32, 32, new RectInt(0, 0, 1, 1), Vector2.zero);
            s.Southwest = Art(32, 32, new RectInt(0, 0, 1, 1), Vector2.one);
            s.Southeast = Art(32, 32, new RectInt(0, 0, 1, 1), new Vector2(0, 1));
            s.ConcaveNorthwest = Art(32, 32, new RectInt(0, 0, 1, 1), new Vector2(1, 0));
            s.ConcaveNortheast = Art(32, 32, new RectInt(0, 0, 1, 1), Vector2.zero);
            s.ConcaveSouthwest = Art(32, 32, new RectInt(0, 0, 1, 1), Vector2.one);
            s.ConcaveSoutheast = Art(32, 32, new RectInt(0, 0, 1, 1), new Vector2(0, 1));
            s.JunctionModules = Enum.GetValues(typeof(LayoutJunctionRole)).Cast<LayoutJunctionRole>().Select(role =>
                new RoomStyleDefinition.JunctionModule { Role = role, Artwork = Art(32, 32, new RectInt(0, 0, 1, 1), new Vector2(.5f, .5f)) }).ToArray();
        }

        internal static void Identity(ShipRoomAuthoring room, string id)
        {
            var data = new SerializedObject(room); data.FindProperty("roomId").stringValue = id; data.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void Fill(ShipRoomAuthoring room, Vector2Int[] cells)
        {
            foreach (var c in cells) room.Occupancy.SetTile(new Vector3Int(c.x, c.y, 0), room.Style.FloorTile);
        }

        private static ShipLayoutAuthoring Pair(RoomAuthoringTestFixtures a, RoomAuthoringTestFixtures b, bool horizontal)
        {
            Configure(a); Configure(b);
            a.Style.CompatibleTransitions = new[] { b.Style }; b.Style.CompatibleTransitions = new[] { a.Style };
            var layout = RoomAuthoringCreation.CreateLayout(a.Scene, a.Style);
            var second = RoomAuthoringCreation.CreateSpace(layout);
            var first = layout.Spaces[0]; first.Style = a.Style; second.Style = b.Style; layout.DefaultStyle = null;
            Identity(first, "z-south-or-west"); Identity(second, "a-north-or-east");
            second.transform.localPosition = horizontal ? Vector3.up * 4 : Vector3.right * 8;
            Fill(first, RoomAuthoringTestFixtures.Cells(8, 4)); Fill(second, RoomAuthoringTestFixtures.Cells(8, 4));
            return layout;
        }

        internal static string[] Snapshot(ShipLayoutAuthoring layout) => layout.GetComponentsInChildren<SpriteRenderer>()
            .Select(r => r.name + ":" + JsonUtility.ToJson(r.transform.position) + ":" + r.sprite.GetEntityId() + ":" +
                r.GetComponentInParent<RoomGeneratedOutput>().Owner.RoomId).OrderBy(s => s, StringComparer.Ordinal).ToArray();

        [TestCase(true)] [TestCase(false)]
        public void TwoFamiliesChooseVisibleNorthFaceOrStableCapAndPreserveOpeningsAcrossEditOrder(bool horizontal)
        {
            using var a = new RoomAuthoringTestFixtures(32); using var b = new RoomAuthoringTestFixtures(32);
            var layout = Pair(a, b, horizontal); var first = layout.Spaces[0]; var second = layout.Spaces[1];
            var connection = new LayoutConnectionDefinition("passage", first.RoomId, second.RoomId,
                horizontal ? new Vector2(2, 4) : new Vector2(8, 1), horizontal, 2, LayoutConnectionType.OpenPassage);
            layout.SetConnections(new[] { connection });
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(layout), layout.GenerationResult?.Summary);
            var role = horizontal ? "PartitionNorth" : "PartitionEast";
            var pieces = layout.GetComponentsInChildren<SpriteRenderer>().Where(r => r.name == role).ToArray();
            Assert.AreEqual(horizontal ? 6 : 2, pieces.Length);
            var owner = horizontal ? first : second;
            var sprite = horizontal ? a.Style.NorthWalls[0].Sprite : b.Style.EastCap.Sprite;
            Assert.IsTrue(pieces.All(p => p.sprite == sprite && p.GetComponentInParent<RoomGeneratedOutput>().Owner == owner));
            Assert.IsFalse(layout.GetComponentsInChildren<SpriteRenderer>().Any(r => r.name == "PartitionSouth"));
            var geometry = LayoutBoundaryResolver.Resolve(RoomAuthoringValidator.TopologyOf(layout), layout.Connections);
            Assert.AreEqual(2, geometry.Edges.Count(e => e.Kind == LayoutEdgeKind.Opening));
            var before = Snapshot(layout); var assessment = JsonUtility.ToJson(layout.GenerationResult);
            var data = new SerializedObject(layout); data.FindProperty("spaces").MoveArrayElement(0, 1); data.ApplyModifiedPropertiesWithoutUndo();
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(second)); CollectionAssert.AreEqual(before, Snapshot(layout));
            Assert.AreEqual(assessment, JsonUtility.ToJson(layout.GenerationResult));
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(first)); CollectionAssert.AreEqual(before, Snapshot(layout));
            CollectionAssert.AreEqual(geometry.Edges, LayoutBoundaryResolver.Resolve(RoomAuthoringValidator.TopologyOf(layout), layout.Connections).Edges);
            Assert.AreEqual(64, layout.Spaces.Sum(RoomAuthoringTestFixtures.FloorCount));
            Assert.IsTrue(RoomAuthoringValidator.IsCurrent(layout));
        }

        [Test] public void MissingMixedTransitionIsLocatedAndCorrectionPreservesCellsAndIndependentArt()
        {
            using var a = new RoomAuthoringTestFixtures(32); using var b = new RoomAuthoringTestFixtures(32);
            var layout = Pair(a, b, false); a.Style.CompatibleTransitions = Array.Empty<RoomStyleDefinition>(); b.Style.CompatibleTransitions = Array.Empty<RoomStyleDefinition>();
            var cells = layout.Spaces.SelectMany(RoomAuthoringOperations.ReadCells).ToArray();
            Assert.IsFalse(RoomAuthoringOperations.Rebuild(layout));
            Assert.AreEqual(RoomGenerationStatus.IncompletePreview, layout.GenerationResult.Status);
            Assert.IsTrue(layout.GenerationResult.Issues.Any(i => i.Kind == RoomIssueKind.Join && i.HasRegion && i.Reason.Contains("compatible transition")));
            Assert.AreEqual(64, layout.Spaces.Sum(RoomAuthoringTestFixtures.FloorCount));
            Assert.IsTrue(layout.GetComponentsInChildren<SpriteRenderer>().Any(r => r.name == "North"));
            a.Style.CompatibleTransitions = new[] { b.Style }; b.Style.CompatibleTransitions = new[] { a.Style };
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(layout), layout.GenerationResult.Summary);
            CollectionAssert.AreEqual(cells, layout.Spaces.SelectMany(RoomAuthoringOperations.ReadCells));
            var before = Snapshot(layout); Assert.IsTrue(RoomAuthoringOperations.Rebuild(layout)); CollectionAssert.AreEqual(before, Snapshot(layout));
        }

        [Test] public void ExplicitCompatibleFallbackIsInformationalAndItsSpriteSettingsAffectCurrentness()
        {
            using var a = new RoomAuthoringTestFixtures(32); using var fallback = new RoomAuthoringTestFixtures(32);
            var layout = Pair(a, fallback, false);
            foreach (var room in layout.Spaces) room.Style = a.Style;
            layout.DefaultStyle = fallback.Style; fallback.Style.CompatibleTransitions = new[] { a.Style };
            a.Style.EastCap.Sprite = null;
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(layout), layout.GenerationResult?.Summary);
            Assert.AreEqual(RoomGenerationStatus.Complete, layout.GenerationResult.Status); Assert.IsEmpty(layout.GenerationResult.Issues);
            Assert.IsTrue(layout.GenerationResult.StyleResolutions.Any(r => r.UsedFallback && r.Role == "PartitionEast" && r.StyleId == fallback.Style.StyleId));
            Assert.IsTrue(layout.GetComponentsInChildren<SpriteRenderer>().Where(r => r.name == "PartitionEast").All(r => r.sprite == fallback.Style.EastCap.Sprite));
            Assert.IsTrue(RoomAuthoringValidator.IsCurrent(layout));
            fallback.Style.EastCap.Sprite.texture.filterMode = FilterMode.Bilinear;
            Assert.IsFalse(RoomAuthoringValidator.IsCurrent(layout));
            Assert.IsFalse(RoomAuthoringOperations.Rebuild(layout)); Assert.AreEqual(64, layout.Spaces.Sum(RoomAuthoringTestFixtures.FloorCount));
            fallback.Style.EastCap.Sprite.texture.filterMode = FilterMode.Point;
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(layout), layout.GenerationResult.Summary);
            fallback.Style.CompatibleTransitions = Array.Empty<RoomStyleDefinition>();
            Assert.IsFalse(RoomAuthoringOperations.Rebuild(layout));
            Assert.IsTrue(layout.GenerationResult.Issues.Any(i => i.HasRegion && i.Reason.Contains("fallback unavailable")));
            Assert.IsFalse(layout.GetComponentsInChildren<SpriteRenderer>().Any(r => r.name == "PartitionEast"));
        }

        [Test] public void ThreeOwnerTeeChoosesOnlyVisibleIncidentOwnersEvenWhenHiddenOwnerSortsFirst()
        {
            using var a = new RoomAuthoringTestFixtures(32); using var b = new RoomAuthoringTestFixtures(32);
            Configure(a); Configure(b); a.Style.CompatibleTransitions = new[] { b.Style }; b.Style.CompatibleTransitions = new[] { a.Style };
            var layout = RoomAuthoringCreation.CreateLayout(a.Scene, a.Style);
            var west = layout.Spaces[0]; Identity(west, "z"); Fill(west, new[] { Vector2Int.zero });
            var east = RoomAuthoringCreation.CreateSpace(layout); Identity(east, "y"); east.Style = b.Style; Fill(east, new[] { Vector2Int.right });
            var hidden = RoomAuthoringCreation.CreateSpace(layout); Identity(hidden, "a"); Fill(hidden, new[] { Vector2Int.up, Vector2Int.one });
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(layout), layout.GenerationResult?.Summary);
            var center = layout.GetComponentsInChildren<SpriteRenderer>().Single(r => r.name == "TeeSouth");
            Assert.AreEqual(east, center.GetComponentInParent<RoomGeneratedOutput>().Owner);
            Assert.AreEqual(b.Style.JunctionModules.Single(m => m.Role == LayoutJunctionRole.TeeSouth).Artwork.Sprite, center.sprite);
            var before = Snapshot(layout);
            var data = new SerializedObject(layout); data.FindProperty("spaces").MoveArrayElement(2, 0); data.ApplyModifiedPropertiesWithoutUndo();
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(hidden)); CollectionAssert.AreEqual(before, Snapshot(layout));
        }

        [Test] public void MixedConcaveThreeOwnerVertexUsesOneOrientedCrossTreatment()
        {
            using var a = new RoomAuthoringTestFixtures(32); using var b = new RoomAuthoringTestFixtures(32);
            Configure(a); Configure(b); a.Style.CompatibleTransitions = new[] { b.Style }; b.Style.CompatibleTransitions = new[] { a.Style };
            var layout = RoomAuthoringCreation.CreateLayout(a.Scene, a.Style);
            Fill(layout.Spaces[0], new[] { Vector2Int.zero });
            var east = RoomAuthoringCreation.CreateSpace(layout); east.Style = b.Style; Fill(east, new[] { Vector2Int.right });
            var north = RoomAuthoringCreation.CreateSpace(layout); Fill(north, new[] { Vector2Int.up });
            var junction = LayoutBoundaryResolver.Resolve(RoomAuthoringValidator.TopologyOf(layout)).JunctionAt(Vector2Int.one);
            Assert.AreEqual(RoomCorner.Northeast, junction.ExteriorCorners.Single().Corner);
            Assert.AreEqual(RoomJoinKind.Concave, junction.ExteriorCorners.Single().Kind);
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(layout), layout.GenerationResult?.Summary);
            Assert.AreEqual(1, layout.GetComponentsInChildren<SpriteRenderer>().Count(r => r.name == "Cross"));
            Assert.AreEqual(3, layout.Spaces.Sum(RoomAuthoringTestFixtures.FloorCount));
        }

        [Test] public void TwoCellNorthPartitionsCoverExactRunsAndKeepWidePassageClear()
        {
            using var a = new RoomAuthoringTestFixtures(32); using var b = new RoomAuthoringTestFixtures(32);
            var layout = Pair(a, b, true); var first = layout.Spaces[0]; var second = layout.Spaces[1];
            var large = a.Module(128, 256, 128);
            var native = Sprite.Create(large.Sprite.texture, new Rect(0, 0, 128, 256), Vector2.zero, 64);
            try
            {
                large.Sprite = native; large.PixelsPerUnit = 64; large.HasFootprint = true;
                large.FootprintsPixels = new[] { new RectInt(0, 0, 128, 1) }; a.Style.NorthWalls = new[] { large };
                var pixels = new Color[128 * 256]; for (var x = 0; x < 128; x++) pixels[x] = Color.white;
                native.texture.SetPixels(pixels); native.texture.Apply();
                layout.SetConnections(new[] { new LayoutConnectionDefinition("wide", first.RoomId, second.RoomId, new Vector2(2, 4), true, 2, LayoutConnectionType.OpenPassage) });
                Assert.IsTrue(RoomAuthoringOperations.Rebuild(layout), layout.GenerationResult?.Summary);
                var walls = layout.GetComponentsInChildren<SpriteRenderer>().Where(r => r.name == "PartitionNorth").ToArray();
                Assert.AreEqual(3, walls.Length); Assert.IsTrue(walls.All(r => r.sprite == native));
                CollectionAssert.AreEquivalent(new[] { new Vector3(0, 4), new Vector3(4, 4), new Vector3(6, 4) }, walls.Select(r => r.transform.position));
                large.OffsetPixels = Vector2.right * 64;
                Assert.IsFalse(RoomAuthoringOperations.Rebuild(layout));
                Assert.IsTrue(layout.GenerationResult.Issues.Any(i => i.Reason.Contains("intrudes into connection wide")));
                Assert.IsFalse(layout.GetComponentsInChildren<SpriteRenderer>().Any(r => r.name == "PartitionNorth" && r.transform.position.x < 4 && r.transform.position.x + 2 > 2));
                Assert.AreEqual(64, layout.Spaces.Sum(RoomAuthoringTestFixtures.FloorCount));
                Assert.AreEqual(2, LayoutBoundaryResolver.Resolve(RoomAuthoringValidator.TopologyOf(layout), layout.Connections).Edges.Count(e => e.Kind == LayoutEdgeKind.Opening));
            }
            finally { UnityEngine.Object.DestroyImmediate(native); }
        }
    }
}
