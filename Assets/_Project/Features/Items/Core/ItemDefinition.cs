using UnityEngine;

namespace SOLITUDE.Items
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

        // Stable, save-portable identity - a raw ScriptableObject reference
        // can't be written into a plain JSON/binary save file. Derived from
        // the asset's own GUID rather than DisplayName, so renaming an item
        // in the Inspector never breaks a save that already references it -
        // only deleting and recreating the asset would (rare, and would
        // break any other reference to it too).
        [SerializeField] private string itemId;
        public string ItemId => itemId;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(itemId))
            {
                string path = UnityEditor.AssetDatabase.GetAssetPath(this);
                if (!string.IsNullOrEmpty(path))
                    itemId = UnityEditor.AssetDatabase.AssetPathToGUID(path);
            }
        }
#endif
    }
}