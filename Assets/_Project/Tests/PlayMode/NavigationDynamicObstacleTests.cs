#if UNITY_EDITOR
using System;
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
    public sealed class NavigationCacheBehaviorTests
    {
        [Test] public void MovingBlockerAndFreshBindingAreObservedByFollowingPhysicsTicks()
        {
            var previous = SceneManager.GetActiveScene();
            var scene = SceneManager.CreateScene("Navigation invalidation " + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(scene);
            var host = new GameObject("Navigation fixture");
            var policy = new InputPolicyCoordinator(new PauseCoordinator()); policy.SetReady(true);
            var simulation = Physics2D.simulationMode; Sec0Controller bot = null;
            try
            {
                var target = new GameObject("Target"); target.transform.SetParent(host.transform); target.transform.position = Vector3.one * 1000;
                var receiver = target.AddComponent<PlayerStunReceiver>();
                var actor = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Features/Npcs/Sec0/PF_Sec0.prefab"), host.transform);
                bot = actor.GetComponentInChildren<Sec0Controller>(); var body = bot.GetComponent<Rigidbody2D>(); var footprint = bot.GetComponent<BoxCollider2D>();
                var first = new GameObject("Goal"); first.transform.SetParent(host.transform); first.transform.position = Vector3.right * 4;
                var second = new GameObject("Return"); second.transform.SetParent(host.transform);
                var fx = (GameObject)typeof(Sec0Controller).GetField("stunFxPrefab", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(bot);
                bot.Configure(bot.GetComponentInChildren<Animator>(), fx, new[] { first.transform, second.transform });
                bot.Bind(this, target.transform, receiver, policy, () => true);
                Physics2D.simulationMode = SimulationMode2D.Script;
                void Step() { bot.Tick(.02f); Physics2D.Simulate(.02f); }
                Physics2D.SyncTransforms(); for (int i = 0; i < 20; i++) Step(); Assert.Greater(body.position.x, .2f);
                var blocker = new GameObject("Moving door"); blocker.transform.SetParent(host.transform);
                blocker.transform.position = new Vector3(body.position.x + 1, .17f, 0); var collider = blocker.AddComponent<BoxCollider2D>(); collider.size = Vector2.one;
                Physics2D.SyncTransforms();
                for (int i = 0; i < 80; i++)
                { Step(); Assert.GreaterOrEqual(footprint.Distance(collider).distance, -Physics2D.defaultContactOffset); }
                blocker.transform.position = Vector3.up * 20; Physics2D.SyncTransforms();
                float afterDoor = body.position.x;
                for (int i = 0; i < 80; i++) Step(); Assert.Greater(body.position.x, afterDoor + .5f);
                bot.Release(this); body.position = Vector2.zero;
                bot.Bind(this, target.transform, receiver, policy, () => true); Physics2D.SyncTransforms();
                for (int i = 0; i < 20; i++) Step(); Assert.Greater(body.position.x, .2f);
            }
            finally
            {
                if (bot != null) bot.Release(this);
                Object.DestroyImmediate(host); policy.Dispose(); Physics2D.simulationMode = simulation;
                SceneManager.SetActiveScene(previous); SceneManager.UnloadSceneAsync(scene);
            }
        }
    }
}
#endif
