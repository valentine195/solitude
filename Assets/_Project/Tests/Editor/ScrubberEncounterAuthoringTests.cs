using System.Linq;
using NUnit.Framework;
using SOLITUDE.Items;
using SOLITUDE.Composition;
using SOLITUDE.SaveLoad;
using SOLITUDE.World.ScrubberEncounter;
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
            Assert.AreEqual(ScrubberEquipment.FuseItemId,item.ItemId);
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
                var room=roots.SelectMany(g=>g.GetComponentsInChildren<ScrubberRoomFeedback>(true)).Single();Assert.IsNotNull(room.Alert.clip);Assert.IsNotNull(room.RunningSound.clip);Assert.IsTrue(room.RunningSound.loop);
                var warning=roots.SelectMany(g=>g.GetComponentsInChildren<TMPro.TextMeshPro>(true)).Single(label=>label.name=="RoomAtmosphereWarning");
                Assert.Greater(warning.renderer.sortingOrder,roots.SelectMany(g=>g.GetComponentsInChildren<UnityEngine.Tilemaps.TilemapRenderer>(true)).Max(r=>r.sortingOrder),"Room warning must render above wall tiles.");
                var status=room.GetComponentInChildren<TMPro.TextMeshPro>(true);Assert.IsNotNull(status);Assert.That(status.text,Does.Contain("OFFLINE"));Assert.Greater(status.renderer.sortingOrder,2000);
                Assert.AreSame(status,new SerializedObject(room).FindProperty("equipmentStatus").objectReferenceValue);
                var screen=roots.SelectMany(g=>g.GetComponentsInChildren<ScrubberScreenView>(true)).Single();var notice=roots.SelectMany(g=>g.GetComponentsInChildren<ScrubberTaskNotificationView>(true)).Single();Assert.AreSame(screen.gameObject,notice.gameObject);
                var rect=screen.transform.Find("TaskCompletion").GetComponent<RectTransform>();Assert.AreEqual(Vector2.one,rect.anchorMax);Assert.IsFalse(rect.gameObject.GetComponent<UnityEngine.UI.Image>().raycastTarget);
                Assert.AreEqual(0,screen.GetComponentsInChildren<UnityEngine.UI.Button>(true).Count(button=>button.transform.IsChildOf(rect)));
                Assert.IsTrue(screen.GetComponentsInChildren<UnityEngine.UI.Button>(true).All(button=>button.transition==UnityEngine.UI.Selectable.Transition.None),"Default tint must not mask focus styling.");
            }
            finally { EditorSceneManager.CloseScene(scene,true); }
        }
    }
}
