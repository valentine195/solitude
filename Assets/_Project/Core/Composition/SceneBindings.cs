using System;
using System.Collections.Generic;
using System.Linq;
using SOLITUDE.Application;
using SOLITUDE.Core.Interaction;
using SOLITUDE.Containers;
using SOLITUDE.Core.Input;
using SOLITUDE.Core.Systems;
using SOLITUDE.Features.Interactables;
using SOLITUDE.Items;
using SOLITUDE.Player;
using SOLITUDE.SaveLoad;
using SOLITUDE.World.Restoration;
using SOLITUDE.Tasks;
using SOLITUDE.UI;
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
        [SerializeField] private RestorationJunction[] restorationJunctions = new RestorationJunction[0];
        [SerializeField] private SceneFeatureComposition[] features = new SceneFeatureComposition[0];
        [SerializeField] private SOLITUDE.Tasks.UI.TaskJournalView journalView;
        [SerializeField] private SOLITUDE.UI.ShipMenuView shipMenu;
        private PauseCoordinator menuPause;
        [SerializeField] private SOLITUDE.Tasks.UI.TaskNoticeView taskNoticeView;
        private TaskNoticeCoordinator taskNotices;
        private TaskJournal tasks;
        private string taskScope;
        private TaskJournalScreenCoordinator journalScreen;
        private TaskJournalPresenter journalPresenter;
        private readonly List<ContainerSaveSession.ContainerBinding> bindings = new();
        private readonly List<WorldPickup> registeredPickups = new();
        private readonly List<SceneFeatureComposition> boundFeatures = new();
        [SerializeField] private UnityEngine.InputSystem.UI.InputSystemUIInputModule uiInput;
        private IDisposable uiLease, primaryLease;
        private InventoryScreenView inventoryScreen;
        private bool useCarriedEquipment;
        private CarriedEquipment equipment;
        public void ConfigureEquipmentFlow(bool enabled) => useCarriedEquipment = enabled;
        private PlayerItemActionComposition itemActions;
        private ItemPresentationCatalog presentations;
        private ItemCapabilityCatalog capabilities;
        internal void ConfigureCapabilities(ItemCapabilityCatalog value)
        {
            if (session != null) throw new InvalidOperationException("Capabilities must be configured before scene binding.");
            capabilities = value;
        }
        private ContainerSaveSession session;
        private PickupCollectionService pickups;
        private Dictionary<LootTable, RuntimeLootTable> loot;
        private InputPolicyCoordinator policy;
        private UnityInputHost input;
        private ContainerModalCoordinator modal;
        private HotbarSelectionState selection;
        private ContainerGestureCoordinator gestures;
        private HeldItemPresenter held;
        private SceneInteractionScope interactions;
        private Action released;
        private Guid inventoryToken, transferToken;
        private IContainerSource inventoryTransferSource;
        private string preparingTargetLabel = "Locker";
        private bool persistentLifecycle;
        private StartingLoadoutCoordinator startingLoadout;
        public StartingLoadoutOutcome InitializeStartingLoadout(StartingLoadoutSpec spec, Func<bool> persist)
        {
            if (!HasPlayer || session == null) return StartingLoadoutOutcome.Rejected;
            startingLoadout ??= new StartingLoadoutCoordinator(session, inventory.Container, hotbar.Container, selection);
            return startingLoadout.Initialize(spec, persist);
        }
        public string StartingLoadoutError => startingLoadout?.LastError;
        public bool HasPlayer => inventory != null;
        internal void ConfigureLifecyclePersistence(bool enabled)
        {
            if (session != null) throw new InvalidOperationException("Lifecycle persistence must be configured before scene binding.");
            persistentLifecycle = enabled;
        }
        public void Validate()
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var source in lockers)
                if (source == null || string.IsNullOrWhiteSpace(source.PersistentId) || !ids.Add(source.PersistentId)) throw new InvalidOperationException("Invalid/duplicate scene locker reference.");
            var pickupIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var pickup in worldPickups)
                if (pickup == null || string.IsNullOrWhiteSpace(pickup.PickupId) || !pickupIds.Add(pickup.PickupId)) throw new InvalidOperationException("Invalid/duplicate scene pickup reference.");
            ValidateFeatures();
            if (!HasPlayer && features.Length != 0) throw new InvalidOperationException("Scene features require a composed player scene.");
            if (!HasPlayer) return;
            if (gameObject.scene.GetRootGameObjects().Any(root => root.GetComponentsInChildren<SceneInteractionBindings>(true).Length != 0))
                throw new InvalidOperationException("Standalone interaction owner cannot coexist with SceneBindings.");
            SceneInteractionScope.Validate(gameObject.scene, interactor);
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
        private void ValidateFeatures()
        {
            if (features == null) throw new InvalidOperationException("Scene feature references are missing.");
            var unique = new HashSet<SceneFeatureComposition>();
            foreach (var feature in features)
            {
                if (feature == null || feature.gameObject.scene != gameObject.scene || !unique.Add(feature))
                    throw new InvalidOperationException("Null, duplicate or wrong-scene feature composition reference.");
                feature.Validate();
            }
            foreach (var root in gameObject.scene.GetRootGameObjects())
                foreach (var feature in root.GetComponentsInChildren<SceneFeatureComposition>(true))
                    if (!unique.Contains(feature)) throw new InvalidOperationException("Unbound authored feature composition: " + feature.name);
        }
        public void Initialize(ContainerSaveSession session, PickupCollectionService pickups, ItemPresentationCatalog presentations,
            Dictionary<LootTable, RuntimeLootTable> loot, ContainerGestureCoordinator gestures, InputPolicyCoordinator policy,
            PauseCoordinator pause, IContainerScreenRequests screens, UnityInputHost input, GameManager game, Action released, TaskJournal tasks = null)
        {
            this.gestures = gestures;
            Validate(); this.presentations = presentations; this.session = session; this.pickups = pickups; this.loot = loot; this.policy = policy; this.input = input; this.released = released;
            this.tasks = tasks; taskScope = Guid.NewGuid().ToString("N");
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
                int normalCapacity = inventory.Capacity;
                if (useCarriedEquipment)
                    session.PrepareEquipmentInventory(normalCapacity, id =>
                        CarriedEquipment.Eligible(capabilities, id, PlayerEquipmentSlot.Weapon) ? PlayerEquipmentSlot.Weapon :
                        CarriedEquipment.Eligible(capabilities, id, PlayerEquipmentSlot.Tool) ? PlayerEquipmentSlot.Tool : PlayerEquipmentSlot.None);
                var inventoryBinding = session.Register(ContainerSaveSession.PlayerInventoryId, inventory.Capacity); bindings.Add(inventoryBinding); inventory.Initialize(inventoryBinding.Container);
                interactions = new SceneInteractionScope(); interactions.Initialize(gameObject.scene, interactor);
                foreach (var junction in restorationJunctions)
                    if (junction != null) junction.Initialize(session.Commands, inventory.Container);
                var hotbarBinding = session.Register(ContainerSaveSession.PlayerHotbarId, hotbar.Capacity); bindings.Add(hotbarBinding);
                if (useCarriedEquipment)
                {
                    equipment = new CarriedEquipment(session, inventory.Container, hotbarBinding.Container, capabilities, presentations, normalCapacity);
                    equipment.ValidateRestored();
                }
                selection = new HotbarSelectionState(hotbarBinding.Container); hotbar.Initialize(hotbarBinding.Container, selection);
                foreach (var controller in controllers)
                    controller.Initialize(presentations, gestures, policy, controller.DefaultSource == hotbar ? selection : null);
                foreach (var tooltip in tooltips) tooltip.Initialize(presentations);
                heldView.Initialize(presentations); held = new HeldItemPresenter(gestures.Interaction, heldView);
                modal = new ContainerModalCoordinator(policy, pause, gestures, this, inventory.Container);
                inventoryModal.Initialize(RequestInventory, () => modal.Close());
                transferModal.Initialize(source => source is LockerContainer locker ? Open(locker) :
                    source != null && Open(source.Container, source.Label), () => modal.Close());
                inventoryModal.RenderVisible(false); transferModal.RenderVisible(false);
                inventoryScreen = inventoryModal.GetComponent<InventoryScreenView>();
                inventoryScreen?.Initialize(inventory.Container, hotbar.Container, session.Commands, presentations, capabilities,
                    gestures, policy, selection, input, () => modal.Close(), equipment);
                foreach (var controller in controllers)
                    if (controller != inventoryModal.Controller && controller != transferModal.InventoryController && controller != transferModal.TargetController && controller.DefaultSource != null)
                        controller.BindDefaultSource();
                if (equipment != null && inventoryScreen != null) input.InputDeviceChanged += inventoryScreen.UpdateEquipmentHud;
                uiLease = input.ConnectUI(uiInput);
                input.Point += Point;
                if (taskNoticeView != null && tasks != null)
                {
                    try
                    {
                        taskNotices = new TaskNoticeCoordinator(tasks, policy);
                        taskNotices.PresentationError += NoticeError;
                        taskNoticeView.Initialize(taskNotices);
                    }
                    catch (Exception error)
                    {
                        taskNoticeView.Release(); taskNotices?.Dispose(); taskNotices = null;
                        Debug.LogWarning("Optional task notice UI unavailable: " + error.Message);
                    }
                }
                var savedSurvivor = persistentLifecycle ? session.Capture().survivor : null;
                var featureContext = new SceneFeatureContext(session.Commands, inventory.Container, presentations,
                    policy, input, tasks, taskScope, interactor, interactions.Events,
                    hotbar.Container, selection, CloseForTransition, interactions.Feedback, Open, Close,
                    persistentLifecycle ? session : null, savedSurvivor != null && savedSurvivor.scenePath != gameObject.scene.path, capabilities, equipment);
                foreach (var feature in features)
                {
                    // Record before binding so a throwing initializer is also unwound.
                    boundFeatures.Add(feature); feature.Initialize(featureContext);
                }
                if (capabilities != null && movement.GetComponent<PlayerVitalsController>() != null)
                {
                    itemActions = movement.GetComponent<PlayerItemActionComposition>();
                    if (itemActions == null) itemActions = movement.gameObject.AddComponent<PlayerItemActionComposition>();
                    if (movement.GetComponent<SOLITUDE.Combat.MeleeSwingView>() == null)
                        movement.gameObject.AddComponent<SOLITUDE.Combat.MeleeSwingView>();
                    itemActions.Bind(session.Commands, hotbar.Container, selection, capabilities, policy, equipment == null ? null : session);
                    primaryLease = input.BindPrimaryAction(() => { if (itemActions != null) itemActions.Request(); });
                }
                if (journalView != null && tasks != null)
                {
                    try
                    {
                        journalView.Initialize(tasks, policy, pause, input);
                        journalScreen = journalView.Screen; journalPresenter = journalView.Presenter;
                    }
                    catch (Exception error) { journalView.Release(); Debug.LogWarning("Optional task journal UI unavailable: " + error.Message); }
                }
                if (shipMenu != null && journalScreen != null && inventoryScreen != null)
                {
                    menuPause = pause;
                    shipMenu.Initialize(input, SelectShipTab,
                        () => { input.ConsumeUIActivation(); Back(); }, () => inventoryScreen.IsHandling);
                    inventoryScreen.MenuFocusFallback = shipMenu.InventoryFocus;
                    journalView.VisibilityChanged += RefreshShipMenu;
                    modal.Changed += RefreshShipMenu;
                }
                input.Bind(movement, interactor, selection, RequestInventory,
                    Back, menuPause == null ? game.TogglePause : OpenInventoryFromEscape,
                    journalScreen, equipment == null ? null : equipment.Switch,
                    menuPause == null ? null : () => OpenShipObjectives(true),
                    equipment == null ? null : equipment.SelectQuickUse);
            }
            catch { Release(); throw; }
        }
        private void RequestInventory()
        {
            if (menuPause == null) modal.ToggleInventory();
            else OpenShipInventory(true);
        }

        private void OpenInventoryFromEscape()
        {
            input.ConsumeUIActivation();
            OpenShipInventory(false);
        }

        private void RefreshShipMenu()
        {
            if (journalScreen.IsOpen)
            {
                if (journalView.Presenter.IsVisible) shipMenu.Show(ShipMenuTab.Objectives);
            }
            else if (modal.Kind == ContainerModalKind.Inventory) shipMenu.Show(ShipMenuTab.Inventory);
            else shipMenu.Hide();
        }

        private void SelectShipTab(ShipMenuTab tab)
        {
            input.ConsumeUIActivation();
            switch (tab)
            {
                case ShipMenuTab.Inventory:
                    OpenShipInventory(false);
                    break;
                case ShipMenuTab.Objectives:
                    OpenShipObjectives(false);
                    break;
                default:
                    if (!journalScreen.IsOpen && OpenShipObjectives(false) != OpenResult.Opened) return;
                    journalView.HidePage();
                    shipMenu.Show(tab);
                    break;
            }
        }

        private OpenResult OpenShipInventory(bool toggle)
        {
            if (journalScreen.IsOpen)
            {
                input.ConsumeUIActivation();
                using var pause = menuPause.Acquire();
                journalScreen.Close();
                var result = modal.ToggleInventory();
                if (result != OpenResult.Opened) journalScreen.OpenDuringPausedHandoff();
                return result;
            }
            return toggle || modal.Kind == ContainerModalKind.Closed
                ? modal.ToggleInventory() : OpenResult.Opened;
        }

        private OpenResult OpenShipObjectives(bool toggle)
        {
            if (modal.Kind == ContainerModalKind.Inventory)
            {
                input.ConsumeUIActivation();
                using var pause = menuPause.Acquire();
                modal.Close();
                var result = journalScreen.OpenDuringPausedHandoff();
                if (result != OpenResult.Opened) modal.ToggleInventory();
                return result;
            }
            if (journalScreen.IsOpen && !journalView.Presenter.IsVisible)
            {
                try
                {
                    journalView.ShowPage();
                    shipMenu.Show(ShipMenuTab.Objectives);
                }
                catch (Exception error)
                {
                    Debug.LogException(error);
                    journalScreen.Close();
                    return OpenResult.BindingFailed;
                }
                return OpenResult.Opened;
            }
            return toggle ? journalScreen.Toggle() : journalScreen.Open();
        }

        private static void NoticeError(Exception error) => Debug.LogWarning("Optional task notice render failed: " + error.Message);
        private void Update() => taskNotices?.Advance(Time.unscaledDeltaTime);
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
        private bool Back()
        {
            if (inventoryScreen != null && inventoryScreen.isActiveAndEnabled && inventoryScreen.CancelHandling()) { input?.ConsumeUIActivation(); return true; }
            if (journalScreen?.IsOpen == true) { input?.ConsumeUIActivation(); return journalScreen.Back(); }
            foreach (var feature in boundFeatures) if (feature != null && feature.TryBack()) return true;
            if (modal?.Back() != true) return false;
            input?.ConsumeUIActivation();
            return true;
        }
        public void CloseForTransition()
        {
            interactor?.ClearFocus();
            interactions?.Feedback.Clear();
            journalScreen?.Close();
            foreach (var feature in boundFeatures) if (feature != null) feature.CloseForTransition();
            modal?.Close();
            gestures?.Cancel();
        }
        public void ShowRecoveryNotice(Action acknowledged) => recoveryNotice.Show(acknowledged);
        public bool Open(LockerContainer source)
        {
            if (modal == null || source == null || !source.EnsureInitialized()) return false;
            return Open(source.Container, source.Label);
        }
        public void Close(LockerContainer source) { if (source?.Container != null) modal?.CloseTarget(source.Container.Handle); }
        public bool Open(IContainerReader source, string label)
        {
            if (modal == null || source?.IsAvailable != true || string.IsNullOrWhiteSpace(label) ||
                !ReferenceEquals(session?.Commands.Read(source.Handle), source)) return false;
            var prior = preparingTargetLabel; preparingTargetLabel = label;
            try
            {
                var result = modal.OpenTarget(source);
                return result == OpenResult.Opened || result == OpenResult.Closed;
            }
            finally { preparingTargetLabel = prior; }
        }
        public void Close(ContainerHandle handle) => modal?.CloseTarget(handle);
        private sealed class ReaderSource : IContainerSource
        {
            public IContainerReader Container { get; }
            public string Label { get; }
            public ReaderSource(IContainerReader reader, string label) { Container = reader; Label = label; }
        }
        public IModalBinding Prepare(ContainerModalKind kind, IContainerReader inventory, IContainerReader target)
        {
            if (!inventory.IsAvailable || (kind == ContainerModalKind.Transfer && target?.IsAvailable != true)) return null;
            return new PreparedModal(this, kind, new ReaderSource(inventory, "Inventory"), target == null ? null : new ReaderSource(target, preparingTargetLabel));
        }
        private sealed class PreparedModal : IModalBinding
        {
            private SceneBindings scope;
            private readonly ContainerModalKind kind;
            private readonly IContainerSource inventory, target;
            private readonly ContainerController first, second;
            private readonly bool useInventoryTransfer;
            private readonly Guid token = Guid.NewGuid();
            private bool active;
            public PreparedModal(SceneBindings scope, ContainerModalKind kind, IContainerSource inventory, IContainerSource target)
            {
                this.scope = scope; this.kind = kind; this.inventory = inventory; this.target = target;
                useInventoryTransfer = kind == ContainerModalKind.Transfer && scope.inventoryScreen?.SupportsTransfer == true;
                first = kind == ContainerModalKind.Inventory || useInventoryTransfer
                    ? scope.inventoryModal.Controller : scope.transferModal.InventoryController;
                second = kind == ContainerModalKind.Transfer && !useInventoryTransfer ? scope.transferModal.TargetController : null;
            }
            public void Activate()
            {
                var previousFirst = first.CurrentSource;
                var previousSecond = second?.CurrentSource;
                var previousInventoryTarget = scope.inventoryTransferSource;
                try
                {
                    if (useInventoryTransfer)
                    {
                        first.Unbind();
                        scope.inventoryScreen.BeginTransfer(target.Container, target.Label);
                        scope.inventoryModal.RenderVisible(true);
                        scope.inventoryTransferSource = target;
                        scope.inventoryToken = token;
                    }
                    else
                    {
                        first.Bind(inventory);
                        second?.Bind(target);
                        if (kind == ContainerModalKind.Inventory)
                        {
                            scope.inventoryModal.RenderVisible(true);
                            scope.inventoryToken = token;
                        }
                        else
                        {
                            scope.transferModal.RenderVisible(true);
                            scope.transferToken = token;
                        }
                    }
                    active = true;
                }
                catch
                {
                    if (useInventoryTransfer)
                    {
                        scope.inventoryScreen.EndTransfer();
                        if (previousInventoryTarget != null)
                            scope.inventoryScreen.BeginTransfer(previousInventoryTarget.Container, previousInventoryTarget.Label);
                    }
                    first.Unbind(); second?.Unbind();
                    if (previousFirst != null) first.Bind(previousFirst);
                    if (previousSecond != null) second.Bind(previousSecond);
                    throw;
                }
            }
            public void Dispose()
            {
                if (scope == null) return;
                if (active && (kind == ContainerModalKind.Inventory || useInventoryTransfer) && scope.inventoryToken == token)
                {
                    first.Unbind();
                    if (scope.inventoryModal != null) scope.inventoryModal.RenderVisible(false);
                    if (useInventoryTransfer)
                    {
                        scope.inventoryScreen.EndTransfer();
                        scope.inventoryTransferSource = null;
                    }
                }
                if (active && kind == ContainerModalKind.Transfer && !useInventoryTransfer && scope.transferToken == token)
                { first.Unbind(); second.Unbind(); if (scope.transferModal != null) scope.transferModal.RenderVisible(false); }
                scope = null;
            }
        }
        private void Point(float x, float y)
        { held?.Point(x, y); foreach (var tooltip in tooltips) tooltip.SetPosition(new Vector2(x, y)); }
        public void Release()
        {
            if (session == null) return;
            primaryLease?.Dispose(); primaryLease = null;
            if (itemActions != null) itemActions.Release(); itemActions = null;
            taskNotices?.Dispose(); taskNotices = null;
            if (taskNoticeView != null) taskNoticeView.Release();
            if (menuPause != null)
            {
                journalView.VisibilityChanged -= RefreshShipMenu;
                modal.Changed -= RefreshShipMenu;
                if (inventoryScreen != null) inventoryScreen.MenuFocusFallback = null;
                if (shipMenu != null) shipMenu.Release();
                menuPause = null;
            }
            journalScreen?.Dispose(); journalScreen = null; journalPresenter?.Dispose(); journalPresenter = null;
            if (journalView != null) journalView.Release();
            for (int i = boundFeatures.Count - 1; i >= 0; i--)
                if (boundFeatures[i] != null) boundFeatures[i].Release();
            boundFeatures.Clear();
            interactions?.Dispose(); interactions = null;
            if (taskScope != null) tasks?.ReleaseScope(taskScope);
            tasks = null; taskScope = null;
            modal?.Dispose(); modal = null;
            if (input != null && inventoryScreen != null) input.InputDeviceChanged -= inventoryScreen.UpdateEquipmentHud;
            inventoryScreen?.Release(); inventoryScreen = null;
            if (input != null && selection != null) { input.Point -= Point; input.Bind(null, null, null, null, null, null); }
            uiLease?.Dispose(); uiLease = null;
            held?.Dispose(); held = null;
            // Never-active controllers may be destroyed without OnDestroy. Their managed
            // presenters must detach before owner unregistration, even after Unity's null check fails.
            foreach (var controller in controllers) if (!ReferenceEquals(controller, null)) controller.Unbind();
            selection?.Dispose(); selection = null;
            foreach (var pickup in registeredPickups) pickup.ReleaseBinding(); registeredPickups.Clear();
            foreach (var source in lockers) { source.Release(); if (source != null) source.GetComponent<Locker>()?.Release(); }
            foreach (var junction in restorationJunctions) if (junction != null) junction.ReleaseBinding();
            foreach (var binding in bindings) binding.Dispose(); bindings.Clear();
            startingLoadout = null; equipment = null; useCarriedEquipment = false; inventory?.Release(); hotbar?.Release(); session = null; gestures = null; capabilities = null; persistentLifecycle = false;
        }
        private void OnDestroy() { var notify = released; released = null; notify?.Invoke(); Release(); }
    }
}
