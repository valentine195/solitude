using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SOLITUDE.World.RoomAuthoring
{
    public sealed class ShipSpaceCells
    {
        public string Id { get; }
        public IReadOnlyList<Vector2Int> Cells { get; }
        public Vector2Int Offset { get; }

        public ShipSpaceCells(string id, IEnumerable<Vector2Int> cells, Vector2Int offset)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Space identity is required.");
            Id = id;
            Cells = Array.AsReadOnly(cells.Distinct().OrderBy(c => c.y).ThenBy(c => c.x).ToArray());
            Offset = offset;
        }
    }

    public sealed class ShipLayoutTopology
    {
        private readonly Dictionary<Vector2Int, IReadOnlyList<string>> owners;
        private readonly Dictionary<string, IReadOnlyList<IReadOnlyList<Vector2Int>>> components;
        private static readonly IReadOnlyList<string> NoOwners = Array.AsReadOnly(Array.Empty<string>());
        private static readonly Vector2Int[] Directions = { Vector2Int.down, Vector2Int.left, Vector2Int.right, Vector2Int.up };
        public IReadOnlyList<Vector2Int> Cells { get; }
        public IReadOnlyList<Vector2Int> Overlaps { get; }
        public IReadOnlyList<string> SpaceIds { get; }

        private ShipLayoutTopology(ShipSpaceCells[] spaces)
        {
            if (spaces.Any(s => s == null) || spaces.Select(s => s.Id).Distinct(StringComparer.Ordinal).Count() != spaces.Length)
                throw new ArgumentException("Topology requires unique, non-null space identities.");
            var allOwners = new Dictionary<Vector2Int, List<string>>();
            components = new Dictionary<string, IReadOnlyList<IReadOnlyList<Vector2Int>>>(StringComparer.Ordinal);
            foreach (var space in spaces.OrderBy(s => s.Id, StringComparer.Ordinal))
            {
                var cells = new HashSet<Vector2Int>();
                foreach (var cell in space.Cells)
                {
                    var position = new Vector2Int(checked(cell.x + space.Offset.x), checked(cell.y + space.Offset.y));
                    cells.Add(position);
                    if (!allOwners.TryGetValue(position, out var list)) allOwners.Add(position, list = new List<string>());
                    list.Add(space.Id);
                }
                var groups = new List<IReadOnlyList<Vector2Int>>();
                while (cells.Count != 0)
                {
                    var start = cells.OrderBy(c => c.y).ThenBy(c => c.x).First();
                    var pending = new Queue<Vector2Int>();
                    var group = new List<Vector2Int>();
                    pending.Enqueue(start); cells.Remove(start);
                    while (pending.Count != 0)
                    {
                        var current = pending.Dequeue(); group.Add(current);
                        foreach (var direction in Directions)
                        {
                            var x = (long)current.x + direction.x; var y = (long)current.y + direction.y;
                            if (x < int.MinValue || x > int.MaxValue || y < int.MinValue || y > int.MaxValue) continue;
                            var next = new Vector2Int((int)x, (int)y);
                            if (cells.Remove(next)) pending.Enqueue(next);
                        }
                    }
                    groups.Add(Array.AsReadOnly(group.OrderBy(c => c.y).ThenBy(c => c.x).ToArray()));
                }
                components.Add(space.Id, groups.AsReadOnly());
            }
            owners = allOwners.ToDictionary(pair => pair.Key,
                pair => (IReadOnlyList<string>)pair.Value.AsReadOnly());
            Cells = Array.AsReadOnly(owners.Keys.OrderBy(c => c.y).ThenBy(c => c.x).ToArray());
            Overlaps = Array.AsReadOnly(Cells.Where(c => owners[c].Count > 1).ToArray());
            SpaceIds = Array.AsReadOnly(spaces.Select(s => s.Id).OrderBy(id => id, StringComparer.Ordinal).ToArray());
        }

        public static ShipLayoutTopology Build(IEnumerable<ShipSpaceCells> spaces) => new ShipLayoutTopology(spaces.ToArray());
        public IReadOnlyList<string> OwnersAt(Vector2Int cell) => owners.TryGetValue(cell, out var value) ? value : NoOwners;
        public IReadOnlyList<IReadOnlyList<Vector2Int>> ComponentsOf(string spaceId) => components[spaceId];
    }
}
