using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SOLITUDE.Editor;
using UnityEditor;
namespace SOLITUDE.Tests
{
    public class MechanicalRepairBuildValidationTests
    {
        [Test] public void ShippingRejectsFixtureAndUnrelatedSceneInjection()
        {
            Assert.IsTrue(WakeupBuildManifest.AllowsScenes(new[] { "Assets/_Project/Scenes/Wakeup.unity" }));
            Assert.IsFalse(WakeupBuildManifest.AllowsScenes(WakeupBuildManifest.RepairScenes));
            Assert.IsFalse(WakeupBuildManifest.AllowsScenes(new[] { PhaseFiveFixtureBuilder.Unrelated }));
        }
        [TestCase(false)] [TestCase(true)]
        public void DisposableOptInAllowsOnlyRepairSetAndRestoresFilesEvenAfterFailure(bool fail)
        {
            var manifest = File.ReadAllBytes(WakeupBuildManifest.AssetPath);
            const string settingsPath = "ProjectSettings/EditorBuildSettings.asset";
            var settings = File.ReadAllBytes(settingsPath);
            try
            {
                using (WakeupBuildManifest.SelectRepairVerification())
                {
                    UnityEditor.EditorUtility.UnloadUnusedAssetsImmediate();
                    Assert.IsTrue(WakeupBuildManifest.IsRepairVerification, "Build asset unloading must preserve the selected scope.");
                    Assert.Throws<UnityEditor.Build.BuildFailedException>(() => WakeupPlayerBuilder.BuildShipping());
                    Assert.IsTrue(WakeupBuildManifest.AllowsScenes(WakeupBuildManifest.Selected.scenes));
                    Assert.IsFalse(WakeupBuildManifest.AllowsScenes(WakeupBuildManifest.RepairScenes.Concat(new[] { PhaseFiveFixtureBuilder.Unrelated }).ToArray()));
                    Assert.IsFalse(WakeupBuildManifest.AllowsScenes(new[] { PhaseFiveFixtureBuilder.Unrelated }));
                    Assert.IsTrue(WakeupValidation.Collect().success, "Opted-in fixture must pass full unchanged authoring checks.");
                    if (fail) throw new InvalidOperationException("injected build failure");
                }
            }
            catch (InvalidOperationException error) when (fail && error.Message == "injected build failure") { }
            CollectionAssert.AreEqual(manifest, File.ReadAllBytes(WakeupBuildManifest.AssetPath));
            CollectionAssert.AreEqual(settings, File.ReadAllBytes(settingsPath));
            Assert.IsFalse(WakeupBuildManifest.IsRepairVerification);
            Assert.IsFalse(WakeupBuildManifest.AllowsScenes(WakeupBuildManifest.RepairScenes));
        }
    }
}
