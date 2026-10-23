using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SOLITUDE.Editor.RoomAuthoring;
using SOLITUDE.World.RoomAuthoring;
using UnityEditor;
using UnityEngine;

namespace SOLITUDE.Tests.RoomAuthoring
{
    public sealed class ShipLayoutJunctionTests
    {
        private static RoomStyleDefinition.Module Art(RoomAuthoringTestFixtures f, int height, RectInt footprint, Vector2 anchor)
        {
            var module = f.Module(32, height, 32);
            var texture = module.Sprite.texture;
            texture.SetPixels(Enumerable.Repeat(Color.clear, 32 * height).ToArray());
            for (var y = footprint.y; y < footprint.yMax; y++)
                for (var x = footprint.x; x < footprint.xMax; x++) texture.SetPixel(x, y, Color.white);
            texture.Apply();
            module.HasFootprint = true; module.FootprintsPixels = new[] { footprint }; module.AnchorPixels = anchor;
            return module;
        }

        private static void Configure(RoomAuthoringTestFixtures f)
        {
            f.Style.FloorEdgeInsetPixels = 1;
            f.Style.NorthWalls = new[] { Art(f, 64, new RectInt(0, 0, 32, 1), Vector2.zero) };
            f.Style.WestCap = Art(f, 32, new RectInt(0, 0, 1, 32), new Vector2(0.5f, 0));
            f.Style.EastCap = Art(f, 32, new RectInt(0, 0, 1, 32), new Vector2(0.5f, 0));
            f.Style.SouthCap = Art(f, 32, new RectInt(0, 0, 32, 1), new Vector2(0, 0.5f));
            f.Style.Northwest = Art(f, 32, new RectInt(0, 0, 1, 1), new Vector2(1, 0));
            f.Style.Northeast = Art(f, 32, new RectInt(0, 0, 1, 1), Vector2.zero);
            f.Style.Southwest = Art(f, 32, new RectInt(0, 0, 1, 1), Vector2.one);
            f.Style.Southeast = Art(f, 32, new RectInt(0, 0, 1, 1), new Vector2(0, 1));
            f.Style.ConcaveNorthwest = Art(f, 32, new RectInt(0, 0, 1, 1), new Vector2(1, 0));
            f.Style.ConcaveNortheast = Art(f, 32, new RectInt(0, 0, 1, 1), Vector2.zero);
            f.Style.ConcaveSouthwest = Art(f, 32, new RectInt(0, 0, 1, 1), Vector2.one);
            f.Style.ConcaveSoutheast = Art(f, 32, new RectInt(0, 0, 1, 1), new Vector2(0, 1));
            f.Style.JunctionModules = Enum.GetValues(typeof(LayoutJunctionRole)).Cast<LayoutJunctionRole>()
                .Select(role => new RoomStyleDefinition.JunctionModule
                { Role = role, Artwork = Art(f, 32, new RectInt(0, 0, 1, 1), new Vector2(0.5f, 0.5f)) }).ToArray();
        }

        private static ShipLayoutAuthoring Layout(RoomAuthoringTestFixtures f, params Vector2Int[][] regions)
        {
            var layout = RoomAuthoringCreation.CreateLayout(f.Scene, f.Style, LogicalSpaceType.Corridor);
            for (var i = 1; i < regions.Length; i++) RoomAuthoringCreation.CreateSpace(layout);
            for (var i = 0; i < regions.Length; i++)
                foreach (var cell in regions[i]) layout.Spaces[i].Occupancy.SetTile(new Vector3Int(cell.x, cell.y, 0), f.Style.FloorTile);
            return layout;
        }

        private static Vector2Int[][] Regions(string kind) => kind == "bend" ? new[]
        { new[] { Vector2Int.zero }, new[] { Vector2Int.right, Vector2Int.up, Vector2Int.one } } : kind == "tee" ? new[]
        { new[] { Vector2Int.zero, Vector2Int.up }, new[] { Vector2Int.right }, new[] { Vector2Int.one } } : new[]
        { new[] { Vector2Int.zero }, new[] { Vector2Int.right }, new[] { Vector2Int.up }, new[] { Vector2Int.one } };

