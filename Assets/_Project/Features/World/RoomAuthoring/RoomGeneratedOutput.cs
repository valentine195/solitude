using System;
using UnityEngine;

namespace SOLITUDE.World.RoomAuthoring
{
    [DisallowMultipleComponent]
    public sealed class RoomGeneratedOutput : MonoBehaviour
    {
        [SerializeField] private ShipRoomAuthoring owner;
        [SerializeField] private ShipLayoutAuthoring layoutOwner;
        [SerializeField, TextArea] private string adoptionRecord;
        public ShipRoomAuthoring Owner => owner;
        public ShipLayoutAuthoring LayoutOwner => layoutOwner;
        public string AdoptionRecord => adoptionRecord;

        public void RecordAdoption(string previousHierarchy)
        {
            if (owner == null || layoutOwner == null)
                throw new InvalidOperationException("Record adoption after assigning room and layout ownership.");
            adoptionRecord = "Room " + owner.RoomId + " at " + previousHierarchy +
                ": independent output → layout " + layoutOwner.LayoutId + ". Existing room, occupancy, style and output references retained.";
        }

        public void Initialize(ShipRoomAuthoring room) => Initialize(room, null);

        public void Initialize(ShipRoomAuthoring room, ShipLayoutAuthoring layout)
        {
            if (room == null) throw new ArgumentNullException(nameof(room));
            if (owner != null && owner != room) throw new InvalidOperationException("Generated output already belongs to another room.");
            if (layoutOwner != null && layoutOwner != layout)
                throw new InvalidOperationException("Generated output already belongs to another layout.");
            if (layout != null && room.Layout != layout)
                throw new InvalidOperationException("Output room must belong to its layout.");
            owner = room;
            layoutOwner = layout;
        }
    }
}
