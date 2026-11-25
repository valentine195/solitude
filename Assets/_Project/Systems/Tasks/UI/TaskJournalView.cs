using System;
using System.Collections.Generic;
using SOLITUDE.Application;
using SOLITUDE.Core.Input;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SOLITUDE.Tasks.UI
{
    public sealed class TaskJournalView : MonoBehaviour, ITaskJournalDisplay, ITaskJournalScreenDisplay
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private UnityEngine.UI.Button activeTab, completedTab;
        [SerializeField] private UnityEngine.UI.Image activeUnderline, completedUnderline;
        [SerializeField] private UnityEngine.UI.Image outcomeIcon;
        [SerializeField] private TMP_Text activeCount, completedCount, title, location, condition, confirmed, outcome, availability, stepLabel, stepText, blocker, empty;
        [SerializeField] private TMP_Text journalKey, backKey;
        [SerializeField] private UnityEngine.UI.ScrollRect listScroll, detailScroll;
        [SerializeField] private TaskJournalRowView rowPrefab;
        [SerializeField] private GameObject currentStepMark, currentStepSurface;
        [SerializeField] private UnityEngine.UI.Graphic titleMark;
        [SerializeField] private UnityEngine.UI.Image currentStepBackground;
        public event Action VisibilityChanged;
        private readonly Dictionary<TaskKey, TaskJournalRowView> rows = new();
        private TaskJournalPresenter presenter;
        private TaskJournalScreenCoordinator screen;
        private UnityInputHost input;
        private GameObject lastFocused;
        public bool IsOpen => screen?.IsOpen == true;
        public TaskJournalPresenter Presenter => presenter;
        public TaskJournalScreenCoordinator Screen => screen;
        public static readonly Color Amber = new Color(.95f,.66f,.23f);
        public static readonly Color White = new Color(.9f,.91f,.87f);
        public static readonly Color Fault = new Color(.9f,.26f,.24f);
        public static readonly Color Operational = new Color(.3f,.85f,.42f);
        private static readonly Color Gray = new Color(.6f,.67f,.68f);
        public static Color ConditionColor(TaskCondition value) => value == TaskCondition.Fault ? Fault : value == TaskCondition.Normal ? Operational : value == TaskCondition.Unknown ? Gray : Amber;
        public void ValidateAuthoring()
        {
            if (panel == null || activeTab == null || completedTab == null || activeUnderline == null || completedUnderline == null || outcomeIcon == null ||
                activeCount == null || completedCount == null || title == null || location == null || condition == null || confirmed == null || outcome == null || availability == null ||
                stepLabel == null || stepText == null || blocker == null || empty == null || journalKey == null || backKey == null ||
                listScroll?.content == null || listScroll.viewport == null || detailScroll?.content == null || detailScroll.viewport == null || rowPrefab == null)
                throw new InvalidOperationException("Task journal authored endpoints are incomplete.");
            if (titleMark != null && (currentStepMark == null || currentStepSurface == null || currentStepBackground == null
                || currentStepSurface.GetComponent<UnityEngine.UI.LayoutElement>() == null))
                throw new InvalidOperationException("Objective detail endpoints are incomplete.");
            rowPrefab.ValidateAuthoring();
            foreach (var label in GetComponentsInChildren<TMP_Text>(true)) if (label.richText) throw new InvalidOperationException("Task journal text must be literal.");
        }
        public void Initialize(TaskJournal journal, InputPolicyCoordinator policy, PauseCoordinator pause, UnityInputHost host)
        {
            Release(); ValidateAuthoring(); input = host;
            presenter = new TaskJournalPresenter(journal, this); presenter.PresentationError += PresentationFailed;
            screen = new TaskJournalScreenCoordinator(policy, pause, this); screen.PresentationError += Debug.LogException;
            activeTab.onClick.AddListener(Active); completedTab.onClick.AddListener(Completed);
            panel.SetActive(false);
        }
        private void Active() { Remember(); presenter?.SelectTab(TaskJournalTab.Active); }
        private void Completed() { Remember(); presenter?.SelectTab(TaskJournalTab.Completed); }
        private void Select(TaskKey key) { Remember(); presenter?.Select(key); }
        public void Show()
        {
            ShowPage();
            VisibilityChanged?.Invoke();
        }
        public void Hide()
        {
            HidePage();
            VisibilityChanged?.Invoke();
        }
        internal void ShowPage()
        {
            if (!isActiveAndEnabled || presenter == null) throw new InvalidOperationException("Journal view is unavailable.");
            if (presenter.IsVisible) return;
            panel.SetActive(true); presenter.SetVisible(true, requireRender: true); RestoreFocus(presenter.Presentation.FocusToken);
        }
        internal void HidePage()
        {
            Remember(); presenter?.SetVisible(false);
            if (EventSystem.current?.currentSelectedGameObject != null && EventSystem.current.currentSelectedGameObject.transform.IsChildOf(panel.transform))
                EventSystem.current.SetSelectedGameObject(null);
            panel.SetActive(false);
        }
        private static string Token(TaskKey key) => "row:" + key.SourceId + "/" + key.LocalTaskId;
        private void Remember()
        {
            if (presenter?.IsVisible != true) return;
            var selected = EventSystem.current?.currentSelectedGameObject; string token = null;
            if (selected == activeTab.gameObject) token = "active";
            else if (selected == completedTab.gameObject) token = "completed";
            else foreach (var row in rows.Values) if (selected == row.Button.gameObject) { token = Token(row.Key); break; }
            presenter.RememberPosition(token, listScroll.verticalNormalizedPosition, detailScroll.verticalNormalizedPosition);
        }
        private void RestoreFocus(string token)
        {
            GameObject target = null;
            if (token == "completed") target = completedTab.gameObject;
            else if (token == "active") target = activeTab.gameObject;
            else foreach (var row in rows.Values) if (Token(row.Key) == token) { target = row.Button.gameObject; break; }
            if (target == null && presenter.Presentation.SelectedKey.HasValue && rows.TryGetValue(presenter.Presentation.SelectedKey.Value,out var current)) target = current.Button.gameObject;
            if (target == null) target = presenter.Presentation.Tab == TaskJournalTab.Active ? activeTab.gameObject : completedTab.gameObject;
            EventSystem.current?.SetSelectedGameObject(target);
        }
        public void Render(TaskJournalPresentation state)
        {
            var focused = EventSystem.current?.currentSelectedGameObject;
            var keep = new HashSet<TaskKey>();
            for (int i = 0; i < state.Rows.Count; i++)
            {
                var item = state.Rows[i]; keep.Add(item.Key);
                if (!rows.TryGetValue(item.Key,out var row)) { row = Instantiate(rowPrefab,listScroll.content); rows.Add(item.Key,row); }
                row.transform.SetSiblingIndex(i); row.Render(item,Select);
            }
            var removed = new List<TaskKey>();
            foreach (var pair in rows) if (!keep.Contains(pair.Key)) { if (focused == pair.Value.Button.gameObject) focused = null; pair.Value.gameObject.SetActive(false); Destroy(pair.Value.gameObject); removed.Add(pair.Key); }
            foreach (var key in removed) rows.Remove(key);
            activeCount.text = "ACTIVE  " + state.ActiveCount; completedCount.text = "COMPLETED  " + state.CompletedCount;
            activeUnderline.enabled = state.Tab == TaskJournalTab.Active; completedUnderline.enabled = state.Tab == TaskJournalTab.Completed;
            title.text = state.Title; location.text = state.Location; condition.text = state.ConditionText; condition.color = ConditionColor(state.Condition);
            confirmed.gameObject.SetActive(state.SelectedKey.HasValue);
            outcome.text = state.Outcome; outcome.color = state.Outcome == "FAILED" ? Fault : White; availability.text = state.Availability;
            outcomeIcon.enabled = state.Outcome == "FAILED" || state.Outcome == "COMPLETED";
            outcomeIcon.color = state.Outcome == "FAILED" ? Fault : Operational;
            stepLabel.text = state.StepLabel; stepText.text = state.StepText; blocker.text = state.Blocker; empty.text = state.EmptyText;
            foreach (var label in new[] { location, condition, outcome, availability, stepLabel, stepText, blocker }) label.gameObject.SetActive(!string.IsNullOrEmpty(label.text));
            if (titleMark != null)
            {
                detailScroll.gameObject.SetActive(state.SelectedKey.HasValue);
                titleMark.gameObject.SetActive(state.SelectedKey.HasValue);
                titleMark.color = state.Outcome == "FAILED" ? Fault : state.Outcome == "COMPLETED" ? Operational : Amber;
                outcome.gameObject.SetActive(state.Outcome != "ACTIVE" && !string.IsNullOrEmpty(state.Outcome));
                bool hasStep = !string.IsNullOrEmpty(state.StepText);
                bool activeStep = hasStep && state.Outcome == "ACTIVE";
                currentStepSurface.SetActive(hasStep);
                currentStepMark.SetActive(activeStep);
                currentStepBackground.enabled = activeStep;
                if (hasStep)
                {
                    var surface = (RectTransform)currentStepSurface.transform;
                    float textWidth = Mathf.Max(1, surface.rect.width - 80);
                    float textHeight = stepText.GetPreferredValues(state.StepText, textWidth, 0).y;
                    surface.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = Mathf.Max(62, textHeight + 16);
                }
            }
            journalKey.text = input.BindingLabel("Gameplay/Journal"); backKey.text = input.BindingLabel("Container/Cancel");
            Canvas.ForceUpdateCanvases(); listScroll.verticalNormalizedPosition = state.ListScroll; detailScroll.verticalNormalizedPosition = state.DetailScroll;
            if (focused == null || !focused.activeInHierarchy) RestoreFocus(state.FocusToken);
            RevealFocus();
        }
        private void LateUpdate()
        {
            if (!IsOpen || !presenter.IsVisible) return;
            var selected = EventSystem.current?.currentSelectedGameObject;
            if (selected != lastFocused) RevealFocus();
            Remember();
            foreach (var button in new[] { activeTab, completedTab })
            {
                bool focus = selected == button.gameObject;
                if (titleMark != null)
                {
                    button.image.color = focus ? new Color32(32, 66, 78, 255) : new Color32(12, 27, 37, 255);
                    button.GetComponentInChildren<TMP_Text>(true).color = focus ? new Color32(120, 243, 188, 255) : White;
                }
                else
                {
                    button.image.color = focus ? Amber : new Color(.08f,.10f,.10f);
                    button.GetComponentInChildren<TMP_Text>(true).color = focus ? Color.black : White;
                }
            }
        }
        private void PresentationFailed(Exception error)
        {
            Debug.LogException(error);
            screen?.Close();
        }
        private void RevealFocus()
        {
            var target = EventSystem.current?.currentSelectedGameObject;
            lastFocused = target;
            if (target == null) return;
            Reveal(listScroll, target.transform as RectTransform);
            Reveal(detailScroll, target.transform as RectTransform);
            Remember();
        }
        private static void Reveal(UnityEngine.UI.ScrollRect scroll, RectTransform target)
        {
            if (target == null || !target.IsChildOf(scroll.content)) return;
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport, target);
            var viewport = scroll.viewport.rect;
            float delta = bounds.size.y > viewport.height || bounds.max.y > viewport.yMax
                ? viewport.yMax - bounds.max.y
                : bounds.min.y < viewport.yMin ? viewport.yMin - bounds.min.y : 0;
            if (Mathf.Abs(delta) < .01f) return;
            scroll.StopMovement();
            scroll.content.position += scroll.viewport.TransformVector(new Vector3(0, delta, 0));
        }
        public void Release()
        {
            screen?.Dispose(); screen = null; presenter?.Dispose(); presenter = null;
            activeTab?.onClick.RemoveListener(Active); completedTab?.onClick.RemoveListener(Completed); input = null;
            if (panel != null) panel.SetActive(false);
        }
        private void OnDisable() => Release();
        private void OnDestroy() => Release();
    }
}
