#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using SOLITUDE.Application.Survivors;
using SOLITUDE.Composition;
using SOLITUDE.Player;
using SOLITUDE.Player.Succession;
using SOLITUDE.SaveLoad;
using SOLITUDE.World.Restoration;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
namespace SOLITUDE.Tests
{
    public class MechanicalRepairRecoveryTests
    {
        private static T One<T>() where T : Object => WeaponSystemTestFixtures.One<T>();
        private static T Field<T>(object o, string n) => WeaponSystemTestFixtures.Field<T>(o,n);
        [UnityTest] public IEnumerator SceneReturnSuccessorAndNewRunUseCanonicalRepairAndOnceOnlyLoadout()
        {
            var args=Environment.GetCommandLineArgs(); int i=Array.IndexOf(args,"-solitudeSaveDirectory");
            Assert.GreaterOrEqual(i,0); StringAssert.Contains("step4-mechanical-",args[i+1]);
            if (Object.FindFirstObjectByType<GameCompositionRoot>() != null) Object.Destroy(Object.FindFirstObjectByType<GameCompositionRoot>().gameObject);
            yield return null;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(MechanicalRepairFixtureTests.ScenePath,new LoadSceneParameters(LoadSceneMode.Single)); yield return null;
            var root=WeaponSystemTestFixtures.Root; var session=WeaponSystemTestFixtures.Session;
            Assert.AreEqual(RuntimeStartupState.Ready,root.State);
            var junction=Object.FindObjectsByType<RestorationJunction>(FindObjectsSortMode.None).Single(j=>j.name=="Auxiliary power socket");
            var part=Field<MechanicalRepairDefinition>(One<MechanicalRepairComposition>(),"definition").Part.ItemId;
            Assert.IsTrue(session.Commands.Grant(One<PlayerInventory>().Container.Handle,part,2).Succeeded);
            Assert.IsTrue(junction.Interact(One<PlayerInteractor>()).IsSuccess);
            Assert.IsTrue(One<SaveGameService>().TryFlush());
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/_Project/Scenes/Wakeup.unity",new LoadSceneParameters(LoadSceneMode.Single)); yield return null;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(MechanicalRepairFixtureTests.ScenePath,new LoadSceneParameters(LoadSceneMode.Single)); yield return null;
            Assert.IsTrue(One<RestorationDoorGate>().AllowsAccess);
            Assert.AreEqual(1,One<PlayerInventory>().Container.GetSlots().Where(s=>s.Stack?.ItemId==part).Sum(s=>s.Stack.Quantity));
            var feature=One<SurvivorSuccessionComposition>(); var old=feature.Coordinator.Current.Id;
            Assert.AreEqual(SuccessionOutcome.Completed,feature.RequestDeath(old));
            Assert.AreEqual(SuccessionOutcome.StaleSurvivor,feature.RequestDeath(old));
            Assert.IsTrue(One<RestorationDoorGate>().AllowsAccess);
            Assert.IsEmpty(One<PlayerInventory>().Container.GetSlots().Where(s=>!s.IsEmpty));
            root.StartNewSave(); yield return null; yield return null;
            Assert.IsEmpty(session.Capture().ship.repairs); Assert.IsTrue(session.StartingLoadoutInitialized);
            Assert.AreEqual(1,WeaponSystemTestFixtures.Bar.Container.GetSlots().Where(s=>!s.IsEmpty).Sum(s=>s.Stack.Quantity));
        }
        [UnityTearDown] public IEnumerator Cleanup() { yield return WeaponSystemTestFixtures.Unload(); }
    }
}
#endif
