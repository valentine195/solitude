using System;
namespace SOLITUDE.Items
{
    public enum MeleeAttackPhase { Windup, Active, Recovery, Idle }
    public sealed class MeleeAttackTiming
    {
        public double Windup { get; }
        public double ActiveDuration { get; }
        public double Recovery { get; }
        public double Elapsed { get; private set; }
        public double TotalDuration => Windup + ActiveDuration + Recovery;
        public MeleeAttackPhase Phase => Elapsed >= TotalDuration ? MeleeAttackPhase.Idle :
            Elapsed < Windup ? MeleeAttackPhase.Windup :
            Elapsed < Windup + ActiveDuration ? MeleeAttackPhase.Active : MeleeAttackPhase.Recovery;
        public double PhaseProgress => Phase == MeleeAttackPhase.Windup ? Elapsed / Windup :
            Phase == MeleeAttackPhase.Active ? (Elapsed - Windup) / ActiveDuration :
            Phase == MeleeAttackPhase.Recovery ? (Elapsed - Windup - ActiveDuration) / Recovery : 1;
        public MeleeAttackTiming(double windup, double active, double recovery)
        {
            Validate(windup); Validate(active); Validate(recovery);
            if (active <= 0 || double.IsInfinity(windup + active + recovery)) throw new ArgumentOutOfRangeException(nameof(active));
            Windup = windup; ActiveDuration = active; Recovery = recovery;
        }
        public static void Validate(double seconds)
        { if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds)); }
        // Returns whether this step processes any part of Active, including coarse crossings.
        public bool Advance(double seconds)
        {
            Validate(seconds);
            double before = Elapsed;
            Elapsed = Math.Min(TotalDuration, Elapsed + seconds);
            return seconds > 0 && before < Windup + ActiveDuration && Elapsed >= Windup;
        }
        public void Interrupt() => Elapsed = TotalDuration;
    }
}
