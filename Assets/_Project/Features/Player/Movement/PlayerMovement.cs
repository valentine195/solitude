using SOLITUDE.Movement;
using UnityEngine;
namespace SOLITUDE.Player
{
    [RequireComponent(typeof(BoxCollider2D)), RequireComponent(typeof(MovementBehavior))]
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5;
        [SerializeField] private Transform cameraTransform;
        private MovementBehavior movement;
        public void SetInput(Vector2 vector)
        { if (movement == null) movement = GetComponent<MovementBehavior>(); movement.SetVector(vector); }
    }
}
