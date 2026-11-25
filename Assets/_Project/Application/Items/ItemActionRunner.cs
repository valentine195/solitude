using System;
using SOLITUDE.Items;
namespace SOLITUDE.Application
{
    /// <summary>One actor's execution and committed lockout, independent of action type.</summary>
    public sealed class ItemActionRunner : IDisposable
    {
        private IItemActionRequest request;
        private IItemActionExecution execution;
        private bool retired;
        private double clock, lockoutUntil;
        public double LockoutRemaining => Math.Max(0, lockoutUntil - clock);
        public bool IsExecuting => request != null;
        public bool TryStart(IItemActionRequest next)
        {
            if (retired || request != null || LockoutRemaining > 0 || next == null || !next.IsValid) return false;
            ValidateElapsed(next.CommittedDuration);
            request = next; lockoutUntil = clock + next.CommittedDuration;
            IItemActionExecution created = null;
            try
            {
                created = next.Start();
                if (created == null || request != next || retired || !next.IsValid)
                { created?.Interrupt(); Cancel(); return false; }
                execution = created; return true;
            }
            catch { created?.Interrupt(); Cancel(); throw; }
        }
        public void Advance(double seconds, bool actorAvailable, bool gameplay)
        {
            ValidateElapsed(seconds);
            if (retired) return;
            if (!actorAvailable || (request != null && !request.IsValid)) Cancel();
            if (!gameplay) return;
            clock += seconds;
            var active = execution;
            if (active == null) return;
            try { active.Advance(seconds); }
            catch { Cancel(); throw; }
            if (execution == active && (active.IsComplete || request == null || !request.IsValid)) Cancel();
        }
        public void Cancel()
        {
            var active = execution; execution = null; request = null;
            active?.Interrupt(); // Clear ownership before reentrant cleanup.
        }
        private static void ValidateElapsed(double seconds)
        { if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds)); }
        public void Dispose()
        { if (retired) return; retired = true; Cancel(); clock = lockoutUntil = 0; }
    }
}
