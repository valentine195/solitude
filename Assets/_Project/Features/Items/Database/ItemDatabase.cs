using System.Collections.Generic;
using UnityEngine;

namespace SOLITUDE.Items
{
    /// <summary>
    /// Resolves a save file's ItemId string back into the real ItemDefinition
    /// asset. This is the one place that needs to know every item in the
    /// game - loot tables, save/load, and anything else that only has an id
    /// string (not a live reference) go through this rather than each
    /// maintaining their own lookup.
    ///
    /// Populate Items via the inspector (drag every ItemDefinition asset in)
    /// or via a build step that scans the project - either way, this asset
    /// itself is Resources-loaded so runtime code can
    /// reach it without a scene reference.
    /// </summary>
    [CreateAssetMenu(menuName = "SOLITUDE/Items/Item Database")]
    public class ItemDatabase : ScriptableObject
    {
        [SerializeField] private List<ItemDefinition> items = new();

        private Dictionary<string, ItemDefinition> byId;

        private static ItemDatabase instance;
        public static ItemDatabase Instance
        {
            get
            {
                if (instance == null)
                    instance = Resources.Load<ItemDatabase>("Database");

                if (instance == null)
                    Debug.LogError("[ItemDatabase] Could not load Resources/Database.asset. Create an Item Database asset at Assets/Resources/Database.asset and populate it.");
                return instance;
            }
        }

        private void BuildLookupIfNeeded()
        {
            if (byId != null) return;

            byId = new Dictionary<string, ItemDefinition>();

            foreach (var item in items)
            {
                if (item == null) continue;

                if (string.IsNullOrEmpty(item.ItemId))
                {
                    Debug.LogError($"[ItemDatabase] '{item.name}' has no ItemId - open it in the inspector once so it can be generated, then re-save this database.", item);
                    continue;
                }

                if (byId.ContainsKey(item.ItemId))
                {
                    Debug.LogError($"[ItemDatabase] Duplicate ItemId for '{item.name}' and '{byId[item.ItemId].name}'.", item);
                    continue;
                }

                byId.Add(item.ItemId, item);
            }
        }

        public ItemDefinition Resolve(string itemId)
        {
            BuildLookupIfNeeded();

            if (string.IsNullOrEmpty(itemId)) return null;

            if (byId.TryGetValue(itemId, out var item))
                return item;

            Debug.LogError($"[ItemDatabase] No item found for id '{itemId}' - was it removed from the database, or is this save from a build that no longer has it?");
            return null;
        }

        private void OnValidate()
        {
            byId = null;
        }

#if UNITY_EDITOR
        // Editor convenience - finds every ItemDefinition in the project and
        // fills Items automatically, so adding a new item doesn't require
        // remembering to also drag it into this list by hand.
        [ContextMenu("Populate From Project")]
        private void PopulateFromProject()
        {
            items.Clear();

            var guids = UnityEditor.AssetDatabase.FindAssets("t:ItemDefinition");
            foreach (var guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var item = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
                if (item != null) items.Add(item);
            }

            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[ItemDatabase] Populated {items.Count} items from project.");
        }
#endif
    }
}
