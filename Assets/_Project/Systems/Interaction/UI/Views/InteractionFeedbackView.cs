using System.Collections;
using TMPro;
using UnityEngine;

namespace SOLITUDE.Core.UI
{
    public class InteractionFeedbackView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private float duration = 1.5f;

        private Coroutine routine;

        public void Show(string message, Color color)
        {
            if (routine != null)
                StopCoroutine(routine);
            root.SetActive(true);
            routine = StartCoroutine(ShowRoutine(message, color));
        }

        private IEnumerator ShowRoutine(string message, Color color)
        {
            label.gameObject.SetActive(true);
            label.text = message;
            label.color = color;

            yield return new WaitForSeconds(duration);

            label.gameObject.SetActive(false);
            root.SetActive(false);
        }
    }
}