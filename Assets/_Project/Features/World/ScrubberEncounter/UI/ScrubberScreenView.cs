using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SOLITUDE.Application;
using SOLITUDE.Core.Input;

namespace SOLITUDE.World.ScrubberEncounter
{
    public sealed class ScrubberScreenView : MonoBehaviour, IScrubberScreenDisplay
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private ScrubberEquipment equipment;
        [SerializeField] private TMP_Text telemetry;
        [SerializeField] private TMP_Text objective;
        [SerializeField] private TMP_Text feedback;
        [SerializeField] private UnityEngine.UI.Button close;
        private ScrubberScreenCoordinator coordinator;
        private UnityInputHost input;
        private GameObject selected;
        private Button install, restart;
        private TMP_Text installReason, restartLabel, intake, distribution, flow, indication;
        public string Feedback => feedback == null ? "" : feedback.text;
        public bool IsOpen => panel != null && panel.activeSelf;
        public string Telemetry => telemetry == null ? "" : telemetry.text;
        public void Configure(GameObject root, ScrubberEquipment unit, TMP_Text readings, TMP_Text task, TMP_Text result, UnityEngine.UI.Button closeButton)
        { panel = root; equipment = unit; telemetry = readings; objective = task; feedback = result; close = closeButton; }
        private void Awake()
        {
            if (close != null) close.onClick.AddListener(Close);
            if(panel==null) return;
            install=panel.transform.Find("InstallFuse")?.GetComponent<Button>(); restart=panel.transform.Find("Restart")?.GetComponent<Button>();
            installReason=panel.transform.Find("InstallReason")?.GetComponent<TMP_Text>(); restartLabel=panel.transform.Find("Restart/RestartLabel")?.GetComponent<TMP_Text>();
            intake=panel.transform.Find("Intake")?.GetComponent<TMP_Text>(); distribution=panel.transform.Find("Distribution")?.GetComponent<TMP_Text>();
            flow=panel.transform.Find("Flow")?.GetComponent<TMP_Text>(); indication=panel.transform.Find("Scrubber")?.GetComponent<TMP_Text>();
            install?.onClick.AddListener(Install); restart?.onClick.AddListener(Restart);
        }
        public void Initialize(InputPolicyCoordinator policy, UnityInputHost host)
        {
            Release(); input = host; coordinator = new ScrubberScreenCoordinator(policy, this);
            if(equipment!=null) equipment.Changed+=Refresh;
        }
        public void Release() { if(equipment!=null) equipment.Changed-=Refresh; coordinator?.Dispose(); coordinator = null; input = null; }
        private void OnEnable() => Refresh();
        private void OnDisable() => Close();
        private void OnDestroy() { Release(); if (close != null) close.onClick.RemoveListener(Close); install?.onClick.RemoveListener(Install); restart?.onClick.RemoveListener(Restart); }
        public bool Open() => coordinator?.Open() == OpenResult.Opened;
        public bool Back()
        {
            if (coordinator?.IsOpen != true) return false;
            Close(); return true;
        }
        public void Show()
        {
            if (panel == null || equipment == null || telemetry == null || objective == null || feedback == null || close == null)
                throw new System.InvalidOperationException("Scrubber screen authoring is incomplete.");
            Refresh();
            feedback.text = "";
            panel.SetActive(true);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(selected != null && selected.activeInHierarchy ? selected : close.gameObject);
        }
        private void Refresh()
        {
            if(equipment==null || telemetry==null || objective==null) return;
            var state = equipment.State;
            telemetry.text = "OPERATING STATE   " + state.OperatingStatus + "\nFAULT   " + state.Fault +
                "\nFUSE   " + state.FuseStatus + "\nAIRFLOW   " + state.AirflowStatus + "\nROOM CO2   " + state.RoomCondition;
            objective.text = "CURRENT TASK\n" + state.Objective.Replace("—", "-");
            if(install!=null) install.interactable=equipment.InstallReason.Length==0;
            if(installReason!=null) installReason.text=equipment.InstallReason;
            if(intake!=null) intake.text="INTAKE\nFLOW "+state.AirflowStatus;
            if(distribution!=null) distribution.text="DISTRIBUTION\nFLOW "+state.AirflowStatus;
            if(flow!=null) flow.text=state.Running ? "-> ACTIVE ->" : "-> INTERRUPTED ->";
            if(indication!=null) { indication.text="SCRUBBER\n"+state.OperatingStatus; indication.color=state.Running ? new Color(.3f,.85f,.42f) : new Color(.9f,.26f,.24f); }
            if(restart!=null) restart.interactable=!state.Running;
            if(restartLabel!=null) restartLabel.text=state.Running ? "ALREADY RUNNING" : "RESTART";
        }
        private void Install() { if(!IsOpen || coordinator?.IsOpen!=true || !isActiveAndEnabled) return; feedback.text=equipment.Install(); Refresh(); }
        private void Restart() { if(!IsOpen || coordinator?.IsOpen!=true || !isActiveAndEnabled) return; feedback.text=equipment.Restart(); Refresh(); }
        public void Hide()
        {
            var current = EventSystem.current == null ? null : EventSystem.current.currentSelectedGameObject;
            if (current != null && panel != null && current.transform.IsChildOf(panel.transform))
            { selected = current; EventSystem.current.SetSelectedGameObject(null); }
            if (panel != null) panel.SetActive(false);
        }
        public void Close() { if (!IsOpen) return; input?.ConsumeUIActivation(); coordinator?.Close(); }
    }
}
