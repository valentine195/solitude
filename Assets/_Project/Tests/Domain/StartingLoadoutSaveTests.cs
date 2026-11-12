using System;
using System.IO;
using System.Collections.Generic;
using NUnit.Framework;
using SOLITUDE.Items;
using SOLITUDE.SaveLoad;
using SOLITUDE.Survivors;
using SOLITUDE.Expeditions;

namespace SOLITUDE.Tests
{
    public sealed class StartingLoadoutSaveTests
    {
        private static ItemCatalog Catalog() => new ItemCatalog(new[] { new ItemSpec("tool", false, 1) });
        private static ContainerSaveRecord Contents(string id) => new ContainerSaveRecord
        {
            saveableId = id, state = new ContainerSaveData
            { slots = new List<ContainerSlotSaveData> { new ContainerSlotSaveData { index = 0, itemId = "tool", quantity = 1 } } }
        };
        private static void Lifecycle(SaveGameData data)
        {
            data.survivor = new SurvivorSnapshot
            {
                id = Guid.NewGuid().ToString(), scenePath = "Assets/_Project/Scenes/Wakeup.unity", x = 1.25, y = -2,
                health = new SavedVital { current = 40.5, max = 100 }, oxygen = new SavedVital { current = 70, max = 100 },
                hunger = new SavedVital { current = 80, max = 100 }, recoveryDelayRemaining = 3.25
            };
            data.expedition = new ExpeditionState { id = Guid.NewGuid().ToString() };
            data.expedition.defeatedEncounterIds.Add("bot"); data.expedition.collectedSupplySourceIds.Add("ration");
            data.ship.repairs.Add(new RepairSaveRecord { id = "scrubber", installed = true, running = true, recoverySeconds = 2 });
            data.ship.doors.Add(new DoorSaveRecord { id = "door", powered = true, unlocked = true, open = true });
            data.ship.permanentDiscoveryIds.Add("log");
        }
        [Test] public void ExplicitNewCandidateIsEmptyPendingWithoutChangingProductionVersion()
        {
            var data = SaveGameData.NewStartingLoadoutCandidate(17); var captured = new ContainerSaveSession(data, Catalog()).Capture();
            Assert.AreEqual(4, captured.version); Assert.IsFalse(captured.startingLoadoutInitialized);
            Assert.AreEqual(17, captured.worldSeed); Assert.IsEmpty(captured.containers); Assert.IsNull(captured.survivor);
            Assert.AreEqual(3, SaveGameData.CurrentVersion); Assert.AreEqual(3, new SaveGameData().version);
        }
        [TestCase(ContainerSaveSession.PlayerInventoryId)] [TestCase(ContainerSaveSession.PlayerHotbarId)]
        public void PendingCandidateRejectsPartialOrCarriedEquipment(string id)
        {
            var data = SaveGameData.NewStartingLoadoutCandidate(1); data.containers.Add(Contents(id));
            Assert.Throws<InvalidDataException>(() => new ContainerSaveSession(data, Catalog()));
            data.startingLoadoutInitialized = true; Assert.DoesNotThrow(() => new ContainerSaveSession(data, Catalog()));
        }
        [Test] public void LifecycleAndWorldFactsCanPrecedeAnEmptyPendingLoadout()
        {
            var data = SaveGameData.NewStartingLoadoutCandidate(12); Lifecycle(data); data.containers.Add(Contents("locker"));
            data.collectedPickupIds.Add("old-world-fact");
            var session = new ContainerSaveSession(data, Catalog());
            using var inventory = session.Register(ContainerSaveSession.PlayerInventoryId, 2);
            using var hotbar = session.Register(ContainerSaveSession.PlayerHotbarId, 2);
            var saved = session.Capture(); Assert.IsFalse(saved.startingLoadoutInitialized);
            Assert.AreEqual(data.survivor.id, saved.survivor.id); Assert.AreEqual(40.5, saved.survivor.health.current);
            Assert.AreEqual(3.25, saved.survivor.recoveryDelayRemaining); Assert.AreEqual("old-world-fact", saved.collectedPickupIds[0]);
            Assert.IsTrue(inventory.Container.GetSlot(0).IsEmpty); Assert.IsTrue(hotbar.Container.GetSlot(0).IsEmpty);
            Assert.AreEqual(1, saved.containers.Find(record => record.saveableId == "locker").state.slots.Count);
        }
        [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void LegacyPreparationCompletesWithoutAddingEquipmentOrInventingFacts(int version)
        {
            var data = new SaveGameData { version = version, worldSeed = 22 }; Lifecycle(data);
            data.containers.Add(Contents(ContainerSaveSession.PlayerHotbarId)); data.collectedPickupIds.Add("picked");
            var migrated = SaveGameData.MigrateStartingLoadoutCandidate(data);
            Assert.AreEqual(4, migrated.version); Assert.IsTrue(migrated.startingLoadoutInitialized);
            Assert.AreEqual(22, migrated.worldSeed); Assert.AreEqual(1, migrated.containers.Count);
            Assert.AreEqual("tool", migrated.containers[0].state.slots[0].itemId);
            Assert.AreEqual(version == 1 ? 0 : 1, migrated.collectedPickupIds.Count);
            if (version < 3) { Assert.IsNull(migrated.survivor); Assert.IsNull(migrated.expedition); Assert.IsEmpty(migrated.ship.repairs); }
            else
            {
                Assert.AreEqual(40.5, migrated.survivor.health.current); Assert.AreEqual(70, migrated.survivor.oxygen.current);
                Assert.AreEqual(80, migrated.survivor.hunger.current); Assert.AreEqual(3.25, migrated.survivor.recoveryDelayRemaining);
                Assert.AreEqual("bot", migrated.expedition.defeatedEncounterIds[0]); Assert.AreEqual("ration", migrated.expedition.collectedSupplySourceIds[0]);
                Assert.AreEqual(2, migrated.ship.repairs[0].recoverySeconds); Assert.IsTrue(migrated.ship.doors[0].open);
                Assert.AreEqual("log", migrated.ship.permanentDiscoveryIds[0]);
            }
        }
        [TestCase(false)] [TestCase(true)] public void CloneAndCaptureKeepTheMarkerDetached(bool completed)
        {
            var source = SaveGameData.NewStartingLoadoutCandidate(4); source.startingLoadoutInitialized = completed;
            if (completed) source.containers.Add(Contents(ContainerSaveSession.PlayerHotbarId));
            var session = new ContainerSaveSession(source, Catalog()); source.startingLoadoutInitialized = !completed; source.containers.Clear();
            var copy = session.Capture(); Assert.AreEqual(completed, copy.startingLoadoutInitialized);
            copy.startingLoadoutInitialized = !completed; copy.containers.Clear();
            var again = session.Capture(); Assert.AreEqual(completed, again.startingLoadoutInitialized);
            Assert.AreEqual(completed ? 1 : 0, again.containers.Count);
        }
        [TestCase(false)] [TestCase(true)] public void ExplicitVersionFourResetStartsPendingAndClearsOldFacts(bool completed)
        {
            var source = SaveGameData.NewStartingLoadoutCandidate(3); source.startingLoadoutInitialized = completed;
            Lifecycle(source); if (completed) source.containers.Add(Contents(ContainerSaveSession.PlayerHotbarId));
            var session = new ContainerSaveSession(source, Catalog()); session.Reset(9, releaseOwners: true);
            var saved = session.Capture(); Assert.AreEqual(4, saved.version); Assert.IsFalse(saved.startingLoadoutInitialized);
            Assert.AreEqual(9, saved.worldSeed); Assert.IsNull(saved.survivor); Assert.IsNull(saved.expedition);
            Assert.IsEmpty(saved.containers); Assert.IsEmpty(saved.ship.repairs); Assert.IsTrue(session.RequiresImmediateSave);
        }
        [Test] public void LivePartialGrantCannotEscapeCaptureAndResetRecoversAnEmptyPendingState()
        {
            var session = new ContainerSaveSession(SaveGameData.NewStartingLoadoutCandidate(1), Catalog());
            using var hotbar = session.Register(ContainerSaveSession.PlayerHotbarId, 1);
            Assert.IsTrue(session.Commands.Grant(hotbar.Container.Handle, "tool", 1).Succeeded);
            Assert.Throws<InvalidDataException>(() => session.Capture());
            session.Reset(8); var saved = session.Capture();
            Assert.IsFalse(saved.startingLoadoutInitialized); Assert.AreEqual(4, saved.version);
            Assert.AreEqual(8, saved.worldSeed); Assert.IsEmpty(saved.containers[0].state.slots);
        }
        [Test] public void PendingLoadoutDoesNotBypassExistingSuccessionValidation()
        {
            var data = SaveGameData.NewStartingLoadoutCandidate(1); Lifecycle(data);
            data.pendingDeath = new SavedDeathTransition();
            Assert.Throws<InvalidDataException>(() => new ContainerSaveSession(data, Catalog()));
            data.pendingDeath = null; data.expedition = null;
            Assert.Throws<InvalidDataException>(() => new ContainerSaveSession(data, Catalog()));
        }
        [TestCase(0)] [TestCase(5)] public void InvalidCandidateVersionDoesNotBecomeAUsableSave(int version)
        {
            var data = SaveGameData.NewStartingLoadoutCandidate(1); data.version = version;
            Assert.Throws<InvalidDataException>(() => SaveGameData.MigrateStartingLoadoutCandidate(data));
        }
    }
}
