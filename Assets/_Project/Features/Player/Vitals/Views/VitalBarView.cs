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
            TryBind();
        }

        private void LateUpdate()
        {
            if (_vital == null) TryBind();
        }

        private void TryBind()
        {
            // The authored HUD can enable before the player's Awake initializes resources.
            var controller = PlayerVitalsController.Instance;
            if (fillImage == null || controller == null || controller.Vitals == null) return;

            _vital = kind == VitalKind.Health
                ? controller.Vitals.Health
                : controller.Vitals.Oxygen;

            _vital.Changed += HandleChanged;
            HandleChanged(_vital); // sync initial fill immediately
        }

        private void OnDisable()
        {
            if (_vital != null) _vital.Changed -= HandleChanged;
            _vital = null;
        }

        private void HandleChanged(Vital vital) => fillImage.fillAmount = vital.Normalized;
    }
}
