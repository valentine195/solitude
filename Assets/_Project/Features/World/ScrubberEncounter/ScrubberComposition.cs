using System;
using System.Collections.Generic;
using System.Linq;
using SOLITUDE.Composition;
using UnityEngine;

namespace SOLITUDE.World.ScrubberEncounter
{
    public sealed class ScrubberComposition : SceneFeatureComposition
    {
        [SerializeField] private ScrubberEquipment[] equipment = new ScrubberEquipment[0];
        [SerializeField] private ScrubberScreenView[] screens = new ScrubberScreenView[0];
        [SerializeField] private ScrubberTaskSource[] taskSources = new ScrubberTaskSource[0];
        private readonly List<ScrubberEquipment> boundEquipment = new();
        private readonly List<ScrubberScreenView> boundScreens = new();
        private readonly List<ScrubberTaskSource> boundSources = new();

        public void Configure(ScrubberEquipment[] units, ScrubberScreenView[] views, ScrubberTaskSource[] sources)
        { equipment = units; screens = views; taskSources = sources; }

        public override void Validate()
        {
            var compositions = Authored<ScrubberComposition>();
            if (compositions.Length != 1 || compositions[0] != this)
                throw new InvalidOperationException("Expected one scrubber composition per scene.");
            ValidateEndpoints(equipment, "equipment");
            ValidateEndpoints(screens, "screens");
            ValidateEndpoints(taskSources, "task sources");
            if (equipment.Length == 0 || screens.Length == 0 || taskSources.Length == 0)
                throw new InvalidOperationException("Scrubber composition endpoints are incomplete.");
            foreach (var screen in screens)
            {
                screen.ValidateAuthoring();
                if (!equipment.Contains(screen.Equipment))
                    throw new InvalidOperationException("Scrubber screen equipment is not owned by this composition.");
            }
            var identities = new HashSet<string>(StringComparer.Ordinal);
            foreach (var source in taskSources)
            {
                if (string.IsNullOrWhiteSpace(source.SourceId) || !identities.Add(source.SourceId) ||
                    !equipment.Contains(source.Equipment) || source.Access == null || source.Access.gameObject.scene != gameObject.scene ||
                    source.Access.GetComponent<ScrubberEquipment>() != source.Equipment || !screens.Contains(source.Access.Screen) ||
                    source.Access.Screen.Equipment != source.Equipment)
                    throw new InvalidOperationException("Invalid/duplicate scrubber task identity or equipment/access/screen binding.");
            }
        }
        private T[] Authored<T>() where T : Component => gameObject.scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
        private void ValidateEndpoints<T>(T[] endpoints, string label) where T : Component
        {
            if (endpoints == null || endpoints.Any(endpoint => endpoint == null || endpoint.gameObject.scene != gameObject.scene) ||
                endpoints.Distinct().Count() != endpoints.Length)
                throw new InvalidOperationException("Null, duplicate or wrong-scene scrubber " + label + ".");
            if (Authored<T>().Any(endpoint => !endpoints.Contains(endpoint)))
                throw new InvalidOperationException("Unbound authored scrubber " + label + ".");
        }
        public override void Initialize(SceneFeatureContext context)
        {
            Release(); Validate();
            if (context == null || context.Commands == null || context.Inventory?.IsAvailable != true ||
                context.Presentations == null || context.Policy == null || context.InteractionEvents == null || context.Interactor == null ||
                context.Interactor.gameObject.scene != gameObject.scene || string.IsNullOrWhiteSpace(context.TaskScope))
                throw new InvalidOperationException("Scrubber composition requires a composed player scene.");
            try
            {
                foreach (var unit in equipment)
                { boundEquipment.Add(unit); unit.Initialize(context.Commands, context.Inventory, context.Presentations); }
                foreach (var screen in screens)
                { boundScreens.Add(screen); screen.Initialize(context.Policy, context.Input); }
                foreach (var source in taskSources)
                {
                    boundSources.Add(source);
                    source.Initialize(context.Tasks, context.TaskScope, context.Policy, context.Interactor, context.InteractionEvents,
                        boundEquipment.Contains(source.Equipment) && context.Presentations.Resolve(ScrubberEquipment.FuseItemId) != null && context.Inventory.IsAvailable);
                }
            }
            catch { Release(); throw; }
        }
        public override bool TryBack()
        {
            foreach (var screen in boundScreens) if (screen != null && screen.Back()) return true;
            return false;
        }
        public override void CloseForTransition()
        { foreach (var screen in boundScreens) if (screen != null) screen.Close(); }
        public override void Release()
        {
            for (int i = boundSources.Count - 1; i >= 0; i--) if (boundSources[i] != null) boundSources[i].ReleaseBinding();
            boundSources.Clear();
            for (int i = boundScreens.Count - 1; i >= 0; i--) if (boundScreens[i] != null) boundScreens[i].Release();
            boundScreens.Clear();
            for (int i = boundEquipment.Count - 1; i >= 0; i--) if (boundEquipment[i] != null) boundEquipment[i].ReleaseBinding();
            boundEquipment.Clear();
        }
        private void OnDestroy() => Release();
    }
}
