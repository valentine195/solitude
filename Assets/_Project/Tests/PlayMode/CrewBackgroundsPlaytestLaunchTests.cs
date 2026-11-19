#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using SOLITUDE.Application;
using SOLITUDE.Composition;
using SOLITUDE.Items;
using SOLITUDE.Movement;
using SOLITUDE.Npcs.Sec0;
using SOLITUDE.Player;
using SOLITUDE.Player.Succession;
using SOLITUDE.World.CrewBackgrounds;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace SOLITUDE.Tests
{
    public sealed class CrewBackgroundsPlaytestLaunchTests
    {
        private static readonly Type Launches =
            Type.GetType("SOLITUDE.Editor.CrewBackgroundsPlaytestLaunches, SOLITUDE.Editor", true);
        private const BindingFlags PrivateStatic = BindingFlags.NonPublic | BindingFlags.Static;

        private static GameCompositionRoot Root => Object.FindAnyObjectByType<GameCompositionRoot>();
        private static CrewBackgroundsPlaytestComposition Crew =>
            Object.FindObjectsByType<CrewBackgroundsPlaytestComposition>().Single();

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Launches.GetMethod("Clear", PrivateStatic).Invoke(null, null);
            if (Root != null) Object.Destroy(Root.gameObject);
            yield return null;
            Time.timeScale = 1;
        }

        [UnityTest]
        public IEnumerator FourPresetPairsUseOneRoomAndFreshSharedPlayerState()
        {
            float[] baseline = null;
            var ids = new System.Collections.Generic.HashSet<Guid>();
            foreach (CrewBackground background in Enum.GetValues(typeof(CrewBackground)))
            foreach (CrewBackgroundsFault fault in Enum.GetValues(typeof(CrewBackgroundsFault)))
            {
                Stage(new CrewBackgroundsPlaytestSetup(background, fault));
                yield return LoadRoom();

                AssertReady(background, fault);
                var vitals = Object.FindObjectsByType<PlayerVitalsController>().Single().Vitals;
                var inventory = Object.FindObjectsByType<PlayerInventory>().Single();
                var movement = Object.FindObjectsByType<MovementBehavior>()
                    .Single(component => component.GetComponent<PlayerMovement>() != null);
                var speed = new SerializedObject(movement).FindProperty("moveSpeed").floatValue;
                var damage = AssetDatabase.LoadAssetAtPath<MeleeAttackConfiguration>(
                    "Assets/_Project/Features/Items/Weapons/MaintenanceWrench/MeleeConfiguration.asset").BaseDamage;
                var actual = new[] { vitals.Health.Current, vitals.Health.Max, vitals.Oxygen.Current,
                    vitals.Oxygen.Max, vitals.Hunger.Current, vitals.Hunger.Max, speed, inventory.Capacity, damage };
                baseline ??= actual;
                CollectionAssert.AreEqual(baseline, actual);
                Assert.IsTrue(inventory.Container.GetSlots().All(slot => slot.IsEmpty));
                var survivor = Object.FindObjectsByType<SurvivorSuccessionComposition>().Single();
                Assert.AreEqual(SurvivorSceneEntry.DisposableAwakening, survivor.EntryKind);
                Assert.IsTrue(ids.Add(survivor.Coordinator.Current.Id));
                vitals.Health.Set(70);
            }
        }

        [UnityTest]
        public IEnumerator InvalidPresetFailsBeforeReadinessThenOrdinaryAndNamedLaunchesRecover()
        {
            Stage(new CrewBackgroundsPlaytestSetup(CrewBackground.Technician, CrewBackgroundsFault.BrokenIntakeLinkage));
            yield return LoadRoom();
            AssertReady(CrewBackground.Technician, CrewBackgroundsFault.BrokenIntakeLinkage);

            var pendingFault = (string)Launches.GetField("PendingFault", PrivateStatic).GetRawConstantValue();
            Stage(new CrewBackgroundsPlaytestSetup(CrewBackground.Technician, CrewBackgroundsFault.LossOfPower));
            SessionState.SetInt(pendingFault, 99);
            LogAssert.Expect(LogType.Error, new Regex("Runtime unavailable; save preserved: Unknown scrubber fault"));
            yield return LoadRoom();
            Assert.AreEqual(RuntimeStartupState.Failed, Root.State);
            Assert.IsFalse(Policy().Ready);
            Assert.IsNull(Crew.ActiveSetup);
            Assert.AreEqual(-1, SessionState.GetInt(pendingFault, -1));

            yield return LoadRoom();
            AssertReady(CrewBackground.Engineer, CrewBackgroundsFault.LossOfPower);

            Stage(new CrewBackgroundsPlaytestSetup(CrewBackground.Technician, CrewBackgroundsFault.LossOfPower));
            yield return LoadRoom();
            AssertReady(CrewBackground.Technician, CrewBackgroundsFault.LossOfPower);
        }

        [UnityTest]
        public IEnumerator SavedRoomHasPaintedFloorBoundariesAndAClosedReturnDoor()
        {
            yield return LoadRoom();
            AssertReady(CrewBackground.Engineer, CrewBackgroundsFault.LossOfPower);

            var maps = Object.FindObjectsByType<Tilemap>();
            var floor = maps.Single(map => map.name == "Floor");
            var boundaries = maps.Single(map => map.name == "Boundary Caps");
            Assert.IsNotNull(floor.layoutGrid);
            Assert.IsNotNull(boundaries.layoutGrid);
            Assert.IsTrue(floor.HasTile(new Vector3Int(25, -1)));
            Assert.IsTrue(boundaries.HasTile(new Vector3Int(24, -1)));
            Assert.IsFalse(boundaries.HasTile(new Vector3Int(25, 0)));

            Physics2D.SyncTransforms();
            var door = Physics2D.OverlapPoint(new Vector2(26, .5f));
            var passageBoundary = Physics2D.OverlapPoint(new Vector2(24.5f, -1.5f));
            Assert.AreEqual("Return Door", door?.name);
            Assert.AreEqual("Return Passage West", passageBoundary?.name);
            Assert.IsNull(Physics2D.OverlapPoint(new Vector2(26, -1.5f)));
            Assert.IsFalse(EditorBuildSettings.scenes.Any(entry => entry.enabled && entry.path == CrewBackgroundsPlaytestSetup.ScenePath));
        }

        private static IEnumerator LoadRoom()
        {
            if (Root != null)
            {
                Object.Destroy(Root.gameObject);
                yield return null;
            }

            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(CrewBackgroundsPlaytestSetup.ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            Assert.AreEqual(CrewBackgroundsPlaytestSetup.ScenePath, SceneManager.GetActiveScene().path);
        }

        private static void Stage(CrewBackgroundsPlaytestSetup setup) =>
            Launches.GetMethod("Stage", PrivateStatic).Invoke(null, new object[] { setup });

        private static InputPolicyCoordinator Policy() => (InputPolicyCoordinator)typeof(GameCompositionRoot)
            .GetField("policy", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Root);

        private static void AssertReady(CrewBackground background, CrewBackgroundsFault fault)
        {
            Assert.AreEqual(RuntimeStartupState.Ready, Root.State);
            Assert.IsTrue(Root.UsesDisposableSession);
            Assert.IsTrue(Policy().Ready);
            Assert.IsTrue(Crew.ActiveSetup.HasValue);
            var setup = Crew.ActiveSetup.Value;
            Assert.AreEqual(background, setup.Background);
            Assert.AreEqual(fault, setup.Fault);
            bool engineer = background == CrewBackground.Engineer;
            Assert.AreEqual(engineer ? CrewProficiencyRank.Specialist : CrewProficiencyRank.Trained,
                setup.Rank(CrewProficiency.Electrical));
            Assert.AreEqual(engineer ? CrewProficiencyRank.Trained : CrewProficiencyRank.Specialist,
                setup.Rank(CrewProficiency.FieldRepair));
            Assert.AreEqual(fault == CrewBackgroundsFault.BrokenIntakeLinkage, Crew.NormalFeedAvailable);
            Assert.AreEqual(fault == CrewBackgroundsFault.LossOfPower, Crew.IntakeLinkageIntact);
            Assert.IsTrue(Crew.ReturnDoorClosed);
            Assert.IsTrue(Object.FindObjectsByType<Sec0Controller>().Single().HasBinding);
        }
    }
}
#endif
