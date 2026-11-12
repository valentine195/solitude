using System;
using System.Reflection;
using NUnit.Framework;
using SOLITUDE.Application.Persistence;
using SOLITUDE.Editor.RoomAuthoring;
using SOLITUDE.Items;
using SOLITUDE.Player.Vitals;
using SOLITUDE.SaveLoad;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace SOLITUDE.Tests
{
    public sealed class PerformanceBehaviorTests
    {
        [Test] public void StrokeCommitsOnceLastSampleWinsAndUndoRestoresBothLayers()
        {
            using var f = new RoomAuthoring.RoomAuthoringTestFixtures(32);
            var room = f.Room(0, 0); Assert.IsTrue(RoomAuthoringOperations.Rebuild(room));
            int generations = 0; Action<SOLITUDE.World.RoomAuthoring.ShipRoomAuthoring> count = _ => generations++;
            RoomAuthoringOperations.GenerationCompleted += count;
            try
            {
                using (var edit = RoomAuthoringOperations.BeginEdit(room, "Stroke"))
                {
                    RoomAuthoringOperations.PaintCell(room, Vector3Int.zero, f.Style.FloorTile, edit);
                    RoomAuthoringOperations.PaintCell(room, Vector3Int.right, f.Style.FloorTile, edit);
                    RoomAuthoringOperations.PaintCell(room, Vector3Int.right, null, edit);
                    Assert.AreEqual(0, generations);
                }
                Assert.AreEqual(1, generations); Assert.AreEqual(1, RoomAuthoringOperations.ReadCells(room).Length);
                UnityEditor.Undo.PerformUndo(); Assert.IsEmpty(RoomAuthoringOperations.ReadCells(room));
                Assert.AreEqual(0, room.GeneratedOutput.transform.childCount);
                UnityEditor.Undo.PerformRedo(); Assert.AreEqual(1, RoomAuthoringOperations.ReadCells(room).Length);
                Assert.IsNotNull(room.GeneratedOutput.GetComponentInChildren<Tilemap>());
            }
            finally { RoomAuthoringOperations.GenerationCompleted -= count; }
        }
        [Test] public void GestureRevalidatesSafetyAtCommit()
        {
            using var f = new RoomAuthoring.RoomAuthoringTestFixtures(32);
            var room = f.Room(0, 0);
            using (var edit = RoomAuthoringOperations.BeginEdit(room, "Stroke"))
            {
                RoomAuthoringOperations.PaintCell(room, Vector3Int.zero, f.Style.FloorTile, edit);
                room.transform.localScale = Vector3.one * 2;
            }
            Assert.IsEmpty(RoomAuthoringOperations.ReadCells(room));
        }
        [Test] public void UnchangedRebuildRetainsObjectsAndDeletedVisualIsRepaired()
        {
            using var f = new RoomAuthoring.RoomAuthoringTestFixtures(32);
            var room = f.Room(); Assert.IsTrue(RoomAuthoringOperations.Rebuild(room));
            var before = room.GeneratedOutput.GetComponentsInChildren<Transform>();
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(room));
            CollectionAssert.AreEqual(before, room.GeneratedOutput.GetComponentsInChildren<Transform>());
            Object.DestroyImmediate(before[2].gameObject);
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(room));
            Assert.AreEqual(before.Length, room.GeneratedOutput.GetComponentsInChildren<Transform>().Length);
        }
        [Test] public void VitalSnapshotRetainsPublicationOrderingAndObserverIsolation()
        {
            var vital = new Vital(100); string calls = "";
            Action<Vital> later = _ => calls += "C";
            Action<Vital> second = _ => calls += "B";
            Action<Vital> first = _ => { calls += "A"; vital.Changed -= second; vital.Changed += later; throw new InvalidOperationException(); };
            int errors = 0; vital.ObserverError += _ => errors++;
            vital.Changed += first; vital.Changed += second;
            vital.Set(90); Assert.AreEqual("AB", calls); Assert.AreEqual(1, errors);
            calls = ""; vital.Set(80); Assert.AreEqual("AC", calls); Assert.AreEqual(2, errors);
            vital.Set(80); Assert.AreEqual(2, errors, "No notification on no-op mutation.");
        }
        private static void Autosave(SaveGameService service, double now) =>
            typeof(SaveGameService).GetMethod("Autosave", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(service, new object[] { now });
        [Test] public void AutosaveHasBoundedDelayCriticalBypassAndRetryableFailure()
        {
            var catalog = new ItemCatalog(new[] { new ItemSpec("item", true, 1000) });
            var session = new ContainerSaveSession(new SaveGameData { startingLoadoutInitialized = true }, catalog);
            var inventory = session.Register(ContainerSaveSession.PlayerInventoryId, 1); session.MarkSaved();
            var files = new PerformanceOptimizationTests.Files(); var go = new GameObject("Autosave test");
            var service = go.AddComponent<SaveGameService>();
            service.Configure(new SaveRecoveryStore("save", files, new SaveJsonCodec(catalog))); service.Initialize(session, null);
            try
            {
                session.Commands.Grant(inventory.Container.Handle, "item", 1); Autosave(service, 0);
                session.Commands.Grant(inventory.Container.Handle, "item", 1); Autosave(service, .24);
                Assert.AreEqual(0, files.Writes);
                Autosave(service, .25); Assert.AreEqual(1, files.Writes); Assert.IsFalse(session.IsDirty);
                var pickups = new PickupCollectionService(session);
                using (pickups.Register("critical-pickup", new PickupReward("item", 1)))
                {
                    Assert.AreEqual(CollectResult.Collected, pickups.TryCollect("critical-pickup", ContainerSaveSession.PlayerInventoryId, "item", 1));
                    Assert.IsTrue(session.RequiresImmediateSave);
                    Autosave(service, .251); Assert.IsFalse(session.IsDirty);
                }
                session.Reset(5); Assert.IsTrue(session.RequiresImmediateSave);
                Autosave(service, .26); Assert.AreEqual(3, files.Writes); Assert.IsFalse(session.RequiresImmediateSave);
                using var hotbar = session.Register(ContainerSaveSession.PlayerHotbarId, 1);
                Assert.IsTrue(session.InitializeStartingLoadout(inventory.Container.Handle, hotbar.Container.Handle,
                    new[] { new PickupReward("item", 1) }, 0).Succeeded);
                session.Commands.Grant(inventory.Container.Handle, "item", 1); files.FailWrite = true;
                LogAssert.Expect(LogType.Error, "[SaveGameService] Failed to save: Injected write failure");
                Assert.IsFalse(service.TryFlush()); Assert.IsTrue(session.IsDirty); Assert.IsNotNull(service.LastSaveError);
                files.FailWrite = false; Assert.IsTrue(service.TryFlush()); Assert.IsFalse(session.IsDirty); Assert.IsNull(service.LastSaveError);
                session.Commands.Grant(inventory.Container.Handle, "item", 1); Autosave(service, 1);
                LogAssert.Expect(LogType.Error, "[SaveGameService] Failed to save: Injected write failure");
                files.FailWrite = true; Autosave(service, 1.25); Assert.IsTrue(session.IsDirty);
                Autosave(service, 1.3); // No retry/log storm while failed.
                files.FailWrite = false; Autosave(service, 1.5); Assert.IsFalse(session.IsDirty);
                int writesBeforeBurst = files.Writes;
                for (int frame = 0; frame < 120; frame++)
                {
                    session.Commands.Grant(inventory.Container.Handle, "item", 1);
                    Autosave(service, 2 + frame / 60.0);
                }
                Assert.IsTrue(service.TryFlush());
                Assert.That(files.Writes - writesBeforeBurst, Is.InRange(7, 9), "Sustained mutation must flush regularly, not debounce forever.");
            }
            finally { files.FailWrite = false; service.Release(); Object.DestroyImmediate(go); }
        }
    }
}
