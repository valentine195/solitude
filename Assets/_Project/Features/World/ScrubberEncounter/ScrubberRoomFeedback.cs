using TMPro;
using UnityEngine;

namespace SOLITUDE.World.ScrubberEncounter
{
    public sealed class ScrubberRoomFeedback : MonoBehaviour
    {
        [SerializeField] private AudioSource alert;
        [SerializeField] private TMP_Text warning;
        public bool IsWarningVisible => warning != null && warning.gameObject.activeInHierarchy;
        public AudioSource Alert => alert;
        public void Configure(AudioSource source, TMP_Text label) { alert = source; warning = label; }
        private void OnEnable()
        {
            if (warning != null) { warning.text = "CO₂ ELEVATED"; warning.gameObject.SetActive(true); }
            if (alert != null && alert.clip != null) alert.Play();
        }
        private void OnDisable() { if (alert != null) alert.Stop(); }
    }
}
