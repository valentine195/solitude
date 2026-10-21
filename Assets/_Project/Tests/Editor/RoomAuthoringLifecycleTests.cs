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

        [UnityTest] public IEnumerator NormalReloadKeepsPartialResultWithoutGuides() => Verify(false, true);
        [UnityTest] public IEnumerator DisabledReloadKeepsPartialResultWithoutGuides() => Verify(true, true);
        [UnityTest] public IEnumerator LayoutNormalReloadPreservesMembershipAndOutput() => Verify(false, false, true);
        [UnityTest] public IEnumerator LayoutDisabledReloadPreservesMembershipAndOutput() => Verify(true, false, true);
        [UnityTest] public IEnumerator LayoutNormalReloadPreservesPartialResult() => Verify(false, true, true);
        [UnityTest] public IEnumerator LayoutDisabledReloadPreservesPartialResult() => Verify(true, true, true);

        private static IEnumerator Verify(bool disableReload, bool partial = false, bool layoutMember = false)
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
            if (layoutMember) RoomAuthoringCreation.AdoptIntoNewLayout(room);
            if (partial)
            {
                room.Style.Northwest.Sprite = null; EditorUtility.SetDirty(room.Style); AssetDatabase.SaveAssets();
                Assert.IsFalse(RoomAuthoringOperations.Rebuild(room));
            }
            SessionState.SetBool(Key + "partial", partial);
            SessionState.SetString(Key + "result", JsonUtility.ToJson(room.GenerationResult));
            SessionState.SetString(Key + "identity", room.RoomId);
            SessionState.SetBool(Key + "layout", layoutMember);
            if (layoutMember)
            {
                SessionState.SetString(Key + "layoutId", room.Layout.LayoutId);
                SessionState.SetString(Key + "layoutResult", JsonUtility.ToJson(room.Layout.GenerationResult));
            }
            Assert.IsTrue(EditorSceneManager.SaveScene(scene, folder + "/Room.unity"));
            room.Occupancy.gameObject.SetActive(true);
            Selection.activeGameObject = room.Occupancy.gameObject;
            if (layoutMember)
            {
                var pending = RoomAuthoringOperations.BeginEdit(room, "Unfinished Layout Gesture");
                RoomAuthoringOperations.PaintCell(room, new Vector3Int(10, 0, 0), room.Style.FloorTile, pending);
                Assert.AreEqual(49, RoomAuthoringOperations.ReadCells(room).Length);
                Assert.AreEqual(48, RoomAuthoringTestFixtures.FloorCount(room));
            }
            yield return new EnterPlayMode(!disableReload);
            var playingRoom = Object.FindAnyObjectByType<ShipRoomAuthoring>();
            Assert.IsNotNull(playingRoom);
            Assert.IsFalse(playingRoom.Occupancy.gameObject.activeInHierarchy);
            Assert.AreEqual(SessionState.GetString(Key + "identity", ""), playingRoom.RoomId);
            Assert.AreEqual(48, RoomAuthoringOperations.ReadCells(playingRoom).Length);
            var sprites = playingRoom.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>();
            Assert.AreEqual(SessionState.GetBool(Key + "partial", false) ? 27 : 28, sprites.Length);
            Assert.AreEqual(SessionState.GetString(Key + "result", ""), JsonUtility.ToJson(playingRoom.GenerationResult));
            if (SessionState.GetBool(Key + "layout", false))
            {
                Assert.AreEqual(SessionState.GetString(Key + "layoutId", ""), playingRoom.Layout.LayoutId);
                Assert.AreSame(playingRoom.GeneratedOutput, playingRoom.Layout.GeneratedOutput);
                Assert.AreSame(playingRoom.Layout, playingRoom.GeneratedOutput.LayoutOwner);
                Assert.AreEqual(SessionState.GetString(Key + "layoutResult", ""), JsonUtility.ToJson(playingRoom.Layout.GenerationResult));
            }
            Assert.IsFalse(RoomAuthoringPreview.CanShow);
            Assert.IsNull(RoomAuthoringPreview.GetFeedback(playingRoom).Assessment);
            Assert.IsNull(RoomAuthoringPreview.CurrentRoom);
            var references = sprites.Select(r => r.GetEntityId()).ToArray();
            yield return null; yield return null;
            CollectionAssert.AreEqual(references, playingRoom.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>().Select(r => r.GetEntityId()));
            yield return new ExitPlayMode();
            var restored = Object.FindAnyObjectByType<ShipRoomAuthoring>();
            Assert.AreEqual(SessionState.GetString(Key + "identity", ""), restored.RoomId);
            Assert.AreEqual(48, RoomAuthoringOperations.ReadCells(restored).Length);
            Assert.IsFalse(restored.Occupancy.gameObject.activeSelf);
            Assert.AreEqual(SessionState.GetString(Key + "result", ""), JsonUtility.ToJson(restored.GenerationResult));
            Assert.IsTrue(RoomAuthoringValidator.IsCurrent(restored));
            if (SessionState.GetBool(Key + "layout", false)) Assert.IsTrue(RoomAuthoringValidator.IsCurrent(restored.Layout));
            Selection.activeGameObject = restored.Occupancy.gameObject;
            Assert.AreSame(restored.Occupancy.gameObject, Selection.activeGameObject);
            Assert.AreEqual(!SessionState.GetBool(Key + "partial", false), RoomAuthoringOperations.Rebuild(restored), restored.GenerationDiagnostic);
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

        [TestCase(false, false)] [TestCase(true, false)]
        [TestCase(false, true)] [TestCase(true, true)]
        public void BuildMaskRemovalPreservesGeneratedOutputAndUnrelatedObjects(bool partial, bool layoutMember)
        {
            using var f = new RoomAuthoringTestFixtures();
            var room = f.Room(); Assert.IsTrue(RoomAuthoringOperations.Rebuild(room));
            if (layoutMember) RoomAuthoringCreation.AdoptIntoNewLayout(room);
            if (partial) Assert.IsFalse(RoomAuthoringOperations.PaintCell(room, new Vector3Int(6, 4, 0), null));
            var generated = room.GeneratedOutput.GetComponentsInChildren<Transform>();
            var sentinel = new GameObject("Authored Gameplay Sentinel");
            SceneManager.MoveGameObjectToScene(sentinel, f.Scene);
            var result = room.Layout == null ? null : JsonUtility.ToJson(room.Layout.GenerationResult);
            RoomAuthoringBuildProcessor.StripAuthoringMask(f.Scene);
            Assert.IsTrue(room.Occupancy == null);
            CollectionAssert.AreEqual(generated, room.GeneratedOutput.GetComponentsInChildren<Transform>());
            Assert.AreEqual(partial ? 35 : 28, room.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>().Length);
            Assert.IsTrue(sentinel != null);
            if (layoutMember)
            {
                Assert.AreSame(room, room.Layout.Spaces.Single());
                Assert.AreSame(room.GeneratedOutput, room.Layout.GeneratedOutput);
                Assert.AreEqual(result, JsonUtility.ToJson(room.Layout.GenerationResult));
            }
        }
    }
}
