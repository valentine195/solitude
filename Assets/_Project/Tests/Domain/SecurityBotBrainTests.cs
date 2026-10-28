using System;
using NUnit.Framework;
using SOLITUDE.Npcs;
namespace SOLITUDE.Tests
{
    public sealed class SecurityBotBrainTests
    {
        [Test] public void ContinuousSightConfirmsIntruderButBriefGlancesDoNot()
        {
            var brain=new SecurityBotBrain();
            brain.Advance(.2,true,4,false,true);Assert.AreEqual(SecurityBotState.Investigating,brain.State);
            brain.Advance(.2,false,4,false,true);
            brain.Advance(.2,true,4,false,true);Assert.AreEqual(SecurityBotState.Investigating,brain.State);
            brain.Advance(.2,true,4,false,true);Assert.AreEqual(SecurityBotState.Pursuing,brain.State);
        }
        private static SecurityBotBrain Charging()
        {
            var brain=new SecurityBotBrain();brain.Advance(.4,true,1,false,true);brain.Advance(.1,true,1,false,true);
            Assert.AreEqual(SecurityBotState.Charging,brain.State);return brain;
        }
        [Test] public void ChargeTelegraphsBeforeOneShotAndCannotRepeatDuringRecovery()
        {
            var brain=Charging();Assert.IsFalse(brain.Advance(.6,true,1,false,true));
            Assert.IsTrue(brain.Advance(.1,true,1,false,true));Assert.AreEqual(SecurityBotState.Recovering,brain.State);
            Assert.IsFalse(brain.Advance(1,true,1,false,true));
            Assert.IsFalse(brain.Advance(.7,true,1,false,true));Assert.AreEqual(SecurityBotState.Pursuing,brain.State);
        }
        [Test] public void BreakingSightOrLeavingRangeCancelsCharge()
        {
            var brain=Charging();Assert.IsFalse(brain.Advance(1,false,1,false,true));Assert.AreEqual(SecurityBotState.Investigating,brain.State);
            brain=Charging();Assert.IsFalse(brain.Advance(1,true,3,false,true));Assert.AreEqual(SecurityBotState.Pursuing,brain.State);
        }
        [Test] public void EligibilityGatesFreezeChargeWithoutCatchup()
        {
            var brain=Charging();Assert.IsFalse(brain.Advance(100,true,1,false,false));
            Assert.AreEqual(SecurityBotState.Charging,brain.State);
            Assert.IsFalse(brain.Advance(.5,true,1,false,true));Assert.IsTrue(brain.Advance(.2,true,1,false,true));
        }
        [Test] public void LostIntruderIsSearchedThenPatrolResumes()
        {
            var brain=new SecurityBotBrain();brain.Advance(.4,true,4,false,true);brain.Advance(.1,false,4,false,true);
            Assert.AreEqual(SecurityBotState.Investigating,brain.State);
            brain.Advance(2,false,4,true,true);Assert.AreEqual(SecurityBotState.Investigating,brain.State);
            brain.Advance(1.1,false,4,true,true);Assert.AreEqual(SecurityBotState.Patrol,brain.State);
        }
        [Test] public void UnreachableLastKnownPositionDoesNotLeaveBotSearchingForever()
        {
            var brain=new SecurityBotBrain();brain.Investigate();brain.Advance(12,false,4,false,true);
            Assert.AreEqual(SecurityBotState.Patrol,brain.State);
        }
        [Test] public void ResetCancelsAnUnfinishedAttack()
        {
            var brain=Charging();brain.Reset();Assert.IsFalse(brain.Advance(1,false,4,true,true));
            Assert.AreEqual(SecurityBotState.Patrol,brain.State);
        }
        [Test] public void InvalidTuningAndTimeAreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(()=>new SecurityBotBrain(windup:double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(()=>new SecurityBotBrain().Advance(-1,false,1,false,true));
        }
    }
}
