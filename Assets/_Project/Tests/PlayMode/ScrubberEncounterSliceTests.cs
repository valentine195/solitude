#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SOLITUDE.Composition;
using SOLITUDE.Core.Interaction;
using SOLITUDE.Player;
using SOLITUDE.World.ScrubberEncounter;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;

namespace SOLITUDE.Tests
{
    public class ScrubberEncounterSliceTests
    {
        private const string Scene = "Assets/_Project/Scenes/Demos/AtmosphericScrubberEncounter.unity";
        private string path;
        private byte[] original;
        private static T[] All<T>() where T : Object => Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        [UnitySetUp] public IEnumerator Prepare()
        {
            var args=Environment.GetCommandLineArgs(); int index=Array.IndexOf(args,"-solitudeSaveDirectory");
            Assert.GreaterOrEqual(index,0,"Run with isolated save directory.");
            foreach(var root in All<GameCompositionRoot>()) Object.Destroy(root.gameObject);
            yield return null;
            path=Path.Combine(args[index+1],"solitude-save.json");original=File.Exists(path)?File.ReadAllBytes(path):null;
            File.WriteAllText(path,"SCRUBBER-SAVE-ISOLATION-SENTINEL");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            foreach(var root in All<GameCompositionRoot>()) Object.Destroy(root.gameObject);
            yield return null;Time.timeScale=1;
            if(path==null)yield break;
            if(original==null){if(File.Exists(path))File.Delete(path);}else File.WriteAllBytes(path,original);
        }
        private static void Press(Keyboard keyboard, params Key[] keys)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); InputSystem.Update();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
        }
        [UnityTest] public IEnumerator ComposedScreenOwnsInputAndReopensAfterLeavingProximity()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Scene,new LoadSceneParameters(LoadSceneMode.Single)); yield return null;
            var screen = All<ScrubberScreenView>().Single(); var access = All<ScrubberAccess>().Single();
            var player = All<PlayerInteractor>().Single(); var body = player.GetComponent<Rigidbody2D>();
            var previousBackground = InputSystem.settings.backgroundBehavior;
            var previousEditor = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                body.position = (Vector2)access.transform.position + Vector2.down * .6f; Physics2D.SyncTransforms();
                yield return new WaitForFixedUpdate(); yield return null; yield return null;
                Press(keyboard, Key.E); yield return null; Assert.IsTrue(screen.IsOpen);
                Assert.IsNotNull(EventSystem.current.currentSelectedGameObject); var selected = EventSystem.current.currentSelectedGameObject;
                Assert.AreEqual(1, Time.timeScale);
                var position = body.position;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.Tab, Key.E)); InputSystem.Update();
                yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
                Assert.Less(Vector2.Distance(position, body.position), .01f); Assert.IsTrue(screen.IsOpen);
                var inventoryView = (SOLITUDE.Containers.ContainerModalView)typeof(SceneBindings).GetField("inventoryModal", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(All<SceneBindings>().Single());
                Assert.IsFalse(inventoryView.IsOpen);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
                Press(keyboard, Key.Escape, Key.E); Assert.IsFalse(screen.IsOpen); yield return null; Assert.IsFalse(screen.IsOpen);
                body.position = position + Vector2.down * 2; Physics2D.SyncTransforms(); yield return new WaitForFixedUpdate(); yield return null;
                body.position = position; Physics2D.SyncTransforms(); yield return new WaitForFixedUpdate(); yield return null; yield return null;
                Press(keyboard, Key.E); yield return null; Assert.IsTrue(screen.IsOpen);
                Assert.AreSame(selected, EventSystem.current.currentSelectedGameObject); Assert.That(screen.Telemetry, Does.Contain("BLOWN"));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter)); yield return null; yield return null;
                Assert.IsFalse(screen.IsOpen, "The EventSystem submit action activates Close.");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update(); yield return null;
                Assert.IsTrue(screen.Open()); All<SceneBindings>().Single().CloseForTransition(); Assert.IsFalse(screen.IsOpen);
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard); InputSystem.settings.backgroundBehavior = previousBackground;
                InputSystem.settings.editorInputBehaviorInPlayMode = previousEditor;
            }
        }
        [UnityTest] public IEnumerator FusePickupRejectsFullInventoryThenCollectsExactlyOnceThroughInput()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Scene,new LoadSceneParameters(LoadSceneMode.Single)); yield return null;
            var pickup = All<SOLITUDE.Items.WorldPickup>().Single(); var player=All<PlayerInteractor>().Single();
            var body=player.GetComponent<Rigidbody2D>();
            var from=body.position; var to=(Vector2)pickup.transform.position;
            Assert.IsFalse(Physics2D.RaycastAll(from,(to-from).normalized,Vector2.Distance(from,to)).Any(hit=>!hit.collider.isTrigger && hit.rigidbody != body),"Fuse has an unobstructed same-room path from spawn.");
            var root=All<GameCompositionRoot>().Single();
            var session=(SOLITUDE.SaveLoad.ContainerSaveSession)typeof(GameCompositionRoot).GetField("session",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(root);
            var inventory=All<PlayerInventory>().Single().Container;
            var database=UnityEditor.AssetDatabase.LoadAssetAtPath<SOLITUDE.Items.ItemDatabase>("Assets/_Project/Features/World/ScrubberEncounter/Demo/ScrubberItemDatabase.asset");
            var filler=database.BuildRuntimeCatalog().Items.First(item=>item.ItemId!=pickup.Definition.Item.ItemId);
            session.Commands.Grant(inventory.Handle,filler.ItemId,inventory.Capacity*filler.MaxStackSize);
            Assert.IsTrue(Enumerable.Range(0,inventory.Capacity).All(i=>!inventory.GetSlot(i).IsEmpty));
            var previousBackground=InputSystem.settings.backgroundBehavior;var previousEditor=InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard=InputSystem.AddDevice<Keyboard>();
            try
            {
                body.position=to+Vector2.down*.3f;Physics2D.SyncTransforms();yield return new WaitForFixedUpdate();yield return null;yield return null;
                Assert.AreSame(pickup,typeof(PlayerInteractor).GetField("currentTarget",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(player));
                Press(keyboard,Key.E);yield return null;Assert.IsTrue(pickup.gameObject.activeSelf);
                var feedback=All<SOLITUDE.Core.UI.InteractionFeedbackView>().Select(view=>(TMPro.TMP_Text)typeof(SOLITUDE.Core.UI.InteractionFeedbackView).GetField("label",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(view));
                Assert.IsTrue(feedback.Any(label=>label!=null && label.text=="Inventory full" && label.gameObject.activeInHierarchy));
                session.Commands.Remove(inventory.GetSlot(0).Address,inventory.GetSlot(0).Stack.Quantity);
                Press(keyboard,Key.E);yield return null;Assert.IsFalse(pickup.gameObject.activeSelf);
                Assert.AreEqual(1,Enumerable.Range(0,inventory.Capacity).Select(i=>inventory.GetSlot(i)).Where(slot=>!slot.IsEmpty && slot.Stack.ItemId==pickup.Definition.Item.ItemId).Sum(slot=>slot.Stack.Quantity));
                pickup.Interact(player);Press(keyboard,Key.E);yield return null;
                Assert.AreEqual(1,Enumerable.Range(0,inventory.Capacity).Select(i=>inventory.GetSlot(i)).Where(slot=>!slot.IsEmpty && slot.Stack.ItemId==pickup.Definition.Item.ItemId).Sum(slot=>slot.Stack.Quantity));
                Assert.AreEqual("SCRUBBER-SAVE-ISOLATION-SENTINEL",File.ReadAllText(path));
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);InputSystem.settings.backgroundBehavior=previousBackground;InputSystem.settings.editorInputBehaviorInPlayMode=previousEditor;
            }
        }
        [UnityTest] public IEnumerator FaultedRoomCanBeInspectedWithoutChangingVitalsOrSave()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Scene,new LoadSceneParameters(LoadSceneMode.Single));yield return null;
            var root=All<GameCompositionRoot>().Single();Assert.AreEqual(RuntimeStartupState.Ready,root.State);Assert.IsTrue(root.UsesDisposableSession);
            var equipment=All<ScrubberEquipment>().Single();var access=All<ScrubberAccess>().Single();var screen=All<ScrubberScreenView>().Single();
            var room=All<ScrubberRoomFeedback>().Single();Assert.IsTrue(room.IsWarningVisible);Assert.IsNotNull(room.Alert.clip);Assert.IsTrue(room.Alert.isPlaying);
            var player=All<PlayerInteractor>().Single();var vitals=player.GetComponent<PlayerVitalsController>();
            var health=vitals.Vitals.Health.Current;var oxygen=vitals.Vitals.Oxygen.Current;
            player.GetComponent<Rigidbody2D>().position=(Vector2)access.transform.position+Vector2.down*.6f;Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();yield return null;yield return null;
            var field=typeof(PlayerInteractor).GetField("currentTarget",BindingFlags.Instance|BindingFlags.NonPublic);
            Assert.AreSame(access,field.GetValue(player));player.TryInteract();yield return null;
            Assert.IsTrue(screen.IsOpen);Assert.That(screen.Telemetry,Does.Contain("OFFLINE").And.Contain("BLOWN").And.Contain("STOPPED").And.Contain("ELEVATED"));
            screen.Close();Assert.IsFalse(screen.IsOpen);screen.Open();Assert.IsTrue(screen.IsOpen);
            Assert.AreEqual("Blown fuse",equipment.State.Fault);Assert.AreEqual(health,vitals.Vitals.Health.Current);Assert.AreEqual(oxygen,vitals.Vitals.Oxygen.Current);
            Assert.AreEqual("SCRUBBER-SAVE-ISOLATION-SENTINEL",File.ReadAllText(path));
        }
    }
}
#endif
