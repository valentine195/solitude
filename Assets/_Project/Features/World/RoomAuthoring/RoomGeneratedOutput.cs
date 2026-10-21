using System;
using UnityEngine;

namespace SOLITUDE.World.RoomAuthoring
{
    [DisallowMultipleComponent]
    public sealed class RoomGeneratedOutput : MonoBehaviour
    {
        [SerializeField] private ShipRoomAuthoring owner;
        [SerializeField] private ShipLayoutAuthoring layoutOwner;
        public ShipRoomAuthoring Owner => owner;
        public ShipLayoutAuthoring LayoutOwner => layoutOwner;

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
