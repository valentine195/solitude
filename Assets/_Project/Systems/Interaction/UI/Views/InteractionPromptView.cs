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
        [SerializeField] private TextMeshProUGUI inputText;
        [SerializeField] private TextMeshProUGUI promptText;
        [SerializeField] private string inputLabel = "E";
        [SerializeField] private Vector2 screenOffset = new(28f, 24f);

        private Transform target;

        private void LateUpdate()
        {
            if (target == null || root == null || !root.activeInHierarchy) return;
            var camera = Camera.main;
            if (camera == null) return;

            var screenPoint = camera.WorldToScreenPoint(target.position);
            if (screenPoint.z < 0f) { Hide(); return; }
            root.transform.position = screenPoint + (Vector3)screenOffset;
        }

        public void SetTarget(Transform value) => target = value;

        public void Show(string text)
        {
            root.SetActive(true);
            if (inputText != null) inputText.text = inputLabel;
            promptText.text = text;
        }

        public void Hide()
        {
            target = null;
            root.SetActive(false);
        }
    }
}
