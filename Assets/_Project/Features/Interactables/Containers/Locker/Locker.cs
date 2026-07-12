using UnityEngine;
using SOLITUDE.Core.Interaction;
using SOLITUDE.Containers;
using SOLITUDE.Player;

namespace SOLITUDE.Features.Interactables
{
    public class Locker : InteractableBase
    {
        [SerializeField] private InteractableProximitySensor sensor;
        [SerializeField] private LockerContainer containerSource;

        private void Awake()
        {
            sensor.OnLeave += HandleLeave;
            if (containerSource == null) containerSource = GetComponent<LockerContainer>();
        }
        public override string GetPrompt() => "Open locker";

        private void HandleLeave(bool left)
        {
            ContainerUIController.Instance.GetView(ContainerUIType.Locker)?.Close();
        }

        private bool viewOpened = false;
        public override InteractionResult Interact(PlayerInteractor player)
        {
            // Pass this locker's own source, not just "open" - the modal is
            // shared across every Locker in the world, so it has to be told
            // which Container to bind on this specific open.
            if (viewOpened)
            {
                ContainerUIController.Instance.GetView(ContainerUIType.Locker)?.Open(containerSource);
            }
            else
            {
                ContainerUIController.Instance.GetView(ContainerUIType.Locker)?.Close();
            }
            return InteractionResult.Success();
        }
    }
}