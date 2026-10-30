using System;
using System.Collections.Generic;

namespace SOLITUDE.GameplayTags
{
    public sealed class GameplayTagQuery
    {
        private readonly GameplayTagRegistry registry;
        private readonly GameplayTag[] required, anyOf, excluded;
        public GameplayTagMatch Match { get; }

        public GameplayTagQuery(GameplayTagRegistry registry, GameplayTagMatch match = GameplayTagMatch.Hierarchical,
            IEnumerable<GameplayTag> required = null, IEnumerable<GameplayTag> anyOf = null, IEnumerable<GameplayTag> excluded = null)
        {
            this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
            GameplayTagRegistry.RequireMatch(match); Match = match;
            this.required = Copy(required); this.anyOf = Copy(anyOf); this.excluded = Copy(excluded);
        }

        public bool Matches(GameplayTagSet tags)
        {
            if (tags == null) throw new ArgumentNullException(nameof(tags));
            if (!ReferenceEquals(tags.Registry, registry)) throw new ArgumentException("Query and set belong to different registry snapshots.", nameof(tags));
            for (int i = 0; i < required.Length; i++) if (!tags.Contains(required[i], Match)) return false;
            for (int i = 0; i < excluded.Length; i++) if (tags.Contains(excluded[i], Match)) return false;
            if (anyOf.Length == 0) return true;
            for (int i = 0; i < anyOf.Length; i++) if (tags.Contains(anyOf[i], Match)) return true;
            return false;
        }

        private GameplayTag[] Copy(IEnumerable<GameplayTag> source)
        {
            if (source == null) return Array.Empty<GameplayTag>();
            var copy = new List<GameplayTag>();
            foreach (var tag in source) { registry.RequireOwned(tag); copy.Add(tag); }
            return copy.ToArray();
        }
    }
}
