using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SOLITUDE.Editor.RoomAuthoring;
using SOLITUDE.World.RoomAuthoring;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SOLITUDE.Tests.RoomAuthoring
{
    public sealed class RoomFloorPanelResourceTests
    {
        [Test] public void RepeatedSeedStyleUndoReopenAndExplicitCleanupRetainOnlyVerifiedResources()
        {
            Assert.IsTrue(Environment.GetCommandLineArgs().Contains("-solitudeRoomAuthoringVerification"));
            var folder = RoomPersistenceTests.CreateAssets();
            var prior = RoomFloorPanelAssetStore.VerificationRoot;
            Undo.ClearAll();
            try
            {
                RoomFloorPanelAssetStore.VerificationRoot = folder + "/Panels";
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var room = RoomPersistenceTests.CreateRoom(scene, folder);
                room.Style.Rendering = new RoomStyleDefinition.RenderingConfiguration();
                EditorUtility.SetDirty(room.Style); AssetDatabase.SaveAssets();
                for (var seed = 0; seed < 5; seed++)
                {
                    var settings = room.FloorPanels.Copy(); settings.Enabled = true; settings.Seed = seed;
                    Assert.IsTrue(RoomAuthoringOperations.SetFloorPanelSettings(room, settings, out var issue), issue);
                    var revision = room.GeneratedOutput.PreparedPanels; Assert.IsNotNull(revision);
                    var hash = RoomFloorPanelAssetStore.ContentHash(revision);
                    var count = RoomFloorPanelAssetStore.RetainedRevisionCount;
                    RoomAuthoringOperations.Rebuild(room); RoomAuthoringOperations.Rebuild(room);
                    Assert.AreSame(revision, room.GeneratedOutput.PreparedPanels);
                    Assert.AreEqual(count, RoomFloorPanelAssetStore.RetainedRevisionCount);
                    if (seed > 0)
                    {
                        Undo.PerformUndo(); Assert.IsNotNull(room.GeneratedOutput.PreparedPanels);
                        Undo.PerformRedo(); Assert.AreEqual(hash, RoomFloorPanelAssetStore.ContentHash(room.GeneratedOutput.PreparedPanels));
                    }
                }
                var saved = room.GeneratedOutput.PreparedPanels; var savedHash = RoomFloorPanelAssetStore.ContentHash(saved);
                var path = folder + "/Resources.unity"; Assert.IsTrue(EditorSceneManager.SaveScene(scene, path));
                Assert.IsFalse(RoomFloorPanelAssetStore.CleanupUnused(saved, true, out var refusal)); StringAssert.Contains("saved reference", refusal);
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                scene = EditorSceneManager.OpenScene(path);
                room = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ShipRoomAuthoring>()).Single();
                Assert.AreEqual(savedHash, RoomFloorPanelAssetStore.ContentHash(room.GeneratedOutput.PreparedPanels));
                Assert.IsTrue(room.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>().All(r => r.sprite != null));
                var revisions = AssetDatabase.FindAssets("t:RoomFloorPanelOutput", new[] { RoomFloorPanelAssetStore.VerificationRoot })
                    .Select(g => AssetDatabase.LoadAssetAtPath<RoomFloorPanelOutput>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
                Assert.AreEqual(5, revisions.Length);
                var old = revisions.First(r => r != saved); var oldPath = AssetDatabase.GetAssetPath(old);
                Assert.IsFalse(RoomFloorPanelAssetStore.CleanupUnused(old, false, out refusal)); StringAssert.Contains("Undo", refusal);
                Assert.IsTrue(RoomFloorPanelAssetStore.CleanupUnused(old, true, out refusal), refusal);
                Assert.IsFalse(File.Exists(oldPath)); Assert.AreEqual(4, RoomFloorPanelAssetStore.RetainedRevisionCount);
                Assert.AreEqual(savedHash, RoomFloorPanelAssetStore.ContentHash(room.GeneratedOutput.PreparedPanels));
                Assert.IsTrue(RoomAuthoringValidator.IsCurrent(room));
                TestContext.WriteLine("Five immutable seed revisions retained; repeat builds add zero; saved revision refused cleanup; one verified unreferenced revision removed only at explicit end-Undo boundary; four remain.");
            }
            finally
            {
                Undo.ClearAll(); EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                AssetDatabase.DeleteAsset(folder); RoomFloorPanelAssetStore.VerificationRoot = prior;
            }
        }
    }
}
