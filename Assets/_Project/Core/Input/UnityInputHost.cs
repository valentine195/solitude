using System;
using SOLITUDE.Application;
using SOLITUDE.Player;
using UnityEngine;
using UnityEngine.InputSystem;
namespace SOLITUDE.Core.Input
{
    public sealed class UnityInputHost : MonoBehaviour
    {
        private readonly System.Collections.Generic.List<IDisposable> uiLeases = new();
        private SOLITUDE_InputActions actions;
        private InputPolicyCoordinator policy;
        private PlayerMovement movement;
        private PlayerInteractor interactor;
        private HotbarSelectionState selection;
        private Func<bool> back;
        private Action inventory;
        private Action fallbackBack;
        private InputControl lastBackControl;
        private double lastBackTime = -1;
        public event Action<float, float> Point;
        public IDisposable ConnectUI(UnityEngine.InputSystem.UI.InputSystemUIInputModule module)
        {
            if (actions.asset.FindActionMap("UI", false) == null) throw new InvalidOperationException("The composed input asset requires a UI map.");
            module.enabled = false;
            module.actionsAsset = actions.asset;
            var references = new[] { module.point, module.move, module.leftClick, module.rightClick, module.middleClick, module.scrollWheel,
                module.submit, module.cancel, module.trackedDevicePosition, module.trackedDeviceOrientation };
            var boundPolicy = policy;
            void ApplyUI() { if (module != null) module.enabled = boundPolicy.Pointer; }
            boundPolicy.Changed += ApplyUI; ApplyUI();
            var lease = new Lease(() =>
            {
                boundPolicy.Changed -= ApplyUI;
                if (module != null) { module.enabled = false; module.UnassignActions(); }
                foreach (var reference in references) if (reference != null) Destroy(reference);
            });
            uiLeases.Add(lease);
            return lease;
        }
        public void Initialize(InputPolicyCoordinator policy)
        {
            Release(); this.policy = policy; actions = new SOLITUDE_InputActions();
            actions.Gameplay.Move.performed += Move; actions.Gameplay.Move.canceled += Stop;
            actions.Gameplay.Interact.performed += Interact; actions.Gameplay.Inventory.performed += Inventory;
            actions.Gameplay.Pause.performed += Back; actions.Container.Cancel.performed += Back;
            actions.Container.Point.performed += Pointer;
            for (int i = 0; i < 9; i++) actions.asset.FindAction("Gameplay/Hotbar" + (i + 1)).performed += Hotbar;
            policy.Changed += Apply; Apply();
        }
        public void Bind(PlayerMovement movement, PlayerInteractor interactor, HotbarSelectionState selection,
            Action inventory, Func<bool> back, Action fallbackBack)
        {
            if (this.movement != null) this.movement.SetInput(Vector2.zero); if (this.interactor != null) this.interactor.SetInputAllowed(false);
            this.movement = movement; this.interactor = interactor; this.selection = selection;
            this.inventory = inventory; this.back = back; this.fallbackBack = fallbackBack; Apply();
        }
        private static void Enable(InputAction action, bool enabled) { if (enabled) action.Enable(); else action.Disable(); }
        private void Apply()
        {
            if (actions == null) return;
            if (!policy.Gameplay && movement != null) movement.SetInput(Vector2.zero);
            if (interactor != null) interactor.SetInputAllowed(policy.Gameplay);
            Enable(actions.Gameplay.Move, policy.Gameplay); Enable(actions.Gameplay.Interact, policy.Gameplay);
            for (int i = 0; i < 9; i++) Enable(actions.asset.FindAction("Gameplay/Hotbar" + (i + 1)), policy.Gameplay);
            Enable(actions.Gameplay.Inventory, policy.Inventory);
            Enable(actions.Gameplay.Pause, policy.Ready); Enable(actions.Container.Cancel, policy.Ready);
            Enable(actions.Container.Point, policy.Pointer);
            // Click delivery belongs exclusively to the EventSystem.
        }
        private void Move(InputAction.CallbackContext context) { if (policy.Gameplay && movement != null) movement.SetInput(context.ReadValue<Vector2>()); }
        private void Stop(InputAction.CallbackContext _) { if (movement != null) movement.SetInput(Vector2.zero); }
        private void Interact(InputAction.CallbackContext _) { if (policy.Gameplay && interactor != null) interactor.TryInteract(); }
        private void Inventory(InputAction.CallbackContext _) { if (policy.Inventory) inventory?.Invoke(); }
        private void Hotbar(InputAction.CallbackContext context)
        { if (policy.Gameplay && int.TryParse(context.action.name.Substring(6), out int index)) selection?.Select(index - 1); }
        private void Pointer(InputAction.CallbackContext context)
        { if (!policy.Pointer) return; var point = context.ReadValue<Vector2>(); Point?.Invoke(point.x, point.y); }
        private void Back(InputAction.CallbackContext context)
        {
            if (!policy.Ready || (lastBackControl == context.control && lastBackTime == context.time)) return;
            lastBackControl = context.control; lastBackTime = context.time;
            if (!(back?.Invoke() ?? false)) fallbackBack?.Invoke();
        }
        public void Release()
        {
            foreach (var lease in uiLeases) lease.Dispose();
            uiLeases.Clear();
            if (movement != null) movement.SetInput(Vector2.zero); if (interactor != null) interactor.SetInputAllowed(false);
            movement = null; interactor = null; selection = null; inventory = null; back = null; fallbackBack = null;
            if (policy != null) policy.Changed -= Apply; policy = null;
            if (actions != null) { actions.Disable(); actions.Dispose(); actions = null; }
            lastBackTime = -1; lastBackControl = null;
        }
        private void OnDestroy() => Release();
    }
}
