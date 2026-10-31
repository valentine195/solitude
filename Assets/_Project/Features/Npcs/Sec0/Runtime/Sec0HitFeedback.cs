using System;
using SOLITUDE.Combat;
using SOLITUDE.Items;
using UnityEngine;

namespace SOLITUDE.Npcs.Sec0
{
    [DisallowMultipleComponent, RequireComponent(typeof(Sec0Controller))]
    public sealed class Sec0HitFeedback : MonoBehaviour, IMeleeHitFeedback
    {
        [SerializeField] private Color flashColor = new Color(1, .85f, .65f, 1);
        private Sec0Controller controller;
        private SpriteRenderer visual;
        private Color original;
        private bool painted;
        private float remaining;
        private Lease owner;
        public IDisposable Present(in DamageContext contact, MeleeAttackSpec configuration, float unitsPerTile)
        {
            controller ??= GetComponent<Sec0Controller>();
            if (!isActiveAndEnabled || !controller.CanPresentFeedback) return null;
            Clear(); visual ??= GetComponentInChildren<Animator>()?.GetComponent<SpriteRenderer>();
            owner = new Lease(this);
            remaining = configuration.FlashDuration;
            controller.RequestKnockback(owner, contact.Direction, configuration.KnockbackTiles * unitsPerTile, configuration.KnockbackDuration);
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
            original = visual.color; visual.color = flashColor; painted = true;
        }
        private void Restore() { if (painted && visual != null) visual.color = original; painted = false; }
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
