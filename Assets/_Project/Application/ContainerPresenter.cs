using System;
using SOLITUDE.Containers;
namespace SOLITUDE.Application
{
    public readonly struct SlotPresentation
    {
        public int Index { get; }
        public string ItemId { get; }
        public int Quantity { get; }
        public bool Selected { get; }
        public bool Active { get; }
        public SlotPresentation(int index, string itemId, int quantity, bool selected = false, bool active = false)
        { Index = index; ItemId = itemId; Quantity = quantity; Selected = selected; Active = active; }
    }
    public readonly struct SlotIntent
    {
        public int Index { get; }
        public long Binding { get; }
        public SlotIntent(int index, long binding) { Index = index; Binding = binding; }
    }
    public interface IContainerDisplay
    {
        event Action<SlotIntent> Hovered;
        event Action<SlotIntent> Unhovered;
        event Action<SlotIntent> Selected;
        event Action<SlotIntent> BeginDrag;
        event Action<SlotIntent> EndDrag;
        event Action<SlotIntent> Drop;
        void Build(string label, int capacity, long binding);
        void Render(SlotPresentation slot);
        void Clear();
    }
    public interface ITooltipDisplay { void Show(string itemId); void Hide(); }
    public interface IHeldItemDisplay { void Render(string itemId, int quantity); void Point(float x, float y); }
    public sealed class HeldItemPresenter : IDisposable
    {
        private readonly ContainerInteractionController interaction;
        private readonly IHeldItemDisplay view;
        public HeldItemPresenter(ContainerInteractionController interaction, IHeldItemDisplay view)
        { this.interaction = interaction; this.view = view; interaction.HeldItemChanged += Changed; Changed(interaction.HeldItem); }
        private void Changed(ItemStack stack) => view.Render(stack?.ItemId, stack?.Quantity ?? 0);
        public void Point(float x, float y) => view.Point(x, y);
        public void Dispose() { interaction.HeldItemChanged -= Changed; view.Render(null, 0); }
    }
    public sealed class ContainerPresenter : IDisposable
    {
        private readonly IContainerReader reader;
        private readonly IContainerDisplay view;
        private readonly ITooltipDisplay tooltip;
        private readonly ContainerGestureCoordinator gestures;
        private readonly InputPolicyCoordinator policy;
        private readonly HotbarSelectionState selection;
        private readonly string label, panel = Guid.NewGuid().ToString();
        private SlotSnapshot[] snapshots;
        private static long nextGeneration;
        private long generation;
        private int selected = -1, hovered = -1;
        private bool disposed;
        public ContainerPresenter(IContainerReader reader, IContainerDisplay view, string label, ContainerGestureCoordinator gestures,
            InputPolicyCoordinator policy, ITooltipDisplay tooltip = null, HotbarSelectionState selection = null)
        {
            this.reader = reader; this.view = view; this.label = label; this.gestures = gestures;
            this.policy = policy; this.tooltip = tooltip; this.selection = selection;
            reader.SlotChanged += Changed; reader.CapacityChanged += Rebuild;
            view.Hovered += Hover; view.Unhovered += Unhover; view.Selected += Select;
            view.BeginDrag += Begin; view.EndDrag += End; view.Drop += Drop;
            policy.Changed += PolicyChanged;
            if (selection != null) selection.Changed += ActiveChanged;
            try { Rebuild(); } catch { Dispose(); throw; }
        }
        private void Rebuild()
        {
            gestures.Unbind(panel); tooltip?.Hide(); selected = hovered = -1;
            generation = System.Threading.Interlocked.Increment(ref nextGeneration); snapshots = new SlotSnapshot[reader.IsAvailable ? reader.Capacity : 0];
            view.Build(label, snapshots.Length, generation);
            for (int i = 0; i < snapshots.Length; i++) Changed(i);
        }
        private void Changed(int index)
        {
            if (index < 0 || index >= snapshots.Length) return;
            var slot = reader.GetSlot(index); snapshots[index] = slot;
            view.Render(new SlotPresentation(index, slot?.Stack?.ItemId, slot?.Stack?.Quantity ?? 0, selected == index, selection?.ActiveIndex == index));
            if (hovered == index) { if (slot == null || slot.IsEmpty) tooltip?.Hide(); else tooltip?.Show(slot.Stack.ItemId); }
        }
        private bool Valid(SlotIntent intent) => !disposed && policy.Pointer && reader.IsAvailable && intent.Binding == generation && intent.Index >= 0 && intent.Index < snapshots.Length;
        private void Hover(SlotIntent intent)
        { if (!Valid(intent)) return; hovered = intent.Index; var stack = snapshots[hovered]?.Stack; if (stack == null) tooltip?.Hide(); else tooltip?.Show(stack.ItemId); }
        private void Unhover(SlotIntent intent) { if (!Valid(intent)) return; hovered = -1; tooltip?.Hide(); }
        private void Select(SlotIntent intent)
        { if (!Valid(intent)) return; int old = selected; selected = selected == intent.Index ? -1 : intent.Index; if (old >= 0) Changed(old); Changed(intent.Index); }
        private void Begin(SlotIntent intent) { if (Valid(intent)) gestures.Begin(panel, snapshots[intent.Index]); }
        private void Drop(SlotIntent intent) { if (Valid(intent)) gestures.Drop(snapshots[intent.Index]); }
        private void End(SlotIntent intent) { if (Valid(intent)) gestures.End(panel); }
        private void ActiveChanged(int _) { for (int i = 0; i < snapshots.Length; i++) Changed(i); }
        private void PolicyChanged() { if (!policy.Pointer) { gestures.Cancel(); hovered = -1; tooltip?.Hide(); } }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            reader.SlotChanged -= Changed; reader.CapacityChanged -= Rebuild;
            view.Hovered -= Hover; view.Unhovered -= Unhover; view.Selected -= Select;
            view.BeginDrag -= Begin; view.EndDrag -= End; view.Drop -= Drop;
            policy.Changed -= PolicyChanged;
            if (selection != null) selection.Changed -= ActiveChanged;
            gestures.Unbind(panel); tooltip?.Hide(); view.Clear();
        }
    }
}
