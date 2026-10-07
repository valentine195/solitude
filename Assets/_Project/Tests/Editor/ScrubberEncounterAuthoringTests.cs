using System.Linq;
using NUnit.Framework;
using SOLITUDE.Items;
using SOLITUDE.Composition;
using SOLITUDE.SaveLoad;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace SOLITUDE.Tests
{
    public class ScrubberEncounterAuthoringTests
    {
        private const string Folder = "Assets/_Project/Features/Items/Definitions/ReplacementFuse";
        [Test] public void FuseCatalogAndSceneInstanceHaveDistinctStableIdentities()
        {
            var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(Folder + "/ReplacementFuse.asset");
            Assert.IsNotNull(item); Assert.AreEqual(AssetDatabase.AssetPathToGUID(Folder + "/ReplacementFuse.asset"),item.ItemId);
            Assert.AreEqual("Replacement fuse",item.DisplayName); Assert.IsNotNull(item.Icon); Assert.IsNotNull(item.PickupPrefab);
            Assert.AreEqual(Vector3.one,item.PickupPrefab.transform.localScale); Assert.IsTrue(string.IsNullOrEmpty(item.PickupPrefab.GetComponent<SaveableId>().Value));
            var database = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/_Project/Features/World/ScrubberEncounter/Demo/ScrubberItemDatabase.asset");
            Assert.IsTrue(database.BuildRuntimeCatalog().TryGet(item.ItemId,out _));
            var production = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/_Project/Features/Items/Resources/Database.asset");
            Assert.IsFalse(production.BuildRuntimeCatalog().TryGet(item.ItemId,out _));
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Demos/AtmosphericScrubberEncounter.unity",OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects(); var pickups = roots.SelectMany(g=>g.GetComponentsInChildren<WorldPickup>(true)).ToArray();
                Assert.AreEqual(1,pickups.Length); var pickup=pickups[0]; Assert.AreSame(item,pickup.Definition.Item); Assert.AreEqual(1,pickup.Definition.Quantity);
                Assert.IsFalse(string.IsNullOrEmpty(pickup.PickupId));
                var ids=roots.SelectMany(g=>g.GetComponentsInChildren<SaveableId>(true)).Select(id=>id.Value).ToArray();
                Assert.AreEqual(ids.Length,ids.Distinct().Count());
                var binding=new SerializedObject(roots.SelectMany(g=>g.GetComponentsInChildren<SceneBindings>(true)).Single());
                var registered=binding.FindProperty("worldPickups"); Assert.AreEqual(1,registered.arraySize); Assert.AreSame(pickup,registered.GetArrayElementAtIndex(0).objectReferenceValue);
                Assert.That(pickup.GetPrompt(),Does.Contain("replacement fuse"));
            }
            finally { EditorSceneManager.CloseScene(scene,true); }
        }
    }
}
