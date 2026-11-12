using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using SOLITUDE.Application;
using SOLITUDE.Containers;
using SOLITUDE.Core.Input;
using SOLITUDE.Items;
using SOLITUDE.SaveLoad;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;
namespace SOLITUDE.Tests
{
    public class UnityInputCompositionTests
    {
        private sealed class Display : IContainerModalDisplay
        { public IModalBinding Prepare(ContainerModalKind kind, IContainerReader inventory, IContainerReader target) => new Binding(); }
        private sealed class ScreenDisplay : IScrubberScreenDisplay { public void Show() { } public void Hide() { } }
        private sealed class Binding : IModalBinding { public void Activate() { } public void Dispose() { } }
        private static void Press(Keyboard keyboard, Key key)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); InputSystem.Update();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
        }
        [UnityTest] public IEnumerator RealKeyboardAndEventSystemSharePolicyAndSurviveTeardownOrder()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            yield return new EnterPlayMode();
            yield return VerifyInput();
            yield return new ExitPlayMode();
        }
        private static IEnumerator VerifyInput()
        {
            var pause = new PauseCoordinator(); var policy = new InputPolicyCoordinator(pause);
            var session = new ContainerSaveSession(new SaveGameData { startingLoadoutInitialized = true }, new ItemCatalog(new[] { new ItemSpec("a", true, 10) }));
            var reader = session.Register("inventory", 3).Container; session.Commands.Grant(reader.Handle, "a", 2);
            var gestures = new ContainerGestureCoordinator(new ContainerInteractionController(session.Commands), policy);
            var modal = new ContainerModalCoordinator(policy, pause, gestures, new Display(), reader);
            var selection = new HotbarSelectionState(reader);
            var inputObject = new GameObject("SingleInputHost"); var input = inputObject.AddComponent<UnityInputHost>(); input.Initialize(policy);
            var uiObject = new GameObject("EventSystem", typeof(EventSystem)); var module = uiObject.AddComponent<InputSystemUIInputModule>();
            var uiLease = input.ConnectUI(module);
            int changes = 0, fallbacks = 0; selection.Changed += _ => changes++;
            input.Bind(null, null, selection, () => modal.ToggleInventory(), () => modal.Back(), () => fallbacks++);
            var previousBackground = InputSystem.settings.backgroundBehavior;
            var previousEditor = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                policy.SetReady(true); Assert.IsTrue(module.enabled);
                var actions = (SOLITUDE_InputActions)typeof(UnityInputHost).GetField("actions", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(input);
                Assert.AreSame(actions.asset, module.actionsAsset);
                Press(keyboard, Key.Digit1); Assert.AreEqual(1, changes);
                Press(keyboard, Key.Tab); Assert.AreEqual(ContainerModalKind.Inventory, modal.Kind); Assert.IsFalse(policy.Gameplay); Assert.IsTrue(module.enabled);
                Press(keyboard, Key.Digit2); Assert.AreEqual(1, changes);
                gestures.Begin("inventory", reader.GetSlot(0)); Press(keyboard, Key.Escape);
                Assert.IsFalse(gestures.Interaction.IsHolding); Assert.AreEqual(ContainerModalKind.Inventory, modal.Kind); Assert.AreEqual(0, fallbacks);
                Press(keyboard, Key.Escape); Assert.AreEqual(ContainerModalKind.Closed, modal.Kind); Assert.AreEqual(0, fallbacks);
                Press(keyboard, Key.Escape); Assert.AreEqual(1, fallbacks);
                using (var screen = new ScrubberScreenCoordinator(policy, new ScreenDisplay()))
                {
                    input.Bind(null, null, selection, () => modal.ToggleInventory(), () =>
                    { if (!screen.IsOpen) return modal.Back(); input.ConsumeUIActivation(); return screen.Back(); }, () => fallbacks++);
                    Assert.AreEqual(OpenResult.Opened, screen.Open()); Assert.AreEqual(1, pause.Scale); Assert.IsTrue(module.enabled);
                    yield return null;
                    Assert.IsTrue(actions.asset.FindAction("UI/Navigate").enabled, "UI navigation must use the composed asset."); Assert.IsTrue(actions.asset.FindAction("UI/Submit").enabled);
                    Press(keyboard, Key.Tab); Assert.AreEqual(ContainerModalKind.Closed, modal.Kind);
                    Assert.AreEqual(OpenResult.Unavailable, modal.ToggleInventory());
                    Press(keyboard, Key.Digit2); Assert.AreEqual(1, changes);
                    Press(keyboard, Key.Escape); Assert.IsFalse(screen.IsOpen); Assert.AreEqual(1, fallbacks);
                    Press(keyboard, Key.Digit2); Assert.AreEqual(1, changes, "Close activation is consumed for this frame.");
                    yield return null;
                    Press(keyboard, Key.Digit2); Assert.AreEqual(2, changes);
                    Assert.AreEqual(OpenResult.Opened, modal.ToggleInventory());
                    Assert.AreEqual(OpenResult.Unavailable, screen.Open()); modal.Close();
                }
                using (pause.Acquire()) { Assert.IsFalse(module.enabled); Press(keyboard, Key.Digit1); Assert.AreEqual(2, changes); }
                policy.SetTransition(true); Press(keyboard, Key.Tab); Assert.AreEqual(ContainerModalKind.Closed, modal.Kind);
                input.Release(); Assert.DoesNotThrow(() => uiLease.Dispose());
            }
            finally
            {
                input.Release(); uiLease.Dispose(); modal.Dispose(); selection.Dispose(); policy.Dispose();
                InputSystem.RemoveDevice(keyboard);
                InputSystem.settings.backgroundBehavior = previousBackground;
                InputSystem.settings.editorInputBehaviorInPlayMode = previousEditor;
                UnityEngine.Object.Destroy(uiObject); UnityEngine.Object.Destroy(inputObject);
            }
            yield return null;
        }
    }
}
