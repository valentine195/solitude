using System;
using NUnit.Framework;
using SOLITUDE.Composition;
using SOLITUDE.Items;
using SOLITUDE.GameplayTags;
using SOLITUDE.Player;
using SOLITUDE.SaveLoad;
using SOLITUDE.World.Restoration;
using SOLITUDE.World.SolitudeStart;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SOLITUDE.Tests
{
    public class MechanicalRepairBindingTests
    {
        public const string DefinitionPath = "Assets/_Project/Features/World/Restoration/AuxiliarySocketRepair.asset";
        public static void CreateSelectedConfiguration()
        {
            if (AssetDatabase.LoadAssetAtPath<MechanicalRepairDefinition>(DefinitionPath) != null) return;
            var definition = ScriptableObject.CreateInstance<MechanicalRepairDefinition>();
            definition.Configure(AssetDatabase.LoadAssetAtPath<GameplayTagRegistryAsset>("Assets/_Project/Systems/GameplayTags/Assets/Registry.asset"),
                AssetDatabase.LoadAssetAtPath<GameplayTagDefinition>("Assets/_Project/Systems/GameplayTags/Assets/ToolMechanical.asset"),
                AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Features/Items/Definitions/Battery/Battery.asset"));
            definition.ValidateAuthoring(); AssetDatabase.CreateAsset(definition, DefinitionPath); AssetDatabase.SaveAssets();
        }
        private GameObject host, doorHost, playerHost;
        private MechanicalRepairComposition composition;
        private RestorableSystem system;
        private RestorationJunction junction;
        private RestorationDoorGate gate;
        private PlayerInteractor actor;
        private ContainerSaveSession session;
        private ContainerSaveSession.ContainerBinding inventory, hotbar;
        private SceneFeatureContext context;
        private ItemDefinition battery, wrench;
        [SetUp] public void SetUp()
        {
            var database = Resources.Load<ItemDatabase>("Database");
            battery = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Features/Items/Definitions/Battery/Battery.asset");
            wrench = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Features/Items/Weapons/MaintenanceWrench/MaintenanceWrench.asset");
            session = new ContainerSaveSession(new SaveGameData { startingLoadoutInitialized = true }, database.BuildRuntimeCatalog());
            inventory = session.Register(ContainerSaveSession.PlayerInventoryId, 5); hotbar = session.Register(ContainerSaveSession.PlayerHotbarId, 4);
            host = new GameObject("Mechanical binding"); system = host.AddComponent<RestorableSystem>(); junction = host.AddComponent<RestorationJunction>();
            var identity = host.AddComponent<SaveableId>(); identity.AssignRuntimeIdentity("verification.auxiliary.socket");
            doorHost = new GameObject("Door"); gate = doorHost.AddComponent<RestorationDoorGate>(); gate.Configure(system);
            playerHost = new GameObject("Actor"); actor = playerHost.AddComponent<PlayerInteractor>();
            composition = host.AddComponent<MechanicalRepairComposition>();
            composition.Configure(identity, AssetDatabase.LoadAssetAtPath<MechanicalRepairDefinition>(DefinitionPath), junction, system, gate);
            context = new SceneFeatureContext(session.Commands, inventory.Container, database.BuildPresentationCatalog(), null, null, null, null, actor, null,
                hotbar.Container, lifecycleSession: session, capabilities: database.BuildCapabilityCatalog());
            composition.Initialize(context);
        }
        [TearDown] public void TearDown() { Object.DestroyImmediate(host); Object.DestroyImmediate(doorHost); Object.DestroyImmediate(playerHost); inventory?.Dispose(); hotbar?.Dispose(); }
        [Test] public void LiveRequirementsFeedbackAndCanonicalPresentationAreCoherent()
        {
            StringAssert.Contains("mechanical tool", junction.Interact(actor).Message);
            session.Commands.Grant(hotbar.Container.Handle, wrench.ItemId, 1);
            StringAssert.Contains("in inventory", junction.Interact(actor).Message);
            session.Commands.Grant(inventory.Container.Handle, battery.ItemId, 2);
            bool observerSawPresentation = false;
            inventory.Container.SlotChanged += _ => observerSawPresentation = system.IsRestored && gate.AllowsAccess;
            Assert.IsTrue(junction.Interact(actor).IsSuccess); Assert.IsTrue(observerSawPresentation);
            Assert.IsTrue(gate.AllowsAccess); Assert.IsFalse(doorHost.GetComponent<SolitudeSlidingDoor>().IsOpen);
            Assert.AreEqual(1, inventory.Container.GetSlot(0).Stack.Quantity);
            session.Commands.Remove(hotbar.Container.GetSlot(0).Address, 1);
            StringAssert.Contains("already restored", junction.Interact(actor).Message);
        }
        [Test] public void ChangedRequirementAfterPromptRejectsAndUnownedActorCannotPay()
        {
            session.Commands.Grant(hotbar.Container.Handle, wrench.ItemId, 1); session.Commands.Grant(inventory.Container.Handle, battery.ItemId, 1);
            junction.GetPrompt(); session.Commands.Remove(hotbar.Container.GetSlot(0).Address, 1);
            Assert.IsFalse(junction.Interact(actor).IsSuccess); Assert.IsFalse(junction.Interact(null).IsSuccess);
            Assert.AreEqual(1, inventory.Container.GetSlot(0).Stack.Quantity); Assert.IsFalse(gate.AllowsAccess);
        }
        [Test] public void RebindingLoadedFactReconcilesBeforeReturningAndReleaseClearsAccess()
        {
            session.Commands.Grant(hotbar.Container.Handle, wrench.ItemId, 1); session.Commands.Grant(inventory.Container.Handle, battery.ItemId, 1);
            Assert.IsTrue(junction.Interact(actor).IsSuccess); composition.Release(); Assert.IsFalse(gate.AllowsAccess);
            Assert.IsFalse(junction.Interact(actor).IsSuccess); composition.Initialize(context);
            Assert.IsTrue(system.IsRestored); Assert.IsTrue(gate.AllowsAccess); Assert.AreEqual("System restored", junction.GetPrompt());
        }
        [Test] public void DisabledAndMissingPresentationRejectWithoutConsumption()
        {
            session.Commands.Grant(hotbar.Container.Handle, wrench.ItemId, 1); session.Commands.Grant(inventory.Container.Handle, battery.ItemId, 1);
            system.enabled = false; Assert.IsFalse(junction.Interact(actor).IsSuccess); Assert.IsFalse(gate.AllowsAccess);
            system.enabled = true; composition.Release(); Assert.IsFalse(junction.Interact(actor).IsSuccess); Assert.AreEqual(1, inventory.Container.GetSlot(0).Stack.Quantity);
        }
        [Test] public void DuplicateLiveBindingCannotOverwriteProgression()
        {
            var definition = AssetDatabase.LoadAssetAtPath<MechanicalRepairDefinition>(DefinitionPath);
            Assert.Throws<InvalidOperationException>(() => session.BindMechanicalRepair(definition.Compile("verification.auxiliary.socket", context.Capabilities.Registry)));
            Assert.IsFalse(system.IsRestored); Assert.IsFalse(gate.AllowsAccess);
        }
    }
}
