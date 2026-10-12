using System;
using System.Reflection;
using System.Linq;
using SOLITUDE.Editor;
using UnityEngine.SceneManagement;
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
            var noticePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SOLITUDE.Editor.ScrubberEncounterBuilder.JournalFolder+"/TaskNotice.prefab");
            noticePrefab.GetComponent<SOLITUDE.Tasks.UI.TaskNoticeView>().ValidateAuthoring();
            Assert.IsEmpty(noticePrefab.GetComponentsInChildren<UnityEngine.UI.Selectable>(true));
            Assert.IsNull(noticePrefab.GetComponent<UnityEngine.UI.GraphicRaycaster>());
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
                var notice = roots.SelectMany(g=>g.GetComponentsInChildren<SOLITUDE.Tasks.UI.TaskNoticeView>(true)).Single();
                notice.ValidateAuthoring(); Assert.AreSame(notice,data.FindProperty("taskNoticeView").objectReferenceValue);
                Assert.IsFalse(roots.SelectMany(g=>g.GetComponentsInChildren<ScrubberTaskNotificationView>(true)).Single().enabled);
                var feature = roots.SelectMany(g => g.GetComponentsInChildren<ScrubberComposition>(true)).Single();
                feature.Validate();
                Assert.AreEqual(2, data.FindProperty("features").arraySize);
                var succession = roots.SelectMany(g => g.GetComponentsInChildren<SOLITUDE.Player.Succession.SurvivorSuccessionComposition>(true)).Single();
                succession.Validate(); Assert.AreSame(succession, data.FindProperty("features").GetArrayElementAtIndex(1).objectReferenceValue);
                Assert.AreSame(feature, data.FindProperty("features").GetArrayElementAtIndex(0).objectReferenceValue);
                var featureData = new SerializedObject(feature);
                Assert.AreEqual(1, featureData.FindProperty("taskSources").arraySize);
                Assert.AreSame(source, featureData.FindProperty("taskSources").GetArrayElementAtIndex(0).objectReferenceValue);
                Assert.IsNull(data.FindProperty("taskSources"), "Task source ownership moved to the feature.");
                Assert.AreEqual(1,roots.SelectMany(g=>g.GetComponentsInChildren<UnityEngine.EventSystems.EventSystem>(true)).Count());
            }
            finally { EditorSceneManager.CloseScene(scene,true); }
        }
        [TestCase("missing-feature")]
        [TestCase("null-feature")]
        [TestCase("duplicate-feature")]
        [TestCase("wrong-scene-feature")]
        [TestCase("duplicate-composition")]
        [TestCase("missing-equipment")]
        [TestCase("duplicate-equipment")]
        [TestCase("wrong-scene-equipment")]
        [TestCase("missing-screen")]
        [TestCase("missing-source")]
        [TestCase("incomplete-screen")]
        [TestCase("wrong-source-access")]
        public void ValidationRejectsIncompleteOrAmbiguousFeatureAuthoring(string defect)
        {
            var scene = EditorSceneManager.OpenScene(ScrubberEncounterBuilder.ScenePath, OpenSceneMode.Additive);
            var other = EditorSceneManager.NewPreviewScene();
            try
            {
                var roots = scene.GetRootGameObjects();
                var scope = roots.SelectMany(g => g.GetComponentsInChildren<SceneBindings>(true)).Single();
                var feature = roots.SelectMany(g => g.GetComponentsInChildren<ScrubberComposition>(true)).Single();
                var equipment = roots.SelectMany(g => g.GetComponentsInChildren<ScrubberEquipment>(true)).Single();
                var screen = roots.SelectMany(g => g.GetComponentsInChildren<ScrubberScreenView>(true)).Single();
                var source = roots.SelectMany(g => g.GetComponentsInChildren<ScrubberTaskSource>(true)).Single();
                scope.Validate();
                var data = new SerializedObject(scope); var list = data.FindProperty("features");
                if (defect == "missing-feature") { UnityEngine.Object.DestroyImmediate(feature); list.arraySize = 0; }
                if (defect == "null-feature") list.GetArrayElementAtIndex(0).objectReferenceValue = null;
                if (defect == "duplicate-feature") { list.arraySize = 2; list.GetArrayElementAtIndex(1).objectReferenceValue = feature; }
                if (defect == "wrong-scene-feature")
                {
                    var foreign = new GameObject("Foreign feature"); SceneManager.MoveGameObjectToScene(foreign, other);
                    list.GetArrayElementAtIndex(0).objectReferenceValue = foreign.AddComponent<ScrubberComposition>();
                }
                data.ApplyModifiedPropertiesWithoutUndo();
                if (defect == "duplicate-composition") feature.gameObject.AddComponent<ScrubberComposition>();
                if (defect == "missing-equipment") feature.Configure(new ScrubberEquipment[0], new[] { screen }, new[] { source });
                if (defect == "duplicate-equipment") feature.Configure(new[] { equipment, equipment }, new[] { screen }, new[] { source });
                if (defect == "wrong-scene-equipment")
                {
                    var foreign = new GameObject("Foreign equipment"); SceneManager.MoveGameObjectToScene(foreign, other);
                    feature.Configure(new[] { foreign.AddComponent<ScrubberEquipment>() }, new[] { screen }, new[] { source });
                }
                if (defect == "missing-screen") feature.Configure(new[] { equipment }, new ScrubberScreenView[0], new[] { source });
                if (defect == "missing-source") feature.Configure(new[] { equipment }, new[] { screen }, new ScrubberTaskSource[0]);
                if (defect == "incomplete-screen")
                {
                    var view = new SerializedObject(screen); view.FindProperty("close").objectReferenceValue = null; view.ApplyModifiedPropertiesWithoutUndo();
                }
                if (defect == "wrong-source-access") source.Configure(source.SourceId, "room", equipment, null);
                var validate = typeof(CompositionValidator).GetMethod("ValidateScene", BindingFlags.Static | BindingFlags.NonPublic);
                var error = Assert.Throws<TargetInvocationException>(() => validate.Invoke(null, new object[] { scene }));
                Assert.That(error.InnerException, Is.TypeOf<UnityEditor.Build.BuildFailedException>());
                Assert.That(error.InnerException.Message.ToLowerInvariant(), Does.Contain("scrubber").Or.Contain("feature"));
            }
            finally { EditorSceneManager.CloseScene(scene, true); EditorSceneManager.ClosePreviewScene(other); }
        }
        [Test] public void MigrationIsIdempotentAndKeepsExistingSceneEndpointIdentities()
        {
            ScrubberEncounterBuilder.MigrateComposition();
            var scene = EditorSceneManager.OpenScene(ScrubberEncounterBuilder.ScenePath, OpenSceneMode.Additive);
            try
            {
                var scope = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<SceneBindings>(true)).Single();
                var feature = scope.GetComponent<ScrubberComposition>();
                var journal = new SerializedObject(scope).FindProperty("journalView").objectReferenceValue;
                Assert.IsFalse(scene.isDirty);
                ScrubberEncounterBuilder.MigrateComposition();
                Assert.AreSame(scope, scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<SceneBindings>(true)).Single());
                Assert.AreSame(feature, scope.GetComponent<ScrubberComposition>());
                Assert.AreSame(journal, new SerializedObject(scope).FindProperty("journalView").objectReferenceValue);
                scope.Validate();
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
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
                Assert.IsTrue(pickup.gameObject.activeInHierarchy, "The demo fuse must be available on direct entry and reset.");
                var ui = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<SOLITUDE.Core.UI.UIController>(true)).Single();
                Assert.IsTrue(ui.isActiveAndEnabled, "The demo interaction UI must be active on direct entry and reset.");
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
