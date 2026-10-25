using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SOLITUDE.Editor.RoomAuthoring;
using SOLITUDE.World.RoomAuthoring;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SOLITUDE.Tests.RoomAuthoring
{
    public sealed class RoomFloorPanelWorkflowTests
    {
        [Test] public void TemporaryPanelStyleCreatesFlatNativeFloorWithoutRewritingSourceOrSceneAndRefusesOverwrite()
        {
            Assert.IsTrue(Environment.GetCommandLineArgs().Contains("-solitudeRoomAuthoringVerification"));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var sourceFolder = RoomPersistenceTests.CreateAssets(); var room = RoomPersistenceTests.CreateRoom(scene, sourceFolder);
            var original = room.Style;
            var source = EditorJsonUtility.ToJson(original); var sceneState = EditorJsonUtility.ToJson(room);
            var output = RoomUndoRedoTests.Snapshot(room); var group = Undo.GetCurrentGroup(); var dirty = scene.isDirty;
            var folder = ShipLayoutSmokeSetup.FolderPrefix + Guid.NewGuid().ToString("N");
            try
            {
                var style = ShipLayoutSmokeSetup.CreateFloorPanelStyle(original, folder);
                Assert.AreNotEqual(original.StyleId, style.StyleId);
                Assert.AreEqual(source, EditorJsonUtility.ToJson(original)); Assert.AreEqual(sceneState, EditorJsonUtility.ToJson(room));
                CollectionAssert.AreEqual(output, RoomUndoRedoTests.Snapshot(room)); Assert.AreEqual(group, Undo.GetCurrentGroup()); Assert.AreEqual(dirty, scene.isDirty);
                Assert.AreEqual(0, style.Rendering.FloorOrder); Assert.AreEqual(1, style.Rendering.DetailOrder); Assert.AreEqual(2, style.Rendering.ArchitectureOrder);
                var tile = (UnityEngine.Tilemaps.Tile)style.FloorTile; Assert.AreEqual(32, tile.sprite.pixelsPerUnit);
                var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(tile.sprite));
                Assert.AreEqual(FilterMode.Point, importer.filterMode); Assert.IsFalse(importer.mipmapEnabled);
                Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression);
                var png = new Texture2D(2, 2); png.LoadImage(File.ReadAllBytes(folder + "/FlatFloor.png"));
                try { Assert.AreEqual(1, png.GetPixels32().Distinct().Count(), "No hand-authored panel grid under the generated seams."); }
                finally { Object.DestroyImmediate(png); }
                var bytes = File.ReadAllBytes(folder + "/PanelStyle.asset"); var guid = AssetDatabase.AssetPathToGUID(folder + "/PanelStyle.asset");
                Assert.Throws<InvalidOperationException>(() => ShipLayoutSmokeSetup.CreateFloorPanelStyle(original, folder));
                CollectionAssert.AreEqual(bytes, File.ReadAllBytes(folder + "/PanelStyle.asset")); Assert.AreEqual(guid, AssetDatabase.AssetPathToGUID(folder + "/PanelStyle.asset"));
            }
            finally { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); AssetDatabase.DeleteAsset(folder); AssetDatabase.DeleteAsset(sourceFolder); }
        }
    }
}
