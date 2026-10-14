#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SOLITUDE.Application;
using SOLITUDE.Composition;
using SOLITUDE.Containers;
using SOLITUDE.Player;
using SOLITUDE.Player.Succession;
using SOLITUDE.SaveLoad;
using SOLITUDE.Survivors;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SOLITUDE.Tests
{
    public sealed class RunLifecycleResumeTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Wakeup.unity";
        private IDisposable pauseLease;
        private static T Field<T>(object owner, string name) =>
            (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
        private static T One<T>() where T : Object => Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
        private static GameCompositionRoot Root => Object.FindFirstObjectByType<GameCompositionRoot>();
        private static ContainerSaveSession Session => Field<ContainerSaveSession>(Root, "session");
        private static InputPolicyCoordinator Policy => Field<InputPolicyCoordinator>(Root, "policy");
        private static SurvivorSuccessionComposition Feature => One<SurvivorSuccessionComposition>();
        private static SurvivorAvatarAdapter Avatar => One<SurvivorAvatarAdapter>();
        private static PlayerVitalsController Vitals => One<PlayerVitalsController>();
        private static SceneFeatureContext Context => Field<SceneFeatureContext>(Feature, "context");

        [UnitySetUp] public IEnumerator Prepare()
        {
            Assert.IsTrue(Environment.GetCommandLineArgs().Contains("-solitudeSaveDirectory"), "Use the isolated verification runner.");
            if (Root != null) { Object.Destroy(Root.gameObject); yield return null; }
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; Assert.AreEqual(RuntimeStartupState.Ready, Root.State);
            Root.StartNewSave(); yield return null; yield return null;
            pauseLease = Field<PauseCoordinator>(Root, "pause").Acquire();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            pauseLease?.Dispose(); pauseLease = null;
            if (Root != null) Object.Destroy(Root.gameObject);
            yield return null; Time.timeScale = 1;
        }
        private static SurvivorSnapshot ChangedSnapshot()
        {
            var state = Avatar.Capture(Feature.Coordinator.Current.Id);
            state.health = new SavedVital { current = 33.125, max = 200 };
            state.oxygen = new SavedVital { current = 72.5, max = 120 };
            state.hunger = new SavedVital { current = 62.875, max = 150 };
            state.recoveryDelayRemaining = 3.25;
            return state;
        }
        private static void CheckVitals(SurvivorSnapshot expected)
        {
            var actual = Avatar.Capture(Feature.Coordinator.Current.Id);
            Assert.AreEqual(expected.health.current, actual.health.current); Assert.AreEqual(expected.health.max, actual.health.max);
            Assert.AreEqual(expected.oxygen.current, actual.oxygen.current); Assert.AreEqual(expected.oxygen.max, actual.oxygen.max);
            Assert.AreEqual(expected.hunger.current, actual.hunger.current); Assert.AreEqual(expected.hunger.max, actual.hunger.max);
            Assert.AreEqual(expected.recoveryDelayRemaining, actual.recoveryDelayRemaining);
        }
        private static SurvivorSnapshot PlaceOffAwakeningPoint()
        {
            var state = ChangedSnapshot(); var point = Feature.AwakeningPoint;
            foreach (var offset in new[] { Vector2.right, Vector2.left, Vector2.up, Vector2.down })
            {
                state.x = point.transform.position.x + .2f * offset.x;
                state.y = point.transform.position.y + .2f * offset.y;
                Avatar.Restore(state, point, true);
                if (!Avatar.UsedResumeFallback) return Avatar.Capture(Feature.Coordinator.Current.Id);
            }
            Assert.Fail("The authored cryobay must permit a valid saved landing away from the awakening point.");
            return null;
        }

        [UnityTest] public IEnumerator FreshBindingInitializesOneColonistAndRebindingDoesNotAwakenAgain()
        {
            Assert.IsFalse(Root.UsesDisposableSession); Assert.AreSame(Session, Context.LifecycleSession);
            Assert.AreEqual(SurvivorSceneEntry.FreshAwakening, Feature.EntryKind);
            var initial = Session.Capture();
            Assert.AreEqual(Feature.Coordinator.Current.Id.ToString("D"), initial.survivor.id);
            Assert.AreEqual(ScenePath, initial.survivor.scenePath);
            Assert.AreEqual(100, initial.survivor.health.current); Assert.AreEqual(100, initial.survivor.oxygen.current);
            Assert.IsTrue(Context.Inventory.GetSlots().All(slot => slot.IsEmpty)); Assert.IsTrue(Context.Hotbar.GetSlots().All(slot => slot.IsEmpty));
            var changed = ChangedSnapshot(); Avatar.Restore(changed, Feature.AwakeningPoint, true);
            var health = Vitals.Vitals.Health; var inventory = Context.Inventory; var context = Context;
            for (int i = 0; i < 3; i++)
            {
                Feature.Initialize(context);
                Assert.IsFalse(Policy.Ready, "Only the root may enable control after complete restoration.");
                Assert.AreEqual(SurvivorSceneEntry.SavedResume, Feature.EntryKind);
                Assert.AreEqual(initial.survivor.id, Feature.Coordinator.Current.Id.ToString("D"));
                Assert.AreEqual(initial.expedition.id, Session.Capture().expedition.id);
                Assert.AreSame(health, Vitals.Vitals.Health); Assert.AreSame(inventory, Context.Inventory);
                CheckVitals(changed); Policy.SetReady(true);
            }
            yield return null;
        }

        [UnityTest] public IEnumerator ProductionSceneReloadPreservesIdentityPreciseVitalsTimingPositionAndEquipment()
        {
            var state = PlaceOffAwakeningPoint(); var id = Feature.Coordinator.Current.Id;
            var expedition = Session.Capture().expedition.id; var oldInventory = Context.Inventory;
            const string item = "c756a156ba7f34cb2b65e32264413a60";
            Assert.IsTrue(Session.Commands.Grant(Context.Inventory.Handle, item, 3).Succeeded);
            Assert.IsTrue(Session.Commands.Grant(Context.Hotbar.Handle, item, 2).Succeeded);
            var body = One<PlayerInteractor>().GetComponent<Rigidbody2D>(); body.linearVelocity = Vector2.one * 4; body.angularVelocity = 2;
            yield return Root.TransitionTo(ScenePath); yield return null;
            Assert.AreEqual(RuntimeStartupState.Ready, Root.State); Assert.AreEqual(SurvivorSceneEntry.SavedResume, Feature.EntryKind);
            Assert.AreEqual(id, Feature.Coordinator.Current.Id); Assert.AreEqual(expedition, Session.Capture().expedition.id);
            CheckVitals(state); body = One<PlayerInteractor>().GetComponent<Rigidbody2D>();
            Assert.AreEqual((float)state.x, body.position.x); Assert.AreEqual((float)state.y, body.position.y);
            Assert.AreEqual(Vector2.zero, body.linearVelocity); Assert.AreEqual(0, body.angularVelocity);
            Assert.IsFalse(oldInventory.IsAvailable); Assert.AreEqual(3, Context.Inventory.GetSlot(0).Stack.Quantity);
            Assert.AreEqual(2, Context.Hotbar.GetSlot(0).Stack.Quantity); Assert.AreEqual(-1, Context.Selection.ActiveIndex);
            Assert.IsFalse(Avatar.UsedResumeFallback);
        }

        [UnityTest] public IEnumerator OutsideAuthoredFloorUsesSameSceneSafePointWithoutChangingLifetime()
        {
            var context = Context; var id = Feature.Coordinator.Current.Id; var saved = Session.Capture();
            var state = ChangedSnapshot(); state.x = 100000; state.y = 100000;
            Feature.Release(); Session.StoreLifecycle(state, saved.expedition);
            Feature.Initialize(context);
            Assert.IsTrue(Avatar.UsedResumeFallback); Assert.IsFalse(Policy.Ready);
            Assert.AreEqual(id, Feature.Coordinator.Current.Id); Assert.AreEqual(saved.expedition.id, Session.Capture().expedition.id);
            Assert.AreEqual((Vector2)Feature.AwakeningPoint.transform.position, One<PlayerInteractor>().GetComponent<Rigidbody2D>().position);
            CheckVitals(state); Assert.AreEqual(ScenePath, Session.Capture().survivor.scenePath);
            Policy.SetReady(true); yield return null;
        }

        [UnityTest] public IEnumerator BlockedSavedLocationFallsBackAndBlockedSafePointRejectsWithoutMutation()
        {
            var state = PlaceOffAwakeningPoint(); var barrier = new GameObject("Resume test obstruction");
            try
            {
                // Choose a distinct obstruction inside the avatar footprint but outside the safe-point footprint.
                var collider = Avatar.Interactor.GetComponentsInChildren<Collider2D>().First(c => !c.isTrigger && c.enabled);
                var direction = ((Vector2)Avatar.Interactor.transform.position - (Vector2)Feature.AwakeningPoint.transform.position).normalized;
                var bounds = collider.bounds;
                float extent = Mathf.Abs(direction.x) * bounds.extents.x + Mathf.Abs(direction.y) * bounds.extents.y;
                barrier.transform.position = bounds.center + (Vector3)(direction * (extent - .03f));
                barrier.AddComponent<BoxCollider2D>().size = Vector2.one * .04f;
                Physics2D.SyncTransforms();
                Avatar.Restore(state, Feature.AwakeningPoint, true);
                Assert.IsTrue(Avatar.UsedResumeFallback);
                Assert.AreEqual((Vector2)Feature.AwakeningPoint.transform.position, One<PlayerInteractor>().GetComponent<Rigidbody2D>().position);
                barrier.transform.position = collider.bounds.center; Physics2D.SyncTransforms();
                var before = Avatar.Capture(Feature.Coordinator.Current.Id);
                state.x = 100000; state.y = 100000;
                Assert.Throws<InvalidDataException>(() => Avatar.Restore(state, Feature.AwakeningPoint, true));
                CheckVitals(before); Assert.AreEqual((float)before.x, One<PlayerInteractor>().GetComponent<Rigidbody2D>().position.x);
            }
            finally { Object.DestroyImmediate(barrier); }
            yield return null;
        }

        [UnityTest] public IEnumerator SceneArrivalKeepsColonistVitalsButUsesAuthoredArrivalPosition()
        {
            var context = Context; var saved = Session.Capture(); var state = ChangedSnapshot();
            state.scenePath = "Assets/_Project/Scenes/AnotherProductionScene.unity"; state.x = 999; state.y = 999;
            var arrival = One<PlayerInteractor>().GetComponent<Rigidbody2D>().position;
            Feature.Release(); Session.StoreLifecycle(state, saved.expedition);
            var arriving = new SceneFeatureContext(context.Commands, context.Inventory, context.Presentations, context.Policy,
                context.Input, context.Tasks, context.TaskScope, context.Interactor, context.InteractionEvents, context.Hotbar,
                context.Selection, context.ClosePlayerScreens, context.Feedback, context.OpenContainer, context.CloseContainer,
                context.LifecycleSession, isSceneArrival: true);
            Feature.Initialize(arriving);
            Assert.AreEqual(SurvivorSceneEntry.SceneArrival, Feature.EntryKind);
            Assert.AreEqual(Guid.Parse(state.id), Feature.Coordinator.Current.Id); CheckVitals(state);
            Assert.AreEqual(arrival, One<PlayerInteractor>().GetComponent<Rigidbody2D>().position);
            Assert.AreEqual(ScenePath, Session.Capture().survivor.scenePath); Assert.AreEqual(saved.expedition.id, Session.Capture().expedition.id);
            Policy.SetReady(true); yield return null;
        }

        [UnityTest] public IEnumerator IncompatibleResumeStaysGatedAndDoesNotEraseTheSavedSnapshot()
        {
            var context = Context; var saved = Session.Capture(); var state = ChangedSnapshot();
            state.scenePath = "Assets/_Project/Scenes/AnotherProductionScene.unity";
            var priorPosition = One<PlayerInteractor>().GetComponent<Rigidbody2D>().position;
            Feature.Release(); Session.StoreLifecycle(state, saved.expedition);
            Assert.Throws<InvalidDataException>(() => Feature.Initialize(context));
            Assert.IsFalse(Policy.Ready); Assert.IsNull(Feature.Coordinator);
            Assert.AreEqual(state.scenePath, Session.Capture().survivor.scenePath);
            Assert.AreEqual(priorPosition, One<PlayerInteractor>().GetComponent<Rigidbody2D>().position);
            yield return null;
        }

        [UnityTest] public IEnumerator VitalRestorationPublishesCoherentlyAndRejectsReentrantOrIncompatibleChanges()
        {
            var state = ChangedSnapshot(); var oldHealth = Vitals.Vitals.Health; bool coherent = false, reentrantRejected = false;
            var targetVitals = Vitals; var targetAvatar = Avatar; var point = Feature.AwakeningPoint;
            Action observer = () =>
            {
                coherent = targetVitals.Vitals.Health.Current == (float)state.health.current
                    && targetVitals.Vitals.Oxygen.Current == (float)state.oxygen.current
                    && targetVitals.Vitals.Hunger.Current == (float)state.hunger.current
                    && targetVitals.RecoveryState.DelayRemaining == state.recoveryDelayRemaining;
                try { targetAvatar.Restore(state, point, true); }
                catch (InvalidOperationException) { reentrantRejected = true; }
            };
            targetVitals.PresentationChanged += observer;
            try { targetAvatar.Restore(state, point, true); }
            finally { targetVitals.PresentationChanged -= observer; }
            Assert.IsTrue(coherent); Assert.IsTrue(reentrantRejected); Assert.AreSame(oldHealth, targetVitals.Vitals.Health);
            CheckVitals(state);
            var invalid = state.Clone(); invalid.health.max = double.MaxValue; var position = targetAvatar.Interactor.transform.position;
            Assert.Throws<InvalidDataException>(() => targetAvatar.Restore(invalid, point, true));
            CheckVitals(state); Assert.AreEqual(position, targetAvatar.Interactor.transform.position);
            yield return null;
        }

        [UnityTest] public IEnumerator DisposableDemoReloadStillStartsFreshWithoutSavedLifecycle()
        {
            pauseLease.Dispose(); pauseLease = null; Object.Destroy(Root.gameObject); yield return null;
            const string demo = "Assets/_Project/Scenes/Demos/AtmosphericScrubberEncounter.unity";
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(demo, new LoadSceneParameters(LoadSceneMode.Single)); yield return null;
            var first = Feature.Coordinator.Current.Id;
            Assert.IsTrue(Root.UsesDisposableSession); Assert.IsNull(Context.LifecycleSession); Assert.IsNull(Session.Capture().survivor);
            Vitals.Vitals.Health.Set(15);
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(demo, new LoadSceneParameters(LoadSceneMode.Single)); yield return null;
            Assert.AreNotEqual(first, Feature.Coordinator.Current.Id); Assert.AreEqual(100, Vitals.Vitals.Health.Current);
            Assert.AreEqual(SurvivorSceneEntry.DisposableAwakening, Feature.EntryKind); Assert.IsNull(Session.Capture().survivor);
        }
    }
}
#endif
