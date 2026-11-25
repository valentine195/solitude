using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using SOLITUDE.Editor.RoomAuthoring;
using UnityEngine;
using UnityEngine.Tilemaps;
using SOLITUDE.World.RoomAuthoring;

namespace SOLITUDE.Tests
{
    public sealed class PerformanceScalingTests
    {
        private static void Measure(string name, Action work)
        {
            work(); // Compile/JIT and warm native paths before recording three repeats.
            var samples = new double[3];
            for (int i = 0; i < samples.Length; i++)
            {
                GC.Collect(); var timer = Stopwatch.StartNew(); work(); timer.Stop(); samples[i] = timer.Elapsed.TotalMilliseconds;
            }
            TestContext.Out.WriteLine($"PERF {name} ms: {string.Join(", ", samples.Select(s => s.ToString("F3")))}");
        }
        [TestCase(16)] [TestCase(32)] [TestCase(64)]
        public void ValidateAllRooms(int count)
        {
            using var fixture = new RoomAuthoring.RoomAuthoringTestFixtures(32);
            var rooms = new ShipRoomAuthoring[count];
            for (int i = 0; i < count; i++)
            {
                rooms[i] = fixture.Room(16, 12);
                rooms[i].transform.position = new Vector3(i % 8 * 20, i / 8 * 16, 0);
            }
            Measure("validateAll" + count, () => { foreach (var room in rooms) Assert.IsNull(RoomAuthoringValidator.Validate(room)); });
        }
        [Test] public void ResizeLargeRoomTenTimes()
        {
            using var fixture = new RoomAuthoring.RoomAuthoringTestFixtures(32);
            var room = fixture.Room(64, 32); Assert.IsTrue(RoomAuthoringOperations.Rebuild(room));
            var before = new HashSet<Transform>(room.GeneratedOutput.GetComponentsInChildren<Transform>());
            void Resize(bool expand)
            {
                var tiles = new TileBase[32]; if (expand) for (int i = 0; i < tiles.Length; i++) tiles[i] = fixture.Style.FloorTile;
                room.Occupancy.SetTilesBlock(new BoundsInt(64, 0, 0, 1, 32, 1), tiles);
                Assert.IsTrue(RoomAuthoringOperations.Rebuild(room), room.GenerationDiagnostic);
            }
            Resize(true);
            TestContext.Out.WriteLine("PERF retained objects after expansion: " + room.GeneratedOutput.GetComponentsInChildren<Transform>().Count(before.Contains));
            Resize(false);
            Measure("resize64x32tenTimes", () => { for (int i = 0; i < 10; i++) Resize(i % 2 == 0); });
        }
    }
}
