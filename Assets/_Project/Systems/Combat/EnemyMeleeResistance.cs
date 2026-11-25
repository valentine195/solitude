using UnityEngine;

namespace SOLITUDE.Combat
{
    [DisallowMultipleComponent]
    public sealed class EnemyMeleeResistance : MonoBehaviour
    {
        [SerializeField, Range(0, 1), Tooltip("Fraction of incoming melee damage absorbed.")]
        private float damageResistance;
        [SerializeField, Range(0, 1), Tooltip("Fraction of incoming melee push distance absorbed.")]
        private float knockbackResistance;

        public double ReceivedDamage(double amount) => amount * (1d - Mathf.Clamp01(damageResistance));
        public float ReceivedKnockback(float distance) => distance * (1f - Mathf.Clamp01(knockbackResistance));
    }
}
