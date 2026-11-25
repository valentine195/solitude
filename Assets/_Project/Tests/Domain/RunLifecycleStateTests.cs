using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SOLITUDE.Expeditions;
using SOLITUDE.Items;
using SOLITUDE.SaveLoad;
using SOLITUDE.Survivors;

namespace SOLITUDE.Tests
{
    public class RunLifecycleStateTests
    {
        private static ItemCatalog Catalog() => new ItemCatalog(new[] { new ItemSpec("fuse", true, 99) });
        private static SurvivorSnapshot Survivor() => new SurvivorSnapshot
        {
            id = Guid.NewGuid().ToString("D"), scenePath = "Assets/_Project/Scenes/Wakeup.unity",
            x = -0.89, y = -2.4, recoveryDelayRemaining = 3.25,
            health = new SavedVital { current = 40.5, max = 100 },
            oxygen = new SavedVital { current = 70, max = 100 },
            hunger = new SavedVital { current = 80, max = 100 }
        };
        private static SaveGameData Initialized() => new SaveGameData { startingLoadoutInitialized = true,
            survivor = Survivor(), expedition = new ExpeditionState { id = Guid.NewGuid().ToString("D") }
        };
        private static void Pending(SaveGameData data, SavedDeathStage stage = SavedDeathStage.Committed)
        {
            data.survivor.status = stage == SavedDeathStage.Committed ? SurvivorLifeStatus.Pending : SurvivorLifeStatus.Active;
            data.pendingDeath = new SavedDeathTransition
            {
                id = Guid.NewGuid().ToString("D"), departingSurvivorId = Guid.NewGuid().ToString("D"),
                successorId = data.survivor.id, departingExpeditionId = Guid.NewGuid().ToString("D"),
                expeditionId = data.expedition.id, stage = stage
            };
        }

        [TestCase(1)] [TestCase(2)]
        public void LegacyMigrationRetainsKnownFactsWithoutInventingLifecycle(int version)
        {
            var original = new SaveGameData { startingLoadoutInitialized = true, version = version, worldSeed = 47 };
            original.collectedPickupIds.Add("pickup.fuse");
            original.containers.Add(new ContainerSaveRecord { saveableId = "locker", state = new ContainerSaveData
            {
                slots = new List<ContainerSlotSaveData> { new ContainerSlotSaveData { index = 0, itemId = "fuse", quantity = 2 } }
            } });
            var session = new ContainerSaveSession(original, Catalog());
            var migrated = session.Capture();
            Assert.AreEqual(4, migrated.version); Assert.AreEqual(47, migrated.worldSeed);
            Assert.IsTrue(migrated.startingLoadoutInitialized); Assert.IsTrue(original.startingLoadoutInitialized);
            Assert.AreEqual(2, migrated.containers[0].state.slots[0].quantity);
            Assert.AreEqual(version == 1 ? 0 : 1, migrated.collectedPickupIds.Count);
            Assert.IsNull(migrated.survivor); Assert.IsNull(migrated.expedition); Assert.IsNull(migrated.pendingDeath);
            Assert.IsEmpty(migrated.ship.repairs); Assert.IsTrue(session.IsDirty);
            Assert.AreEqual(version, original.version); Assert.AreEqual(1, original.collectedPickupIds.Count);
            var repeated = new ContainerSaveSession(migrated, Catalog()).Capture();
            Assert.IsNull(repeated.survivor); Assert.IsNull(repeated.expedition);
        }

        [Test]
        public void InitializationHappensOnceAndNeverDuringCaptureOrDecode()
        {
            var session = new ContainerSaveSession(new SaveGameData { startingLoadoutInitialized = true }, Catalog());
            Assert.IsNull(session.Capture().survivor);
            var colonist = Survivor(); var expedition = new ExpeditionState { id = Guid.NewGuid().ToString("D") };
            Assert.IsTrue(session.TryInitializeLifecycle(colonist, expedition)); Assert.IsTrue(session.IsDirty);
            var restored = new ContainerSaveSession(session.Capture(), Catalog());
            restored.MarkSaved();
            Assert.IsFalse(restored.TryInitializeLifecycle(Survivor(), new ExpeditionState { id = Guid.NewGuid().ToString("D") }));
            Assert.IsFalse(restored.IsDirty);
            Assert.AreEqual(colonist.id, restored.Capture().survivor.id);
            Assert.AreEqual(expedition.id, restored.Capture().expedition.id);
            Assert.AreEqual(40.5, restored.Capture().survivor.health.current);
        }

