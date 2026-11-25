using System;
using System.Collections.Generic;
using SOLITUDE.Containers;
using SOLITUDE.Items;
using SOLITUDE.SaveLoad;

namespace SOLITUDE.Application
{
    public sealed class StartingLoadoutSpec
    {
        public IReadOnlyList<PickupReward> HotbarGrants { get; }
        public int SelectedSlot { get; }
        public StartingLoadoutSpec(IItemCatalog catalog, IEnumerable<PickupReward> grants, int selectedSlot)
        {
            if (catalog == null || grants == null || selectedSlot < 0) throw new ArgumentException("Invalid starting loadout.");
            var copy = new List<PickupReward>();
            foreach (var grant in grants)
            {
                if (grant.Quantity < 1 || !catalog.TryGet(grant.ItemId, out _)) throw new ArgumentException("Unknown item or invalid starting quantity.");
                copy.Add(grant);
            }
            if (copy.Count == 0) throw new ArgumentException("Starting loadout is empty.");
            HotbarGrants = copy.AsReadOnly(); SelectedSlot = selectedSlot;
        }
    }
    public enum StartingLoadoutOutcome { Ready, AlreadyInitialized, Busy, Rejected, PersistenceFailed }
    public sealed class StartingLoadoutCoordinator
    {
        private readonly ContainerSaveSession session;
        private readonly IContainerReader inventory, hotbar;
        private readonly HotbarSelectionState selection;
        private bool executing, selectionPending;
        private int pendingSlot;
        public CommandStatus LastCommandStatus { get; private set; }
        public string LastError { get; private set; }
        public StartingLoadoutCoordinator(ContainerSaveSession session, IContainerReader inventory,
            IContainerReader hotbar, HotbarSelectionState selection)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            this.hotbar = hotbar ?? throw new ArgumentNullException(nameof(hotbar));
            this.selection = selection ?? throw new ArgumentNullException(nameof(selection));
        }
        public StartingLoadoutOutcome Initialize(StartingLoadoutSpec spec, Func<bool> persist)
        {
            if (executing || session.IsCollecting) return StartingLoadoutOutcome.Busy;
            if (spec == null || persist == null) return StartingLoadoutOutcome.Rejected;
            executing = true; LastError = null;
            try
            {
                if (!session.StartingLoadoutInitialized)
                {
                    var result = session.InitializeStartingLoadout(inventory.Handle, hotbar.Handle, spec.HotbarGrants, spec.SelectedSlot);
                    LastCommandStatus = result.Status;
                    if (result.Status != CommandStatus.Applied) { LastError = result.Status.ToString(); return StartingLoadoutOutcome.Rejected; }
                    selectionPending = true; pendingSlot = spec.SelectedSlot;
                }
                if (!persist()) { LastError = "Starting equipment could not be saved."; return StartingLoadoutOutcome.PersistenceFailed; }
                if (!selectionPending) return StartingLoadoutOutcome.AlreadyInitialized;
                if (session.Equipment == null) selection.Select(pendingSlot);
                selectionPending = false;
                return StartingLoadoutOutcome.Ready;
            }
            catch (Exception error) { LastError = error.Message; return StartingLoadoutOutcome.PersistenceFailed; }
            finally { executing = false; }
        }
    }
}
