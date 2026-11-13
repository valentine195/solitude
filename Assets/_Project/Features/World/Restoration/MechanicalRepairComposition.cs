using System;
using SOLITUDE.Composition;
using SOLITUDE.Core.Interaction;
using SOLITUDE.Player;
using SOLITUDE.Restoration;
using SOLITUDE.SaveLoad;
using UnityEngine;

namespace SOLITUDE.World.Restoration
{
    public sealed class MechanicalRepairComposition : SceneFeatureComposition
    {
        [SerializeField] private SaveableId identity;
        [SerializeField] private MechanicalRepairDefinition definition;
        [SerializeField] private RestorationJunction junction;
        [SerializeField] private RestorableSystem system;
        [SerializeField] private RestorationDoorGate gate;
        private ContainerSaveSession.MechanicalRepairBinding binding;
        private SceneFeatureContext context;
        public void Configure(SaveableId identity, MechanicalRepairDefinition definition, RestorationJunction junction,
            RestorableSystem system, RestorationDoorGate gate)
        {
            this.identity = identity; this.definition = definition; this.junction = junction; this.system = system; this.gate = gate;
            junction.ConfigureMechanicalRepair(this);
        }
        public override void Validate()
        {
            if (identity == null || string.IsNullOrWhiteSpace(identity.Value) || identity.Value != identity.Value.Trim()
                || definition == null || junction == null || system == null || gate == null || !junction.IsMechanicalOwner(this)
                || !gate.IsConfiguredFor(system)) throw new InvalidOperationException("Mechanical repair references are incomplete.");
            definition.ValidateAuthoring();
        }
        public override void Initialize(SceneFeatureContext value)
        {
            Release(); Validate();
            if (value?.LifecycleSession == null || value.Capabilities == null || value.Inventory == null || value.Hotbar == null
                || value.Interactor == null || !ReferenceEquals(value.Commands, value.LifecycleSession.Commands))
                throw new InvalidOperationException("Mechanical repair requires a production session and carried owners.");
            context = value;
            binding = value.LifecycleSession.BindMechanicalRepair(definition.Compile(identity.Value, value.Capabilities.Registry));
            binding.Changed += Reconcile;
            Reconcile();
        }
        private void Reconcile() { if (system != null) system.State.ReconcileCompletedRepair(binding != null && binding.IsCompleted); }
        public string Prompt => !Available ? "System unavailable" : binding.IsCompleted ? "System restored" : "Offline — requires mechanical tool and 1 " + definition.Part.DisplayName + " in inventory";
        private bool Available => isActiveAndEnabled && context != null && binding != null && system != null && system.isActiveAndEnabled
            && junction != null && junction.isActiveAndEnabled && gate != null && gate.isActiveAndEnabled;
        public InteractionResult Repair(PlayerInteractor player)
        {
            if (!Available || player == null || !player.isActiveAndEnabled || player != context.Interactor || (context.Policy != null && !context.Policy.Gameplay)) return InteractionResult.Blocked("Installation unavailable; no item installed");
            var outcome = binding.TryRepair(context.Inventory.Handle, context.Hotbar.Handle, id => context.Capabilities.Resolve(id)?.Tags);
            switch (outcome)
            {
                case MechanicalRepairOutcome.Committed: return InteractionResult.Success("System restored");
                case MechanicalRepairOutcome.AlreadyRepaired: return InteractionResult.Blocked("System already restored");
                case MechanicalRepairOutcome.MissingTool: return InteractionResult.Blocked("Requires a mechanical tool in inventory or hotbar");
                case MechanicalRepairOutcome.MissingPart: return InteractionResult.Blocked("Requires 1 " + definition.Part.DisplayName + " in inventory");
                default: return InteractionResult.Blocked("Installation unavailable; no item installed");
            }
        }
        public override bool TryBack() => false;
        public override void CloseForTransition() { }
        public override void Release()
        {
            if (binding != null) { binding.Changed -= Reconcile; binding.Dispose(); binding = null; }
            context = null;
            if (system != null) system.State.ReconcileCompletedRepair(false);
        }
        private void OnDisable() => Release();
        private void OnDestroy() => Release();
    }
}
