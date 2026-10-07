using System;
using System.Linq;
using SOLITUDE.Containers;
using SOLITUDE.Items;
using SOLITUDE.Restoration;
using SOLITUDE.SaveLoad;
using UnityEngine;
namespace SOLITUDE.World.ScrubberEncounter
{
    public sealed class ScrubberEquipment : MonoBehaviour
    {
        // Stable GUID-derived identity of the authored encounter fuse.
        public const string FuseItemId = "4716e84c715564ad2ac79c4be74037dd";
        private readonly ScrubberEncounterState state = new ScrubberEncounterState();
        private ContainerCommandService commands;
        private IContainerReader inventory;
        public ScrubberEncounterState State => state;
        public event Action Changed;
        private bool Available => commands != null && inventory != null && inventory.IsAvailable && inventory.Handle.Id == ContainerSaveSession.PlayerInventoryId && ReferenceEquals(commands.Read(inventory.Handle),inventory);
        public string InstallReason => state.FuseInstalled ? "Fuse already installed." : !Available ? "Installation unavailable; no item installed." :
            inventory.GetSlots().Any(slot=>!slot.IsEmpty && slot.Stack.ItemId==FuseItemId) ? "" : "Requires 1 replacement fuse in inventory.";
        public void Initialize(ContainerCommandService service,IContainerReader playerInventory,ItemPresentationCatalog catalog)
        {
            ReleaseBinding();
            if (catalog?.Resolve(FuseItemId)==null) return;
            commands=service; inventory=playerInventory;
            if (inventory!=null) { inventory.SlotChanged+=SlotChanged; inventory.CapacityChanged+=Publish; }
            state.Changed+=Publish;
        }
        private void SlotChanged(int _) => Publish();
        private void Publish() => Changed?.Invoke();
        public string Install()
        {
            switch(state.Install(commands,inventory,FuseItemId))
            {
                case RestorationOutcome.Restored: return "Fuse replaced. Restart scrubber.";
                case RestorationOutcome.AlreadyRestored: return "Fuse already installed.";
                case RestorationOutcome.MissingItem: return "Requires 1 replacement fuse in inventory.";
                default: return "Installation unavailable; no item installed.";
            }
        }
        public string Restart()
        {
            if(!Available || !isActiveAndEnabled) return "Restart unavailable.";
            return state.TryRestart() ? "Scrubber restarted. Room air recovering." : state.RestartFeedback;
        }
        private void Update() { if(Available) state.Advance(Time.deltaTime); }
        public void ReleaseBinding()
        {
            if(inventory!=null) { inventory.SlotChanged-=SlotChanged; inventory.CapacityChanged-=Publish; }
            state.Changed-=Publish; commands=null; inventory=null;
        }
        private void OnDestroy() => ReleaseBinding();
    }
}
