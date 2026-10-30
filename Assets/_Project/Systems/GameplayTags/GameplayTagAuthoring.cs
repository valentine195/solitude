using System;
using UnityEngine;

namespace SOLITUDE.GameplayTags
{
    [Serializable]
    public sealed class GameplayTagAuthoring
    {
        [SerializeField] private GameplayTagDefinition[] tags = Array.Empty<GameplayTagDefinition>();
        public GameplayTagAuthoring() { }
        public GameplayTagAuthoring(params GameplayTagDefinition[] definitions)
            => tags = definitions == null ? null : (GameplayTagDefinition[])definitions.Clone();

        public GameplayTagSet Compile(GameplayTagRegistryAsset authoringRegistry, GameplayTagRegistry compiled)
        {
            if (authoringRegistry == null) throw new ArgumentNullException(nameof(authoringRegistry));
            if (compiled == null) throw new ArgumentNullException(nameof(compiled));
            if (tags == null) throw new InvalidOperationException("Gameplay tag assignments are missing.");
            var resolved = new GameplayTag[tags.Length];
            for (int i = 0; i < tags.Length; i++) resolved[i] = authoringRegistry.Resolve(tags[i], compiled);
            return new GameplayTagSet(compiled, resolved);
        }
    }

    [Serializable]
    public sealed class GameplayTagQueryAuthoring
    {
        [SerializeField] private GameplayTagMatch match = GameplayTagMatch.Hierarchical;
        [SerializeField] private GameplayTagAuthoring required = new GameplayTagAuthoring();
        [SerializeField] private GameplayTagAuthoring anyOf = new GameplayTagAuthoring();
        [SerializeField] private GameplayTagAuthoring excluded = new GameplayTagAuthoring();

        public GameplayTagQueryAuthoring() { }
        public GameplayTagQueryAuthoring(GameplayTagMatch mode, GameplayTagAuthoring required,
            GameplayTagAuthoring anyOf, GameplayTagAuthoring excluded)
        { match = mode; this.required = required; this.anyOf = anyOf; this.excluded = excluded; }

        public GameplayTagQuery Compile(GameplayTagRegistryAsset authoringRegistry, GameplayTagRegistry compiled)
        {
            if (required == null || anyOf == null || excluded == null)
                throw new InvalidOperationException("Gameplay tag query groups are missing.");
            return new GameplayTagQuery(compiled, match, required.Compile(authoringRegistry, compiled).Tags,
                anyOf.Compile(authoringRegistry, compiled).Tags, excluded.Compile(authoringRegistry, compiled).Tags);
        }
    }
}
