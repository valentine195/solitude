#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.Reflection;
using NUnit.Framework;
using SOLITUDE.Application;
using SOLITUDE.Npcs.Sec0;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SOLITUDE.Tests
{
    public sealed class PerformanceRuntimeTests
    {
        [TestCase(16)] [TestCase(64)]
        public void ActiveEnemiesPatrolAroundIndependentBlockers(int count)
        {
            var previous = SceneManager.GetActiveScene();
            var scene = SceneManager.CreateScene("Performance enemies " + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(scene);
            var host = new GameObject("Performance fixture");
            var policy = new InputPolicyCoordinator(new PauseCoordinator()); policy.SetReady(true);
            var simulation = Physics2D.simulationMode;
            var bots = new Sec0Controller[count]; var bodies = new Rigidbody2D[count]; var starts = new Vector2[count];
            var queryCounts = new Func<int>[count];
            try
            {
                var player = new GameObject("Distant target"); player.transform.SetParent(host.transform);
                player.transform.position = new Vector3(1000, 1000, 0);
                var receiver = player.AddComponent<PlayerStunReceiver>();
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Features/Npcs/Sec0/PF_Sec0.prefab");
                for (int i = 0; i < bots.Length; i++)
                {
                    var actor = Object.Instantiate(prefab, host.transform); actor.transform.position = new Vector3(0, i * 10, 0);
                    var bot = actor.GetComponentInChildren<Sec0Controller>(); bots[i] = bot; bodies[i] = bot.GetComponent<Rigidbody2D>(); starts[i] = bodies[i].position;
                    var first = new GameObject("Patrol goal"); first.transform.SetParent(host.transform); first.transform.position = new Vector3(4, i * 10, 0);
                    var second = new GameObject("Patrol return"); second.transform.SetParent(host.transform); second.transform.position = new Vector3(0, i * 10, 0);
                    var obstacle = new GameObject("Patrol blocker"); obstacle.transform.SetParent(host.transform);
                    obstacle.transform.position = new Vector3(2, i * 10, 0); obstacle.AddComponent<BoxCollider2D>().size = Vector2.one;
                    var effect = (GameObject)typeof(Sec0Controller).GetField("stunFxPrefab", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(bot);
                    bot.Configure(bot.GetComponentInChildren<Animator>(), effect, new[] { first.transform, second.transform });
                    bot.Bind(this, player.transform, receiver, policy, () => true);
                    Func<int> Counter(string name) => (Func<int>)Delegate.CreateDelegate(typeof(Func<int>), bot,
                        typeof(Sec0Controller).GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic).GetGetMethod(true));
                    queryCounts[i] = Counter("NavigationOverlapQueries");
                }
                Physics2D.simulationMode = SimulationMode2D.Script; Physics2D.SyncTransforms();
                for (int tick = 0; tick < 5; tick++) { foreach (var bot in bots) bot.Tick(.02f); Physics2D.Simulate(.02f); }
                long queries = 0;
                var clock = Stopwatch.StartNew();
                for (int tick = 0; tick < 100; tick++)
                {
                    for (int i = 0; i < bots.Length; i++)
                    { bots[i].Tick(.02f); queries += queryCounts[i](); }
                    Physics2D.Simulate(.02f);
                }
                clock.Stop();
                TestContext.Out.WriteLine($"PERF {count}activeEnemies100physicsTicks: {clock.Elapsed.TotalMilliseconds:F3} ms (includes 2D simulation, excludes rendering)");
                TestContext.Out.WriteLine($"PERF navigation overlap queries: {queries}");
                for (int i = 0; i < bots.Length; i++)
                { Assert.IsTrue(bots[i].HasBinding); Assert.Greater(Vector2.Distance(starts[i], bodies[i].position), .2f, "Every enemy must actually move."); }
            }
            finally
            {
                foreach (var bot in bots) if (bot != null) bot.Release(this);
                Object.DestroyImmediate(host); policy.Dispose(); Physics2D.simulationMode = simulation;
                SceneManager.SetActiveScene(previous); SceneManager.UnloadSceneAsync(scene);
            }
        }
    }
}
#endif
