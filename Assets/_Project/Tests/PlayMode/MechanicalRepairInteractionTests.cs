#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using SOLITUDE.Composition;
using SOLITUDE.GameplayTags;
using SOLITUDE.Items;
using SOLITUDE.Player;
using SOLITUDE.SaveLoad;
using SOLITUDE.World.Restoration;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
namespace SOLITUDE.Tests
{
    public class MechanicalRepairInteractionTests
    {
        private GameObject host, door;
        private MechanicalRepairDefinition definition;
        private Keyboard keyboard;
        private Gamepad pad;
        private InputSettings.BackgroundBehavior background;
        private InputSettings.EditorInputBehaviorInPlayMode editor;
        [UnitySetUp] public IEnumerator Prepare()
        {
            background = InputSystem.settings.backgroundBehavior; editor = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard = InputSystem.AddDevice<Keyboard>(); pad = InputSystem.AddDevice<Gamepad>();
            yield return WeaponSystemTestFixtures.Load();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Object.Destroy(host); Object.Destroy(door); Object.Destroy(definition);
            if (keyboard != null) InputSystem.RemoveDevice(keyboard); if (pad != null) InputSystem.RemoveDevice(pad);
            InputSystem.settings.backgroundBehavior = background; InputSystem.settings.editorInputBehaviorInPlayMode = editor;
            yield return WeaponSystemTestFixtures.Unload();
        }
        [UnityTest] public IEnumerator UnrepairedTargetResumesAfterComponentActivation()
        { yield return ActivationCycle(false, false); }
        [UnityTest] public IEnumerator UnrepairedTargetResumesAfterGameObjectActivation()
        { yield return ActivationCycle(true, false); }
        [UnityTest] public IEnumerator LoadedCompletedTargetResumesAfterComponentActivation()
        { yield return ActivationCycle(false, true); }
        [UnityTest] public IEnumerator LoadedCompletedTargetResumesAfterGameObjectActivation()
        { yield return ActivationCycle(true, true); }
        private IEnumerator ActivationCycle(bool deactivateObject, bool completed)
        {
            var database = Resources.Load<ItemDatabase>("Database");
            var capabilities = database.BuildCapabilityCatalog();
            var battery = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Features/Items/Definitions/Battery/Battery.asset");
            definition = ScriptableObject.CreateInstance<MechanicalRepairDefinition>();
            definition.Configure(database.TagRegistry, AssetDatabase.LoadAssetAtPath<GameplayTagDefinition>("Assets/_Project/Systems/GameplayTags/Assets/ToolMechanical.asset"), battery);
            const string targetId = "verification.activation.socket";
            var requirement = definition.Compile(targetId, capabilities.Registry);
            var seed = new ContainerSaveSession(new SaveGameData { startingLoadoutInitialized = true }, database.BuildRuntimeCatalog());
            using (var inventory = seed.Register(ContainerSaveSession.PlayerInventoryId, 5))
            using (var hotbar = seed.Register(ContainerSaveSession.PlayerHotbarId, 4))
            {
                seed.Commands.Grant(inventory.Container.Handle, battery.ItemId, 2);
                seed.Commands.Grant(hotbar.Container.Handle, WeaponSystemTestFixtures.Wrench.ItemId, 1);
                if (completed)
                    using (var binding = seed.BindMechanicalRepair(requirement))
                        Assert.AreEqual(SOLITUDE.Restoration.MechanicalRepairOutcome.Committed,
                            binding.TryRepair(inventory.Container.Handle, hotbar.Container.Handle, id => capabilities.Resolve(id)?.Tags));
            }
            // Reconstruct the session from its saved facts before binding presentation.
            var session = new ContainerSaveSession(seed.Capture(), database.BuildRuntimeCatalog());
            using (var inventory = session.Register(ContainerSaveSession.PlayerInventoryId, 5))
            using (var hotbar = session.Register(ContainerSaveSession.PlayerHotbarId, 4))
            {
                var actor = WeaponSystemTestFixtures.One<PlayerInteractor>();
                host = new GameObject("Activation repair");
                var system = host.AddComponent<RestorableSystem>();
                var junction = host.AddComponent<RestorationJunction>();
                var identity = host.AddComponent<SaveableId>(); identity.AssignRuntimeIdentity(targetId);
                door = new GameObject("Activation gated door");
                var gate = door.AddComponent<RestorationDoorGate>(); gate.Configure(system);
                var composition = host.AddComponent<MechanicalRepairComposition>();
                composition.Configure(identity, definition, junction, system, gate);
                composition.Initialize(new SceneFeatureContext(session.Commands, inventory.Container, database.BuildPresentationCatalog(),
                    null, null, null, null, actor, null, hotbar.Container, lifecycleSession: session, capabilities: capabilities));
                Assert.AreEqual(completed, system.IsRestored); Assert.AreEqual(completed, gate.AllowsAccess);
                int quantity = completed ? 1 : 2;
                if (deactivateObject) host.SetActive(false); else composition.enabled = false;
                yield return null;
                Assert.IsFalse(junction.Interact(actor).IsSuccess);
                Assert.AreEqual(quantity, inventory.Container.GetSlot(0).Stack.Quantity);
                // Disabling presentation must not surrender scene-owned target identity.
                Assert.Throws<System.InvalidOperationException>(() => session.BindMechanicalRepair(requirement));
                if (deactivateObject) host.SetActive(true); else composition.enabled = true;
                yield return null;
                Assert.AreEqual(completed, system.IsRestored); Assert.AreEqual(completed, gate.AllowsAccess);
                if (completed) StringAssert.Contains("already restored", junction.Interact(actor).Message);
                else Assert.IsTrue(junction.Interact(actor).IsSuccess);
                Assert.IsTrue(system.IsRestored); Assert.IsTrue(gate.AllowsAccess);
                Assert.IsFalse(door.GetComponent<SOLITUDE.World.SolitudeStart.SolitudeSlidingDoor>().IsOpen);
                Assert.AreEqual(1, inventory.Container.GetSlot(0).Stack.Quantity);
                Assert.AreEqual(1, hotbar.Container.GetSlot(0).Stack.Quantity);
                composition.Release(); Assert.IsFalse(gate.AllowsAccess);
                Assert.IsFalse(junction.Interact(actor).IsSuccess);
                using (session.BindMechanicalRepair(requirement)) { }
            }
        }
        [UnityTest] public IEnumerator ContextualInputUsesLiveCarriedRequirementsAndNeverRequestsPrimary()
        {
            var database = Resources.Load<ItemDatabase>("Database"); var capabilities = database.BuildCapabilityCatalog();
            var battery = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Features/Items/Definitions/Battery/Battery.asset");
            var actor = WeaponSystemTestFixtures.One<PlayerInteractor>();
            var inventory = WeaponSystemTestFixtures.One<PlayerInventory>().Container;
            host = new GameObject("Persistent repair interaction"); host.transform.position = actor.transform.position;
            var system = host.AddComponent<RestorableSystem>(); var junction = host.AddComponent<RestorationJunction>();
            var id = host.AddComponent<SaveableId>(); id.AssignRuntimeIdentity("verification.interaction.socket");
            door = new GameObject("Gated door"); var gate = door.AddComponent<RestorationDoorGate>(); gate.Configure(system);
            definition = ScriptableObject.CreateInstance<MechanicalRepairDefinition>();
            definition.Configure(database.TagRegistry, AssetDatabase.LoadAssetAtPath<GameplayTagDefinition>("Assets/_Project/Systems/GameplayTags/Assets/ToolMechanical.asset"), battery);
            var composition = host.AddComponent<MechanicalRepairComposition>(); composition.Configure(id, definition, junction, system, gate);
            var selection = WeaponSystemTestFixtures.Field<SOLITUDE.Application.HotbarSelectionState>(WeaponSystemTestFixtures.Bar, "selection");
            composition.Initialize(new SceneFeatureContext(WeaponSystemTestFixtures.Session.Commands, inventory, database.BuildPresentationCatalog(),
                WeaponSystemTestFixtures.Policy, WeaponSystemTestFixtures.Input, null, null, actor, null, WeaponSystemTestFixtures.Bar.Container,
                selection, lifecycleSession: WeaponSystemTestFixtures.Session, capabilities: capabilities));
            actor.Register(junction); WeaponSystemTestFixtures.Bar.SetActive(1); yield return null; yield return null;
            int swings = 0; WeaponSystemTestFixtures.Actions.ActiveSample += _ => swings++;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E)); InputSystem.Update(); yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update(); yield return null;
            Assert.IsFalse(system.IsRestored); Assert.AreEqual(0, swings);
            WeaponSystemTestFixtures.Session.Commands.Grant(inventory.Handle, battery.ItemId, 1);
            using (WeaponSystemTestFixtures.Policy.AcquirePrimaryScreen())
            {
                Assert.IsFalse(junction.Interact(actor).IsSuccess); Assert.IsFalse(system.IsRestored);
            }
            yield return null; yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E)); InputSystem.Update(); yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update(); yield return null;
            Assert.IsTrue(system.IsRestored); Assert.IsTrue(gate.AllowsAccess); Assert.AreEqual(0, swings);
            Assert.AreEqual(1, selection.ActiveIndex); Assert.AreEqual(1, WeaponSystemTestFixtures.Bar.Container.GetSlot(0).Stack.Quantity);
            WeaponSystemTestFixtures.Bar.SetActive(0); yield return null;
            InputSystem.QueueStateEvent(pad, new GamepadState { rightTrigger = 1 }); InputSystem.Update(); yield return null; yield return null;
            Assert.IsTrue(WeaponSystemTestFixtures.Actions.Runner.IsExecuting);
            yield return new WaitForSecondsRealtime(.7f); Assert.Greater(swings, 0);
            composition.Release(); Assert.IsFalse(gate.AllowsAccess); Assert.IsFalse(junction.Interact(actor).IsSuccess);
        }
    }
}
#endif
