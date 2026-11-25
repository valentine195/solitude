#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SOLITUDE.Composition;
using SOLITUDE.Containers;
using SOLITUDE.Core.Interaction;
using SOLITUDE.Core.UI;
using SOLITUDE.Items;
using SOLITUDE.Player;
using SOLITUDE.SaveLoad;
using SOLITUDE.World.Restoration;
using SOLITUDE.World.SolitudeStart;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
namespace SOLITUDE.Tests
{
    public class MechanicalRepairFixtureTests
    {
        public const string ScenePath = "Assets/_Project/Tests/Fixtures/MechanicalRepair.unity";
        private Keyboard keyboard; private Gamepad pad;
        private InputSettings.BackgroundBehavior background; private InputSettings.EditorInputBehaviorInPlayMode editor;
        private PlayerInteractor player;
        private string output;
        private static T One<T>() where T : Object => WeaponSystemTestFixtures.One<T>();
        private static T Field<T>(object o, string n) => WeaponSystemTestFixtures.Field<T>(o, n);
        private static ContainerSaveSession Session => WeaponSystemTestFixtures.Session;
        private static string Feedback => Field<TextMeshProUGUI>(One<InteractionFeedbackView>(), "label").text;
        [UnitySetUp] public IEnumerator Prepare()
        {
            var args = Environment.GetCommandLineArgs(); int i = Array.IndexOf(args, "-solitudeSaveDirectory");
            Assert.GreaterOrEqual(i, 0); output = Path.GetFullPath(args[i+1]);
            Assert.IsTrue(output.Contains("step3-disposable-saves"), "Requires uniquely named disposable fixture save directory.");
            Assert.IsFalse(File.Exists(Path.Combine(output, "solitude-save.json")), "Use fresh fixture journey saves.");
            background = InputSystem.settings.backgroundBehavior; editor = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard = InputSystem.AddDevice<Keyboard>(); pad = InputSystem.AddDevice<Gamepad>();
            foreach (var root in Object.FindObjectsByType<GameCompositionRoot>(FindObjectsSortMode.None)) Object.Destroy(root.gameObject);
            yield return null; Time.timeScale = 1;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single)); yield return null;
            Assert.AreEqual(RuntimeStartupState.Ready, WeaponSystemTestFixtures.Root.State);
            Assert.IsFalse(WeaponSystemTestFixtures.Root.UsesDisposableSession);
            Assert.IsNull(Object.FindFirstObjectByType<RestorationDemoSession>());
            player = One<PlayerInteractor>();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (keyboard != null) InputSystem.RemoveDevice(keyboard); if (pad != null) InputSystem.RemoveDevice(pad);
            InputSystem.settings.backgroundBehavior = background; InputSystem.settings.editorInputBehaviorInPlayMode = editor;
            yield return WeaponSystemTestFixtures.Unload();
        }
        private IEnumerator Focus(Component target, float below = .6f)
        {
            var body = player.GetComponent<Rigidbody2D>(); body.position = (Vector2)target.transform.position + Vector2.down * below;
            Physics2D.SyncTransforms(); yield return new WaitForFixedUpdate(); yield return null; yield return null;
            Assert.AreSame(target, Field<IInteractable>(player, "currentTarget"), target.name);
        }
        private IEnumerator Interact()
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E)); InputSystem.Update(); yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update(); yield return null;
        }
        private IEnumerator Capture(string name)
        {
            // Diagnose the original yield without letting a batch runner wait forever.
            bool reachedEndOfFrame = false;
            var probe = player.StartCoroutine(EndOfFrameProbe(() => reachedEndOfFrame = true));
            for (int frame = 0; frame < 3 && !reachedEndOfFrame; frame++) yield return null;
            Debug.Log($"Mechanical capture {name}: batch={UnityEngine.Application.isBatchMode}, end-of-frame resumed={reachedEndOfFrame} after three player-loop frames.");
            if (!reachedEndOfFrame) player.StopCoroutine(probe);
            Assert.IsTrue(reachedEndOfFrame || UnityEngine.Application.isBatchMode, "Unexpected non-batch capture stall.");
            // Explicitly render the actual scene and HUD in batch mode, which has no end-of-frame yield.
            var camera = Camera.main; Assert.IsNotNull(camera);
            var previousTarget = camera.targetTexture; var previousActive = RenderTexture.active;
            var target = RenderTexture.GetTemporary(1280, 720, 24);
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                .Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
            var previousCameras = canvases.Select(c => c.worldCamera).ToArray();
            var previousDistances = canvases.Select(c => c.planeDistance).ToArray();
            Texture2D image = null;
            try
            {
                camera.targetTexture = target;
                foreach (var canvas in canvases) { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1; }
                Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
                image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); image.Apply();
                File.WriteAllBytes(Path.Combine(output, name + ".png"), image.EncodeToPNG());
            }
            finally
            {
                for (int i = 0; i < canvases.Length; i++) { canvases[i].renderMode = RenderMode.ScreenSpaceOverlay; canvases[i].worldCamera = previousCameras[i]; canvases[i].planeDistance = previousDistances[i]; }
                camera.targetTexture = previousTarget; RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(target); if (image != null) Object.Destroy(image);
            }
        }
        private static IEnumerator EndOfFrameProbe(Action completed) { yield return new WaitForEndOfFrame(); completed(); }
        [UnityTest] public IEnumerator NormalInputCollectsBatteryRepairsWithUnselectedStartingWrenchAndOpensDoorSeparately()
        {
            var junction = Object.FindObjectsByType<RestorationJunction>(FindObjectsSortMode.None).Single(j => j.name == "Auxiliary power socket");
            var system = Field<RestorableSystem>(junction, "system"); var door = One<SolitudeSlidingDoor>(); var gate = One<RestorationDoorGate>();
            var inventory = One<PlayerInventory>().Container; var bar = WeaponSystemTestFixtures.Bar;
            Assert.AreEqual(WeaponSystemTestFixtures.Wrench.ItemId, bar.Container.GetSlot(0).Stack.ItemId);
            int samples = 0; WeaponSystemTestFixtures.Actions.ActiveSample += _ => samples++;
            bar.SetActive(1); yield return Focus(junction); yield return Interact();
            StringAssert.Contains("in inventory", Feedback); Assert.IsFalse(system.IsRestored); Assert.IsFalse(gate.AllowsAccess);
            yield return Capture("offline");
            var battery = Object.FindObjectsByType<WorldPickup>(FindObjectsSortMode.None).First(p => p.Definition.Item.DisplayName == "Battery");
            var partId = battery.Definition.Item.ItemId;
            yield return Focus(battery); yield return Interact();
            Assert.AreEqual(1, inventory.GetSlots().Where(s => s.Stack?.ItemId == partId).Sum(s => s.Stack.Quantity));
            yield return Focus(junction); yield return Interact();
            Assert.IsTrue(system.IsRestored); Assert.IsTrue(gate.AllowsAccess); Assert.IsFalse(door.IsOpen);
            Assert.AreEqual(0, inventory.GetSlots().Where(s => s.Stack?.ItemId == partId).Sum(s => s.Stack.Quantity));
            Assert.AreEqual(1, bar.ActiveIndex); Assert.AreEqual(1, bar.Container.GetSlot(0).Stack.Quantity); Assert.AreEqual(0, samples);
            yield return Capture("restored-door-closed");
            yield return Focus(door); yield return Interact(); yield return new WaitForSeconds(1f); Assert.IsTrue(door.IsOpen);
            bar.SetActive(0); yield return null;
            InputSystem.QueueStateEvent(pad, new GamepadState { rightTrigger = 1 }); InputSystem.Update(); yield return null; yield return null;
            Assert.IsTrue(WeaponSystemTestFixtures.Actions.Runner.IsExecuting); yield return new WaitForSecondsRealtime(.7f); Assert.Greater(samples, 0);
            Assert.IsTrue(One<SaveGameService>().TryFlush());
            yield return Capture("door-open");
            File.WriteAllText(Path.Combine(output, "journey.txt"), "Production session ready; real starting wrench retained unselected; no Battery rejection; pickup via E; repair via E cost 1 inventory Battery without melee; door initially closed then opened via E; Primary trigger swings separately; flush succeeded.\n");
        }
    }
}
#endif
