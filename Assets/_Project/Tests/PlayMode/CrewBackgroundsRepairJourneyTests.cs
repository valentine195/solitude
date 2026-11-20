#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SOLITUDE.Application;
using SOLITUDE.Composition;
using SOLITUDE.Containers;
using SOLITUDE.Core.UI;
using SOLITUDE.Items;
using SOLITUDE.Npcs.Sec0;
using SOLITUDE.Player;
using SOLITUDE.SaveLoad;
using SOLITUDE.World.CrewBackgrounds;
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
    public sealed class CrewBackgroundsRepairJourneyTests
    {
        private static readonly Type Launches =
            Type.GetType("SOLITUDE.Editor.CrewBackgroundsPlaytestLaunches, SOLITUDE.Editor", true);
        private static GameCompositionRoot Root => Object.FindAnyObjectByType<GameCompositionRoot>();
        private static CrewBackgroundsPlaytestComposition Crew => Object.FindAnyObjectByType<CrewBackgroundsPlaytestComposition>();
        private static PlayerInteractor Player => Object.FindAnyObjectByType<PlayerInteractor>();
        private static SolitudeSlidingDoor Door => Object.FindAnyObjectByType<SolitudeSlidingDoor>();
        private static Rigidbody2D Body => Player.GetComponent<Rigidbody2D>();
        private static IContainerReader Inventory => Object.FindAnyObjectByType<PlayerInventory>().Container;
        private static ContainerCommandService Commands => Field<ContainerSaveSession>(Root, "session").Commands;
        private static CrewBackgroundsEncounterState Encounter => Crew.Encounter;
        private static string Message => Field<TMP_Text>(Object.FindAnyObjectByType<InteractionFeedbackView>(), "label").text;
        private const string DifferentPartPath = "Assets/_Project/Features/Items/Definitions/ReplacementFuse/ReplacementFuse.asset";
        private static ItemDefinition DifferentPart => AssetDatabase.LoadAssetAtPath<ItemDefinition>(DifferentPartPath);
        private Keyboard keyboard;
        private InputSettings.BackgroundBehavior previousBackground;
        private InputSettings.EditorInputBehaviorInPlayMode previousEditor;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            previousBackground = InputSystem.settings.backgroundBehavior;
            previousEditor = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard = InputSystem.AddDevice<Keyboard>();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.backgroundBehavior = previousBackground;
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditor;
            if (Root != null) Object.Destroy(Root.gameObject);
            yield return null;
            Time.timeScale = 1;
        }

        [UnityTest]
        public IEnumerator BothBackgroundsFollowTheDeadFeedAndResetTheBreaker()
        {
            foreach (CrewBackground background in Enum.GetValues(typeof(CrewBackground)))
            {
                yield return Launch(background, CrewBackgroundsFault.LossOfPower);
                var scrubber = Control("AtmosphericScrubber");
                var breaker = Control("Main Breaker");
                var bot = Object.FindAnyObjectByType<Sec0Controller>();
                var initialBotPosition = bot.transform.position;

                yield return Approach(scrubber);
                Press(Key.E);
                Assert.That(Message, Does.Contain("MAIN BREAKER beyond the patrol"));
                Assert.IsFalse(Encounter.Airflow);
                Assert.IsFalse(Door.Interact(Player).IsSuccess);
                Assert.IsTrue(Crew.ReturnDoorClosed);

                yield return new WaitForSeconds(.7f);
                Assert.IsTrue(bot.HasBinding);
                Assert.Greater(Vector2.Distance(initialBotPosition, bot.transform.position), .02f);
                Assert.Greater(breaker.transform.position.x, bot.transform.parent.position.x);

                yield return WalkUntil(Key.W, () => Body.position.y >= 7.2f, "Reach the wall-side path");
                yield return WalkUntil(Key.D, () => Body.position.x >= breaker.transform.position.x - .15f,
                    "Cross past the patrol under keyboard control");
                yield return WalkUntil(Key.S, () => Body.position.y <= breaker.transform.position.y - .25f,
                    "Reach the breaker");
                yield return null;
                Press(Key.E);
                Assert.That(Message, Does.Contain("Reset it"));
                Assert.AreEqual("Reset breaker", breaker.GetPrompt());
                Press(Key.E);
                Assert.IsTrue(breaker.IsWorking);
                Assert.IsFalse(Encounter.NormalFeedAvailable);
                yield return Until(() => Encounter.Airflow, "Breaker completion");

                Assert.IsTrue(Encounter.NormalFeedAvailable);
                Assert.IsTrue(Encounter.IntakeLinkageIntact);
                Assert.IsTrue(Inventory.GetSlots().All(slot => slot.IsEmpty));
                Assert.IsTrue(GameObject.Find("Global Light 2D").GetComponents<Behaviour>()
                    .Any(light => light.GetType().Name == "Light2D" && light.enabled));
                yield return CheckRecoveryAndTraverse(checkPause: background == CrewBackground.Engineer);
            }
        }

        [UnityTest]
        public IEnumerator BothBackgroundsIdentifyAndFitTheActualPickup()
        {
            foreach (CrewBackground background in Enum.GetValues(typeof(CrewBackground)))
            {
                yield return Launch(background, CrewBackgroundsFault.BrokenIntakeLinkage);
                var scrubber = Control("AtmosphericScrubber");
                var pickup = Object.FindAnyObjectByType<WorldPickup>();
                var part = pickup.Definition.Item;

                yield return Approach(scrubber);
                Press(Key.E);
                Assert.That(Message, Does.Contain("replacement intake linkage"));
                Press(Key.E);
                Assert.That(Message, Is.EqualTo("Requires 1 Intake linkage"));
                Assert.IsFalse(Encounter.Airflow);

                yield return Approach(pickup);
                Press(Key.E);
                Assert.That(Message, Does.Contain("Intake linkage"));
                Assert.IsFalse(pickup.gameObject.activeSelf);
                Assert.AreEqual(1, Quantity(part));

                yield return Approach(scrubber);
                Press(Key.E);
                Assert.IsTrue(scrubber.IsWorking);
                Assert.AreEqual(1, Quantity(part));
                Assert.IsFalse(Encounter.Airflow);
                yield return Until(() => Encounter.Airflow, "Linkage completion");
                Assert.AreEqual(0, Quantity(part));

                Press(Key.E);
                Assert.That(Message, Is.EqualTo("Repair already complete"));
                Assert.AreEqual(0, Quantity(part));
                yield return CheckRecoveryAndTraverse(checkPause: false);
            }
        }

        [UnityTest]
        public IEnumerator CancelAndMaterialLossPreserveTheFaultAndItemAccounting()
        {
            yield return Launch(CrewBackground.Technician, CrewBackgroundsFault.BrokenIntakeLinkage);
            var part = Object.FindAnyObjectByType<WorldPickup>().Definition.Item;
            var scrubber = Control("AtmosphericScrubber");

            Assert.AreEqual(CommandStatus.Applied, Commands.Grant(Inventory.Handle, DifferentPart.ItemId, 1).Status);
            yield return Approach(scrubber);
            Press(Key.E);
            Press(Key.E);
            Assert.That(Message, Is.EqualTo("Requires 1 Intake linkage"));
            Assert.AreEqual(1, Quantity(DifferentPart));
            Assert.IsFalse(Encounter.Airflow);

            Assert.AreEqual(CommandStatus.Applied, Commands.Grant(Inventory.Handle, part.ItemId, 1).Status);
            Press(Key.E);
            Assert.IsTrue(scrubber.IsWorking);
            Press(Key.E);
            Assert.That(Message, Is.EqualTo("Repair in progress"));
            Press(Key.Escape);
            Assert.IsFalse(scrubber.IsWorking);
            Assert.AreEqual(1, Quantity(part));
            Assert.IsFalse(Encounter.Airflow);

            Press(Key.E);
            Assert.IsTrue(scrubber.IsWorking);
            var slot = Inventory.GetSlots().Single(entry => !entry.IsEmpty && entry.Stack.ItemId == part.ItemId);
            Assert.AreEqual(CommandStatus.Applied, Commands.Remove(slot.Address, 1).Status);
            yield return Until(() => !scrubber.IsWorking, "Revalidation after removing material");
            Assert.That(Message, Is.EqualTo("Requires 1 Intake linkage"));
            Assert.AreEqual(1, Quantity(DifferentPart));
            Assert.IsFalse(Encounter.Airflow);
            Assert.IsTrue(Crew.ReturnDoorClosed);
        }

        [UnityTest]
        public IEnumerator SameControlRepairsASecondTargetUsingItsAuthoredPart()
        {
            yield return Launch(CrewBackground.Engineer, CrewBackgroundsFault.BrokenIntakeLinkage);
            var actualPart = Object.FindAnyObjectByType<WorldPickup>().Definition.Item;
            Assert.AreEqual(CommandStatus.Applied, Commands.Grant(Inventory.Handle, actualPart.ItemId, 1).Status);
            Assert.AreEqual(CommandStatus.Applied, Commands.Grant(Inventory.Handle, DifferentPart.ItemId, 1).Status);
            var target = new CrewBackgroundsEncounterState(CrewBackgroundsFault.BrokenIntakeLinkage);
            var fixture = new GameObject("Independent Repair Control");
            fixture.transform.position = Player.transform.position;
            var control = fixture.AddComponent<CrewBackgroundsRepairControl>();
            try
            {
                control.Configure(new[] { new CrewBackgroundsRepairControl.ActionBinding
                {
                    Fault = CrewBackgroundsFault.BrokenIntakeLinkage,
                    Operation = CrewRepairOperation.FitIntakeLinkage,
                    Material = DifferentPart,
                    Skill = CrewProficiency.FieldRepair,
                    Clue = "Fixture is broken",
                    TrainedClue = "Fixture requires replacement fuse",
                    ActionLabel = "Fit 1 replacement fuse",
                    Completion = "Fixture repaired"
                } }, fixture.AddComponent<SpriteRenderer>());
                control.Bind(Field<SceneFeatureContext>(Crew, "context"), Crew.ActiveSetup.Value, target);

                Assert.That(control.Interact(Player).Message, Is.EqualTo("Fixture requires replacement fuse"));
                Assert.IsTrue(control.Interact(Player).IsSuccess);
                yield return Until(() => target.Airflow, "Independent target completion");

                Assert.AreEqual(0, Quantity(DifferentPart));
                Assert.AreEqual(1, Quantity(actualPart));
                Assert.IsFalse(Encounter.Airflow);
                Assert.IsTrue(Crew.ReturnDoorClosed);
            }
            finally { Object.Destroy(fixture); }
        }

        private IEnumerator CheckRecoveryAndTraverse(bool checkPause)
        {
            yield return null;
            var status = Object.FindObjectsByType<TMP_Text>().Single(label => label.name == "Scrubber Status");
            var feedback = Object.FindAnyObjectByType<CrewBackgroundsRoomFeedback>();
            var running = Field<AudioSource>(feedback, "runningSound");
            var alert = Field<AudioSource>(feedback, "alert");
            Assert.That(status.text, Is.EqualTo("AIR RECOVERING"));
            Assert.AreEqual("scrubber_running", running.clip.name);
            Assert.IsTrue(running.isPlaying, "The scrubber running sound must start on committed airflow.");
            Assert.IsTrue(alert.isPlaying);
            Assert.IsTrue(Crew.ReturnDoorClosed);

            if (checkPause)
            {
                using (Field<PauseCoordinator>(Root, "pause").Acquire())
                {
                    double elapsed = Encounter.RecoverySeconds;
                    yield return new WaitForSecondsRealtime(.35f);
                    Assert.AreEqual(elapsed, Encounter.RecoverySeconds);
                    Assert.IsTrue(Crew.ReturnDoorClosed);
                }
            }

            yield return Until(() => Encounter.RecoverySeconds >= 4.6, "Recovering for at least 4.6 seconds");
            Assert.IsFalse(Door.IsOpen);
            Assert.IsTrue(Crew.ReturnDoorClosed);
            yield return Until(() => Door.IsOpen, "Door opening after room recovery");
            Assert.That(status.text, Is.EqualTo("AIRFLOW RESTORED"));
            Assert.IsFalse(alert.isPlaying);
            Assert.IsFalse(Crew.ReturnDoorClosed);

            var doorPosition = (Vector2)Door.transform.position;
            yield return Place(doorPosition + Vector2.up * 1.1f);
            SetKeys(Key.S);
            try
            {
                yield return Until(() => Body.position.y < doorPosition.y - .8f, "Keyboard traversal through return door");
            }
            finally { SetKeys(); }
        }

        private IEnumerator WalkUntil(Key key, Func<bool> completed, string detail)
        {
            SetKeys(key);
            try { yield return Until(completed, detail); }
            finally { SetKeys(); }
        }

        private static IEnumerator Launch(CrewBackground background, CrewBackgroundsFault fault)
        {
            if (Root != null)
            {
                Object.Destroy(Root.gameObject);
                yield return null;
            }

            Launches.GetMethod("Stage", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { new CrewBackgroundsPlaytestSetup(background, fault) });
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(CrewBackgroundsPlaytestSetup.ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            Assert.AreEqual(RuntimeStartupState.Ready, Root.State);
            Assert.AreEqual(background, Crew.ActiveSetup.Value.Background);
            Assert.AreEqual(fault, Encounter.Fault);
        }

        private static CrewBackgroundsRepairControl Control(string name) =>
            Object.FindObjectsByType<CrewBackgroundsRepairControl>().Single(control => control.name == name);

        private static IEnumerator Approach(Component target) => Place((Vector2)target.transform.position + Vector2.down * .4f);

        private static IEnumerator Place(Vector2 position)
        {
            Body.position = position;
            Player.transform.position = position;
            Body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            Player.RebuildProximityAfterRelocation();
            yield return new WaitForFixedUpdate();
            yield return null;
        }

        private static T Field<T>(object owner, string name) =>
            (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);

        private static int Quantity(ItemDefinition item) => Inventory.GetSlots()
            .Where(slot => !slot.IsEmpty && slot.Stack.ItemId == item.ItemId).Sum(slot => slot.Stack.Quantity);

        private void Press(Key key)
        {
            SetKeys(key);
            SetKeys();
        }
        private void SetKeys(params Key[] keys)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
            InputSystem.Update();
        }

        private static IEnumerator Until(Func<bool> completed, string detail)
        {
            float deadline = Time.realtimeSinceStartup + 8;
            while (!completed() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsTrue(completed(), detail);
        }
    }
}
#endif
