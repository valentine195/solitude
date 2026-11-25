using System;
using UnityEngine;

namespace SOLITUDE.World.RoomAuthoring
{
    // Space-relative cell coordinates. Float storage retains malformed external records for diagnosis.
    [Serializable]
    public sealed class RoomFloorReservation
    {
        [SerializeField] private string id;
        [SerializeField] private string spaceId;
        [SerializeField] private Vector2 position;
        [SerializeField] private Vector2 size;
        public string Id => id;
        public string SpaceId => spaceId;
        public Vector2 Position => position;
        public Vector2 Size => size;

        public RoomFloorReservation(string identity, string owner, Vector2 start, Vector2 dimensions)
        { id = identity; spaceId = owner; position = start; size = dimensions; }

        public RoomFloorReservation Copy() => new RoomFloorReservation(id, spaceId, position, size);

        public string AddressIssue()
        {
            if (string.IsNullOrWhiteSpace(id)) return "Reservation identity is required.";
            if (string.IsNullOrWhiteSpace(spaceId)) return "Reservation space identity is required.";
            if (!Integral(position.x) || !Integral(position.y) || !Integral(size.x) || !Integral(size.y) || size.x <= 0 || size.y <= 0)
                return "Reservation requires an integral cell position and positive integral width/height.";
            if ((double)position.x + size.x > int.MaxValue || (double)position.y + size.y > int.MaxValue)
                return "Reservation bounds exceed the supported integer grid.";
            return null;
        }

        private static bool Integral(float value) => !float.IsNaN(value) && !float.IsInfinity(value) &&
            (double)value >= int.MinValue && (double)value <= int.MaxValue && Math.Truncate(value) == value;
    }
}
