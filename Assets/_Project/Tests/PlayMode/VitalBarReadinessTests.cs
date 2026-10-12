using System.Collections;
using System.Reflection;
using NUnit.Framework;
using SOLITUDE.Player;
using SOLITUDE.Player.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SOLITUDE.Tests
{
    public class VitalBarReadinessTests
    {
        [UnityTest]
        public IEnumerator ViewBeforePlayer_BindsWhenReady_AndReconcilesWithoutHealing()
        {
            // A single empty scene gives this startup-order test no ambient player or HUD.
            foreach (var root in Object.FindObjectsByType<SOLITUDE.Composition.GameCompositionRoot>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.Destroy(root.gameObject);
            yield return null;
            var empty = SceneManager.CreateScene("VitalBarReadiness");
            SceneManager.SetActiveScene(empty);
            for (var i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene != empty) yield return SceneManager.UnloadSceneAsync(scene);
            }
            var viewObject = new GameObject("ReadinessView", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            viewObject.SetActive(false);
            var image = viewObject.GetComponent<UnityEngine.UI.Image>();
            image.type = UnityEngine.UI.Image.Type.Filled;
            image.fillAmount = .5f;
            var view = viewObject.AddComponent<VitalBarView>();
            typeof(VitalBarView).GetField("fillImage", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(view, image);
            typeof(VitalBarView).GetField("kind", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(view, VitalKind.Oxygen);
            viewObject.SetActive(true);
            yield return null;
            Assert.AreEqual(.5f, image.fillAmount);

            var playerObject = new GameObject("ReadinessPlayer");
            playerObject.SetActive(false);
            var player = playerObject.AddComponent<PlayerVitalsController>();
            playerObject.SetActive(true);
            var oxygen = player.Vitals.Oxygen;
            oxygen.Set(40);
            yield return null;
            yield return null;
            Assert.AreEqual(.4f, image.fillAmount);
            oxygen.Set(30);
            Assert.AreEqual(.3f, image.fillAmount);
            viewObject.SetActive(false);
            oxygen.Set(20);
            Assert.AreEqual(.3f, image.fillAmount, "Disabled view unsubscribes.");
            viewObject.SetActive(true);
            Assert.AreEqual(.2f, image.fillAmount);
            Assert.AreEqual(20, oxygen.Current, "Binding must not heal the survivor.");
            player.ResetForAwakening();
            Assert.AreSame(oxygen, player.Vitals.Oxygen);
            Assert.AreEqual(1, image.fillAmount);
            Object.Destroy(viewObject);
            Object.Destroy(playerObject);
            yield return null;
        }
    }
}
