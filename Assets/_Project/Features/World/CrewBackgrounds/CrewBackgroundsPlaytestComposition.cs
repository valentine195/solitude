using System;
using SOLITUDE.Composition;
using UnityEngine;

namespace SOLITUDE.World.CrewBackgrounds
{
    [DisallowMultipleComponent]
    public sealed class CrewBackgroundsPlaytestComposition : SceneFeatureComposition
    {
        [SerializeField] private CrewBackground defaultBackground;
        [SerializeField] private CrewBackgroundsFault defaultFault;
        [SerializeField] private Collider2D returnDoor;

        public CrewBackgroundsPlaytestSetup? ActiveSetup { get; private set; }
        public bool NormalFeedAvailable => ActiveSetup?.Fault == CrewBackgroundsFault.BrokenIntakeLinkage;
        public bool IntakeLinkageIntact => ActiveSetup?.Fault == CrewBackgroundsFault.LossOfPower;
        public bool ReturnDoorClosed => returnDoor != null && returnDoor.enabled;

        public override void Validate()
        {
            if (returnDoor == null || returnDoor.gameObject.scene != gameObject.scene)
                throw new InvalidOperationException("The crew playtest requires a return door in the same scene.");

            _ = new CrewBackgroundsPlaytestSetup(defaultBackground, defaultFault);
        }

        public override void Initialize(SceneFeatureContext context)
        {
            Release();
            Validate();
            if (context?.Policy == null || context.Interactor == null
                || context.Interactor.gameObject.scene != gameObject.scene || context.LifecycleSession != null)
                throw new InvalidOperationException("The crew playtest requires a disposable local player scene.");

            ActiveSetup = CrewBackgroundsPlaytestSetup.Resolve(gameObject.scene.path,
                new CrewBackgroundsPlaytestSetup(defaultBackground, defaultFault));
            returnDoor.enabled = true;
        }

        public override bool TryBack() => false;
        public override void CloseForTransition() { }
        public override void Release() => ActiveSetup = null;
        private void OnDestroy() => Release();
    }
}
