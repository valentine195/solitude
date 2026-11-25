using System;
using SOLITUDE.Application;
using SOLITUDE.Composition;
using UnityEngine;

namespace SOLITUDE.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerVitalsComposition : SceneFeatureComposition
    {
        [SerializeField] private PlayerVitalsController controller;
        private bool initialized;
        private IDisposable useBinding;
        public PlayerVitalsController Controller => controller;
        public SelectedVitalItemUseCoordinator ItemUse { get; private set; }
        public void Configure(PlayerVitalsController player) { Release(); controller = player; }
        public override void Validate()
        {
            if (controller == null || controller.gameObject.scene != gameObject.scene)
                throw new InvalidOperationException("Player vitals require a controller in the same scene.");
        }
        public override void Initialize(SceneFeatureContext context)
        {
            Release(); Validate();
            if (context?.Policy == null) throw new InvalidOperationException("Player vitals require gameplay policy.");
            controller.BindGameplay(this, context.Policy, () => initialized && isActiveAndEnabled);
            initialized = true; controller.RefreshPresentation();
            if (context.Commands != null && context.Hotbar != null && context.Selection != null && context.Presentations != null)
            {
                ItemUse = new SelectedVitalItemUseCoordinator(context.Commands, context.Hotbar,
                    context.Selection, context.Presentations.Consumables, controller, context.Policy);
                if (context.Input != null) useBinding = context.Input.BindVitalUse(UseSelected);
            }
        }
        private void UseSelected() { if (initialized && isActiveAndEnabled) ItemUse?.TryUse(); }
        private void Update() { if (initialized && controller != null) controller.Advance(Time.deltaTime); }
        private void OnEnable() { if (initialized && controller != null) controller.RefreshPresentation(); }
        private void OnDisable() { if (initialized && controller != null) controller.RefreshPresentation(); }
        private void OnDestroy() => Release();
        public override bool TryBack() => false;
        public override void CloseForTransition() { if (controller != null) controller.RefreshPresentation(); }
        public override void Release()
        {
            initialized = false;
            useBinding?.Dispose(); useBinding = null;
            ItemUse?.Dispose(); ItemUse = null;
            if (controller != null) controller.UnbindGameplay(this);
        }
    }
}
