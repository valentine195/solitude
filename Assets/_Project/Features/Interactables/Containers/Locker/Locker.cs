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
            if (containerSource == null) containerSource = GetComponent<LockerContainer>();
            if (sensor != null) sensor.OnLeave += HandleLeave;
        }

        private void OnDestroy()
        {
            if (sensor != null) sensor.OnLeave -= HandleLeave;
        }
        public override string GetPrompt() => "Open locker";

        private void HandleLeave(bool left)
        {
            ContainerUIController.Instance?.GetView(ContainerUIType.Locker)?.Close();
        }

        public override InteractionResult Interact(PlayerInteractor player)
        {
            if (containerSource == null)
                return InteractionResult.Blocked("Locker is unavailable");

            var uiController = ContainerUIController.Instance;
            if (uiController == null)
                return InteractionResult.Blocked("Locker UI is unavailable");

            var lockerView = uiController.GetView(ContainerUIType.Locker);
            if (lockerView == null)
                return InteractionResult.Blocked("Locker UI is unavailable");

            // Pass this locker's own source, not just "open" - the modal is
            // shared across every Locker in the world, so it has to be told
            // which Container to bind on this specific open.
            if (lockerView.IsOpen)
            {
                lockerView.Close();
            }
            else
            {
                if (!containerSource.EnsureInitialized())
                    return InteractionResult.Blocked("Locker contents could not be loaded");

                lockerView.Open(containerSource);
            }
            return InteractionResult.Success();
        }
    }
}
