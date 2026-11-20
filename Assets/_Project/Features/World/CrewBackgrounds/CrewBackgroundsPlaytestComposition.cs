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
        [SerializeField] private GameObject jumperPickup;
        [SerializeField] private CrewBackgroundsServicePanel servicePanel;
        [SerializeField] private CrewBackgroundsRepairControl[] controls = Array.Empty<CrewBackgroundsRepairControl>();
        private SceneFeatureContext context;
        private bool lastAirflow;
        private bool lastRecovered;
        private bool lastCorridorLit;

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
                || jumperPickup == null || jumperPickup.scene != gameObject.scene
                || servicePanel == null || servicePanel.gameObject.scene != gameObject.scene
                || controls == null || controls.Length == 0)
                throw new InvalidOperationException("The crew playtest requires local repair controls, room feedback, and a return door.");

            roomFeedback.ValidateAuthoring();
            servicePanel.ValidateAuthoring();
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
            servicePanel.Bind(context.Commands);
            Encounter = servicePanel.NewEncounter(setup.Fault);
            if (setup.Fault == CrewBackgroundsFault.LossOfPower) linkagePickup.SetActive(false);
            else jumperPickup.SetActive(false);
            foreach (var control in controls) control.Bind(context, setup, Encounter);
            lastAirflow = lastRecovered = false;
            lastCorridorLit = true;
            roomFeedback.ResetRoom();
        }

        private void Update()
        {
            if (Encounter == null) return;
            if (context.Policy.Gameplay) Encounter.Advance(Time.deltaTime);

            bool airflow = Encounter.Airflow;
            bool recovered = Encounter.RoomRecovered;
            bool lit = Encounter.CorridorLightsOn;
            if (airflow == lastAirflow && recovered == lastRecovered && lit == lastCorridorLit) return;
            lastAirflow = airflow;
            lastRecovered = recovered;
            lastCorridorLit = lit;
            roomFeedback.Render(airflow, recovered, lit);
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
            if (servicePanel != null) servicePanel.Release();
            context = null;
            ActiveSetup = null;
            Encounter = null;
        }

        private void OnDestroy() => Release();
    }
}
