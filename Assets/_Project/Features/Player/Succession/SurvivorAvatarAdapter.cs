using System;
using UnityEngine;
namespace SOLITUDE.Player.Succession
{
    public sealed class SurvivorAvatarAdapter : MonoBehaviour
    {
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private PlayerVitalsController vitals;
        public PlayerInteractor Interactor => interactor;
        public void Configure(PlayerInteractor player)
        {
            interactor = player;
            movement = player.GetComponent<PlayerMovement>();
            vitals = player.GetComponent<PlayerVitalsController>();
        }
        public void Validate()
        {
            if (interactor == null || movement == null || vitals == null ||
                interactor.gameObject.scene != gameObject.scene || movement.gameObject != interactor.gameObject ||
                vitals.gameObject != interactor.gameObject || interactor.GetComponent<Rigidbody2D>() == null)
                throw new InvalidOperationException("Survivor avatar bindings are incomplete or belong to another scene/avatar.");
        }
        public void Awaken(SurvivorAwakeningPoint point)
        {
            Validate();
            if (point == null || point.gameObject.scene != gameObject.scene)
                throw new InvalidOperationException("Survivor awakening point is missing or belongs to another scene.");
            movement.SetInput(Vector2.zero);
            var body = interactor.GetComponent<Rigidbody2D>();
            body.linearVelocity = Vector2.zero; body.angularVelocity = 0;
            interactor.transform.position = point.transform.position;
            body.position = point.transform.position;
            Physics2D.SyncTransforms();
            interactor.RebuildProximityAfterRelocation();
            vitals.ResetForAwakening();
        }
    }
}
