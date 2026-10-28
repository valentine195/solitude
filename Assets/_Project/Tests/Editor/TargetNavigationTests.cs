using System;
using System.Reflection;
using NUnit.Framework;
using SOLITUDE.Core.Input;
using UnityEngine;

namespace SOLITUDE.Tests
{
    public sealed class TargetNavigationTests
    {
        private sealed class Navigator
        {
            public readonly Action<Vector2, float> Target;
            public readonly Func<Vector2, float, Vector2> Next;
            public readonly Action Invalidate, Reset;
            public readonly Func<int> Searches, Expanded;
            public Navigator(Func<Vector2, Vector2, bool> clear)
            {
                var type = typeof(UnityInputHost).Assembly.GetType("SOLITUDE.Npcs.Sec0.Sec0Navigator", true);
                var instance = Activator.CreateInstance(type, new object[] { clear });
                Target = (Action<Vector2, float>)Delegate.CreateDelegate(typeof(Action<Vector2, float>), instance, type.GetMethod("SetDestination"));
                Next = (Func<Vector2, float, Vector2>)Delegate.CreateDelegate(typeof(Func<Vector2, float, Vector2>), instance,
                    type.GetMethod("Next", new[] { typeof(Vector2), typeof(float) }));
                Invalidate = (Action)Delegate.CreateDelegate(typeof(Action), instance, type.GetMethod("InvalidatePath"));
                Reset = (Action)Delegate.CreateDelegate(typeof(Action), instance, type.GetMethod("Reset"));
                Func<int> Counter(string name) => (Func<int>)Delegate.CreateDelegate(typeof(Func<int>), instance,
                    type.GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic).GetGetMethod(true));
                Searches = Counter("SearchCount"); Expanded = Counter("LastExpandedNodes");
            }
        }
        private static bool Clear(Vector2 from, Vector2 to)
        {
            int samples = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(from, to) / .05f));
            for (int i = 0; i <= samples; i++)
            {
                var p = Vector2.Lerp(from, to, (float)i / samples);
                if (p.x > .75f && p.x < 1.25f && Mathf.Abs(p.y) < 1.25f) return false;
            }
            return true;
        }
        [Test] public void StationaryTargetDoesNotExpireHealthyRouteAfterTwoSeconds()
        {
            var nav = new Navigator(Clear); nav.Target(Vector2.right * 3, 0);
            var first = nav.Next(Vector2.zero, .02f); Assert.AreNotEqual(Vector2.zero, first);
            for (int tick = 0; tick < 100; tick++)
            {
                nav.Target(Vector2.right * 3, 0);
                Assert.AreEqual(first, nav.Next(Vector2.zero, .02f));
            }
            Assert.AreEqual(1, nav.Searches());
        }
        [Test] public void AStarFindsShortestGridRouteAndRetainsItThroughArrival()
        {
            var nav = new Navigator(Clear); var goal = Vector2.right * 3; nav.Target(goal, 0);
            var position = Vector2.zero; float length = 0;
            for (int step = 0; step < 40 && position != goal; step++)
            {
                var next = nav.Next(position, .2f); Assert.IsTrue(Clear(position, next));
                Assert.AreNotEqual(position, next); length += Vector2.Distance(position, next); position = next;
            }
            Assert.AreEqual(goal, position); Assert.AreEqual(6, length, .001f); Assert.AreEqual(1, nav.Searches());
        }
        [Test] public void SmallTargetMotionAdjustsEndpointAndAccumulatedMotionRequestsNewRoute()
        {
            var nav = new Navigator(Clear); nav.Target(Vector2.right * 3, .4f); nav.Next(Vector2.zero, .02f);
            nav.Target(new Vector2(3, .15f), .4f); nav.Next(Vector2.zero, .02f); Assert.AreEqual(1, nav.Searches());
            var position = Vector2.zero; var smallGoal = new Vector2(3, .15f);
            for (int i = 0; i < 40 && position != smallGoal; i++) position = nav.Next(position, .02f);
            Assert.AreEqual(smallGoal, position); Assert.AreEqual(1, nav.Searches());
            nav.Target(new Vector2(3, 1), .4f); nav.Next(Vector2.zero, .02f); Assert.AreEqual(2, nav.Searches());
        }
        [Test] public void ChangedDestinationAndExplicitOpeningNotificationInvalidateImmediately()
        {
            bool closed = true; var nav = new Navigator((a, b) => !closed || Clear(a, b));
            nav.Target(Vector2.right * 3, 0); nav.Next(Vector2.zero, .02f);
            nav.Target(new Vector2(3, .2f), 0); nav.Next(Vector2.zero, .02f); Assert.AreEqual(2, nav.Searches());
            closed = false; nav.Invalidate(); Assert.AreEqual(new Vector2(3, .2f), nav.Next(Vector2.zero, .02f));
        }
        [Test] public void BlockedNextSegmentAndOffRouteDisplacementReplanWithoutPeriodicTimer()
        {
            bool blocked = false; Vector2 blockedPoint = default;
            var nav = new Navigator((a, b) => Clear(a, b) && !(blocked && (b - blockedPoint).sqrMagnitude < .001f));
            nav.Target(Vector2.right * 3, 0); blockedPoint = nav.Next(Vector2.zero, .02f); blocked = true;
            Assert.AreNotEqual(blockedPoint, nav.Next(Vector2.zero, .02f)); Assert.AreEqual(2, nav.Searches());
            blocked = false; var displaced = Vector2.left * 3; var next = nav.Next(displaced, .02f);
            Assert.AreEqual(3, nav.Searches()); Assert.AreNotEqual(displaced, next); Assert.IsTrue(Clear(displaced, next));
        }
        [Test] public void UnreachableDestinationsRetryAtBoundedRateAndCanBeInvalidatedImmediately()
        {
            bool open = false; var nav = new Navigator((_, __) => open); nav.Target(Vector2.right * 3, 0);
            for (int tick = 0; tick < 100; tick++) Assert.AreEqual(Vector2.zero, nav.Next(Vector2.zero, .02f));
            Assert.That(nav.Searches(), Is.InRange(4, 5));
            int before = nav.Searches(); nav.Target(new Vector2(3, 1), 0); nav.Next(Vector2.zero, .02f);
            Assert.AreEqual(before + 1, nav.Searches());
            open = true; nav.Invalidate(); Assert.AreEqual(new Vector2(3, 1), nav.Next(Vector2.zero, .02f));
        }
        [Test] public void NearbyBlockedDestinationIsNotReturnedAsWalkable()
        {
            var nav = new Navigator(Clear); var position = new Vector2(.74f, 0);
            nav.Target(new Vector2(.84f, 0), 0);
            Assert.AreEqual(position, nav.Next(position, .02f));
        }
        [Test] public void ResetDropsDestinationAndSearchBudgetRemainsBounded()
        {
            var nav = new Navigator((a, b) => !(Mathf.Min(a.x, b.x) <= 1 && Mathf.Max(a.x, b.x) >= 1));
            nav.Target(Vector2.right * 3, 0); Assert.AreEqual(Vector2.zero, nav.Next(Vector2.zero, .02f));
            Assert.LessOrEqual(nav.Expanded(), 900); nav.Reset();
            int searches = nav.Searches(); Assert.AreEqual(Vector2.left, nav.Next(Vector2.left, 5)); Assert.AreEqual(searches, nav.Searches());
        }
    }
}
