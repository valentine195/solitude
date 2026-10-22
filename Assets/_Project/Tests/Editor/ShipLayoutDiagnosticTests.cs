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

        [TestCase("disabled")]
        [TestCase("removed")]
        [TestCase("forced-off")]
        [TestCase("sorting")]
        [TestCase("tint")]
        public void ModifiedFloorRenderingIsReadOnlyStaleAndExplicitRebuildRecovers(string change)
        {
            using var f = new RoomAuthoringTestFixtures(32);
            var layout = RoomAuthoringCreation.CreateLayout(f.Scene, f.Style);
            var room = layout.Spaces[0]; RoomAuthoringCreation.CreateSpace(layout);
            Assert.IsTrue(RoomAuthoringOperations.PaintBox(room, new BoundsInt(0, 0, 0, 2, 2, 1), f.Style.FloorTile));
            var neighbor = layout.Spaces[1]; neighbor.transform.localPosition = Vector3.right * 5;
            Assert.IsTrue(RoomAuthoringOperations.PaintBox(neighbor, new BoundsInt(0, 0, 0, 2, 2, 1), f.Style.FloorTile));
            Assert.IsTrue(RoomAuthoringValidator.IsCurrent(layout));
            var authored = RoomAuthoringOperations.ReadCells(room); var neighborCells = RoomAuthoringOperations.ReadCells(neighbor);
            var sentinel = new GameObject("Authored Sentinel"); SceneManager.MoveGameObjectToScene(sentinel, f.Scene);
            sentinel.transform.position = new Vector3(20, 30, 0);
            var floor = room.GeneratedOutput.GetComponentInChildren<Tilemap>(); var renderer = floor.GetComponent<TilemapRenderer>();
            if (change == "disabled") renderer.enabled = false;
            else if (change == "removed") Object.DestroyImmediate(renderer);
            else if (change == "forced-off") renderer.forceRenderingOff = true;
            else if (change == "sorting") renderer.sortingOrder = -100;
            else floor.color = Color.clear;
            var staleFingerprint = RoomAuthoringValidator.OutputFingerprint(layout);
            var saved = JsonUtility.ToJson(layout.GenerationResult); var group = Undo.GetCurrentGroup(); var dirty = f.Scene.isDirty; var calls = 0;
            void Generated(ShipRoomAuthoring _) => calls++;
            RoomAuthoringOperations.GenerationCompleted += Generated;
            try
            {
                Assert.AreEqual(RoomGenerationStatus.Complete, RoomAuthoringPreview.GetFeedback(layout).Assessment.Status);
                Assert.IsFalse(RoomAuthoringValidator.IsCurrent(layout));
                Assert.IsFalse(RoomAuthoringValidator.IsCurrent(room));
                Assert.IsFalse(RoomAuthoringPreview.GetFeedback(layout).IsCurrent);
                Assert.IsFalse(RoomAuthoringPreview.GetFeedback(room).IsCurrent);
                Assert.AreEqual(staleFingerprint, RoomAuthoringValidator.OutputFingerprint(layout));
                Assert.AreEqual(saved, JsonUtility.ToJson(layout.GenerationResult));
                Assert.AreEqual(group, Undo.GetCurrentGroup()); Assert.AreEqual(dirty, f.Scene.isDirty); Assert.AreEqual(0, calls);
                Assert.IsTrue(RoomAuthoringOperations.Rebuild(layout));
                var restored = room.GeneratedOutput.GetComponentInChildren<Tilemap>(); var visible = restored.GetComponent<TilemapRenderer>();
                Assert.IsNotNull(visible); Assert.IsTrue(visible.enabled); Assert.IsFalse(visible.forceRenderingOff);
                Assert.AreEqual(0, visible.sortingOrder); Assert.AreEqual(Color.white, restored.color);
                Assert.IsTrue(RoomAuthoringValidator.IsCurrent(layout)); Assert.IsTrue(RoomAuthoringValidator.IsCurrent(room));
                Assert.AreEqual(4, RoomAuthoringTestFixtures.FloorCount(room));
                CollectionAssert.AreEqual(authored, RoomAuthoringOperations.ReadCells(room));
                CollectionAssert.AreEqual(neighborCells, RoomAuthoringOperations.ReadCells(neighbor));
                Assert.AreEqual(new Vector3(20, 30, 0), sentinel.transform.position); Assert.AreEqual(1, calls);
            }
            finally { RoomAuthoringOperations.GenerationCompleted -= Generated; }
        }

        [TestCase("tint")]
        [TestCase("forced-off")]
        public void HiddenSharedPartitionIsReadOnlyStaleAndExplicitRebuildRecovers(string change)
        {
            var folder = CreateSharedAssets(); var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var oldSelection = Selection.activeObject; var calls = 0;
            void Generated(ShipRoomAuthoring _) => calls++;
            try
            {
                var layout = CreateSharedLayout(scene, folder);
                Assert.AreEqual(RoomGenerationStatus.Complete, layout.GenerationResult.Status, layout.GenerationResult.Summary);
                Assert.IsTrue(RoomAuthoringValidator.IsCurrent(layout));
                var partition = layout.GetComponentsInChildren<SpriteRenderer>().First(r => r.name.StartsWith("Partition", StringComparison.Ordinal));
                var member = partition.GetComponentInParent<RoomGeneratedOutput>().Owner;
                var role = partition.name; var position = partition.transform.localPosition;
                var cells = layout.Spaces.Select(RoomAuthoringOperations.ReadCells).ToArray();
                var sentinel = new GameObject("Authored Sentinel"); sentinel.transform.position = new Vector3(20, 30, 0);
                if (change == "tint") partition.color = Color.clear; else partition.forceRenderingOff = true;
                Assert.IsTrue(EditorSceneManager.SaveScene(scene, folder + "/StalePartition.unity"));
                var serialized = layout.Spaces.Select(r => EditorJsonUtility.ToJson(r)).ToArray();
                var saved = JsonUtility.ToJson(layout.GenerationResult); var fingerprint = RoomAuthoringValidator.OutputFingerprint(layout);
                Selection.activeGameObject = layout.gameObject; var group = Undo.GetCurrentGroup();
                RoomAuthoringOperations.GenerationCompleted += Generated;
                Assert.AreEqual(RoomGenerationStatus.Complete, RoomAuthoringPreview.GetFeedback(layout).Assessment.Status);
                Assert.IsFalse(RoomAuthoringValidator.IsCurrent(layout)); Assert.IsFalse(RoomAuthoringValidator.IsCurrent(member));
                Assert.IsFalse(RoomAuthoringPreview.GetFeedback(layout).IsCurrent); Assert.IsFalse(RoomAuthoringPreview.GetFeedback(member).IsCurrent);
                var editor = UnityEditor.Editor.CreateEditor(layout);
                try { Assert.IsNotNull(editor.CreateInspectorGUI()); } finally { Object.DestroyImmediate(editor); }
                Assert.AreEqual(saved, JsonUtility.ToJson(layout.GenerationResult));
                CollectionAssert.AreEqual(serialized, layout.Spaces.Select(r => EditorJsonUtility.ToJson(r)).ToArray());
                Assert.AreEqual(fingerprint, RoomAuthoringValidator.OutputFingerprint(layout));
                Assert.AreEqual(group, Undo.GetCurrentGroup()); Assert.IsFalse(scene.isDirty); Assert.AreEqual(0, calls);
                Assert.IsTrue(RoomAuthoringOperations.Rebuild(layout));
                var restored = member.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>().Single(r => r.name == role && r.transform.localPosition == position);
                Assert.AreEqual(Color.white, restored.color); Assert.IsFalse(restored.forceRenderingOff); Assert.IsTrue(restored.enabled);
                Assert.IsTrue(RoomAuthoringValidator.IsCurrent(layout)); Assert.IsTrue(RoomAuthoringValidator.IsCurrent(member));
                for (var i = 0; i < layout.Spaces.Count; i++) CollectionAssert.AreEqual(cells[i], RoomAuthoringOperations.ReadCells(layout.Spaces[i]));
                Assert.AreEqual(48, layout.Spaces.Sum(RoomAuthoringTestFixtures.FloorCount));
                Assert.AreEqual(new Vector3(20, 30, 0), sentinel.transform.position); Assert.AreEqual(1, calls);
            }
            finally
            {
                RoomAuthoringOperations.GenerationCompleted -= Generated; RoomAuthoringPreview.ClearSelection(); Selection.activeObject = oldSelection;
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); AssetDatabase.DeleteAsset(folder);
            }
        }

        [TestCase("tint")]
        [TestCase("transform")]
        public void ModifiedFloorCellIsReadOnlyStaleAndExplicitRebuildRecovers(string change)
        {
            var folder = CreateSharedAssets(); var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var oldSelection = Selection.activeObject; var calls = 0;
            void Generated(ShipRoomAuthoring _) => calls++;
            try
            {
                var layout = CreateSharedLayout(scene, folder); var member = layout.Spaces[0];
                Assert.AreEqual(RoomGenerationStatus.Complete, layout.GenerationResult.Status, layout.GenerationResult.Summary);
                Assert.IsTrue(RoomAuthoringValidator.IsCurrent(layout));
                var cells = layout.Spaces.Select(RoomAuthoringOperations.ReadCells).ToArray();
                var sentinel = new GameObject("Authored Sentinel"); sentinel.transform.position = new Vector3(20, 30, 0);
                var floor = member.GeneratedOutput.GetComponentInChildren<Tilemap>(); var cell = new Vector3Int(1, 1, 0);
                var tile = floor.GetTile(cell); Assert.IsNotNull(tile);
                var color = floor.GetColor(cell); var matrix = floor.GetTransformMatrix(cell); var flags = floor.GetTileFlags(cell);
                var originalFingerprint = RoomAuthoringValidator.OutputFingerprint(layout);
                var unlocked = flags & ~(change == "tint" ? TileFlags.LockColor : TileFlags.LockTransform);
                floor.SetTileFlags(cell, unlocked);
                // Capture after unlocking so only the rendering change can satisfy the fingerprint assertion.
                var unlockedFingerprint = RoomAuthoringValidator.OutputFingerprint(layout);
                if (change == "tint") { floor.SetColor(cell, Color.clear); Assert.AreEqual(Color.clear, floor.GetColor(cell)); }
                else { floor.SetTransformMatrix(cell, Matrix4x4.Translate(Vector3.right * 3)); Assert.AreNotEqual(matrix, floor.GetTransformMatrix(cell)); }
                Assert.AreNotEqual(unlockedFingerprint, RoomAuthoringValidator.OutputFingerprint(layout));
                Assert.AreSame(tile, floor.GetTile(cell)); Assert.AreEqual(Color.white, floor.color);
                Assert.IsTrue(EditorSceneManager.SaveScene(scene, folder + "/StaleFloorCell.unity"));
                var serialized = layout.Spaces.Select(r => EditorJsonUtility.ToJson(r)).ToArray();
                var saved = JsonUtility.ToJson(layout.GenerationResult); var snapshot = Snapshot(layout);
                var fingerprint = RoomAuthoringValidator.OutputFingerprint(layout);
                Selection.activeGameObject = member.gameObject; var group = Undo.GetCurrentGroup();
                RoomAuthoringOperations.GenerationCompleted += Generated;
                Assert.AreEqual(RoomGenerationStatus.Complete, RoomAuthoringPreview.GetFeedback(layout).Assessment.Status);
                Assert.IsFalse(RoomAuthoringValidator.IsCurrent(layout)); Assert.IsFalse(RoomAuthoringValidator.IsCurrent(member));
                Assert.IsFalse(RoomAuthoringPreview.GetFeedback(layout).IsCurrent); Assert.IsFalse(RoomAuthoringPreview.GetFeedback(member).IsCurrent);
                foreach (var target in new Object[] { layout, member })
                {
                    var editor = UnityEditor.Editor.CreateEditor(target);
                    try { Assert.IsNotNull(editor.CreateInspectorGUI()); } finally { Object.DestroyImmediate(editor); }
                }
                Assert.AreEqual(saved, JsonUtility.ToJson(layout.GenerationResult));
                CollectionAssert.AreEqual(serialized, layout.Spaces.Select(r => EditorJsonUtility.ToJson(r)).ToArray());
                CollectionAssert.AreEqual(snapshot, Snapshot(layout));
                Assert.AreEqual(fingerprint, RoomAuthoringValidator.OutputFingerprint(layout));
                Assert.AreEqual(group, Undo.GetCurrentGroup()); Assert.IsFalse(scene.isDirty); Assert.AreEqual(0, calls);
                Assert.IsTrue(RoomAuthoringOperations.Rebuild(layout));
                var restored = member.GeneratedOutput.GetComponentInChildren<Tilemap>();
                Assert.AreSame(tile, restored.GetTile(cell)); Assert.AreEqual(color, restored.GetColor(cell));
                Assert.AreEqual(matrix, restored.GetTransformMatrix(cell)); Assert.AreEqual(flags, restored.GetTileFlags(cell));
                Assert.AreEqual(originalFingerprint, RoomAuthoringValidator.OutputFingerprint(layout));
                Assert.IsTrue(RoomAuthoringValidator.IsCurrent(layout)); Assert.IsTrue(RoomAuthoringValidator.IsCurrent(member));
                for (var i = 0; i < layout.Spaces.Count; i++) CollectionAssert.AreEqual(cells[i], RoomAuthoringOperations.ReadCells(layout.Spaces[i]));
                Assert.AreEqual(48, layout.Spaces.Sum(RoomAuthoringTestFixtures.FloorCount));
                Assert.AreEqual(new Vector3(20, 30, 0), sentinel.transform.position); Assert.AreEqual(1, calls);
            }
            finally
            {
                RoomAuthoringOperations.GenerationCompleted -= Generated; RoomAuthoringPreview.ClearSelection(); Selection.activeObject = oldSelection;
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); AssetDatabase.DeleteAsset(folder);
            }
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
