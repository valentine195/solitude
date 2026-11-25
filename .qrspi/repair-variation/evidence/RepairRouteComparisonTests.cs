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
    public sealed class RepairRouteComparisonTests
    {
        private static readonly Type Launches =
            Type.GetType("SOLITUDE.Editor.CrewBackgroundsPlaytestLaunches, SOLITUDE.Editor", true);
        private static GameCompositionRoot Root => Object.FindAnyObjectByType<GameCompositionRoot>();
        private static CrewBackgroundsPlaytestComposition Crew => Object.FindAnyObjectByType<CrewBackgroundsPlaytestComposition>();
        private static PlayerInteractor Player => Object.FindAnyObjectByType<PlayerInteractor>();
        private static SolitudeSlidingDoor Door => Object.FindAnyObjectByType<SolitudeSlidingDoor>();
        private static Rigidbody2D Body => Player.GetComponent<Rigidbody2D>();
        private static CrewBackgroundsEncounterState Encounter => Crew.Encounter;
        private static string Message => Field<TMP_Text>(Object.FindAnyObjectByType<InteractionFeedbackView>(), "label").text;
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


        [UnityTest] public IEnumerator PatrolPresentBreaker() => Run(true, false);
        [UnityTest] public IEnumerator PatrolPresentJumper() => Run(true, true);
        [UnityTest] public IEnumerator PatrolElsewhereBreaker() => Run(false, false);
        [UnityTest] public IEnumerator PatrolElsewhereJumper() => Run(false, true);

        private Sec0Controller bot;
        private float sightSeconds;
        private float closest;
        private IEnumerator Run(bool patrolPresent, bool jumper)
        {
            yield return Launch(CrewBackground.Engineer, CrewBackgroundsFault.LossOfPower);
            bot = Object.FindAnyObjectByType<Sec0Controller>();
            if (!patrolPresent) bot.gameObject.SetActive(false);
            sightSeconds = 0;
            closest = float.PositiveInfinity;
            var start = Time.time;
            Debug.Log($"REPAIR_AB_START patrol={patrolPresent} jumper={jumper} player={Body.position} door={Door.transform.position}");

            if (jumper)
            {
                var pickup = Object.FindObjectsByType<WorldPickup>().Single();
                yield return Go(new Vector2(21, 3.0f), "jumper pickup");
                Press(Key.E);
                Assert.IsFalse(pickup.gameObject.activeSelf, Message);
                yield return Go(new Vector2(24.6f, 2.55f), "service panel");
                Press(Key.E);
                Assert.That(Message, Does.Contain("Lights go out"));
            }
            else
            {
                yield return Go(new Vector2(22, 7.2f), "wall-side lane");
                yield return Go(new Vector2(27.05f, 7.2f), "across the patrol");
                yield return Go(new Vector2(27.05f, 6.35f), "breaker");
                Press(Key.E);
                Assert.That(Message, Does.Contain("Reset it"));
            }
            Press(Key.E);
            yield return ObserveUntil(() => Encounter.Airflow, "repair completion");
            float repairTime = Time.time-start;
            yield return null;
            var feedback = Object.FindAnyObjectByType<CrewBackgroundsRoomFeedback>();
            Assert.AreEqual(!jumper, feedback.CorridorLightEnabled);
            Assert.AreEqual(jumper, Encounter.JumperInstalled);
            if (!jumper)
            {
                yield return Go(new Vector2(27.05f, 2.0f), "return from breaker");
            }
            yield return Go(new Vector2(26.0f, 1.6f), "door approach");
            yield return ObserveUntil(() => Door.IsOpen, "return door opens");
            yield return Go(new Vector2(26.0f, -.4f), "walk through the opening");
            Debug.Log($"REPAIR_AB patrol={patrolPresent} jumper={jumper} repair={repairTime:F2}s exit={Time.time-start:F2}s sight={sightSeconds:F2}s minDistance={closest:F2} shots={bot.ShotsFired} stuns={bot.StunsLanded} lights={feedback.CorridorLightEnabled} installed={Encounter.JumperInstalled}");
        }

        private IEnumerator Go(Vector2 point, string detail)
        {
            float deadline = Time.realtimeSinceStartup + 9f;
            while (Vector2.Distance(Body.position, point) > .16f && Time.realtimeSinceStartup < deadline)
            {
                Vector2 offset = point - Body.position;
                if (Mathf.Abs(offset.x) > .11f && Mathf.Abs(offset.y) > .11f)
                    SetKeys(offset.x < 0 ? Key.A : Key.D, offset.y < 0 ? Key.S : Key.W);
                else if (Mathf.Abs(offset.x) > Mathf.Abs(offset.y))
                    SetKeys(offset.x < 0 ? Key.A : Key.D);
                else SetKeys(offset.y < 0 ? Key.S : Key.W);
                Observe();
                yield return null;
            }
            SetKeys();
            Assert.That(Vector2.Distance(Body.position, point), Is.LessThanOrEqualTo(.2f), $"{detail}: at {Body.position}, wanted {point}");
            yield return null;
        }

        private IEnumerator ObserveUntil(Func<bool> done, string detail)
        {
            float deadline = Time.realtimeSinceStartup + 9f;
            while (!done() && Time.realtimeSinceStartup < deadline)
            {
                Observe();
                yield return null;
            }
            Assert.IsTrue(done(), $"{detail}: at {Body.position}, bot state {bot.State}, shots {bot.ShotsFired}, stuns {bot.StunsLanded}");
        }

        private void Observe()
        {
            if (!bot.isActiveAndEnabled) return;
            if (bot.CanSeeTarget()) sightSeconds += Time.deltaTime;
            closest = Mathf.Min(closest, Vector2.Distance(bot.transform.position, Body.position));
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

        private static T Field<T>(object owner, string name) =>
            (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);

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

    }
}
#endif
