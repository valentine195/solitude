using SOLITUDE.Containers.Views;
using SOLITUDE.Modals;
using UnityEngine;

namespace SOLITUDE.Containers
{
    /// <summary>
    /// A single transfer session between the player's inventory and one
    /// contextual source (locker, crate, chest). The two visual endpoints are
    /// semantic panels, while ContainerInteractionHub remains the one shared
    /// owner of held-item state and container input.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ContainerTransferModalView : ModalView, IContainerView
    {
        [SerializeField] private ContainerUIType type = ContainerUIType.Locker;
        [SerializeField] private ContainerPanel playerInventoryPanel;
        [SerializeField] private ContainerPanel targetContainerPanel;

        public ContainerUIType Type => type;

        public void Open(IContainerSource targetSource)
        {
            if (targetSource == null)
            {
                Debug.LogError($"[{nameof(ContainerTransferModalView)}] Cannot open with a null target source.", this);
                return;
            }

            if (playerInventoryPanel == null || targetContainerPanel == null)
            {
                Debug.LogError(
                    $"[{nameof(ContainerTransferModalView)}] Assign Player Inventory Panel and Target Container Panel.",
                    this);
                return;
            }

            // Bind before activation. If the modal root is inactive, each panel
            // stores its source and its controller binds during OnEnable.
            // The inventory panel's controller owns its fixed PlayerInventory
            // adapter. This modal never needs a domain-object reference to it.
            if (!playerInventoryPanel.BindDefaultSource() || !targetContainerPanel.Bind(targetSource))
                return;

            base.Open();
        }

        public override void Toggle()
        {
            if (IsOpen) Close();
            else Debug.LogWarning($"[{nameof(ContainerTransferModalView)}] Open requires a target container source.", this);
        }
    }
}
