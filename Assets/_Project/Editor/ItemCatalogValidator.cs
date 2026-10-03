using SOLITUDE.Items;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace SOLITUDE.Editor
{
    public sealed class ItemCatalogValidator
    {
        public int callbackOrder => -10;
        
        [MenuItem("SOLITUDE/Validation/Validate Item Catalog")]
        public static void ValidateForCI() => WakeupValidation.ValidateForCI();
        public static void AuditAllAssets()
        {
            try
            {
                var database = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/Resources/Database.asset");
                if (database == null) throw new System.InvalidOperationException("Resources/Database.asset is missing.");
                var catalog = database.BuildRuntimeCatalog();
                database.BuildPresentationCatalog();
                foreach (var guid in AssetDatabase.FindAssets("t:LootTable"))
                {
                    var table = AssetDatabase.LoadAssetAtPath<LootTable>(AssetDatabase.GUIDToAssetPath(guid));
                    table.Compile(catalog);
                }
                foreach (var guid in AssetDatabase.FindAssets("t:PickupDefinition"))
                {
                    var definition = AssetDatabase.LoadAssetAtPath<PickupDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                    if (definition.Item == null || !catalog.TryGet(definition.Item.ItemId, out _) || database.Resolve(definition.Item.ItemId) != definition.Item || definition.Quantity < 1)
                        throw new System.InvalidOperationException("Invalid pickup reward: " + AssetDatabase.GetAssetPath(definition));
                }
                Debug.Log("[ItemCatalogValidator] Item catalog, pickup rewards, and loot tables validated.");
            }
            catch (System.Exception error) { throw new BuildFailedException(error.Message); }
        }
    }
}
