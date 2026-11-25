using System;
using UnityEngine;

namespace SOLITUDE.World.RoomAuthoring
{
    [DisallowMultipleComponent]
    public sealed class RoomGeneratedOutput : MonoBehaviour
    {
        [SerializeField] private ShipRoomAuthoring owner;
        public ShipRoomAuthoring Owner => owner;

        public void Initialize(ShipRoomAuthoring room)
        {
            if (room == null) throw new ArgumentNullException(nameof(room));
            if (owner != null && owner != room) throw new InvalidOperationException("Generated output already belongs to another room.");
            owner = room;
        }
    }
}
