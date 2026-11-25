using System;
using System.Linq;
using SOLITUDE.Survivors;
using UnityEngine;
using UnityEngine.Tilemaps;
namespace SOLITUDE.Player.Succession
{
    public sealed class SurvivorAvatarAdapter : MonoBehaviour
    {
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private PlayerVitalsController vitals;
        public PlayerInteractor Interactor => interactor;
        public bool IsAvailable => interactor != null && movement != null && vitals != null
            && interactor.GetComponent<Rigidbody2D>() != null;
        public bool UsedResumeFallback { get; private set; }
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
            UsedResumeFallback = false;
            Relocate(point.transform.position);
            vitals.ResetForAwakening();
        }
        private void Relocate(Vector3 position)
        {
            movement.SetInput(Vector2.zero);
            var body = interactor.GetComponent<Rigidbody2D>();
            body.linearVelocity = Vector2.zero; body.angularVelocity = 0;
            interactor.transform.position = position;
            body.position = position;
            Physics2D.SyncTransforms();
            interactor.RebuildProximityAfterRelocation();
        }
        public SurvivorSnapshot Capture(Guid identity)
        {
            Validate();
            var body = interactor.GetComponent<Rigidbody2D>();
            var snapshot = new SurvivorSnapshot { id = identity.ToString("D"), scenePath = gameObject.scene.path,
                x = body.position.x, y = body.position.y, z = interactor.transform.position.z };
            vitals.CaptureVitals(snapshot); snapshot.Validate();
            return snapshot;
        }
        public void ValidateResumePoint(SurvivorAwakeningPoint point)
        {
            Validate();
            if (point == null || point.gameObject.scene != gameObject.scene || !point.gameObject.activeInHierarchy
                || !HasFloorAt(point.transform.position))
                throw new InvalidOperationException("A same-scene awakening/safe point on authored floor is required.");
        }
        private bool HasFloorAt(Vector3 position)
        {
            if (float.IsNaN(position.x) || float.IsInfinity(position.x) || float.IsNaN(position.y) || float.IsInfinity(position.y)
                || float.IsNaN(position.z) || float.IsInfinity(position.z)) return false;
            // Existing noncolliding tilemaps provide the authored floor boundary. Fixtures
            // without tilemaps can still validate landing against their solid geometry.
            var floors = gameObject.scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Tilemap>(false))
                .Where(map => map.GetComponent<Collider2D>() == null).ToArray();
            return floors.Length == 0 || floors.Any(map => map.HasTile(map.WorldToCell(position)));
        }
        private bool CanLandAt(Vector3 position)
        {
            if (!HasFloorAt(position)) return false;
            Physics2D.SyncTransforms();
            var body = interactor.GetComponent<Rigidbody2D>();
            foreach (var collider in interactor.GetComponentsInChildren<Collider2D>())
            {
                if (!collider.enabled || collider.isTrigger || collider.attachedRigidbody != body) continue;
                var bounds = collider.bounds;
                var center = (Vector2)bounds.center + (Vector2)position - body.position;
                // Leave the contact offset at the landing boundary, not a full cell margin.
                var size = new Vector2(Mathf.Max(.001f, bounds.size.x - 2 * Physics2D.defaultContactOffset),
                    Mathf.Max(.001f, bounds.size.y - 2 * Physics2D.defaultContactOffset));
                if (Physics2D.OverlapBoxAll(center, size, 0, Physics2D.GetLayerCollisionMask(collider.gameObject.layer))
                    .Any(hit => hit.enabled && !hit.isTrigger && hit.attachedRigidbody != body && hit.gameObject.scene == gameObject.scene))
                    return false;
            }
            return true;
        }
        public void Restore(SurvivorSnapshot snapshot, SurvivorAwakeningPoint fallback, bool restoreLocation)
        {
            ValidateResumePoint(fallback); vitals.ValidateRestore(snapshot);
            if (restoreLocation && snapshot.scenePath != gameObject.scene.path)
                throw new System.IO.InvalidDataException("Saved resume belongs to another scene.");
            var target = restoreLocation ? new Vector3((float)snapshot.x, (float)snapshot.y, (float)snapshot.z)
                : interactor.transform.position;
            bool useFallback = !CanLandAt(target);
            if (useFallback)
            {
                target = fallback.transform.position;
                if (!CanLandAt(target)) throw new System.IO.InvalidDataException("The scene's safe point is obstructed.");
            }
            // Validate everything before changing either position or vitals.
            UsedResumeFallback = useFallback;
            Relocate(target); vitals.RestoreVitals(snapshot);
        }
    }
}
