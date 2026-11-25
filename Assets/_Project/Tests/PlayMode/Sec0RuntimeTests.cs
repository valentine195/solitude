#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using SOLITUDE.Application;
using SOLITUDE.Composition;
using SOLITUDE.Core.Animations;
using SOLITUDE.Movement;
using SOLITUDE.Npcs;
using SOLITUDE.Npcs.Sec0;
using SOLITUDE.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace SOLITUDE.Tests
{
    public sealed class Sec0TestAnimation : MonoBehaviour,ICreatureAnimationController { public void SetMovement(Vector2 v) { } }
    public sealed class Sec0RuntimeTests
    {
        private GameObject host,actor,player,wall;
        private Sec0Controller bot;
        private PlayerStunReceiver stun;
        private MovementBehavior movement;
        private PauseCoordinator pause;
        private InputPolicyCoordinator policy;
        private Sec0Composition composition;
        [SetUp] public void Prepare()
        {
            host=new GameObject("SEC-0 Test Fixture");
            actor=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Features/Npcs/Sec0/PF_Sec0.prefab"));actor.transform.SetParent(host.transform);bot=actor.GetComponentInChildren<Sec0Controller>();
            player=new GameObject("Target");player.transform.SetParent(host.transform);player.transform.position=new Vector3(0,-1,0);
            var body=player.AddComponent<Rigidbody2D>();body.gravityScale=0;body.constraints=RigidbodyConstraints2D.FreezeRotation;
            player.AddComponent<BoxCollider2D>().size=new Vector2(.5f,.5f);player.AddComponent<Sec0TestAnimation>();
            movement=player.AddComponent<MovementBehavior>();stun=player.AddComponent<PlayerStunReceiver>();
            var interactor=player.AddComponent<PlayerInteractor>();var vitals=player.AddComponent<PlayerVitalsController>();
            composition=host.AddComponent<Sec0Composition>();composition.Configure(new[]{bot},stun,vitals);
            pause=new PauseCoordinator();policy=new InputPolicyCoordinator(pause);policy.SetReady(true);
            composition.Initialize(new SceneFeatureContext(null,null,null,policy,null,null,"sec0-tests",interactor,null));Physics2D.SyncTransforms();
        }
        [TearDown] public void Cleanup()
        {
            composition?.Release();if(wall!=null)Object.DestroyImmediate(wall);if(host!=null)Object.DestroyImmediate(host);policy?.Dispose();
        }
        private void Place(Vector2 position) {player.transform.position=position;player.GetComponent<Rigidbody2D>().position=position;Physics2D.SyncTransforms();}
        private void Cover(Vector2 position)
        {wall=new GameObject("Sight blocking cover");wall.transform.position=position;wall.AddComponent<BoxCollider2D>().size=new Vector2(.8f,.15f);Physics2D.SyncTransforms();}
        private void Charge() {bot.Tick(.4f);bot.Tick(.02f);Assert.AreEqual(SecurityBotState.Charging,bot.State);}
        [Test] public void SightRequiresConeRangeAndUnobstructedSolidGeometry()
        {
            Place(new Vector2(0,-3));Assert.IsTrue(bot.CanSeeTarget());
            Cover(new Vector2(0,-1.5f));Assert.IsFalse(bot.CanSeeTarget());Object.DestroyImmediate(wall);wall=null;
            Place(new Vector2(0,3));Assert.IsFalse(bot.CanSeeTarget());
            Place(new Vector2(0,-8));Assert.IsFalse(bot.CanSeeTarget());
        }
        [Test] public void CoverCancelsWindupWithoutShootingOrStunning()
        {
            Charge();Cover(new Vector2(0,-.4f));bot.Tick(1);
            Assert.AreEqual(0,bot.ShotsFired);Assert.AreEqual(0,bot.StunsLanded);Assert.IsFalse(stun.IsStunned);
        }
        [Test] public void SideStepDodgesLockedShotAndRecoveryPreventsRepeatFire()
        {
            Charge();Place(new Vector2(.7f,-.8f));bot.Tick(1);
            Assert.AreEqual(1,bot.ShotsFired);Assert.AreEqual(0,bot.StunsLanded);
            bot.Tick(.5f);Assert.AreEqual(1,bot.ShotsFired);
        }
        [Test] public void WarningAndDischargeFollowTheLockedShotAndThenClear()
        {
            Charge();
            var line = bot.GetComponentInChildren<LineRenderer>();
            Assert.IsTrue(line.enabled);
            float warningWidth = line.widthMultiplier;
            Vector3 expectedDirection = (line.GetPosition(1) - line.GetPosition(0)).normalized;
            Place(new Vector2(.7f, -.8f));
            bot.Tick(.4f);
            Assert.Greater(line.widthMultiplier, warningWidth);
            Vector3 end = line.GetPosition(1);
            Assert.That(Vector3.Angle(expectedDirection, end - line.GetPosition(0)), Is.LessThan(.01f));
            bot.Tick(.3f);
            Assert.AreEqual(0, bot.StunsLanded);
            Assert.AreEqual(end, line.GetPosition(1));
            Assert.Greater(line.startColor.b, line.startColor.r);
            bot.Tick(.15f);
            Assert.IsFalse(line.enabled);
        }

        [Test] public void KnockbackDuringChargeOpensRecoveryWithoutFiring()
        {
            Charge();
            Assert.IsTrue(bot.RequestKnockback(this, Vector2.up, .25f, .1f));
            Assert.AreEqual(SecurityBotState.Recovering, bot.State);
            Assert.IsFalse(bot.GetComponentInChildren<LineRenderer>().enabled);
            bot.Tick(1);
            Assert.AreEqual(0, bot.ShotsFired);
            Assert.IsFalse(stun.IsStunned);
        }

        [Test] public void DirectHitStunsWithoutChangingHealthAndReleaseUnwindsBinding()
        {
            double health=player.GetComponent<PlayerVitalsController>().Vitals.Health.Current;
            Charge();bot.Tick(1);Assert.AreEqual(1,bot.StunsLanded);Assert.IsTrue(stun.IsStunned);
            Assert.AreEqual(health,player.GetComponent<PlayerVitalsController>().Vitals.Health.Current);
            composition.Release();composition.Release();Assert.IsFalse(bot.HasBinding);Assert.IsFalse(stun.IsStunned);Assert.IsFalse(movement.IsMovementBlocked);
        }
        [Test] public void GameplayGatesFreezeAttackAndStunTimersWithoutCatchup()
        {
            Charge();policy.SetTransition(true);bot.Tick(100);Assert.AreEqual(SecurityBotState.Charging,bot.State);Assert.AreEqual(0,bot.ShotsFired);
            policy.SetTransition(false);bot.Tick(1);Assert.IsTrue(stun.IsStunned);
            float remaining=stun.Remaining;policy.SetTransition(true);stun.Advance(100);Assert.AreEqual(remaining,stun.Remaining);
            policy.SetTransition(false);stun.Advance(.2f);Assert.AreEqual(remaining-.2f,stun.Remaining,.0001f);
        }
        [UnityTest] public IEnumerator MovementIsStoppedForStunAndRestoresHeldDirectionAfterExpiry()
        {
            bot.enabled=false;Place(new Vector2(10,10));movement.SetVector(Vector2.right);
            yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
            Assert.IsTrue(stun.TryStun(2));Vector2 stopped=player.GetComponent<Rigidbody2D>().position;
            yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
            Assert.That(Vector2.Distance(stopped,player.GetComponent<Rigidbody2D>().position),Is.LessThan(.005f));
            stun.Advance(3);yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
            Assert.That(player.GetComponent<Rigidbody2D>().position.x,Is.GreaterThan(stopped.x));
        }
        [UnityTest] public IEnumerator PatrolRoutesAroundCoverWithoutCrossingIt()
        {
            Place(new Vector2(15,15));Cover(new Vector2(2,0));wall.GetComponent<BoxCollider2D>().size=new Vector2(1,1);Physics2D.SyncTransforms();
            for(int i=0;i<600 && bot.transform.position.x<3.8f;i++) {yield return new WaitForFixedUpdate();Assert.That(bot.GetComponent<BoxCollider2D>().Distance(wall.GetComponent<BoxCollider2D>()).distance,Is.GreaterThanOrEqualTo(-Physics2D.defaultContactOffset),"Rover penetrated cover.");}
            Assert.That(bot.transform.position.x,Is.GreaterThan(3),"Patrol should route around the cover, then reach the next waypoint.");
        }
    }
}
#endif
