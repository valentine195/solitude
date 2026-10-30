using System;
using System.Collections.Generic;

namespace SOLITUDE.GameplayTags
{
    public sealed class GameplayTagSet
    {
        private readonly HashSet<GameplayTag> exact = new HashSet<GameplayTag>();
        private readonly HashSet<GameplayTag> hierarchical = new HashSet<GameplayTag>();
        internal GameplayTagRegistry Registry { get; }
        public IReadOnlyList<GameplayTag> Tags { get; }

        public GameplayTagSet(GameplayTagRegistry registry, IEnumerable<GameplayTag> tags)
        {
            Registry = registry ?? throw new ArgumentNullException(nameof(registry));
            if (tags == null) throw new ArgumentNullException(nameof(tags));
            var assigned = new List<GameplayTag>();
            foreach (var tag in tags)
            {
                registry.RequireOwned(tag);
                if (!exact.Add(tag)) throw new ArgumentException("Duplicate assigned gameplay tag: " + tag.Name, nameof(tags));
                assigned.Add(tag);
                for (var ancestor = tag; ancestor != null; ancestor = ancestor.Parent) hierarchical.Add(ancestor);
            }
            Tags = assigned.AsReadOnly();
        }

        public bool HasTag(GameplayTag tag, GameplayTagMatch match = GameplayTagMatch.Hierarchical)
        { Registry.RequireOwned(tag); GameplayTagRegistry.RequireMatch(match); return Contains(tag, match); }

        public bool HasAll(IReadOnlyList<GameplayTag> requested, GameplayTagMatch match = GameplayTagMatch.Hierarchical)
        {
            Validate(requested, match);
            for (int i = 0; i < requested.Count; i++) if (!Contains(requested[i], match)) return false;
            return true;
        }

        public bool HasAny(IReadOnlyList<GameplayTag> requested, GameplayTagMatch match = GameplayTagMatch.Hierarchical)
        {
            Validate(requested, match);
            for (int i = 0; i < requested.Count; i++) if (Contains(requested[i], match)) return true;
            return false;
        }

        public bool HasNone(IReadOnlyList<GameplayTag> requested, GameplayTagMatch match = GameplayTagMatch.Hierarchical)
        { return !HasAny(requested, match); }

        internal bool Contains(GameplayTag tag, GameplayTagMatch match)
            => match == GameplayTagMatch.Exact ? exact.Contains(tag) : hierarchical.Contains(tag);

        private void Validate(IReadOnlyList<GameplayTag> requested, GameplayTagMatch match)
        {
            if (requested == null) throw new ArgumentNullException(nameof(requested));
            GameplayTagRegistry.RequireMatch(match);
            // Validate the entire condition, even when an earlier tag would decide its result.
            for (int i = 0; i < requested.Count; i++) Registry.RequireOwned(requested[i]);
        }
    }
}
