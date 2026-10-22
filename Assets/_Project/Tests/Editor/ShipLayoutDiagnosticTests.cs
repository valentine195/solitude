using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SOLITUDE.Editor.RoomAuthoring;
using SOLITUDE.World.RoomAuthoring;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace SOLITUDE.Tests.RoomAuthoring
{
    public sealed class ShipLayoutDiagnosticTests
    {
        public static string CreateSharedAssets()
        {
            var folder = "Assets/_Project/Tests/RoomAuthoringFixture" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets/_Project/Tests", Path.GetFileName(folder));
            Sprite Import(string name, int height, RectInt ink, Color color)
            {
                var texture = new Texture2D(32, height);
                texture.SetPixels(Enumerable.Repeat(Color.clear, 32 * height).ToArray());
                for (var y = ink.y; y < ink.yMax; y++) for (var x = ink.x; x < ink.xMax; x++) texture.SetPixel(x, y, color);
                texture.Apply(); var path = folder + "/" + name + ".png";
                File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture); AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 32; importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
                return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            var floor = ScriptableObject.CreateInstance<Tile>();
            floor.sprite = Import("Floor", 32, new RectInt(0, 0, 32, 32), new Color(.28f, .42f, .52f));
            AssetDatabase.CreateAsset(floor, folder + "/Floor.asset");
            var horizontal = Import("Horizontal", 32, new RectInt(0, 0, 32, 1), Color.green);
            var north = Import("North", 64, new RectInt(0, 0, 32, 1), Color.cyan);
            var vertical = Import("Vertical", 32, new RectInt(0, 0, 1, 32), Color.green);
            var dot = Import("Vertex", 32, new RectInt(0, 0, 1, 1), Color.yellow);
            RoomStyleDefinition.Module Module(Sprite sprite, RectInt footprint, Vector2 anchor) => new RoomStyleDefinition.Module
            { Sprite = sprite, CanvasPixels = Vector2Int.RoundToInt(sprite.rect.size), RepeatPixels = 32, AnchorPixels = anchor,
                HasFootprint = true, FootprintsPixels = new[] { footprint } };
            var line = new RectInt(0, 0, 32, 1); var point = new RectInt(0, 0, 1, 1);
            var style = ScriptableObject.CreateInstance<RoomStyleDefinition>();
            style.DisplayName = "Shared geometry placeholder — production art not qualified"; style.FloorTile = floor; style.FloorEdgeInsetPixels = 1;
            style.NorthWalls = new[] { Module(north, line, Vector2.zero) };
            style.WestCap = Module(vertical, new RectInt(0, 0, 1, 32), new Vector2(.5f, 0));
            style.EastCap = Module(vertical, new RectInt(0, 0, 1, 32), new Vector2(.5f, 0));
            style.SouthCap = Module(horizontal, line, new Vector2(0, .5f));
            style.Northwest = Module(dot, point, new Vector2(1, 0)); style.Northeast = Module(dot, point, Vector2.zero);
            style.Southwest = Module(dot, point, Vector2.one); style.Southeast = Module(dot, point, new Vector2(0, 1));
            style.ConcaveNorthwest = Module(dot, point, new Vector2(1, 0)); style.ConcaveNortheast = Module(dot, point, Vector2.zero);
            style.ConcaveSouthwest = Module(dot, point, Vector2.one); style.ConcaveSoutheast = Module(dot, point, new Vector2(0, 1));
            style.JunctionModules = Enum.GetValues(typeof(LayoutJunctionRole)).Cast<LayoutJunctionRole>().Select(role =>
                new RoomStyleDefinition.JunctionModule { Role = role, Artwork = Module(dot, point, new Vector2(.5f, .5f)) }).ToArray();
            AssetDatabase.CreateAsset(style, folder + "/Style.asset"); AssetDatabase.SaveAssets(); return folder;
        }

        public static ShipLayoutAuthoring CreateSharedLayout(Scene scene, string folder)
        {
            var style = AssetDatabase.LoadAssetAtPath<RoomStyleDefinition>(folder + "/Style.asset");
            var layout = RoomAuthoringCreation.CreateLayout(scene, style, LogicalSpaceType.Corridor); layout.name = "Shared Walkthrough Layout";
            layout.transform.position = new Vector3(-10, -8, 0);
            var a = layout.Spaces[0]; a.name = "Corridor A";
            var b = RoomAuthoringCreation.CreateSpace(layout); b.name = "Room B"; b.transform.localPosition = Vector3.right * 4;
            var c = RoomAuthoringCreation.CreateSpace(layout); c.name = "Room C"; c.transform.localPosition = new Vector3(4, 3, 0);
            RoomAuthoringOperations.PaintBox(a, new BoundsInt(0, 0, 0, 4, 6, 1), style.FloorTile);
            RoomAuthoringOperations.PaintBox(b, new BoundsInt(0, 0, 0, 4, 3, 1), style.FloorTile);
            RoomAuthoringOperations.PaintBox(c, new BoundsInt(0, 0, 0, 4, 3, 1), style.FloorTile);
            a.AuthoringMetadata = "Corridor A fixture metadata";
            b.AuthoringMetadata = "Room B fixture metadata";
            c.AuthoringMetadata = "Room C fixture metadata";
            return layout;
        }

        public static string[] Snapshot(ShipLayoutAuthoring layout) => layout.Spaces.OrderBy(r => r.RoomId, StringComparer.Ordinal)
            .SelectMany(r => new[] { r.RoomId, r.SpaceType.ToString(), r.AuthoringMetadata, AssetDatabase.GetAssetPath(r.Style) }
                .Concat(RoomUndoRedoTests.Snapshot(r))).Concat(new[] { JsonUtility.ToJson(layout.GenerationResult) }).ToArray();

        [TestCase(false)]
        [TestCase(true)]
        public void LayoutAndMemberFocusUseLayoutCoordinatesAndAreReadOnly(bool selectMember)
        {
            var folder = CreateSharedAssets(); var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var oldSelection = Selection.activeObject; var calls = 0;
            void Generated(ShipRoomAuthoring _) => calls++;
            try
            {
                var layout = CreateSharedLayout(scene, folder); var member = layout.Spaces[1];
                member.Style.JunctionModules.Single(b => b.Role == LayoutJunctionRole.TeeEast).Artwork.Sprite = null;
                Assert.IsFalse(RoomAuthoringOperations.Rebuild(layout));
                Assert.IsTrue(EditorSceneManager.SaveScene(scene, folder + "/Diagnostics.unity"));
                Selection.activeGameObject = selectMember ? member.gameObject : layout.gameObject;
                var issue = layout.GenerationResult.Issues.First(i => i.Field == "PartitionEndpoint" && i.Start == new Vector2Int(4, 3));
                var before = Snapshot(layout); var objects = layout.GetComponentsInChildren<Transform>(true);
                var group = Undo.GetCurrentGroup(); RoomAuthoringOperations.GenerationCompleted += Generated;
                var feedback = RoomAuthoringPreview.GetFeedback(layout);
                Assert.IsTrue(feedback.IsCurrent); Assert.AreEqual(RoomGenerationStatus.IncompletePreview, feedback.Assessment.Status);
                Assert.IsTrue(selectMember ? RoomAuthoringPreview.SelectIssue(member, issue.Id, false) : RoomAuthoringPreview.SelectIssue(layout, issue.Id, false));
                var bounds = selectMember ? RoomAuthoringPreview.IssueBounds(member, issue) : RoomAuthoringPreview.IssueBounds(layout, issue);
                Assert.AreEqual(new Vector3(-6, -5, 0), bounds.center, "Member offset must not be applied twice.");
                Assert.AreEqual(issue.Id, RoomAuthoringPreview.SelectedIssueId);
                var editor = UnityEditor.Editor.CreateEditor(selectMember ? (Object)member : layout);
                try { Assert.IsNotNull(editor.CreateInspectorGUI()); } finally { Object.DestroyImmediate(editor); }
                CollectionAssert.AreEqual(before, Snapshot(layout)); CollectionAssert.AreEqual(objects, layout.GetComponentsInChildren<Transform>(true));
                Assert.IsFalse(scene.isDirty); Assert.AreEqual(group, Undo.GetCurrentGroup()); Assert.AreEqual(0, calls);
                Assert.IsTrue(RoomAuthoringPreview.IssueLabel(issue).Contains(issue.RoomId));
            }
            finally
            {
                RoomAuthoringOperations.GenerationCompleted -= Generated; RoomAuthoringPreview.ClearSelection(); Selection.activeObject = oldSelection;
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); AssetDatabase.DeleteAsset(folder);
            }
        }

        [Test] public void MultiSpaceMissingFloorGuidesAndRefusedOverlapRemainLocatedWithoutMutation()
        {
            using var f = new RoomAuthoringTestFixtures(32);
            var layout = RoomAuthoringCreation.CreateLayout(f.Scene, f.Style); var a = layout.Spaces[0]; var b = RoomAuthoringCreation.CreateSpace(layout);
            b.transform.localPosition = Vector3.right * 4; a.Occupancy.SetTile(Vector3Int.zero, f.Style.FloorTile); b.Occupancy.SetTile(Vector3Int.zero, f.Style.FloorTile);
            f.Style.FloorTile = null; RoomAuthoringOperations.Rebuild(layout);
            CollectionAssert.AreEquivalent(new[] { Vector2Int.zero, Vector2Int.right * 4 }, RoomAuthoringPreview.GetFeedback(layout).MissingFloorCells);
            CollectionAssert.AreEqual(new[] { Vector2Int.zero }, RoomAuthoringPreview.GetFeedback(b).MissingFloorCells);
            b.transform.localPosition = Vector3.zero; Selection.activeGameObject = layout.gameObject;
            var before = Snapshot(layout); var group = Undo.GetCurrentGroup(); var feedback = RoomAuthoringPreview.GetFeedback(layout);
            Assert.AreEqual(RoomGenerationStatus.Refused, feedback.Assessment.Status); Assert.IsFalse(feedback.IsCurrent);
            var issue = feedback.Assessment.Issues.Single(); Assert.IsTrue(issue.HasRegion);
            Assert.IsTrue(RoomAuthoringPreview.SelectIssue(layout, issue.Id, false));
            CollectionAssert.AreEqual(before, Snapshot(layout)); Assert.AreEqual(group, Undo.GetCurrentGroup());
            RoomAuthoringPreview.ClearSelection(); Selection.activeObject = null;
        }

        [Test] public void StaleAssessmentAndCorrectionClearOnlyObsoleteSelection()
        {
            using var f = new RoomAuthoringTestFixtures(32);
            var layout = RoomAuthoringCreation.CreateLayout(f.Scene, f.Style); RoomAuthoringCreation.CreateSpace(layout);
            var room = layout.Spaces[0]; room.Occupancy.SetTile(Vector3Int.zero, f.Style.FloorTile);
            var sprite = f.Style.Northwest.Sprite; f.Style.Northwest.Sprite = null; RoomAuthoringOperations.Rebuild(layout);
            Selection.activeGameObject = layout.gameObject;
            var issue = layout.GenerationResult.Issues.First(i => i.Field == "Northwest");
            Assert.IsTrue(RoomAuthoringPreview.SelectIssue(layout, issue.Id, false));
            f.Style.Northwest.Sprite = sprite; Assert.IsFalse(RoomAuthoringPreview.GetFeedback(layout).IsCurrent);
            Assert.IsNull(RoomAuthoringPreview.SelectedIssueId); Assert.IsTrue(RoomAuthoringOperations.Rebuild(layout));
            Assert.IsTrue(RoomAuthoringPreview.GetFeedback(layout).IsCurrent); Assert.IsEmpty(layout.GenerationResult.Issues);
            RoomAuthoringPreview.ClearSelection(); Selection.activeObject = null;
        }
    }
}
