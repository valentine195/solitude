using UnityEngine;
using SOLITUDE.Features.Player;
using SOLITUDE.Core.Systems;
using SOLITUDE.Core.Animations;
using SOLITUDE.Core.Input;
using UnityEngine.InputSystem;

namespace SOLITUDE.Features.Player
{
    /// <summary>
    /// Handles player locomotion only.
    /// No input handling, no interaction logic.
    /// Pure movement system.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;

        [Header("Optional")]
        [SerializeField] private Transform cameraTransform;
        private Rigidbody2D rb;

        private Vector2 moveInput;

        private HumanAnimationController animationController;

        private void Awake()

        {

            rb = GetComponent<Rigidbody2D>();
            animationController = GetComponent<HumanAnimationController>();

            GameInput.Actions.Gameplay.Move.performed += TryMove;

        }

        private void Update()

        {


        }
        public void TryMove(InputAction.CallbackContext ctx)
        {
            if (ctx.ReadValue<Vector2>().sqrMagnitude > 1f)

                moveInput.Normalize();
        }
        private void FixedUpdate()

        {

            rb.MovePosition(

                rb.position +

                moveInput * moveSpeed * Time.fixedDeltaTime);

            animationController.SetMovement(moveInput);

        }
    }
}