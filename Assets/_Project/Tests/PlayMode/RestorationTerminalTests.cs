using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using SOLITUDE.Containers;
using SOLITUDE.Core.UI;
using SOLITUDE.Items;
using SOLITUDE.Restoration;
using SOLITUDE.SaveLoad;
using SOLITUDE.World.Restoration;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SOLITUDE.Tests
{
    public class RestorationTerminalTests
    {
        private GameObject host, systemHost, otherHost, ui;
        private RestorationTerminal terminal;
        private RestorableSystem system;
        private SpriteRenderer screen;
        private ContainerCommandService commands;
        private Container inventory;
        [SetUp] public void SetUp()
        {
            systemHost = new GameObject("System"); system = systemHost.AddComponent<RestorableSystem>();
            host = new GameObject("Terminal"); host.SetActive(false);
            screen = host.AddComponent<SpriteRenderer>(); terminal = host.AddComponent<RestorationTerminal>();
            terminal.Configure(system, screen, "Placeholder transmission.", Color.black, Color.green);
            commands = new ContainerCommandService(new ItemCatalog(new[] { new ItemSpec("cell", true, 10) }));
            inventory = commands.Register(ContainerSaveSession.PlayerInventoryId, 2); commands.Grant(inventory.Handle, "cell", 2);
        }
        private void Restore() => Assert.AreEqual(RestorationOutcome.Restored, system.State.TryRestore(commands, inventory, "cell"));
        [TearDown] public void TearDown()
        {
            Object.DestroyImmediate(ui); Object.DestroyImmediate(host); Object.DestroyImmediate(otherHost); Object.DestroyImmediate(systemHost);
        }
        [UnityTest] public IEnumerator PowerChangesScreenAndDisplaysRepeatableConfiguredContent()
        {
            host.SetActive(true); Assert.AreEqual(Color.black, screen.color); Assert.IsFalse(terminal.Interact(null).IsSuccess);
            Restore(); Assert.IsTrue(terminal.IsPowered); Assert.AreEqual(Color.green, screen.color);
            Assert.AreEqual("Read terminal", terminal.GetPrompt());
            for (int i = 0; i < 2; i++) Assert.AreEqual("Placeholder transmission.", terminal.Interact(null).Message);
            ui = new GameObject("Feedback"); var view = ui.AddComponent<InteractionFeedbackView>();
            var text = new GameObject("Text", typeof(RectTransform)); text.transform.SetParent(ui.transform);
            var label = text.AddComponent<TMPro.TextMeshProUGUI>();
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(InteractionFeedbackView).GetField("root", flags).SetValue(view, text);
            typeof(InteractionFeedbackView).GetField("label", flags).SetValue(view, label);
            InteractionFeedback.Initialize(view); InteractionFeedback.Handle(terminal.Interact(null));
            yield return null;
            Assert.AreEqual("Placeholder transmission.", label.text); Assert.IsTrue(text.activeSelf);
            Assert.AreEqual(1, inventory.GetSlot(0).Stack.Quantity);
        }
        [Test] public void LateEnableAndEnableCyclesReconcileWithoutRepeatedInstallation()
        {
            Restore(); host.SetActive(true);
            for (int i = 0; i < 3; i++)
            {
                Assert.IsTrue(terminal.IsPowered); Assert.AreEqual(Color.green, screen.color);
                terminal.enabled = false; Assert.IsFalse(terminal.Interact(null).IsSuccess); Assert.AreEqual(Color.black, screen.color);
                terminal.enabled = true;
            }
            Assert.IsTrue(terminal.IsPowered); Assert.AreEqual(1, inventory.GetSlot(0).Stack.Quantity);
            // Verify the subscription count is stable across enable cycles.
            var handlers = (Delegate)typeof(RestorableSystemState).GetField("Changed", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(system.State);
            Assert.AreEqual(1, handlers.GetInvocationList().Length);
        }
        [Test] public void OtherSystemsAndMissingDependenciesStayOffline()
        {
            otherHost = new GameObject("Independent system"); var other = otherHost.AddComponent<RestorableSystem>();
            terminal.Configure(other, screen, "Content", Color.black, Color.green); host.SetActive(true); Restore();
            Assert.IsFalse(terminal.IsPowered); Assert.AreEqual(Color.black, screen.color);
            terminal.Configure(null, screen, "Content", Color.black, Color.green); Assert.IsFalse(terminal.Interact(null).IsSuccess);
            terminal.Configure(system, null, "Content", Color.black, Color.green); Assert.IsFalse(terminal.Interact(null).IsSuccess);
        }
        [Test] public void FailingEarlierSubscriberDoesNotPreventTerminalPower()
        {
            system.State.Changed += () => throw new InvalidOperationException("terminal observer fixture");
            host.SetActive(true);
            LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("InvalidOperationException: terminal observer fixture"));
            Restore(); Assert.IsTrue(terminal.IsPowered); Assert.AreEqual(Color.green, screen.color);
        }
        [UnityTest] public IEnumerator DestroyedSystemTurnsOffPresentation()
        {
            host.SetActive(true); Restore(); Object.DestroyImmediate(systemHost);
            yield return null;
            Assert.IsFalse(terminal.Interact(null).IsSuccess); Assert.AreEqual(Color.black, screen.color);
        }
    }
}
