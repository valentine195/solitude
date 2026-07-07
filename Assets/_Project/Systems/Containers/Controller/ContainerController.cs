using UnityEngine;

namespace SOLITUDE.Containers
{
    [RequireComponent(typeof(ContainerView))]
    public class ContainerController : MonoBehaviour
    {
        [SerializeField] private ContainerView view;
        [SerializeField] private ContainerTooltipView tooltip;

        // Must be a component that implements IContainer (e.g. PlayerInventory, Chest, Locker).
        // Unity can't serialize an interface reference directly, so this is
        // assigned in the inspector as the concrete MonoBehaviour and cast below.
        [SerializeField] private MonoBehaviour containerSource;

        private IContainer container;
        private ContainerInteractionController interaction;

        private void Awake()
        {
            // Fall back to sibling components so most prefabs need only
            // containerSource wired up by hand.
            if (view == null) view = GetComponent<ContainerView>();
        }

        private void OnEnable()
        {
            container = containerSource as IContainer;
            if (container == null)
            {
                Debug.LogError(
                    $"{nameof(ContainerController)}: containerSource does not implement IContainer.",
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

            view.Bind(container);
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
        }

        private void OnDisable()
        {
            if (view != null)
            {
                view.SlotHovered -= OnSlotHovered;
                view.SlotUnhovered -= OnSlotUnhovered;
                view.SlotSelected -= OnSlotSelected;
                view.Unbind();
            }

            tooltip?.Hide();

            if (ContainerInteractionHub.Instance != null)
            {
                ContainerInteractionHub.Instance.Input.Point -= OnPoint;

                // If this container is closing while it's the source of the
                // active hold, resolve that hold back into (what is now) an
                // empty origin slot rather than leaving a phantom item held
                // with no visible container left to drop it into. The global
                // Escape cancel (see ContainerInteractionHub) covers "the
                // player presses Escape"; this covers "the player just
                // closed the window" without requiring that extra step.
                if (interaction != null && interaction.IsHolding && OwnsSlot(interaction.OriginSlot))
                    interaction.Cancel();
            }
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
