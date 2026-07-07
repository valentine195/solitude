using System;
using SOLITUDE.Core.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SOLITUDE.Containers
{
    /// <summary>
    /// Thin wrapper over the "Container" action map on the single shared
    /// GameInput.Actions asset (see GameInput).
    ///
    /// This used to be a MonoBehaviour, one per ContainerController, each
    /// constructing and enabling its own copy of the "Container" action map.
    /// That meant opening two containers at once double-fired every
    /// Point/Click/Cancel callback, and Cancel only existed while at least
    /// one container's bridge happened to be enabled - so closing every
    /// open container mid-drag left no way to cancel a held item at all.
    ///
    /// Now there is exactly one of these, owned by ContainerInteractionHub
    /// (see its Awake/OnDestroy), which wires Cancel directly to
    /// Interaction.Cancel() so it always works regardless of which - or
    /// whether any - container window is open.
    /// </summary>
    public class ContainerInputBridge
    {
        private readonly SOLITUDE_InputActions.ContainerActions map;
        private bool enabled;

        public event Action<Vector2> Point;
        public event Action Click;
        public event Action Cancel;

        public ContainerInputBridge()
        {
            map = GameInput.Actions.Container;
        }

        public void Enable()
        {
            if (enabled) return;
            enabled = true;

            map.Enable();
            map.Point.performed += OnPoint;
            map.Click.performed += OnClick;
            map.Cancel.performed += OnCancel;
        }

        public void Disable()
        {
            if (!enabled) return;
            enabled = false;

            map.Point.performed -= OnPoint;
            map.Click.performed -= OnClick;
            map.Cancel.performed -= OnCancel;
            map.Disable();
        }

        private void OnPoint(InputAction.CallbackContext ctx) => Point?.Invoke(ctx.ReadValue<Vector2>());
        private void OnClick(InputAction.CallbackContext ctx) => Click?.Invoke();
        private void OnCancel(InputAction.CallbackContext ctx) => Cancel?.Invoke();
    }
}
