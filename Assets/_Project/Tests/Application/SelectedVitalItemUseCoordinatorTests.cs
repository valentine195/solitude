using System;
using NUnit.Framework;
using SOLITUDE.Application;
using SOLITUDE.Containers;
using SOLITUDE.Items;
using SOLITUDE.Vitals;
namespace SOLITUDE.Tests
{
    public class SelectedVitalItemUseCoordinatorTests
    {
        private ContainerCommandService commands;
        private Container hotbar;
        private HotbarSelectionState selection;
        private InputPolicyCoordinator policy;
        private SelectedVitalItemUseCoordinator use;
        private Target target;
        private sealed class Target : IVitalEffectTarget
        {
            public string LastItem; public int Applies; public Action Publish;
            public VitalUseOutcome TryReserve(VitalConsumableSpec effect, out IVitalEffectReservation reservation)
            { LastItem = effect.ItemId; reservation = new Reservation(this); return VitalUseOutcome.Ready; }
            private sealed class Reservation : IVitalEffectReservation
            {
                private Target target; private bool applied;
                public Reservation(Target target) { this.target = target; }
                public void ApplySilently() { if (target == null || applied) return; target.Applies++; applied = true; }
                public void Complete() { var t = target; target = null; if (t != null && applied) t.Publish?.Invoke(); }
            }
        }
        [SetUp] public void Setup()
        {
            commands = new ContainerCommandService(new ItemCatalog(new[] { new ItemSpec("food", true, 10), new ItemSpec("medical", true, 10), new ItemSpec("tool", true, 10) }));
            hotbar = commands.Register("hotbar", 3); selection = new HotbarSelectionState(hotbar);
            policy = new InputPolicyCoordinator(new PauseCoordinator()); policy.SetReady(true); target = new Target();
            use = new SelectedVitalItemUseCoordinator(commands, hotbar, selection, new VitalConsumableCatalog(new[] {
                new VitalConsumableSpec("food", VitalConsumableKind.Food, 25), new VitalConsumableSpec("medical", VitalConsumableKind.Medical, 25), new VitalConsumableSpec("tool") }), target, policy);
        }
        [TearDown] public void Cleanup() { use.Dispose(); selection.Dispose(); policy.Dispose(); }
        [Test] public void AttemptResolvesCurrentSelectedSlotAndLastStackClearsSelection()
        {
            commands.Grant(hotbar.Handle, "food", 1); commands.Grant(hotbar.Handle, "medical", 2);
            selection.Select(1); Assert.IsTrue(use.TryUse().Committed); Assert.AreEqual("medical", target.LastItem);
            Assert.AreEqual(1, hotbar.GetSlot(1).Stack.Quantity); Assert.AreEqual(1, hotbar.GetSlot(0).Stack.Quantity);
            selection.Select(0); Assert.IsTrue(use.TryUse().Committed); Assert.AreEqual("food", target.LastItem);
            Assert.AreEqual(-1, selection.ActiveIndex); Assert.AreEqual(VitalUseOutcome.NoSelection, use.TryUse().Outcome);
            Assert.AreEqual(2, target.Applies);
        }
        [Test] public void EmptyUnsupportedAndNoSelectionPreserveSelectionAndContents()
        {
            Assert.AreEqual(VitalUseOutcome.NoSelection, use.TryUse().Outcome);
            selection.Select(2); Assert.AreEqual(VitalUseOutcome.Empty, use.TryUse().Outcome); Assert.AreEqual(2, selection.ActiveIndex);
            commands.Grant(hotbar.Handle, "tool", 1); selection.Select(0);
            Assert.AreEqual(VitalUseOutcome.Unsupported, use.TryUse().Outcome); Assert.AreEqual(0, selection.ActiveIndex);
            Assert.AreEqual(1, hotbar.GetSlot(0).Stack.Quantity); Assert.AreEqual(0, target.Applies);
        }
        [Test] public void PolicyReleaseAndInventoryAvailabilityBlockWithoutMutation()
        {
            commands.Grant(hotbar.Handle, "food", 2); selection.Select(0);
            using (policy.AcquirePrimaryScreen()) Assert.AreEqual(VitalUseOutcome.Unavailable, use.TryUse().Outcome);
            policy.SetTransition(true); Assert.AreEqual(VitalUseOutcome.Unavailable, use.TryUse().Outcome); policy.SetTransition(false);
            policy.SetReady(false); Assert.AreEqual(VitalUseOutcome.Unavailable, use.TryUse().Outcome); policy.SetReady(true);
            Assert.AreEqual(2, hotbar.GetSlot(0).Stack.Quantity); Assert.AreEqual(0, target.Applies);
            use.Dispose(); Assert.AreEqual(VitalUseOutcome.Unavailable, use.TryUse().Outcome);
            commands.Unregister(hotbar.Handle); Assert.AreEqual(-1, selection.ActiveIndex);
            Assert.AreEqual(VitalUseOutcome.Unavailable, use.TryUse().Outcome);
        }
        [Test] public void FaultyObserversSeeCompletedStateAndCannotReenterAfterLastStack()
        {
            commands.Grant(hotbar.Handle, "food", 1); selection.Select(0); int errors = 0; bool coherent = true;
            commands.ObserverError += _ => errors++;
            commands.Completed += _ => { coherent &= hotbar.GetSlot(0).IsEmpty && target.Applies == 1 && use.TryUse().Outcome == VitalUseOutcome.Busy; throw new Exception("inventory"); };
            target.Publish = () => { coherent &= selection.ActiveIndex == -1 && use.TryUse().Outcome == VitalUseOutcome.Busy; throw new Exception("vital"); };
            Assert.IsTrue(use.TryUse().Committed); Assert.IsTrue(coherent); Assert.AreEqual(2, errors); Assert.AreEqual(1, target.Applies);
            Assert.AreEqual(VitalUseOutcome.NoSelection, use.TryUse().Outcome);
        }
        [Test] public void MismatchedReaderCannotConsumeFromAnotherCommandService()
        {
            var other = new ContainerCommandService(new ItemCatalog(new[] { new ItemSpec("food", true, 10) }));
            var alien = other.Register("hotbar", 1); other.Grant(alien.Handle, "food", 1);
            using var alienSelection = new HotbarSelectionState(alien); alienSelection.Select(0);
            using var broken = new SelectedVitalItemUseCoordinator(commands, alien, alienSelection, new VitalConsumableCatalog(new[] { new VitalConsumableSpec("food", VitalConsumableKind.Food, 25) }), target, policy);
            Assert.AreEqual(VitalUseOutcome.Unavailable, broken.TryUse().Outcome); Assert.AreEqual(1, alien.GetSlot(0).Stack.Quantity); Assert.AreEqual(0, target.Applies);
        }
    }
}
