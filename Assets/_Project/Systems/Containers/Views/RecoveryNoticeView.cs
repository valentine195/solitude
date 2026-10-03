using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace SOLITUDE.Containers
{
    public sealed class RecoveryNoticeView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TextMeshProUGUI message;
        [SerializeField] private Button dismiss;
        private Action acknowledged;
        public void ValidateAuthoring()
        { if (root == null || message == null || dismiss == null) throw new InvalidOperationException("Recovery notice endpoints are missing."); }
        public void Initialize() { dismiss.onClick.RemoveListener(Dismiss); dismiss.onClick.AddListener(Dismiss); root.SetActive(false); }
        public void Show(Action acknowledged)
        {
            this.acknowledged = acknowledged;
            message.text = "Your last save could not be loaded. We restored the previous backup; recent progress may be missing.";
            root.SetActive(true);
        }
        private void Dismiss()
        {
            try { acknowledged?.Invoke(); acknowledged = null; root.SetActive(false); }
            catch (Exception error) { Debug.LogWarning("[RecoveryNotice] Could not acknowledge recovery: " + error.Message); }
        }
        private void OnDestroy() { if (dismiss != null) dismiss.onClick.RemoveListener(Dismiss); acknowledged = null; }
    }
}
