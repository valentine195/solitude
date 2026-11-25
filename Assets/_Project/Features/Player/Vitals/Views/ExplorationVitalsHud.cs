using SOLITUDE.Vitals;
using UnityEngine;

namespace SOLITUDE.Player.UI
{
    [DisallowMultipleComponent]
    public sealed class ExplorationVitalsHud : MonoBehaviour
    {
        [SerializeField] private PlayerVitalsController controller;
        [SerializeField] private CanvasGroup display;
        [SerializeField] private VitalsHudMeter health;
        [SerializeField] private VitalsHudMeter oxygen;
        [SerializeField] private CanvasGroup hunger;
        [SerializeField, Min(0)] private float hungerDuration = .18f;
        [SerializeField, Min(0)] private float recoverySweepDuration = .30f;

        private float healthValue;
        private int healthSegments;
        private bool visible;
        private bool hungry;
        private float hungerProgress;
        private float sweepElapsed = -1;
        private Vector2 hungerPosition;

        private void Awake() => hungerPosition = ((RectTransform)hunger.transform).anchoredPosition;

        private void OnEnable()
        {
            if (controller != null) controller.PresentationChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (!ReferenceEquals(controller, null)) controller.PresentationChanged -= Refresh;
            visible = false;
            sweepElapsed = -1;
        }

        private void Refresh()
        {
            bool show = controller != null && controller.Vitals != null && controller.GameplayEligible;
            display.alpha = show ? 1 : 0;
            if (!show)
            {
                visible = false;
                sweepElapsed = -1;
                return;
            }

            var vitals = controller.Vitals;
            var settings = controller.Settings;
            bool valid = controller.IsConfigurationValid;
            int segments = valid ? settings.SegmentCount : 1;
            float nextHealth = vitals.Health.Normalized;
            // A completed passive recovery is distinct from loading values or using a medical item.
            if (!visible || healthSegments != segments)
                sweepElapsed = -1;
            else if (nextHealth != healthValue)
                sweepElapsed = nextHealth > healthValue && recoverySweepDuration > 0
                    && controller.IsPublishingPassiveAdvance && controller.RecoveryStatus == RecoveryReason.AtCeiling ? 0 : -1;

            healthSegments = segments;
            healthValue = nextHealth;
            RenderHealth();
            oxygen.Show(vitals.Oxygen.Normalized, 1);

            hungry = valid && vitals.Hunger.PreciseCurrent < settings.FedThreshold;
            if (!visible || hungerDuration <= 0)
            {
                hungerProgress = hungry ? 1 : 0;
                RenderHunger();
            }
            visible = true;
        }

        private void LateUpdate()
        {
            if (!visible) return;

            float target = hungry ? 1 : 0;
            if (hungerProgress != target)
            {
                hungerProgress = hungerDuration > 0
                    ? Mathf.MoveTowards(hungerProgress, target, Time.unscaledDeltaTime / hungerDuration)
                    : target;
                RenderHunger();
            }

            if (sweepElapsed < 0) return;
            sweepElapsed += Time.unscaledDeltaTime;
            if (sweepElapsed >= recoverySweepDuration) sweepElapsed = -1;
            RenderHealth();
        }

        private void RenderHealth() =>
            health.Show(healthValue, healthSegments, sweepElapsed < 0 ? -1 : sweepElapsed / recoverySweepDuration);

        private void RenderHunger()
        {
            float reveal = 1 - (1 - hungerProgress) * (1 - hungerProgress);
            hunger.alpha = reveal;
            ((RectTransform)hunger.transform).anchoredPosition = hungerPosition + new Vector2(-6 * (1 - reveal), 0);
        }
    }
}
