using UnityEngine;
using SOLITUDE.Containers;
using SOLITUDE.Core.Input;

namespace SOLITUDE.Player
{
    public class PlayerInventoryModalController : MonoBehaviour
    {
        [SerializeField] private ContainerModalView view;

        private void OnEnable()
        {
            GameInput.Actions.Gameplay.Enable();
            GameInput.Actions.Gameplay.Inventory.performed += OnInventoryPressed;
        }

        private void OnDisable()
        {
            GameInput.Actions.Gameplay.Inventory.performed -= OnInventoryPressed;
        }

        private void OnInventoryPressed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
        {
            view.Toggle();
        }
    }
}
