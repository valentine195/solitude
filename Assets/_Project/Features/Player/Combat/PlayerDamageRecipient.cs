using SOLITUDE.Combat;
using SOLITUDE.GameplayTags;
using SOLITUDE.Vitals;
using UnityEngine;

namespace SOLITUDE.Player
{
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerVitalsController))]
    public sealed class PlayerDamageRecipient : MonoBehaviour, IDamageRecipient
    {
        private PlayerVitalsController vitals;
        public GameObject Entity => this != null ? gameObject : null;
        public GameplayTagSet Tags => GetComponent<GameplayTagSource>() is GameplayTagSource source && source.IsInitialized ? source.Tags : null;
        public bool IsAvailable => this != null && isActiveAndEnabled && vitals != null && vitals.GameplayEligible
            && vitals.Vitals != null && vitals.Vitals.Health.PreciseCurrent > 0
            && (GetComponent<GameplayTagSource>() is not GameplayTagSource source || source.IsInitialized);
        private void Awake() => vitals = GetComponent<PlayerVitalsController>();
        public DamageResult ApplyDamage(in DamageContext context, double amount)
        {
            if (!DamageDelivery.IsValid(context) || !ReferenceEquals(context.Target, this) || !DamageDelivery.Positive(amount))
                return new DamageResult(DamageOutcome.InvalidContext);
            if (!IsAvailable) return new DamageResult(DamageOutcome.Unavailable);
            double before = vitals.Vitals.Health.PreciseCurrent;
            var result = vitals.Damage(amount);
            return result.Outcome == VitalsRecoveryOutcome.Applied
                ? new DamageResult(DamageOutcome.Applied, before - result.Values.Health)
                : new DamageResult(DamageOutcome.Rejected);
        }
    }
}
