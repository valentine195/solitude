#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SOLITUDE.Composition;
using SOLITUDE.Containers;
using SOLITUDE.Core.Interaction;
using SOLITUDE.Core.UI;
using SOLITUDE.Features.Interactables;
using SOLITUDE.Items;
using SOLITUDE.Player;
using SOLITUDE.SaveLoad;
using SOLITUDE.World.Restoration;
using SOLITUDE.World.SolitudeStart;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SOLITUDE.Tests
{
    public class RestorationSliceTests
    {
        private const string Demo = "Assets/_Project/Scenes/Demos/ShipSystemRestoration.unity";
        private const string Cell = "c756a156ba7f34cb2b65e32264413a60";
        private static T[] All<T>() where T : Object => Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        private static T Field<T>(object o, string n) => (T)o.GetType().GetField(n, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(o);
        private static GameCompositionRoot Root => All<GameCompositionRoot>().SingleOrDefault();
        private static AsyncOperation Load() => EditorSceneManager.LoadSceneAsyncInPlayMode(Demo, new LoadSceneParameters(LoadSceneMode.Single));
        private string savePath; private byte[] original;
        private PlayerInteractor player;
        private static int Count(IContainerReader inventory) => inventory.GetSlots().Where(s => s.Stack?.ItemId == Cell).Sum(s => s.Stack.Quantity);
        private static string Feedback => Field<TextMeshProUGUI>(All<InteractionFeedbackView>().Single(), "label").text;
        private static string Prompt => Field<TextMeshProUGUI>(All<InteractionPromptView>().Single(), "promptText").text;

        [UnitySetUp] public IEnumerator Prepare()
        {
            var args = Environment.GetCommandLineArgs(); int i = Array.IndexOf(args, "-solitudeSaveDirectory");
            Assert.GreaterOrEqual(i, 0, "Requires isolated verification runner.");
            if (Root != null) { Object.Destroy(Root.gameObject); yield return null; }
            savePath = Path.Combine(args[i + 1], "solitude-save.json");
            original = File.Exists(savePath) ? File.ReadAllBytes(savePath) : null;
            File.WriteAllText(savePath, "RESTORATION-SLICE-SENTINEL");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (player != null) player.GetComponent<PlayerMovement>().SetInput(Vector2.zero);
            if (Root != null) Object.Destroy(Root.gameObject);
            yield return null; Time.timeScale = 1;
            if (savePath == null) yield break;
            if (original == null) { if (File.Exists(savePath)) File.Delete(savePath); }
            else File.WriteAllBytes(savePath, original);
        }
        // Positioning skips travel between props, but uses real proximity sensors and focus selection.
        private IEnumerator Focus(Component target, float below = .6f)
        {
            var body = player.GetComponent<Rigidbody2D>();
            body.position = (Vector2)target.transform.position + Vector2.down * below;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate(); yield return null; yield return null;
            Assert.AreSame(target, Field<IInteractable>(player, "currentTarget"), "Scene sensor/focus for " + target.name);
        }
        private IEnumerator WalkNorth(int frames)
        {
            player.GetComponent<PlayerMovement>().SetInput(Vector2.up);
            for (int i = 0; i < frames; i++) yield return new WaitForFixedUpdate();
            player.GetComponent<PlayerMovement>().SetInput(Vector2.zero);
            yield return new WaitForFixedUpdate();
        }
        [UnityTest] public IEnumerator FullLoopThroughSceneTargetingTransferPassageAndReload()
        {
            yield return Load(); yield return null;
            Assert.AreEqual(RuntimeStartupState.Ready, Root.State); Assert.IsTrue(Root.UsesDisposableSession);
            player = All<PlayerInteractor>().Single();
            var inventory = All<PlayerInventory>().Single().Container;
            var commands = Field<ContainerSaveSession>(Root, "session").Commands;
            var junction = All<RestorationJunction>().Single(j => j.name == "Auxiliary power socket");
            var second = All<RestorationJunction>().Single(j => j != junction);
            var main = Field<RestorableSystem>(junction, "system"); var other = Field<RestorableSystem>(second, "system");
            var terminal = All<RestorationTerminal>().Single(t => Field<RestorableSystem>(t, "system") == main);
            var otherTerminal = All<RestorationTerminal>().Single(t => t != terminal);
            var door = All<SolitudeSlidingDoor>().Single(); var blocker = Field<Collider2D>(door, "blocker");
            int transitions = 0; main.State.Changed += () => transitions++;
            Assert.IsFalse(main.IsRestored); Assert.IsFalse(other.IsRestored);
            yield return Focus(door); player.TryInteract(); StringAssert.Contains("No power", Feedback);
            yield return WalkNorth(35);
            Assert.Less(player.transform.position.y, blocker.bounds.min.y, "Closed door prevents physical passage");
            yield return Focus(junction); StringAssert.Contains("Offline", Prompt);
            player.TryInteract(); StringAssert.Contains("Requires 1", Feedback); Assert.AreEqual(0, Count(inventory));
            Assert.IsFalse(main.IsRestored);
            yield return Focus(terminal); player.TryInteract(); StringAssert.Contains("offline", Feedback);
            var screen = Field<SpriteRenderer>(terminal, "screen"); var off = screen.color;
            terminal.gameObject.SetActive(false); // Miss the transition deliberately.
            foreach (var pickup in All<WorldPickup>().Where(p => p.Definition.Item.ItemId == Cell))
            { yield return Focus(pickup); player.TryInteract(); }
            Assert.AreEqual(2, Count(inventory));
            var locker = All<LockerContainer>().Single(); Assert.IsTrue(locker.EnsureInitialized());
            var stored = locker.Container.GetSlots().First(s => s.IsEmpty);
            var held = inventory.GetSlots().First(s => s.Stack?.ItemId == Cell);
            Assert.AreEqual(CommandStatus.Applied, commands.Transfer(held.Address, stored.Address).Status);
            yield return Focus(junction); player.TryInteract(); Assert.IsFalse(main.IsRestored);
            StringAssert.Contains("Requires 1", Feedback); Assert.AreEqual(0, Count(inventory));
            Assert.AreEqual(CommandStatus.Applied, commands.Transfer(locker.Container.GetSlot(stored.Address.Index).Address, inventory.GetSlots().First(s => s.IsEmpty).Address).Status);
            player.TryInteract(); yield return null;
            Assert.AreEqual(1, Count(inventory)); Assert.IsTrue(main.IsRestored); Assert.IsFalse(other.IsRestored);
            Assert.AreEqual("System restored", Prompt); Assert.AreEqual("System restored", Feedback);
            Assert.IsFalse(door.IsOpen); Assert.IsTrue(blocker.enabled); Assert.IsFalse(otherTerminal.IsPowered);
            player.TryInteract(); Assert.AreEqual(1, Count(inventory)); Assert.AreEqual(1, transitions);
            terminal.gameObject.SetActive(true); Assert.IsTrue(terminal.IsPowered); Assert.AreNotEqual(off, screen.color);
            for (int i = 0; i < 3; i++) { terminal.enabled = false; terminal.enabled = true; }
            Assert.IsTrue(terminal.IsPowered); Assert.AreEqual(1, transitions);
            yield return Focus(door); player.TryInteract(); yield return new WaitForSeconds(1f);
            Assert.IsTrue(door.IsOpen); Assert.IsFalse(blocker.enabled);
            yield return WalkNorth(40);
            Assert.Greater(player.transform.position.y, door.transform.position.y + 1.2f, "Player traverses doorway using real movement/physics");
            yield return Focus(terminal); player.TryInteract(); StringAssert.Contains("AI fragment", Feedback);
            var message = Feedback; player.TryInteract(); Assert.AreEqual(message, Feedback); Assert.AreEqual(1, Count(inventory));
            yield return Focus(second); player.TryInteract(); Assert.IsTrue(other.IsRestored); Assert.IsTrue(otherTerminal.IsPowered);
            Assert.AreEqual(0, Count(inventory)); Assert.AreEqual(1, transitions);
            Field<SaveGameService>(Root, "persistence").Flush();
            Assert.AreEqual("RESTORATION-SLICE-SENTINEL", File.ReadAllText(savePath));
            var root = Root; yield return Load(); yield return null;
            Assert.AreSame(root, Root); Assert.AreEqual(RuntimeStartupState.Ready, Root.State);
            player = All<PlayerInteractor>().Single(); Assert.AreEqual(0, Count(All<PlayerInventory>().Single().Container));
            Assert.IsTrue(All<RestorableSystem>().All(s => !s.IsRestored)); Assert.IsTrue(All<RestorationTerminal>().All(t => !t.IsPowered));
            Assert.IsFalse(All<SolitudeSlidingDoor>().Single().IsOpen);
            Assert.AreEqual(2, All<WorldPickup>().Count(p => p.Definition.Item.ItemId == Cell && p.gameObject.activeSelf));
            Assert.AreEqual("RESTORATION-SLICE-SENTINEL", File.ReadAllText(savePath));
        }
    }
}
#endif
