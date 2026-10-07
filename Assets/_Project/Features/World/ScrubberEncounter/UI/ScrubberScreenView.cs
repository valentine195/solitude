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
        public bool IsOpen => panel != null && panel.activeSelf;
        public string Telemetry => telemetry == null ? "" : telemetry.text;
        public void Configure(GameObject root, ScrubberEquipment unit, TMP_Text readings, TMP_Text task, TMP_Text result, UnityEngine.UI.Button closeButton)
        { panel = root; equipment = unit; telemetry = readings; objective = task; feedback = result; close = closeButton; }
        private void Awake() { if (close != null) close.onClick.AddListener(Close); }
        public void Initialize(InputPolicyCoordinator policy, UnityInputHost host)
        {
            Release(); input = host; coordinator = new ScrubberScreenCoordinator(policy, this);
        }
        public void Release() { coordinator?.Dispose(); coordinator = null; input = null; }
        private void OnDisable() => Close();
        private void OnDestroy() { Release(); if (close != null) close.onClick.RemoveListener(Close); }
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
            var state = equipment.State;
            telemetry.text = "OPERATING STATE   " + state.OperatingStatus + "\nFAULT   " + state.Fault +
                "\nFUSE   " + state.FuseStatus + "\nAIRFLOW   " + state.AirflowStatus + "\nROOM CO₂   " + state.RoomCondition;
            objective.text = "CURRENT TASK\n" + state.Objective;
            feedback.text = "";
            panel.SetActive(true);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(selected != null && selected.activeInHierarchy ? selected : close.gameObject);
        }
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
