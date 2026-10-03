using System.Reflection;
using NUnit.Framework;
using SOLITUDE.Containers;
using SOLITUDE.Items;
using SOLITUDE.SaveLoad;
using SOLITUDE.World.Restoration;
using UnityEngine;

namespace SOLITUDE.Tests
{
    public class RestorationJunctionTests
    {
        private GameObject host;
        private ItemDefinition item;
        private RestorationJunction junction;
        private RestorableSystem system;
        private ContainerCommandService commands;
        private Container inventory;
        [SetUp] public void SetUp()
        {
            item = ScriptableObject.CreateInstance<ItemDefinition>(); item.DisplayName = "Power cell";
            typeof(ItemDefinition).GetField("itemId", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(item, "cell");
            host = new GameObject("Junction test"); system = host.AddComponent<RestorableSystem>(); junction = host.AddComponent<RestorationJunction>();
            commands = new ContainerCommandService(new ItemCatalog(new[] { new ItemSpec("cell", true, 10) }));
            inventory = commands.Register(ContainerSaveSession.PlayerInventoryId, 2);
            junction.Configure(system, item); junction.Initialize(commands, inventory);
        }
        [TearDown] public void TearDown() { Object.DestroyImmediate(host); Object.DestroyImmediate(item); }
        [Test] public void DiagnosisInstallationAndEnableCyclesKeepState()
        {
            StringAssert.Contains("Power cell", junction.GetPrompt());
            Assert.IsFalse(junction.Interact(null).IsSuccess); Assert.IsFalse(system.IsRestored);
            commands.Grant(inventory.Handle, "cell", 2);
            Assert.IsTrue(junction.Interact(null).IsSuccess); Assert.AreEqual("System restored", junction.GetPrompt());
            host.SetActive(false); host.SetActive(true);
            Assert.IsTrue(system.IsRestored); Assert.IsFalse(junction.Interact(null).IsSuccess);
            Assert.AreEqual(1, inventory.GetSlot(0).Stack.Quantity);
        }
        [Test] public void MissingConfigurationAndReleasedOrDisabledBindingRejectWithoutLoss()
        {
            commands.Grant(inventory.Handle, "cell", 1);
            junction.Configure(null, item); Assert.IsFalse(junction.Interact(null).IsSuccess);
            junction.Configure(system, null); Assert.IsFalse(junction.Interact(null).IsSuccess);
            junction.Configure(system, item); junction.ReleaseBinding(); Assert.IsFalse(junction.Interact(null).IsSuccess);
            junction.Initialize(commands, inventory); junction.enabled = false; Assert.IsFalse(junction.Interact(null).IsSuccess);
            Assert.IsFalse(system.IsRestored); Assert.AreEqual(1, inventory.GetSlot(0).Stack.Quantity);
        }
        [Test] public void FocusedPromptRefreshesWithoutAnotherFocusEvent()
        {
            var ui = new GameObject("Prompt test"); ui.SetActive(false);
            var textObject = new GameObject("Text", typeof(RectTransform)); textObject.transform.SetParent(ui.transform);
            var label = textObject.AddComponent<TMPro.TextMeshProUGUI>();
            var view = ui.AddComponent<SOLITUDE.Core.UI.InteractionPromptView>();
            var controller = ui.AddComponent<SOLITUDE.Core.UI.UIController>();
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            typeof(SOLITUDE.Core.UI.InteractionPromptView).GetField("root", flags).SetValue(view, textObject);
            typeof(SOLITUDE.Core.UI.InteractionPromptView).GetField("promptText", flags).SetValue(view, label);
            typeof(SOLITUDE.Core.UI.UIController).GetField("interactionPrompt", flags).SetValue(controller, view);
            try
            {
                ui.SetActive(true);
                // EditMode does not dispatch the runtime MonoBehaviour lifecycle.
                typeof(SOLITUDE.Core.UI.UIController).GetMethod("OnEnable", flags).Invoke(controller, null);
                SOLITUDE.Core.Events.InteractionEventBus.Publish(new SOLITUDE.Core.Events.InteractionFocusChangedEvent { interactable = junction });
                StringAssert.Contains("Power cell", label.text);
                commands.Grant(inventory.Handle, "cell", 1); junction.Interact(null);
                typeof(SOLITUDE.Core.UI.UIController).GetMethod("LateUpdate", flags).Invoke(controller, null);
                Assert.AreEqual("System restored", label.text);
                typeof(SOLITUDE.Core.UI.UIController).GetMethod("OnDisable", flags).Invoke(controller, null);
                Assert.IsFalse(textObject.activeSelf);
            }
            finally
            {
                typeof(SOLITUDE.Core.UI.UIController).GetMethod("OnDisable", flags).Invoke(controller, null);
                Object.DestroyImmediate(ui);
            }
        }
        [Test] public void SceneReleaseClearsInjectedJunctionBinding()
        {
            var scope = host.AddComponent<SOLITUDE.Composition.SceneBindings>();
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            typeof(SOLITUDE.Composition.SceneBindings).GetField("restorationJunctions", flags).SetValue(scope, new[] { junction });
            typeof(SOLITUDE.Composition.SceneBindings).GetField("session", flags).SetValue(scope,
                new ContainerSaveSession(new SaveGameData(), new ItemCatalog(new[] { new ItemSpec("cell", true, 10) })));
            commands.Grant(inventory.Handle, "cell", 1); scope.Release();
            Assert.IsFalse(junction.Interact(null).IsSuccess); Assert.IsFalse(system.IsRestored);
        }
    }
}
