#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SOLITUDE.Application;
using SOLITUDE.Composition;
using SOLITUDE.Player;
using SOLITUDE.Player.UI;
using SOLITUDE.Player.Vitals;
using SOLITUDE.Vitals;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SOLITUDE.Tests
{
    public class PlayerVitalsPresentationTests
    {
        private GameObject host, hud, canvas;
        private PlayerVitalsController player;
        private PlayerVitalsComposition feature;
        private InputPolicyCoordinator policy;
        private TMP_Text status;
        private TMP_Text detail;
        private float previousScale;
        private Scene fixtureScene, previousScene;
        private const string Folder = "Assets/_Project/Features/Player/Vitals/Views/";
        private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private SceneFeatureContext Context => new SceneFeatureContext(null, null, null, policy, null, null, "vitals-presentation", null, null);

        [UnitySetUp] public IEnumerator Prepare()
        {
            previousScale = Time.timeScale; Time.timeScale = 1;
            foreach (var root in Object.FindObjectsByType<GameCompositionRoot>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Object.Destroy(root.gameObject);
            yield return null;
            // Isolate gameplay objects without unloading the scene containing Unity's runner.
            foreach (var view in Object.FindObjectsByType<VitalBarView>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Object.Destroy(view.gameObject);
            foreach (var view in Object.FindObjectsByType<VitalRecoveryStatusView>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Object.Destroy(view.gameObject);
            foreach (var controller in Object.FindObjectsByType<PlayerVitalsController>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Object.Destroy(controller.gameObject);
            yield return null;
            previousScene = SceneManager.GetActiveScene();
            fixtureScene = SceneManager.CreateScene("PlayerVitalsPresentation"); SceneManager.SetActiveScene(fixtureScene);
            policy = new InputPolicyCoordinator(new PauseCoordinator());
            CreatePlayer();
            canvas = new GameObject("PresentationCanvas", typeof(RectTransform), typeof(Canvas));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            hud = Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "PlayerVitalsHud.prefab"), canvas.transform, false);
            status = hud.GetComponentInChildren<VitalRecoveryStatusView>(true).GetComponent<TMP_Text>();
            detail = status.transform.Find("RecoveryDetail").GetComponent<TMP_Text>();
        }
        private void CreatePlayer()
        {
            host = new GameObject("PresentationPlayer"); player = host.AddComponent<PlayerVitalsController>();
            feature = host.AddComponent<PlayerVitalsComposition>(); feature.Configure(player); feature.Initialize(Context); policy.SetReady(true);
        }
        [TearDown] public void Cleanup()
        {
            if (canvas != null) Object.DestroyImmediate(canvas);
            if (host != null) Object.DestroyImmediate(host);
            policy?.Dispose(); Time.timeScale = previousScale;
        }
        [UnityTearDown] public IEnumerator ReleaseFixtureScene()
        {
            if (previousScene.IsValid() && previousScene.isLoaded) SceneManager.SetActiveScene(previousScene);
            if (fixtureScene.IsValid() && fixtureScene.isLoaded) yield return SceneManager.UnloadSceneAsync(fixtureScene);
        }
        private Transform Bar(string row) => hud.transform.Find(row + "/" + row.Replace("Row", "Bar") + "/Bar");
        private UnityEngine.UI.Image Fill(string row) => Bar(row).Find("Fill").GetComponent<UnityEngine.UI.Image>();
        private RectTransform[] Markers(string row) => Bar(row).Find("Boundaries").Cast<Transform>()
            .Where(t => t.gameObject.activeSelf).Select(t => (RectTransform)t).ToArray();
        private void AssertMarkers(string row, params float[] fractions)
        {
            var markers = Markers(row); Assert.AreEqual(fractions.Length, markers.Length);
            for (int i = 0; i < markers.Length; i++)
            {
                Assert.AreEqual(fractions[i], markers[i].anchorMin.x, 1e-6f);
                Assert.AreEqual(fractions[i], markers[i].anchorMax.x, 1e-6f);
                Assert.AreEqual(0, markers[i].anchorMin.y); Assert.AreEqual(1, markers[i].anchorMax.y);
            }
        }
        private static int ViewSubscriptions(object source, string eventName)
        {
            var callbacks = source.GetType().GetField(eventName, Private).GetValue(source) as Delegate;
            return callbacks?.GetInvocationList().Count(d => d.Target is VitalBarView || d.Target is VitalRecoveryStatusView) ?? 0;
        }

        [Test] public void ExistingKindsAndPrefabGuidArePreservedAndDecorationConsumesNoInput()
        {
            Assert.AreEqual(0, (int)VitalKind.Health); Assert.AreEqual(1, (int)VitalKind.Oxygen); Assert.AreEqual(2, (int)VitalKind.Hunger);
            Assert.AreEqual("382ef7bd2db05481c83d13ec043d7890", UnityEditor.AssetDatabase.AssetPathToGUID(Folder + "VitalSlider.prefab"));
            Assert.AreEqual(3, hud.GetComponentsInChildren<VitalBarView>().Length);
            Assert.AreEqual(1, hud.GetComponentsInChildren<VitalRecoveryStatusView>().Length);
            foreach (var graphic in hud.GetComponentsInChildren<UnityEngine.UI.Graphic>(true)) Assert.IsFalse(graphic.raycastTarget);
            AssertMarkers("HealthRow", .25f, .5f, .75f); AssertMarkers("HungerRow", .2f); AssertMarkers("OxygenRow");
            Canvas.ForceUpdateCanvases();
            foreach (var label in hud.GetComponentsInChildren<TMP_Text>())
            { Assert.Greater(label.rectTransform.rect.width, 0); Assert.Greater(label.rectTransform.rect.height, 0); }
            Assert.Greater(Fill("HealthRow").rectTransform.rect.width, 0); Assert.Greater(Fill("HealthRow").rectTransform.rect.height, 0);
        }

        [Test] public void ConfigurationRefreshesBoundaryCollectionWithoutAccumulationOrChangingValues()
        {
            player.Vitals.Health.Set(30); player.Vitals.Hunger.Set(60);
            player.Configure(new VitalsRecoverySettings(segmentCount: 3)); AssertMarkers("HealthRow", 1f / 3, 2f / 3);
            player.Configure(new VitalsRecoverySettings(segmentCount: 2)); AssertMarkers("HealthRow", .5f);
            player.Configure(new VitalsRecoverySettings()); AssertMarkers("HealthRow", .25f, .5f, .75f);
            for (int i = 0; i < 5; i++) player.Vitals.Hunger.Set(60 - i);
            AssertMarkers("HealthRow", .25f, .5f, .75f);
            Assert.AreEqual(30, player.Vitals.Health.Current); Assert.AreEqual(100, player.Vitals.Oxygen.Current);
        }

        [Test] public void MaximumAndThresholdChangesRefreshFillWithCurrentsHeldConstant()
        {
            player.Vitals.Health.Set(30); player.Vitals.Hunger.Set(60); player.Vitals.Oxygen.Set(40);
            Assert.AreEqual(.3f, Fill("HealthRow").fillAmount); Assert.AreEqual(.6f, Fill("HungerRow").fillAmount);
            player.Configure(new VitalsRecoverySettings(maxHealth: 200, maxHunger: 200, segmentCount: 3, fedThresholdFraction: .35));
            Assert.AreEqual(30, player.Vitals.Health.Current); Assert.AreEqual(60, player.Vitals.Hunger.Current);
            Assert.AreEqual(.15f, Fill("HealthRow").fillAmount); Assert.AreEqual(.3f, Fill("HungerRow").fillAmount);
            Assert.AreEqual(.4f, Fill("OxygenRow").fillAmount);
            AssertMarkers("HealthRow", 1f / 3, 2f / 3); AssertMarkers("HungerRow", .35f);
            player.Vitals.Oxygen.SetMax(200); Assert.AreEqual(.2f, Fill("OxygenRow").fillAmount);
        }

        [Test] public void StatusPrecedenceMatchesRuntimeAndRespondsWhileHealthIsConstant()
        {
            player.Vitals.Health.Set(40); player.Damage(10); Assert.AreEqual("Recovery delayed", status.text);
            player.Vitals.Hunger.Set(0); Assert.AreEqual("HUNGRY", status.text);
            using (policy.AcquirePrimaryScreen())
            {
                Assert.AreEqual("Recovery paused", status.text);
                player.Configure(new VitalsRecoverySettings(segmentCount: 0)); Assert.AreEqual("Recovery unavailable", status.text);
                AssertMarkers("HealthRow"); AssertMarkers("HungerRow");
                player.Configure(new VitalsRecoverySettings());
                player.Vitals.Health.Set(0); Assert.AreEqual("Recovery unavailable", status.text);
                player.Vitals.Health.Set(50); Assert.AreEqual("", status.text, "Ceiling takes precedence over hunger, gating, and delay.");
                player.Vitals.Health.Set(100); Assert.AreEqual("", status.text);
                player.Vitals.Health.Set(30); Assert.AreEqual("Recovery paused", status.text);
            }
            Assert.AreEqual("HUNGRY", status.text);
            player.Vitals.Hunger.Set(100); Assert.AreEqual("Recovery delayed", status.text);
            player.Advance(5); Assert.AreEqual(30, player.Vitals.Health.Current); Assert.AreEqual("Recovering", status.text);
        }

        [Test] public void HungryNotificationShowsCauseAndConsequenceAndClearsOnEveryOtherReason()
        {
            player.Vitals.Health.Set(30); player.Vitals.Hunger.Set(10);
            Assert.AreEqual("HUNGRY", status.text); Assert.AreEqual("Recovery paused", detail.text);
            Assert.AreNotEqual(status.color, detail.color, "Heading warns in amber; explanation remains muted.");
            Canvas.ForceUpdateCanvases();
            Assert.Greater(status.rectTransform.position.y, detail.rectTransform.position.y);
            Assert.AreEqual("NUTRITION", hud.transform.Find("HungerRow/HungerBar").GetComponentInChildren<TMP_Text>().text);
            using (policy.AcquirePrimaryScreen())
            { Assert.AreEqual("Recovery paused", status.text); Assert.AreEqual("", detail.text); }
            Assert.AreEqual("Recovery paused", detail.text);
            player.Vitals.Health.Set(50); Assert.AreEqual("", status.text); Assert.AreEqual("", detail.text);
            player.Vitals.Health.Set(30); player.Vitals.Hunger.Set(20);
            Assert.AreEqual("Recovering", status.text); Assert.AreEqual("", detail.text);
            player.Configure(new VitalsRecoverySettings(maxHealth: -1));
            Assert.AreEqual("Recovery unavailable", status.text); Assert.AreEqual("", detail.text);
            player.Vitals.Oxygen.Set(99); Assert.AreEqual("Recovery unavailable", status.text);
            player.Vitals.Hunger.Set(10); Assert.AreEqual("Recovery unavailable", status.text); Assert.AreEqual("", detail.text);
            player.Configure(new VitalsRecoverySettings()); Assert.AreEqual("Recovery paused", detail.text);
            player.ResetForAwakening(); Assert.AreEqual("", status.text); Assert.AreEqual("", detail.text);
        }

        [Test] public void InclusiveThresholdAndCooldownChangesUpdateTextWithoutHealing()
        {
            player.Vitals.Health.Set(40); player.Damage(10); player.Vitals.Hunger.Set(0);
            player.Advance(5); Assert.AreEqual(30, player.Vitals.Health.Current); Assert.AreEqual("HUNGRY", status.text);
            player.Vitals.Hunger.Set(20); Assert.AreEqual("Recovering", status.text);
            player.Vitals.Hunger.Set(19); Assert.AreEqual("HUNGRY", status.text);
            Assert.AreEqual(30, player.Vitals.Health.Current);
        }

        [Test] public void DisableAndReenableDetachAllViewCallbacksAndSynchronizeWithoutHealing()
        {
            var health = player.Vitals.Health; var hunger = player.Vitals.Hunger; var oxygen = player.Vitals.Oxygen;
            health.Set(30); hunger.Set(60); oxygen.Set(40);
            Assert.AreEqual(4, ViewSubscriptions(player, "PresentationChanged"));
            for (int i = 0; i < 3; i++)
            {
                hud.SetActive(false); Assert.AreEqual(0, ViewSubscriptions(player, "PresentationChanged"));
                foreach (var vital in new[] { health, hunger, oxygen }) Assert.AreEqual(0, ViewSubscriptions(vital, "Changed"));
                hunger.Set(10); player.Configure(new VitalsRecoverySettings(segmentCount: 3));
                Assert.AreEqual("Recovering", status.text); Assert.AreEqual(.6f, Fill("HungerRow").fillAmount);
                hud.SetActive(true); Assert.AreEqual("HUNGRY", status.text); Assert.AreEqual(.1f, Fill("HungerRow").fillAmount);
                AssertMarkers("HealthRow", 1f / 3, 2f / 3);
                Assert.AreEqual(4, ViewSubscriptions(player, "PresentationChanged"));
                foreach (var vital in new[] { health, hunger, oxygen }) Assert.AreEqual(1, ViewSubscriptions(vital, "Changed"));
                hunger.Set(60);
            }
            Assert.AreSame(health, player.Vitals.Health); Assert.AreSame(hunger, player.Vitals.Hunger); Assert.AreSame(oxygen, player.Vitals.Oxygen);
            Assert.AreEqual(30, health.Current); Assert.AreEqual(40, oxygen.Current);
        }

        [Test] public void AwakeningKeepsBoundObjectsAndSynchronizesAllViews()
        {
            var health = player.Vitals.Health; var hunger = player.Vitals.Hunger; var oxygen = player.Vitals.Oxygen;
            health.Set(40); player.Damage(10); hunger.Set(1); oxygen.Set(12);
            player.ResetForAwakening();
            Assert.AreSame(health, player.Vitals.Health); Assert.AreSame(hunger, player.Vitals.Hunger); Assert.AreSame(oxygen, player.Vitals.Oxygen);
            foreach (var row in new[] { "HealthRow", "HungerRow", "OxygenRow" }) Assert.AreEqual(1, Fill(row).fillAmount);
            Assert.AreEqual("", status.text); Assert.AreEqual(0, player.RecoveryState.DelayRemaining);
            Assert.AreEqual(4, ViewSubscriptions(player, "PresentationChanged"));
        }

        [Test] public void ReleaseRefreshesPausedStatusAndDisableDetachesReleasedSource()
        {
            player.Vitals.Health.Set(30); Assert.AreEqual("Recovering", status.text);
            feature.Release(); Assert.AreEqual("Recovery paused", status.text); Assert.AreEqual(30, player.Vitals.Health.Current);
            hud.SetActive(false); Assert.AreEqual(0, ViewSubscriptions(player, "PresentationChanged"));
            policy.SetTransition(true); player.Vitals.Health.Set(0);
            Assert.AreEqual("Recovery paused", status.text); Assert.AreEqual(.3f, Fill("HealthRow").fillAmount);
        }

        [UnityTest] public IEnumerator ViewsHandleLateAndReplacementControllersAndDetachDestroyedSource()
        {
            var oldHealth = player.Vitals.Health;
            Object.Destroy(host); yield return null; yield return null;
            Assert.AreEqual("Recovery unavailable", status.text); AssertMarkers("HealthRow");
            Assert.AreEqual(0, ViewSubscriptions(oldHealth, "Changed"));
            CreatePlayer(); feature.enabled = false;
            player.Vitals.Health.Set(30); player.Vitals.Hunger.Set(10); player.Vitals.Oxygen.Set(40);
            var health = player.Vitals.Health; var hunger = player.Vitals.Hunger;
            yield return null; yield return null;
            Assert.AreEqual(.3f, Fill("HealthRow").fillAmount); Assert.AreEqual(.1f, Fill("HungerRow").fillAmount);
            Assert.AreEqual(.4f, Fill("OxygenRow").fillAmount); Assert.AreEqual("Recovery paused", status.text);
            AssertMarkers("HealthRow", .25f, .5f, .75f);
            Assert.AreSame(health, player.Vitals.Health); Assert.AreSame(hunger, player.Vitals.Hunger);
            Assert.AreEqual(30, health.Current); Assert.AreEqual(10, hunger.Current);
            oldHealth.Set(90); Assert.AreEqual(.3f, Fill("HealthRow").fillAmount);
        }
    }
}
#endif
