using System;
using System.Collections.Generic;
using UnityEngine;

namespace SOLITUDE.World.RoomAuthoring
{
    public enum LayoutConnectionType { OpenPassage, Doorway, AirlockOpening }

    // The owner pair is directional: below/left is negative; above/right is positive.
    // Float storage deliberately retains malformed external/off-grid data for diagnosis.
    [Serializable]
    public sealed class LayoutConnectionDefinition
    {
        [SerializeField] private string id;
        [SerializeField] private string negativeOwner;
        [SerializeField] private string positiveOwner;
        [SerializeField] private Vector2 start;
        [SerializeField] private bool horizontal;
        [SerializeField] private int width;
        [SerializeField] private LayoutConnectionType type;
        public string Id => id;
        public string NegativeOwner => negativeOwner;
        public string PositiveOwner => positiveOwner;
        public Vector2 Start => start;
        public bool Horizontal => horizontal;
        public int Width => width;
        public LayoutConnectionType Type => type;

        public LayoutConnectionDefinition(string identity, string negative, string positive, Vector2 position,
            bool isHorizontal, int openingWidth, LayoutConnectionType connectionType)
        { id = identity; negativeOwner = negative; positiveOwner = positive; start = position;
            horizontal = isHorizontal; width = openingWidth; type = connectionType; }

        public string AddressIssue()
        {
            if (string.IsNullOrWhiteSpace(id)) return "Connection identity is required.";
            if (string.IsNullOrWhiteSpace(negativeOwner) || string.IsNullOrWhiteSpace(positiveOwner) || negativeOwner == positiveOwner)
                return "Connection requires two distinct expected space owners.";
            if (!Enum.IsDefined(typeof(LayoutConnectionType), type)) return "Unknown connection type.";
            if (width <= 0) return "Connection width must be a positive integral number of unit edges.";
            if (float.IsNaN(start.x) || float.IsNaN(start.y) || float.IsInfinity(start.x) || float.IsInfinity(start.y) ||
                (double)start.x < int.MinValue || (double)start.x > int.MaxValue || (double)start.y < int.MinValue || (double)start.y > int.MaxValue ||
                Math.Truncate(start.x) != start.x || Math.Truncate(start.y) != start.y)
                return "Connection start must be an integral grid boundary position.";
            if ((horizontal ? (long)start.x : (long)start.y) + width > int.MaxValue)
                return "Connection span exceeds the supported integer grid.";
            return null;
        }

        public IEnumerable<LayoutEdgeAddress> Edges()
        {
            var issue = AddressIssue();
            if (issue != null) throw new InvalidOperationException(issue);
            for (var i = 0; i < width; i++)
                yield return new LayoutEdgeAddress(new Vector2Int((int)start.x + (horizontal ? i : 0),
                    (int)start.y + (horizontal ? 0 : i)), horizontal);
        }
    }
}
