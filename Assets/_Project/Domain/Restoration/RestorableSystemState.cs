using System;
using SOLITUDE.Containers;
using SOLITUDE.SaveLoad;

namespace SOLITUDE.Restoration
{
    public enum RestorationOutcome { Restored, AlreadyRestored, MissingItem, Unavailable, Rejected, Busy }

    // One scene-owned state. No Unity dependencies or shared/global progress.
    public sealed class RestorableSystemState
    {
        public bool IsRestored { get; private set; }
        private bool restoring;
        public event Action Changed;
        public event Action<Exception> ObserverError;

        public RestorationOutcome TryRestore(ContainerCommandService commands, IContainerReader inventory, string itemId)
        {
            if (restoring) return RestorationOutcome.Busy;
            if (IsRestored) return RestorationOutcome.AlreadyRestored;
            if (commands == null || inventory == null || !inventory.IsAvailable ||
                inventory.Handle.Id != ContainerSaveSession.PlayerInventoryId ||
                !ReferenceEquals(commands.Read(inventory.Handle), inventory) || string.IsNullOrWhiteSpace(itemId))
                return RestorationOutcome.Unavailable;
            if (commands.IsBusy) return RestorationOutcome.Busy;
            restoring = true;
            try
            {
                foreach (var slot in inventory.GetSlots())
                {
                    if (slot.IsEmpty || slot.Stack.ItemId != itemId) continue;
                    // The command commits both facts before publishing inventory notifications.
                    var result = commands.Remove(slot.Address, 1, () => IsRestored = true);
                    if (result.Status != CommandStatus.Applied) return RestorationOutcome.Rejected;
                    Publish();
                    return RestorationOutcome.Restored;
                }
                return RestorationOutcome.MissingItem;
            }
            finally { restoring = false; }
        }

        private void Publish()
        {
            if (Changed == null) return;
            foreach (Action observer in Changed.GetInvocationList())
                try { observer(); }
                catch (Exception error)
                {
                    if (ObserverError == null) continue;
                    foreach (Action<Exception> report in ObserverError.GetInvocationList())
                        try { report(error); } catch { /* Reporting must not interrupt other consumers. */ }
                }
        }
    }
}
