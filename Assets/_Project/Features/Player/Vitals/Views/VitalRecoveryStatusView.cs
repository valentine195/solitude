using SOLITUDE.Vitals;
using TMPro;
using UnityEngine;

namespace SOLITUDE.Player.UI
{
    public sealed class VitalRecoveryStatusView : MonoBehaviour
    {
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text detailText;
        [SerializeField] private Color mutedColor = new Color(.65f, .68f, .68f);
        [SerializeField] private Color recoveringColor = new Color(.48f, .78f, .57f);
        [SerializeField] private Color hungryColor = new Color(.94f, .69f, .29f);
        [SerializeField] private Color unavailableColor = new Color(.9f, .39f, .36f);
        private PlayerVitalsController controller;

        private void OnEnable() => TryBind();
        private void LateUpdate()
        {
            if (controller != PlayerVitalsController.Instance || controller == null) TryBind();
        }
        private void TryBind()
        {
            Unbind(); controller = PlayerVitalsController.Instance;
            if (controller != null) controller.PresentationChanged += Synchronize;
            Synchronize();
        }
        private void OnDisable() => Unbind();
        private void Unbind()
        {
            if (!ReferenceEquals(controller, null)) controller.PresentationChanged -= Synchronize;
            controller = null;
        }
        private void Synchronize()
        {
            if (statusText == null) return;
            var reason = controller == null ? RecoveryReason.Unavailable : controller.RecoveryStatus;
            if (detailText != null)
            {
                detailText.text = reason == RecoveryReason.Hungry ? "Recovery paused" : "";
                detailText.color = mutedColor;
            }
            statusText.color = reason == RecoveryReason.Recovering ? recoveringColor
                : reason == RecoveryReason.Hungry ? hungryColor
                : reason == RecoveryReason.Unavailable ? unavailableColor : mutedColor;
            switch (reason)
            {
                case RecoveryReason.AtCeiling: statusText.text = ""; break;
                case RecoveryReason.Paused: statusText.text = "Recovery paused"; break;
                case RecoveryReason.Hungry: statusText.text = "HUNGRY"; break;
                case RecoveryReason.Delayed: statusText.text = "Recovery delayed"; break;
                case RecoveryReason.Recovering: statusText.text = "Recovering"; break;
                default: statusText.text = "Recovery unavailable"; break;
            }
        }
    }
}
