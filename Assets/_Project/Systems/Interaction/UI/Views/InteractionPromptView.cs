using UnityEngine;
using TMPro;

namespace SOLITUDE.Core.UI
{
    /// <summary>
    /// Handles only rendering of interaction prompt text.
    /// No logic. No subscriptions.
    /// </summary>
    public class InteractionPromptView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TextMeshProUGUI promptText;

        public void Show(string text)
        {
            root.SetActive(true);
            promptText.text = text;
        }

        public void Hide()
        {
            root.SetActive(false);
        }
    }
}