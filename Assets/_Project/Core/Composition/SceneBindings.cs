using System;
using System.Collections.Generic;
using SOLITUDE.Application;
using SOLITUDE.Containers;
using SOLITUDE.Core.Input;
using SOLITUDE.Core.Systems;
using SOLITUDE.Features.Interactables;
using SOLITUDE.Items;
using SOLITUDE.Player;
using SOLITUDE.SaveLoad;
using UnityEngine;
namespace SOLITUDE.Composition
{
    public sealed class SceneBindings : MonoBehaviour, IContainerModalDisplay
    {
        [SerializeField] private RecoveryNoticeView recoveryNotice;
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private SOLITUDE.Hotbar.Hotbar hotbar;
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private ContainerModalView inventoryModal;
        [SerializeField] private ContainerTransferModalView transferModal;
        [SerializeField] private ContainerController[] controllers = new ContainerController[0];
        [SerializeField] private ContainerHeldItemView heldView;
        [SerializeField] private ContainerTooltipView[] tooltips = new ContainerTooltipView[0];
        [SerializeField] private LockerContainer[] lockers = new LockerContainer[0];
        [SerializeField] private WorldPickup[] worldPickups = new WorldPickup[0];
        private readonly List<ContainerSaveSession.ContainerBinding> bindings = new();
        private readonly List<WorldPickup> registeredPickups = new();
        [SerializeField] private UnityEngine.InputSystem.UI.InputSystemUIInputModule uiInput;
        private IDisposable uiLease;
        private ItemPresentationCatalog presentations;
        private ContainerSaveSession session;
        private PickupCollectionService pickups;
        private Dictionary<LootTable, RuntimeLootTable> loot;
        private InputPolicyCoordinator policy;
        private UnityInputHost input;
        private ContainerModalCoordinator modal;
        private HotbarSelectionState selection;
        private HeldItemPresenter held;
        private Action released;
        private Guid inventoryToken, transferToken;
        public bool HasPlayer => inventory != null;
        public void Validate()
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var source in lockers)
                if (source == null || string.IsNullOrWhiteSpace(source.PersistentId) || !ids.Add(source.PersistentId)) throw new InvalidOperationException("Invalid/duplicate scene locker reference.");
            var pickupIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var pickup in worldPickups)
                if (pickup == null || string.IsNullOrWhiteSpace(pickup.PickupId) || !pickupIds.Add(pickup.PickupId)) throw new InvalidOperationException("Invalid/duplicate scene pickup reference.");
            if (!HasPlayer) return;
            if (recoveryNotice == null) throw new InvalidOperationException("Recovery notice is missing.");
            recoveryNotice.ValidateAuthoring();
            if (hotbar == null || movement == null || interactor == null || inventoryModal == null || transferModal == null || heldView == null || uiInput == null)
                throw new InvalidOperationException("Player scene endpoints are incomplete.");
            if (!inventoryModal.IsConfigured || !transferModal.IsConfigured) throw new InvalidOperationException("Modal root is missing.");
            if (inventoryModal.Controller == null || transferModal.InventoryController == null || transferModal.TargetController == null)
                throw new InvalidOperationException("Modal panels are incomplete.");
            var unique = new HashSet<ContainerController>();
            foreach (var controller in controllers)
            {
                if (controller == null || !unique.Add(controller)) throw new InvalidOperationException("Invalid/duplicate scene presenter endpoint.");
                controller.ValidateAuthoring();
            }
            var uniqueTooltips = new HashSet<ContainerTooltipView>();
            foreach (var tooltip in tooltips)
            {
                if (tooltip == null || !uniqueTooltips.Add(tooltip)) throw new InvalidOperationException("Invalid/duplicate tooltip endpoint.");
                tooltip.ValidateAuthoring();
            }
            heldView.ValidateAuthoring();
            if (!unique.Contains(inventoryModal.Controller) || !unique.Contains(transferModal.InventoryController) || !unique.Contains(transferModal.TargetController))
                throw new InvalidOperationException("Every modal panel must be a configured presenter endpoint.");
        }
        public void Initialize(ContainerSaveSession session, PickupCollectionService pickups, ItemPresentationCatalog presentations,
            Dictionary<LootTable, RuntimeLootTable> loot, ContainerGestureCoordinator gestures, InputPolicyCoordinator policy,
            PauseCoordinator pause, IContainerScreenRequests screens, UnityInputHost input, GameManager game, Action released)
        {
            Validate(); this.presentations = presentations; this.session = session; this.pickups = pickups; this.loot = loot; this.policy = policy; this.input = input; this.released = released;
            try
            {
                foreach (var source in lockers)
                {
                    if (source.Loot != null && !loot.ContainsKey(source.Loot)) throw new InvalidOperationException("Locker loot table is not configured.");
                    source.Initialize(OpenOwner, (handle, data) => session.Restore(handle, data));
                    // Saved owners must be compatible before backup promotion or input.
                    // Unvisited lockers remain lazy and do not generate loot here.
                    if(session.TryGetContainer(source.PersistentId,out _) && !source.EnsureInitialized())
                        throw new System.IO.InvalidDataException("Saved locker cannot fit its authored capacity: "+source.PersistentId);
                    source.GetComponent<Locker>()?.Initialize(screens);
                }
                foreach (var pickup in worldPickups) RegisterPickup(pickup);
                if (!HasPlayer) return;
                recoveryNotice.Initialize();
                var inventoryBinding = session.Register(ContainerSaveSession.PlayerInventoryId, inventory.Capacity); bindings.Add(inventoryBinding); inventory.Initialize(inventoryBinding.Container);
                var hotbarBinding = session.Register(ContainerSaveSession.PlayerHotbarId, hotbar.Capacity); bindings.Add(hotbarBinding);
                selection = new HotbarSelectionState(hotbarBinding.Container); hotbar.Initialize(hotbarBinding.Container, selection);
                foreach (var controller in controllers)
                    controller.Initialize(presentations, gestures, policy, controller.DefaultSource == hotbar ? selection : null);
                foreach (var tooltip in tooltips) tooltip.Initialize(presentations);
                heldView.Initialize(presentations); held = new HeldItemPresenter(gestures.Interaction, heldView);
                modal = new ContainerModalCoordinator(policy, pause, gestures, this, inventory.Container);
                inventoryModal.Initialize(() => modal.ToggleInventory(), () => modal.Close());
                transferModal.Initialize(source => source is LockerContainer locker && Open(locker), () => modal.Close());
                inventoryModal.RenderVisible(false); transferModal.RenderVisible(false);
                foreach (var controller in controllers)
                    if (controller != inventoryModal.Controller && controller != transferModal.InventoryController && controller != transferModal.TargetController && controller.DefaultSource != null)
                        controller.BindDefaultSource();
                uiLease = input.ConnectUI(uiInput);
                input.Point += Point;
                input.Bind(movement, interactor, selection, () => modal.ToggleInventory(), () => modal.Back(), game.TogglePause);
            }
            catch { Release(); throw; }
        }
        public IDisposable RegisterPickup(WorldPickup pickup)
        {
            if (pickups == null || pickup == null || pickup.Definition?.Item == null || pickup.Definition.Quantity < 1 || presentations.Resolve(pickup.Definition.Item.ItemId) == null)
                throw new InvalidOperationException("Pickup composition is unavailable or invalid.");
            if (registeredPickups.Contains(pickup)) return new Lease(() => { });
            // Resolve through the frozen session catalog via a validated reward registration/command.
            if (!pickup.Initialize(pickups)) throw new InvalidOperationException("Pickup registration failed: " + pickup.PickupId);
            registeredPickups.Add(pickup);
            return new Lease(() => { pickup.ReleaseBinding(); registeredPickups.Remove(pickup); });
        }
        private IContainerReader OpenOwner(LockerContainer source)
        {
            bool saved = session.TryGetContainer(source.PersistentId, out _);
            var candidates = !saved && source.Loot != null ? loot[source.Loot].Roll(new System.Random(SeedUtility.DeriveSeed(session.WorldSeed, source.PersistentId))) : null;
            var binding = session.Register(source.PersistentId, source.Capacity, initialLoot: candidates); bindings.Add(binding); return binding.Container;
        }
        public void CloseForTransition() => modal?.Close();
        public void ShowRecoveryNotice(Action acknowledged) => recoveryNotice.Show(acknowledged);
        public bool Open(LockerContainer source)
        {
            if (modal == null || source == null || !source.EnsureInitialized()) return false;
            var result = modal.OpenTarget(source.Container); return result == OpenResult.Opened || result == OpenResult.Closed;
        }
        public void Close(LockerContainer source) { if (source?.Container != null) modal?.CloseTarget(source.Container.Handle); }
        private sealed class ReaderSource : IContainerSource
        {
            public IContainerReader Container { get; }
            public string Label { get; }
            public ReaderSource(IContainerReader reader, string label) { Container = reader; Label = label; }
        }
        public IModalBinding Prepare(ContainerModalKind kind, IContainerReader inventory, IContainerReader target)
        {
            if (!inventory.IsAvailable || (kind == ContainerModalKind.Transfer && target?.IsAvailable != true)) return null;
            return new PreparedModal(this, kind, new ReaderSource(inventory, "Inventory"), target == null ? null : new ReaderSource(target, "Locker"));
        }
        private sealed class PreparedModal : IModalBinding
        {
            private SceneBindings scope;
            private readonly ContainerModalKind kind;
            private readonly IContainerSource inventory, target;
            private readonly ContainerController first, second;
            private readonly Guid token = Guid.NewGuid();
            private bool active;
            public PreparedModal(SceneBindings scope, ContainerModalKind kind, IContainerSource inventory, IContainerSource target)
            {
                this.scope = scope; this.kind = kind; this.inventory = inventory; this.target = target;
                first = kind == ContainerModalKind.Inventory ? scope.inventoryModal.Controller : scope.transferModal.InventoryController;
                second = kind == ContainerModalKind.Transfer ? scope.transferModal.TargetController : null;
            }
            public void Activate()
            {
                var previousFirst = first.CurrentSource;
                var previousSecond = second?.CurrentSource;
                try
                {
                    first.Bind(inventory);
                    second?.Bind(target);
                    if (kind == ContainerModalKind.Inventory)
                    { scope.inventoryModal.RenderVisible(true); scope.inventoryToken = token; }
                    else
                    { scope.transferModal.RenderVisible(true); scope.transferToken = token; }
                    active = true;
                }
                catch
                {
                    first.Unbind(); second?.Unbind();
                    if (previousFirst != null) first.Bind(previousFirst);
                    if (previousSecond != null) second.Bind(previousSecond);
                    throw;
                }
            }
            public void Dispose()
            {
                if (scope == null) return;
                if (active && kind == ContainerModalKind.Inventory && scope.inventoryToken == token)
                { first.Unbind(); if (scope.inventoryModal != null) scope.inventoryModal.RenderVisible(false); }
                if (active && kind == ContainerModalKind.Transfer && scope.transferToken == token)
                { first.Unbind(); second.Unbind(); if (scope.transferModal != null) scope.transferModal.RenderVisible(false); }
                scope = null;
            }
        }
        private void Point(float x, float y)
        { held?.Point(x, y); foreach (var tooltip in tooltips) tooltip.SetPosition(new Vector2(x, y)); }
        public void Release()
        {
            if (session == null) return;
            modal?.Dispose(); modal = null;
            if (input != null && selection != null) { input.Point -= Point; input.Bind(null, null, null, null, null, null); }
            uiLease?.Dispose(); uiLease = null;
            held?.Dispose(); held = null;
            foreach (var controller in controllers) if (controller != null) controller.Unbind();
            selection?.Dispose(); selection = null;
            foreach (var pickup in registeredPickups) pickup.ReleaseBinding(); registeredPickups.Clear();
            foreach (var source in lockers) { source.Release(); if (source != null) source.GetComponent<Locker>()?.Release(); }
            foreach (var binding in bindings) binding.Dispose(); bindings.Clear();
            inventory?.Release(); hotbar?.Release(); session = null;
        }
        private void OnDestroy() { var notify = released; released = null; notify?.Invoke(); Release(); }
    }
}
