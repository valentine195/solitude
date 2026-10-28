using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using NUnit.Framework;
using SOLITUDE.Application.Persistence;
using SOLITUDE.Core.Input;
using SOLITUDE.Editor.RoomAuthoring;
using SOLITUDE.Items;
using SOLITUDE.Player.Vitals;
using SOLITUDE.SaveLoad;
using SOLITUDE.World.RoomAuthoring;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace SOLITUDE.Tests
{
    // Repeatable synthetic workloads; timings are diagnostic, never flaky pass thresholds.
    public sealed class PerformanceOptimizationTests
    {
        private static void Measure(string name, Action workload)
        {
            GC.Collect();
            var clock = Stopwatch.StartNew();
            workload(); clock.Stop();
            TestContext.Out.WriteLine($"PERF {name}: {clock.Elapsed.TotalMilliseconds:F3} ms, allocation counter unavailable in this Unity Mono runtime");
        }
        [Test] public void RoomStroke64Cells()
        {
            using var f = new RoomAuthoring.RoomAuthoringTestFixtures(32);
            var room = f.Room(0, 0); var brush = ScriptableObject.CreateInstance<ShipRoomBrush>();
            brush.OccupancyMarker = f.Style.FloorTile;
            int generations = 0; Action<ShipRoomAuthoring> count = _ => generations++;
            RoomAuthoringOperations.GenerationCompleted += count;
            try
            {
                var grid = room.GetComponent<Grid>();
                Measure("stroke64", () =>
                {
                    brush.BeginGesture(room, GridBrushBase.Tool.Paint);
                    for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++)
                        brush.Paint(grid, room.Occupancy.gameObject, new Vector3Int(x, y, 0));
                    brush.EndGesture();
                });
                TestContext.Out.WriteLine("PERF stroke generations: " + generations);
                Assert.AreEqual(64, RoomAuthoringOperations.ReadCells(room).Length);
                Assert.IsEmpty(room.GenerationDiagnostic);
            }
            finally { RoomAuthoringOperations.GenerationCompleted -= count; Object.DestroyImmediate(brush); }
        }
        [Test] public void EightRoomsRepeatedGeneration()
        {
            using var f = new RoomAuthoring.RoomAuthoringTestFixtures(32);
            var rooms = new ShipRoomAuthoring[8];
            for (int i = 0; i < rooms.Length; i++) { rooms[i] = f.Room(16, 12); rooms[i].transform.position = Vector3.right * i * 20; }
            Measure("eightRoomsInitial", () => { foreach (var room in rooms) Assert.IsTrue(RoomAuthoringOperations.Rebuild(room)); });
            Measure("eightRoomsUnchanged", () => { foreach (var room in rooms) Assert.IsTrue(RoomAuthoringOperations.Rebuild(room)); });
        }
        [Test] public void VitalsAndIdleInput()
        {
            var vital = new Vital(100); int calls = 0; vital.Changed += _ => calls++;
            vital.Set(1); calls = 0;
            Measure("vital10000", () => { for (int i = 0; i < 10000; i++) vital.Set(i % 99 + 1); });
            Assert.AreEqual(9999, calls);
            var go = new GameObject("Input perf"); var host = go.AddComponent<UnityInputHost>();
            var update = (Action)Delegate.CreateDelegate(typeof(Action), host, typeof(UnityInputHost).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic));
            var release = (Action)Delegate.CreateDelegate(typeof(Action), host, typeof(UnityInputHost).GetMethod("RefreshUseRelease", BindingFlags.Instance | BindingFlags.NonPublic));
            try { update(); release(); Measure("idleInput10000", () => { for (int i = 0; i < 10000; i++) { update(); release(); } }); }
            finally { Object.DestroyImmediate(go); }
        }
        [Test] public void SixteenNavigatorsAroundObstacle()
        {
            var go = new GameObject("Navigation perf obstacle"); var obstacle = go.AddComponent<BoxCollider2D>();
            obstacle.size = new Vector2(.5f, 4); go.transform.position = Vector3.right;
            Physics2D.SyncTransforms(); int queries = 0;
            Func<Vector2, Vector2, bool> clear = (from, to) => { queries++; return Physics2D.Linecast(from, to).collider == null; };
            var type = typeof(UnityInputHost).Assembly.GetType("SOLITUDE.Npcs.Sec0.Sec0Navigator", true);
            var steps = new Func<Vector2, Vector2, float, Vector2>[16];
            for (int i = 0; i < steps.Length; i++)
            {
                var navigator = Activator.CreateInstance(type, new object[] { clear });
                steps[i] = (Func<Vector2, Vector2, float, Vector2>)Delegate.CreateDelegate(typeof(Func<Vector2, Vector2, float, Vector2>), navigator, type.GetMethod("Next", new[] { typeof(Vector2), typeof(Vector2), typeof(float) }));
            }
            try
            {
                Measure("sixteenNavigators20ticks", () => { for (int tick = 0; tick < 20; tick++) foreach (var step in steps) step(Vector2.zero, Vector2.right * 3, .1f); });
                TestContext.Out.WriteLine("PERF navigation physics queries: " + queries);
            }
            finally { Object.DestroyImmediate(go); }
        }
        internal sealed class Files : ISaveFiles
        {
            internal readonly Dictionary<string, string> Data = new();
            internal int Writes;
            internal bool FailWrite;
            public bool Exists(string path) => Data.ContainsKey(path);
            public string Read(string path) => Data[path];
            public void WriteDurable(string path, string text) { if (FailWrite) throw new System.IO.IOException("Injected write failure"); Writes++; Data[path] = text; }
            public void Move(string from, string to) { Data[to] = Data[from]; Data.Remove(from); }
            public void Replace(string from, string to, string backup) { Data[backup] = Data[to]; Move(from, to); }
            public void Delete(string path) => Data.Remove(path);
        }
        [Test] public void RapidSaveMutations()
        {
            var catalog = new ItemCatalog(new[] { new ItemSpec("perf", true, 10000) });
            var session = new ContainerSaveSession(new SaveGameData(), catalog);
            var inventory = session.Register(ContainerSaveSession.PlayerInventoryId, 1);
            session.MarkSaved();
            var files = new Files(); var go = new GameObject("Save perf"); var service = go.AddComponent<SaveGameService>();
            service.Configure(new SaveRecoveryStore("save", files, new SaveJsonCodec(catalog))); service.Initialize(session, null);
            var tick = (Action)Delegate.CreateDelegate(typeof(Action), service, typeof(SaveGameService).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic));
            try
            {
                Measure("save120dirtyFrames", () => { for (int i = 0; i < 120; i++) { session.Commands.Grant(inventory.Container.Handle, "perf", 1); tick(); } service.Flush(); });
                TestContext.Out.WriteLine("PERF durable writes: " + files.Writes);
                Assert.IsFalse(session.IsDirty);
                Assert.AreEqual(120, new SaveJsonCodec(catalog).Decode(files.Data["save"]).containers[0].state.slots[0].quantity);
            }
            finally { service.Release(); Object.DestroyImmediate(go); }
        }
    }
}
