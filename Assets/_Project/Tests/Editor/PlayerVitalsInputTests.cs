using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SOLITUDE.Application;
using SOLITUDE.Composition;
using SOLITUDE.Containers;
using SOLITUDE.Core.Input;
using SOLITUDE.Items;
using SOLITUDE.Player;
using SOLITUDE.Vitals;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SOLITUDE.Tests
{
    public class PlayerVitalsInputTests
    {
        private sealed class Display : IContainerModalDisplay
        {
            public IModalBinding Prepare(ContainerModalKind kind, IContainerReader inventory, IContainerReader target) => new Binding();
            private sealed class Binding : IModalBinding { public void Activate() { } public void Dispose() { } }
        }
        private static void KeyState(Keyboard device, params Key[] keys)
        { InputSystem.QueueStateEvent(device, new KeyboardState(keys)); InputSystem.Update(); }
        private static void PadState(Gamepad device, bool pressed)
        { InputSystem.QueueStateEvent(device, pressed ? new GamepadState().WithButton(GamepadButton.West) : new GamepadState()); InputSystem.Update(); }
        private static void Press(Keyboard device, Key key) { KeyState(device, key); KeyState(device); }
        private static ItemDefinition Definition(string id, VitalConsumableKind kind)
        {
            var item = ScriptableObject.CreateInstance<ItemDefinition>(); item.ConsumableKind = kind; item.RestorationAmount = 25;
            typeof(ItemDefinition).GetField("itemId", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(item, id); return item;
        }
        [UnityTest] public IEnumerator RealUseInputRequiresFreshPressAcrossPolicyAwakeningAndLifetimeChanges()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            yield return new EnterPlayMode();
            yield return Verify();
            yield return new ExitPlayMode();
        }
        private static IEnumerator Verify()
        {
            var pause = new PauseCoordinator(); var policy = new InputPolicyCoordinator(pause);
            var food = Definition("input-food", VitalConsumableKind.Food); var medical = Definition("input-medical", VitalConsumableKind.Medical);
            var tool = Definition("input-tool", VitalConsumableKind.None); var database = ScriptableObject.CreateInstance<ItemDatabase>();
            typeof(ItemDatabase).GetField("items", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(database, new List<ItemDefinition> { food, medical, tool });
            var commands = new ContainerCommandService(database.BuildRuntimeCatalog()); var hotbar = commands.Register("input-hotbar", 3);
            commands.Grant(hotbar.Handle, food.ItemId, 40); commands.Grant(hotbar.Handle, medical.ItemId, 20); commands.Grant(hotbar.Handle, tool.ItemId, 1);
            var selection = new HotbarSelectionState(hotbar);
            var gestures = new ContainerGestureCoordinator(new ContainerInteractionController(commands), policy);
            var modal = new ContainerModalCoordinator(policy, pause, gestures, new Display(), hotbar);
            var host = new GameObject("VitalsInputFixture"); var input = host.AddComponent<UnityInputHost>(); input.Initialize(policy);
            var player = host.AddComponent<PlayerVitalsController>(); var feature = host.AddComponent<PlayerVitalsComposition>(); feature.Configure(player);
            input.Bind(null, null, selection, null, null, null);
            var context = new SceneFeatureContext(commands, null, database.BuildPresentationCatalog(), policy, input, null, "input-vitals", null, null, hotbar, selection);
            feature.Initialize(context); policy.SetReady(true);
            player.Configure(new VitalsRecoverySettings(hungerDepletionRate: 0, recoveryRate: 0));
            var background = InputSystem.settings.backgroundBehavior; var editor = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard = InputSystem.AddDevice<Keyboard>(); var gamepad = InputSystem.AddDevice<Gamepad>();
            int Quantity(int index) => hotbar.GetSlot(index).Stack.Quantity;
            void Hungry() { player.Vitals.Health.Set(30); player.Vitals.Hunger.Set(0); selection.Select(0); }
            try
            {
                var actions = (SOLITUDE_InputActions)typeof(UnityInputHost).GetField("actions", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(input);
                Assert.AreEqual(InputActionType.Button, actions.Gameplay.Use.type);
                CollectionAssert.AreEquivalent(new[] { "<Keyboard>/f", "<Gamepad>/buttonWest" }, Array.ConvertAll(actions.Gameplay.Use.bindings.ToArray(), b => b.path));
                Hungry(); int count = Quantity(0); KeyState(keyboard, Key.F);
                Assert.AreEqual(count - 1, Quantity(0)); Assert.AreEqual(25, player.Vitals.Hunger.Current);
                yield return null; player.Vitals.Hunger.Set(0); InputSystem.Update();
                Assert.AreEqual(count - 1, Quantity(0), "Holding across frames does not repeat.");
                KeyState(keyboard); Press(keyboard, Key.F); Assert.AreEqual(count - 2, Quantity(0));
                Hungry(); count = Quantity(0); PadState(gamepad, true); Assert.AreEqual(count - 1, Quantity(0));
                yield return null; player.Vitals.Hunger.Set(0); InputSystem.Update(); Assert.AreEqual(count - 1, Quantity(0)); PadState(gamepad, false);

                foreach (var gate in new[] { "ready", "pause", "modal", "transition", "primary" })
                {
                    Hungry(); count = Quantity(0); IDisposable lease = null;
                    if (gate == "ready") policy.SetReady(false);
                    if (gate == "pause") lease = pause.Acquire();
                    if (gate == "modal") modal.ToggleInventory();
                    if (gate == "transition") policy.SetTransition(true);
                    if (gate == "primary") lease = policy.AcquirePrimaryScreen();
                    if (gate == "primary") Assert.AreEqual(1, pause.Scale, "Primary gate must work at nonzero time scale.");
                    KeyState(keyboard, Key.F); Assert.AreEqual(count, Quantity(0), gate);
                    lease?.Dispose(); policy.SetReady(true); modal.Close(); policy.SetTransition(false);
                    yield return null; InputSystem.Update(); Assert.AreEqual(count, Quantity(0), "Held input must not replay after " + gate);
                    KeyState(keyboard); Press(keyboard, Key.F); Assert.AreEqual(count - 1, Quantity(0), "Fresh use after " + gate);
                }

                Hungry(); count = Quantity(0); input.ConsumeUIActivation(); Press(keyboard, Key.F);
                Assert.AreEqual(count, Quantity(0), "A close-activation frame cannot use an item.");
                yield return null; Press(keyboard, Key.F); Assert.AreEqual(count - 1, Quantity(0));

                Hungry(); count = Quantity(0); input.enabled = false; KeyState(keyboard, Key.F); input.enabled = true;
                yield return null; InputSystem.Update(); Assert.AreEqual(count, Quantity(0), "Enable cannot replay held Use.");
                KeyState(keyboard); Press(keyboard, Key.F); Assert.AreEqual(count - 1, Quantity(0));

                Hungry(); KeyState(keyboard, Key.F); count = Quantity(0); input.ResetSurvivorInput(); player.ResetForAwakening();
                player.Configure(new VitalsRecoverySettings(hungerDepletionRate: 0, recoveryRate: 0)); Hungry();
                yield return null; InputSystem.Update(); Assert.AreEqual(count, Quantity(0), "Awakening cannot replay held Use.");
                KeyState(keyboard); Press(keyboard, Key.F); Assert.AreEqual(count - 1, Quantity(0));

                Hungry(); PadState(gamepad, true); count = Quantity(0); input.ResetSurvivorInput(); player.ResetForAwakening();
                player.Configure(new VitalsRecoverySettings(hungerDepletionRate: 0, recoveryRate: 0)); Hungry();
                yield return null; InputSystem.Update(); Assert.AreEqual(count, Quantity(0), "Awakening cannot replay held gamepad Use.");
                PadState(gamepad, false); PadState(gamepad, true); Assert.AreEqual(count - 1, Quantity(0)); PadState(gamepad, false);

                Hungry(); count = Quantity(0); feature.Initialize(context); feature.Initialize(context);
                Press(keyboard, Key.F); Assert.AreEqual(count - 1, Quantity(0), "Rebinding must leave one route.");
                feature.Release(); Hungry(); count = Quantity(0); Press(keyboard, Key.F); Assert.AreEqual(count, Quantity(0));
                feature.Initialize(context); Press(keyboard, Key.F); Assert.AreEqual(count - 1, Quantity(0));
                feature.enabled = false; Hungry(); count = Quantity(0); Press(keyboard, Key.F); Assert.AreEqual(count, Quantity(0)); feature.enabled = true;

                selection.Select(-1); count = Quantity(0); Press(keyboard, Key.F); Assert.AreEqual(count, Quantity(0)); Assert.AreEqual(-1, selection.ActiveIndex);
                selection.Select(2); Press(keyboard, Key.F); Assert.AreEqual(1, Quantity(2)); Assert.AreEqual(2, selection.ActiveIndex);
                player.Vitals.Hunger.Set(100); selection.Select(0); Press(keyboard, Key.F); Assert.AreEqual(count, Quantity(0)); Assert.AreEqual(0, selection.ActiveIndex);
                player.Vitals.Health.Set(90); player.Vitals.Hunger.Set(0); Press(keyboard, Key.Digit2); Assert.AreEqual(1, selection.ActiveIndex);
                int medicines = Quantity(1); Press(keyboard, Key.F); Assert.AreEqual(100, player.Vitals.Health.Current); Assert.AreEqual(0, player.Vitals.Hunger.Current); Assert.AreEqual(medicines - 1, Quantity(1));
                Assert.AreEqual("E", input.BindingLabel("Gameplay/Interact")); Assert.AreEqual("TAB", input.BindingLabel("Gameplay/Inventory"));
                Assert.IsTrue(actions.Gameplay.Move.enabled); Assert.IsTrue(actions.Gameplay.Pause.enabled);

                input.Release(); feature.Release(); Hungry(); count = Quantity(0); Press(keyboard, Key.F); PadState(gamepad, true); Assert.AreEqual(count, Quantity(0));
                PadState(gamepad, false); KeyState(keyboard);
            }
            finally
            {
                feature.Release(); input.Release(); modal.Dispose(); selection.Dispose(); policy.Dispose();
                InputSystem.RemoveDevice(keyboard); InputSystem.RemoveDevice(gamepad);
                InputSystem.settings.backgroundBehavior = background; InputSystem.settings.editorInputBehaviorInPlayMode = editor;
                Object.Destroy(host); Object.Destroy(food); Object.Destroy(medical); Object.Destroy(tool); Object.Destroy(database);
            }
            yield return null;
        }
    }
}