        private static IEnumerable<RoomStyleDefinition.Module> Modules(RoomStyleDefinition style) => style.NorthWalls.Concat(new[]
        { style.WestCap, style.EastCap, style.SouthCap, style.Northwest, style.Northeast, style.Southwest, style.Southeast,
            style.ConcaveNorthwest, style.ConcaveNortheast, style.ConcaveSouthwest, style.ConcaveSoutheast })
            .Concat(style.JunctionModules.Select(b => b.Artwork));

        private static void AssertClearFloorCores(ShipLayoutAuthoring layout, RoomStyleDefinition style)
        {
            var cells = RoomAuthoringValidator.TopologyOf(layout).Cells;
            foreach (var renderer in layout.GetComponentsInChildren<SpriteRenderer>())
            {
                var module = Modules(style).First(m => m.Sprite == renderer.sprite);
                foreach (var rect in module.FootprintsPixels)
                {
                    var min = (Vector2)renderer.transform.position + ((Vector2)rect.position - module.Sprite.pivot) / 32f;
                    var max = min + (Vector2)rect.size / 32f;
                    foreach (var cell in cells)
                    {
                        var p = (Vector2)layout.transform.position + cell;
                        Assert.IsFalse(min.x < p.x + 31 / 32f - 0.000001f && max.x > p.x + 1 / 32f + 0.000001f &&
                            min.y < p.y + 31 / 32f - 0.000001f && max.y > p.y + 1 / 32f + 0.000001f,
                            renderer.name + " blocks cell " + cell);
                    }
                }
            }
        }

        private static string[] Snapshot(ShipLayoutAuthoring layout) => layout.GetComponentsInChildren<SpriteRenderer>()
            .Select(r => r.name + ":" + JsonUtility.ToJson(r.transform.position) + ":" + r.GetComponentInParent<RoomGeneratedOutput>().Owner.RoomId)
            .OrderBy(s => s, StringComparer.Ordinal).ToArray();

        [TestCase("L")]
        [TestCase("T")]
        [TestCase("four-way")]
        [TestCase("dead-end")]
        [TestCase("width")]
        [TestCase("concave")]
        [TestCase("narrow")]
        public void ConnectedCorridorShapesHaveNoInternalBlockingWallsOrFootprintIntrusion(string shape)
        {
            using var f = new RoomAuthoringTestFixtures(32); Configure(f);
            var cells = shape == "L" ? new[] { Vector2Int.zero, Vector2Int.right, Vector2Int.right * 2, new Vector2Int(2, 1), new Vector2Int(2, 2) } :
                shape == "T" ? new[] { new Vector2Int(0, 1), Vector2Int.one, new Vector2Int(2, 1), Vector2Int.right } :
                shape == "four-way" ? new[] { Vector2Int.one, Vector2Int.up, new Vector2Int(2, 1), Vector2Int.right, new Vector2Int(1, 2) } :
                shape == "dead-end" ? new[] { Vector2Int.zero, Vector2Int.right, Vector2Int.right * 2 } :
                shape == "width" ? RoomAuthoringTestFixtures.Cells(4, 2).Where(c => c.y == 0 || c.x >= 2).ToArray() :
                shape == "concave" ? RoomAuthoringTestFixtures.Cells(3, 3).Where(c => c != new Vector2Int(2, 2)).ToArray() :
                new[] { new Vector2Int(-2, 0), new Vector2Int(-1, 0), new Vector2Int(-1, 1), new Vector2Int(0, 1), new Vector2Int(0, 2) };
            var layout = Layout(f, cells, Array.Empty<Vector2Int>());
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(layout), layout.GenerationResult?.Summary);
            Assert.AreEqual(RoomGenerationStatus.Complete, layout.GenerationResult.Status);
            Assert.AreEqual(cells.Length, RoomAuthoringTestFixtures.FloorCount(layout.Spaces[0]));
            var geometry = LayoutBoundaryResolver.Resolve(RoomAuthoringValidator.TopologyOf(layout));
            Assert.IsFalse(geometry.Edges.Any(e => e.Kind == LayoutEdgeKind.Partition));
            Assert.IsFalse(layout.GetComponentsInChildren<SpriteRenderer>().Any(r => r.name.StartsWith("Partition", StringComparison.Ordinal)));
            Assert.AreEqual(geometry.Edges.Count(e => e.Kind != LayoutEdgeKind.Open), geometry.Spans.Sum(s => s.Length));
            AssertClearFloorCores(layout, f.Style);
        }