        [Test]
        public void CaptureAndStoreDetachAllMutableLifecycleAndShipRecords()
        {
            var data = Initialized();
            data.expedition.defeatedEncounterIds.Add("sec0"); data.expedition.collectedSupplySourceIds.Add("bandage");
            data.ship.repairs.Add(new RepairSaveRecord { id = "scrubber", installed = true, running = true, recoverySeconds = 2 });
            data.ship.doors.Add(new DoorSaveRecord { id = "shortcut", powered = true, unlocked = true, open = true });
            data.ship.criticalSources.Add(new CriticalSourceSaveRecord { id = "fuse", objectiveId = "scrubber" });
            data.ship.permanentDiscoveryIds.Add("log");
            Pending(data);
            var session = new ContainerSaveSession(data, Catalog());
            data.survivor.health.current = 1; data.pendingDeath.id = "mutated";
            var detached = session.Capture();
            detached.survivor.health.current = 2; detached.survivor.oxygen.current = 3; detached.survivor.hunger.current = 4;
            detached.expedition.defeatedEncounterIds.Clear(); detached.expedition.collectedSupplySourceIds.Clear();
            detached.ship.repairs[0].recoverySeconds = 0; detached.ship.doors[0].open = false;
            detached.ship.criticalSources[0].available = true; detached.ship.permanentDiscoveryIds.Clear();
            detached.pendingDeath.id = "other";
            var saved = session.Capture();
            Assert.AreEqual(40.5, saved.survivor.health.current); Assert.AreEqual(70, saved.survivor.oxygen.current);
            Assert.AreEqual(80, saved.survivor.hunger.current); Assert.AreEqual(1, saved.expedition.defeatedEncounterIds.Count);
            Assert.AreEqual(1, saved.expedition.collectedSupplySourceIds.Count);
            Assert.AreEqual(2, saved.ship.repairs[0].recoverySeconds); Assert.IsTrue(saved.ship.doors[0].open);
            Assert.IsFalse(saved.ship.criticalSources[0].available); Assert.AreEqual(1, saved.ship.permanentDiscoveryIds.Count);
            Assert.DoesNotThrow(() => saved.ValidateLifecycle());
            session.StoreLifecycle(saved.survivor, saved.expedition, saved.pendingDeath);
            session.StoreShipState(saved.ship);
            saved.survivor.health.current = 0; saved.expedition.defeatedEncounterIds.Clear();
            saved.pendingDeath.id = "changed after store"; saved.ship.doors.Clear();
            Assert.AreEqual(40.5, session.Capture().survivor.health.current);
            Assert.AreEqual(1, session.Capture().expedition.defeatedEncounterIds.Count);
            Assert.AreEqual(1, session.Capture().ship.doors.Count);
            Assert.DoesNotThrow(() => session.Capture().ValidateLifecycle());
        }

        [TestCase("identity")] [TestCase("empty-expedition")] [TestCase("partial")]
        [TestCase("nan")] [TestCase("infinity")] [TestCase("coordinate-overflow")]
        [TestCase("range")] [TestCase("negative-delay")] [TestCase("scene")]
        [TestCase("duplicate-encounter")] [TestCase("missing-supplies")] [TestCase("dead-without-transition")]
        public void InvalidLifecycleCannotBecomeAUsableSession(string kind)
        {
            var data = Initialized();
            switch (kind)
            {
                case "identity": data.survivor.id = "not-a-guid"; break;
                case "empty-expedition": data.expedition.id = Guid.Empty.ToString(); break;
                case "partial": data.expedition = null; break;
                case "nan": data.survivor.health.current = double.NaN; break;
                case "infinity": data.survivor.x = double.PositiveInfinity; break;
                case "coordinate-overflow": data.survivor.z = double.MaxValue; break;
                case "range": data.survivor.health.current = 101; break;
                case "negative-delay": data.survivor.recoveryDelayRemaining = -1; break;
                case "scene": data.survivor.scenePath = "Assets/../outside.unity"; break;
                case "duplicate-encounter": data.expedition.defeatedEncounterIds.AddRange(new[] { "bot", "bot" }); break;
                case "missing-supplies": data.expedition.collectedSupplySourceIds = null; break;
                case "dead-without-transition": data.survivor.status = SurvivorLifeStatus.Dead; break;
            }
            Assert.Throws<InvalidDataException>(() => new ContainerSaveSession(data, Catalog()));
        }

