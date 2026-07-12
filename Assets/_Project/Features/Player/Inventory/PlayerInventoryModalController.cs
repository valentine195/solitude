using UnityEngine;
using SOLITUDE.Containers;
using SOLITUDE.Core.Input;
using SOLITUDE.Core.Systems;

namespace SOLITUDE.Player
{
    public class PlayerInventoryModalController : MonoBehaviour
    {
        [SerializeField] private ContainerModalView view;
        private bool IsOpen = false;

        private void OnEnable()
        {
            GameInput.Actions.Gameplay.Enable();
            GameInput.Actions.Gameplay.Inventory.performed += OnInventoryPressed;
            IsOpen = false;
            view.Close();
        }

        private void OnDisable()
        {
            GameInput.Actions.Gameplay.Inventory.performed -= OnInventoryPressed;
        }

        private void OnInventoryPressed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
        {
            view.Toggle();
            IsOpen = !IsOpen;
            if (IsOpen)
            {
                GameManager.Instance.PauseGame();
            }
            else
            {
                GameManager.Instance.ResumeGame();
            }
        }
    }
}
