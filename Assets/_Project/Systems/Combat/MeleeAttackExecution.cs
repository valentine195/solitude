using System;
using SOLITUDE.Items;
namespace SOLITUDE.Combat
{
    public sealed class MeleeAttackExecution : IItemActionExecution
    {
        public ItemActionContext Context { get; }
        public MeleeAttackSpec Configuration { get; }
        public MeleeAttackTiming Timing { get; }
        private IDisposable slowdown, facing;
        private bool released;
        public bool IsEvaluatingActive { get; private set; }
        public bool IsComplete => released;
        public event Action<DamageContext, DamageResult> HitApplied;
        internal void ReportApplied(in DamageContext context, DamageResult result) => HitApplied?.Invoke(context, result);
        public MeleeAttackExecution(ItemActionContext context, MeleeAttackSpec configuration)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            Timing = new MeleeAttackTiming(configuration.Windup, configuration.ActiveDuration, configuration.Recovery);
            if (!context.IsValid) { Interrupt(); return; }
            try
            {
                context.HitResolver?.Begin(this);
                slowdown = context.Movement?.RestrictSpeed(configuration.MovementFactor);
                facing = context.Animation?.OverrideFacing(context.Direction);
                if (context.View != null) context.View.Begin(this);
            }
            catch { Interrupt(); throw; }
        }
        public void Advance(double gameplaySeconds)
        {
            MeleeAttackTiming.Validate(gameplaySeconds);
            if (released) return;
            if (!Context.IsValid) { Interrupt(); return; }
            bool active = Timing.Advance(gameplaySeconds);
            if (active && Context.IsValid)
            {
                IsEvaluatingActive = true;
                try
                {
                    Context.EvaluateActive?.Invoke(this);
                    if (!released && Context.IsValid) Context.HitResolver?.Sample(this);
                }
                finally { IsEvaluatingActive = false; }
                if (released || !Context.IsValid) { Interrupt(); return; }
            }
            if (Context.View != null) Context.View.Present(this);
            if (Timing.Phase == MeleeAttackPhase.Idle) Interrupt();
        }
        public void Interrupt()
        {
            if (released) return;
            released = true; Timing.Interrupt();
            Context.HitResolver?.End(this);
            try { if (Context.View != null) Context.View.End(this); }
            finally { try { facing?.Dispose(); } finally { slowdown?.Dispose(); facing = slowdown = null; } }
        }
    }
}
