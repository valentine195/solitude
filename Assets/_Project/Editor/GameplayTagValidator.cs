using System;
using System.Collections.Generic;
using SOLITUDE.GameplayTags;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace SOLITUDE.Editor
{
    public static class GameplayTagValidator
    {
        public const string AssetFolder = "Assets/_Project/Systems/GameplayTags/Assets";
        public const string RegistryPath = AssetFolder + "/Registry.asset";

        [MenuItem("SOLITUDE/Validation/Validate Gameplay Tags")]
        public static void ValidateForCI()
        {
            var paths = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:GameplayTagRegistryAsset")) paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            paths.Sort(StringComparer.Ordinal);
            var errors = new List<string>();
            foreach (var path in paths)
            {
                var registry = AssetDatabase.LoadAssetAtPath<GameplayTagRegistryAsset>(path);
                foreach (var error in registry.CollectErrors()) errors.Add(path + ": " + error);
            }
            if (errors.Count != 0) throw new BuildFailedException(string.Join("\n", errors));
            Debug.Log("[GameplayTagValidator] Validated " + paths.Count + " gameplay tag registries.");
        }

        [MenuItem("SOLITUDE/Gameplay Tags/Create Initial Registry")]
        public static void CreateInitialRegistry()
        {
            if (AssetDatabase.LoadAssetAtPath<GameplayTagRegistryAsset>(RegistryPath) != null)
            { ValidateForCI(); return; }
            EnsureFolder(AssetFolder);
            var definitions = new List<GameplayTagDefinition>();
            var item = Definition("Item", "Item", null, definitions);
            var weapon = Definition("Weapon", "Weapon", null, definitions);
            var damage = Definition("Damage", "Damage", null, definitions);
            var tool = Definition("Tool", "Tool", null, definitions);
            Definition("ItemWeapon", "Weapon", item, definitions);
            Definition("ItemTool", "Tool", item, definitions);
            Definition("ItemImprovised", "Improvised", item, definitions);
            Definition("WeaponMelee", "Melee", weapon, definitions);
            Definition("DamageBlunt", "Blunt", damage, definitions);
            Definition("ToolMechanical", "Mechanical", tool, definitions);
            var registry = ScriptableObject.CreateInstance<GameplayTagRegistryAsset>();
            registry.Configure(definitions);
            registry.Compile();
            AssetDatabase.CreateAsset(registry, RegistryPath);
            EditorUtility.SetDirty(registry); AssetDatabase.SaveAssets(); ValidateForCI();
        }

        private static GameplayTagDefinition Definition(string filename, string segment, GameplayTagDefinition parent,
            List<GameplayTagDefinition> definitions)
        {
            string path = AssetFolder + "/" + filename + ".asset";
            var definition = AssetDatabase.LoadAssetAtPath<GameplayTagDefinition>(path);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<GameplayTagDefinition>();
                AssetDatabase.CreateAsset(definition, path);
                definition.Configure(segment, parent); EditorUtility.SetDirty(definition);
            }
            definitions.Add(definition); return definition;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int split = path.LastIndexOf('/'); string parent = path.Substring(0, split);
            EnsureFolder(parent); AssetDatabase.CreateFolder(parent, path.Substring(split + 1));
        }
    }
}
