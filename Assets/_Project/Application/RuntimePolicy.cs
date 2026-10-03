using System;
using System.Collections.Generic;
using SOLITUDE.Containers;
namespace SOLITUDE.Application
{
    public sealed class PauseCoordinator
    {
        private readonly HashSet<Guid> leases = new HashSet<Guid>();
        private float baseScale = 1;
        public float Scale => leases.Count == 0 ? baseScale : 0;
        public bool IsPaused => Scale == 0;
        public event Action Changed;
        public IDisposable Acquire()
        {
            var id = Guid.NewGuid(); leases.Add(id); Changed?.Invoke();
            return new Lease(() => { if (leases.Remove(id)) Changed?.Invoke(); });
        }
        public void SetBaseScale(float scale)
        {
            if (float.IsNaN(scale) || float.IsInfinity(scale) || scale < 0 || scale > 10) throw new ArgumentOutOfRangeException(nameof(scale));
            if (scale == baseScale) return; baseScale = scale; Changed?.Invoke();
        }
    }
    public sealed class Lease : IDisposable
    {
        private Action release;
        public Lease(Action release) => this.release = release;
        public void Dispose() { var action = release; release = null; action?.Invoke(); }
    }
    public sealed class InputPolicyCoordinator
    {
        private readonly PauseCoordinator pause;
        private bool ready, transition, modal;
        public bool Ready => ready && !transition;
        public bool Gameplay => Ready && !modal && !pause.IsPaused;
        public bool Pointer => Ready && (modal || !pause.IsPaused);
        public bool Inventory => Ready && (modal || !pause.IsPaused);
        public event Action Changed;
        public InputPolicyCoordinator(PauseCoordinator pause) { this.pause = pause; pause.Changed += Publish; }
        public void SetReady(bool value) { ready = value; Publish(); }
        public void SetTransition(bool value) { transition = value; Publish(); }
        internal void SetModal(bool value) { modal = value; Publish(); }
        private void Publish() => Changed?.Invoke();
        public void Dispose() => pause.Changed -= Publish;
    }
    public enum ContainerModalKind { Closed, Inventory, Transfer }
    public enum OpenResult { Opened, Closed, Unavailable, BindingFailed }
    public interface IModalBinding : IDisposable { void Activate(); }
    public interface IContainerModalDisplay
    {
        IModalBinding Prepare(ContainerModalKind kind, IContainerReader inventory, IContainerReader target);
    }
    public sealed class ContainerModalCoordinator : IDisposable
    {
        private readonly InputPolicyCoordinator policy;
        private readonly PauseCoordinator pause;
        private readonly ContainerGestureCoordinator gestures;
        private readonly IContainerModalDisplay display;
        private readonly IContainerReader inventory;
        private IDisposable pauseLease;
        private IModalBinding binding;
        private IContainerReader target;
        public ContainerModalKind Kind { get; private set; }
        public ContainerHandle? Target => target?.Handle;
        public event Action Changed;
        public ContainerModalCoordinator(InputPolicyCoordinator policy, PauseCoordinator pause, ContainerGestureCoordinator gestures,
            IContainerModalDisplay display, IContainerReader inventory)
        { this.policy = policy; this.pause = pause; this.gestures = gestures; this.display = display; this.inventory = inventory; }
        public OpenResult ToggleInventory()
        {
            if (Kind != ContainerModalKind.Closed) { Close(); return OpenResult.Closed; }
            return Open(ContainerModalKind.Inventory, null);
        }
        public OpenResult OpenTarget(IContainerReader reader)
        {
            if (reader != null && Target.HasValue && Target.Value.Equals(reader.Handle)) { Close(); return OpenResult.Closed; }
            return Open(ContainerModalKind.Transfer, reader);
        }
        private OpenResult Open(ContainerModalKind kind, IContainerReader reader)
        {
            if (!policy.Ready || inventory == null || !inventory.IsAvailable || (kind == ContainerModalKind.Transfer && (reader == null || !reader.IsAvailable)))
                return OpenResult.Unavailable;
            IModalBinding next;
            try { next = display.Prepare(kind, inventory, reader); }
            catch { return OpenResult.BindingFailed; }
            if (next == null) return OpenResult.BindingFailed;
            var lease = pause.Acquire();
            policy.SetModal(true);
            try { next.Activate(); }
            catch
            {
                next.Dispose(); lease.Dispose(); policy.SetModal(Kind != ContainerModalKind.Closed);
                return OpenResult.BindingFailed;
            }
            gestures.Cancel();
            binding?.Dispose(); pauseLease?.Dispose(); Unwatch();
            binding = next; pauseLease = lease; target = reader; Kind = kind;
            inventory.CapacityChanged += OnAvailability;
            if (target != null) target.CapacityChanged += OnAvailability;
            Changed?.Invoke(); return OpenResult.Opened;
        }
        public bool Back()
        {
            if (gestures.Interaction.IsHolding) { gestures.Cancel(); return true; }
            if (Kind == ContainerModalKind.Closed) return false;
            Close(); return true;
        }
        public void CloseTarget(ContainerHandle handle) { if (Target.HasValue && Target.Value.Equals(handle)) Close(); }
        private void OnAvailability() { if (!inventory.IsAvailable || (target != null && !target.IsAvailable)) Close(); }
        private void Unwatch()
        {
            if (inventory != null) inventory.CapacityChanged -= OnAvailability;
            if (target != null) target.CapacityChanged -= OnAvailability;
        }
        public void Close()
        {
            if (Kind == ContainerModalKind.Closed && binding == null) return;
            gestures.Cancel(); Unwatch(); target = null; Kind = ContainerModalKind.Closed;
            binding?.Dispose(); binding = null;
            // Keep modal gating until the lease is released, avoiding transient gameplay.
            pauseLease?.Dispose(); pauseLease = null; policy.SetModal(false); Changed?.Invoke();
        }
        public void Dispose() => Close();
    }
    public sealed class ContainerGestureCoordinator
    {
        private readonly InputPolicyCoordinator policy;
        private string panel;
        public ContainerInteractionController Interaction { get; }
        public ContainerGestureCoordinator(ContainerInteractionController interaction, InputPolicyCoordinator policy)
        { Interaction = interaction; this.policy = policy; }
        public void Begin(string owner, SlotSnapshot slot)
        {
            if (!policy.Pointer || Interaction.IsHolding) return;
            Interaction.BeginDrag(slot); if (Interaction.IsHolding) panel = owner;
        }
        public void Drop(SlotSnapshot target) { if (policy.Pointer) Interaction.Drop(target); }
        public void End(string owner) { if (panel == owner) Cancel(); }
        public void Unbind(string owner) { if (panel == owner) Cancel(); }
        public void Cancel() { panel = null; Interaction.Cancel(); }
    }
    public sealed class HotbarSelectionState : IDisposable
    {
        private readonly IContainerReader reader;
        public int ActiveIndex { get; private set; } = -1;
        public event Action<int> Changed;
        public HotbarSelectionState(IContainerReader reader)
        { this.reader = reader; reader.SlotChanged += SlotChanged; reader.CapacityChanged += CapacityChanged; }
        public void Select(int index)
        {
            if (!reader.IsAvailable || index < -1 || index >= reader.Capacity || index == ActiveIndex) return;
            ActiveIndex = index; Changed?.Invoke(index);
        }
        private void SlotChanged(int index) { if (ActiveIndex == index && (reader.GetSlot(index)?.IsEmpty ?? true)) Clear(); }
        private void CapacityChanged() { if (!reader.IsAvailable || ActiveIndex >= reader.Capacity) Clear(); }
        private void Clear() { if (ActiveIndex == -1) return; ActiveIndex = -1; Changed?.Invoke(-1); }
        public void Dispose() { reader.SlotChanged -= SlotChanged; reader.CapacityChanged -= CapacityChanged; }
    }
}
