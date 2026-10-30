#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SOLITUDE.Application.Survivors;
using SOLITUDE.Combat;
using SOLITUDE.Composition;
using SOLITUDE.Containers;
using SOLITUDE.Hotbar;
using SOLITUDE.Player;
using SOLITUDE.Player.Succession;
using SOLITUDE.SaveLoad;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;
namespace SOLITUDE.Tests
{
    public sealed class EquippedWrenchSwingTests
    {
        private Mouse mouse;
        private Gamepad pad;
        private Keyboard keyboard;
        private InputSettings.BackgroundBehavior background;
        private InputSettings.EditorInputBehaviorInPlayMode editor;
        private readonly HashSet<MeleeAttackExecution> swings = new();
        private static PlayerItemActionComposition Actions => WeaponSystemTestFixtures.Actions;
        [UnitySetUp] public IEnumerator Prepare()
        {
            background = InputSystem.settings.backgroundBehavior; editor = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            mouse = InputSystem.AddDevice<Mouse>(); pad = InputSystem.AddDevice<Gamepad>(); keyboard = InputSystem.AddDevice<Keyboard>();
            yield return WeaponSystemTestFixtures.Load(); swings.Clear(); Actions.ActiveSample += execution => swings.Add(execution);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (mouse != null) InputSystem.RemoveDevice(mouse); if (pad != null) InputSystem.RemoveDevice(pad); if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.backgroundBehavior = background; InputSystem.settings.editorInputBehaviorInPlayMode = editor;
            yield return WeaponSystemTestFixtures.Unload();
        }
        private void MouseState(Vector2 position, bool down)
        { InputSystem.QueueStateEvent(mouse, new MouseState { position = position }.WithButton(MouseButton.Left, down)); InputSystem.Update(); }
        private Vector2 WorldPoint()
        {
            var system = EventSystem.current; var results = new List<RaycastResult>();
            foreach (float y in new[] { .5f, .65f, .8f }) foreach (float x in new[] { .5f, .7f, .3f })
            {
                var point = new Vector2(Screen.width * x, Screen.height * y);
                if (system == null) return point;
                results.Clear(); system.RaycastAll(new PointerEventData(system) { position = point }, results);
                if (results.Count == 0) return point;
            }
            Assert.Fail("Fixture has no clear gameplay pointer position."); return Vector2.zero;
        }
        private static ContainerSlotView VisibleSlot(int index) => Object.FindObjectsByType<ContainerSlotView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .Single(slot => slot.Index == index && slot.GetComponent<HotbarSlotDecorator>() != null);
        [UnityTest] public IEnumerator ActualPrimaryControlsDriveSelectedSpriteAndHotbarWithoutRepeatingOrConsuming()
        {
            var slot = VisibleSlot(0); var icon = WeaponSystemTestFixtures.Field<Image>(slot, "icon");
            var highlight = WeaponSystemTestFixtures.Field<GameObject>(slot.GetComponent<HotbarSlotDecorator>(), "activeHighlight");
            Assert.AreSame(WeaponSystemTestFixtures.Wrench.Icon, icon.sprite); Assert.IsTrue(icon.enabled); Assert.IsTrue(highlight.activeSelf);
            var point = WorldPoint(); MouseState(point, true); yield return null; yield return null;
            Assert.IsTrue(Actions.Runner.IsExecuting); Assert.IsTrue(Actions.GetComponent<MeleeSwingView>().HeldRenderer.enabled);
            yield return new WaitForSecondsRealtime(.7f); Assert.IsFalse(Actions.Runner.IsExecuting); Assert.AreEqual(1, swings.Count);
            InputSystem.Update(); yield return null; yield return null; Assert.AreEqual(1, swings.Count, "Held mouse must not repeat after recovery.");
            Assert.AreEqual(1, WeaponSystemTestFixtures.Bar.Container.GetSlot(0).Stack.Quantity);
            MouseState(point, false); InputSystem.QueueStateEvent(pad, new GamepadState { rightTrigger = 1 }); InputSystem.Update(); yield return null; yield return null;
            Assert.IsTrue(Actions.Runner.IsExecuting); yield return new WaitForSecondsRealtime(.7f); Assert.AreEqual(2, swings.Count);
            InputSystem.QueueStateEvent(pad, new GamepadState()); InputSystem.Update();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F)); InputSystem.Update();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update(); yield return null; yield return null;
            Assert.IsFalse(Actions.Runner.IsExecuting); Assert.AreEqual(1, WeaponSystemTestFixtures.Bar.Container.GetSlot(0).Stack.Quantity);
            WeaponSystemTestFixtures.Bar.SetActive(1); MouseState(point, true); yield return null; yield return null; Assert.IsFalse(Actions.Runner.IsExecuting);
            Assert.IsFalse(highlight.activeSelf); MouseState(point, false);
            var database = Resources.Load<SOLITUDE.Items.ItemDatabase>("Database");
            var capabilities = database.BuildCapabilityCatalog();
            var actionless = database.BuildRuntimeCatalog().Items.First(item =>
                capabilities.Resolve(item.ItemId).Resolve(SOLITUDE.Items.ItemActionSlot.Primary) == null);
            Assert.AreEqual(CommandStatus.Applied, WeaponSystemTestFixtures.Session.Commands.Grant(
                WeaponSystemTestFixtures.Bar.Container.Handle, actionless.ItemId, 2).Status);
            WeaponSystemTestFixtures.Bar.SetActive(1); MouseState(point, true); yield return null; yield return null;
            Assert.IsFalse(Actions.Runner.IsExecuting); Assert.AreEqual(2, WeaponSystemTestFixtures.Bar.Container.GetSlot(1).Stack.Quantity);
            MouseState(point, false);
        }
        [UnityTest] public IEnumerator ActualHudClickAndConsumedFrameCannotAlsoSwing()
        {
            Canvas.ForceUpdateCanvases(); var slot = VisibleSlot(0); var canvas = slot.GetComponentInParent<Canvas>();
            var point = RectTransformUtility.WorldToScreenPoint(canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, slot.transform.position);
            MouseState(point, true); yield return null; yield return null; Assert.IsFalse(Actions.Runner.IsExecuting); Assert.AreEqual(0, swings.Count);
            MouseState(point, false); yield return null; yield return null; Assert.IsFalse(Actions.Runner.IsExecuting);
            var world = WorldPoint(); MouseState(world, true); WeaponSystemTestFixtures.Input.ConsumeUIActivation();
            yield return null; yield return null; Assert.IsFalse(Actions.Runner.IsExecuting); Assert.AreEqual(0, swings.Count); MouseState(world, false);
        }
        [UnityTest] public IEnumerator ReplacementTransferAndSceneReleaseInterruptTheActualInputRoute()
        {
            var point = WorldPoint(); MouseState(point, true); yield return null; yield return null; Assert.IsTrue(Actions.Runner.IsExecuting);
            var commands = WeaponSystemTestFixtures.Session.Commands; var bar = WeaponSystemTestFixtures.Bar;
            commands.Restore(bar.Container.Handle, ContainerSaveSerializer.Capture(bar.Container));
            Assert.IsFalse(Actions.Runner.IsExecuting); yield return new WaitForSecondsRealtime(.7f); Assert.IsFalse(Actions.Runner.IsExecuting);
            MouseState(point, false); MouseState(point, true); yield return null; yield return null; Assert.IsTrue(Actions.Runner.IsExecuting);
            var inventory = WeaponSystemTestFixtures.One<PlayerInventory>().Container;
            var target = inventory.GetSlots().First(slot => slot.IsEmpty).Address;
            Assert.AreEqual(CommandStatus.Applied, commands.Transfer(bar.Container.GetSlot(0).Address, target).Status);
            Assert.IsFalse(Actions.Runner.IsExecuting); Assert.AreEqual(-1, bar.ActiveIndex);
            MouseState(point, false); MouseState(point, true); yield return null; yield return null; Assert.IsFalse(Actions.Runner.IsExecuting);
            MouseState(point, false); yield return new WaitForSecondsRealtime(.7f);
            commands.Transfer(inventory.GetSlot(target.Index).Address, bar.Container.GetSlot(0).Address); bar.SetActive(0);
            MouseState(point, true); yield return null; yield return null; Assert.IsTrue(Actions.Runner.IsExecuting);
            WeaponSystemTestFixtures.One<SceneBindings>().Release(); Assert.IsNull(Actions.Runner);
            yield return new WaitForSecondsRealtime(.7f); Assert.IsNull(Actions.Runner); Assert.IsFalse(Actions.Request());
        }
        [UnityTest] public IEnumerator CommittedSuccessionDiscardsDepartedLockoutButRejectedRetirementPreservesIt()
        {
            // Keep frame-driven input live without letting elapsed frames hide inherited lockout.
            Time.timeScale = 0;
            var feature = WeaponSystemTestFixtures.One<SurvivorSuccessionComposition>();
            var commands = WeaponSystemTestFixtures.Session.Commands;
            var bar = WeaponSystemTestFixtures.Bar;
            foreach (double elapsed in new[] { .05, .15, .30 })
            {
                var point = WorldPoint(); MouseState(point, false); MouseState(point, true);
                yield return null; yield return null;
                var departedRunner = Actions.Runner;
                Assert.IsTrue(departedRunner.IsExecuting);
                Actions.Advance(elapsed);
                Assert.That(departedRunner.LockoutRemaining, Is.EqualTo(.55 - elapsed).Within(.000001));
                var departed = feature.Coordinator.Current;

                // A rejection before commitment leaves the same survivor and its cooldown.
                var prefab = feature.RecoveryPrefab;
                feature.ConfigureRecovery(null);
                try { Assert.AreEqual(SuccessionOutcome.Failed, feature.RequestDeath(departed.Id)); }
                finally { feature.ConfigureRecovery(prefab); }
                Assert.AreSame(departed, feature.Coordinator.Current);
                Assert.AreSame(departedRunner, Actions.Runner);
                Assert.IsFalse(departedRunner.IsExecuting);
                Assert.That(departedRunner.LockoutRemaining, Is.EqualTo(.55 - elapsed).Within(.000001));
                Assert.IsFalse(Actions.Request());

                Assert.AreEqual(SuccessionOutcome.Completed, feature.RequestDeath(departed.Id));
                Assert.AreNotEqual(departed.Id, feature.Coordinator.Current.Id);
                Assert.AreNotSame(departedRunner, Actions.Runner);
                Assert.AreEqual(0, departedRunner.LockoutRemaining);
                Assert.AreEqual(0, Actions.Runner.LockoutRemaining);
                Assert.IsFalse(Actions.GetComponent<MeleeSwingView>().HeldRenderer.enabled);
                Assert.AreEqual(1, Actions.GetComponent<SOLITUDE.Movement.MovementBehavior>().SpeedFactor);
                var recovery = feature.RecoveryContainers.Single(cache => cache.SourceSurvivorId == departed.Id);
                var wrench = recovery.Container.GetSlots().Single(slot => slot.Stack?.ItemId == WeaponSystemTestFixtures.Wrench.ItemId);
                Assert.AreEqual(CommandStatus.Applied, commands.Transfer(wrench.Address, bar.Container.GetSlot(0).Address).Status);
                bar.SetActive(0);
                yield return null; yield return null;
                Assert.IsFalse(Actions.Runner.IsExecuting, "Held Primary must not replay into the successor.");
                Assert.IsTrue(WeaponSystemTestFixtures.Policy.Gameplay);
                MouseState(WorldPoint(), false); MouseState(WorldPoint(), true);
                yield return null; yield return null;
                Assert.IsTrue(Actions.Runner.IsExecuting, "The successor must accept fresh Primary without advancing the old cooldown.");
                Assert.That(Actions.Runner.LockoutRemaining, Is.EqualTo(.55).Within(.000001));
                var successorRunner = Actions.Runner;
                Assert.AreEqual(SuccessionOutcome.StaleSurvivor, feature.RequestDeath(departed.Id));
                Assert.AreSame(successorRunner, Actions.Runner);
                Assert.That(Actions.Runner.LockoutRemaining, Is.EqualTo(.55).Within(.000001));
                Actions.Advance(.55);
            }
            Time.timeScale = 1;
        }
        [UnityTest] public IEnumerator ReloadWithHeldMouseDoesNotReplayIntoNewSceneBinding()
        {
            var point = WorldPoint(); MouseState(point, true); yield return null; yield return null; Assert.IsTrue(Actions.Runner.IsExecuting);
            WeaponSystemTestFixtures.Root.StartNewSave(); yield return null; yield return null; yield return null; yield return null;
            var bar = WeaponSystemTestFixtures.Bar;
            WeaponSystemTestFixtures.Session.Commands.Grant(bar.Container.Handle, WeaponSystemTestFixtures.Wrench.ItemId, 1); bar.SetActive(0);
            yield return new WaitForSecondsRealtime(.7f); Assert.IsFalse(Actions.Runner.IsExecuting);
            MouseState(WorldPoint(), false); MouseState(WorldPoint(), true); yield return null; yield return null; Assert.IsTrue(Actions.Runner.IsExecuting);
        }
    }
}
#endif
