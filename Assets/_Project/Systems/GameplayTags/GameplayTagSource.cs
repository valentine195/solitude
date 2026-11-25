using System;
using UnityEngine;

namespace SOLITUDE.GameplayTags
{
    [DisallowMultipleComponent]
    public sealed class GameplayTagSource : MonoBehaviour
    {
        [SerializeField] private GameplayTagRegistryAsset registry;
        [SerializeField] private GameplayTagAuthoring tags = new GameplayTagAuthoring();
        private GameplayTagSet compiledTags;
        public bool IsInitialized => compiledTags != null;
        public GameplayTagSet Tags => compiledTags ?? throw new InvalidOperationException("Gameplay tag source has not been initialized.");

        public void Configure(GameplayTagRegistryAsset registryAsset, GameplayTagAuthoring assignments)
        { Release(); registry = registryAsset; tags = assignments; }

        public void ValidateAuthoring()
        {
            if (registry == null || tags == null) throw new InvalidOperationException("Gameplay tag source requires a registry and assignments.");
            tags.Compile(registry, registry.Compile());
        }

        /// <summary>Scene composition supplies the same immutable snapshot to sets and queries.</summary>
        public void Initialize(GameplayTagRegistryAsset registryAsset, GameplayTagRegistry compiled)
        {
            Release();
            if (registry == null || registry != registryAsset || tags == null)
                throw new InvalidOperationException("Gameplay tag source requires its configured registry.");
            compiledTags = tags.Compile(registryAsset, compiled);
        }

        public void Release() => compiledTags = null;
        private void OnDisable() => Release();
    }
}
