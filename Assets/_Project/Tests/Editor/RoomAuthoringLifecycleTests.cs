using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using SOLITUDE.Editor.RoomAuthoring;
using SOLITUDE.World.RoomAuthoring;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SOLITUDE.Tests.RoomAuthoring
{
    public sealed class RoomAuthoringLifecycleTests
    {
        private const string Key = "SOLITUDE.RoomAuthoring.Lifecycle.";
        private static void RequireIsolatedRunner() => Assert.IsTrue(Environment.GetCommandLineArgs().Contains("-solitudeRoomAuthoringVerification"), "Lifecycle tests require an isolated project and explicit verification flag.");

        [UnityTest] public IEnumerator NormalReloadKeepsAuthoredMaskAndGeneratedOutput() => Verify(false);
        [UnityTest] public IEnumerator DisabledReloadKeepsAuthoredMaskAndGeneratedOutput() => Verify(true);

        private static IEnumerator Verify(bool disableReload)
        {
            RequireIsolatedRunner();
            SessionState.SetBool(Key + "enabled", EditorSettings.enterPlayModeOptionsEnabled);
            SessionState.SetInt(Key + "options", (int)EditorSettings.enterPlayModeOptions);
            SessionState.SetBool(Key + "running", true);
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = disableReload ? EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload : EnterPlayModeOptions.None;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var folder = RoomPersistenceTests.CreateAssets(); SessionState.SetString(Key + "folder", folder);
            var room = RoomPersistenceTests.CreateRoom(scene, folder);
            SessionState.SetString(Key + "identity", room.RoomId);
            Assert.IsTrue(EditorSceneManager.SaveScene(scene, folder + "/Room.unity"));
            room.Occupancy.gameObject.SetActive(true);
            Selection.activeGameObject = room.Occupancy.gameObject;
            yield return new EnterPlayMode(!disableReload);
            var playingRoom = Object.FindAnyObjectByType<ShipRoomAuthoring>();
            Assert.IsNotNull(playingRoom);
            Assert.IsFalse(playingRoom.Occupancy.gameObject.activeInHierarchy);
            Assert.AreEqual(SessionState.GetString(Key + "identity", ""), playingRoom.RoomId);
            Assert.AreEqual(48, RoomAuthoringOperations.ReadCells(playingRoom).Length);
            var sprites = playingRoom.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>();
            Assert.AreEqual(28, sprites.Length);
            var references = sprites.Select(r => r.GetEntityId()).ToArray();
            yield return null; yield return null;
            CollectionAssert.AreEqual(references, playingRoom.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>().Select(r => r.GetEntityId()));
            yield return new ExitPlayMode();
            var restored = Object.FindAnyObjectByType<ShipRoomAuthoring>();
            Assert.AreEqual(SessionState.GetString(Key + "identity", ""), restored.RoomId);
            Assert.AreEqual(48, RoomAuthoringOperations.ReadCells(restored).Length);
            Assert.IsFalse(restored.Occupancy.gameObject.activeSelf);
            Selection.activeGameObject = restored.Occupancy.gameObject;
            Assert.AreSame(restored.Occupancy.gameObject, Selection.activeGameObject);
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(restored), restored.GenerationDiagnostic);
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (!SessionState.GetBool(Key + "running", false)) yield break;
            if (UnityEngine.Application.isPlaying) yield return new ExitPlayMode();
            Selection.activeObject = null;
            EditorSettings.enterPlayModeOptionsEnabled = SessionState.GetBool(Key + "enabled", false);
            EditorSettings.enterPlayModeOptions = (EnterPlayModeOptions)SessionState.GetInt(Key + "options", 0);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var folder = SessionState.GetString(Key + "folder", "");
            if (!string.IsNullOrEmpty(folder)) AssetDatabase.DeleteAsset(folder);
            SessionState.SetBool(Key + "running", false);
        }

        [Test] public void BuildMaskRemovalPreservesGeneratedOutputAndUnrelatedObjects()
        {
            using var f = new RoomAuthoringTestFixtures();
            var room = f.Room(); Assert.IsTrue(RoomAuthoringOperations.Rebuild(room));
            var generated = room.GeneratedOutput.GetComponentsInChildren<Transform>();
            RoomAuthoringBuildProcessor.StripAuthoringMask(f.Scene);
            Assert.IsTrue(room.Occupancy == null);
            CollectionAssert.AreEqual(generated, room.GeneratedOutput.GetComponentsInChildren<Transform>());
            Assert.AreEqual(28, room.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>().Length);
        }
    }
}
