using System;
using System.Linq;
using SOLITUDE.World.RoomAuthoring;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace SOLITUDE.Editor.RoomAuthoring
{
    public static class RoomAuthoringCreation
    {
        [MenuItem("GameObject/SOLITUDE/Room Authoring", false, 10)]
        private static void CreateFromMenu()
        {
            var room = Create(SceneManager.GetActiveScene());
            Selection.activeGameObject = room.gameObject;
        }

        [MenuItem("GameObject/SOLITUDE/Ship Layout Authoring", false, 9)]
        private static void CreateLayoutFromMenu()
        {
            Selection.activeGameObject = CreateLayout(SceneManager.GetActiveScene()).gameObject;
        }

        public static ShipLayoutAuthoring CreateLayout(Scene scene, RoomStyleDefinition style = null,
            LogicalSpaceType type = LogicalSpaceType.Room)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !scene.IsValid() || !scene.isLoaded)
                throw new InvalidOperationException("Create a layout in a loaded Edit Mode scene.");
            Undo.IncrementCurrentGroup();
            var group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Create Ship Layout");
            var root = new GameObject("ShipLayout", typeof(ShipLayoutAuthoring));
            SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Create Ship Layout");
            var layout = root.GetComponent<ShipLayoutAuthoring>();
            var room = Create(scene);
            Undo.SetTransformParent(room.transform, root.transform, "Create Ship Layout");
            room.InitializeLayout(layout, type);
            room.GeneratedOutput.Initialize(room, layout);
            layout.Initialize(Guid.NewGuid().ToString("N"), style, room, room.GeneratedOutput);
            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(scene);
            return layout;
        }

        public static ShipRoomAuthoring Create(Scene scene, RoomStyleDefinition style = null)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !scene.IsValid() || !scene.isLoaded)
                throw new InvalidOperationException("Create a room in a loaded Edit Mode scene.");
            var root = new GameObject("ShipRoom", typeof(Grid), typeof(ShipRoomAuthoring));
            SceneManager.MoveGameObjectToScene(root, scene);
            root.GetComponent<Grid>().cellSize = Vector3.one;
            var mask = new GameObject("Authored Occupancy", typeof(Tilemap));
            mask.transform.SetParent(root.transform, false);
            mask.tag = "EditorOnly";
            mask.SetActive(false);
            var generated = new GameObject("Generated Architecture", typeof(RoomGeneratedOutput));
            generated.transform.SetParent(root.transform, false);
            var room = root.GetComponent<ShipRoomAuthoring>();
            var output = generated.GetComponent<RoomGeneratedOutput>();
            room.Initialize(Guid.NewGuid().ToString("N"), style, mask.GetComponent<Tilemap>(), output);
            output.Initialize(room);
            Undo.RegisterCreatedObjectUndo(root, "Create Room");
            EditorSceneManager.MarkSceneDirty(scene);
            return room;
        }

        public static ShipRoomAuthoring CreateSpace(ShipLayoutAuthoring layout, LogicalSpaceType type = LogicalSpaceType.Room)
        {
            var issue = RoomAuthoringOperations.GetSafetyIssue(layout);
            if (issue != null) throw new InvalidOperationException("Cannot add space: " + issue);
            RoomAuthoringOperations.CancelActiveEdit();
            Undo.IncrementCurrentGroup();
            var group = Undo.GetCurrentGroup();
            const string action = "Create Layout Space";
            Undo.SetCurrentGroupName(action);
            try
            {
                var room = Create(layout.gameObject.scene);
                Undo.SetTransformParent(room.transform, layout.transform, action);
                Undo.RecordObject(room.transform, action);
                room.transform.localPosition = Vector3.zero;
                Undo.RegisterCompleteObjectUndo(room, action);
                Undo.RegisterCompleteObjectUndo(room.GeneratedOutput, action);
                Undo.RegisterCompleteObjectUndo(layout, action);
                room.InitializeLayout(layout, type);
                room.GeneratedOutput.Initialize(room, layout);
                layout.AddSpace(room);
                Undo.FlushUndoRecordObjects();
                Undo.CollapseUndoOperations(group);
                EditorUtility.SetDirty(layout);
                return room;
            }
            catch { Undo.RevertAllDownToGroup(group); throw; }
            finally { Undo.IncrementCurrentGroup(); }
        }

        public static string GetAdoptionIssue(ShipRoomAuthoring room)
        {
            var issue = RoomAuthoringOperations.GetSafetyIssue(room);
            if (issue != null) return issue;
            if (room.Layout != null) return "This space already belongs to a layout.";
            if (EditorUtility.IsPersistent(room) || PrefabUtility.IsPartOfPrefabInstance(room) ||
                PrefabStageUtility.GetPrefabStage(room.gameObject) != null)
                return "Adopt a scene room outside a prefab instance.";
            if (room.GetComponentsInChildren<ShipRoomAuthoring>(true).Any(other => other != room) ||
                room.GetComponentInParent<ShipLayoutAuthoring>(true) != null ||
                room.GetComponentsInChildren<ShipLayoutAuthoring>(true).Length != 0)
                return "Resolve nested room/layout ownership before adoption.";
            var assessment = RoomAuthoringValidator.Assess(room, out _);
            if (assessment.Status == RoomGenerationStatus.Refused) return assessment.Summary;
            return RoomAuthoringValidator.GetAdoptionOutputIssue(room);
        }

        public static ShipLayoutAuthoring AdoptIntoNewLayout(ShipRoomAuthoring room)
        {
            var issue = GetAdoptionIssue(room);
            if (issue != null) throw new InvalidOperationException("Cannot adopt room: " + issue);
            var wasCurrent = RoomAuthoringValidator.IsCurrent(room);
            var previousPath = room.name;
            for (var parent = room.transform.parent; parent != null; parent = parent.parent)
                previousPath = parent.name + "/" + previousPath;
            Undo.IncrementCurrentGroup();
            var group = Undo.GetCurrentGroup();
            const string action = "Adopt Room into Ship Layout";
            Undo.SetCurrentGroupName(action);
            try
            {
                var root = new GameObject("ShipLayout", typeof(ShipLayoutAuthoring));
                SceneManager.MoveGameObjectToScene(root, room.gameObject.scene);
                Undo.RegisterCreatedObjectUndo(root, action);
                var layout = root.GetComponent<ShipLayoutAuthoring>();
                Undo.SetTransformParent(room.transform, root.transform, action);
                Undo.RegisterCompleteObjectUndo(room, action);
                Undo.RegisterCompleteObjectUndo(room.GeneratedOutput, action);
                Undo.RegisterCompleteObjectUndo(layout, action);
                room.InitializeLayout(layout, room.SpaceType);
                room.GeneratedOutput.Initialize(room, layout);
                layout.Initialize(Guid.NewGuid().ToString("N"), room.Style, room, room.GeneratedOutput);
                room.GeneratedOutput.RecordAdoption(previousPath);
                if (wasCurrent)
                {
                    var result = RoomAuthoringValidator.Assess(layout, out _);
                    result.OutputFingerprint = RoomAuthoringValidator.OutputFingerprint(room);
                    room.SetGenerationResult(result);
                }
                // Undo restores inline serialized objects in place; layout deletion must not clear the room's result.
                layout.SetGenerationResult(room.GenerationResult == null ? null :
                    JsonUtility.FromJson<RoomGenerationResult>(JsonUtility.ToJson(room.GenerationResult)));
                EditorUtility.SetDirty(room);
                EditorUtility.SetDirty(room.GeneratedOutput);
                EditorUtility.SetDirty(layout);
                Undo.FlushUndoRecordObjects();
                Undo.CollapseUndoOperations(group);
                EditorSceneManager.MarkSceneDirty(room.gameObject.scene);
                return layout;
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                throw;
            }
            finally { Undo.IncrementCurrentGroup(); }
        }

        public static string GetOwnershipIssue(ShipRoomAuthoring room, bool allowMissingOutput = false)
            => GetOwnershipIssue(room, allowMissingOutput, null);

        internal static string GetOwnershipIssue(ShipRoomAuthoring room, bool allowMissingOutput, RoomValidationSnapshot snapshot)
        {
            if (room == null || !room.gameObject.scene.IsValid() || !room.gameObject.scene.isLoaded)
                return "Select a room in a loaded scene.";
            snapshot ??= new RoomValidationSnapshot(room);
            var issue = GetIndependentOwnershipIssue(room, allowMissingOutput, snapshot);
            return issue ?? GetMembershipIssue(room, allowMissingOutput);
        }

        private static string GetIndependentOwnershipIssue(ShipRoomAuthoring room, bool allowMissingOutput, RoomValidationSnapshot snapshot)
        {
            if (room == null || !room.gameObject.scene.IsValid() || !room.gameObject.scene.isLoaded)
                return "Select a room in a loaded scene.";
            if (string.IsNullOrWhiteSpace(room.RoomId)) return "Room identity is missing.";
            if (!snapshot.IdentityCounts.TryGetValue(room.RoomId, out int identities) || identities != 1) return "Room identity is ambiguous in this scene.";
            if (room.Occupancy == null) return "Room occupancy reference is missing.";
            if (room.Occupancy.transform == room.transform || !room.Occupancy.transform.IsChildOf(room.transform))
                return "Occupancy must belong to this room's hierarchy.";
            if (snapshot.OccupancyReferences[room.Occupancy] > 1)
                return "Room occupancy is referenced by another room.";
            var outputs = snapshot.Outputs;
            if (room.GeneratedOutput == null)
            {
                if (!allowMissingOutput) return "Room output reference is missing.";
                if (outputs.Any(output => output.Owner == room || output.transform.IsChildOf(room.transform)))
                    return "Existing generated ownership must be resolved before replacing a missing output reference.";
                return null;
            }
            if (room.GeneratedOutput.Owner != room || room.GeneratedOutput.transform == room.transform ||
                !room.GeneratedOutput.transform.IsChildOf(room.transform)) return "Generated output is not owned by this room.";
            if (!snapshot.OwnedOutputs.TryGetValue(room, out int ownedOutputs) || ownedOutputs != 1)
                return "Generated output ownership is ambiguous: this room must own exactly one output root.";
            if (room.Occupancy.transform.IsChildOf(room.GeneratedOutput.transform) ||
                room.GeneratedOutput.transform.IsChildOf(room.Occupancy.transform)) return "Occupancy and generated output must be separate.";
            if (snapshot.OutputReferences[room.GeneratedOutput] > 1)
                return "Room occupancy or output is referenced by another room.";
            return null;
        }

        private static ShipLayoutAuthoring[] LoadedLayouts() => Resources.FindObjectsOfTypeAll<ShipLayoutAuthoring>()
            .Where(layout => layout.gameObject.scene.IsValid() && layout.gameObject.scene.isLoaded).ToArray();

        private static string GetMembershipIssue(ShipRoomAuthoring room, bool allowMissingOutput)
        {
            var containing = LoadedLayouts().Where(layout => layout.Spaces.Contains(room)).ToArray();
            if (room.Layout == null)
                return containing.Length != 0 || (room.GeneratedOutput != null && room.GeneratedOutput.LayoutOwner != null)
                    ? "Space layout membership is inconsistent." : null;
            if (containing.Length != 1 || containing[0] != room.Layout)
                return "Space layout membership is ambiguous or inconsistent.";
            return GetLayoutOwnershipIssue(room.Layout, allowMissingOutput);
        }

        public static string GetLayoutOwnershipIssue(ShipLayoutAuthoring layout, bool allowMissingOutput = false)
        {
            if (layout == null || !layout.gameObject.scene.IsValid() || !layout.gameObject.scene.isLoaded)
                return "Select a layout in a loaded scene.";
            var layouts = LoadedLayouts();
            if (string.IsNullOrWhiteSpace(layout.LayoutId)) return "Layout identity is missing.";
            if (layouts.Count(other => other.gameObject.scene == layout.gameObject.scene && other.LayoutId == layout.LayoutId) != 1)
                return "Layout identity is ambiguous in this scene.";
            if (layout.Spaces.Count == 0 || layout.Spaces.Any(room => room == null) ||
                layout.Spaces.Distinct().Count() != layout.Spaces.Count)
                return "Layout requires distinct, non-null spaces.";
            var snapshot = new RoomValidationSnapshot(layout.Spaces[0]);
            foreach (var room in layout.Spaces)
            {
                if (room.Layout != layout || room.gameObject.scene != layout.gameObject.scene ||
                    !room.transform.IsChildOf(layout.transform) || layouts.Count(other => other.Spaces.Contains(room)) != 1)
                    return "Layout space membership is ambiguous or inconsistent.";
                var issue = GetIndependentOwnershipIssue(room, allowMissingOutput, snapshot);
                if (issue != null) return issue;
                if (room.GeneratedOutput != null && room.GeneratedOutput.LayoutOwner != layout)
                    return "Layout space output association is inconsistent.";
            }
            if (layout.gameObject.scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<ShipRoomAuthoring>(true))
                .Any(room => room.Layout == layout && !layout.Spaces.Contains(room)))
                return "Layout has a space with an unlisted membership back-reference.";
            var outputs = Resources.FindObjectsOfTypeAll<RoomGeneratedOutput>()
                .Where(output => output.gameObject.scene.IsValid() && output.gameObject.scene.isLoaded).ToArray();
            if (outputs.Any(output => output.LayoutOwner == layout &&
                (!layout.Spaces.Contains(output.Owner) || output.Owner.GeneratedOutput != output)))
                return "Layout generated output ownership is ambiguous.";
            if (layout.GeneratedOutput == null)
                return allowMissingOutput && layout.Spaces[0].GeneratedOutput == null
                    ? null : "Layout output reference is missing or ambiguous.";
            if (layout.GeneratedOutput.LayoutOwner != layout || !layout.Spaces.Contains(layout.GeneratedOutput.Owner) ||
                layout.GeneratedOutput.Owner.GeneratedOutput != layout.GeneratedOutput ||
                layouts.Any(other => other != layout && other.GeneratedOutput == layout.GeneratedOutput))
                return "Layout generated output ownership is ambiguous.";
            return null;
        }
    }
}
