using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using SOLITUDE.Containers;
using SOLITUDE.Items;
using SOLITUDE.SaveLoad;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SOLITUDE.Tests
{
    public class PickupPersistenceTests
    {
        private ItemDefinition item;
        private Container inventory;
        private ContainerSaveSession session;
        private PickupCollectionService service;
        private IDisposable inventoryBinding;

        [SetUp]
        public void SetUp()
        {
            item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.Stackable = true;
            item.MaxStackSize = 10;
            SetField(item, "itemId", "test.item");
            session = new ContainerSaveSession(new SaveGameData(), Catalog());
            var binding = session.Register(ContainerSaveSession.PlayerInventoryId, 2);
            inventory = binding.Container;
            inventoryBinding = binding;
            service = new PickupCollectionService(session);
        }

        [TearDown]
        public void TearDown()
        {
            inventoryBinding?.Dispose();
            if (item != null) UnityEngine.Object.DestroyImmediate(item);
        }

        private static ItemCatalog Catalog() => new ItemCatalog(new[] { new ItemSpec("test.item", true, 10) });
        private CollectResult Collect(string id, int quantity = 1) =>
            service.TryCollect(id, ContainerSaveSession.PlayerInventoryId, "test.item", quantity);
        private static void SetField(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);

        [Test]
        public void CollectionRoundTripPreservesInventoryAndFact()
        {
            service.Register("pickup.a", new PickupReward("test.item", 2));
            Assert.AreEqual(CollectResult.Collected, Collect("pickup.a", 2));
            var data = JsonUtility.FromJson<SaveGameData>(JsonUtility.ToJson(session.Capture()));
            var restored = new ContainerSaveSession(data, Catalog());
            var restoredInventory = restored.Register(ContainerSaveSession.PlayerInventoryId, 2).Container;
            var restoredService = new PickupCollectionService(restored);
            restoredService.Register("pickup.a", new PickupReward("test.item", 2));
            Assert.AreEqual(CollectResult.AlreadyCollected,
                restoredService.TryCollect("pickup.a", ContainerSaveSession.PlayerInventoryId, "test.item", 2));
            Assert.AreEqual(2, restoredInventory.GetSlot(0).Stack.Quantity);
        }

        [Test]
        public void PartialCapacityDoesNotMutateAnySlotOrFact()
        {
            session.Commands.Grant(inventory.Handle, item.ItemId, 19);
            var stack = inventory.GetSlot(1).Stack;
            service.Register("pickup.a", new PickupReward("test.item", 2));
            session.MarkSaved();
            Assert.AreEqual(CollectResult.InventoryFull, Collect("pickup.a", 2));
            Assert.AreSame(stack, inventory.GetSlot(1).Stack);
            Assert.AreEqual(9, stack.Quantity);
            Assert.IsFalse(service.IsCollected("pickup.a"));
            Assert.IsFalse(session.IsDirty);
        }

        [Test]
        public void ReentrantCollectionAndCaptureCannotObservePartialTransaction()
        {
            service.Register("pickup.a", new PickupReward("test.item", 12));
            int notifications = 0;
            var observerErrors = new System.Collections.Generic.List<Exception>(); session.Commands.ObserverError += observerErrors.Add;
            inventory.SlotChanged += _ =>
            {
                notifications++;
                Assert.AreEqual(2, inventory.GetSlot(1).Stack.Quantity);
                Assert.IsTrue(service.IsCollected("pickup.a"));
                Assert.AreEqual(CollectResult.NotReady, Collect("pickup.a", 12));
                Assert.Throws<InvalidOperationException>(() => session.Capture());
            };
            Assert.AreEqual(CollectResult.Collected, Collect("pickup.a", 12));
            Assert.AreEqual(2, notifications); Assert.IsEmpty(observerErrors);
            Assert.AreEqual(CollectResult.AlreadyCollected, Collect("pickup.a", 12));
        }

        [Test]
        public void InstancesSharingRewardCollectIndependently()
        {
            service.Register("pickup.a", new PickupReward("test.item", 1));
            service.Register("pickup.b", new PickupReward("test.item", 1));
            Assert.AreEqual(CollectResult.Collected, Collect("pickup.a"));
            Assert.AreEqual(CollectResult.Collected, Collect("pickup.b"));
            Assert.AreEqual(2, inventory.GetSlot(0).Stack.Quantity);
        }

        [Test]
        public void DuplicateOrMissingIdentityCannotGrantReward()
        {
            Assert.Throws<ArgumentException>(() => service.Register("", new PickupReward("test.item", 1)));
            service.Register("pickup.a", new PickupReward("test.item", 1));
            Assert.Throws<InvalidOperationException>(() => service.Register("pickup.a", new PickupReward("test.item", 1)));
            Assert.AreEqual(CollectResult.InvalidPickup, Collect("pickup.a"));
            Assert.AreEqual(CollectResult.InvalidPickup, Collect("missing"));
            Assert.IsTrue(inventory.IsEmpty(0));
        }

        [Test]
        public void AlteredRewardAndUnknownItemAreRejected()
        {
            service.Register("pickup.a", new PickupReward("test.item", 1));
            Assert.AreEqual(CollectResult.InvalidPickup, Collect("pickup.a", 2));
            service.Register("pickup.b", new PickupReward("unknown", 1));
            Assert.AreEqual(CollectResult.InvalidPickup,
                service.TryCollect("pickup.b", ContainerSaveSession.PlayerInventoryId, "unknown", 1));
            Assert.IsFalse(service.IsCollected("pickup.a"));
        }

        [Test]
        public void UnregisteredInventoryCannotReceivePersistentPickup()
        {
            service.Register("pickup.a", new PickupReward("test.item", 1));
            inventoryBinding.Dispose();
            Assert.AreEqual(CollectResult.NotReady, Collect("pickup.a"));
            Assert.IsFalse(service.IsCollected("pickup.a"));
        }

        [Test]
        public void SceneUnregistrationRetainsCollectionFact()
        {
            var binding = service.Register("pickup.a", new PickupReward("test.item", 1));
            Collect("pickup.a");
            binding.Dispose();
            inventoryBinding.Dispose();
            Assert.Contains("pickup.a", session.Capture().collectedPickupIds);
            service.Register("pickup.a", new PickupReward("test.item", 1));
            inventoryBinding = session.Register(ContainerSaveSession.PlayerInventoryId, 2);
            Assert.AreEqual(CollectResult.AlreadyCollected, Collect("pickup.a"));
        }

        [Test]
        public void SceneScopeReleasesBindingsForNeverActivePickups()
        {
            var hostObject = new GameObject("TestSaveComposition");
            var pickupObject = new GameObject("NeverActivePickup");
            var config = ScriptableObject.CreateInstance<PickupDefinition>();
            var database = ScriptableObject.CreateInstance<ItemDatabase>();
            SetField(database, "items", new System.Collections.Generic.List<ItemDefinition> { item });
            SetField(config, "item", item);
            pickupObject.SetActive(false);
            var view = pickupObject.AddComponent<WorldPickup>();
            SetField(view, "definition", config);
            SetField(pickupObject.GetComponent<SaveableId>(), "id", "inactive.pickup");
            var scope = hostObject.AddComponent<SOLITUDE.Composition.SceneBindings>();
            SetField(scope, "worldPickups", new[] { view });
            var pause = new SOLITUDE.Application.PauseCoordinator();
            var policy = new SOLITUDE.Application.InputPolicyCoordinator(pause);
            var gestures = new SOLITUDE.Application.ContainerGestureCoordinator(new ContainerInteractionController(session.Commands), policy);
            try
            {
                scope.Initialize(session, service, database.BuildPresentationCatalog(), new System.Collections.Generic.Dictionary<LootTable, RuntimeLootTable>(), gestures,
                    policy, pause, null, null, null, null);
                Assert.IsTrue(service.IsValid("inactive.pickup"));
                scope.Release();
                Assert.IsFalse(service.IsValid("inactive.pickup"));
                Assert.DoesNotThrow(() => service.Register("inactive.pickup", new PickupReward("test.item", 1)).Dispose());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(pickupObject);
                UnityEngine.Object.DestroyImmediate(hostObject);
                UnityEngine.Object.DestroyImmediate(config);
                UnityEngine.Object.DestroyImmediate(database);
                policy.Dispose();
            }
        }

        [Test]
        public void ResetClearsInventoryAndCollectionFacts()
        {
            service.Register("pickup.a", new PickupReward("test.item", 1));
            Collect("pickup.a");
            session.Reset(456);
            Assert.IsTrue(inventory.IsEmpty(0));
            Assert.IsFalse(service.IsCollected("pickup.a"));
            Assert.AreEqual(CollectResult.Collected, Collect("pickup.a"));
        }

        [Test]
        public void VersionOneMigrationPreservesExistingWorldAndContainers()
        {
            session.Commands.Grant(inventory.Handle, item.ItemId, 3);
            var data = session.Capture();
            data.version = 1;
            data.worldSeed = 123;
            data.collectedPickupIds = null;
            var migrated = SaveGameData.Migrate(data);
            Assert.AreEqual(2, migrated.version);
            Assert.AreEqual(123, migrated.worldSeed);
            Assert.AreEqual(3, migrated.containers[0].state.slots[0].quantity);
            Assert.IsEmpty(migrated.collectedPickupIds);
        }

        [Test]
        public void LoadedFactsAreValidatedDeduplicatedAndSorted()
        {
            var data = new SaveGameData();
            data.collectedPickupIds.AddRange(new[] { "z", "a", "z" });
            CollectionAssert.AreEqual(new[] { "a", "z" }, SaveGameData.Migrate(data).collectedPickupIds);
            data.collectedPickupIds.Add(" ");
            Assert.Throws<InvalidDataException>(() => SaveGameData.Migrate(data));
            Assert.Throws<InvalidDataException>(() => SaveGameData.Migrate(new SaveGameData { version = 99 }));
        }

        [Test]
        public void FailedWriteKeepsOldSnapshotAndDirtyStateForCompleteRetry()
        {
            string directory = Path.Combine(Path.GetTempPath(), "solitude-pickup-" + Guid.NewGuid());
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "save.json");
            try
            {
                string previous = JsonUtility.ToJson(session.Capture());
                SaveFileStore.Write(path, previous);
                session.MarkSaved();
                service.Register("pickup.a", new PickupReward("test.item", 1));
                Collect("pickup.a");
                Directory.CreateDirectory(path + ".tmp");
                Assert.Throws<UnauthorizedAccessException>(() => SaveFileStore.Write(path, JsonUtility.ToJson(session.Capture())));
                Assert.AreEqual(previous, File.ReadAllText(path));
                Assert.IsTrue(session.IsDirty);
                Directory.Delete(path + ".tmp");
                SaveFileStore.Write(path, JsonUtility.ToJson(session.Capture()));
                session.MarkSaved();
                var saved = JsonUtility.FromJson<SaveGameData>(File.ReadAllText(path));
                Assert.Contains("pickup.a", saved.collectedPickupIds);
                Assert.AreEqual(1, saved.containers[0].state.slots[0].quantity);
            }
            finally { Directory.Delete(directory, true); }
        }

        [UnityTest]
        public IEnumerator WorldPresentationStaysCollectedAfterSceneUnloadAndReload()
        {
            // Run against an empty test world; never touch the player's save host.
            if (!UnityEngine.Application.isPlaying)
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            yield return new EnterPlayMode();
            yield return VerifyWorldReload();
            yield return new ExitPlayMode();
        }

        private sealed class ReaderSource : IContainerSource
        {
            public string Label => "Test";
            public IContainerReader Container { get; }
            public ReaderSource(IContainerReader container) => Container = container;
        }
        private static IEnumerator VerifyResizedViews(ContainerSaveSession runtimeSession, Container model, Scene scene)
        {
            var root = new GameObject("ResizeViews"); SceneManager.MoveGameObjectToScene(root, scene);
            var templateObject = new GameObject("SlotTemplate"); templateObject.transform.SetParent(root.transform);
            var template = templateObject.AddComponent<ContainerSlotView>(); templateObject.SetActive(false);
            var parent = new GameObject("Slots"); parent.transform.SetParent(root.transform);
            var view = root.AddComponent<ContainerView>();
            SetField(view, "slotViewPrefab", template); SetField(view, "slotsParent", parent.transform);
            var pause = new SOLITUDE.Application.PauseCoordinator();
            var policy = new SOLITUDE.Application.InputPolicyCoordinator(pause); policy.SetReady(true);
            var gestures = new SOLITUDE.Application.ContainerGestureCoordinator(new ContainerInteractionController(runtimeSession.Commands), policy);
            var presenter = new SOLITUDE.Application.ContainerPresenter(model, view, "Test", gestures, policy);
            Assert.AreEqual(2, parent.GetComponentsInChildren<ContainerSlotView>(true).Length);
            runtimeSession.Commands.Resize(model.Handle, 4); yield return null;
            Assert.AreEqual(4, parent.GetComponentsInChildren<ContainerSlotView>(true).Length);
            runtimeSession.Commands.Resize(model.Handle, 1); yield return null;
            Assert.AreEqual(1, parent.GetComponentsInChildren<ContainerSlotView>(true).Length);
            presenter.Dispose(); yield return null;
            var authored = new GameObject("AuthoredSlot"); authored.transform.SetParent(parent.transform);
            var authoredView = authored.AddComponent<ContainerSlotView>();
            SetField(view, "useExistingSlotViews", true);
            SetField(view, "existingSlotViews", new System.Collections.Generic.List<ContainerSlotView> { authoredView });
            presenter = new SOLITUDE.Application.ContainerPresenter(model, view, "Test", gestures, policy);
            runtimeSession.Commands.Resize(model.Handle, 3); yield return null;
            Assert.AreEqual(3, parent.GetComponentsInChildren<ContainerSlotView>(true).Length);
            runtimeSession.Commands.Resize(model.Handle, 1); yield return null;
            Assert.AreEqual(1, parent.GetComponentsInChildren<ContainerSlotView>(true).Length);
            presenter.Dispose(); policy.Dispose(); Assert.IsNotNull(authoredView); UnityEngine.Object.Destroy(root);
        }

        private static IEnumerator VerifyWorldReload()
        {
            var runtimeItem = ScriptableObject.CreateInstance<ItemDefinition>();
            SetField(runtimeItem, "itemId", "runtime.item");
            var config = ScriptableObject.CreateInstance<PickupDefinition>();
            SetField(config, "item", runtimeItem);
            SetField(config, "quantity", 1);
            var catalog = new ItemCatalog(new[] { new ItemSpec("runtime.item", true, 99) });
            var runtimeSession = new ContainerSaveSession(new SaveGameData(), catalog);
            var model = runtimeSession.Register(ContainerSaveSession.PlayerInventoryId, 2).Container;
            var runtimeService = new PickupCollectionService(runtimeSession);
            var scene = SceneManager.CreateScene("PickupTestWorld");
            var instance = new GameObject("GenericPickup");
            SceneManager.MoveGameObjectToScene(instance, scene);
            var view = instance.AddComponent<WorldPickup>();
            SetField(view, "definition", config);
            instance.GetComponent<SaveableId>().AssignRuntimeIdentity("world.pickup");
            view.Initialize(runtimeService);
            Assert.IsTrue(view.Interact(null).IsSuccess);
            Assert.IsFalse(instance.activeSelf);
            var json = JsonUtility.ToJson(runtimeSession.Capture());
            yield return SceneManager.UnloadSceneAsync(scene);
            runtimeSession = new ContainerSaveSession(JsonUtility.FromJson<SaveGameData>(json), catalog);
            model = runtimeSession.Register(ContainerSaveSession.PlayerInventoryId, 2).Container;
            runtimeService = new PickupCollectionService(runtimeSession);
            scene = SceneManager.CreateScene("PickupTestWorldReloaded");
            instance = new GameObject("GenericPickupReloaded");
            SceneManager.MoveGameObjectToScene(instance, scene);
            view = instance.AddComponent<WorldPickup>();
            SetField(view, "definition", config);
            instance.GetComponent<SaveableId>().AssignRuntimeIdentity("world.pickup");
            view.Initialize(runtimeService);
            Assert.IsFalse(instance.activeSelf);
            Assert.IsFalse(view.CanInteract(null));
            Assert.AreEqual(1, model.GetSlot(0).Stack.Quantity);
            yield return VerifyResizedViews(runtimeSession, model, scene);
            yield return SceneManager.UnloadSceneAsync(scene);
            UnityEngine.Object.Destroy(config);
            UnityEngine.Object.Destroy(runtimeItem);
        }
    }
}
