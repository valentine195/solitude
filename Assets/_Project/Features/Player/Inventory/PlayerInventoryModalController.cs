using UnityEngine;
using SOLITUDE.Containers;

namespace SOLITUDE.Features.Player
{
    public class PlayerInventoryModalController : MonoBehaviour
    {
        [SerializeField] private ContainerModalView view;
        private SOLITUDE_InputActions inputActions;

        private void Awake()
        {
            inputActions = new SOLITUDE_InputActions();
            Debug.Log("[PlayerInventoryModalController] awake with inputActions " + inputActions);
        }

        private void OnEnable()
        {
            inputActions.Enable();
            inputActions.Gameplay.Inventory.performed += OnInventoryPressed;
        }

        private void OnDisable()
        {
            inputActions.Gameplay.Inventory.performed -= OnInventoryPressed;
            inputActions.Disable();
        }

        private void OnInventoryPressed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
        {
            Debug.Log("[PlayerInventoryModalController] Inventory pressed");
            view.Toggle();
        }
    }
}