using UnityEngine;

namespace SOLITUDE.Core.Animations
{
    [RequireComponent(typeof(Animator))]
    public class HumanAnimationController : MonoBehaviour
    {
        private Animator animator;

        private Vector2 moveInput;
        private bool isMoving;

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        public void SetMovement(Vector2 movement)
        {
            moveInput = movement;

            isMoving = movement.sqrMagnitude > 0.01f;

            animator.SetFloat("MoveX", movement.x);
            animator.SetFloat("MoveY", movement.y);
            animator.SetBool("IsMoving", isMoving);
        }
    }
}