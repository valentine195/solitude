using SOLITUDE.GameplayTags;
using UnityEngine;

namespace SOLITUDE.Combat
{
    public interface IDamageRecipient
    {
        GameObject Entity { get; }
        bool IsAvailable { get; }
        // Null means untagged, not an invalid or partially compiled tag set.
        GameplayTagSet Tags { get; }
        DamageResult ApplyDamage(in DamageContext context, double amount);
    }
}
