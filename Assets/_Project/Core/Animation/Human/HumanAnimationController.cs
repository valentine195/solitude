using UnityEngine;
using System;
using SOLITUDE.Application;

namespace SOLITUDE.Core.Animations
{
    [RequireComponent(typeof(Animator))]
    public class HumanAnimationController : MonoBehaviour, ICreatureAnimationController
    {
        // Hold the current cardinal near the diagonal so small stick changes do not alternate clips.
        private const float FacingHysteresis = .08f;
        private Animator animator;
        private Vector2 animationFacing = Vector2.down;

        private Vector2 moveInput;
        private bool isMoving;
        private object facingOwner;
        private Vector2 lockedFacing;
        public Vector2 Facing => animationFacing;

        public IDisposable OverrideFacing(Vector2 direction)
        {
            var token = new object(); facingOwner = token; lockedFacing = direction;
            SetMovement(moveInput);
            return new Lease(() => { if (ReferenceEquals(facingOwner, token)) { facingOwner = null; SetMovement(moveInput); } });
        }

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        public void SetMovement(Vector2 movement)
        {
            moveInput = movement;

            isMoving = movement.sqrMagnitude > 0.01f;

            var facing = facingOwner == null ? movement : lockedFacing;
            if (facing.sqrMagnitude > .01f) UpdateAnimationFacing(facing.normalized);
            if (animator == null || animator.runtimeAnimatorController == null) return;
            animator.SetFloat("MoveX", animationFacing.x);
            animator.SetFloat("MoveY", animationFacing.y);
            animator.SetBool("IsMoving", isMoving);
        }

        private void UpdateAnimationFacing(Vector2 direction)
        {
            float horizontal = Mathf.Abs(direction.x);
            float vertical = Mathf.Abs(direction.y);
            if (Mathf.Abs(horizontal - vertical) <= FacingHysteresis
                && Vector2.Dot(direction, animationFacing) > 0f)
                return;

            animationFacing = horizontal >= vertical
                ? new Vector2(Mathf.Sign(direction.x), 0f)
                : new Vector2(0f, Mathf.Sign(direction.y));
        }
    }
}
