using UnityEngine;

namespace SOLITUDE.Containers
{
    [CreateAssetMenu(menuName = "SOLITUDE/Items/Item Definition")]
    public class ItemDefinition : ScriptableObject
    {
        [Header("Display")]
        public string DisplayName;

        public Sprite Icon;

        [TextArea]
        public string Description;

        [Header("World")]
        public GameObject PickupPrefab;

        [Header("Inventory")]
        public bool Stackable = true;
        public int MaxStackSize = 99;
    }
}