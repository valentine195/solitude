#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SOLITUDE.Application;
using SOLITUDE.Composition;
using SOLITUDE.Containers;
using SOLITUDE.Player;
using SOLITUDE.Player.Succession;
using SOLITUDE.Player.UI;
using SOLITUDE.Vitals;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SOLITUDE.Tests
{
    public class PlayerVitalsSliceTests
    {
        private Keyboard keyboard;
        private InputSettings.BackgroundBehavior background;
        private InputSettings.EditorInputBehaviorInPlayMode editor;
        private static T One<T>() where T : Object => Object.FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Single();
        private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
        private static GameCompositionRoot Root => Object.FindAnyObjectByType<GameCompositionRoot>();
        private static PlayerVitalsController Player => One<PlayerVitalsController>();
        private static InputPolicyCoordinator Policy => Field<InputPolicyCoordinator>(Root,"policy");
        private static TMP_Text Status => One<VitalRecoveryStatusView>().GetComponent<TMP_Text>();
        private void Keys(params Key[] keys) { InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys)); InputSystem.Update(); }
        [UnitySetUp] public IEnumerator Prepare()
        {
            Assert.IsTrue(Environment.GetCommandLineArgs().Contains("-solitudeSaveDirectory"),"Run with disposable saves.");
            if(Root!=null){Object.Destroy(Root.gameObject);yield return null;}
            Time.timeScale=1; background=InputSystem.settings.backgroundBehavior;editor=InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard=InputSystem.AddDevice<Keyboard>();
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/_Project/Scenes/Wakeup.unity",new LoadSceneParameters(LoadSceneMode.Single)); yield return null;
            Assert.AreEqual(RuntimeStartupState.Ready,Root.State); Root.StartNewSave();yield return null;yield return null;
            Assert.IsTrue(Player.GameplayEligible); Assert.AreSame(Player,One<PlayerVitalsComposition>().Controller);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(keyboard!=null)InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.backgroundBehavior=background;InputSystem.settings.editorInputBehaviorInPlayMode=editor;
            if(Root!=null)Object.Destroy(Root.gameObject);
            yield return null;Time.timeScale=1;
        }
        private static UnityEngine.UI.Image Fill(string row) => Status.transform.parent.parent.Find(row+"/"+row.Replace("Row","Bar")+"/Bar/Fill").GetComponent<UnityEngine.UI.Image>();
        [UnityTest] public IEnumerator ActualWakeupClockDelaysThenRecoversToBoundaryWithoutChangingOxygen()
        {
            var p=Player; Assert.AreEqual(4,p.Settings.SegmentCount);Assert.AreEqual(5,p.Settings.DamageDelay);Assert.AreEqual(2,p.Settings.RecoveryRate);
            p.Vitals.Health.Set(40);p.Vitals.Hunger.Set(100);Assert.IsTrue(p.Damage(10).Accepted);Assert.AreEqual("Recovery delayed",Status.text);
            yield return new WaitForSeconds(.2f);Assert.AreEqual(30,p.Vitals.Health.Current);Assert.Greater(p.RecoveryState.DelayRemaining,4);
            yield return new WaitForSeconds(5.1f);Assert.Greater(p.Vitals.Health.Current,30);Assert.Less(p.Vitals.Health.Current,35);Assert.AreEqual("Recovering",Status.text);
            yield return new WaitForSeconds(10.1f);Assert.AreEqual(50,p.Vitals.Health.Current);Assert.AreEqual("",Status.text);
            yield return new WaitForSeconds(.2f);Assert.AreEqual(50,p.Vitals.Health.Current);Assert.Less(p.Vitals.Hunger.Current,99);Assert.AreEqual(100,p.Vitals.Oxygen.Current);
            Assert.AreEqual(.5f,Fill("HealthRow").fillAmount);
        }
        [UnityTest] public IEnumerator HungryPauseMaximumChangeAndPrimaryScreenUseActualSceneBindings()
        {
            var p=Player;p.Configure(new VitalsRecoverySettings(damageDelay:.1));p.Vitals.Health.Set(40);p.Damage(10);p.Vitals.Hunger.Set(0);
            yield return new WaitForSeconds(.2f);Assert.AreEqual(30,p.Vitals.Health.Current);Assert.AreEqual(0,p.RecoveryState.DelayRemaining);Assert.AreEqual("Recovery paused: hungry",Status.text);
            Assert.IsTrue(p.TryApplyPrepared(p.PrepareFood(25)));yield return null;Assert.Greater(p.Vitals.Health.Current,30);Assert.AreEqual("Recovering",Status.text);
            float health=p.Vitals.Health.Current,hunger=p.Vitals.Hunger.Current;
            p.Configure(new VitalsRecoverySettings(maxHealth:200,maxHunger:200,segmentCount:3,fedThresholdFraction:.4));
            Assert.AreEqual(health,p.Vitals.Health.Current);Assert.AreEqual(hunger,p.Vitals.Hunger.Current);Assert.AreEqual(health/200,Fill("HealthRow").fillAmount,1e-6f);Assert.AreEqual("Recovery paused: hungry",Status.text);
            using(Policy.AcquirePrimaryScreen())
            {
                Assert.AreEqual(1,Time.timeScale);Assert.AreEqual("Recovery paused",Status.text);
                yield return new WaitForSecondsRealtime(.2f);Assert.AreEqual(health,p.Vitals.Health.Current);Assert.AreEqual(hunger,p.Vitals.Hunger.Current);
            }
            yield return null;Assert.AreEqual(health,p.Vitals.Health.Current);Assert.Less(p.Vitals.Hunger.Current,hunger);
            p.Configure(new VitalsRecoverySettings());p.Vitals.Health.Set(30);p.Vitals.Hunger.Set(100);
            Keys(Key.Tab);Keys();yield return null;Assert.IsTrue(Field<ContainerModalView>(One<SceneBindings>(),"inventoryModal").IsOpen);Assert.IsFalse(p.GameplayEligible);Assert.AreEqual("Recovery paused",Status.text);
            health=p.Vitals.Health.Current;hunger=p.Vitals.Hunger.Current;
            yield return new WaitForSecondsRealtime(.2f);Assert.AreEqual(health,p.Vitals.Health.Current);Assert.AreEqual(hunger,p.Vitals.Hunger.Current);
            Keys(Key.Escape);Keys();yield return null;Assert.IsTrue(p.GameplayEligible);Assert.IsFalse(Field<ContainerModalView>(One<SceneBindings>(),"inventoryModal").IsOpen);
            yield return new WaitForSeconds(.1f);Assert.Greater(p.Vitals.Health.Current,health);
        }
        [UnityTest] public IEnumerator RetainedAwakeningAndSceneReleaseResetAndFreezeBoundViews()
        {
            var p=Player;var health=p.Vitals.Health;var oxygen=p.Vitals.Oxygen;var hunger=p.Vitals.Hunger;var view=Status;
            health.Set(40);p.Damage(10);hunger.Set(10);oxygen.Set(12);
            One<SurvivorAvatarAdapter>().Awaken(One<SurvivorSuccessionComposition>().AwakeningPoint);
            Assert.AreSame(health,p.Vitals.Health);Assert.AreSame(oxygen,p.Vitals.Oxygen);Assert.AreSame(hunger,p.Vitals.Hunger);Assert.AreSame(view,Status);
            Assert.AreEqual(100,health.Current);Assert.AreEqual(100,oxygen.Current);Assert.AreEqual(100,hunger.Current);Assert.AreEqual(0,p.RecoveryState.DelayRemaining);Assert.AreEqual("",Status.text);
            health.Set(30);Assert.AreEqual("Recovering",Status.text);
            One<SceneBindings>().Release();Assert.IsFalse(p.GameplayEligible);Assert.AreEqual("Recovery paused",Status.text);
            yield return new WaitForSecondsRealtime(.2f);Assert.AreEqual(30,health.Current);Assert.AreEqual(100,hunger.Current);
        }
    }
}
#endif
