using System;
using NUnit.Framework;
using SOLITUDE.Application;
using SOLITUDE.Items;
namespace SOLITUDE.Tests
{
    public sealed class ItemActionRunnerTests
    {
        // Intentionally has no melee phases: proves the runner's generic boundary.
        private sealed class Execution : IItemActionExecution
        {
            public bool IsComplete { get; private set; }
            public int Steps, Interruptions;
            public Action Callback;
            public void Advance(double seconds) { Steps++; Callback?.Invoke(); }
            public void Interrupt() { if (IsComplete) return; IsComplete = true; Interruptions++; }
        }
        private sealed class Request : IItemActionRequest
        {
            public bool Valid = true;
            public bool IsValid => Valid;
            public double CommittedDuration => .55;
            public Execution Execution = new Execution();
            public Action Starting;
            public IItemActionExecution Start() { Starting?.Invoke(); return Execution; }
        }
        [Test] public void RepeatedRequestsAndCancellationCannotBypassCommittedDuration()
        {
            using var runner = new ItemActionRunner(); var first = new Request();
            Assert.IsTrue(runner.TryStart(first)); Assert.IsFalse(runner.TryStart(new Request()));
            runner.Advance(.12, true, true); runner.Cancel(); runner.Cancel();
            Assert.AreEqual(1, first.Execution.Interruptions); Assert.IsFalse(runner.TryStart(new Request()));
            runner.Advance(.1, true, true); Assert.IsFalse(runner.TryStart(new Request()));
            runner.Advance(.33, true, true); Assert.IsTrue(runner.TryStart(new Request()));
        }
        [Test] public void PolicyFreezeDoesNotAdvanceButInvalidOwnerStillCancels()
        {
            using var runner = new ItemActionRunner(); var request = new Request(); runner.TryStart(request);
            runner.Advance(1, true, false); Assert.AreEqual(0, request.Execution.Steps); Assert.AreEqual(.55, runner.LockoutRemaining);
            request.Valid = false; runner.Advance(1, true, false);
            Assert.IsFalse(runner.IsExecuting); Assert.AreEqual(1, request.Execution.Interruptions); Assert.AreEqual(.55, runner.LockoutRemaining);
        }
        [Test] public void ReentrantStartAndStepCancellationReleaseExactlyOnce()
        {
            using var runner = new ItemActionRunner(); var request = new Request { Starting = runner.Cancel };
            Assert.IsFalse(runner.TryStart(request)); Assert.AreEqual(1, request.Execution.Interruptions);
            runner.Advance(.55, true, true); var next = new Request(); next.Execution.Callback = runner.Cancel;
            Assert.IsTrue(runner.TryStart(next)); runner.Advance(.1, true, true);
            Assert.IsFalse(runner.IsExecuting); Assert.AreEqual(1, next.Execution.Interruptions);
        }
        [Test] public void ActorsDoNotShareExecutionAndRetirementReleasesOwnedState()
        {
            using var a = new ItemActionRunner(); using var b = new ItemActionRunner();
            var ar = new Request(); var br = new Request(); a.TryStart(ar); b.TryStart(br);
            a.Advance(.12, true, true); Assert.AreEqual(1, ar.Execution.Steps); Assert.AreEqual(0, br.Execution.Steps);
            a.Dispose(); a.Dispose(); Assert.AreEqual(0, a.LockoutRemaining); Assert.IsFalse(a.TryStart(new Request()));
            Assert.IsTrue(b.IsExecuting); Assert.AreEqual(0, br.Execution.Interruptions);
        }
        [Test] public void UnavailableActorAndFaultedExecutionCleanUp()
        {
            using var runner = new ItemActionRunner(); var request = new Request(); runner.TryStart(request);
            runner.Advance(0, false, true); Assert.AreEqual(1, request.Execution.Interruptions);
            runner.Advance(.55, true, true); var fault = new Request(); fault.Execution.Callback = () => throw new InvalidOperationException();
            runner.TryStart(fault); Assert.Throws<InvalidOperationException>(() => runner.Advance(.1, true, true));
            Assert.IsFalse(runner.IsExecuting); Assert.AreEqual(1, fault.Execution.Interruptions);
            Assert.Throws<ArgumentOutOfRangeException>(() => runner.Advance(double.NaN, true, true));
        }
    }
}
