#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using SOLITUDE.Composition;
using SOLITUDE.Npcs.Sec0;
using SOLITUDE.Player;
using SOLITUDE.World.Restoration;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace SOLITUDE.Tests
{
    public sealed class Sec0EncounterTests
    {
        [UnityTest] public IEnumerator AuthoredDemoBindsPatrolsStunsAndReloadsWithDisposableState()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/_Project/Scenes/Demos/Sec0Patrol.unity",new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var root=Object.FindFirstObjectByType<GameCompositionRoot>();Assert.IsNotNull(root);Assert.AreEqual(RuntimeStartupState.Ready,root.State);Assert.IsTrue(root.UsesDisposableSession);
            var files=Object.FindFirstObjectByType<RestorationDemoSession>().Files;Assert.IsNotNull(files);
            var capture=System.Type.GetType("SOLITUDE.Editor.Sec0NpcBuilder, SOLITUDE.Editor").GetMethod("CapturePreview");
            capture.Invoke(null,new object[]{System.IO.Path.Combine(UnityEngine.Application.dataPath,"../work/Reviews/Sec0Npc/demo_preview.png")});
            var bot=Object.FindFirstObjectByType<Sec0Controller>();Assert.IsTrue(bot.HasBinding);
            var player=Object.FindFirstObjectByType<PlayerMovement>();
            var stun=player.GetComponent<PlayerStunReceiver>();
            Vector2 start=bot.transform.position;
            player.transform.position=new Vector3(60,-2,0);player.GetComponent<Rigidbody2D>().position=player.transform.position;player.SetInput(Vector2.zero);
            yield return new WaitForSeconds(1.1f);Assert.That(Vector2.Distance(start,bot.transform.position),Is.GreaterThan(.2f));
            Vector2 target=(Vector2)bot.transform.position+bot.Facing*1.1f;
            player.transform.position=target;player.GetComponent<Rigidbody2D>().position=target;Physics2D.SyncTransforms();
            for(int i=0;i<150 && bot.StunsLanded==0;i++) yield return new WaitForFixedUpdate();
            Assert.That(bot.StunsLanded,Is.GreaterThan(0),"Composed target should receive a short-range stun.");Assert.IsTrue(stun.IsStunned);
            Assert.AreEqual(100,player.GetComponent<PlayerVitalsController>().Vitals.Health.Current);
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/_Project/Scenes/Demos/Sec0Patrol.unity",new LoadSceneParameters(LoadSceneMode.Single));yield return null;
            Assert.AreSame(root,Object.FindFirstObjectByType<GameCompositionRoot>());Assert.AreEqual(RuntimeStartupState.Ready,root.State);
            Assert.IsFalse(Object.FindFirstObjectByType<PlayerStunReceiver>().IsStunned);Assert.AreEqual(0,Object.FindFirstObjectByType<Sec0Controller>().ShotsFired);
            Assert.IsTrue(root.UsesDisposableSession);Assert.Greater(files.Reads,0);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            var root=Object.FindFirstObjectByType<GameCompositionRoot>();if(root!=null)Object.Destroy(root.gameObject);
            yield return null;
            var scene=SceneManager.GetActiveScene();var empty=SceneManager.CreateScene("Sec0TestCleanup");SceneManager.SetActiveScene(empty);yield return SceneManager.UnloadSceneAsync(scene);
        }
    }
}
#endif
