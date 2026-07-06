
using UnityEngine;
using UnityEngine.InputSystem;

namespace SOLITUDE.Core.Systems
{
    public class InputRouter : MonoBehaviour
    {
        public static InputRouter Instance { get; private set; }

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

        /*        private void OnDisable()
                {
                    inputActions.Disable();
                }*/

        private void OnInventory(InputAction.CallbackContext ctx)
        {
            if (!InputEnabled) return;
            Debug.Log("InventoryPressed");
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
            Debug.Log("InteractPressed");
            InteractPressed = ctx.performed;
        }

        private void OnPause(InputAction.CallbackContext ctx)
        {
            if (!InputEnabled) return;
            Debug.Log("PausePressed");
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