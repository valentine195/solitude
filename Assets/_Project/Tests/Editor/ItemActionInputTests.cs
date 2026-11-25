using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using SOLITUDE.Application;
using SOLITUDE.Containers;
using SOLITUDE.Core.Input;
using SOLITUDE.Items;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
namespace SOLITUDE.Tests
{
    public sealed class ItemActionInputTests
    {
        private sealed class Display : IContainerModalDisplay
        {
            public IModalBinding Prepare(ContainerModalKind kind, IContainerReader inventory, IContainerReader target) => new Binding();
            private sealed class Binding : IModalBinding { public void Activate() { } public void Dispose() { } }
        }
        [Test] public void PrimaryBindingsAndGeneratedWrapperPreserveUseInteractAndAssetIdentity()
        {
            const string path = "Assets/_Project/Core/Input/SOLITUDE_InputActions.inputactions";
            Assert.AreEqual("a961543dcc1d545b38bf989cef0494a5", AssetDatabase.AssetPathToGUID(path));
            var authored = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
            var generated = new SOLITUDE_InputActions();
            try
            {
            foreach (var action in authored)
                Assert.AreEqual(action.id, generated.asset.FindAction(action.actionMap.name + "/" + action.name, true).id);
            Assert.AreEqual(InputActionType.Button, generated.Gameplay.Primary.type);
            CollectionAssert.AreEquivalent(new[] { "<Mouse>/leftButton", "<Gamepad>/rightTrigger" }, generated.Gameplay.Primary.bindings.Select(b => b.path).ToArray());
            CollectionAssert.AreEquivalent(new[] { "<Keyboard>/f", "<Gamepad>/buttonWest" }, generated.Gameplay.Use.bindings.Select(b => b.path).ToArray());
            CollectionAssert.AreEquivalent(new[] { "<Keyboard>/e", "<Gamepad>/buttonEast" }, generated.Gameplay.Interact.bindings.Select(b => b.path).ToArray());
            Assert.AreEqual("063096fe-f44b-40f7-acad-238af293440a", generated.Gameplay.Use.id.ToString());
            Assert.AreEqual("5649f9bd-c4dc-43e1-a2d9-e0218bfc1075", generated.Gameplay.Interact.id.ToString());
            }
            finally { Object.DestroyImmediate(generated.asset); }
        }
        [UnityTest] public IEnumerator RealPrimaryControlsRespectFreshnessPolicyOwnershipAndUIConsumption()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            yield return new EnterPlayMode(); yield return Verify(); yield return new ExitPlayMode();
        }
        private static IEnumerator Verify()
        {
            var pause = new PauseCoordinator(); var policy = new InputPolicyCoordinator(pause);
            var commands = new ContainerCommandService(new ItemCatalog(new[] { new ItemSpec("fixture", true, 10) }));
            var hotbar = commands.Register("primary-input", 1); var gestures = new ContainerGestureCoordinator(new ContainerInteractionController(commands), policy);
            var modal = new ContainerModalCoordinator(policy, pause, gestures, new Display(), hotbar);
            var host = new GameObject("Primary input fixture"); var input = host.AddComponent<UnityInputHost>(); input.Initialize(policy);
            var background = InputSystem.settings.backgroundBehavior; var editor = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var mouse = InputSystem.AddDevice<Mouse>(); var pad = InputSystem.AddDevice<Gamepad>(); var keyboard = InputSystem.AddDevice<Keyboard>();
            int count = 0, uses = 0; IDisposable primary = input.BindPrimaryAction(() => count++); var vital = input.BindVitalUse(() => uses++);
            void SetMouse(bool pressed) { InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left, pressed)); InputSystem.Update(); }
            void PadState(bool pressed) { InputSystem.QueueStateEvent(pad, new GamepadState { rightTrigger = pressed ? 1 : 0 }); InputSystem.Update(); }
            try
            {
                Assert.Throws<InvalidOperationException>(() => input.BindPrimaryAction(() => { })); policy.SetReady(true);
                int pressedFrame = Time.frameCount; SetMouse(true); Assert.AreEqual(0, count, "Primary waits until UI has processed the frame."); yield return null; yield return null; Assert.AreEqual(1, count, "Queued at frame " + pressedFrame + ", resumed at " + Time.frameCount);
                InputSystem.Update(); yield return null; yield return null; Assert.AreEqual(1, count, "Held mouse cannot repeat.");
                SetMouse(false); SetMouse(true); yield return null; yield return null; Assert.AreEqual(2, count); SetMouse(false);
                PadState(true); yield return null; yield return null; Assert.AreEqual(3, count); InputSystem.Update(); yield return null; yield return null; Assert.AreEqual(3, count); PadState(false);
                Assert.AreEqual(0, uses, "Primary never routes to consumables.");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F)); InputSystem.Update(); Assert.AreEqual(1, uses);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
                foreach (var gate in new[] { "ready", "pause", "modal", "primary", "transition" })
                {
                    int before = count; IDisposable lease = null;
                    if (gate == "ready") policy.SetReady(false);
                    if (gate == "pause") lease = pause.Acquire();
                    if (gate == "modal") modal.ToggleInventory();
                    if (gate == "primary") lease = policy.AcquirePrimaryScreen();
                    if (gate == "transition") policy.SetTransition(true);
                    SetMouse(true); yield return null; yield return null; Assert.AreEqual(before, count, gate);
                    lease?.Dispose(); modal.Close(); policy.SetReady(true); policy.SetTransition(false);
                    yield return null; yield return null; InputSystem.Update(); Assert.AreEqual(before, count, "Held after " + gate);
                    SetMouse(false); SetMouse(true); yield return null; yield return null; Assert.AreEqual(before + 1, count, "Fresh after " + gate); SetMouse(false);
                }
                int current = count; SetMouse(true); input.ConsumeUIActivation(); yield return null; yield return null; Assert.AreEqual(current, count); SetMouse(false);
                SetMouse(true); input.ResetSurvivorInput(); yield return null; yield return null; Assert.AreEqual(current, count);
                SetMouse(false); SetMouse(true); yield return null; yield return null; Assert.AreEqual(++current, count);
                primary.Dispose(); primary = input.BindPrimaryAction(() => count++); yield return null; yield return null; Assert.AreEqual(current, count, "Rebind cannot replay held mouse.");
                SetMouse(false); SetMouse(true); yield return null; yield return null; Assert.AreEqual(++current, count);
                SetMouse(false); SetMouse(true); input.enabled = false; input.enabled = true;
                yield return null; yield return null; Assert.AreEqual(current, count, "Disable/re-enable cancels pending and held input.");
                SetMouse(false); SetMouse(true); yield return null; yield return null; Assert.AreEqual(++current, count); SetMouse(false);
                PadState(true); input.ResetSurvivorInput(); yield return null; yield return null; Assert.AreEqual(current, count);
                PadState(false); PadState(true); yield return null; yield return null; Assert.AreEqual(++current, count);
                primary.Dispose(); input.Release(); input.Initialize(policy); primary = input.BindPrimaryAction(() => count++);
                yield return null; yield return null; Assert.AreEqual(current, count, "Whole input-host reinitialization cannot replay a held trigger.");
                PadState(false); PadState(true); yield return null; yield return null; Assert.AreEqual(current + 1, count); PadState(false);
                primary.Dispose(); SetMouse(true); yield return null; yield return null; Assert.AreEqual(current + 1, count);
            }
            finally
            {
                primary.Dispose(); vital.Dispose(); input.Release(); modal.Dispose(); policy.Dispose();
                InputSystem.RemoveDevice(mouse); InputSystem.RemoveDevice(pad); InputSystem.RemoveDevice(keyboard);
                InputSystem.settings.backgroundBehavior = background; InputSystem.settings.editorInputBehaviorInPlayMode = editor;
                Object.Destroy(host);
            }
            yield return null; yield return null;
        }
    }
}
