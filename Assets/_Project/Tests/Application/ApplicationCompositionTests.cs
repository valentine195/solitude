using System;
using System.Collections.Generic;
using NUnit.Framework;
using SOLITUDE.Application;
using SOLITUDE.Containers;
using SOLITUDE.Items;
using SOLITUDE.SaveLoad;
namespace SOLITUDE.Tests
{
    public class ApplicationCompositionTests
    {
        private PauseCoordinator pause;
        private InputPolicyCoordinator policy;
        private ContainerSaveSession session;
        private Container inventory, locker;
        private ContainerGestureCoordinator gestures;
        private Display display;
        private ContainerModalCoordinator modal;
        [SetUp] public void Setup()
        {
            pause = new PauseCoordinator(); policy = new InputPolicyCoordinator(pause); policy.SetReady(true);
            session = new ContainerSaveSession(new SaveGameData(), new ItemCatalog(new[] { new ItemSpec("a", true, 10), new ItemSpec("b", true, 10) }));
            inventory = session.Register("inventory", 3).Container; locker = session.Register("locker", 3).Container;
            gestures = new ContainerGestureCoordinator(new ContainerInteractionController(session.Commands), policy);
            display = new Display(); modal = new ContainerModalCoordinator(policy, pause, gestures, display, inventory);
        }
        [TearDown] public void TearDown() { modal.Dispose(); policy.Dispose(); }
        private sealed class Display : IContainerModalDisplay
        {
            public bool Fail, ThrowActivate;
            public int Open, Close;
            public IModalBinding Prepare(ContainerModalKind kind, IContainerReader inventory, IContainerReader target) => Fail ? null : new Binding(this);
            private sealed class Binding : IModalBinding
            {
                private Display owner;
                public Binding(Display owner) => this.owner = owner;
                public void Activate() { if (owner.ThrowActivate) throw new Exception("view failure"); owner.Open++; }
                public void Dispose() { if (owner == null) return; owner.Close++; owner = null; }
            }
        }
        [Test] public void BothScreensPauseAndGateGameplayButKeepPointerAndToggle()
        {
            Assert.AreEqual(OpenResult.Opened, modal.ToggleInventory());
            Assert.IsTrue(pause.IsPaused); Assert.IsFalse(policy.Gameplay); Assert.IsTrue(policy.Pointer); Assert.IsTrue(policy.Inventory);
            Assert.AreEqual(OpenResult.Closed, modal.ToggleInventory()); Assert.IsTrue(policy.Gameplay);
            Assert.AreEqual(OpenResult.Opened, modal.OpenTarget(locker)); Assert.AreEqual(0, pause.Scale); Assert.IsTrue(policy.Pointer);
            modal.Close(); Assert.IsTrue(policy.Gameplay); Assert.AreEqual(1, pause.Scale);
        }
        [Test] public void EscapeCancelsGestureBeforeClosingAndNeverChangesQuantities()
        {
            session.Commands.Grant(inventory.Handle, "a", 3); modal.ToggleInventory();
            gestures.Begin("inventory", inventory.GetSlot(0)); Assert.IsTrue(gestures.Interaction.IsHolding);
            Assert.IsTrue(modal.Back()); Assert.IsFalse(gestures.Interaction.IsHolding); Assert.AreEqual(ContainerModalKind.Inventory, modal.Kind);
            Assert.AreEqual(0, pause.Scale); Assert.AreEqual(3, inventory.GetSlot(0).Stack.Quantity);
            Assert.IsTrue(modal.Back()); Assert.AreEqual(ContainerModalKind.Closed, modal.Kind); Assert.IsFalse(modal.Back());
        }
        [Test] public void ClosingDoesNotResumeIndependentPauseOrLoseSlowMotion()
        {
            pause.SetBaseScale(0.2f); using var independent = pause.Acquire(); modal.OpenTarget(locker);
            modal.Close(); modal.Close(); Assert.AreEqual(0, pause.Scale);
            independent.Dispose(); Assert.AreEqual(0.2f, pause.Scale, 0.0001f);
        }
        [Test] public void ReplacementNeverEnablesGameplayBetweenScreens()
        {
            modal.ToggleInventory(); var observed = new List<bool>(); policy.Changed += () => observed.Add(policy.Gameplay);
            Assert.AreEqual(OpenResult.Opened, modal.OpenTarget(locker)); Assert.IsNotEmpty(observed); Assert.IsFalse(observed.Contains(true));
        }
        [Test] public void FailedReplacementPreservesOldModalAndPause()
        {
            modal.ToggleInventory(); display.Fail = true;
            Assert.AreEqual(OpenResult.BindingFailed, modal.OpenTarget(locker)); Assert.AreEqual(ContainerModalKind.Inventory, modal.Kind);
            Assert.AreEqual(0, pause.Scale); Assert.AreEqual(0, display.Close);
        }
        [Test] public void FailedActivationReleasesTemporaryPauseAndDoesNotOpenState()
        {
            display.ThrowActivate = true; Assert.AreEqual(OpenResult.BindingFailed, modal.ToggleInventory());
            Assert.AreEqual(ContainerModalKind.Closed, modal.Kind); Assert.AreEqual(1, pause.Scale); Assert.IsTrue(policy.Gameplay);
        }
        [Test] public void StaleProximityCloseCannotCloseReplacementTarget()
        {
            modal.OpenTarget(locker); var other = session.Register("other", 1).Container; modal.OpenTarget(other);
            modal.CloseTarget(locker.Handle); Assert.AreEqual(ContainerModalKind.Transfer, modal.Kind); Assert.AreEqual(other.Handle, modal.Target);
        }
        [Test] public void OwnerUnregisterClosesModalAndCancelsGesture()
        {
            var binding = session.Register("target", 1); session.Commands.Grant(inventory.Handle, "a", 2);
            modal.OpenTarget(binding.Container); gestures.Begin("source", inventory.GetSlot(0)); binding.Dispose();
            Assert.AreEqual(ContainerModalKind.Closed, modal.Kind); Assert.IsFalse(gestures.Interaction.IsHolding); Assert.IsTrue(policy.Gameplay);
        }
        [Test] public void UnreadyAndTransitionPoliciesRejectOpenAndInput()
        {
            policy.SetReady(false); Assert.AreEqual(OpenResult.Unavailable, modal.ToggleInventory()); Assert.IsFalse(policy.Pointer);
            policy.SetReady(true); policy.SetTransition(true); Assert.AreEqual(OpenResult.Unavailable, modal.OpenTarget(locker)); Assert.IsFalse(policy.Gameplay);
            policy.SetTransition(false); Assert.IsTrue(policy.Gameplay);
        }
        private sealed class View : IContainerDisplay, ITooltipDisplay
        {
            public event Action<SlotIntent> Hovered, Unhovered, Selected, BeginDrag, EndDrag, Drop;
            public long Generation;
            public int Capacity;
            public readonly Dictionary<int, SlotPresentation> Slots = new();
            public string Tooltip;
            public void Build(string label, int capacity, long binding) { Generation = binding; Capacity = capacity; Slots.Clear(); }
            public void Render(SlotPresentation slot) => Slots[slot.Index] = slot;
            public void Clear() { Slots.Clear(); }
            public void Show(string id) => Tooltip = id;
            public void Hide() => Tooltip = null;
            public void Begin(int index, long? version = null) => BeginDrag?.Invoke(new SlotIntent(index, version ?? Generation));
            public void Place(int index) => Drop?.Invoke(new SlotIntent(index, Generation));
            public void Hover(int index) => Hovered?.Invoke(new SlotIntent(index, Generation));
            public void Click(int index) => Selected?.Invoke(new SlotIntent(index, Generation));
        }
        [Test] public void PresentersTransferUsingRenderedAddressesAndKeepPartialRemainders()
        {
            session.Commands.Grant(inventory.Handle, "a", 5); session.Commands.Grant(locker.Handle, "a", 8);
            var left = new View(); var right = new View();
            using var p = new ContainerPresenter(inventory, left, "Inventory", gestures, policy, left);
            using var q = new ContainerPresenter(locker, right, "Locker", gestures, policy, right);
            left.Begin(0); right.Place(0);
            Assert.AreEqual(3, left.Slots[0].Quantity); Assert.AreEqual(10, right.Slots[0].Quantity);
            Assert.AreEqual(13, inventory.GetSlot(0).Stack.Quantity + locker.GetSlot(0).Stack.Quantity);
        }
        [Test] public void ResizeInvalidatesOldViewEventsAndClearsSelectionTooltip()
        {
            session.Commands.Grant(inventory.Handle, "a", 2); var view = new View();
            using var presenter = new ContainerPresenter(inventory, view, "Inventory", gestures, policy, view);
            view.Hover(0); view.Click(0); Assert.AreEqual("a", view.Tooltip); Assert.IsTrue(view.Slots[0].Selected);
            long old = view.Generation; session.Commands.Resize(inventory.Handle, 4);
            view.Begin(0, old); Assert.IsFalse(gestures.Interaction.IsHolding); Assert.IsNull(view.Tooltip); Assert.IsFalse(view.Slots[0].Selected);
            Assert.AreEqual(4, view.Capacity);
        }
        [Test] public void DisposedAndUnrelatedPanelCannotCancelAnotherPanelGesture()
        {
            session.Commands.Grant(inventory.Handle, "a", 1); var left = new View(); var right = new View();
            using var p = new ContainerPresenter(inventory, left, "Inventory", gestures, policy);
            var q = new ContainerPresenter(locker, right, "Locker", gestures, policy);
            left.Begin(0); q.Dispose(); Assert.IsTrue(gestures.Interaction.IsHolding);
            q.Dispose(); right.Begin(0); Assert.IsTrue(gestures.Interaction.IsHolding);
            p.Dispose(); Assert.IsFalse(gestures.Interaction.IsHolding); Assert.AreEqual(1, inventory.GetSlot(0).Stack.Quantity);
        }
        [Test] public void HotbarSelectionIsTransientAndClearsWhenEmptyOrShrunk()
        {
            using var selected = new HotbarSelectionState(inventory); int changes = 0; selected.Changed += _ => changes++;
            selected.Select(0); selected.Select(0); Assert.AreEqual(1, changes); // Existing empty-index selection is retained.
            session.Commands.Grant(inventory.Handle, "a", 1); session.Commands.Remove(inventory.GetSlot(0).Address, 1);
            Assert.AreEqual(-1, selected.ActiveIndex); selected.Select(2); session.Commands.Resize(inventory.Handle, 2); Assert.AreEqual(-1, selected.ActiveIndex);
            Assert.IsEmpty(session.Capture().containers[0].state.slots);
        }
        [Test] public void PresentersCannotIssueGameplayInputWhileIndependentPauseBlocksPointer()
        {
            session.Commands.Grant(inventory.Handle, "a", 1); var view = new View();
            using var presenter = new ContainerPresenter(inventory, view, "Inventory", gestures, policy);
            using var independent = pause.Acquire(); view.Begin(0); Assert.IsFalse(gestures.Interaction.IsHolding);
            independent.Dispose(); view.Begin(0); Assert.IsTrue(gestures.Interaction.IsHolding);
        }
        [Test] public void ApplicationAssemblyHasNoUnityReferences()
        {
            foreach (var assembly in typeof(PauseCoordinator).Assembly.GetReferencedAssemblies()) Assert.IsFalse(assembly.Name.StartsWith("Unity"));
        }
    }
}
