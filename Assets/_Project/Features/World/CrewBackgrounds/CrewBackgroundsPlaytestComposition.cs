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
        [SerializeField] private CrewBackgroundsRoomFeedback roomFeedback;
        [SerializeField] private GameObject linkagePickup;
        [SerializeField] private CrewBackgroundsRepairControl[] controls = Array.Empty<CrewBackgroundsRepairControl>();
        private SceneFeatureContext context;
        private bool lastAirflow;
        private bool lastRecovered;

        public CrewBackgroundsPlaytestSetup? ActiveSetup { get; private set; }
        public CrewBackgroundsEncounterState Encounter { get; private set; }
        public bool NormalFeedAvailable => Encounter?.NormalFeedAvailable == true;
        public bool IntakeLinkageIntact => Encounter?.IntakeLinkageIntact == true;
        public bool ReturnDoorClosed => returnDoor != null && returnDoor.enabled;

        public override void Validate()
        {
            if (returnDoor == null || returnDoor.gameObject.scene != gameObject.scene
                || roomFeedback == null || roomFeedback.gameObject.scene != gameObject.scene
                || linkagePickup == null || linkagePickup.scene != gameObject.scene
                || controls == null || controls.Length == 0)
                throw new InvalidOperationException("The crew playtest requires local repair controls, room feedback, and a return door.");

            roomFeedback.ValidateAuthoring();
            foreach (var control in controls)
            {
                if (control == null || control.gameObject.scene != gameObject.scene)
                    throw new InvalidOperationException("The crew playtest contains a missing or external repair control.");
                control.ValidateAuthoring();
            }
            _ = new CrewBackgroundsPlaytestSetup(defaultBackground, defaultFault);
        }

        public override void Initialize(SceneFeatureContext context)
        {
            Release();
            Validate();
            if (context?.Policy == null || context.Interactor == null
                || context.Interactor.gameObject.scene != gameObject.scene || context.LifecycleSession != null)
                throw new InvalidOperationException("The crew playtest requires a disposable local player scene.");

            this.context = context;
            var setup = CrewBackgroundsPlaytestSetup.Resolve(gameObject.scene.path,
                new CrewBackgroundsPlaytestSetup(defaultBackground, defaultFault));
            ActiveSetup = setup;
            Encounter = new CrewBackgroundsEncounterState(setup.Fault);
            if (setup.Fault == CrewBackgroundsFault.LossOfPower) linkagePickup.SetActive(false);
            foreach (var control in controls) control.Bind(context, setup, Encounter);
            lastAirflow = lastRecovered = false;
            roomFeedback.ResetRoom();
        }

        private void Update()
        {
            if (Encounter == null) return;
            if (context.Policy.Gameplay) Encounter.Advance(Time.deltaTime);

            if (Encounter.Airflow == lastAirflow && Encounter.RoomRecovered == lastRecovered) return;
            lastAirflow = Encounter.Airflow;
            lastRecovered = Encounter.RoomRecovered;
            roomFeedback.Render(lastAirflow, lastRecovered);
        }

        public override bool TryBack()
        {
            foreach (var control in controls)
                if (control != null && control.InstantCancel()) return true;
            return false;
        }

        public override void CloseForTransition() => TryBack();

        public override void Release()
        {
            if (controls != null)
                foreach (var control in controls)
                    if (control != null) control.Release();
            if (roomFeedback != null) roomFeedback.Release();
            context = null;
            ActiveSetup = null;
            Encounter = null;
        }

        private void OnDestroy() => Release();
    }
}
