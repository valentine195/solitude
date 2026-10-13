using System;
using SOLITUDE.Composition;
using UnityEngine;

namespace SOLITUDE.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerVitalsComposition : SceneFeatureComposition
    {
        [SerializeField] private PlayerVitalsController controller;
        private bool initialized;
        public PlayerVitalsController Controller => controller;
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
        }
        private void Update() { if (initialized && controller != null) controller.Advance(Time.deltaTime); }
        private void OnEnable() { if (initialized && controller != null) controller.RefreshPresentation(); }
        private void OnDisable() { if (initialized && controller != null) controller.RefreshPresentation(); }
        private void OnDestroy() => Release();
        public override bool TryBack() => false;
        public override void CloseForTransition() { if (controller != null) controller.RefreshPresentation(); }
        public override void Release()
        {
            initialized = false;
            if (controller != null) controller.UnbindGameplay(this);
        }
    }
}
