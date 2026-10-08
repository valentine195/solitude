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
        [Test] public void JournalPrefabAndBuilderKeepAuthoredControlsAndSourceBindings()
        {
            var path = SOLITUDE.Editor.ScrubberEncounterBuilder.ScenePath;
            var guid = AssetDatabase.AssetPathToGUID(path);
            SOLITUDE.Editor.ScrubberEncounterBuilder.AddTaskJournal();
            SOLITUDE.Editor.ScrubberEncounterBuilder.AddTaskJournal();
            Assert.AreEqual(guid,AssetDatabase.AssetPathToGUID(path));
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SOLITUDE.Editor.ScrubberEncounterBuilder.JournalFolder+"/TaskJournal.prefab");
            prefab.GetComponent<SOLITUDE.Tasks.UI.TaskJournalView>().ValidateAuthoring();
            Assert.AreEqual(2,prefab.GetComponentsInChildren<UnityEngine.UI.ScrollRect>(true).Length);
            Assert.IsTrue(prefab.GetComponentsInChildren<TMPro.TMP_Text>(true).All(label=>!label.richText));
            var scene = EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                var view = roots.SelectMany(g=>g.GetComponentsInChildren<SOLITUDE.Tasks.UI.TaskJournalView>(true)).Single(); view.ValidateAuthoring();
                var source = roots.SelectMany(g=>g.GetComponentsInChildren<ScrubberTaskSource>(true)).Single();
                Assert.AreEqual("scrubber-room-01",source.SourceId); Assert.IsNotNull(source.Equipment); Assert.IsNotNull(source.Access);
                var data = new SerializedObject(roots.SelectMany(g=>g.GetComponentsInChildren<SceneBindings>(true)).Single());
                Assert.AreSame(view,data.FindProperty("journalView").objectReferenceValue);
                Assert.AreEqual(1,data.FindProperty("taskSources").arraySize); Assert.AreSame(source,data.FindProperty("taskSources").GetArrayElementAtIndex(0).objectReferenceValue);
                Assert.AreEqual(1,roots.SelectMany(g=>g.GetComponentsInChildren<UnityEngine.EventSystems.EventSystem>(true)).Count());
            }
            finally { EditorSceneManager.CloseScene(scene,true); }
        }
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
