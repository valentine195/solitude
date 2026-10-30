using System;
using SOLITUDE.Application;
using SOLITUDE.Combat;
using SOLITUDE.GameplayTags;
using SOLITUDE.Player.Vitals;
using UnityEngine;

namespace SOLITUDE.Npcs.Sec0
{
    [DisallowMultipleComponent, RequireComponent(typeof(Sec0Controller))]
    public sealed class Sec0DamageRecipient : MonoBehaviour, IDamageRecipient
    {
        [SerializeField, Min(.01f)] private float maximumHealth = 40;
        private Vital health;
        private Sec0Controller controller;
        private InputPolicyCoordinator policy;
        private object owner;
        public GameObject Entity => this != null ? gameObject : null;
        public double MaximumHealth => health?.PreciseMax ?? maximumHealth;
        public double CurrentHealth => health?.PreciseCurrent ?? 0;
        public bool IsDefeated => controller != null && controller.IsDefeated;
        public event Action Defeated;
        public GameplayTagSet Tags => GetComponent<GameplayTagSource>() is GameplayTagSource source && source.IsInitialized ? source.Tags : null;
        public bool IsAvailable => this != null && isActiveAndEnabled && owner != null && policy.Gameplay
            && controller != null && controller.isActiveAndEnabled && controller.HasBinding && !IsDefeated && CurrentHealth > 0
            && (GetComponent<GameplayTagSource>() is not GameplayTagSource source || source.IsInitialized);
        public void ValidateAuthoring()
        {
            if (!DamageDelivery.Positive(maximumHealth)) throw new InvalidOperationException("SEC-0 maximum health must be finite and positive.");
        }
        public void Bind(object bindingOwner, InputPolicyCoordinator gameplay)
        {
            if (bindingOwner == null || gameplay == null) throw new ArgumentNullException(nameof(bindingOwner));
            if (owner != null && !ReferenceEquals(owner, bindingOwner)) throw new InvalidOperationException("SEC-0 health already owned.");
            ValidateAuthoring(); controller = GetComponent<Sec0Controller>();
            if (health == null)
            {
                health = new Vital(maximumHealth);
                health.Depleted += OnDepleted;
            }
            owner = bindingOwner; policy = gameplay;
        }
        public DamageResult ApplyDamage(in DamageContext context, double amount)
        {
            if (!DamageDelivery.IsValid(context) || !ReferenceEquals(context.Target, this) || !DamageDelivery.Positive(amount))
                return new DamageResult(DamageOutcome.InvalidContext);
            if (!IsAvailable) return new DamageResult(DamageOutcome.Unavailable);
            double before = health.PreciseCurrent;
            var change = health.ApplySilently(health.PreciseMax, Math.Max(0, before - amount));
            double applied = before - health.PreciseCurrent;
            health.Publish(change);
            return applied > 0 ? new DamageResult(DamageOutcome.Applied, applied) : new DamageResult(DamageOutcome.Rejected);
        }
        private void OnDepleted(Vital _)
        {
            controller.Defeat();
            Defeated?.Invoke();
        }
        public void Release(object bindingOwner)
        { if (ReferenceEquals(owner, bindingOwner)) { owner = null; policy = null; } }
        private void OnDestroy()
        { if (health != null) health.Depleted -= OnDepleted; owner = null; policy = null; }
    }
}
