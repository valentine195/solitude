using UnityEngine;

namespace SOLITUDE.Containers
{
    [RequireComponent(typeof(ContainerView))]
    public class ContainerController : MonoBehaviour
    {
        [SerializeField] private ContainerView view;
        [SerializeField] private ContainerTooltipView tooltip;

        [Tooltip("Only needed for a panel permanently bound to one fixed source, e.g. the player's own inventory. Leave empty for a shared/reusable panel (Locker, Chest) - callers rebind it at runtime via Bind(source) each time it opens, since the same panel is reused across many world objects that can't all be wired into one Inspector field.")]
        [SerializeField] private MonoBehaviour defaultContainerSource;

        private IContainerSource currentSource;
        private IContainer container;
        private ContainerInteractionController interaction;
        private bool isBound;

        private void Awake()
        {
            // Fall back to sibling components so most prefabs need only
            // containerSource wired up by hand.
            if (view == null) view = GetComponent<ContainerView>();
        }

        // Called by its owning ContainerPanel. Safe while inactive: the source
        // is remembered and bound by OnEnable when the modal opens.
        public void Bind(IContainerSource source)
        {
            if (ReferenceEquals(currentSource, source) && isBound) return;

            if (isActiveAndEnabled)
                UnbindCurrentSource(cancelHeldItem: true);

            currentSource = source;
            if (isActiveAndEnabled)
                BindToCurrentSource();
        }

        public void Unbind()
        {
            UnbindCurrentSource(cancelHeldItem: true);
            currentSource = null;
        }

        /// <summary>Uses this panel's Inspector-wired fixed source.</summary>
        public bool BindDefaultSource()
        {
            var source = defaultContainerSource as IContainerSource;
            if (source == null)
            {
                Debug.LogError($"[{nameof(ContainerController)}] No valid Default Container Source is assigned.", this);
                return false;
            }

            Bind(source);
            return true;
        }

        private void OnEnable()
        {
            // Only fall back to the design-time default if nothing has been
            // explicitly Bind()'d yet - covers the "always the same source"
            // case (player inventory) without requiring every caller to
            // Bind() something that never changes.
            if (currentSource == null && defaultContainerSource != null)
                currentSource = defaultContainerSource as IContainerSource;

            BindToCurrentSource();
        }

        private void BindToCurrentSource()
        {

            container = currentSource?.Container;
            if (container == null)
            {
                Debug.LogError(
                    $"{nameof(ContainerController)}: no source bound (neither Bind() was called nor is a Default Container Source assigned).",
                    this);
                return;
            }

            if (view == null)
            {
                Debug.LogError($"{nameof(ContainerController)}: no ContainerView assigned or found.", this);
                return;
            }

            if (ContainerInteractionHub.Instance == null)
            {
                Debug.LogError(
                    $"{nameof(ContainerController)}: no {nameof(ContainerInteractionHub)} found in the scene. " +
                    "Add one instance of it somewhere persistent (e.g. your UI root).",
                    this);
                return;
            }

            // Shared across every open container - NOT constructed here -
            // so an item can be picked up in one container and placed in
            // another. See ContainerInteractionHub.
            interaction = ContainerInteractionHub.Instance.Interaction;

            view.Bind(currentSource);
            view.SlotHovered += OnSlotHovered;
            view.SlotUnhovered += OnSlotUnhovered;
            view.SlotSelected += OnSlotSelected;

            // Point is the only per-container concern left here (positioning
            // this container's own tooltip) - Click was never wired to
            // anything, and Cancel is now handled globally by the Hub so it
            // still works even while no container window is open at all.
            // There's exactly one ContainerInputBridge for the whole game
            // now (owned by the Hub), so subscribing here doesn't enable a
            // second copy of the "Container" action map.
            ContainerInteractionHub.Instance.Input.Point += OnPoint;
            isBound = true;
        }

        private void OnDisable()
        {
            UnbindCurrentSource(cancelHeldItem: true);
        }

        private void UnbindCurrentSource(bool cancelHeldItem)
        {
            if (view != null)
            {
                view.SlotHovered -= OnSlotHovered;
                view.SlotUnhovered -= OnSlotUnhovered;
                view.SlotSelected -= OnSlotSelected;
                view.Unbind();
            }

            tooltip?.Hide();

            var hub = ContainerInteractionHub.Instance;
            if (hub != null)
            {
                hub.Input.Point -= OnPoint;

                // If this container is closing while it's the source of the
                // active hold, resolve that hold back into (what is now) an
                // empty origin slot rather than leaving a phantom item held
                // with no visible container left to drop it into. The global
                // Escape cancel (see ContainerInteractionHub) covers "the
                // player presses Escape"; this covers "the player just
                // closed the window" without requiring that extra step.
                if (cancelHeldItem && interaction != null && interaction.IsHolding && OwnsSlot(interaction.OriginSlot))
                    interaction.Cancel();
            }

            interaction = null;
            container = null;
            isBound = false;
        }

        private bool OwnsSlot(ContainerSlot slot)
        {
            if (slot == null || container == null) return false;

            var slots = container.GetSlots();
            for (int i = 0; i < slots.Count; i++)
            {
                if (ReferenceEquals(slots[i], slot)) return true;
            }

            return false;
        }

        private void OnSlotHovered(int index)
        {
            var slot = container.GetSlot(index);
            interaction?.SetHover(slot);

            if (slot.IsEmpty)
                tooltip?.Hide();
            else
                tooltip?.Show(slot.Definition);
        }

        private void OnSlotUnhovered(int index)
        {
            interaction?.SetHover(null);
            tooltip?.Hide();
        }

        // Click no longer drives item movement (that's now a drag gesture -
        // see ContainerSlotView/ContainerInteractionHub). This is here as the
        // hook point for future selection-based actions (a "use"/"drop"
        // hotkey acting on whatever's selected, a context menu, etc).
        private void OnSlotSelected(int index) { }

        private void OnPoint(Vector2 pos) => tooltip?.SetPosition(pos);
    }
}
