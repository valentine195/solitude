using System;
namespace SOLITUDE.Npcs
{
    public enum SecurityBotState { Patrol, Investigating, Pursuing, Charging, Recovering }
    public sealed class SecurityBotBrain
    {
        private readonly double confirmation, search, windup, recovery, attackRange;
        private double timer, sightTime, investigationAge;
        public SecurityBotState State { get; private set; }
        public SecurityBotBrain(double confirmation = .35, double search = 3,
            double windup = .65, double recovery = 1.6, double attackRange = 1.8)
        {
            if (!Positive(confirmation) || !Positive(search) || !Positive(windup)
                || !Positive(recovery) || !Positive(attackRange)) throw new ArgumentOutOfRangeException(nameof(confirmation));
            this.confirmation=confirmation;this.search=search;this.windup=windup;
            this.recovery=recovery;this.attackRange=attackRange;
        }
        private static bool Positive(double value) => value>0 && !double.IsInfinity(value) && !double.IsNaN(value);
        public void Reset() { State=SecurityBotState.Patrol;timer=0;sightTime=0;investigationAge=0; }
        public void Investigate() { if(State==SecurityBotState.Patrol) Enter(SecurityBotState.Investigating); }
        private void Enter(SecurityBotState state) { State=state;timer=0;investigationAge=0; }
        // Returns one firing attempt per completed charge. The runtime must recheck
        // the locked firing direction against solid geometry and the current target.
        public bool Advance(double elapsed, bool visible, double distance, bool atLastKnown, bool eligible)
        {
            if(!eligible || elapsed==0) return false;
            if(!Positive(elapsed) || distance<0 || double.IsNaN(distance)) throw new ArgumentOutOfRangeException(nameof(elapsed));
            switch(State)
            {
                case SecurityBotState.Patrol:
                case SecurityBotState.Investigating:
                    if(visible)
                    {
                        timer=0;sightTime+=elapsed;
                        if(State==SecurityBotState.Patrol) Enter(SecurityBotState.Investigating);
                        if(sightTime>=confirmation) { sightTime=0;Enter(SecurityBotState.Pursuing); }
                    }
                    else
                    {
                        sightTime=0;
                        if(State==SecurityBotState.Investigating)
                        {
                            investigationAge+=elapsed;
                            if(atLastKnown) timer+=elapsed;
                            if(timer>=search || investigationAge>=search+8) Enter(SecurityBotState.Patrol);
                        }
                    }
                    break;
                case SecurityBotState.Pursuing:
                    if(!visible) Enter(SecurityBotState.Investigating);
                    else if(distance<=attackRange) Enter(SecurityBotState.Charging);
                    break;
                case SecurityBotState.Charging:
                    if(!visible || distance>attackRange) { Enter(visible?SecurityBotState.Pursuing:SecurityBotState.Investigating);break; }
                    timer+=elapsed;
                    if(timer>=windup) { Enter(SecurityBotState.Recovering);return true; }
                    break;
                case SecurityBotState.Recovering:
                    timer+=elapsed;
                    if(timer>=recovery) Enter(visible?SecurityBotState.Pursuing:SecurityBotState.Investigating);
                    break;
            }
            return false;
        }
    }
}
