using System;
using SOLITUDE.Combat;
using SOLITUDE.Items;
using UnityEngine;

namespace SOLITUDE.Npcs.Sec0
{
    [DisallowMultipleComponent, RequireComponent(typeof(Sec0Controller))]
    public sealed class Sec0HitFeedback : MonoBehaviour, IMeleeHitFeedback
    {
        [SerializeField] private Color flashColor = new Color(1, .48f, .32f, 1);
        private Sec0Controller controller;
        private SpriteRenderer visual;
        private Color original;
        private Vector3 originalScale;
        private float duration;
        private bool painted;
        private float remaining;
        private Lease owner;
        public IDisposable Present(in DamageContext contact, MeleeAttackSpec configuration, float unitsPerTile)
        {
            controller ??= GetComponent<Sec0Controller>();
            if (!isActiveAndEnabled || !controller.CanPresentFeedback) return null;
            Clear(); visual ??= GetComponentInChildren<Animator>()?.GetComponent<SpriteRenderer>();
            owner = new Lease(this);
            remaining = duration = configuration.FlashDuration;
            float distance = configuration.KnockbackTiles * unitsPerTile;
            if (TryGetComponent<EnemyMeleeResistance>(out var resistance) && resistance.isActiveAndEnabled)
                distance = resistance.ReceivedKnockback(distance);
            controller.RequestKnockback(owner, contact.Direction, distance, configuration.KnockbackDuration);
            Paint(); return owner;
        }
        // Restore before Animator evaluates; capture its current warning color in LateUpdate.
        private void Update()
        {
            Restore();
            if (owner == null) return;
            if (controller == null || !controller.HasBinding || controller.IsDefeated) { Clear(); return; }
            if (controller.CanPresentFeedback) remaining = Mathf.Max(0, remaining - Time.deltaTime);
        }
        private void LateUpdate() => Paint();
        private void Paint()
        {
            if (remaining <= 0 || visual == null || painted) return;
            original = visual.color;
            originalScale = visual.transform.localScale;
            float recoil = remaining / duration;
            visual.transform.localScale = Vector3.Scale(originalScale, new Vector3(1 + .12f * recoil, 1 - .18f * recoil, 1));
            visual.color = Color.Lerp(original, flashColor, recoil);
            painted = true;
        }
        private void Restore()
        {
            if (painted && visual != null)
            {
                visual.color = original;
                visual.transform.localScale = originalScale;
            }
            painted = false;
        }
        public void Clear()
        {
            Restore(); remaining = 0;
            if (owner != null) { controller?.CancelKnockback(owner); owner.Detach(); owner = null; }
        }
        private void OnDisable() => Clear();
        private void OnDestroy() => Clear();
        private sealed class Lease : IDisposable
        {
            private Sec0HitFeedback target;
            public Lease(Sec0HitFeedback target) => this.target = target;
            public void Detach() => target = null;
            public void Dispose() { if (target != null && ReferenceEquals(target.owner, this)) target.Clear(); target = null; }
        }
    }
}
