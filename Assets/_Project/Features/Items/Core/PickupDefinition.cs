using UnityEngine;

namespace SOLITUDE.Items
{
    [CreateAssetMenu(menuName = "SOLITUDE/Items/Pickup Definition")]
    public sealed class PickupDefinition : ScriptableObject
    {
        [SerializeField] private ItemDefinition item;
        [SerializeField, Min(1)] private int quantity = 1;
        [SerializeField] private string successText;
        public ItemDefinition Item => item;
        public int Quantity => quantity;
        public string SuccessText => string.IsNullOrWhiteSpace(successText)
            ? $"Picked up {item?.DisplayName}" : successText;
    }
}
