#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SOLITUDE.Composition;
using SOLITUDE.Containers;
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
                selected=EventSystem.current.currentSelectedGameObject;
                Press(keyboard, Key.Escape, Key.E); Assert.IsFalse(screen.IsOpen); yield return null; Assert.IsFalse(screen.IsOpen);
                body.position = position + Vector2.down * 2; Physics2D.SyncTransforms(); yield return new WaitForFixedUpdate(); yield return null;
                body.position = position; Physics2D.SyncTransforms(); yield return new WaitForFixedUpdate(); yield return null; yield return null;
                Press(keyboard, Key.E); yield return null; Assert.IsTrue(screen.IsOpen);
                Assert.AreSame(selected, EventSystem.current.currentSelectedGameObject); Assert.That(screen.Telemetry, Does.Contain("BLOWN"));
                EventSystem.current.SetSelectedGameObject(screen.GetComponentsInChildren<UnityEngine.UI.Button>(true).Single(button=>button.name=="Close").gameObject);
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
        [UnityTest] public IEnumerator ActionNavigationUsesAmberWithoutActivatingOrRecoloringFault()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Scene,new LoadSceneParameters(LoadSceneMode.Single));yield return null;
            var screen=All<ScrubberScreenView>().Single();var equipment=All<ScrubberEquipment>().Single();
            var root=All<GameCompositionRoot>().Single();var session=(SOLITUDE.SaveLoad.ContainerSaveSession)typeof(GameCompositionRoot).GetField("session",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(root);
            session.Commands.Grant(All<PlayerInventory>().Single().Container.Handle,ScrubberEquipment.FuseItemId,1);Assert.IsTrue(screen.Open());
            var buttons=screen.GetComponentsInChildren<UnityEngine.UI.Button>(true);var close=buttons.Single(b=>b.name=="Close");var restart=buttons.Single(b=>b.name=="Restart");var install=buttons.Single(b=>b.name=="InstallFuse");
            var fault=screen.GetComponentsInChildren<TMPro.TMP_Text>(true).Single(label=>label.name=="Scrubber");var faultColor=fault.color;
            var oldBackground=InputSystem.settings.backgroundBehavior;var oldEditor=InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard=InputSystem.AddDevice<Keyboard>();
            try
            {
                yield return null;yield return null;Assert.AreEqual(new Color(.95f,.66f,.23f),close.image.color);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.UpArrow));yield return null;yield return null;
                Assert.AreSame(restart.gameObject,EventSystem.current.currentSelectedGameObject);Assert.AreEqual(new Color(.95f,.66f,.23f),restart.image.color);
                Assert.AreEqual(Color.black,restart.GetComponentInChildren<TMPro.TMP_Text>().color);Assert.AreEqual(new Color(.16f,.18f,.18f),close.image.color);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.UpArrow));yield return null;yield return null;
                Assert.AreSame(install.gameObject,EventSystem.current.currentSelectedGameObject);Assert.AreEqual(new Color(.95f,.66f,.23f),install.image.color);
                Assert.AreEqual(Color.black,install.GetComponentInChildren<TMPro.TMP_Text>().color);Assert.AreEqual(new Color(.16f,.18f,.18f),restart.image.color);
                Assert.IsFalse(equipment.State.FuseInstalled);Assert.IsFalse(equipment.State.Running);Assert.AreEqual(faultColor,fault.color);Assert.That(fault.text,Does.Contain("OFFLINE"));
            }
            finally { InputSystem.RemoveDevice(keyboard);InputSystem.settings.backgroundBehavior=oldBackground;InputSystem.settings.editorInputBehaviorInPlayMode=oldEditor; }
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
                Assert.IsTrue(feedback.Any(label=>label!=null && label.text=="Inventory full" && label.gameObject.activeInHierarchy),
                    string.Join("; ",feedback.Select(label=>label==null?"missing label":$"{label.text}: active={label.gameObject.activeInHierarchy}")));
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
        [UnityTest] public IEnumerator RepairScreenRechecksInventoryAndKeepsRepairedScrubberOffline()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Scene,new LoadSceneParameters(LoadSceneMode.Single));yield return null;
            var screen=All<ScrubberScreenView>().Single();var equipment=All<ScrubberEquipment>().Single();
            var root=All<GameCompositionRoot>().Single();var session=(SOLITUDE.SaveLoad.ContainerSaveSession)typeof(GameCompositionRoot).GetField("session",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(root);
            var inventory=All<PlayerInventory>().Single().Container;var buttons=screen.GetComponentsInChildren<UnityEngine.UI.Button>(true);
            var install=buttons.Single(b=>b.name=="InstallFuse");var restart=buttons.Single(b=>b.name=="Restart");
            Assert.IsTrue(screen.Open());Assert.IsFalse(install.interactable);Assert.IsTrue(restart.interactable);
            restart.onClick.Invoke();Assert.AreEqual("Restart failed: blown fuse. Replace fuse first.",screen.Feedback);Assert.IsFalse(equipment.State.FuseInstalled);
            session.Commands.Grant(inventory.Handle,ScrubberEquipment.FuseItemId,1);Assert.IsTrue(install.interactable);
            var fuse=inventory.GetSlots().Single(slot=>!slot.IsEmpty && slot.Stack.ItemId==ScrubberEquipment.FuseItemId);session.Commands.Remove(fuse.Address,1);
            install.onClick.Invoke();Assert.IsFalse(equipment.State.FuseInstalled);Assert.AreEqual("Requires 1 replacement fuse in inventory.",screen.Feedback);
            session.Commands.Grant(inventory.Handle,ScrubberEquipment.FuseItemId,2);Assert.IsTrue(install.interactable);
            bool observerSawRepair=false;inventory.SlotChanged+=_=>observerSawRepair=equipment.State.FuseInstalled;
            install.onClick.Invoke();Assert.IsTrue(observerSawRepair);Assert.AreEqual("Fuse replaced. Restart scrubber.",screen.Feedback);
            Assert.IsTrue(equipment.State.FuseInstalled);Assert.IsFalse(install.interactable);Assert.IsTrue(restart.interactable);
            Assert.AreEqual(1,inventory.GetSlots().Where(slot=>!slot.IsEmpty && slot.Stack.ItemId==ScrubberEquipment.FuseItemId).Sum(slot=>slot.Stack.Quantity));
            Assert.That(screen.Telemetry,Does.Contain("INTACT").And.Contain("OFFLINE").And.Contain("STOPPED").And.Contain("ELEVATED"));
            install.onClick.Invoke();Assert.AreEqual("Fuse already installed.",screen.Feedback);screen.Close();
            var hiddenFeedback=screen.Feedback;install.onClick.Invoke();restart.onClick.Invoke();Assert.AreEqual(hiddenFeedback,screen.Feedback);
            Assert.IsTrue(screen.Open());Assert.That(screen.Telemetry,Does.Contain("INTACT"));Assert.IsFalse(install.interactable);Assert.IsTrue(restart.interactable);
            Assert.AreEqual(1,inventory.GetSlots().Where(slot=>!slot.IsEmpty && slot.Stack.ItemId==ScrubberEquipment.FuseItemId).Sum(slot=>slot.Stack.Quantity));
            Assert.AreEqual("SCRUBBER-SAVE-ISOLATION-SENTINEL",File.ReadAllText(path));
        }
        [UnityTest] public IEnumerator RecoveryContinuesWithScreenOpenAndAwayButActualPauseSuspendsIt()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Scene,new LoadSceneParameters(LoadSceneMode.Single));yield return null;
            var screen=All<ScrubberScreenView>().Single();var equipment=All<ScrubberEquipment>().Single();var room=All<ScrubberRoomFeedback>().Single();var player=All<PlayerInteractor>().Single();
            var vitals=player.GetComponent<PlayerVitalsController>();float health=vitals.Vitals.Health.Current,oxygen=vitals.Vitals.Oxygen.Current;
            var root=All<GameCompositionRoot>().Single();var session=(SOLITUDE.SaveLoad.ContainerSaveSession)typeof(GameCompositionRoot).GetField("session",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(root);
            session.Commands.Grant(All<PlayerInventory>().Single().Container.Handle,ScrubberEquipment.FuseItemId,1);Assert.IsTrue(screen.Open());
            var buttons=screen.GetComponentsInChildren<UnityEngine.UI.Button>(true);buttons.Single(b=>b.name=="InstallFuse").onClick.Invoke();buttons.Single(b=>b.name=="Restart").onClick.Invoke();
            Assert.IsTrue(equipment.State.Running);Assert.That(screen.Feedback,Does.Contain("restarted"));Assert.IsTrue(room.RunningSound.isPlaying);Assert.IsTrue(room.IsWarningVisible);
            var status=room.GetComponentInChildren<TMPro.TextMeshPro>();Assert.That(status.text,Does.Contain("RUNNING"));Assert.IsTrue(status.gameObject.activeInHierarchy);
            Assert.That(screen.Telemetry,Does.Contain("RUNNING").And.Contain("ACTIVE").And.Contain("RECOVERING"));Assert.IsFalse(buttons.Single(b=>b.name=="Restart").interactable);
            yield return new WaitForSeconds(1);Assert.Greater(equipment.State.RecoverySeconds,.8);Assert.IsFalse(equipment.State.Operational);
            var elapsed=equipment.State.RecoverySeconds;equipment.Restart();Assert.AreEqual(elapsed,equipment.State.RecoverySeconds);
            var pause=(SOLITUDE.Application.PauseCoordinator)typeof(GameCompositionRoot).GetField("pause",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(root);
            using(pause.Acquire()) { yield return new WaitForSecondsRealtime(.2f);Assert.AreEqual(elapsed,equipment.State.RecoverySeconds); }
            screen.Close();player.GetComponent<Rigidbody2D>().position=new Vector2(20,2);Physics2D.SyncTransforms();
            yield return new WaitForSeconds(4.2f);Assert.IsTrue(equipment.State.Operational);Assert.IsFalse(room.IsWarningVisible);Assert.IsFalse(room.Alert.isPlaying);Assert.IsTrue(room.RunningSound.isPlaying);
            Assert.That(status.text,Does.Contain("RUNNING"));Assert.IsTrue(status.gameObject.activeInHierarchy);Assert.AreEqual(equipment.GetComponent<SpriteRenderer>().color,status.color);
            Assert.IsTrue(screen.Open());Assert.That(screen.Telemetry,Does.Contain("RUNNING").And.Contain("NORMAL"));
            var labels=screen.GetComponentsInChildren<TMPro.TMP_Text>(true);Assert.That(labels.Single(label=>label.name=="Intake").text,Does.Contain("ACTIVE"));Assert.That(labels.Single(label=>label.name=="Scrubber").text,Does.Contain("RUNNING"));
            Assert.AreEqual(health,vitals.Vitals.Health.Current);Assert.AreEqual(oxygen,vitals.Vitals.Oxygen.Current);Assert.AreEqual("SCRUBBER-SAVE-ISOLATION-SENTINEL",File.ReadAllText(path));
        }
        [UnityTest] public IEnumerator CompletionNotificationDefersExpiresAndNeverReplays()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Scene,new LoadSceneParameters(LoadSceneMode.Single));yield return null;
            var equipment=All<ScrubberEquipment>().Single();var screen=All<ScrubberScreenView>().Single();var notification=All<SOLITUDE.Tasks.UI.TaskNoticeView>().Single();var room=All<ScrubberRoomFeedback>().Single();
            var root=All<GameCompositionRoot>().Single();var session=(SOLITUDE.SaveLoad.ContainerSaveSession)typeof(GameCompositionRoot).GetField("session",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(root);
            session.Commands.Grant(All<PlayerInventory>().Single().Container.Handle,ScrubberEquipment.FuseItemId,1);
            // Journal notices require accepted discovery; access discovers while its open screen defers the notice.
            Assert.IsTrue(All<ScrubberAccess>().Single().Interact(All<PlayerInteractor>().Single()).IsSuccess);
            Assert.IsTrue(screen.IsOpen);
            Assert.IsTrue(root.Tasks.ReadSnapshot().Find(All<ScrubberTaskSource>().Single().Key).State.Discovered);
            equipment.Install(); equipment.Restart();
            var selected=EventSystem.current.currentSelectedGameObject;equipment.State.Advance(5);yield return null;
            Assert.IsTrue(equipment.State.Operational);Assert.IsFalse(room.IsWarningVisible);Assert.IsFalse(room.Alert.isPlaying);Assert.IsFalse(notification.IsVisible);Assert.AreEqual(0,notification.PresentationCount);Assert.AreSame(selected,EventSystem.current.currentSelectedGameObject);
            Assert.That(screen.GetComponentsInChildren<TMPro.TMP_Text>(true).Single(label=>label.name=="Objective").text,Does.Contain("complete"));
            screen.Close();yield return null;yield return null;Assert.IsTrue(notification.IsVisible);Assert.AreEqual(1,notification.PresentationCount);Assert.IsNull(EventSystem.current.currentSelectedGameObject);
            yield return new WaitForSecondsRealtime(4.1f);Assert.IsFalse(notification.IsVisible);Assert.AreEqual(1,notification.PresentationCount);
            Assert.IsTrue(screen.Open());Assert.That(screen.Telemetry,Does.Contain("RUNNING").And.Contain("NORMAL"));Assert.That(screen.GetComponentsInChildren<TMPro.TMP_Text>(true).Single(label=>label.name=="Objective").text,Does.Contain("complete"));
            notification.enabled=false;notification.enabled=true;screen.Close();room.enabled=false;room.enabled=true;yield return null;
            Assert.IsFalse(notification.IsVisible);Assert.AreEqual(1,notification.PresentationCount);Assert.IsFalse(room.IsWarningVisible);Assert.IsFalse(room.Alert.isPlaying);
        }
        [UnityTest] public IEnumerator FaultedRoomCanBeInspectedWithoutChangingVitalsOrSave()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Scene,new LoadSceneParameters(LoadSceneMode.Single));yield return null;
            var root=All<GameCompositionRoot>().Single();Assert.AreEqual(RuntimeStartupState.Ready,root.State);Assert.IsTrue(root.UsesDisposableSession);
            var equipment=All<ScrubberEquipment>().Single();var access=All<ScrubberAccess>().Single();var screen=All<ScrubberScreenView>().Single();
            var room=All<ScrubberRoomFeedback>().Single();Assert.IsTrue(room.IsWarningVisible);Assert.IsNotNull(room.Alert.clip);Assert.IsTrue(room.Alert.isPlaying);
            var status=room.GetComponentInChildren<TMPro.TextMeshPro>();Assert.That(status.text,Does.Contain("OFFLINE"));Assert.IsTrue(status.gameObject.activeInHierarchy);Assert.AreEqual(equipment.GetComponent<SpriteRenderer>().color,status.color);
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
