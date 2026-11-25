#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SOLITUDE.Application;
using SOLITUDE.Composition;
using SOLITUDE.Items;
using SOLITUDE.Player;
using SOLITUDE.SaveLoad;
using SOLITUDE.World.CrewBackgrounds;
using SOLITUDE.World.SolitudeStart;
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
    public sealed class CrewBackgroundsFinalAcceptanceTests
    {
        private static GameCompositionRoot Root => Object.FindAnyObjectByType<GameCompositionRoot>();
        private static CrewBackgroundsPlaytestComposition Crew => Object.FindAnyObjectByType<CrewBackgroundsPlaytestComposition>();
        private static PlayerInteractor Player => Object.FindAnyObjectByType<PlayerInteractor>();
        private static Rigidbody2D Body => Player.GetComponent<Rigidbody2D>();
        private Gamepad pad;
        private InputSettings.BackgroundBehavior oldBackground;
        private InputSettings.EditorInputBehaviorInPlayMode oldEditor;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            oldBackground = InputSystem.settings.backgroundBehavior;
            oldEditor = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            pad = InputSystem.AddDevice<Gamepad>();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator CleanUp()
        {
            if (pad != null) InputSystem.RemoveDevice(pad);
            InputSystem.settings.backgroundBehavior = oldBackground;
            InputSystem.settings.editorInputBehaviorInPlayMode = oldEditor;
            if (Root != null) Object.Destroy(Root.gameObject);
            yield return null;
            Time.timeScale = 1;
        }

        [UnityTest]
        public IEnumerator GamepadEngineerPower() => RunGamepad(CrewBackground.Engineer, CrewBackgroundsFault.LossOfPower);
        [UnityTest]
        public IEnumerator GamepadTechnicianPower() => RunGamepad(CrewBackground.Technician, CrewBackgroundsFault.LossOfPower);
        [UnityTest]
        public IEnumerator GamepadEngineerLinkage() => RunGamepad(CrewBackground.Engineer, CrewBackgroundsFault.BrokenIntakeLinkage);
        [UnityTest]
        public IEnumerator GamepadTechnicianLinkage() => RunGamepad(CrewBackground.Technician, CrewBackgroundsFault.BrokenIntakeLinkage);

        [UnityTest]
        public IEnumerator AllFourCrewSessionsPreserveAnIsolatedProductionSave()
        {
            var args = Environment.GetCommandLineArgs();
            int argument = Array.IndexOf(args, "-solitudeSaveDirectory");
            Assert.That(argument, Is.GreaterThanOrEqualTo(0), "The acceptance run requires an isolated save directory.");
            string path = Path.Combine(args[argument + 1], "solitude-save.json");

            var database = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/_Project/Features/Items/Resources/Database.asset");
            var codec = new SaveJsonCodec(database.BuildRuntimeCatalog());
            var existing = new SaveGameData { worldSeed = 91823, startingLoadoutInitialized = true };
            existing.collectedPickupIds.Add("isolated-previous-expedition-pickup");
            string encoded = codec.Encode(existing);
            Assert.AreEqual(existing.worldSeed, codec.Decode(encoded).worldSeed);
            File.WriteAllText(path, encoded);
            byte[] before = File.ReadAllBytes(path);
            foreach (CrewBackground background in Enum.GetValues(typeof(CrewBackground)))
            foreach (CrewBackgroundsFault fault in Enum.GetValues(typeof(CrewBackgroundsFault)))
            {
                yield return Launch(background, fault);
                var pickup = Object.FindObjectsByType<WorldPickup>().First();
                Assert.IsTrue(pickup.Interact(Player).IsSuccess);
                Assert.IsTrue(Field<SaveGameService>(Root, "persistence").TryFlush());
                Object.Destroy(Root.gameObject);
                yield return null;
                CollectionAssert.AreEqual(before, File.ReadAllBytes(path), background + "/" + fault);
            }
        }

        private IEnumerator RunGamepad(CrewBackground background, CrewBackgroundsFault fault)
        {
            yield return Launch(background, fault);
            bool bypass = background == CrewBackground.Engineer && fault == CrewBackgroundsFault.LossOfPower;
            bool rig = background == CrewBackground.Technician && fault == CrewBackgroundsFault.BrokenIntakeLinkage;
            var controlName = bypass ? "Service Panel" : fault == CrewBackgroundsFault.LossOfPower ? "Main Breaker" : "AtmosphericScrubber";
            string pickupName = bypass ? "Reusable jumper" : rig ? "Linkage salvage" :
                fault == CrewBackgroundsFault.BrokenIntakeLinkage ? "Intake linkage" : null;
            if (pickupName != null)
            {
                var pickup = Object.FindObjectsByType<WorldPickup>().Single(p => p.Definition.Item.DisplayName == pickupName);
                yield return Position((Vector2)pickup.transform.position + Vector2.down * .4f);
                TapInteract();
                Assert.IsFalse(pickup.gameObject.activeSelf, "Gamepad Interact must collect the authored item.");
            }

            var control = Object.FindObjectsByType<CrewBackgroundsRepairControl>().Single(c => c.name == controlName);
            Transform access = rig ? control.transform.Find("Rig Bracket") : control.transform;
            yield return Position((Vector2)access.position + Vector2.down * .4f);
            TapInteract();
            TapInteract();
            Assert.IsTrue(control.IsWorking, "Gamepad Interact must start the selected repair.");
            yield return Until(() => Crew.Encounter.Airflow, "Running scrubber");
            var door = Object.FindAnyObjectByType<SolitudeSlidingDoor>();
            yield return Until(() => door.IsOpen, "Automatic door after recovery");
            var doorPosition = (Vector2)door.transform.position;
            yield return Position(doorPosition + Vector2.up * 1.1f);
            Queue(new GamepadState { leftStick = Vector2.down });
            try { yield return Until(() => Body.position.y < doorPosition.y - .8f, "Gamepad walk through the door"); }
            finally { Queue(new GamepadState()); }
        }

        private void TapInteract()
        {
            Queue(new GamepadState().WithButton(GamepadButton.East));
            Queue(new GamepadState());
            Assert.IsTrue(Field<InputPolicyCoordinator>(Root, "policy").Gameplay,
                "A gamepad Interact press in exploration must leave the gameplay input map ready.");
        }

        private void Queue(GamepadState state)
        {
            InputSystem.QueueStateEvent(pad, state);
            InputSystem.Update();
        }

        private static IEnumerator Launch(CrewBackground background, CrewBackgroundsFault fault)
        {
            if (Root != null)
            {
                Object.Destroy(Root.gameObject);
                yield return null;
            }
            var launches = Type.GetType("SOLITUDE.Editor.CrewBackgroundsPlaytestLaunches, SOLITUDE.Editor", true);
            launches.GetMethod("Stage", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { new CrewBackgroundsPlaytestSetup(background, fault) });
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(CrewBackgroundsPlaytestSetup.ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            Assert.AreEqual(RuntimeStartupState.Ready, Root.State);
            Assert.IsTrue(Root.UsesDisposableSession);
            Assert.AreEqual(background, Crew.ActiveSetup.Value.Background);
            Assert.AreEqual(fault, Crew.Encounter.Fault);
        }

        private static IEnumerator Position(Vector2 position)
        {
            Body.position = position;
            Player.transform.position = position;
            Body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            Player.RebuildProximityAfterRelocation();
            yield return new WaitForFixedUpdate();
            yield return null;
        }

        private static IEnumerator Until(Func<bool> test, string reason)
        {
            float deadline = Time.realtimeSinceStartup + 8;
            while (!test() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsTrue(test(), reason);
        }

        private static T Field<T>(object owner, string name) =>
            (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
    }
}
#endif
