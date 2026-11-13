using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SOLITUDE.Containers;
using SOLITUDE.Expeditions;
using SOLITUDE.GameplayTags;
using SOLITUDE.Items;
using SOLITUDE.Restoration;
using SOLITUDE.SaveLoad;

namespace SOLITUDE.Tests
{
    public class MechanicalRepairTests
    {
        private ContainerSaveSession session;
        private Container inventory, hotbar;
        private ContainerSaveSession.MechanicalRepairBinding binding;
        private GameplayTagRegistry registry;
        private Dictionary<string, GameplayTagSet> tags;
        private readonly ItemCatalog catalog = new ItemCatalog(new[] {
            new ItemSpec("battery", true, 10), new ItemSpec("wrench", false, 1),
            new ItemSpec("other-tool", false, 1), new ItemSpec("spanner", false, 1), new ItemSpec("parent", false, 1) });
        [SetUp] public void SetUp()
        {
            registry = new GameplayTagRegistry(new[] { new GameplayTagSpec("t", "Tool"),
                new GameplayTagSpec("m", "Mechanical", "t"), new GameplayTagSpec("c", "Precision", "m"),
                new GameplayTagSpec("o", "Other", "t") });
            tags = new Dictionary<string, GameplayTagSet> { ["battery"] = registry.EmptySet,
                ["wrench"] = Set("m"), ["spanner"] = Set("c"), ["other-tool"] = Set("o"), ["parent"] = Set("t") };
            session = new ContainerSaveSession(new SaveGameData { startingLoadoutInitialized = true }, catalog);
            inventory = session.Register(ContainerSaveSession.PlayerInventoryId, 6).Container;
            hotbar = session.Register(ContainerSaveSession.PlayerHotbarId, 4).Container;
            binding = session.BindMechanicalRepair(Requirement());
        }
        private GameplayTagSet Set(string id) => new GameplayTagSet(registry, new[] { registry.GetTag(id) });
        private MechanicalRepairRequirement Requirement(string id = "aux.socket") => new MechanicalRepairRequirement(id, "battery", registry, registry.GetTag("m"));
        private GameplayTagSet Lookup(string id) => tags[id];
        private MechanicalRepairOutcome Repair() => binding.TryRepair(inventory.Handle, hotbar.Handle, Lookup);
        private void Grant(Container owner, string item, int count = 1) => Assert.AreEqual(CommandStatus.Applied, session.Commands.Grant(owner.Handle, item, count).Status);
        [TestCase(false, "wrench")][TestCase(true, "wrench")][TestCase(false, "spanner")][TestCase(true, "spanner")]
        public void CarriedCompatibleToolIsRetainedAndExactlyOneInventoryBatteryIsConsumed(bool inHotbar, string tool)
        {
            var owner = inHotbar ? hotbar : inventory;
            Grant(owner, tool); Grant(inventory, "battery", 2); Grant(hotbar, "battery", 3);
            var slot = owner.GetSlot(0);
            Assert.AreEqual(MechanicalRepairOutcome.Committed, Repair());
            Assert.AreEqual(slot.Address.Revision, owner.GetSlot(0).Address.Revision);
            Assert.AreEqual(1, owner.GetSlot(0).Stack.Quantity);
            Assert.AreEqual(1, Count(inventory, "battery")); Assert.AreEqual(3, Count(hotbar, "battery"));
            Assert.IsTrue(binding.IsCompleted); Assert.IsTrue(session.IsDirty); Assert.IsTrue(session.RequiresImmediateSave);
            Assert.AreEqual(MechanicalRepairOutcome.AlreadyRepaired, Repair()); Assert.AreEqual(1, Count(inventory, "battery"));
        }
        private int Count(Container owner, string id) { int n = 0; foreach (var s in owner.GetSlots()) if (!s.IsEmpty && s.Stack.ItemId == id) n += s.Stack.Quantity; return n; }
        [TestCase("parent")][TestCase("other-tool")]
        public void ParentAndUnrelatedCapabilitiesCannotSatisfyMechanicalRequirement(string item)
        {
            Grant(inventory, item); Grant(inventory, "battery");
            Assert.AreEqual(MechanicalRepairOutcome.MissingTool, Repair()); Assert.AreEqual(1, Count(inventory, "battery")); Assert.IsFalse(binding.IsCompleted);
        }
        [Test] public void MissingToolPrecedesMissingPartAndHotbarBatteryDoesNotPay()
        {
            Assert.AreEqual(MechanicalRepairOutcome.MissingTool, Repair()); Grant(hotbar, "wrench"); Grant(hotbar, "battery");
            Assert.AreEqual(MechanicalRepairOutcome.MissingPart, Repair()); Assert.AreEqual(1, Count(hotbar, "battery")); Assert.IsFalse(binding.IsCompleted);
        }
        [Test] public void PublicationIsCoherentCaptureAndReentryAreBlockedAndObserverFailuresDoNotUndoCommit()
        {
            Grant(hotbar, "wrench"); Grant(inventory, "battery", 2);
            int observations = 0, presentations = 0;
            bool presentationCoherent = false, commandCoherent = false, slotCoherent = false, captureBlocked = false;
            MechanicalRepairOutcome reentry = MechanicalRepairOutcome.Committed;
            binding.Changed += () => { presentations++; presentationCoherent = binding.IsCompleted && Count(inventory, "battery") == 1; };
            binding.Changed += () => throw new Exception("observer");
            session.Commands.Completed += _ => {
                observations++; commandCoherent = binding.IsCompleted && presentations == 1 && Count(inventory, "battery") == 1;
                try { session.Capture(); } catch (InvalidOperationException) { captureBlocked = true; }
                reentry = Repair(); };
            inventory.SlotChanged += _ => slotCoherent = binding.IsCompleted && Count(inventory, "battery") == 1;
            Assert.AreEqual(MechanicalRepairOutcome.Committed, Repair()); Assert.AreEqual(1, observations);
            Assert.IsTrue(presentationCoherent); Assert.IsTrue(commandCoherent); Assert.IsTrue(slotCoherent); Assert.IsTrue(captureBlocked);
            Assert.AreEqual(MechanicalRepairOutcome.Unavailable, reentry);
            var saved = session.Capture(); Assert.IsTrue(saved.ship.repairs[0].installed); Assert.IsTrue(saved.ship.repairs[0].running);
            Assert.AreEqual(1, saved.containers.Find(r => r.saveableId == ContainerSaveSession.PlayerInventoryId).state.slots[0].quantity);
            session.Commands.Remove(hotbar.GetSlot(0).Address, 1);
            Assert.AreEqual(MechanicalRepairOutcome.AlreadyRepaired, Repair());
        }
        [Test] public void ChangedResourcesAndStaleOwnersCannotCommit()
        {
            Grant(hotbar, "wrench"); Grant(inventory, "battery");
            session.Commands.Remove(inventory.GetSlot(0).Address, 1);
            Assert.AreEqual(MechanicalRepairOutcome.MissingPart, Repair());
            Grant(inventory, "battery"); session.Commands.Unregister(hotbar.Handle);
            Assert.AreEqual(MechanicalRepairOutcome.Unavailable, Repair()); Assert.AreEqual(1, Count(inventory, "battery")); Assert.IsFalse(binding.IsCompleted);
        }
        [Test] public void UnavailableOrWrongRegistryLookupRejectsWithoutMutationAndCannotMutateDuringValidation()
        {
            Grant(hotbar, "wrench"); Grant(inventory, "battery");
            Assert.AreEqual(MechanicalRepairOutcome.Unavailable, binding.TryRepair(inventory.Handle, hotbar.Handle, _ => null));
            Assert.AreEqual(MechanicalRepairOutcome.Unavailable, binding.TryRepair(inventory.Handle, hotbar.Handle, _ => new GameplayTagRegistry(Array.Empty<GameplayTagSpec>()).EmptySet));
            Assert.AreEqual(MechanicalRepairOutcome.Committed, binding.TryRepair(inventory.Handle, hotbar.Handle, id => {
                Assert.AreEqual(CommandStatus.Busy, session.Commands.Remove(inventory.GetSlot(0).Address, 1).Status); return Lookup(id); }));
            Assert.AreEqual(0, Count(inventory, "battery"));
        }
        [Test] public void BadAndDuplicateBindingsRejectAndReleaseCannotAuthorizeStaleRequests()
        {
            Assert.Throws<InvalidOperationException>(() => session.BindMechanicalRepair(Requirement()));
            Assert.Throws<ArgumentException>(() => Requirement(" "));
            Assert.Throws<ArgumentException>(() => new MechanicalRepairRequirement("id", "battery", registry, registry.GetTag("t")));
            Assert.Throws<InvalidOperationException>(() => session.BindMechanicalRepair(new MechanicalRepairRequirement("id", "unknown", registry, registry.GetTag("m"))));
            Grant(hotbar, "wrench"); Grant(inventory, "battery"); binding.Dispose();
            Assert.AreEqual(MechanicalRepairOutcome.Unavailable, Repair()); Assert.AreEqual(1, Count(inventory, "battery"));
            binding = session.BindMechanicalRepair(Requirement()); Assert.IsFalse(binding.IsCompleted);
        }
        [Test] public void AbsentRecordsRemainUnrepairedAndUnrelatedShipRecordsSurvive()
        {
            var ship = session.Capture().ship;
            ship.repairs.Add(new RepairSaveRecord { id = "other", installed = true });
            ship.doors.Add(new DoorSaveRecord { id = "other-door", unlocked = true }); ship.permanentDiscoveryIds.Add("discovery");
            session.StoreShipState(ship);
            Assert.IsFalse(binding.IsCompleted); Grant(hotbar, "wrench"); Grant(inventory, "battery");
            Assert.AreEqual(MechanicalRepairOutcome.Committed, Repair()); var saved = session.Capture();
            Assert.AreEqual(2, saved.ship.repairs.Count); Assert.IsFalse(saved.ship.repairs.Find(r => r.id == "other").running);
            Assert.IsTrue(saved.ship.doors[0].unlocked); Assert.AreEqual("discovery", saved.ship.permanentDiscoveryIds[0]);
            Assert.AreEqual(SaveGameData.CurrentVersion, saved.version);
        }
        [Test] public void MalformedSavedRepairIsNotDiscarded()
        {
            var bad = new SaveGameData(); bad.ship.repairs.Add(new RepairSaveRecord { id = "same" }); bad.ship.repairs.Add(new RepairSaveRecord { id = "same" });
            Assert.Throws<InvalidDataException>(() => new ContainerSaveSession(bad, catalog));
        }
    }
}
