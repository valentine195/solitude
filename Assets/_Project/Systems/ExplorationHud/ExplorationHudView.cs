using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SOLITUDE.Player.UI;

namespace SOLITUDE.UI.Exploration
{
    /// <summary>Presentation bindings for an exploration HUD. Callers own state and actions.</summary>
    public sealed class ExplorationHudView : MonoBehaviour
    {
        [Serializable]
        public sealed class Vitals
        {
            [SerializeField] private VitalsHudMeter health;
            [SerializeField] private VitalsHudMeter oxygen;
            [SerializeField] private GameObject healthWarning;
            [SerializeField] private GameObject oxygenWarning;

            public void Show(float healthFraction, float oxygenFraction, bool lowHealth, bool lowOxygen)
            {
                health.Show(healthFraction, 6);
                oxygen.Show(oxygenFraction, 1);
                healthWarning.SetActive(lowHealth);
                oxygenWarning.SetActive(lowOxygen);
                var oxygenRow = (RectTransform)oxygenWarning.transform;
                var healthRow = (RectTransform)healthWarning.transform;
                oxygenRow.anchoredPosition = healthRow.anchoredPosition + (lowHealth ? Vector2.down * 30 : Vector2.zero);
            }
        }

        [Serializable]
        public sealed class Navigation
        {
            [SerializeField] private TMP_Text location;
            [SerializeField] private TMP_Text objective;
            [SerializeField] private TMP_Text task;
            [SerializeField] private GameObject objectiveBlock;
            [SerializeField] private RectTransform playerMarker;
            [SerializeField] private RectTransform objectiveMarker;

            public void Show(string locationName, string objectiveTitle, string currentTask,
                Vector2 playerPosition, float playerHeading, Vector2? objectivePosition)
            {
                ShowLocation(locationName, objectiveTitle, currentTask, objectivePosition);
                ShowPlayer(playerPosition, playerHeading);
            }

            public void ShowLocation(string locationName, string objectiveTitle, string currentTask,
                Vector2? objectivePosition)
            {
                location.text = locationName;
                objective.text = objectiveTitle;
                task.text = currentTask;
                objectiveBlock.SetActive(!string.IsNullOrEmpty(objectiveTitle));
                objectiveMarker.gameObject.SetActive(objectivePosition.HasValue);
                if (objectivePosition.HasValue)
                    PlaceMarker(objectiveMarker, objectivePosition.Value);
            }

            public void ShowPlayer(Vector2 position, float heading)
            {
                PlaceMarker(playerMarker, position);
                playerMarker.localRotation = Quaternion.Euler(0, 0, -heading);
            }

            private static void PlaceMarker(RectTransform marker, Vector2 position)
            {
                marker.anchorMin = marker.anchorMax = new Vector2(
                    Mathf.Clamp01(position.x), 1 - Mathf.Clamp01(position.y));
                marker.anchoredPosition = Vector2.zero;
            }
        }

        [Serializable]
        public sealed class Equipment
        {
            [SerializeField] private Image icon;
            [SerializeField] private TMP_Text itemName;
            [SerializeField] private TMP_Text condition;
            [SerializeField] private VitalsHudMeter durability;

            public void Show(Sprite sprite, string name, float normalizedDurability, string conditionText)
            {
                ShowItem(sprite, name, conditionText);
                durability.Show(normalizedDurability, 5);
            }

            public void ShowItem(Sprite sprite, string name, string detail)
            {
                icon.sprite = sprite;
                icon.enabled = sprite != null;
                itemName.text = name;
                condition.text = detail;
            }
        }

        [Serializable]
        public sealed class QuickUseSlot
        {
            [SerializeField] private Button button;
            [SerializeField] private Image icon;
            [SerializeField] private TMP_Text quantity;
            [SerializeField] private Graphic rim;

            public Button Button => button;

            public void Show(Sprite sprite, int count, bool selected, Color quiet, Color accent)
            {
                icon.sprite = sprite;
                icon.enabled = sprite != null;
                icon.color = count > 0 ? Color.white : new Color(1, 1, 1, .42f);
                quantity.text = sprite == null ? string.Empty : $"×{Mathf.Max(0, count)}";
                rim.color = selected ? accent : quiet;
            }
        }

        [Serializable]
        public sealed class QuickUse
        {
            [SerializeField] private QuickUseSlot[] slots;
            [SerializeField] private CanvasGroup detail;
            [SerializeField] private TMP_Text description;
            [SerializeField] private GameObject prompt;
            [SerializeField] private TMP_Text inputGlyph;
            [SerializeField] private Color quietRim;
            [SerializeField] private Color selectionRim;

            public int Count => slots.Length;
            public Button GetButton(int index) => slots[index].Button;

            public void ShowSlot(int index, Sprite sprite, int count, bool selected)
            {
                slots[index].Show(sprite, count, selected, quietRim, selectionRim);
            }

            public void ShowDetail(string copy, string glyph, bool canUse, float opacity)
            {
                description.text = copy;
                inputGlyph.text = glyph;
                prompt.SetActive(canUse);
                detail.alpha = Mathf.Clamp01(opacity);
            }
        }

        [Serializable]
        public sealed class Colonist
        {
            [SerializeField] private Image portrait;
            [SerializeField] private TMP_Text colonistName;
            [SerializeField] private TMP_Text background;
            [SerializeField] private TMP_Text trait;

            public void Show(Sprite illustration, string name, string profession, string definingTrait)
            {
                portrait.sprite = illustration;
                portrait.enabled = illustration != null;
                colonistName.text = name;
                background.text = profession;
                trait.text = definingTrait;
            }
        }

        [SerializeField] private Vitals vitals;
        [SerializeField] private Navigation navigation;
        [SerializeField] private Equipment equipment;
        [SerializeField] private QuickUse quickUse;
        [SerializeField] private Colonist colonist;

        public Vitals VitalsReadout => vitals;
        public Navigation NavigationReadout => navigation;
        public Equipment EquipmentReadout => equipment;
        public QuickUse QuickUseReadout => quickUse;
        public Colonist ColonistReadout => colonist;
    }
}
