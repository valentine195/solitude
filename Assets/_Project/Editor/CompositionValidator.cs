using System;
using System.Linq;
using SOLITUDE.Composition;
using SOLITUDE.Core.Input;
using SOLITUDE.Items;
using SOLITUDE.Player;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
namespace SOLITUDE.Editor
{
    public sealed class CompositionValidator
    {
        public int callbackOrder => 10;
        
        [MenuItem("SOLITUDE/Validation/Validate Runtime Composition")]
        public static void ValidateForCI()
        {
            WakeupValidation.ValidateForCI();
        }
        internal static void ValidateScene(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            var scopes = roots.SelectMany(r => r.GetComponentsInChildren<SceneBindings>(true)).ToArray();
            var bootstraps = roots.SelectMany(r => r.GetComponentsInChildren<GameCompositionRoot>(true)).ToArray();
            if (scopes.Length == 0 && bootstraps.Length == 0) return; // Unmigrated scenes are outside this composition graph.
            try
            {
                if (scopes.Length != 1 || bootstraps.Length > 1) throw new InvalidOperationException("Expected one scene scope and at most one bootstrap.");
                scopes[0].Validate();
                foreach (var root in bootstraps)
                {
                    var authoring = new SerializedObject(root);
                    foreach (var field in new[] { "config", "persistence", "input", "clock", "game" })
                        if (authoring.FindProperty(field).objectReferenceValue == null) throw new InvalidOperationException("Missing bootstrap " + field);
                    var config = (GameRuntimeConfig)authoring.FindProperty("config").objectReferenceValue;
                    if (config.database == null) throw new InvalidOperationException("Missing item database.");
                    var catalog = config.database.BuildRuntimeCatalog();
                    if (config.lootTables.Any(t => t == null) || config.lootTables.Distinct().Count() != config.lootTables.Length)
                        throw new InvalidOperationException("Invalid/duplicate configured loot table.");
                    foreach (var table in config.lootTables) table.Compile(catalog);
                }
                var hosts = roots.SelectMany(r => r.GetComponentsInChildren<UnityInputHost>(true)).ToArray();
                if (hosts.Length != bootstraps.Length) throw new InvalidOperationException("Each bootstrap must have exactly one input host.");
                var scopeFields = new SerializedObject(scopes[0]);
                foreach (var tuple in new[] { ("lockers", typeof(SOLITUDE.Features.Interactables.LockerContainer)), ("worldPickups", typeof(WorldPickup)), ("controllers", typeof(SOLITUDE.Containers.ContainerController)) })
                {
                    var list = scopeFields.FindProperty(tuple.Item1);
                    var configured = Enumerable.Range(0, list.arraySize).Select(i => list.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
                    var authored = roots.SelectMany(r => r.GetComponentsInChildren(tuple.Item2, true)).ToArray();
                    if(configured.Any(c=>c==null)||configured.Distinct().Count()!=configured.Length||configured.Any(c=>!authored.Contains(c)))
                        throw new InvalidOperationException("Null, duplicate or wrong-scene endpoint in "+tuple.Item1);
                    if (authored.Any(c => !configured.Contains(c))) throw new InvalidOperationException("Unbound authored endpoint in " + tuple.Item1);
                }
                var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_Project/Core/Input/SOLITUDE_InputActions.inputactions");
                foreach (var name in new[] { "Gameplay/Move", "Gameplay/Interact", "Gameplay/Inventory", "Gameplay/Pause", "Container/Cancel", "Container/Point", "UI/Point", "UI/Click" })
                    if (actions == null || actions.FindAction(name, false) == null) throw new InvalidOperationException("Missing input action " + name);
            }
            catch (Exception error) { throw new BuildFailedException(scene.path + ": " + error.Message); }
        }
    }
}
