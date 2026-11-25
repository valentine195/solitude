using System;
using System.IO;
using System.Linq;
using SOLITUDE.Composition;
using SOLITUDE.Core.Interaction;
using SOLITUDE.Items;
using SOLITUDE.Player;
using SOLITUDE.World.OpeningSlice;
using SOLITUDE.World.Restoration;
using SOLITUDE.World.SolitudeStart;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SOLITUDE.Editor
{
    public static class RestorationDemoBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Demos/ShipSystemRestoration.unity";
        private const string Source = "Assets/_Project/Art/SOLITUDE/ReferenceScenes/SOLITUDE_OpeningSlice.unity";
        private static T[] All<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();
        [MenuItem("SOLITUDE/Demos/Build Ship System Restoration")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before building the demo.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null) throw new InvalidOperationException("Demo already exists; refusing to overwrite.");
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath)); AssetDatabase.Refresh();
            if (!AssetDatabase.CopyAsset(Source, ScenePath)) throw new InvalidOperationException("Cannot copy opening scene.");
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                foreach (var c in All<OpeningReviewSession>(scene)) UnityEngine.Object.DestroyImmediate(c.gameObject);
                foreach (var c in All<OpeningFirstContact>(scene)) UnityEngine.Object.DestroyImmediate(c.gameObject);
                var marker = new GameObject("Disposable restoration demo session").AddComponent<RestorationDemoSession>();
                var battery = All<WorldPickup>(scene).Single(p => p.Definition.Item.ItemId == "c756a156ba7f34cb2b65e32264413a60");
                var socket = All<OpeningPowerSocket>(scene).Single(); var socketGO = socket.gameObject;
                UnityEngine.Object.DestroyImmediate(socket);
                var main = new GameObject("Local power system").AddComponent<RestorableSystem>();
                var junction = socketGO.AddComponent<RestorationJunction>(); junction.Configure(main, battery.Definition.Item);
                var door = All<SolitudeSlidingDoor>(scene).Single(); door.gameObject.AddComponent<RestorationDoorGate>().Configure(main);
                Sensor(door.gameObject, new Vector2(0, -.5f), new Vector2(2, 2));
                var terminalGO = scene.GetRootGameObjects().Single(g => g.name == "AI intercom");
                terminalGO.transform.position = new Vector3(23, 12.5f, 0);
                var terminal = terminalGO.AddComponent<RestorationTerminal>();
                terminal.Configure(main, terminalGO.GetComponent<SpriteRenderer>(), "AUXILIARY POWER ONLINE. AI fragment: Proceed to the next compartment.", new Color(.12f,.15f,.15f), Color.cyan);
                Sensor(terminalGO, Vector2.zero, new Vector2(2,2));
                var second = new GameObject("Independent service system").AddComponent<RestorableSystem>();
                var secondGO = UnityEngine.Object.Instantiate(socketGO); secondGO.name = "Independent service junction"; secondGO.transform.position = new Vector3(25.5f, 4.5f, 0);
                var secondJunction = secondGO.GetComponent<RestorationJunction>(); secondJunction.Configure(second, battery.Definition.Item);
                var screenGO = UnityEngine.Object.Instantiate(terminalGO); screenGO.name = "Independent service terminal"; screenGO.transform.position = new Vector3(26.5f, 6.5f, 0);
                screenGO.GetComponent<RestorationTerminal>().Configure(second, screenGO.GetComponent<SpriteRenderer>(), "SERVICE MONITOR ONLINE. Independent circuit restored.", new Color(.12f,.15f,.15f), Color.green);
                // A second guaranteed pickup supports both systems without random loot.
                var secondBattery = UnityEngine.Object.Instantiate(battery.gameObject); secondBattery.name = "Second power cell"; secondBattery.transform.position = new Vector3(24, 5, 0);
                var id = new SerializedObject(secondBattery.GetComponent<SOLITUDE.SaveLoad.SaveableId>()); id.FindProperty("id").stringValue = "restoration.demo.second-cell"; id.ApplyModifiedPropertiesWithoutUndo();
                var scope = All<SceneBindings>(scene).Single(); var serialized = new SerializedObject(scope);
                var list = serialized.FindProperty("restorationJunctions"); list.arraySize = 2; list.GetArrayElementAtIndex(0).objectReferenceValue = junction; list.GetArrayElementAtIndex(1).objectReferenceValue = secondJunction;
                var pickups = All<WorldPickup>(scene); list = serialized.FindProperty("worldPickups"); list.arraySize = pickups.Length;
                for (int i = 0; i < pickups.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = pickups[i];
                serialized.ApplyModifiedPropertiesWithoutUndo();
                All<PlayerInteractor>(scene).Single().transform.position = new Vector3(20, 4, 0);
                scope.Validate(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            }
            finally { EditorSceneManager.CloseScene(scene, true); if (previous.IsValid()) SceneManager.SetActiveScene(previous); }
            Debug.Log("Restoration demo created: " + ScenePath);
        }
        private static void Sensor(GameObject owner, Vector2 offset, Vector2 size)
        {
            var range = new GameObject("Interaction range", typeof(BoxCollider2D), typeof(InteractableProximitySensor));
            range.transform.SetParent(owner.transform, false); var box = range.GetComponent<BoxCollider2D>(); box.isTrigger = true; box.offset = offset; box.size = size;
        }
    }
}
