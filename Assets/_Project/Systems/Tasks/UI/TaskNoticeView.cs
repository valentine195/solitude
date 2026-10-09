using System;
using SOLITUDE.Application;
using SOLITUDE.Tasks;
using TMPro;
using UnityEngine;

namespace SOLITUDE.Tasks.UI
{
    public sealed class TaskNoticeView : MonoBehaviour, ITaskNoticeDisplay
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text heading, title, location, outcome, body;
        private TaskNoticeCoordinator coordinator;
        private TaskNoticePresentation last;
        public bool IsVisible => panel != null && panel.activeInHierarchy;
        public TaskNoticePresentation Presentation => last;
        public int PresentationCount { get; private set; }
        public void ValidateAuthoring()
        {
            if (panel == null || heading == null || title == null || location == null || outcome == null || body == null)
                throw new InvalidOperationException("Task notice serialized panel/text endpoints are incomplete.");
            foreach (var text in new[] { heading, title, location, outcome, body })
                if (!text.transform.IsChildOf(panel.transform) || text.richText || text.raycastTarget)
                    throw new InvalidOperationException("Task notice text must belong to its panel, render literal copy and ignore pointer input.");
        }
        public void Initialize(TaskNoticeCoordinator value)
        {
            Release(); ValidateAuthoring(); coordinator = value ?? throw new ArgumentNullException(nameof(value));
            if (isActiveAndEnabled) coordinator.BindDisplay(this);
        }
        public void Render(TaskNoticePresentation presentation)
        {
            if (presentation == null) { panel.SetActive(false); return; }
            heading.text = presentation.Heading; title.text = presentation.Title; location.text = presentation.Location;
            body.text = presentation.Text;
            outcome.text = (presentation.Outcome == "COMPLETED" ? "+ " : presentation.Outcome == "FAILED" ? "! " : "> ") + presentation.Outcome;
            outcome.color = presentation.Outcome == "COMPLETED" ? TaskJournalView.Operational :
                presentation.Outcome == "FAILED" ? TaskJournalView.Fault : new Color(.72f,.76f,.77f);
            if (last == null || last.Key != presentation.Key || last.Kind != presentation.Kind || last.Revision != presentation.Revision)
                PresentationCount++;
            last = presentation; panel.SetActive(true);
        }
        private void OnEnable() { coordinator?.BindDisplay(this); }
        private void OnDisable() { coordinator?.BindDisplay(null); if (panel != null) panel.SetActive(false); }
        public void Release()
        {
            coordinator?.BindDisplay(null); coordinator = null; last = null;
            if (panel != null) panel.SetActive(false);
        }
        private void OnDestroy() { Release(); }
    }
}
