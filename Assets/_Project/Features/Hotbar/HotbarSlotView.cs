using TMPro;
using UnityEngine;
namespace SOLITUDE.Hotbar
{
    public class HotbarSlotDecorator : MonoBehaviour
    {
        [SerializeField] private GameObject activeHighlight;
        [SerializeField] private TextMeshProUGUI numberLabel;
        public void Render(int index, bool active)
        { if (numberLabel != null) numberLabel.text = (index + 1).ToString(); if (activeHighlight != null) activeHighlight.SetActive(active); }
    }
}
