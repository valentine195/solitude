using UnityEngine;
using SOLITUDE.Containers;
using SOLITUDE.Core.Input;
using SOLITUDE.Core.Systems;

namespace SOLITUDE.Player
{
    public class PlayerInventoryModalController : MonoBehaviour
    {
        [SerializeField] private PlayerInventory inventory;
        private bool IsOpen = false;

        private void OnEnable()
        {
            GameInput.Actions.Gameplay.Enable();
            GameInput.Actions.Gameplay.Inventory.performed += OnInventoryPressed;
            IsOpen = false;
            ContainerUIController.Instance?.GetView(ContainerUIType.PlayerInventory)?.Close();
        }

        private void OnDisable()
        {
            GameInput.Actions.Gameplay.Inventory.performed -= OnInventoryPressed;
        }

        private void OnInventoryPressed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
        {

            if (!IsOpen)
            {
                IsOpen = true;
                ContainerUIController.Instance?.GetView(ContainerUIType.PlayerInventory)?.Open(inventory);
                GameManager.Instance.PauseGame();
            }
            else
            {
                IsOpen = false;
                ContainerUIController.Instance?.GetView(ContainerUIType.PlayerInventory)?.Close();
                GameManager.Instance.ResumeGame();
            }
        }
    }
}
