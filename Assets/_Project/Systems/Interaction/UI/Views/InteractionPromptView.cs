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
            Debug.Log("PROMPT SHOW CALLED");

            var t = Time.realtimeSinceStartup;

            root.SetActive(true);

            Debug.Log("SetActive: " + (Time.realtimeSinceStartup - t));

            t = Time.realtimeSinceStartup;

            promptText.text = text;

            Debug.Log("TMP SetText: " + (Time.realtimeSinceStartup - t));
        }

        public void Hide()
        {
            root.SetActive(false);
        }
    }
}