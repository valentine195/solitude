using UnityEngine;
using UnityEngine.InputSystem;

namespace SOLITUDE.Core.Systems
{
    public class InputRouter : MonoBehaviour
    {
        public static InputRouter Instance { get; private set; }

        [SerializeField] private bool debugInput = false;

        public Vector2 Move { get; private set; }
        public bool InventoryPressed { get; private set; }
        public bool InteractPressed { get; private set; }
        public bool PausePressed { get; private set; }

        public bool InputEnabled = true;

        private SOLITUDE_InputActions inputActions;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            inputActions = new SOLITUDE_InputActions();
        }

        private void OnEnable()
        {
            inputActions.Enable();

            inputActions.Gameplay.Move.performed += OnMove;
            inputActions.Gameplay.Move.canceled += OnMove;

            inputActions.Gameplay.Interact.performed += OnInteract;
            inputActions.Gameplay.Inventory.performed += OnInventory;
            inputActions.Gameplay.Pause.performed += OnPause;
        }

        private void OnDisable()
        {
            // This used to be commented out entirely, meaning every OnEnable
            // stacked another subscription onto the same delegate without
            // ever removing the previous one - harmless while these
            // callbacks only set a bool/vector, but a ticking bug the
            // moment one of them does something non-idempotent, and it also
            // meant the action map was never actually disabled once enabled.
            inputActions.Gameplay.Move.performed -= OnMove;
            inputActions.Gameplay.Move.canceled -= OnMove;

            inputActions.Gameplay.Interact.performed -= OnInteract;
            inputActions.Gameplay.Inventory.performed -= OnInventory;
            inputActions.Gameplay.Pause.performed -= OnPause;

            inputActions.Disable();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;

            // inputActions owns native Input System resources - without
            // this, destroying/recreating this object (e.g. across editor
            // play sessions or scene reloads that don't fully tear it down)
            // leaks them.
            inputActions?.Dispose();
        }

        private void OnInventory(InputAction.CallbackContext ctx)
        {
            if (!InputEnabled) return;
            if (debugInput) Debug.Log("InventoryPressed");
            InventoryPressed = ctx.performed;

        }
        private void OnMove(InputAction.CallbackContext ctx)
        {
            if (!InputEnabled) return;

            Move = ctx.ReadValue<Vector2>();
        }

        private void OnInteract(InputAction.CallbackContext ctx)
        {
            if (!InputEnabled) return;
            if (debugInput) Debug.Log("InteractPressed");
            InteractPressed = ctx.performed;
        }

        private void OnPause(InputAction.CallbackContext ctx)
        {
            if (!InputEnabled) return;
            if (debugInput) Debug.Log("PausePressed");
            PausePressed = ctx.performed;
        }

        public void LateUpdate()
        {
            // reset one-frame buttons
            InteractPressed = false;
            PausePressed = false;
            InventoryPressed = false;
        }

        public void SetInputEnabled(bool enabled)
        {
            InputEnabled = enabled;

            if (!enabled)
            {
                Move = Vector2.zero;
                InteractPressed = false;
                PausePressed = false;
                InventoryPressed = false;
            }
        }
    }
}