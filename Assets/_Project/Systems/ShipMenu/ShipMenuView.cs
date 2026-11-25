using System;
using SOLITUDE.Core.Input;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SOLITUDE.UI
{
    public enum ShipMenuTab { Inventory, Objectives, Map, Crew, Data, Settings }

    public sealed class ShipMenuView : MonoBehaviour
    {
        [Serializable]
        private sealed class Tab
        {
            public Button button;
            public Graphic fill, icon;
            public TMP_Text label;
        }

        [SerializeField] private Tab[] tabs;
        [SerializeField] private GameObject blankPage;
        [SerializeField] private Button close;
        [SerializeField] private TMP_Text closeKey, closeLabel;
        [SerializeField] private Color selectedFill, focusFill, selectedText, idleText;
        private UnityInputHost input;
        private Func<bool> cancelMove;
        private Action back;
        private UnityAction[] tabActions;

        internal ShipMenuTab SelectedTab { get; private set; }
        public GameObject InventoryFocus => tabs[0].button.gameObject;

        public void Initialize(UnityInputHost host, Action<ShipMenuTab> selectTab, Action closeMenu, Func<bool> hasMove)
        {
            if (tabs == null || tabs.Length != Enum.GetValues(typeof(ShipMenuTab)).Length
                || Array.Exists(tabs, tab => tab == null || tab.button == null || tab.fill == null || tab.icon == null || tab.label == null)
                || blankPage == null || close == null || closeKey == null || closeLabel == null)
                throw new InvalidOperationException("Ship menu authored endpoints are incomplete.");

            Release();
            input = host;
            input.InputDeviceChanged += RefreshCloseKey;
            RefreshCloseKey();
            back = closeMenu;
            cancelMove = hasMove;
            tabActions = new UnityAction[tabs.Length];
            for (int i = 0; i < tabs.Length; i++)
            {
                var tab = (ShipMenuTab)i;
                tabActions[i] = () => selectTab(tab);
                tabs[i].button.onClick.AddListener(tabActions[i]);
            }
            close.onClick.AddListener(Close);
        }

        public void Show(ShipMenuTab tab)
        {
            SelectedTab = tab;
            bool isBlank = tab != ShipMenuTab.Inventory && tab != ShipMenuTab.Objectives;
            blankPage.SetActive(isBlank);
            gameObject.SetActive(true);
            if (isBlank)
                EventSystem.current?.SetSelectedGameObject(tabs[(int)tab].button.gameObject);
            LateUpdate();
        }

        public void Hide()
        {
            var focused = EventSystem.current?.currentSelectedGameObject;
            if (focused != null && focused.transform.IsChildOf(transform))
                EventSystem.current.SetSelectedGameObject(null);
            gameObject.SetActive(false);
        }

        private void Close() => back?.Invoke();

        private void LateUpdate()
        {
            var focused = EventSystem.current?.currentSelectedGameObject;
            for (int i = 0; i < tabs.Length; i++)
            {
                Tab tab = tabs[i];
                bool selected = i == (int)SelectedTab;
                tab.fill.color = selected ? selectedFill : focused == tab.button.gameObject ? focusFill : Color.clear;
                tab.label.color = tab.icon.color = selected ? selectedText : idleText;
            }
            closeLabel.text = cancelMove?.Invoke() == true ? "Cancel move" : "Close";
            closeLabel.color = focused == close.gameObject ? selectedFill : idleText;
        }

        private void RefreshCloseKey() => closeKey.text = input.DeviceBindingLabel("Container/Cancel");

        public void Release()
        {
            if (tabActions != null)
            {
                for (int i = 0; i < tabActions.Length; i++)
                    if (tabs[i].button != null) tabs[i].button.onClick.RemoveListener(tabActions[i]);
                tabActions = null;
            }
            if (close != null) close.onClick.RemoveListener(Close);
            if (input != null) input.InputDeviceChanged -= RefreshCloseKey;
            input = null;
            back = null;
            cancelMove = null;
            Hide();
        }
    }
}
