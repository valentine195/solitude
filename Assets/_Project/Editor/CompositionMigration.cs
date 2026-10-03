using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SOLITUDE.Composition;
using SOLITUDE.Containers;
using SOLITUDE.Core.Input;
using SOLITUDE.Core.Systems;
using SOLITUDE.Features.Interactables;
using SOLITUDE.Items;
using SOLITUDE.Player;
using SOLITUDE.SaveLoad;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Editor;
using UnityEngine.InputSystem.UI;
namespace SOLITUDE.Editor
{
    public static class CompositionMigration
    {
        private static T[] Components<T>() where T : Component => EditorSceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();
        private static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
        { var serialized = new SerializedObject(target); serialized.FindProperty(field).objectReferenceValue = value; serialized.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(target); }
        private static void SetArray(UnityEngine.Object target, string field, IEnumerable<UnityEngine.Object> values)
        { var serialized = new SerializedObject(target); var property = serialized.FindProperty(field); var copy = values.ToArray(); property.arraySize = copy.Length; for (int i = 0; i < copy.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = copy[i]; serialized.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(target); }
        [MenuItem("SOLITUDE/Migrations/Compose Wakeup Runtime")]
        public static void ConfigureWakeup()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/Wakeup.unity" || EditorApplication.isPlaying) throw new InvalidOperationException("Open Wakeup in Edit Mode before migration.");
            var persistence = Components<SaveGameService>().Single();
            var game = Components<GameManager>().Single(); var clock = Components<TimeSystem>().Single();
            if (game.gameObject != persistence.gameObject) throw new InvalidOperationException("The dedicated game bootstrap must own GameManager and persistence.");
            var root = persistence.GetComponent<GameCompositionRoot>(); if (root == null) root = Undo.AddComponent<GameCompositionRoot>(persistence.gameObject);
            var input = persistence.GetComponent<UnityInputHost>(); if (input == null) input = Undo.AddComponent<UnityInputHost>(persistence.gameObject);
            var configPath = "Assets/_Project/Core/Composition/GameRuntimeConfig.asset";
            var config = AssetDatabase.LoadAssetAtPath<GameRuntimeConfig>(configPath);
            if (config == null) { config = ScriptableObject.CreateInstance<GameRuntimeConfig>(); AssetDatabase.CreateAsset(config, configPath); }
            config.database = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/Resources/Database.asset");
            config.lootTables = AssetDatabase.FindAssets("t:LootTable").Select(g => AssetDatabase.LoadAssetAtPath<LootTable>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
            EditorUtility.SetDirty(config);
            Set(root, "config", config); Set(root, "persistence", persistence); Set(root, "input", input); Set(root, "clock", clock); Set(root, "game", game);
            if (!clock.transform.IsChildOf(root.transform)) clock.transform.SetParent(root.transform, true);
            var scope = Components<SceneBindings>().SingleOrDefault(); if (scope == null) scope = new GameObject("SceneBindings").AddComponent<SceneBindings>();
            Set(scope, "inventory", Components<PlayerInventory>().Single()); Set(scope, "hotbar", Components<SOLITUDE.Hotbar.Hotbar>().Single());
            Set(scope, "movement", Components<PlayerMovement>().Single()); Set(scope, "interactor", Components<PlayerInteractor>().Single());
            Set(scope, "inventoryModal", Components<ContainerModalView>().Single(v => v.Type == ContainerUIType.PlayerInventory));
            Set(scope, "transferModal", Components<ContainerTransferModalView>().Single()); Set(scope, "heldView", Components<ContainerHeldItemView>().Single());
            SetArray(scope, "controllers", Components<ContainerController>()); SetArray(scope, "tooltips", Components<ContainerTooltipView>());
            SetArray(scope, "lockers", Components<LockerContainer>()); SetArray(scope, "worldPickups", Components<WorldPickup>());
            var module = Components<InputSystemUIInputModule>().Single(); Set(scope, "uiInput", module);
            const string actionPath = "Assets/_Project/Core/Input/SOLITUDE_InputActions.inputactions";
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(actionPath);
            if (actions.FindActionMap("UI", false) == null)
            {
                var copy = InputActionAsset.FromJson(actions.ToJson());
                copy.Disable();
                copy.AddActionMap(module.actionsAsset.FindActionMap("UI", true).Clone());
                File.WriteAllText(actionPath, copy.ToJson());
                UnityEngine.Object.DestroyImmediate(copy);
                AssetDatabase.ImportAsset(actionPath, ImportAssetOptions.ForceUpdate);
                actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(actionPath);
            }
            InputActionCodeGenerator.GenerateWrapperCode("Assets/_Project/Core/Input/SOLITUDE_InputActions.cs", actions,
                new InputActionCodeGenerator.Options { className = "SOLITUDE_InputActions", sourceAssetPath = actionPath });
            AssetDatabase.ImportAsset("Assets/_Project/Core/Input/SOLITUDE_InputActions.cs");
            scope.Validate(); AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Debug.Log("[CompositionMigration] Explicit Wakeup runtime wiring saved; existing UI/input identities retained.");
        }
    }
}
