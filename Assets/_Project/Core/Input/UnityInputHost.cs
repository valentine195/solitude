using System;
using System.Collections.Generic;
using System.Linq;
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
        private TaskJournalScreenCoordinator journal;
        private InputControl lastBackControl;
        private double lastBackTime = -1;
        private readonly Dictionary<InputAction, InputControl[]> heldAtReset = new();
        private InputControl[] heldUI = new InputControl[0];
        private event Action SurvivorInputChanged;
        // Actions re-enabled by policy may observe held controls. Require their release before accepting a new survivor action.
        public void ResetSurvivorInput()
        {
            heldAtReset.Clear();
            foreach (var action in actions.asset)
                if (action.actionMap.name == "Gameplay" || action.name == "Cancel")
                {
                    var held = action.controls.Where(c => c.IsActuated()).ToArray();
                    if (held.Length != 0) heldAtReset[action] = held;
                }
            heldUI = actions.asset.Where(a => a.actionMap.name == "UI" && (a.name == "Click" || a.name == "Submit"))
                .SelectMany(a => a.controls.Where(c => c.IsActuated())).ToArray();
            movement?.SetInput(Vector2.zero); ConsumeUIActivation(); SurvivorInputChanged?.Invoke();
        }
        private bool Fresh(InputAction.CallbackContext context) => !heldAtReset.ContainsKey(context.action);
        private void Update()
        {
            foreach (var action in heldAtReset.Keys.ToArray())
                if (!heldAtReset[action].Any(c => c.IsActuated())) heldAtReset.Remove(action);
            if (heldUI.Length != 0 && !heldUI.Any(c => c.IsActuated()))
            { heldUI = new InputControl[0]; SurvivorInputChanged?.Invoke(); }
        }
        private int consumedFrame = -1;
        public void ConsumeUIActivation() => consumedFrame = Time.frameCount;
        private bool GameplayAllowed => policy.Gameplay && consumedFrame != Time.frameCount;
        public event Action<float, float> Point;
        public IDisposable ConnectUI(UnityEngine.InputSystem.UI.InputSystemUIInputModule module)
        {
            if (actions.asset.FindActionMap("UI", false) == null) throw new InvalidOperationException("The composed input asset requires a UI map.");
            module.enabled = false;
            module.actionsAsset = actions.asset;
            InputActionReference UI(string name) => InputActionReference.Create(actions.asset.FindAction("UI/" + name, true));
            module.point = UI("Point"); module.move = UI("Navigate");
            module.leftClick = UI("Click"); module.rightClick = UI("RightClick"); module.middleClick = UI("MiddleClick");
            module.scrollWheel = UI("ScrollWheel"); module.submit = UI("Submit"); module.cancel = UI("Cancel");
            module.trackedDevicePosition = UI("TrackedDevicePosition"); module.trackedDeviceOrientation = UI("TrackedDeviceOrientation");
            var references = new[] { module.point, module.move, module.leftClick, module.rightClick, module.middleClick, module.scrollWheel,
                module.submit, module.cancel, module.trackedDevicePosition, module.trackedDeviceOrientation };
            var boundPolicy = policy;
            void ApplyUI() { if (module != null) module.enabled = boundPolicy.Pointer && heldUI.Length == 0; }
            boundPolicy.Changed += ApplyUI; SurvivorInputChanged += ApplyUI; ApplyUI();
            var lease = new Lease(() =>
            {
                boundPolicy.Changed -= ApplyUI; SurvivorInputChanged -= ApplyUI;
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
            actions.Gameplay.Journal.performed += ToggleJournal;
            actions.Container.Point.performed += Pointer;
            for (int i = 0; i < 9; i++) actions.asset.FindAction("Gameplay/Hotbar" + (i + 1)).performed += Hotbar;
            policy.Changed += Apply; Apply();
        }
        public void Bind(PlayerMovement movement, PlayerInteractor interactor, HotbarSelectionState selection,
            Action inventory, Func<bool> back, Action fallbackBack, TaskJournalScreenCoordinator journal = null)
        {
            if (!ReferenceEquals(this.journal, journal)) this.journal?.Close();
            if (this.movement != null) this.movement.SetInput(Vector2.zero); if (this.interactor != null) this.interactor.SetInputAllowed(false);
            this.movement = movement; this.interactor = interactor; this.selection = selection;
            this.inventory = inventory; this.back = back; this.fallbackBack = fallbackBack;
            this.journal = journal; Apply();
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
            Enable(actions.Gameplay.Journal, policy.Ready && journal != null);
            Enable(actions.Container.Point, policy.Pointer);
            // Click delivery belongs exclusively to the EventSystem.
        }
        private void Move(InputAction.CallbackContext context) { if (Fresh(context) && GameplayAllowed && movement != null) movement.SetInput(context.ReadValue<Vector2>()); }
        private void Stop(InputAction.CallbackContext _) { if (movement != null) movement.SetInput(Vector2.zero); }
        private void Interact(InputAction.CallbackContext context) { if (Fresh(context) && GameplayAllowed && interactor != null) interactor.TryInteract(); }
        private void Inventory(InputAction.CallbackContext context) { if (Fresh(context) && policy.Inventory && consumedFrame != Time.frameCount) inventory?.Invoke(); }
        private void Hotbar(InputAction.CallbackContext context)
        { if (Fresh(context) && GameplayAllowed && int.TryParse(context.action.name.Substring(6), out int index)) selection?.Select(index - 1); }
        private void Pointer(InputAction.CallbackContext context)
        { if (!policy.Pointer) return; var point = context.ReadValue<Vector2>(); Point?.Invoke(point.x, point.y); }
        private void ToggleJournal(InputAction.CallbackContext context)
        {
            if (!Fresh(context) || !policy.Ready || consumedFrame == Time.frameCount || journal == null) return;
            var result = journal.Toggle();
            if (result == OpenResult.Closed) ConsumeUIActivation();
            Apply();
        }
        public string BindingLabel(string actionPath)
        {
            var action = actions?.asset.FindAction(actionPath, false);
            if (action == null) return "UNBOUND";
            for (int i = 0; i < action.bindings.Count; i++)
                if (action.bindings[i].effectivePath?.StartsWith("<Keyboard>/", StringComparison.Ordinal) == true)
                    return action.GetBindingDisplayString(i).ToUpperInvariant();
            return action.GetBindingDisplayString().ToUpperInvariant();
        }
        private void Back(InputAction.CallbackContext context)
        {
            if (!Fresh(context) || !policy.Ready || (lastBackControl == context.control && lastBackTime == context.time)) return;
            lastBackControl = context.control; lastBackTime = context.time;
            if (journal?.IsOpen == true) { ConsumeUIActivation(); journal.Back(); Apply(); return; }
            if (!(back?.Invoke() ?? false)) fallbackBack?.Invoke();
        }
        public void Release()
        {
            journal?.Close(); journal = null;
            foreach (var lease in uiLeases) lease.Dispose();
            uiLeases.Clear();
            if (movement != null) movement.SetInput(Vector2.zero); if (interactor != null) interactor.SetInputAllowed(false);
            movement = null; interactor = null; selection = null; inventory = null; back = null; fallbackBack = null;
            if (policy != null) policy.Changed -= Apply; policy = null;
            if (actions != null) { actions.Disable(); actions.Dispose(); actions = null; }
            lastBackTime = -1; lastBackControl = null;
            consumedFrame = -1; heldAtReset.Clear(); heldUI = new InputControl[0];
        }
        private void OnDestroy() => Release();
    }
}
