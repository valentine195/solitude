using System;
using NUnit.Framework;
using SOLITUDE.Application;

namespace SOLITUDE.Tests
{
    public class TaskJournalScreenCoordinatorTests
    {
        private sealed class Display : ITaskJournalScreenDisplay
        {
            public bool ThrowShow, ThrowHide;
            public int Shows, Hides;
            public Action OnShow;
            public void Show() { Shows++; OnShow?.Invoke(); if (ThrowShow) throw new Exception("show"); }
            public void Hide() { Hides++; if (ThrowHide) throw new Exception("hide"); }
        }
        private PauseCoordinator pause;
        private InputPolicyCoordinator policy;
        private Display display;
        private TaskJournalScreenCoordinator screen;
        [SetUp] public void SetUp()
        {
            pause = new PauseCoordinator(); policy = new InputPolicyCoordinator(pause); policy.SetReady(true);
            display = new Display(); screen = new TaskJournalScreenCoordinator(policy, pause, display);
        }
        [TearDown] public void TearDown() { screen.Dispose(); policy.Dispose(); }
        [Test] public void ReadyOpenPausesAndGatesWithoutChangingBaseSpeedOrOtherPauseOwnership()
        {
            pause.SetBaseScale(.5f);
            Assert.AreEqual(OpenResult.Opened, screen.Open()); Assert.IsTrue(screen.IsOpen);
            Assert.AreEqual(0, pause.Scale); Assert.IsFalse(policy.Gameplay); Assert.IsFalse(policy.Inventory); Assert.IsTrue(policy.Pointer);
            Assert.AreEqual(OpenResult.Opened, screen.Open()); Assert.AreEqual(1, display.Shows);
            var independent = pause.Acquire(); screen.Close(); Assert.AreEqual(0, pause.Scale); Assert.IsFalse(policy.PrimaryScreenActive);
            independent.Dispose(); Assert.AreEqual(.5f, pause.Scale); Assert.IsTrue(policy.Gameplay);
            screen.Dispose(); screen.Dispose(); Assert.AreEqual(.5f, pause.Scale);
        }
        [Test] public void OtherPrimaryPauseUnreadyAndTransitionRequestsLeaveOwnersUntouched()
        {
            var owner = policy.AcquirePrimaryScreen();
            Assert.AreEqual(OpenResult.Unavailable, screen.Open()); Assert.IsTrue(policy.PrimaryScreenActive); owner.Dispose();
            var independent = pause.Acquire(); Assert.AreEqual(OpenResult.Unavailable, screen.Open()); Assert.IsTrue(pause.IsPaused); independent.Dispose();
            policy.SetReady(false); Assert.AreEqual(OpenResult.Unavailable, screen.Open());
            policy.SetReady(true); policy.SetTransition(true); Assert.AreEqual(OpenResult.Unavailable, screen.Open());
            policy.SetTransition(false); pause.SetBaseScale(0); Assert.AreEqual(OpenResult.Unavailable, screen.Open());
            Assert.AreEqual(0, display.Shows);
        }
        [Test] public void FailedShowAndHideReleaseBothLeasesAndBackConsumesOnlyOpenScreen()
        {
            display.ThrowShow = true; display.ThrowHide = true;
            Assert.AreEqual(OpenResult.BindingFailed, screen.Open()); Assert.IsFalse(screen.IsOpen);
            Assert.AreEqual(1, pause.Scale); Assert.IsFalse(policy.PrimaryScreenActive); Assert.IsTrue(policy.Gameplay);
            Assert.IsFalse(screen.Back()); display.ThrowShow = false;
            Assert.AreEqual(OpenResult.Opened, screen.Open()); Assert.IsTrue(screen.Back()); Assert.IsFalse(screen.Back());
            Assert.AreEqual(1, pause.Scale); Assert.IsTrue(policy.Gameplay);
        }
        [Test] public void TransitionAndLostBindingCloseSafelyIncludingDuringShow()
        {
            screen.Open(); policy.SetTransition(true); Assert.IsFalse(screen.IsOpen); Assert.AreEqual(1, pause.Scale);
            policy.SetTransition(false); display.OnShow = () => policy.SetReady(false);
            Assert.AreEqual(OpenResult.Unavailable, screen.Open()); Assert.IsFalse(policy.PrimaryScreenActive); Assert.AreEqual(1, pause.Scale);
            policy.SetReady(true); display.OnShow = () => screen.Close();
            Assert.AreEqual(OpenResult.Unavailable, screen.Open()); Assert.IsFalse(screen.IsOpen); Assert.AreEqual(1, pause.Scale);
        }
        [Test] public void MissingDisplayAndDisposedCoordinatorNeverAcquireOwnership()
        {
            using (var missing = new TaskJournalScreenCoordinator(policy, pause, null)) Assert.AreEqual(OpenResult.Unavailable, missing.Open());
            screen.Dispose(); Assert.AreEqual(OpenResult.Unavailable, screen.Open()); Assert.IsTrue(policy.Gameplay);
        }
    }
}
