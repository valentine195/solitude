#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SOLITUDE.Application;
using SOLITUDE.Combat;
using SOLITUDE.Composition;
using SOLITUDE.Containers;
using SOLITUDE.Core.Animations;
using SOLITUDE.Items;
using SOLITUDE.Movement;
using SOLITUDE.Npcs;
using SOLITUDE.Npcs.Sec0;
using SOLITUDE.Player;
using SOLITUDE.SaveLoad;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SOLITUDE.Tests
{
    // Real authored scene/root/input and physics. The gameplay clock is explicitly stepped
    // to make phase interruption and commitment assertions independent of runner frame rate.
    public sealed class WeaponCombatIntegrationTests
    {
        private Gamepad pad;
        private InputSettings.BackgroundBehavior background;
        private InputSettings.EditorInputBehaviorInPlayMode editor;
        private Sec0Controller bot;
        private Sec0DamageRecipient recipient;
        private PlayerItemActionComposition actions;
        private PlayerMovement player;
        private MovementBehavior movement;
        private PlayerStunReceiver stun;
        private PlayerVitalsController vitals;
        private readonly List<Object> temporary = new();
        private readonly HashSet<MeleeAttackExecution> swings = new();
        private static T Field<T>(object owner, string name) => WeaponSystemTestFixtures.Field<T>(owner, name);
        private List<GameObject> Markers => Field<List<GameObject>>(actions.GetComponent<MeleeImpactFeedback>(), "effects");
        [UnitySetUp] public IEnumerator Prepare()
        {
            background = InputSystem.settings.backgroundBehavior; editor = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            pad = InputSystem.AddDevice<Gamepad>(); yield return WeaponSystemTestFixtures.LoadSec0Encounter();
            bot = WeaponSystemTestFixtures.One<Sec0Controller>(); recipient = bot.GetComponent<Sec0DamageRecipient>();
            actions = WeaponSystemTestFixtures.Actions; player = actions.GetComponent<PlayerMovement>();
            movement = player.GetComponent<MovementBehavior>(); stun = player.GetComponent<PlayerStunReceiver>(); vitals = player.GetComponent<PlayerVitalsController>();
            Time.timeScale = 0; Place(new Vector2(55, 0), new Vector2(55, -.9f), Vector2.up);
            swings.Clear(); actions.ActiveSample += execution => swings.Add(execution);
            Assert.IsTrue(bot.HasBinding); Assert.IsTrue(recipient.IsAvailable); Assert.AreEqual(40, recipient.CurrentHealth);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (pad != null) InputSystem.RemoveDevice(pad);
            InputSystem.settings.backgroundBehavior = background; InputSystem.settings.editorInputBehaviorInPlayMode = editor;
            // Release execution before destroying its temporary configuration/catalog.
            if (actions != null) actions.Release();
            foreach (var item in temporary) if (item != null) Object.DestroyImmediate(item); temporary.Clear();
            yield return WeaponSystemTestFixtures.Unload();
        }
        private void Place(Vector2 robot, Vector2 actor, Vector2 facing)
        {
            bot.transform.position = robot; bot.GetComponent<Rigidbody2D>().position = robot;
            player.transform.position = actor; player.GetComponent<Rigidbody2D>().position = actor;
            movement.SetVector(facing); Physics2D.SyncTransforms();
        }
        private IEnumerator Primary()
        {
            InputSystem.QueueStateEvent(pad, new GamepadState()); InputSystem.Update(); yield return null;
            InputSystem.QueueStateEvent(pad, new GamepadState { rightTrigger = 1 }); InputSystem.Update();
            yield return null; yield return null;
        }
        private void CleanPresentation()
        {
            Assert.AreEqual(1, movement.SpeedFactor);
            Assert.IsFalse(player.GetComponent<SpriteRenderer>().forceRenderingOff);
            var swingBody = player.transform.Find("MeleeBodyAnimation");
            if (swingBody != null) Assert.IsFalse(swingBody.GetComponent<SpriteRenderer>().enabled);
            Assert.AreEqual(0, Markers.Count); Assert.IsNull(Field<object>(bot, "knockbackOwner"));
        }
        [UnityTest] public IEnumerator FourActualPrimarySwingsDefeatAuthoredRobotAndClearContactResources()
        {
            int deaths = 0; recipient.Defeated += () => deaths++;
            for (int hit = 1; hit <= 4; hit++)
            {
                yield return Primary(); Assert.IsTrue(actions.Runner.IsExecuting);
                var wrenchAttack = (SOLITUDE.Items.MeleeAttackAction)WeaponSystemTestFixtures.Wrench.Actions[0].Action;
                Assert.AreSame(wrenchAttack.Configuration.PosedWeaponFrames[4], actions.GetComponent<MeleeSwingView>().HeldRenderer.sprite);
                actions.Advance(.119); Assert.AreEqual(50 - hit * 10, recipient.CurrentHealth);
                actions.Advance(.002); Assert.AreEqual(40 - hit * 10, recipient.CurrentHealth); Assert.AreEqual(1, Markers.Count);
                actions.Advance(.04); Assert.AreEqual(40 - hit * 10, recipient.CurrentHealth, "Repeated active sampling must not duplicate damage.");
                actions.Advance(.40); Assert.IsFalse(actions.Runner.IsExecuting); CleanPresentation();
            }
            Assert.AreEqual(4, swings.Count); Assert.AreEqual(1, deaths); Assert.IsTrue(bot.IsDefeated);
            Assert.IsFalse(bot.GetComponent<Rigidbody2D>().simulated); Assert.IsTrue(bot.GetComponentsInChildren<Collider2D>().All(c => !c.enabled));
            yield return Primary(); actions.Advance(.56); Assert.AreEqual(0, recipient.CurrentHealth); Assert.AreEqual(1, deaths);
            Assert.AreEqual(1, WeaponSystemTestFixtures.Bar.Container.GetSlot(0).Stack.Quantity);
        }
        [UnityTest] public IEnumerator MeleeDamageAndPushResistanceWorkIndependentlyOnRepeatedActiveSamples()
        {
            var resistance = new UnityEditor.SerializedObject(bot.GetComponent<EnemyMeleeResistance>());
            var visual = bot.GetComponentInChildren<Animator>().transform;
            var restingScale = visual.localScale;
            resistance.FindProperty("damageResistance").floatValue = 1;
            resistance.FindProperty("knockbackResistance").floatValue = .5f;
            resistance.ApplyModifiedPropertiesWithoutUndo();
            yield return Primary();
            actions.Advance(.13);
            Assert.AreEqual(40, recipient.CurrentHealth);
            Assert.AreEqual(.325f * swings.Single().Context.UnitsPerTile, Field<float>(bot, "knockbackDistance"), .0001f);
            Assert.Less(visual.localScale.y, restingScale.y);
            actions.Advance(.03);
            Assert.AreEqual(1, Markers.Count);
            actions.Advance(.4);
            Assert.AreEqual(restingScale, visual.localScale);

            resistance.FindProperty("damageResistance").floatValue = .25f;
            resistance.FindProperty("knockbackResistance").floatValue = 1;
            resistance.ApplyModifiedPropertiesWithoutUndo();
            yield return Primary();
            actions.Advance(.13);
            Assert.AreEqual(32.5, recipient.CurrentHealth);
            Assert.IsNull(Field<object>(bot, "knockbackOwner"));
            Assert.Less(visual.localScale.y, restingScale.y);
        }
        [UnityTest] public IEnumerator ActualPrimaryMissesForRangeArcAndAuthoredCollisionCover()
        {
            Place(new Vector2(55, 2), new Vector2(55, -.9f), Vector2.up);
            yield return Primary(); actions.Advance(.56); Assert.AreEqual(40, recipient.CurrentHealth); CleanPresentation();
            Place(new Vector2(55, 0), new Vector2(55, -.9f), Vector2.down);
            yield return Primary(); actions.Advance(.56); Assert.AreEqual(40, recipient.CurrentHealth); CleanPresentation();
            Place(new Vector2(55, 0), new Vector2(55, -.9f), Vector2.up);
            var cover = new GameObject("Disposable melee cover"); temporary.Add(cover);
            cover.transform.position = new Vector2(55, -.45f); cover.AddComponent<BoxCollider2D>().size = new Vector2(2, .1f); Physics2D.SyncTransforms();
            yield return Primary(); actions.Advance(.56); Assert.AreEqual(40, recipient.CurrentHealth); CleanPresentation();
            Object.DestroyImmediate(cover); Physics2D.SyncTransforms();
            yield return Primary(); actions.Advance(.12); Assert.AreEqual(30, recipient.CurrentHealth);
        }
        [UnityTest] public IEnumerator ExistingRobotPulseInterruptsWindupAndMovementRecoversAfterStun()
        {
            yield return Primary(); actions.Advance(.05);
            bot.Tick(.4f); bot.Tick(.02f); Assert.AreEqual(SecurityBotState.Charging, bot.State);
            bot.Tick(.7f); Assert.AreEqual(1, bot.ShotsFired); Assert.AreEqual(1, bot.StunsLanded); Assert.IsTrue(stun.IsStunned);
            actions.Advance(0); Assert.IsFalse(actions.Runner.IsExecuting); Assert.AreEqual(40, recipient.CurrentHealth);
            Assert.IsTrue(movement.IsMovementBlocked); CleanPresentation();
            yield return Primary(); Assert.IsFalse(actions.Runner.IsExecuting); actions.Advance(1); Assert.AreEqual(40, recipient.CurrentHealth);
            stun.Advance(1); Assert.IsFalse(stun.IsStunned); Assert.IsFalse(movement.IsMovementBlocked);
            actions.Advance(.56); yield return Primary(); Assert.IsTrue(actions.Runner.IsExecuting);
            actions.Advance(.12); Assert.AreEqual(30, recipient.CurrentHealth); Assert.AreEqual(100, vitals.Vitals.Health.Current);
        }
        [UnityTest] public IEnumerator VitalZeroInterruptsActiveHitAndCannotLeaveDamageOrPresentationBehind()
        {
            yield return Primary(); actions.Advance(.15); Assert.AreEqual(30, recipient.CurrentHealth); Assert.AreEqual(1, Markers.Count);
            vitals.Vitals.Health.Set(0); Assert.IsFalse(actions.Runner.IsExecuting); CleanPresentation();
            yield return Primary(); Assert.IsFalse(actions.Runner.IsExecuting); actions.Advance(10); Assert.AreEqual(30, recipient.CurrentHealth);
            Assert.AreEqual(0, vitals.Vitals.Health.Current); Assert.IsTrue(recipient.IsAvailable); Assert.IsFalse(actions.Request());
        }
        [UnityTest] public IEnumerator SameDefinitionReplacementClearsHitFeedbackAndCannotBypassCommitment()
        {
            yield return Primary(); actions.Advance(.15); Assert.AreEqual(30, recipient.CurrentHealth); Assert.AreEqual(1, Markers.Count);
            var bar = WeaponSystemTestFixtures.Bar; var commands = WeaponSystemTestFixtures.Session.Commands;
            commands.Restore(bar.Container.Handle, ContainerSaveSerializer.Capture(bar.Container));
            Assert.IsFalse(actions.Runner.IsExecuting); CleanPresentation();
            Assert.That(actions.Runner.LockoutRemaining, Is.EqualTo(.4).Within(.000001));
            yield return Primary(); Assert.IsFalse(actions.Runner.IsExecuting); Assert.AreEqual(30, recipient.CurrentHealth);
            actions.Advance(.41); yield return Primary(); Assert.IsTrue(actions.Runner.IsExecuting);
            actions.Advance(.12); Assert.AreEqual(20, recipient.CurrentHealth);
        }
        [UnityTest] public IEnumerator LostEquipmentOwnerPreventsFreshAndStaleDamage()
        {
            yield return Primary(); actions.Advance(.15); var execution = swings.Single();
            WeaponSystemTestFixtures.Session.Commands.Unregister(WeaponSystemTestFixtures.Bar.Container.Handle);
            Assert.IsFalse(actions.Runner.IsExecuting); Assert.IsTrue(execution.IsComplete); CleanPresentation();
            Assert.That(actions.Runner.LockoutRemaining, Is.EqualTo(.4).Within(.000001));
            execution.Advance(10); actions.Advance(10); yield return Primary(); Assert.IsFalse(actions.Runner.IsExecuting);
            Assert.AreEqual(30, recipient.CurrentHealth);
        }
        [UnityTest] public IEnumerator SceneReleaseDisposesActiveGeometryAndAllPresentationResources()
        {
            yield return Primary(); actions.Advance(.15); var execution = swings.Single();
            WeaponSystemTestFixtures.One<SceneBindings>().Release(); Assert.IsNull(actions.Runner); CleanPresentation();
            Assert.IsTrue(execution.IsComplete); execution.Advance(10); yield return Primary();
            Assert.IsNull(actions.Runner); Assert.AreEqual(30, recipient.CurrentHealth); Assert.IsFalse(actions.Request());
        }
        [UnityTest] public IEnumerator SharedDefinitionsDoNotShareHitTrackingOrCancellationBetweenActors()
        {
            var database = Resources.Load<ItemDatabase>("Database"); var catalog = database.BuildCapabilityCatalog();
            var commands = new ContainerCommandService(database.BuildRuntimeCatalog()); var bar = commands.Register("second-actor", 1);
            commands.Grant(bar.Handle, WeaponSystemTestFixtures.Wrench.ItemId, 1);
            using var selection = new HotbarSelectionState(bar); selection.Select(0);
            var other = new GameObject("Independent actor sharing wrench"); temporary.Add(other); other.transform.position = new Vector2(54.1f, 0);
            other.AddComponent<Rigidbody2D>().gravityScale = 0; other.AddComponent<HumanAnimationController>();
            var otherMovement = other.AddComponent<MovementBehavior>(); otherMovement.SetVector(Vector2.right);
            var otherVitals = other.AddComponent<PlayerVitalsController>(); var feature = other.AddComponent<PlayerVitalsComposition>();
            feature.Configure(otherVitals); feature.Initialize(new SceneFeatureContext(null, null, null, WeaponSystemTestFixtures.Policy, null, null, "shared-actor", null, null));
            otherVitals.ResetForAwakening(); other.AddComponent<MeleeSwingView>(); var otherActions = other.AddComponent<PlayerItemActionComposition>();
            otherActions.Bind(commands, bar, selection, catalog, WeaponSystemTestFixtures.Policy);
            yield return Primary(); Assert.IsTrue(otherActions.Request()); actions.Advance(.12); Assert.AreEqual(30, recipient.CurrentHealth);
            Assert.That(otherActions.Runner.LockoutRemaining, Is.EqualTo(.55).Within(.000001));
            actions.Release(); Assert.IsTrue(otherActions.Runner.IsExecuting); Assert.AreEqual(.65f, otherMovement.SpeedFactor);
            otherActions.Advance(.12); Assert.AreEqual(20, recipient.CurrentHealth); otherActions.Advance(.44);
            Assert.IsFalse(otherActions.Runner.IsExecuting); Assert.AreEqual(1, otherMovement.SpeedFactor);
        }
        [UnityTest] public IEnumerator TemporaryMeleeDefinitionUsesDifferentTagsTuningAndTheSamePrimaryInputRoute()
        {
            var item = Object.Instantiate(WeaponSystemTestFixtures.Wrench); temporary.Add(item);
            var serialized = new SerializedObject(item); serialized.FindProperty("itemId").stringValue = "temporary-melee-integration"; serialized.ApplyModifiedPropertiesWithoutUndo();
            item.GameplayTags = new[] { AssetDatabase.LoadAssetAtPath<SOLITUDE.GameplayTags.GameplayTagDefinition>("Assets/_Project/Systems/GameplayTags/Assets/ToolMechanical.asset") };
            var action = ScriptableObject.CreateInstance<MeleeAttackAction>(); temporary.Add(action);
            var source = ((MeleeAttackAction)WeaponSystemTestFixtures.Wrench.Actions[0].Action).Configuration;
            action.Configuration = Object.Instantiate(source); temporary.Add(action.Configuration);
            action.Configuration.BaseDamage = 7; action.Configuration.RangeTiles = 1; action.Configuration.Windup = .05;
            action.Configuration.ActiveDuration = .05; action.Configuration.Recovery = .2;
            item.Actions = new[] { new ItemActionBinding(ItemActionSlot.Primary, action) };
            var database = Object.Instantiate(Resources.Load<ItemDatabase>("Database")); temporary.Add(database);
            var data = new SerializedObject(database); var items = data.FindProperty("items"); items.InsertArrayElementAtIndex(items.arraySize);
            items.GetArrayElementAtIndex(items.arraySize - 1).objectReferenceValue = item; data.ApplyModifiedPropertiesWithoutUndo();
            var catalog = database.BuildCapabilityCatalog(); var commands = new ContainerCommandService(database.BuildRuntimeCatalog());
            var bar = commands.Register("alternate-melee", 1); commands.Grant(bar.Handle, item.ItemId, 1);
            using var selection = new HotbarSelectionState(bar); selection.Select(0);
            actions.Bind(commands, bar, selection, catalog, WeaponSystemTestFixtures.Policy);
            yield return Primary(); Assert.IsTrue(actions.Runner.IsExecuting);
            Assert.That(actions.Runner.LockoutRemaining, Is.EqualTo(.3).Within(.000001));
            actions.Advance(.05); Assert.AreEqual(33, recipient.CurrentHealth); var execution = swings.Single();
            Assert.AreEqual(item.ItemId, execution.Context.ItemId); Assert.AreSame(action, execution.Context.SourceAction);
            actions.Advance(.26); Assert.IsFalse(actions.Runner.IsExecuting);
            Assert.AreEqual(10, source.BaseDamage); Assert.AreEqual(.12, source.Windup); Assert.AreEqual(6, WeaponSystemTestFixtures.Wrench.GameplayTags.Length);
        }
    }
}
#endif
