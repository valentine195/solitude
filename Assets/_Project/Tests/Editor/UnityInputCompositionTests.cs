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
            var session = new ContainerSaveSession(new SaveGameData(), new ItemCatalog(new[] { new ItemSpec("a", true, 10) }));
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
                using (pause.Acquire()) { Assert.IsFalse(module.enabled); Press(keyboard, Key.Digit2); Assert.AreEqual(1, changes); }
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
