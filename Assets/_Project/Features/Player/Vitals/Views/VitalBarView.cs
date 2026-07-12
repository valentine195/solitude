using UnityEngine;
using UnityEngine.UI;
using SOLITUDE.Player.Vitals;

namespace SOLITUDE.Player.UI
{

    public enum VitalKind { Health, Oxygen }

    public class VitalBarView : MonoBehaviour
    {
        [SerializeField] private Image fillImage;
        [SerializeField] private VitalKind kind;

        private Vital _vital;

        private void OnEnable()
        {
            _vital = kind == VitalKind.Health
                ? PlayerVitalsController.Instance.Vitals.Health
                : PlayerVitalsController.Instance.Vitals.Oxygen;

            _vital.Changed += HandleChanged;
            HandleChanged(_vital); // sync initial fill immediately
        }

        private void OnDisable()
        {
            if (_vital != null) _vital.Changed -= HandleChanged;
        }

        private void HandleChanged(Vital vital) => fillImage.fillAmount = vital.Normalized;
    }
}