        [TestCase("running-uninstalled")] [TestCase("recovery-stopped")] [TestCase("open-locked")]
        [TestCase("duplicate-repair")] [TestCase("completed-source")] [TestCase("null-record")]
        public void ContradictoryShipFactsAreRejectedWithoutChangingStoredState(string kind)
        {
            var session = new ContainerSaveSession(Initialized(), Catalog()); session.MarkSaved();
            var ship = new ShipStateSnapshot();
            switch (kind)
            {
                case "running-uninstalled": ship.repairs.Add(new RepairSaveRecord { id = "scrubber", running = true }); break;
                case "recovery-stopped": ship.repairs.Add(new RepairSaveRecord { id = "scrubber", recoverySeconds = 1 }); break;
                case "open-locked": ship.doors.Add(new DoorSaveRecord { id = "door", open = true }); break;
                case "duplicate-repair": ship.repairs.AddRange(new[] { new RepairSaveRecord { id = "r" }, new RepairSaveRecord { id = "r" } }); break;
                case "completed-source":
                    ship.repairs.Add(new RepairSaveRecord { id = "r", installed = true });
                    ship.criticalSources.Add(new CriticalSourceSaveRecord { id = "pickup", objectiveId = "r", available = true }); break;
                case "null-record": ship.doors.Add(null); break;
            }
            Assert.Throws<InvalidDataException>(() => session.StoreShipState(ship));
            Assert.IsFalse(session.IsDirty); Assert.IsEmpty(session.Capture().ship.repairs);
        }

        [TestCase(SavedDeathStage.Committed)] [TestCase(SavedDeathStage.Activated)]
        public void CompleteTransitionReceiptsSurviveCapture(SavedDeathStage stage)
        {
            var data = Initialized(); Pending(data, stage);
            var restored = new ContainerSaveSession(new ContainerSaveSession(data, Catalog()).Capture(), Catalog()).Capture();
            Assert.AreEqual(data.pendingDeath.id, restored.pendingDeath.id);
            Assert.AreEqual(stage, restored.pendingDeath.stage);
            Assert.AreEqual(data.expedition.id, restored.expedition.id);
        }

        [TestCase("same-survivor")] [TestCase("same-expedition")] [TestCase("wrong-successor")]
        [TestCase("wrong-status")] [TestCase("unknown-stage")] [TestCase("departing-equipment")]
        public void InvalidTransitionCannotRetainMixedOwnership(string kind)
        {
            var data = Initialized(); Pending(data);
            switch (kind)
            {
                case "same-survivor": data.pendingDeath.departingSurvivorId = data.survivor.id.ToUpperInvariant(); break;
                case "same-expedition": data.pendingDeath.departingExpeditionId = data.expedition.id; break;
                case "wrong-successor": data.pendingDeath.successorId = Guid.NewGuid().ToString(); break;
                case "wrong-status": data.survivor.status = SurvivorLifeStatus.Active; break;
                case "unknown-stage": data.pendingDeath.stage = (SavedDeathStage)77; break;
                case "departing-equipment":
                    data.containers.Add(new ContainerSaveRecord { saveableId = ContainerSaveSession.PlayerInventoryId,
                        state = new ContainerSaveData { slots = new List<ContainerSlotSaveData> { new ContainerSlotSaveData { index = 0, itemId = "fuse", quantity = 1 } } } }); break;
            }
            Assert.Throws<InvalidDataException>(() => new ContainerSaveSession(data, Catalog()));
        }

        [Test]
        public void RejectedLifecycleUpdateDoesNotEraseCurrentColonistOrDirtyTheSession()
        {
            var data = Initialized(); var session = new ContainerSaveSession(data, Catalog()); session.MarkSaved();
            var bad = Survivor(); bad.health.current = -1;
            Assert.Throws<InvalidDataException>(() => session.StoreLifecycle(bad, data.expedition));
            Assert.AreEqual(data.survivor.id, session.Capture().survivor.id); Assert.IsFalse(session.IsDirty);
        }

        [Test]
        public void NewSaveClearsLifecycleShipAndCollectionFactsWhileDisposingOwnersSafely()
        {
            var data = Initialized(); data.ship.permanentDiscoveryIds.Add("found"); data.collectedPickupIds.Add("collected");
            var session = new ContainerSaveSession(data, Catalog());
            var old = session.Register("locker", 1); session.Commands.Grant(old.Container.Handle, "fuse", 1);
            session.Reset(73, releaseOwners: true); old.Dispose();
            var reset = session.Capture();
            Assert.AreEqual(73, reset.worldSeed); Assert.IsNull(reset.survivor); Assert.IsNull(reset.expedition);
            Assert.IsNull(reset.pendingDeath); Assert.IsEmpty(reset.ship.permanentDiscoveryIds);
            Assert.IsEmpty(reset.collectedPickupIds); Assert.IsEmpty(reset.containers); Assert.IsTrue(session.IsDirty);
            Assert.IsTrue(session.TryInitializeLifecycle(Survivor(), new ExpeditionState { id = Guid.NewGuid().ToString() }));
        }
    }
}
