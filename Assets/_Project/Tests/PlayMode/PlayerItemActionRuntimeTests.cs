using System;
using NUnit.Framework;
using SOLITUDE.Application;
using SOLITUDE.Combat;
using SOLITUDE.Composition;
using SOLITUDE.Containers;
using SOLITUDE.Core.Animations;
using SOLITUDE.Items;
using SOLITUDE.Movement;
using SOLITUDE.Npcs.Sec0;
using SOLITUDE.Player;
using UnityEngine;
using Object = UnityEngine.Object;
namespace SOLITUDE.Tests
{
    public sealed class PlayerItemActionRuntimeTests
    {
        private GameObject actor;
        private PlayerItemActionComposition actions;
        private PlayerVitalsComposition vitalsFeature;
        private PlayerVitalsController vitals;
        private PlayerStunReceiver stun;
        private MovementBehavior movement;
        private MeleeSwingView view;
        private PauseCoordinator pause;
        private InputPolicyCoordinator policy;
        private ContainerCommandService commands;
        private Container hotbar;
        private HotbarSelectionState selection;
        private ItemDatabase database;
        private ItemCapabilityCatalog capabilities;
        private string wrenchId;
        [SetUp] public void Setup()
        {
            pause = new PauseCoordinator(); policy = new InputPolicyCoordinator(pause); policy.SetReady(true);
            database = Resources.Load<ItemDatabase>("Database"); capabilities = database.BuildCapabilityCatalog();
            foreach (var spec in database.BuildRuntimeCatalog().Items)
                if (capabilities.Resolve(spec.ItemId)?.Resolve(ItemActionSlot.Primary) != null) wrenchId = spec.ItemId;
            Assert.NotNull(wrenchId);
            commands = new ContainerCommandService(database.BuildRuntimeCatalog()); hotbar = commands.Register("action-hotbar", 3);
            selection = new HotbarSelectionState(hotbar); commands.Grant(hotbar.Handle, wrenchId, 1); selection.Select(0);
            actor = new GameObject("Shared swing fixture"); actor.AddComponent<Rigidbody2D>().gravityScale = 0;
            actor.AddComponent<HumanAnimationController>(); movement = actor.AddComponent<MovementBehavior>();
            vitals = actor.AddComponent<PlayerVitalsController>(); vitalsFeature = actor.AddComponent<PlayerVitalsComposition>();
            vitalsFeature.Configure(vitals); vitalsFeature.Initialize(new SceneFeatureContext(null, null, null, policy, null, null, "swing", null, null));
            vitals.ResetForAwakening(); stun = actor.AddComponent<PlayerStunReceiver>();
            stun.Bind(this, policy, () => actor != null && vitals.Vitals.Health.Current > 0);
            view = actor.AddComponent<MeleeSwingView>(); actions = actor.AddComponent<PlayerItemActionComposition>();
            actions.Bind(commands, hotbar, selection, capabilities, policy);
        }
        [TearDown] public void Cleanup()
        {
            if (actor != null) Object.DestroyImmediate(actor);
            selection?.Dispose(); policy?.Dispose();
        }
        [Test] public void CapturedFacingSlowdownAndSharedSpriteFollowConfiguredPhases()
        {
            Assert.AreEqual(Vector2.down, movement.FacingDirection);
            movement.SetVector(new Vector2(1, 1)); var direction = movement.FacingDirection; MeleeAttackExecution active = null;
            actions.ActiveSample += execution => { Assert.IsTrue(execution.IsEvaluatingActive); active = execution; };
            Assert.IsTrue(actions.Request()); Assert.IsTrue(view.HeldRenderer.enabled);
            Assert.AreSame(capabilities.Resolve(wrenchId).HeldSprite, view.HeldRenderer.sprite);
            Assert.AreEqual(.65f, movement.SpeedFactor); movement.SetVector(Vector2.left);
            actions.Advance(.12); Assert.NotNull(active); Assert.AreEqual(direction, active.Context.Direction);
            using (movement.BlockMovement()) { Assert.IsTrue(movement.IsMovementBlocked); actions.Advance(.10); Assert.IsTrue(movement.IsMovementBlocked); }
            actions.Advance(.33); Assert.AreEqual(1, movement.SpeedFactor); Assert.IsFalse(view.HeldRenderer.enabled);
            Assert.IsFalse(actions.Runner.IsExecuting); Assert.AreEqual(Vector2.left, movement.FacingDirection);
        }
        [Test] public void CoarseActiveSampleIsDeliveredAndReentrantUnequipCleansUp()
        {
            int samples = 0;
            actions.ActiveSample += execution => { samples++; Assert.IsTrue(execution.IsEvaluatingActive); Assert.IsFalse(execution.IsComplete); selection.Select(-1); };
            Assert.IsTrue(actions.Request()); actions.Advance(.55);
            Assert.AreEqual(1, samples); Assert.AreEqual(1, movement.SpeedFactor); Assert.IsFalse(view.HeldRenderer.enabled);
            Assert.IsFalse(actions.Runner.IsExecuting); Assert.IsFalse(actions.Request());
        }
        [TestCase(.05)] [TestCase(.15)] [TestCase(.3)]
        public void DeathStunAndDisableCancelInEveryPhase(double elapsed)
        {
            actions.Request(); actions.Advance(elapsed); vitals.Vitals.Health.Set(0);
            Assert.IsFalse(actions.Runner.IsExecuting); Assert.AreEqual(1, movement.SpeedFactor); Assert.IsFalse(actions.Request());
            vitals.ResetForAwakening(); actions.Advance(.55);
            Assert.IsTrue(actions.Request()); actions.Advance(elapsed); Assert.IsTrue(stun.TryStun(1)); actions.Advance(0);
            Assert.IsFalse(actions.Runner.IsExecuting); Assert.AreEqual(1, movement.SpeedFactor); Assert.IsTrue(movement.IsMovementBlocked);
            stun.Advance(1); actions.Advance(.55); Assert.IsTrue(actions.Request());
            actions.enabled = false; Assert.IsFalse(actions.Runner.IsExecuting); Assert.AreEqual(1, movement.SpeedFactor);
        }
        [Test] public void PauseAndScreenFreezeWhileTransitionCancels()
        {
            actions.Request(); using (pause.Acquire())
            { actions.Advance(1); Assert.That(actions.Runner.LockoutRemaining, Is.EqualTo(.55)); Assert.IsFalse(actions.Request()); Assert.AreEqual(.65f, movement.SpeedFactor); }
            using (policy.AcquirePrimaryScreen()) { actions.Advance(1); Assert.That(actions.Runner.LockoutRemaining, Is.EqualTo(.55)); }
            policy.SetTransition(true); Assert.IsFalse(actions.Runner.IsExecuting); Assert.AreEqual(1, movement.SpeedFactor);
            policy.SetTransition(false); Assert.IsFalse(actions.Request()); actions.Advance(.55); Assert.IsTrue(actions.Request());
        }
        [Test] public void SameDefinitionReplacementOwnerLossAndRepeatedReleaseCannotLeaveState()
        {
            actions.Request(); var saved = SOLITUDE.SaveLoad.ContainerSaveSerializer.Capture(hotbar);
            commands.Restore(hotbar.Handle, saved); Assert.IsFalse(actions.Runner.IsExecuting); Assert.IsFalse(actions.Request());
            actions.Advance(.55); Assert.IsTrue(actions.Request()); commands.Unregister(hotbar.Handle);
            Assert.IsFalse(actions.Runner.IsExecuting); Assert.AreEqual(1, movement.SpeedFactor);
            actions.Release(); actions.Release(); Assert.IsNull(actions.Runner);
        }
        [Test] public void MissingPresentationAndActionlessSelectionsAreSafe()
        {
            Object.DestroyImmediate(view); actions.Release();
            actions.Bind(commands, hotbar, selection, capabilities, policy);
            Assert.IsTrue(actions.Request()); actions.Advance(.55); Assert.IsFalse(actions.Runner.IsExecuting);
            selection.Select(2); Assert.IsFalse(actions.Request()); Assert.IsFalse(actions.Request(ItemActionSlot.Contextual));
        }
        [Test] public void TwoActorsShareAssetsWithoutSharingExecutionOrRestrictions()
        {
            var other = new GameObject("Second swing actor");
            try
            {
                var otherMovement = other.AddComponent<MovementBehavior>(); other.AddComponent<HumanAnimationController>();
                var otherVitals = other.AddComponent<PlayerVitalsController>(); var feature = other.AddComponent<PlayerVitalsComposition>();
                feature.Configure(otherVitals); feature.Initialize(new SceneFeatureContext(null, null, null, policy, null, null, "second", null, null));
                otherVitals.ResetForAwakening(); var otherActions = other.AddComponent<PlayerItemActionComposition>();
                otherActions.Bind(commands, hotbar, selection, capabilities, policy);
                Assert.IsTrue(actions.Request()); Assert.IsTrue(otherActions.Request()); actions.Advance(.55);
                Assert.IsFalse(actions.Runner.IsExecuting); Assert.IsTrue(otherActions.Runner.IsExecuting);
                Assert.AreEqual(1, movement.SpeedFactor); Assert.AreEqual(.65f, otherMovement.SpeedFactor);
                actions.Release(); Assert.IsTrue(otherActions.Runner.IsExecuting); otherActions.Advance(.55);
                Assert.AreEqual(1, otherMovement.SpeedFactor);
            }
            finally { Object.DestroyImmediate(other); }
        }
    }
}
