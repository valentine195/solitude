using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Tilemaps;
using SOLITUDE.World.RoomAuthoring;
using UnityEngine;

namespace SOLITUDE.Editor.RoomAuthoring
{
    // A synchronous validation call owns its snapshot. Never retain this across edits,
    // Undo, scene changes, or generated-object mutations: those require a fresh snapshot.
    internal sealed class RoomValidationSnapshot
    {
        internal readonly ShipRoomAuthoring[] Rooms;
        internal readonly ShipRoomAuthoring[] LoadedRooms;
        internal readonly RoomGeneratedOutput[] Outputs;
        internal readonly Dictionary<string, int> IdentityCounts = new Dictionary<string, int>();
        internal readonly Dictionary<Tilemap, int> OccupancyReferences = new Dictionary<Tilemap, int>();
        internal readonly Dictionary<RoomGeneratedOutput, int> OutputReferences = new Dictionary<RoomGeneratedOutput, int>();
        internal readonly Dictionary<ShipRoomAuthoring, int> OwnedOutputs = new Dictionary<ShipRoomAuthoring, int>();
        internal readonly Dictionary<Transform, string> AuthoredDescendants = new Dictionary<Transform, string>();
        internal readonly HashSet<Transform> NestedOwnership = new HashSet<Transform>();

        internal RoomValidationSnapshot(ShipRoomAuthoring room)
        {
            Rooms = room != null && room.gameObject.scene.IsValid() && room.gameObject.scene.isLoaded
                ? room.gameObject.scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<ShipRoomAuthoring>(true)).ToArray()
                : Array.Empty<ShipRoomAuthoring>();
            LoadedRooms = Resources.FindObjectsOfTypeAll<ShipRoomAuthoring>()
                .Where(other => other.gameObject.scene.IsValid() && other.gameObject.scene.isLoaded).ToArray();
            Outputs = Resources.FindObjectsOfTypeAll<RoomGeneratedOutput>()
                .Where(output => output.gameObject.scene.IsValid() && output.gameObject.scene.isLoaded).ToArray();
            foreach (var other in Rooms)
            {
                if (other.RoomId != null) Count(IdentityCounts, other.RoomId);
                if (other.Occupancy != null) Count(OccupancyReferences, other.Occupancy);
                if (other.GeneratedOutput != null) Count(OutputReferences, other.GeneratedOutput);
            }
            foreach (var other in LoadedRooms)
            {
                MarkAncestors(other.transform, NestedOwnership);
                if (other.Occupancy == null) continue;
                for (var ancestor = other.Occupancy.transform; ancestor != null; ancestor = ancestor.parent)
                    if (!AuthoredDescendants.ContainsKey(ancestor)) AuthoredDescendants.Add(ancestor, other.RoomId);
            }
            foreach (var output in Outputs)
            {
                if (output.Owner != null) Count(OwnedOutputs, output.Owner);
                MarkAncestors(output.transform.parent, NestedOwnership);
            }
        }
        private static void Count<T>(Dictionary<T, int> counts, T key)
        { counts.TryGetValue(key, out int count); counts[key] = count + 1; }

        private static void MarkAncestors(Transform transform, HashSet<Transform> marked)
        { for (var ancestor = transform; ancestor != null; ancestor = ancestor.parent) marked.Add(ancestor); }
    }
}
