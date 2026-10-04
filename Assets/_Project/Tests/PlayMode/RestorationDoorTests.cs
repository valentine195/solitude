using System.Collections;
using System.Reflection;
using NUnit.Framework;
using SOLITUDE.Containers;
using SOLITUDE.Items;
using SOLITUDE.Player;
using SOLITUDE.SaveLoad;
using SOLITUDE.World.Restoration;
using SOLITUDE.World.SolitudeStart;
using UnityEngine;
using UnityEngine.TestTools;

namespace SOLITUDE.Tests
{
    public class RestorationDoorTests
    {
        private GameObject host, systemHost, playerHost;
        private SolitudeSlidingDoor door;
        private BoxCollider2D blocker;
        private RestorableSystem system;
        private Texture2D texture;
        private Sprite sprite;
        private ContainerCommandService commands;
        private Container inventory;
        [SetUp] public void SetUp()
        {
            host = new GameObject("Test door"); host.SetActive(false);
            var visual = host.AddComponent<SpriteRenderer>(); blocker = host.AddComponent<BoxCollider2D>();
            door = host.AddComponent<SolitudeSlidingDoor>();
            texture = new Texture2D(1, 1); sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.one * .5f);
            var poses = new Sprite[9]; for (int i = 0; i < poses.Length; i++) poses[i] = sprite;
            door.Configure(visual, poses, blocker);
            systemHost = new GameObject("Test system"); system = systemHost.AddComponent<RestorableSystem>();
            commands = new ContainerCommandService(new ItemCatalog(new[] { new ItemSpec("cell", true, 10) }));
            inventory = commands.Register(ContainerSaveSession.PlayerInventoryId, 2); commands.Grant(inventory.Handle, "cell", 1);
        }
        private void Restore() => Assert.AreEqual(SOLITUDE.Restoration.RestorationOutcome.Restored, system.State.TryRestore(commands, inventory, "cell"));
        [TearDown] public void TearDown()
        {
            Object.DestroyImmediate(playerHost); Object.DestroyImmediate(host); Object.DestroyImmediate(systemHost);
            Object.DestroyImmediate(sprite); Object.DestroyImmediate(texture);
        }
        [UnityTest] public IEnumerator OfflineDirectAndPlayerCallsBlockThenRestorationPermitsOpening()
        {
            var gate = host.AddComponent<RestorationDoorGate>(); gate.Configure(system); host.SetActive(true);
            Assert.IsTrue(door.CanInteract(null)); Assert.IsFalse(door.Interact(null).IsSuccess);
            StringAssert.Contains("No power", door.GetPrompt()); Assert.IsTrue(blocker.enabled);
            playerHost = new GameObject("Test player"); var player = playerHost.AddComponent<PlayerInteractor>(); player.SetInputAllowed(true);
            typeof(PlayerInteractor).GetField("currentTarget", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(player, door);
            player.TryInteract(); Assert.IsFalse(door.IsOpen); Assert.IsTrue(blocker.enabled);
            Restore(); Assert.IsFalse(door.IsOpen); Assert.IsTrue(blocker.enabled); Assert.IsTrue(gate.AllowsAccess);
            player.TryInteract();
            yield return new WaitForSeconds(1f);
            Assert.IsTrue(door.IsOpen); Assert.IsFalse(blocker.enabled);
        }
        [UnityTest] public IEnumerator UngatedDoorPreservesOpeningBehavior()
        {
            host.SetActive(true); Assert.IsTrue(door.Interact(null).IsSuccess);
            yield return new WaitForSeconds(1f);
            Assert.IsTrue(door.IsOpen); Assert.IsFalse(blocker.enabled);
        }
        [Test] public void LateEnableReenableAndMissingSystemReconcileWithoutOpening()
        {
            var gate = host.AddComponent<RestorationDoorGate>(); gate.Configure(system); Restore(); host.SetActive(true);
            Assert.IsTrue(gate.AllowsAccess); Assert.IsFalse(door.IsOpen);
            for (int i = 0; i < 3; i++) { gate.enabled = false; Assert.IsFalse(door.Interact(null).IsSuccess); gate.enabled = true; Assert.IsTrue(gate.AllowsAccess); }
            gate.Configure(null); Assert.IsFalse(gate.AllowsAccess); Assert.IsFalse(door.Interact(null).IsSuccess);
            gate.Configure(system); Object.DestroyImmediate(systemHost); Assert.IsFalse(door.Interact(null).IsSuccess);
            Object.DestroyImmediate(gate); Assert.IsFalse(door.Interact(null).IsSuccess); Assert.IsTrue(blocker.enabled);
        }
    }
}
