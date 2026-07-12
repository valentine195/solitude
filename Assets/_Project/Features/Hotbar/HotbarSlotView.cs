using SOLITUDE.Containers;
using TMPro;
using UnityEngine;

namespace SOLITUDE.Hotbar
{
    /// <summary>
    /// Sits alongside ContainerSlotView on the hotbar's slotViewPrefab. Adds
    /// the two things unique to a hotbar slot - a number label and an
    /// equipped/active highlight - without touching icon/drag/drop logic,
    /// which ContainerSlotView already owns.
    /// </summary>
    [RequireComponent(typeof(ContainerSlotView))]
    public class HotbarSlotDecorator : MonoBehaviour
    {
        [SerializeField] private GameObject activeHighlight;
        [SerializeField] private TextMeshProUGUI numberLabel;

        private ContainerSlotView slotView;

        private void Awake()
        {
            slotView = GetComponent<ContainerSlotView>();

            // Subscribe before ContainerView calls Bind() on this instance -
            // Bound fires with this slot's real index, which a serialized
            // field can't provide since every hotbar slot is cloned from the
            // same prefab reference.
            Debug.Log($"Registering slot view Bound... ");
            slotView.Bound += HandleBound;
        }

        private void OnDestroy()
        {
            if (slotView != null) slotView.Bound -= HandleBound;
        }

        private void OnEnable()
        {
            if (Hotbar.Instance != null)
                Hotbar.Instance.ActiveIndexChanged += HandleActiveChanged;
        }

        private void OnDisable()
        {
            if (Hotbar.Instance != null)
                Hotbar.Instance.ActiveIndexChanged -= HandleActiveChanged;
        }

        private void HandleBound(int index)
        {
            if (numberLabel != null)
                numberLabel.text = (index + 1).ToString();

            // Sync immediately in case this slot bound after ActiveIndex was
            // already set (e.g. hotbar UI rebuilt while an item was equipped).
            if (Hotbar.Instance != null)
                HandleActiveChanged(Hotbar.Instance.ActiveIndex);
        }

        private void HandleActiveChanged(int activeIndex)
        {
            if (activeHighlight != null)
                activeHighlight.SetActive(activeIndex == slotView.Index);
        }
    }
}