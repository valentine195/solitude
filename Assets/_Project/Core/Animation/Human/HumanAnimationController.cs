using UnityEngine;
using System;
using SOLITUDE.Application;

namespace SOLITUDE.Core.Animations
{
    [RequireComponent(typeof(Animator))]
    public class HumanAnimationController : MonoBehaviour, ICreatureAnimationController
    {
        private Animator animator;

        private Vector2 moveInput;
        private bool isMoving;
        private object facingOwner;
        private Vector2 lockedFacing;
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
            if (animator == null || animator.runtimeAnimatorController == null) return;
            animator.SetFloat("MoveX", facing.x);
            animator.SetFloat("MoveY", facing.y);
            animator.SetBool("IsMoving", isMoving);
        }
    }
}