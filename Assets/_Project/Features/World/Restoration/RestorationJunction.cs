using SOLITUDE.Containers;
using SOLITUDE.Core.Interaction;
using SOLITUDE.Items;
using SOLITUDE.Player;
using SOLITUDE.Restoration;
using UnityEngine;

namespace SOLITUDE.World.Restoration
{
    public sealed class RestorationJunction : InteractableBase
    {
        [SerializeField] private RestorableSystem system;
        [SerializeField] private ItemDefinition requiredItem;
        private ContainerCommandService commands;
        private IContainerReader inventory;
        public void Configure(RestorableSystem target, ItemDefinition item) { system = target; requiredItem = item; }
        public void Initialize(ContainerCommandService service, IContainerReader playerInventory)
        { commands = service; inventory = playerInventory; }
        public void ReleaseBinding() { commands = null; inventory = null; }
        private bool Configured => system != null && requiredItem != null && !string.IsNullOrWhiteSpace(requiredItem.ItemId);
        private string Requirement => string.IsNullOrWhiteSpace(requiredItem.DisplayName) ? requiredItem.ItemId : requiredItem.DisplayName;
        public override string GetPrompt() => !Configured ? "System unavailable" :
            system.IsRestored ? "System restored" : "Offline — install 1 " + Requirement;
        public override bool CanInteract(PlayerInteractor player) => isActiveAndEnabled;
        public override InteractionResult Interact(PlayerInteractor player)
        {
            if (!isActiveAndEnabled || !Configured) return InteractionResult.Blocked("System unavailable");
            switch (system.State.TryRestore(commands, inventory, requiredItem.ItemId))
            {
                case RestorationOutcome.Restored: return InteractionResult.Success("System restored");
                case RestorationOutcome.AlreadyRestored: return InteractionResult.Blocked("System already restored");
                case RestorationOutcome.MissingItem: return InteractionResult.Blocked("Requires 1 " + Requirement + " in inventory");
                default: return InteractionResult.Blocked("Installation unavailable; no item installed");
            }
        }
    }
}
