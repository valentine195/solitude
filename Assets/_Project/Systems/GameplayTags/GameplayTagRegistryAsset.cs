using System;
using System.Collections.Generic;
using UnityEngine;

namespace SOLITUDE.GameplayTags
{
    [CreateAssetMenu(menuName = "SOLITUDE/Gameplay Tags/Registry")]
    public sealed class GameplayTagRegistryAsset : ScriptableObject
    {
        [SerializeField] private List<GameplayTagDefinition> definitions = new List<GameplayTagDefinition>();
        public int Count => definitions?.Count ?? 0;
        public GameplayTagDefinition GetDefinition(int index) => definitions[index];

        public void Configure(IEnumerable<GameplayTagDefinition> values)
            => definitions = values == null ? null : new List<GameplayTagDefinition>(values);

        public IReadOnlyList<string> CollectErrors()
        {
            var errors = new List<string>();
            if (definitions == null) { errors.Add("Gameplay tag registry definitions are missing."); return errors.AsReadOnly(); }
            var registered = new HashSet<GameplayTagDefinition>(definitions);
            for (int i = 0; i < definitions.Count; i++)
            {
                var definition = definitions[i];
                if (definition == null) continue; // The pure registry reports its indexed missing definition.
                if (definition.IsRoot && definition.Parent != null)
                    errors.Add("Root gameplay tag must not have a parent: " + definition.name + ".");
                if (!definition.IsRoot && definition.Parent == null)
                    errors.Add("Child gameplay tag has a missing parent reference: " + definition.name + ".");
                if (definition.Parent != null && !registered.Contains(definition.Parent))
                    errors.Add("Gameplay tag parent is not registered: " + definition.name + ".");
            }
            foreach (var error in GameplayTagRegistry.Validate(Snapshot())) errors.Add(error);
            return errors.AsReadOnly();
        }

        public GameplayTagRegistry Compile()
        {
            var errors = CollectErrors();
            if (errors.Count != 0) throw new InvalidOperationException(string.Join("\n", errors));
            return new GameplayTagRegistry(Snapshot());
        }

        public GameplayTag Resolve(GameplayTagDefinition definition, GameplayTagRegistry compiled)
        {
            if (compiled == null) throw new ArgumentNullException(nameof(compiled));
            if (definition == null) throw new ArgumentException("Missing gameplay tag reference.", nameof(definition));
            if (definitions == null || !definitions.Contains(definition))
                throw new ArgumentException("Gameplay tag reference is not registered: " + definition.name, nameof(definition));
            return compiled.GetTag(definition.TagId);
        }

        private List<GameplayTagSpec> Snapshot()
        {
            var result = new List<GameplayTagSpec>();
            if (definitions == null) return result;
            foreach (var definition in definitions)
                result.Add(definition == null ? null : new GameplayTagSpec(definition.TagId, definition.Segment,
                    definition.IsRoot || definition.Parent == null ? null : definition.Parent.TagId));
            return result;
        }
    }
}
