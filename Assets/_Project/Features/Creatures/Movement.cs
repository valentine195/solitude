using SOLITUDE.Core.Animations;
using System;
using System.Collections.Generic;
using SOLITUDE.Application;
using UnityEngine;

namespace SOLITUDE.Movement
{

    [RequireComponent(typeof(Rigidbody2D))]
    public class MovementBehavior : MonoBehaviour
    {
        [SerializeField] Rigidbody2D body;

        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;

        private ICreatureAnimationController animationController;
        private Vector2 vector;
        private readonly HashSet<object> movementBlocks = new();
        private readonly Dictionary<object, float> speedRestrictions = new();
        public Vector2 FacingDirection { get; private set; } = Vector2.down;
        public float SpeedFactor
        {
            get { float factor = 1; foreach (var value in speedRestrictions.Values) factor = Mathf.Min(factor, value); return factor; }
        }
        public IDisposable RestrictSpeed(float factor)
        {
            if (float.IsNaN(factor) || float.IsInfinity(factor) || factor < 0 || factor > 1) throw new ArgumentOutOfRangeException(nameof(factor));
            var token = new object(); speedRestrictions.Add(token, factor);
            return new Lease(() => speedRestrictions.Remove(token));
        }
        public bool IsMovementBlocked => movementBlocks.Count != 0;
        public IDisposable BlockMovement()
        {
            var token=new object();movementBlocks.Add(token);
            if(body==null) body=GetComponent<Rigidbody2D>();
            body.linearVelocity=Vector2.zero;
            return new Lease(()=>movementBlocks.Remove(token));
        }

        private void Start()
        {
            animationController = GetComponent<ICreatureAnimationController>();
            if (animationController == null)
                Debug.LogError($"No ICreatureAnimationController found on {gameObject.name}", gameObject);
        }
        private void OnValidate()
        {
            if (animationController == null)
                animationController = GetComponent<ICreatureAnimationController>();
        }

        public void SetVector(Vector2 vector)
        {

            if (vector.sqrMagnitude > 1f)
                vector.Normalize();

            this.vector = vector;
            if (vector.sqrMagnitude > .0001f) FacingDirection = vector.normalized;
        }

        private void FixedUpdate()

        {

            if(body==null) body=GetComponent<Rigidbody2D>();
            var effective=IsMovementBlocked?Vector2.zero:vector * SpeedFactor;
            body.MovePosition(body.position + effective * moveSpeed * Time.fixedDeltaTime);
            animationController?.SetMovement(effective);

        }
    }
}
