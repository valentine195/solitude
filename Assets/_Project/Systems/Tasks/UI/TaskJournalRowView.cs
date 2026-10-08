using System;
using SOLITUDE.Application;
using SOLITUDE.Tasks;
using TMPro;
using UnityEngine;

namespace SOLITUDE.Tasks.UI
{
    public sealed class TaskJournalRowView : MonoBehaviour, UnityEngine.EventSystems.ISelectHandler
    {
        [SerializeField] private UnityEngine.UI.Button select;
        [SerializeField] private UnityEngine.UI.Image selection;
        [SerializeField] private UnityEngine.UI.Image outcomeIcon;
        [SerializeField] private TMP_Text title, location, condition, outcome, availability;
        private Action<TaskKey> activate;
        public TaskKey Key { get; private set; }
        public UnityEngine.UI.Button Button => select;
        private void Awake() => select.onClick.AddListener(Activate);
        private void Activate() => activate?.Invoke(Key);
        public void OnSelect(UnityEngine.EventSystems.BaseEventData eventData) => Activate();
        public void Render(TaskRowPresentation state, Action<TaskKey> action)
        {
            Key = state.Key; activate = action;
            title.text = state.Title; location.text = state.Location; outcome.text = state.Outcome;
            condition.text = state.Condition.ToString().ToUpperInvariant(); condition.color = TaskJournalView.ConditionColor(state.Condition);
            availability.text = state.Availability;
            availability.gameObject.SetActive(!string.IsNullOrEmpty(state.Availability));
            selection.enabled = state.Selected;
            outcome.color = state.Outcome == "FAILED" ? TaskJournalView.Fault : TaskJournalView.White;
            outcomeIcon.enabled = state.Outcome == "FAILED" || state.Outcome == "COMPLETED";
            outcomeIcon.color = state.Outcome == "FAILED" ? TaskJournalView.Fault : TaskJournalView.Operational;
        }
        public void ValidateAuthoring()
        {
            if (select == null || selection == null || outcomeIcon == null || title == null || location == null || condition == null || outcome == null || availability == null)
                throw new InvalidOperationException("Task journal row endpoints are incomplete.");
        }
    }
}
