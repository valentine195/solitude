using UnityEngine;
using UnityEngine.InputSystem;
using System;

namespace SOLITUDE.Containers
{
    public class ContainerInputBridge : MonoBehaviour
    {
        private SOLITUDE_InputActions input;

        public event Action<Vector2> Point;
        public event Action Click;
        public event Action Cancel;

        private void Awake()
        {
            input = new SOLITUDE_InputActions();
        }

        private void OnEnable()
        {
            input.Container.Enable();

            input.Container.Point.performed += OnPoint;
            input.Container.Click.performed += OnClick;
            input.Container.Cancel.performed += OnCancel;
        }

        private void OnDisable()
        {
            input.Container.Point.performed -= OnPoint;
            input.Container.Click.performed -= OnClick;
            input.Container.Cancel.performed -= OnCancel;

            input.Container.Disable();
        }

        private void OnPoint(InputAction.CallbackContext ctx)
            => Point?.Invoke(ctx.ReadValue<Vector2>());

        private void OnClick(InputAction.CallbackContext ctx)
            => Click?.Invoke();

        private void OnCancel(InputAction.CallbackContext ctx)
            => Cancel?.Invoke();
    }

}