using System;
using System.Collections.Generic;

namespace SOLITUDE.GameplayTags
{
    public enum GameplayTagMatch { Exact, Hierarchical }

    /// <summary>Unity-independent authoring snapshot. Identity is not the readable name.</summary>
    public sealed class GameplayTagSpec
    {
        public string Id { get; }
        public string Segment { get; }
        public string ParentId { get; }
        public GameplayTagSpec(string id, string segment, string parentId = null)
        { Id = id; Segment = segment; ParentId = parentId; }
    }

    /// <summary>A resolved, immutable definition belonging to one registry snapshot.</summary>
    public sealed class GameplayTag
    {
        internal GameplayTagRegistry Registry { get; }
        public string Id { get; }
        public string Name { get; }
        public GameplayTag Parent { get; }
        internal GameplayTag(GameplayTagRegistry registry, GameplayTagSpec spec, GameplayTag parent)
        {
            Registry = registry; Id = spec.Id; Parent = parent;
            Name = parent == null ? spec.Segment : parent.Name + "." + spec.Segment;
        }
        public override string ToString() => Name;
    }

    public sealed class GameplayTagRegistry
    {
        private readonly Dictionary<string, GameplayTag> byId = new Dictionary<string, GameplayTag>(StringComparer.Ordinal);
        public IReadOnlyList<GameplayTag> Tags { get; }
        public GameplayTagSet EmptySet { get; }

        public GameplayTagRegistry(IEnumerable<GameplayTagSpec> definitions)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            var specs = new List<GameplayTagSpec>(definitions);
            var errors = Validate(specs);
            if (errors.Count != 0) throw new ArgumentException(string.Join("\n", errors), nameof(definitions));
            var source = new Dictionary<string, GameplayTagSpec>(StringComparer.Ordinal);
            foreach (var spec in specs) source.Add(spec.Id, spec);
            // Iterative construction supports child-first input and avoids hierarchy recursion.
            var chain = new List<GameplayTagSpec>();
            foreach (var spec in specs)
            {
                chain.Clear(); var next = spec;
                while (next != null && !byId.ContainsKey(next.Id))
                {
                    chain.Add(next);
                    next = next.ParentId == null ? null : source[next.ParentId];
                }
                for (int i = chain.Count - 1; i >= 0; i--)
                {
                    var current = chain[i];
                    byId.Add(current.Id, new GameplayTag(this, current,
                        current.ParentId == null ? null : byId[current.ParentId]));
                }
            }
            var tags = new List<GameplayTag>(specs.Count);
            foreach (var spec in specs) tags.Add(byId[spec.Id]);
            Tags = tags.AsReadOnly();
            EmptySet = new GameplayTagSet(this, Array.Empty<GameplayTag>());
        }

        public bool TryGetTag(string id, out GameplayTag tag)
        { tag = null; return id != null && byId.TryGetValue(id, out tag); }

        /// <summary>Resolve serialized identity during setup; frequent rules use the resolved tag.</summary>
        public GameplayTag GetTag(string id)
        {
            if (!TryGetTag(id, out var tag)) throw new ArgumentException("Unregistered gameplay tag identity: " + id, nameof(id));
            return tag;
        }

        internal void RequireOwned(GameplayTag tag)
        {
            if (tag == null) throw new ArgumentNullException(nameof(tag), "A missing gameplay tag is not an empty condition.");
            if (!ReferenceEquals(tag.Registry, this)) throw new ArgumentException("Gameplay tag belongs to a different registry snapshot.", nameof(tag));
        }

        internal static void RequireMatch(GameplayTagMatch match)
        {
            if (match != GameplayTagMatch.Exact && match != GameplayTagMatch.Hierarchical)
                throw new ArgumentOutOfRangeException(nameof(match));
        }

        public static IReadOnlyList<string> Validate(IEnumerable<GameplayTagSpec> definitions)
        {
            var errors = new List<string>();
            if (definitions == null) { errors.Add("Gameplay tag definitions are missing."); return errors.AsReadOnly(); }
            var source = new Dictionary<string, GameplayTagSpec>(StringComparer.Ordinal);
            var unique = new List<GameplayTagSpec>(); int index = 0;
            foreach (var spec in definitions)
            {
                if (spec == null) errors.Add("Missing gameplay tag definition at index " + index + ".");
                else
                {
                    if (string.IsNullOrWhiteSpace(spec.Id) || spec.Id != spec.Id.Trim())
                        errors.Add("Invalid gameplay tag identity at index " + index + ".");
                    else if (source.ContainsKey(spec.Id)) errors.Add("Duplicate gameplay tag identity: " + spec.Id + ".");
                    else { source.Add(spec.Id, spec); unique.Add(spec); }
                    if (!ValidSegment(spec.Segment)) errors.Add("Invalid gameplay tag name segment at index " + index + ".");
                }
                index++;
            }
            var names = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var spec in unique)
            {
                if (spec.ParentId == spec.Id) errors.Add("Gameplay tag is its own parent: " + spec.Id + ".");
                else if (spec.ParentId != null && !source.ContainsKey(spec.ParentId))
                    errors.Add("Unregistered parent for gameplay tag: " + spec.Id + ".");
                var seen = new HashSet<string>(StringComparer.Ordinal);
                var segments = new List<string>(); var current = spec; bool valid = true;
                while (current != null)
                {
                    if (!seen.Add(current.Id))
                    { errors.Add("Parent cycle reached from gameplay tag: " + spec.Id + "."); valid = false; break; }
                    if (!ValidSegment(current.Segment)) { valid = false; break; }
                    segments.Add(current.Segment);
                    if (current.ParentId == null) break;
                    if (!source.TryGetValue(current.ParentId, out current)) { valid = false; break; }
                }
                if (!valid) continue;
                segments.Reverse(); string fullName = string.Join(".", segments);
                if (names.ContainsKey(fullName)) errors.Add("Duplicate gameplay tag full name: " + fullName + ".");
                else names.Add(fullName, spec.Id);
            }
            return errors.AsReadOnly();
        }

        private static bool ValidSegment(string segment)
        {
            if (string.IsNullOrEmpty(segment) || !char.IsLetter(segment[0])) return false;
            for (int i = 1; i < segment.Length; i++)
                if (!char.IsLetterOrDigit(segment[i]) && segment[i] != '_') return false;
            return true;
        }
    }
}
