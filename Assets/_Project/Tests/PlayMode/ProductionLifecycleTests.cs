using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SOLITUDE.Application;
using SOLITUDE.Composition;
using SOLITUDE.Containers;
using SOLITUDE.Features.Interactables;
using SOLITUDE.Items;
using SOLITUDE.SaveLoad;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace SOLITUDE.Tests
{
    public class ProductionLifecycleTests
    {
        private static T Field<T>(object owner,string name)=>(T)owner.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(owner);
        private static GameCompositionRoot Root=>UnityEngine.Object.FindFirstObjectByType<GameCompositionRoot>();
        private static SceneBindings PlayerScope=>UnityEngine.Object.FindObjectsByType<SceneBindings>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(s=>s.HasPlayer);
        [UnitySetUp] public IEnumerator Load()
        {
            // Tests are run in an isolated project/save directory by the repository gate.
            Assert.IsTrue(Environment.GetCommandLineArgs().Contains("-solitudeSaveDirectory"),"Production tests require the isolated verification runner.");
            if(Root!=null){UnityEngine.Object.Destroy(Root.gameObject);yield return null;}
            RuntimeStartupState? stateBeforeBinding=null;
            void Observe(Scene scene,LoadSceneMode mode)
            {if(scene.path=="Assets/Scenes/Wakeup.unity")stateBeforeBinding=Root.State;}
            SceneManager.sceneLoaded+=Observe;
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/Wakeup.unity",LoadSceneMode.Single);
            SceneManager.sceneLoaded-=Observe;
            Assert.AreEqual(RuntimeStartupState.Unready,stateBeforeBinding,"Input must wait for the initial scene binding callback.");
            yield return null;Assert.AreEqual(RuntimeStartupState.Ready,Root.State);
            Root.StartNewSave();yield return null;yield return null;
        }
        [UnityTearDown] public IEnumerator Release()
        {if(Root!=null)UnityEngine.Object.Destroy(Root.gameObject);yield return null;Time.timeScale=1;}
        [UnityTest] public IEnumerator StartupAndInactiveBindingsStaySingleAcrossEnableCycles()
        {
            var scope=PlayerScope;var input=Field<SOLITUDE.Core.Input.UnityInputHost>(Root,"input");var modal=Field<ContainerModalCoordinator>(scope,"modal");
            for(int i=0;i<3;i++){input.enabled=false;input.enabled=true;scope.enabled=false;scope.enabled=true;}
            Assert.AreEqual(1,UnityEngine.Object.FindObjectsByType<GameCompositionRoot>(FindObjectsSortMode.None).Length);
            Assert.AreEqual(OpenResult.Opened,modal.ToggleInventory());Assert.AreEqual(0,Time.timeScale);modal.Close();Assert.AreEqual(1,Time.timeScale);
            var inactive=UnityEngine.Object.FindObjectsByType<WorldPickup>(FindObjectsInactive.Include,FindObjectsSortMode.None).First();
            inactive.gameObject.SetActive(false);Assert.IsTrue(Field<PickupCollectionService>(Root,"pickups").IsValid(inactive.PickupId));yield return null;
        }
        [UnityTest] public IEnumerator AdditiveTargetUnloadClosesOnlyItsModalAndInvalidatesHandle()
        {
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Tests/Fixtures/Context.unity",LoadSceneMode.Additive);
            var context=SceneManager.GetSceneByPath("Assets/_Project/Tests/Fixtures/Context.unity");
            var locker=context.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<LockerContainer>(true)).Single();
            Assert.IsTrue(Root.Open(locker));var handle=locker.Container.Handle;var modal=Field<ContainerModalCoordinator>(PlayerScope,"modal");
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Tests/Fixtures/Unrelated.unity",LoadSceneMode.Additive);
            yield return SceneManager.UnloadSceneAsync(SceneManager.GetSceneByPath("Assets/_Project/Tests/Fixtures/Unrelated.unity"));
            Assert.AreEqual(ContainerModalKind.Transfer,modal.Kind);Assert.AreEqual(0,Time.timeScale);
            yield return SceneManager.UnloadSceneAsync(context);
            Assert.AreEqual(ContainerModalKind.Closed,modal.Kind);Assert.AreEqual(1,Time.timeScale);Assert.IsNull(Field<ContainerSaveSession>(Root,"session").Commands.Read(handle));
        }
        [UnityTest] public IEnumerator CollectionSurvivesActualPlayerSceneReloadAndNewSaveClearsIt()
        {
            var pickup=UnityEngine.Object.FindObjectsByType<WorldPickup>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(p=>p.gameObject.activeSelf);string id=pickup.PickupId;
            Assert.IsTrue(pickup.Interact(null).IsSuccess);Field<SaveGameService>(Root,"persistence").Flush();
            var transition=Root.TransitionTo("Assets/Scenes/Wakeup.unity");Assert.IsFalse(Field<InputPolicyCoordinator>(Root,"policy").Ready);
            yield return transition;yield return null;
            var restored=UnityEngine.Object.FindObjectsByType<WorldPickup>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(p=>p.PickupId==id);
            Assert.IsFalse(restored.gameObject.activeSelf);Assert.IsTrue(Field<ContainerSaveSession>(Root,"session").IsPickupCollected(id));
            var modal=Field<ContainerModalCoordinator>(PlayerScope,"modal");modal.ToggleInventory();Root.StartNewSave();yield return null;yield return null;
            Assert.AreEqual(1,Time.timeScale);Assert.AreEqual(RuntimeStartupState.Ready,Root.State);
            restored=UnityEngine.Object.FindObjectsByType<WorldPickup>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(p=>p.PickupId==id);Assert.IsTrue(restored.gameObject.activeSelf);Assert.IsFalse(Field<ContainerSaveSession>(Root,"session").IsPickupCollected(id));
        }
        [UnityTest] public IEnumerator DuplicateBootstrapLeavesExistingGraphAndUnrelatedObjectsAlive()
        {
            var existing=Root;var unrelated=new GameObject("UnrelatedFeature");var duplicate=new GameObject("DuplicateBootstrap").AddComponent<GameCompositionRoot>();
            yield return null;Assert.IsTrue(duplicate==null);Assert.AreSame(existing,Root);Assert.IsNotNull(unrelated);Assert.AreEqual(RuntimeStartupState.Ready,Root.State);UnityEngine.Object.Destroy(unrelated);
        }
        [UnityTest] public IEnumerator DestroyedViewsBeforeScopeDoNotBreakPauseOrInputCleanup()
        {
            var scope=PlayerScope;Field<ContainerModalCoordinator>(scope,"modal").ToggleInventory();
            var view=Field<ContainerModalView>(scope,"inventoryModal");UnityEngine.Object.Destroy(view.gameObject);yield return null;
            scope.Release();Assert.AreEqual(1,Time.timeScale);Assert.DoesNotThrow(scope.Release);
        }
        [UnityTest] public IEnumerator EventSystemAndInputDestroyedBeforeScopeReleaseSafely()
        {
            var scope=PlayerScope;Field<ContainerModalCoordinator>(scope,"modal").ToggleInventory();
            UnityEngine.Object.Destroy(UnityEngine.EventSystems.EventSystem.current.gameObject);
            UnityEngine.Object.Destroy(Field<SOLITUDE.Core.Input.UnityInputHost>(Root,"input"));yield return null;
            Assert.DoesNotThrow(scope.Release);Assert.AreEqual(1,Time.timeScale);Assert.DoesNotThrow(scope.Release);
        }
    }
}
