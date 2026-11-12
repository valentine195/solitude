using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SOLITUDE.Application;
using SOLITUDE.Containers;
using SOLITUDE.Items;
using SOLITUDE.SaveLoad;
namespace SOLITUDE.Tests
{
    public sealed class StartingLoadoutCoordinatorTests
    {
        private ItemCatalog catalog;
        private ContainerSaveSession session;
        private ContainerSaveSession.ContainerBinding inventory, hotbar;
        private HotbarSelectionState selection;
        private StartingLoadoutCoordinator coordinator;
        [SetUp] public void Prepare()
        {
            catalog = new ItemCatalog(new[] { new ItemSpec("tool", false, 1), new ItemSpec("supplies", true, 10) });
            session = new ContainerSaveSession(new SaveGameData(), catalog);
            inventory = session.Register(ContainerSaveSession.PlayerInventoryId, 2);
            hotbar = session.Register(ContainerSaveSession.PlayerHotbarId, 3);
            selection = new HotbarSelectionState(hotbar.Container);
            coordinator = new StartingLoadoutCoordinator(session, inventory.Container, hotbar.Container, selection);
        }
        [TearDown] public void Cleanup() { selection.Dispose(); inventory.Dispose(); hotbar.Dispose(); }
        private StartingLoadoutSpec Spec(int quantity = 1, int slot = 0) => new StartingLoadoutSpec(catalog, new[] { new PickupReward("tool", quantity) }, slot);
        private int Count => hotbar.Container.GetSlots().Where(s => !s.IsEmpty).Sum(s => s.Stack.Quantity);
        private bool Persist() { session.Capture(); session.MarkSaved(); return true; }
        [Test] public void GrantsAndCompletionPublishTogetherAndSelectionWaitsForPersistence()
        {
            int publications = 0; CommandStatus reentrant = CommandStatus.Invalid;
            session.Commands.Completed += _ =>
            {
                publications++; Assert.IsTrue(session.StartingLoadoutInitialized); Assert.AreEqual(1, Count);
                Assert.Throws<InvalidOperationException>(() => session.Capture());
                reentrant = session.InitializeStartingLoadout(inventory.Container.Handle, hotbar.Container.Handle, Spec().HotbarGrants, 0).Status;
            };
            Assert.AreEqual(StartingLoadoutOutcome.Ready, coordinator.Initialize(Spec(), () =>
            {
                Assert.AreEqual(-1, selection.ActiveIndex); Assert.IsTrue(session.Capture().startingLoadoutInitialized);
                Assert.IsTrue(session.RequiresImmediateSave); return Persist();
            }));
            Assert.AreEqual(1, publications); Assert.AreEqual(CommandStatus.Busy, reentrant); Assert.AreEqual(0, selection.ActiveIndex);
            Assert.IsFalse(session.IsDirty);
        }
        [TestCase(false)] [TestCase(true)] public void FailedOrThrownFlushRetriesWithoutDuplicatingInMemoryGrants(bool throws)
        {
            Assert.AreEqual(StartingLoadoutOutcome.PersistenceFailed, coordinator.Initialize(Spec(), () =>
            { if (throws) throw new InvalidOperationException("fixture write failure"); return false; }));
            Assert.IsTrue(session.StartingLoadoutInitialized); Assert.AreEqual(1, Count); Assert.AreEqual(-1, selection.ActiveIndex);
            Assert.AreEqual(StartingLoadoutOutcome.Ready, coordinator.Initialize(Spec(), Persist));
            Assert.AreEqual(1, Count); Assert.AreEqual(0, selection.ActiveIndex);
            selection.Select(-1); Assert.AreEqual(StartingLoadoutOutcome.AlreadyInitialized, coordinator.Initialize(Spec(), Persist));
            Assert.AreEqual(-1, selection.ActiveIndex); Assert.AreEqual(1, Count);
        }
        [Test] public void ReentrantInitializationDuringFlushCannotEnterAgain()
        {
            StartingLoadoutOutcome reentrant = StartingLoadoutOutcome.Ready;
            Assert.AreEqual(StartingLoadoutOutcome.Ready, coordinator.Initialize(Spec(), () =>
            { reentrant = coordinator.Initialize(Spec(), Persist); return Persist(); }));
            Assert.AreEqual(StartingLoadoutOutcome.Busy, reentrant); Assert.AreEqual(1, Count);
        }
        [TestCase(4, 0)] [TestCase(1, 2)] [TestCase(int.MaxValue, 0)]
        public void CapacityOrUnfilledSelectedSlotRejectsBeforeAnyMutation(int quantity, int slot)
        {
            var address = hotbar.Container.GetSlot(0).Address;
            Assert.AreEqual(StartingLoadoutOutcome.Rejected, coordinator.Initialize(Spec(quantity, slot), Persist));
            Assert.IsFalse(session.StartingLoadoutInitialized); Assert.AreEqual(0, Count); Assert.AreEqual(address, hotbar.Container.GetSlot(0).Address);
            Assert.IsFalse(session.Capture().startingLoadoutInitialized); Assert.AreEqual(-1, selection.ActiveIndex);
            Assert.AreEqual(StartingLoadoutOutcome.Ready, coordinator.Initialize(Spec(), Persist));
        }
        [TestCase(0)] [TestCase(-1)] public void InvalidAuthoredQuantitiesRejectBeforeCommands(int quantity)
        { Assert.Throws<ArgumentException>(() => Spec(quantity)); Assert.AreEqual(0, Count); Assert.IsFalse(session.StartingLoadoutInitialized); }
        [Test] public void MissingUnknownAndEmptyDefinitionsAreRejected()
        {
            Assert.Throws<ArgumentException>(() => new StartingLoadoutSpec(catalog, null, 0));
            Assert.Throws<ArgumentException>(() => new StartingLoadoutSpec(catalog, Array.Empty<PickupReward>(), 0));
            Assert.Throws<ArgumentException>(() => new StartingLoadoutSpec(catalog, new[] { new PickupReward("missing", 1) }, 0));
            Assert.Throws<ArgumentException>(() => Spec(slot: -1)); Assert.AreEqual(0, Count);
        }
        [Test] public void DifferentOrderedLoadoutUsesItsOwnSelectedSlotAndFrozenConfiguration()
        {
            var input = new List<PickupReward> { new PickupReward("supplies", 2), new PickupReward("tool", 1) };
            var spec = new StartingLoadoutSpec(catalog, input, 1); input.Clear();
            Assert.AreEqual(StartingLoadoutOutcome.Ready, coordinator.Initialize(spec, Persist));
            Assert.AreEqual("supplies", hotbar.Container.GetSlot(0).Stack.ItemId); Assert.AreEqual(2, hotbar.Container.GetSlot(0).Stack.Quantity);
            Assert.AreEqual("tool", hotbar.Container.GetSlot(1).Stack.ItemId); Assert.AreEqual(1, selection.ActiveIndex);
        }
        [Test] public void OccupiedPendingInventoryIsRejectedWithoutChangingEitherOwner()
        {
            session.Commands.Grant(inventory.Container.Handle, "supplies", 1);
            Assert.AreEqual(StartingLoadoutOutcome.Rejected, coordinator.Initialize(Spec(), Persist));
            Assert.IsFalse(session.StartingLoadoutInitialized); Assert.AreEqual(0, Count);
            Assert.AreEqual(1, inventory.Container.GetSlot(0).Stack.Quantity);
        }
        [Test] public void CompletedResumeNeverRegrantsMovedOrLostEquipment()
        {
            coordinator.Initialize(Spec(), Persist);
            session.Commands.Transfer(hotbar.Container.GetSlot(0).Address, inventory.Container.GetSlot(0).Address); selection.Select(-1);
            var rebound = new StartingLoadoutCoordinator(session, inventory.Container, hotbar.Container, selection);
            Assert.AreEqual(StartingLoadoutOutcome.AlreadyInitialized, rebound.Initialize(Spec(), Persist));
            Assert.AreEqual(0, Count); Assert.AreEqual(1, inventory.Container.GetSlot(0).Stack.Quantity); Assert.AreEqual(-1, selection.ActiveIndex);
        }
        [Test] public void PublicationObserverFailureCannotRollBackCompletionOrDuplicateARepeat()
        {
            int errors = 0; session.Commands.ObserverError += _ => errors++;
            session.Commands.Completed += _ => throw new InvalidOperationException("fixture observer failure");
            Assert.AreEqual(StartingLoadoutOutcome.Ready, coordinator.Initialize(Spec(), Persist));
            Assert.AreEqual(1, errors); Assert.AreEqual(1, Count); Assert.IsTrue(session.StartingLoadoutInitialized);
            Assert.AreEqual(StartingLoadoutOutcome.AlreadyInitialized, coordinator.Initialize(Spec(), Persist)); Assert.AreEqual(1, Count);
        }
        [Test] public void HotbarAcceptanceRulesRejectBeforeCompletionOrPublication()
        {
            selection.Dispose(); hotbar.Dispose();
            hotbar = session.Register(ContainerSaveSession.PlayerHotbarId, 3, filter: item => item.ItemId == "supplies");
            selection = new HotbarSelectionState(hotbar.Container);
            coordinator = new StartingLoadoutCoordinator(session, inventory.Container, hotbar.Container, selection);
            Assert.AreEqual(StartingLoadoutOutcome.Rejected, coordinator.Initialize(Spec(), Persist));
            Assert.AreEqual(CommandStatus.Rejected, coordinator.LastCommandStatus);
            Assert.AreEqual(0, Count); Assert.IsFalse(session.StartingLoadoutInitialized);
        }
        [Test] public void UnavailableOwnerCannotCompleteTheLoadout()
        {
            session.Commands.Unregister(hotbar.Container.Handle);
            Assert.AreEqual(StartingLoadoutOutcome.Rejected, coordinator.Initialize(Spec(), Persist));
            Assert.IsFalse(session.StartingLoadoutInitialized);
        }
    }
}
