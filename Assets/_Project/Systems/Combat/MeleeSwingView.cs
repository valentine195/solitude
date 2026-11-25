using UnityEngine;
using SOLITUDE.Items;
namespace SOLITUDE.Combat
{
    /// <summary>One shared sprite transform animation; item/configuration supply the appearance and motion.</summary>
    public sealed class MeleeSwingView : MonoBehaviour
    {
        [SerializeField] private Vector2 originOffsetTiles = new Vector2(0, 1);
        private SpriteRenderer held;
        private MeleeAttackExecution owner;
        private MeleeImpactFeedback feedback;
        public SpriteRenderer HeldRenderer => held;
        public void Begin(MeleeAttackExecution execution)
        {
            if (owner != null && owner != execution) throw new System.InvalidOperationException("Swing view is already owned.");
            owner = execution;
            feedback ??= GetComponent<MeleeImpactFeedback>() ?? gameObject.AddComponent<MeleeImpactFeedback>();
            feedback.Begin(execution);
            if (held == null)
            {
                var visual = new GameObject("HeldItemSwing"); visual.transform.SetParent(transform, false);
                held = visual.AddComponent<SpriteRenderer>();
                var actorSprite = GetComponentInChildren<SpriteRenderer>();
                if (actorSprite != null && actorSprite != held)
                { held.sortingLayerID = actorSprite.sortingLayerID; held.sortingOrder = actorSprite.sortingOrder + 1; }
            }
            held.sprite = execution.Context.HeldSprite;
            held.enabled = held.sprite != null;
            Present(execution);
        }
        public void Present(MeleeAttackExecution execution)
        {
            if (owner != execution || held == null) return;
            var spec = execution.Configuration; var timing = execution.Timing;
            float progress = (float)timing.PhaseProgress;
            float angle = timing.Phase == MeleeAttackPhase.Windup ? Mathf.Lerp(spec.RestAngle, spec.StartAngle, progress) :
                timing.Phase == MeleeAttackPhase.Active ? Mathf.Lerp(spec.StartAngle, spec.EndAngle, progress) :
                Mathf.Lerp(spec.EndAngle, spec.RestAngle, progress);
            var direction = execution.Context.Direction;
            float facing = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            var rotation = Quaternion.Euler(0, 0, facing + angle - 90);
            held.transform.localRotation = rotation;
            held.transform.localPosition = (Vector3)originOffsetTiles * execution.Context.UnitsPerTile +
                rotation * Vector3.up * (spec.HeldDistanceTiles * execution.Context.UnitsPerTile);
        }
        public void End(MeleeAttackExecution execution)
        {
            if (owner != execution) return;
            feedback?.End(execution);
            owner = null; if (held != null) held.enabled = false;
        }
        private void OnDisable() { if (owner != null) feedback?.End(owner); if (held != null) held.enabled = false; }
        private void OnDestroy() { owner = null; }
    }
}
