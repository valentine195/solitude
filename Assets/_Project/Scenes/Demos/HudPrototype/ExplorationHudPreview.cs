using System;
using UnityEngine;
using UnityEngine.InputSystem;
using SOLITUDE.UI.Exploration;

namespace SOLITUDE.Demos
{
    public sealed class ExplorationHudPreview : MonoBehaviour
    {
        [Serializable]
        private struct PreviewItem
        {
            public Sprite icon;
            public string name;
            public string description;
            public int quantity;
        }

        public enum Scenario { Exploration, Warning, WornTool }

        [SerializeField] private ExplorationHudView view;
        [SerializeField] private GameObject brightLighting;
        [SerializeField] private Sprite wrench;
        [SerializeField] private Sprite portrait;
        [SerializeField] private PreviewItem[] items;
        [SerializeField, Min(.5f)] private float detailDuration = 4f;
        [SerializeField] private Scenario initialScenario;

        private static readonly Key[] SlotKeys =
            { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5 };
        private int[] quantities;
        private int selected;
        private float detailUntil;
        private Scenario scenario;
        private float health;
        private float oxygen;
        private bool controllerActive;
        private bool bright;

        private void Start()
        {
            for (int i = 0; i < items.Length; i++)
            {
                int slot = i;
                view.QuickUseReadout.GetButton(i).onClick.AddListener(() =>
                {
                    controllerActive = false;
                    SelectSlot(slot);
                });
            }

            view.NavigationReadout.Show("DECK 3 — MEDBAY", "RESTORE LIFE SUPPORT",
                "Find O<sub>2</sub> regulator", new Vector2(.52f, .52f), 0, new Vector2(.9f, .56f));
            view.ColonistReadout.Show(portrait, "MARA VALE", "Engineer", "Repair +10%");
            ShowScenario(initialScenario);
        }

        public void ShowScenario(Scenario next)
        {
            scenario = next;
            health = next == Scenario.Warning ? 1f / 6 : 5f / 6;
            oxygen = next == Scenario.Warning ? .18f : .72f;
            quantities = Array.ConvertAll(items, item => item.quantity);
            ShowVitals();
            bool worn = next != Scenario.Exploration;
            view.EquipmentReadout.Show(wrench, "WRENCH", worn ? .26f : .8f,
                worn ? "WORN" : "DURABLE");
            SelectSlot(0);
        }

        public void SelectSlot(int index)
        {
            if ((uint)index >= items.Length)
                return;

            selected = index;
            for (int i = 0; i < items.Length; i++)
                view.QuickUseReadout.ShowSlot(i, items[i].icon, quantities[i], i == selected);
            ShowDetail();
        }

        public int SelectedIndex => selected;
        public int GetQuantity(int index) => quantities[index];

        public void UseSelected()
        {
            if (quantities[selected] <= 0)
            {
                ShowDetail();
                return;
            }

            quantities[selected]--;
            if (selected == 0) health = Mathf.Min(1f, health + .25f);
            if (selected == 3) health = Mathf.Min(1f, health + .5f);
            if (selected == 4) oxygen = Mathf.Min(1f, oxygen + .35f);
            ShowVitals();
            SelectSlot(selected);
        }

        public void SetBrightBackdrop(bool enabled)
        {
            bright = enabled;
            brightLighting.SetActive(enabled);
        }

        private void ShowVitals()
        {
            view.VitalsReadout.Show(health, oxygen, health <= .25f, oxygen <= .25f);
        }

        private void ShowDetail()
        {
            PreviewItem item = items[selected];
            bool available = quantities[selected] > 0;
            string description = available ? item.description : "Depleted";
            view.QuickUseReadout.ShowDetail($"<b>{item.name}</b>  ·  {description}",
                controllerActive ? "Y" : "F", available, 1f);
            detailUntil = Time.unscaledTime + detailDuration;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                for (int i = 0; i < SlotKeys.Length && i < items.Length; i++)
                {
                    if (!keyboard[SlotKeys[i]].wasPressedThisFrame) continue;
                    controllerActive = false;
                    SelectSlot(i);
                }

                if (keyboard.fKey.wasPressedThisFrame)
                {
                    controllerActive = false;
                    UseSelected();
                }
                if (keyboard.f1Key.wasPressedThisFrame || keyboard.f2Key.wasPressedThisFrame
                    || keyboard.f3Key.wasPressedThisFrame)
                {
                    controllerActive = false;
                    if (keyboard.f1Key.wasPressedThisFrame) ShowScenario(Scenario.Exploration);
                    else if (keyboard.f2Key.wasPressedThisFrame) ShowScenario(Scenario.Warning);
                    else ShowScenario(Scenario.WornTool);
                }
                if (keyboard.f4Key.wasPressedThisFrame) SetBrightBackdrop(!bright);
            }

            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                if (gamepad.leftShoulder.wasPressedThisFrame || gamepad.rightShoulder.wasPressedThisFrame)
                {
                    controllerActive = true;
                    int step = gamepad.rightShoulder.wasPressedThisFrame ? 1 : -1;
                    SelectSlot((selected + step + items.Length) % items.Length);
                }
                if (gamepad.buttonNorth.wasPressedThisFrame)
                {
                    controllerActive = true;
                    UseSelected();
                }
                if (gamepad.selectButton.wasPressedThisFrame)
                {
                    controllerActive = true;
                    ShowScenario((Scenario)(((int)scenario + 1) % 3));
                }
            }

            if (detailUntil > 0 && Time.unscaledTime >= detailUntil)
            {
                view.QuickUseReadout.ShowDetail(string.Empty, string.Empty, false, 0f);
                detailUntil = 0f;
            }
        }
    }
}
