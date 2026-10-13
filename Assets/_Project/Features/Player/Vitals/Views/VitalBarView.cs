using System.Collections.Generic;
using SOLITUDE.Player.Vitals;
using UnityEngine;

namespace SOLITUDE.Player.UI
{
    // Existing prefab values are stable: Health=0, Oxygen=1.
    public enum VitalKind { Health = 0, Oxygen = 1, Hunger = 2 }

    public class VitalBarView : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Image fillImage;
        [SerializeField] private VitalKind kind;
        [SerializeField] private RectTransform markerRoot;
        [SerializeField] private RectTransform markerPrefab;

        private PlayerVitalsController controller;
        private Vital vital;
        private readonly List<RectTransform> markers = new List<RectTransform>();

        private void OnEnable() => TryBind();
        private void LateUpdate()
        {
            // A HUD can enable before the player, or outlive a scene's controller.
            if (controller == null || controller != PlayerVitalsController.Instance || vital == null) TryBind();
        }
        private void TryBind()
        {
            Unbind();
            controller = PlayerVitalsController.Instance;
            if (fillImage == null || controller == null || controller.Vitals == null)
            { controller = null; SetMarkerCount(0); return; }
            vital = kind == VitalKind.Health ? controller.Vitals.Health
                : kind == VitalKind.Hunger ? controller.Vitals.Hunger : controller.Vitals.Oxygen;
            vital.Changed += HandleChanged;
            controller.PresentationChanged += Synchronize;
            Synchronize();
        }
        private void OnDisable() => Unbind();
        private void Unbind()
        {
            if (vital != null) vital.Changed -= HandleChanged;
            if (!ReferenceEquals(controller, null)) controller.PresentationChanged -= Synchronize;
            vital = null; controller = null;
        }
        private void HandleChanged(Vital value)
        { if (fillImage != null) fillImage.fillAmount = value.Normalized; }
        private void Synchronize()
        {
            if (vital == null || controller == null) return;
            HandleChanged(vital);
            var settings = controller.Settings;
            int count = controller.IsConfigurationValid && markerRoot != null && markerPrefab != null
                ? kind == VitalKind.Health ? settings.SegmentCount - 1 : kind == VitalKind.Hunger ? 1 : 0 : 0;
            SetMarkerCount(count);
            for (int i = 0; i < markers.Count; i++)
            {
                float fraction = kind == VitalKind.Hunger ? (float)settings.FedThresholdFraction
                    : (float)(i + 1) / settings.SegmentCount;
                var marker = markers[i];
                marker.anchorMin = new Vector2(fraction, 0);
                marker.anchorMax = new Vector2(fraction, 1);
                marker.anchoredPosition = Vector2.zero;
            }
        }
        private void SetMarkerCount(int count)
        {
            while (markers.Count > count)
            {
                int last = markers.Count - 1;
                var marker = markers[last]; markers.RemoveAt(last);
                marker.gameObject.SetActive(false); Destroy(marker.gameObject);
            }
            while (markers.Count < count)
            {
                var marker = Instantiate(markerPrefab, markerRoot, false);
                marker.name = "VitalBoundary";
                // Authored marker graphics are decorative even if a template is misconfigured.
                foreach (var graphic in marker.GetComponentsInChildren<UnityEngine.UI.Graphic>(true)) graphic.raycastTarget = false;
                marker.gameObject.SetActive(true); markers.Add(marker);
            }
        }
    }
}
