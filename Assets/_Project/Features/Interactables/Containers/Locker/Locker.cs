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

        private IContainerScreenRequests screens;
        public void Initialize(IContainerScreenRequests screens) => this.screens = screens;
        public void Release() => screens = null;
        private void HandleLeave(bool left) { if (left) screens?.Close(containerSource); }
        public override InteractionResult Interact(PlayerInteractor player) =>
            screens != null && screens.Open(containerSource) ? InteractionResult.Success() : InteractionResult.Blocked("Locker unavailable");
    }
}
