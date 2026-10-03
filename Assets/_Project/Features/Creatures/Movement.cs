using SOLITUDE.Core.Animations;
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
        }

        private void FixedUpdate()

        {

            body.MovePosition(

                body.position +

                vector * moveSpeed * Time.fixedDeltaTime);

            animationController.SetMovement(vector);

        }
    }
}
