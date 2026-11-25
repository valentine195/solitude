using System;
using NUnit.Framework;
using SOLITUDE.Containers;
using SOLITUDE.Items;
using SOLITUDE.Vitals;
namespace SOLITUDE.Tests
{
    public class VitalConsumableTransactionTests
    {
        private ContainerCommandService commands;
        private Container bag;
        private Target target;
        private VitalConsumableTransaction use;
        [SetUp] public void Setup()
        {
            commands = new ContainerCommandService(new ItemCatalog(new[] { new ItemSpec("ration", true, 10), new ItemSpec("medical", true, 10), new ItemSpec("tool", false, 1) }));
            bag = commands.Register("hotbar", 2); target = new Target(); use = new VitalConsumableTransaction();
        }
        private sealed class Target : IVitalEffectTarget
        {
            public VitalsRecoverySettings Settings = new VitalsRecoverySettings();
            public VitalsValues Values = new VitalsValues(30, 90);
            public VitalsRecoveryState Timing = new VitalsRecoveryState(5);
            public bool Available = true, Reserved;
            public int Applied, Completed;
            public Action Publish;
            public VitalUseOutcome TryReserve(VitalConsumableSpec effect, out IVitalEffectReservation reservation)
            {
                reservation = null;
                if (Reserved) return VitalUseOutcome.Busy;
                if (!Available) return VitalUseOutcome.Unavailable;
                if (Settings == null || !Settings.IsValid) return VitalUseOutcome.InvalidConfiguration;
                if (Values.Health <= 0) return VitalUseOutcome.Dead;
                if ((effect.Kind == VitalConsumableKind.Food ? Values.Hunger >= Settings.MaxHunger : Values.Health >= Settings.MaxHealth)) return VitalUseOutcome.Full;
                var result = effect.Kind == VitalConsumableKind.Food
                    ? VitalsRecoveryRules.RestoreFood(Settings, Values, Timing, effect.Amount, true)
                    : VitalsRecoveryRules.RestoreHealth(Settings, Values, Timing, effect.Amount, true);
                if (!result.Accepted) return VitalUseOutcome.InvalidEffect;
                reservation = new Reservation(this, result); Reserved = true; return VitalUseOutcome.Ready;
            }
            private sealed class Reservation : IVitalEffectReservation
            {
                private Target owner; private readonly VitalsRecoveryResult result; private bool applied;
                public Reservation(Target owner, VitalsRecoveryResult result) { this.owner = owner; this.result = result; }
                public void ApplySilently()
                { if (owner == null || applied) return; owner.Values = result.Values; owner.Timing = result.State; owner.Applied++; applied = true; }
                public void Complete()
                {
                    if (owner == null) return; var t = owner; owner = null; t.Completed++; t.Reserved = false;
                    if (applied) t.Publish?.Invoke();
                }
            }
        }
        private SlotAddress Grant(string id = "ration", int quantity = 2)
        { commands.Grant(bag.Handle, id, quantity); return bag.GetSlot(0).Address; }
        private VitalUseResult Use(SlotAddress address, VitalConsumableKind kind = VitalConsumableKind.Food, double amount = 25, string id = "ration")
            => use.TryUse(commands, address, new VitalConsumableSpec(id, kind, amount), target);
        [TestCase(VitalConsumableKind.Food, 30, 90, 30, 100)]
        [TestCase(VitalConsumableKind.Medical, 90, 60, 100, 60)]
        [TestCase(VitalConsumableKind.Medical, 30, 0, 55, 0)]
        public void OneItemAppliesOneClampedEffectWithoutChangingDelay(VitalConsumableKind kind, double health, double hunger, double expectedHealth, double expectedHunger)
        {
            string id = kind == VitalConsumableKind.Food ? "ration" : "medical";
            var address = Grant(id); target.Values = new VitalsValues(health, hunger);
            Assert.IsTrue(Use(address, kind, id: id).Committed);
            Assert.AreEqual(expectedHealth, target.Values.Health); Assert.AreEqual(expectedHunger, target.Values.Hunger);
            Assert.AreEqual(5, target.Timing.DelayRemaining); Assert.AreEqual(1, bag.GetSlot(0).Stack.Quantity);
            Assert.AreEqual(1, target.Applied); Assert.AreEqual(1, target.Completed); Assert.IsFalse(target.Reserved);
            Assert.AreEqual(CommandStatus.Stale, Use(address, kind, id: id).InventoryStatus);
            Assert.AreEqual(1, target.Applied); Assert.AreEqual(1, bag.GetSlot(0).Stack.Quantity);
        }
        [TestCase(5)] [TestCase(0)]
        public void FoodPreservesPendingOrCompletedDelayAndRecoveryStartsOnlyWhenEligible(double delay)
        {
            target.Values = new VitalsValues(30, 0); target.Timing = new VitalsRecoveryState(delay);
            Assert.IsTrue(Use(Grant()).Committed);
            Assert.AreEqual(delay, target.Timing.DelayRemaining); Assert.AreEqual(30, target.Values.Health);
            var next = VitalsRecoveryRules.Advance(target.Settings, target.Values, target.Timing, 1, true);
            Assert.AreEqual(delay == 0 ? 32 : 30, next.Values.Health);
        }
        [TestCase(0)] [TestCase(-1)] [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)] [TestCase(double.NegativeInfinity)]
        public void InvalidRestorationPreservesBothSnapshots(double amount)
        {
            var address = Grant(); Assert.AreEqual(VitalUseOutcome.InvalidEffect, Use(address, amount: amount).Outcome);
            Assert.AreEqual(2, bag.GetSlot(0).Stack.Quantity); Assert.AreEqual(90, target.Values.Hunger); Assert.AreEqual(0, target.Applied);
            Assert.AreEqual(address.Revision, bag.GetSlot(0).Address.Revision);
        }
        [TestCase(VitalConsumableKind.Food)] [TestCase(VitalConsumableKind.Medical)]
        public void FullOrDeadTargetsKeepTheirItems(VitalConsumableKind kind)
        {
            string id = kind == VitalConsumableKind.Food ? "ration" : "medical"; var address = Grant(id);
            target.Values = new VitalsValues(100, 100); Assert.AreEqual(VitalUseOutcome.Full, Use(address, kind, id: id).Outcome);
            target.Values = new VitalsValues(0, 0); Assert.AreEqual(VitalUseOutcome.Dead, Use(address, kind, id: id).Outcome);
            Assert.AreEqual(2, bag.GetSlot(0).Stack.Quantity); Assert.AreEqual(0, target.Applied);
        }
        [Test] public void MissingUnsupportedInvalidAndUnavailableEffectsDoNotConsume()
        {
            var address = Grant();
            Assert.AreEqual(VitalUseOutcome.Unsupported, use.TryUse(commands, address, null, target).Outcome);
            Assert.AreEqual(VitalUseOutcome.Unsupported, Use(address, VitalConsumableKind.None).Outcome);
            Assert.AreEqual(VitalUseOutcome.Unsupported, Use(address, (VitalConsumableKind)99).Outcome);
            target.Settings = new VitalsRecoverySettings(maxHealth: 0); Assert.AreEqual(VitalUseOutcome.InvalidConfiguration, Use(address).Outcome);
            target.Available = false; Assert.AreEqual(VitalUseOutcome.Unavailable, Use(address).Outcome);
            Assert.AreEqual(2, bag.GetSlot(0).Stack.Quantity); Assert.AreEqual(0, target.Applied);
        }
        [Test] public void StaleEmptyWrongIdentityAndUnavailableInventoryRejectBeforeReservation()
        {
            var address = Grant(); commands.Remove(address, 2);
            Assert.AreEqual(CommandStatus.Stale, Use(address).InventoryStatus);
            Assert.AreEqual(CommandStatus.InsufficientQuantity, Use(bag.GetSlot(0).Address).InventoryStatus);
            Grant("medical"); Assert.AreEqual(CommandStatus.Stale, Use(bag.GetSlot(0).Address).InventoryStatus);
            commands.Unregister(bag.Handle); Assert.AreEqual(CommandStatus.Unavailable, Use(bag.GetSlot(0)?.Address ?? address).InventoryStatus);
            Assert.AreEqual(0, target.Applied); Assert.AreEqual(0, target.Completed);
        }
        [Test] public void ObserverFaultsRemainCommittedAndReentrantAttemptsCannotUseAgain()
        {
            var address = Grant(); int errors = 0, observers = 0; bool coherent = true;
            commands.ObserverError += _ => throw new Exception("diagnostic"); commands.ObserverError += _ => errors++;
            commands.Completed += _ =>
            {
                coherent &= target.Values.Hunger == 100 && bag.GetSlot(0).Stack.Quantity == 1;
                coherent &= Use(bag.GetSlot(0).Address).Outcome == VitalUseOutcome.Busy; throw new Exception("inventory observer");
            };
            bag.SlotChanged += _ => observers++;
            target.Publish = () =>
            { coherent &= bag.GetSlot(0).Stack.Quantity == 1 && Use(bag.GetSlot(0).Address).Outcome == VitalUseOutcome.Busy; throw new Exception("vital observer"); };
            Assert.IsTrue(Use(address).Committed); Assert.IsTrue(coherent, "Observer snapshots and reentrant rejection must be checked outside isolated callbacks."); Assert.AreEqual(2, errors); Assert.AreEqual(1, observers);
            Assert.AreEqual(1, target.Applied); Assert.AreEqual(1, target.Completed); Assert.IsFalse(target.Reserved);
            Assert.AreEqual(VitalUseOutcome.Full, Use(bag.GetSlot(0).Address).Outcome);
        }
        [Test] public void CatalogRetainsIndependentStableIdsAndInvalidMetadataWithoutChangingInventorySpecs()
        {
            var catalog = new VitalConsumableCatalog(new[] { new VitalConsumableSpec("ration", VitalConsumableKind.Food, 25), new VitalConsumableSpec("tool"), new VitalConsumableSpec("bad", VitalConsumableKind.Medical, double.NaN) });
            Assert.AreEqual(25, catalog.Resolve("ration").Amount); Assert.IsFalse(catalog.Resolve("tool").Supported);
            Assert.IsFalse(catalog.Resolve("bad").IsValid); Assert.IsNull(catalog.Resolve("missing"));
            Assert.Throws<ArgumentException>(() => new VitalConsumableCatalog(new[] { new VitalConsumableSpec("a"), new VitalConsumableSpec("a") }));
        }
    }
}