        [TestCase("bend", RoomEndpointKind.Bend, LayoutJunctionRole.BendSouthwest, 2)]
        [TestCase("tee", RoomEndpointKind.Tee, LayoutJunctionRole.TeeEast, 3)]
        [TestCase("cross", RoomEndpointKind.Cross, LayoutJunctionRole.Cross, 4)]
        public void MultiOwnerVerticesResolveOneTypedTreatmentAndDeterministicOutput(string kind, RoomEndpointKind expected, LayoutJunctionRole role, int owners)
        {
            using var f = new RoomAuthoringTestFixtures(32); Configure(f);
            var layout = Layout(f, Regions(kind));
            var geometry = LayoutBoundaryResolver.Resolve(RoomAuthoringValidator.TopologyOf(layout));
            var junction = geometry.JunctionAt(Vector2Int.one);
            Assert.AreEqual(expected, junction.Kind); Assert.AreEqual(role, junction.Role);
            Assert.AreEqual(owners, junction.Owners.Count);
            Assert.IsTrue(geometry.Spans.Any(s => s.First.Address.Start == Vector2Int.one && s.StartKind == expected || s.End == Vector2Int.one && s.EndKind == expected));
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(layout), layout.GenerationResult?.Summary);
            var binding = f.Style.JunctionModules.Single(b => b.Role == role);
            var center = layout.GetComponentsInChildren<SpriteRenderer>().Where(r => r.name == role.ToString() &&
                r.transform.position - (Vector3)((binding.Artwork.Sprite.pivot - binding.Artwork.AnchorPixels + binding.Artwork.OffsetPixels) / 32f) == new Vector3(1, 1, 0)).ToArray();
            Assert.AreEqual(1, center.Length);
            var visibleOwners = geometry.Edges.Where(e => (e.Kind == LayoutEdgeKind.Exterior || e.Kind == LayoutEdgeKind.Partition) &&
                (e.Address.Start == Vector2Int.one || e.Address.End == Vector2Int.one))
                .Select(e => e.Kind == LayoutEdgeKind.Partition && e.Address.Horizontal ? e.NegativeOwner : e.VisualOwner)
                .OrderBy(id => id, StringComparer.Ordinal);
            Assert.AreEqual(visibleOwners.First(), center[0].GetComponentInParent<RoomGeneratedOutput>().Owner.RoomId);
            var before = Snapshot(layout);
            var data = new SerializedObject(layout); data.FindProperty("spaces").MoveArrayElement(0, layout.Spaces.Count - 1); data.ApplyModifiedPropertiesWithoutUndo();
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(layout.Spaces[0]));
            CollectionAssert.AreEqual(before, Snapshot(layout));
            AssertClearFloorCores(layout, f.Style);
        }

        [TestCase(0, LayoutJunctionRole.TeeEast)]
        [TestCase(1, LayoutJunctionRole.TeeNorth)]
        [TestCase(2, LayoutJunctionRole.TeeWest)]
        [TestCase(3, LayoutJunctionRole.TeeSouth)]
        public void TeeOrientationFollowsIncidentDirections(int rotations, LayoutJunctionRole expected)
        {
            var regions = Regions("tee");
            for (var n = 0; n < rotations; n++) regions = regions.Select(cells => cells.Select(c => new Vector2Int(1 - c.y, c.x)).ToArray()).ToArray();
            var topology = ShipLayoutTopology.Build(regions.Select((cells, i) => new ShipSpaceCells("owner" + i, cells, Vector2Int.zero)));
            Assert.AreEqual(expected, LayoutBoundaryResolver.Resolve(topology).JunctionAt(Vector2Int.one).Role);
        }

        [Test] public void DiagonalBranchesStaySeparateAndDoNotBecomeACrossing()
        {
            using var f = new RoomAuthoringTestFixtures(32); Configure(f);
            var layout = Layout(f, new[] { Vector2Int.zero }, new[] { Vector2Int.one });
            var geometry = LayoutBoundaryResolver.Resolve(RoomAuthoringValidator.TopologyOf(layout));
            var junction = geometry.JunctionAt(Vector2Int.one);
            Assert.AreEqual(RoomEndpointKind.Diagonal, junction.Kind); Assert.IsNull(junction.Role);
            Assert.AreEqual(2, geometry.Exterior.Junctions.Count(j => j.Position == Vector2Int.one));
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(layout), layout.GenerationResult?.Summary);
            Assert.IsFalse(layout.GetComponentsInChildren<SpriteRenderer>().Any(r => r.name == "Cross"));
            Assert.AreEqual(1, RoomAuthoringTestFixtures.FloorCount(layout.Spaces[0]));
            Assert.AreEqual(1, RoomAuthoringTestFixtures.FloorCount(layout.Spaces[1]));
            AssertClearFloorCores(layout, f.Style);
        }

        [Test] public void MultiOwnerCrossRetainsItsConcaveExteriorCornerClassification()
        {
            using var f = new RoomAuthoringTestFixtures(32); Configure(f);
            var layout = Layout(f, new[] { Vector2Int.zero }, new[] { Vector2Int.right }, new[] { Vector2Int.up });
            var junction = LayoutBoundaryResolver.Resolve(RoomAuthoringValidator.TopologyOf(layout)).JunctionAt(Vector2Int.one);
            Assert.AreEqual(RoomEndpointKind.Cross, junction.Kind);
            Assert.AreEqual(RoomJoinKind.Concave, junction.ExteriorCorners.Single().Kind);
            Assert.AreEqual(RoomCorner.Northeast, junction.ExteriorCorners.Single().Corner);
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(layout), layout.GenerationResult?.Summary);
            AssertClearFloorCores(layout, f.Style);
        }

        [TestCase("sprite")]
        [TestCase("footprint")]
        [TestCase("duplicate")]
        public void LocalJunctionFaultRetainsIndependentOutputAndCorrectionRestoresIt(string fault)
        {
            using var f = new RoomAuthoringTestFixtures(32); Configure(f);
            var layout = Layout(f, Regions("tee"));
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(layout)); var before = Snapshot(layout);
            var binding = f.Style.JunctionModules.Single(b => b.Role == LayoutJunctionRole.TeeEast);
            var sprite = binding.Artwork.Sprite;
            if (fault == "sprite") binding.Artwork.Sprite = null;
            else if (fault == "footprint") binding.Artwork.AnchorPixels = new Vector2(-8, -8);
            else f.Style.JunctionModules = f.Style.JunctionModules.Concat(new[] { binding }).ToArray();
            Assert.IsFalse(RoomAuthoringValidator.IsCurrent(layout));
            Assert.IsFalse(RoomAuthoringOperations.Rebuild(layout));
            Assert.AreEqual(RoomGenerationStatus.IncompletePreview, layout.GenerationResult.Status);
            Assert.AreEqual(4, layout.Spaces.Sum(RoomAuthoringTestFixtures.FloorCount));
            Assert.IsTrue(layout.GenerationResult.Issues.Any(i => i.Kind == RoomIssueKind.Join && i.Start == Vector2Int.one && i.HasRegion));
            Assert.IsTrue(layout.GetComponentsInChildren<SpriteRenderer>().Any(r => r.name.StartsWith("Partition", StringComparison.Ordinal)));
            Assert.IsFalse(layout.GetComponentsInChildren<SpriteRenderer>().Any(r => r.name == "TeeEast"));
            binding.Artwork.Sprite = sprite; binding.Artwork.AnchorPixels = new Vector2(0.5f, 0.5f);
            f.Style.JunctionModules = f.Style.JunctionModules.Distinct().ToArray();
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(layout)); CollectionAssert.AreEqual(before, Snapshot(layout));
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(layout)); CollectionAssert.AreEqual(before, Snapshot(layout));
        }

        [TestCase("missing")]
        [TestCase("coverage")]
        public void UnresolvedEndpointWithholdsItsReservedDependentSpan(string fault)
        {
            using var f = new RoomAuthoringTestFixtures(32); Configure(f);
            var layout = Layout(f, Regions("tee"));
            var binding = f.Style.JunctionModules.Single(b => b.Role == LayoutJunctionRole.TeeEast);
            binding.HorizontalReservationsPixels = new Vector2Int(0, 32);
            if (fault == "missing") binding.Artwork.Sprite = null;
            Assert.IsFalse(RoomAuthoringOperations.Rebuild(layout));
            Assert.AreEqual(4, layout.Spaces.Sum(RoomAuthoringTestFixtures.FloorCount));
            Assert.IsTrue(layout.GenerationResult.Issues.Any(i => i.Kind == RoomIssueKind.Span && i.Start == Vector2Int.one && i.End == new Vector2Int(2, 1)));
            Assert.IsTrue(layout.GenerationResult.Issues.Any(i => i.Kind == RoomIssueKind.Join && i.Start == Vector2Int.one));
            Assert.IsTrue(layout.GetComponentsInChildren<SpriteRenderer>().Length > 0);
        }

        [Test] public void JunctionCanSupplyOneExactReservedEdgeWithoutADuplicateWallPanel()
        {
            using var f = new RoomAuthoringTestFixtures(32); Configure(f);
            var layout = Layout(f, Regions("tee"));
            var binding = f.Style.JunctionModules.Single(b => b.Role == LayoutJunctionRole.TeeEast);
            binding.Artwork = Art(f, 32, new RectInt(0, 0, 32, 1), new Vector2(0, 0.5f));
            binding.HorizontalReservationsPixels = new Vector2Int(0, 32);
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(layout), layout.GenerationResult?.Summary);
            Assert.IsFalse(layout.GetComponentsInChildren<SpriteRenderer>().Any(r => r.name == "PartitionNorth" &&
                r.transform.position == new Vector3(1, 1 - 0.5f / 32, 0)));
            Assert.AreEqual(1, layout.GetComponentsInChildren<SpriteRenderer>().Count(r => r.name == "TeeEast"));
            AssertClearFloorCores(layout, f.Style);
            f.Style.Southwest.Sprite = null;
            Assert.IsFalse(RoomAuthoringOperations.Rebuild(layout));
            Assert.IsFalse(layout.GenerationResult.Issues.Any(i => i.Kind == RoomIssueKind.Span && i.Start == Vector2Int.one && i.End == new Vector2Int(2, 1)));
            Assert.AreEqual(1, layout.GetComponentsInChildren<SpriteRenderer>().Count(r => r.name == "TeeEast"));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void NorthPanelFitIsCheckedAgainstTheOtherSpacesFloor(bool declaredClearance)
        {
            using var f = new RoomAuthoringTestFixtures(32); Configure(f);
            var layout = Layout(f, new[] { Vector2Int.zero }, new[] { Vector2Int.up * 2 });
            if (declaredClearance) f.Style.NorthWallClearanceCells = 2;
            else f.Style.NorthWalls = new[] { Art(f, 64, new RectInt(0, 0, 32, 64), Vector2.zero) };
            Assert.IsFalse(RoomAuthoringOperations.Rebuild(layout));
            Assert.AreEqual(2, layout.Spaces.Sum(RoomAuthoringTestFixtures.FloorCount));
            Assert.AreEqual(1, layout.GetComponentsInChildren<SpriteRenderer>().Count(r => r.name == "North"));
            Assert.IsTrue(layout.GenerationResult.Issues.Any(i => i.Kind == RoomIssueKind.VisualFit && i.Start == Vector2Int.up));
        }

        [Test] public void JunctionSpriteImportChangesMakeStoredOutputStale()
        {
            using var f = new RoomAuthoringTestFixtures(32); Configure(f);
            var layout = Layout(f, Regions("tee")); Assert.IsTrue(RoomAuthoringOperations.Rebuild(layout));
            var binding = f.Style.JunctionModules.Single(b => b.Role == LayoutJunctionRole.TeeEast);
            binding.Artwork.Sprite.texture.filterMode = FilterMode.Bilinear;
            Assert.IsFalse(RoomAuthoringValidator.IsCurrent(layout));
            Assert.IsFalse(RoomAuthoringOperations.Rebuild(layout));
            Assert.AreEqual(RoomGenerationStatus.IncompletePreview, layout.GenerationResult.Status);
        }

        [Test] public void AmbiguousOwnershipRefusesWhileMissingArtRemainsAPartialPreview()
        {
            using var f = new RoomAuthoringTestFixtures(32); Configure(f);
            var layout = Layout(f, Regions("tee")); Assert.IsTrue(RoomAuthoringOperations.Rebuild(layout));
            var before = layout.GetComponentsInChildren<Transform>(true);
            layout.Spaces[1].Occupancy.SetTile(Vector3Int.zero, f.Style.FloorTile);
            Assert.IsFalse(RoomAuthoringOperations.Rebuild(layout));
            Assert.AreEqual(RoomGenerationStatus.Refused, layout.GenerationResult.Status);
            CollectionAssert.AreEqual(before, layout.GetComponentsInChildren<Transform>(true));
        }
    }
}
