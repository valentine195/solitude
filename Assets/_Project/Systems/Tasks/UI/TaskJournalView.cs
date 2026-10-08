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
        [SerializeField] private UnityEngine.UI.Button activeTab, completedTab, pinButton;
        [SerializeField] private UnityEngine.UI.Image activeUnderline, completedUnderline;
        [SerializeField] private UnityEngine.UI.Image outcomeIcon;
        [SerializeField] private TMP_Text activeCount, completedCount, title, location, condition, confirmed, outcome, availability, stepLabel, stepText, blocker, empty, pinLabel;
        [SerializeField] private TMP_Text pinKey, journalKey, backKey, pinHint;
        [SerializeField] private UnityEngine.UI.ScrollRect listScroll, detailScroll;
        [SerializeField] private TaskJournalRowView rowPrefab;
        private readonly Dictionary<TaskKey, TaskJournalRowView> rows = new();
        private TaskJournalPresenter presenter;
        private TaskJournalScreenCoordinator screen;
        private UnityInputHost input;
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
            if (panel == null || activeTab == null || completedTab == null || pinButton == null || activeUnderline == null || completedUnderline == null || outcomeIcon == null ||
                activeCount == null || completedCount == null || title == null || location == null || condition == null || confirmed == null || outcome == null || availability == null ||
                stepLabel == null || stepText == null || blocker == null || empty == null || pinLabel == null || pinKey == null || journalKey == null || backKey == null || pinHint == null ||
                listScroll?.content == null || listScroll.viewport == null || detailScroll?.content == null || detailScroll.viewport == null || rowPrefab == null)
                throw new InvalidOperationException("Task journal authored endpoints are incomplete.");
            rowPrefab.ValidateAuthoring();
            foreach (var label in GetComponentsInChildren<TMP_Text>(true)) if (label.richText) throw new InvalidOperationException("Task journal text must be literal.");
        }
        public void Initialize(TaskJournal journal, InputPolicyCoordinator policy, PauseCoordinator pause, UnityInputHost host)
        {
            Release(); ValidateAuthoring(); input = host;
            presenter = new TaskJournalPresenter(journal, journal, this); presenter.PresentationError += Debug.LogException;
            screen = new TaskJournalScreenCoordinator(policy, pause, this); screen.PresentationError += Debug.LogException;
            activeTab.onClick.AddListener(Active); completedTab.onClick.AddListener(Completed); pinButton.onClick.AddListener(Pin);
            panel.SetActive(false);
        }
        private void Active() { Remember(); presenter?.SelectTab(TaskJournalTab.Active); }
        private void Completed() { Remember(); presenter?.SelectTab(TaskJournalTab.Completed); }
        private void Pin() { Remember(); presenter?.TogglePin(); }
        private void Select(TaskKey key) { Remember(); presenter?.Select(key); }
        public void Show()
        {
            if (!isActiveAndEnabled || presenter == null) throw new InvalidOperationException("Journal view is unavailable.");
            panel.SetActive(true); presenter.SetVisible(true); RestoreFocus(presenter.Presentation.FocusToken);
        }
        public void Hide()
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
            else if (selected == pinButton.gameObject) token = "pin";
            else foreach (var row in rows.Values) if (selected == row.Button.gameObject) { token = Token(row.Key); break; }
            presenter.RememberPosition(token, listScroll.verticalNormalizedPosition, detailScroll.verticalNormalizedPosition);
        }
        private void RestoreFocus(string token)
        {
            GameObject target = null;
            if (token == "pin" && pinButton.interactable) target = pinButton.gameObject;
            else if (token == "completed") target = completedTab.gameObject;
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
            pinButton.gameObject.SetActive(state.CanPin); pinButton.interactable = state.CanPin; pinLabel.text = state.PinLabel;
            pinHint.text = state.CanPin ? state.PinLabel : "PIN UNAVAILABLE";
            pinKey.text = input.BindingLabel("Gameplay/JournalPin"); journalKey.text = input.BindingLabel("Gameplay/Journal"); backKey.text = input.BindingLabel("Container/Cancel");
            Canvas.ForceUpdateCanvases(); listScroll.verticalNormalizedPosition = state.ListScroll; detailScroll.verticalNormalizedPosition = state.DetailScroll;
            if (focused == null || !focused.activeInHierarchy) RestoreFocus(state.FocusToken);
        }
        private void LateUpdate()
        {
            if (!IsOpen) return;
            Remember();
            var selected = EventSystem.current?.currentSelectedGameObject;
            foreach (var button in new[] { activeTab, completedTab, pinButton })
            {
                bool focus = selected == button.gameObject;
                button.image.color = focus ? Amber : new Color(.08f,.10f,.10f);
                button.GetComponentInChildren<TMP_Text>(true).color = focus ? Color.black : White;
            }
        }
        public void Release()
        {
            screen?.Dispose(); screen = null; presenter?.Dispose(); presenter = null;
            activeTab?.onClick.RemoveListener(Active); completedTab?.onClick.RemoveListener(Completed); pinButton?.onClick.RemoveListener(Pin); input = null;
            if (panel != null) panel.SetActive(false);
        }
        private void OnDisable() => Release();
        private void OnDestroy() => Release();
    }
}
