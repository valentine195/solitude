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

        public static string GetOwnershipIssue(ShipRoomAuthoring room, bool allowMissingOutput = false)
        {
            if (room == null || !room.gameObject.scene.IsValid() || !room.gameObject.scene.isLoaded)
                return "Select a room in a loaded scene.";
            if (string.IsNullOrWhiteSpace(room.RoomId)) return "Room identity is missing.";
            var rooms = room.gameObject.scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<ShipRoomAuthoring>(true)).ToArray();
            if (rooms.Count(other => other.RoomId == room.RoomId) != 1) return "Room identity is ambiguous in this scene.";
            if (room.Occupancy == null) return "Room occupancy reference is missing.";
            if (room.Occupancy.transform == room.transform || !room.Occupancy.transform.IsChildOf(room.transform))
                return "Occupancy must belong to this room's hierarchy.";
            if (rooms.Any(other => other != room && other.Occupancy == room.Occupancy))
                return "Room occupancy is referenced by another room.";
            var outputs = Resources.FindObjectsOfTypeAll<RoomGeneratedOutput>()
                .Where(output => output.gameObject.scene.IsValid() && output.gameObject.scene.isLoaded).ToArray();
            if (room.GeneratedOutput == null)
            {
                if (!allowMissingOutput) return "Room output reference is missing.";
                if (outputs.Any(output => output.Owner == room || output.transform.IsChildOf(room.transform)))
                    return "Existing generated ownership must be resolved before replacing a missing output reference.";
                return null;
            }
            if (room.GeneratedOutput.Owner != room || room.GeneratedOutput.transform == room.transform ||
                !room.GeneratedOutput.transform.IsChildOf(room.transform)) return "Generated output is not owned by this room.";
            if (outputs.Count(output => output.Owner == room) != 1)
                return "Generated output ownership is ambiguous: this room must own exactly one output root.";
            if (room.Occupancy.transform.IsChildOf(room.GeneratedOutput.transform) ||
                room.GeneratedOutput.transform.IsChildOf(room.Occupancy.transform)) return "Occupancy and generated output must be separate.";
            if (rooms.Any(other => other != room && (other.Occupancy == room.Occupancy || other.GeneratedOutput == room.GeneratedOutput)))
                return "Room occupancy or output is referenced by another room.";
            return null;
        }
    }
}
