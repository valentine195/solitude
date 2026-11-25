using System;
using System.IO;
using System.Linq;
using SOLITUDE.Composition;
using SOLITUDE.Player;
using SOLITUDE.SaveLoad;
using SOLITUDE.World.Restoration;
using SOLITUDE.World.SolitudeStart;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SOLITUDE.Editor
{
    public static class MechanicalRepairFixtureBuilder
    {
        public const string ScenePath = "Assets/_Project/Tests/Fixtures/MechanicalRepair.unity";
        public const string TargetId = "verification.mechanical.auxiliary-socket";
        private static T[] All<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();
        public static void Prepare()
        {
            if (EditorApplication.isPlaying || !Environment.GetCommandLineArgs().Contains("-solitudeMechanicalRepairFixture")
                || Directory.Exists(Path.Combine(Directory.GetParent(UnityEngine.Application.dataPath).FullName, ".git"))
                || File.Exists(Path.Combine(Directory.GetParent(UnityEngine.Application.dataPath).FullName, ".git")))
                throw new InvalidOperationException("Mechanical repair fixture requires an explicitly opted-in disposable verification project.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null) throw new InvalidOperationException("Refusing to overwrite an existing fixture.");
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath)); AssetDatabase.Refresh();
            if (!AssetDatabase.CopyAsset(RestorationDemoBuilder.ScenePath, ScenePath)) throw new InvalidOperationException("Cannot copy authored restoration arrangement.");
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            foreach (var marker in All<RestorationDemoSession>(scene)) UnityEngine.Object.DestroyImmediate(marker.gameObject);
            var scope = All<SceneBindings>(scene).Single();
            var junction = All<RestorationJunction>(scene).Single(j => j.name == "Auxiliary power socket");
            var system = (RestorableSystem)new SerializedObject(junction).FindProperty("system").objectReferenceValue;
            var gate = All<RestorationDoorGate>(scene).Single();
            if (!gate.IsConfiguredFor(system)) throw new InvalidOperationException("Selected authored socket and door are not linked.");
            var identity = junction.GetComponent<SaveableId>() ?? junction.gameObject.AddComponent<SaveableId>();
            var id = new SerializedObject(identity); id.FindProperty("id").stringValue = TargetId; id.ApplyModifiedPropertiesWithoutUndo();
            var repair = scope.gameObject.AddComponent<MechanicalRepairComposition>();
            repair.Configure(identity, AssetDatabase.LoadAssetAtPath<MechanicalRepairDefinition>("Assets/_Project/Features/World/Restoration/AuxiliarySocketRepair.asset"), junction, system, gate);
            var vitals = scope.gameObject.AddComponent<PlayerVitalsComposition>(); vitals.Configure(All<PlayerVitalsController>(scene).Single());
            var data = new SerializedObject(scope); var features = data.FindProperty("features");
            features.arraySize = 2; features.GetArrayElementAtIndex(0).objectReferenceValue = vitals; features.GetArrayElementAtIndex(1).objectReferenceValue = repair;
            data.ApplyModifiedPropertiesWithoutUndo();
            SurvivorSuccessionAuthoring.ConfigureScene(scene, "MechanicalRepairAwakeningPoint");
            scope.Validate(); repair.Validate();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            // Authoring never changes shipping build settings. Verification builds opt in separately.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Debug.Log("Created disposable production-session fixture: " + ScenePath + "; target " + TargetId);
        }
    }
}
