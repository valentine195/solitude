using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using SOLITUDE.Editor.RoomAuthoring;
using SOLITUDE.World.RoomAuthoring;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace SOLITUDE.Tests.RoomAuthoring
{
    public sealed class RoomFloorPanelPerformanceTests
    {
        private string folder;
        private ShipRoomAuthoring room;
        private RoomFloorPanelOutput output;

        [SetUp] public void Setup()
        {
            Assert.IsTrue(Environment.GetCommandLineArgs().Contains("-solitudeRoomAuthoringVerification"));
            Undo.ClearAll();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            folder = RoomPersistenceTests.CreateAssets();
            RoomFloorPanelAssetStore.VerificationRoot = folder + "/Panels";
            room = RoomPersistenceTests.CreateRoom(scene, folder);
            room.Style.Rendering = new RoomStyleDefinition.RenderingConfiguration();
            // Two spatial chunks sharing one container; deliberately different RGBA channels and alpha.
            foreach (var cell in RoomAuthoringTestFixtures.Cells(16, 8))
                room.Occupancy.SetTile(new Vector3Int(cell.x, cell.y, 0), room.Style.FloorTile);
            var settings = room.FloorPanels.Copy(); settings.Enabled = true; settings.Seed = 17;
            settings.SeamPalette = new[] { (Color)new Color32(17, 43, 89, 137), (Color)new Color32(211, 3, 127, 63) };
            Assert.IsTrue(RoomAuthoringOperations.SetFloorPanelSettings(room, settings, out var issue), issue);
            output = AssetDatabase.LoadAssetAtPath<RoomFloorPanelOutput>(AssetDatabase.GetAssetPath(
                room.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>().First(r => r.name == "Generated Floor Panels").sprite));
            Assert.AreEqual(2, output.Chunks.Count);
        }

        [TearDown] public void Cleanup()
        {
            Undo.ClearAll(); EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (folder != null) AssetDatabase.DeleteAsset(folder);
            RoomFloorPanelAssetStore.VerificationRoot = null;
        }

        private static string LegacyHash(RoomFloorPanelOutput revision)
        {
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
            {
                writer.Write(revision.OwnerId); writer.Write(revision.Version);
                foreach (var chunk in revision.Chunks)
                {
                    writer.Write(chunk.Origin.x); writer.Write(chunk.Origin.y); writer.Write(chunk.Texture.width);
                    foreach (var pixel in chunk.Texture.GetPixels32())
                    { writer.Write(pixel.r); writer.Write(pixel.g); writer.Write(pixel.b); writer.Write(pixel.a); }
                }
            }
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(stream.ToArray())).Replace("-", "").ToLowerInvariant();
        }

        private sealed class FeedbackWindow : EditorWindow { }

        [UnityTest] public IEnumerator InspectorFeedbackCoalescesEditsAndStopsWhenDetached()
        {
            var window = ScriptableObject.CreateInstance<FeedbackWindow>();
            var root = new VisualElement(); var refreshes = 0;
            RoomAuthoringPreview.TrackInspectorFeedback(root, () => refreshes++);
            try
            {
                window.Show(); window.rootVisualElement.Add(root);
                var deadline = EditorApplication.timeSinceStartup + 5;
                while (refreshes == 0 && EditorApplication.timeSinceStartup < deadline) yield return null;
                Assert.Greater(refreshes, 0, "Initial attached feedback must render.");
                // Let initial window/asset notifications settle before checking idle behavior.
                for (var i = 0; i < 10; i++) yield return null;
                var before = refreshes;
                var idleUntil = EditorApplication.timeSinceStartup + .7;
                while (EditorApplication.timeSinceStartup < idleUntil) yield return null;
                Assert.AreEqual(before, refreshes, "Idle inspectors must not poll expensive feedback.");
                Undo.RecordObject(room.transform, "Move performance fixture");
                room.transform.position += Vector3.right; Undo.FlushUndoRecordObjects();
                deadline = EditorApplication.timeSinceStartup + 5;
                while (refreshes == before && EditorApplication.timeSinceStartup < deadline) yield return null;
                Assert.Greater(refreshes, before, "Object edits must refresh feedback.");
                root.RemoveFromHierarchy(); before = refreshes;
                Undo.PerformUndo();
                for (var i = 0; i < 5; i++) yield return null;
                Assert.AreEqual(before, refreshes, "Detached inspectors must unsubscribe and cancel queued work.");
            }
            finally { window.Close(); }
        }

        [Test] public void BulkHashPreservesExistingMultiChunkRgbaContentIdentity()
        {
            Assert.AreEqual(LegacyHash(output), RoomFloorPanelAssetStore.ContentHash(output));
            Assert.AreEqual(output.ContentId, RoomFloorPanelAssetStore.ContentHash(output));
            Assert.IsTrue(RoomFloorPanelAssetStore.IsVerified(output));
            var fingerprint = RoomAuthoringValidator.OutputFingerprint(room);
            Assert.AreEqual(fingerprint, RoomAuthoringValidator.OutputFingerprint(room));
            Assert.IsTrue(RoomAuthoringValidator.IsCurrent(room));
            Assert.IsTrue(RoomAuthoringPreview.GetFeedback(room).IsCurrent);
        }

        [Test] public void ConsecutiveAssessmentsDetectUnsavedTextureChangesAndRestorationImmediately()
        {
            var fingerprint = RoomAuthoringValidator.OutputFingerprint(room);
            var texture = output.Chunks[1].Texture; var pixels = texture.GetPixels32();
            try
            {
                var changed = (Color32[])pixels.Clone(); changed[137] = new Color32(222, 111, 33, 177);
                texture.SetPixels32(changed); texture.Apply(false, false);
                Assert.AreEqual(LegacyHash(output), RoomFloorPanelAssetStore.ContentHash(output));
                Assert.IsFalse(RoomFloorPanelAssetStore.IsVerified(output));
                Assert.AreNotEqual(fingerprint, RoomAuthoringValidator.OutputFingerprint(room));
                Assert.IsFalse(RoomAuthoringValidator.IsCurrent(room));
                Assert.IsFalse(RoomAuthoringPreview.GetFeedback(room).IsCurrent);
            }
            finally { texture.SetPixels32(pixels); texture.Apply(false, false); }
            Assert.AreEqual(fingerprint, RoomAuthoringValidator.OutputFingerprint(room));
            Assert.IsTrue(RoomFloorPanelAssetStore.IsVerified(output));
            Assert.IsTrue(RoomAuthoringValidator.IsCurrent(room));
            Assert.IsTrue(RoomAuthoringPreview.GetFeedback(room).IsCurrent);
        }
    }
}
