using System;
using NUnit.Framework;
using SOLITUDE.Application;
namespace SOLITUDE.Tests
{
    public class ScrubberScreenCoordinatorTests
    {
        private sealed class Display : IScrubberScreenDisplay
        {
            public bool Fail; public int Shown, Hidden;
            public void Show() { Shown++; if (Fail) throw new InvalidOperationException(); }
            public void Hide() { Hidden++; }
        }
        [Test] public void PrimaryScreenBlocksWorldAndInventoryWithoutPausing()
        {
            var pause = new PauseCoordinator(); var policy = new InputPolicyCoordinator(pause); policy.SetReady(true);
            var display = new Display(); using var screen = new ScrubberScreenCoordinator(policy, display);
            Assert.AreEqual(OpenResult.Opened, screen.Open()); Assert.IsFalse(policy.Gameplay); Assert.IsFalse(policy.Inventory);
            Assert.IsTrue(policy.Pointer); Assert.AreEqual(1, pause.Scale); Assert.IsNull(policy.AcquirePrimaryScreen());
            Assert.AreEqual(OpenResult.Opened, screen.Open()); Assert.AreEqual(1, display.Shown);
            Assert.IsTrue(screen.Back()); Assert.IsFalse(screen.Back()); Assert.IsTrue(policy.Gameplay); Assert.IsTrue(policy.Inventory);
            Assert.AreEqual(1, display.Hidden); policy.Dispose();
        }
        [Test] public void BindingFailureReleasesLeaseAndCanBeRetried()
        {
            var pause = new PauseCoordinator(); var policy = new InputPolicyCoordinator(pause); policy.SetReady(true);
            var display = new Display { Fail = true }; using var screen = new ScrubberScreenCoordinator(policy, display);
            Assert.AreEqual(OpenResult.BindingFailed, screen.Open()); Assert.IsFalse(screen.IsOpen); Assert.IsTrue(policy.Gameplay);
            display.Fail = false; Assert.AreEqual(OpenResult.Opened, screen.Open()); screen.Dispose(); screen.Dispose();
            Assert.IsTrue(policy.Gameplay); Assert.AreEqual(OpenResult.Unavailable, screen.Open()); policy.Dispose();
        }
        [Test] public void UnreadyPausedAndTransitioningScreensCannotOpen()
        {
            var pause = new PauseCoordinator(); var policy = new InputPolicyCoordinator(pause);
            using var screen = new ScrubberScreenCoordinator(policy, new Display());
            Assert.AreEqual(OpenResult.Unavailable, screen.Open()); policy.SetReady(true);
            using (pause.Acquire()) Assert.AreEqual(OpenResult.Unavailable, screen.Open());
            policy.SetTransition(true); Assert.AreEqual(OpenResult.Unavailable, screen.Open());
            policy.SetTransition(false); Assert.AreEqual(OpenResult.Opened, screen.Open());
            using (pause.Acquire()) { screen.Close(); Assert.IsFalse(policy.Gameplay); Assert.AreEqual(0, pause.Scale); }
            Assert.IsTrue(policy.Gameplay); policy.Dispose();
        }
    }
}
