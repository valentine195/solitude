#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SOLITUDE.Combat;
using SOLITUDE.Composition;
using SOLITUDE.Containers;
using SOLITUDE.Core.Animations;
using SOLITUDE.Features.Interactables;
using SOLITUDE.Items;
using SOLITUDE.Player;
using SOLITUDE.SaveLoad;
using SOLITUDE.Vitals;
using SOLITUDE.UI.Exploration;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SOLITUDE.Tests
{
    public sealed class CarriedEquipmentJourneyTests
    {
        private const string Demo = "Assets/_Project/Scenes/Demos/AtmosphericScrubberEncounter.unity";
        private const string Tool = "test.carried-tool";
        private const string Medical = "test.carried-medical";

        private static T Field<T>(object target, string name) =>
            (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);

        private static void SetId(ItemDefinition item, string id) =>
            typeof(ItemDefinition).GetField("itemId", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(item, id);

        private static void Queue<T>(InputDevice device, T state) where T : struct, IInputStateTypeInfo
        {
            InputSystem.QueueStateEvent(device, state);
            InputSystem.Update();
        }

        private static Vector2 SlotPoint(Component slot)
        {
            var rect = (RectTransform)slot.transform;
            return RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        }

        private static IEnumerator Drag(Mouse mouse, Component source, Vector2 destination)
        {
            Canvas.ForceUpdateCanvases();
            var start = SlotPoint(source);
            Queue(mouse, new MouseState { position = start });
            yield return null;
            Queue(mouse, new MouseState { position = start }.WithButton(MouseButton.Left));
            yield return null;
            var delta = destination - start;
            Queue(mouse, new MouseState { position = destination, delta = delta }.WithButton(MouseButton.Left));
            yield return null;
            Queue(mouse, new MouseState { position = destination });
            yield return null;
        }

        [UnityTest]
        public IEnumerator WrenchAndBatonShareTheSwingAndFollowTheActiveHand()
        {
            yield return WeaponSystemTestFixtures.Unload();
            const string cabin = "Assets/_Project/Art/SOLITUDE/ReferenceScenes/IllustratedCabin.unity";
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(cabin, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            try
            {
                var root = WeaponSystemTestFixtures.Root;
                Assert.AreEqual(RuntimeStartupState.Ready, root.State);
                Assert.IsTrue(root.UsesDisposableSession);
                var equipment = Field<CarriedEquipment>(WeaponSystemTestFixtures.One<SceneBindings>(), "equipment");
                var inventory = WeaponSystemTestFixtures.One<PlayerInventory>().Container;
                var actions = WeaponSystemTestFixtures.Actions;
                var view = actions.GetComponent<MeleeSwingView>();
                var actor = actions.GetComponent<SpriteRenderer>();
                var animator = actions.GetComponent<Animator>();
                var animation = actions.GetComponent<HumanAnimationController>();
                var movement = actions.GetComponent<SOLITUDE.Movement.MovementBehavior>();
                var wrench = WeaponSystemTestFixtures.Wrench;
                var baton = AssetDatabase.LoadAssetAtPath<ItemDefinition>(
                    "Assets/_Project/Features/Items/Weapons/ElectricBaton/ElectricBaton.asset");
                Assert.AreNotSame(wrench.Actions[0].Action, baton.Actions[0].Action);
                Assert.AreSame(wrench.HeldSprite, view.HeldRenderer.sprite);

                var pickup = Object.FindObjectsByType<WorldPickup>(FindObjectsSortMode.None)
                    .Single(candidate => candidate.Definition.Item == baton);
                Assert.IsTrue(pickup.Interact(WeaponSystemTestFixtures.One<PlayerInteractor>()).IsSuccess);
                Assert.IsFalse(pickup.gameObject.activeSelf);
                Assert.IsTrue(equipment.Unequip(PlayerEquipmentSlot.Weapon).Succeeded);
                Assert.IsTrue(equipment.Equip(inventory.GetSlots().Single(slot => slot.Stack?.ItemId == wrench.ItemId).Address,
                    PlayerEquipmentSlot.Tool).Succeeded);
                Assert.IsTrue(equipment.Equip(inventory.GetSlots().Single(slot => slot.Stack?.ItemId == baton.ItemId).Address,
                    PlayerEquipmentSlot.Weapon).Succeeded);
                if (equipment.State.activeSlot == PlayerEquipmentSlot.Tool) equipment.Switch();

                Time.timeScale = 0;
                movement.SetVector(Vector2.right);
                animation.SetMovement(Vector2.right);
                animator.Play("Base Layer.Walk_east", 0, 0);
                animator.Update(0);
                yield return null;
                Assert.IsTrue(view.HeldRenderer.enabled);
                Assert.AreSame(baton.HeldSprite, view.HeldRenderer.sprite);
                var grip = actions.transform.Find("ForegroundGrip").GetComponent<SpriteRenderer>();
                Assert.IsTrue(grip.enabled);
                Assert.Greater(grip.sortingOrder, view.HeldRenderer.sortingOrder);
                Assert.AreEqual("grip_" + actor.sprite.name, grip.sprite.name);
                var firstWalkSprite = actor.sprite;
                var firstGrip = view.HeldRenderer.transform.localPosition;
                animator.Update(.15f);
                yield return null;
                Assert.AreNotSame(firstWalkSprite, actor.sprite);
                Assert.AreNotEqual(firstGrip, view.HeldRenderer.transform.localPosition);
                Assert.AreEqual("grip_" + actor.sprite.name, grip.sprite.name);

                Assert.IsTrue(actions.Request());
                var body = actions.transform.Find("MeleeBodyAnimation").GetComponent<SpriteRenderer>();
                Assert.AreEqual("swing_east_windup", body.sprite.name);
                Assert.AreSame(baton.HeldSprite, view.HeldRenderer.sprite);
                actions.Advance(.11);
                var strike = body.sprite;
                Assert.AreEqual("swing_east_strike", strike.name);
                Assert.AreEqual("grip_" + strike.name, grip.sprite.name);
                Assert.Less(Vector2.Angle(view.HeldRenderer.transform.up, actor.transform.right), 15f,
                    "The east strike should extend the handle along the outstretched forearm.");
                equipment.Switch();
                Assert.IsFalse(actions.Runner.IsExecuting);
                Assert.IsFalse(body.enabled);
                Assert.IsFalse(actor.forceRenderingOff);
                Assert.IsTrue(view.HeldRenderer.enabled);
                Assert.AreSame(wrench.HeldSprite, view.HeldRenderer.sprite);
                Assert.AreEqual("grip_" + actor.sprite.name, grip.sprite.name);

                actions.Advance(.55);
                Assert.IsTrue(actions.Request());
                actions.Advance(.13);
                Assert.AreSame(strike, body.sprite);
                var wrenchAction = (SOLITUDE.Items.MeleeAttackAction)wrench.Actions[0].Action;
                Assert.AreSame(wrenchAction.Configuration.PosedWeaponFrames[13], view.HeldRenderer.sprite);
                Assert.AreEqual("wrench_east_strike", view.HeldRenderer.sprite.name);
                Assert.IsFalse(grip.enabled, "The registered wrench insert already follows the finger contour.");
                actions.Advance(.55);
                Assert.IsFalse(body.enabled);
                Assert.IsTrue(view.HeldRenderer.enabled);
                Assert.AreSame(wrench.HeldSprite, view.HeldRenderer.sprite);

                animation.SetMovement(Vector2.up);
                animator.Play("Base Layer.Walk_north", 0, 0);
                animator.Update(0);
                yield return null;
                Assert.IsTrue(grip.enabled);
                Assert.AreEqual("grip_walk_north_0", grip.sprite.name);
                Assert.Greater(grip.sortingOrder, actor.sortingOrder);
                Assert.Less(view.HeldRenderer.sortingOrder, actor.sortingOrder);
                animator.Update(.15f);
                yield return null;
                Assert.IsFalse(grip.enabled);

                animation.SetMovement(Vector2.down);
                animation.SetMovement(Vector2.zero);
                animator.Play("Base Layer.Idle_south", 0, 0);
                animator.Update(0);
                yield return null;
                Assert.AreEqual("solitude_colonist_idle_south_equipped", actor.sprite.name);
                Assert.AreEqual("grip_idle_south", grip.sprite.name);
                Assert.Greater(grip.sortingOrder, view.HeldRenderer.sortingOrder);

                foreach (var (direction, name) in new[]
                {
                    (Vector2.up, "north"), (Vector2.left, "west"),
                    (Vector2.right, "east"), (Vector2.down, "south")
                })
                {
                    foreach (var weapon in new[] { wrench, baton })
                    {
                        if (view.HeldRenderer.sprite != weapon.HeldSprite) equipment.Switch();
                        movement.SetVector(direction);
                        movement.SetVector(Vector2.zero);
                        animation.SetMovement(direction);
                        animation.SetMovement(Vector2.zero);
                        animator.Play("Base Layer.Idle_" + name, 0, 0);
                        animator.Update(0);
                        yield return null;
                        var idleSprite = actor.sprite;
                        var idlePosition = view.HeldRenderer.transform.localPosition;
                        var idleRotation = view.HeldRenderer.transform.localRotation;
                        var idleScale = view.HeldRenderer.transform.localScale;
                        var idleOrder = view.HeldRenderer.sortingOrder;
                        var idleGrip = grip.enabled ? grip.sprite : null;
                        Assert.IsTrue(actions.Request());
                        actions.Advance(.40);
                        Assert.IsTrue(body.enabled);
                        Assert.AreSame(idleSprite, body.sprite, name);
                        Assert.AreSame(weapon.HeldSprite, view.HeldRenderer.sprite);
                        Assert.AreEqual(idlePosition, view.HeldRenderer.transform.localPosition);
                        Assert.AreEqual(idleRotation, view.HeldRenderer.transform.localRotation);
                        Assert.AreEqual(idleScale, view.HeldRenderer.transform.localScale);
                        Assert.AreEqual(idleOrder, view.HeldRenderer.sortingOrder);
                        Assert.AreSame(idleGrip, grip.enabled ? grip.sprite : null);
                        actions.Advance(.20);
                        Assert.IsFalse(body.enabled);
                        Assert.AreSame(idleSprite, actor.sprite);
                        Assert.AreEqual(idlePosition, view.HeldRenderer.transform.localPosition);
                    }
                }

                Assert.IsTrue(equipment.Unequip(PlayerEquipmentSlot.Weapon).Succeeded);
                Assert.IsTrue(equipment.Unequip(PlayerEquipmentSlot.Tool).Succeeded);
                Assert.IsFalse(grip.enabled);
                Assert.IsFalse(view.HeldRenderer.enabled);
                Assert.AreEqual("idle_south", actor.sprite.name);
            }
            finally
            {
                Time.timeScale = 1;
                var root = Object.FindFirstObjectByType<GameCompositionRoot>();
                if (root != null) Object.Destroy(root.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator CrewLockerUsesInventoryTransferAndClearsStoredWeapon()
        {
            var oldRoot = Object.FindFirstObjectByType<GameCompositionRoot>();
            if (oldRoot != null) { Object.Destroy(oldRoot.gameObject); yield return null; }
            var background = InputSystem.settings.backgroundBehavior;
            var editor = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var pad = InputSystem.AddDevice<Gamepad>();
            var mouse = InputSystem.AddDevice<Mouse>();
            try
            {
                yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Demo, new LoadSceneParameters(LoadSceneMode.Single));
                yield return null;
                var root = Object.FindFirstObjectByType<GameCompositionRoot>();
                Assert.AreEqual(RuntimeStartupState.Ready, root.State);
                Assert.IsTrue(root.UsesDisposableSession);
                var scope = Object.FindFirstObjectByType<SceneBindings>();
                var equipment = Field<CarriedEquipment>(scope, "equipment");
                var commands = Field<ContainerSaveSession>(root, "session").Commands;
                var inventory = Object.FindFirstObjectByType<PlayerInventory>().Container;
                string wrench = equipment.State.weaponItemId;
                Assert.IsTrue(inventory.GetSlot(0).IsEmpty);
                foreach (var name in new[] { "Bandage", "Battery", "ReplacementFuse" })
                {
                    var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>($"Assets/_Project/Features/Items/Definitions/{name}/{name}.asset");
                    Assert.IsTrue(commands.Grant(inventory.Handle, item.ItemId, 1).Succeeded);
                }
                var locker = Object.FindFirstObjectByType<LockerContainer>();
                var player = Object.FindFirstObjectByType<PlayerInteractor>().GetComponent<Rigidbody2D>();
                player.position = (Vector2)locker.transform.position + Vector2.down * .6f;
                Physics2D.SyncTransforms();
                yield return new WaitForFixedUpdate();
                yield return null;

                Queue(keyboard, new KeyboardState(Key.Tab));
                yield return null;
                Queue(keyboard, new KeyboardState());
                var fullScreen = Object.FindFirstObjectByType<InventoryScreenView>();
                var capacity = Field<TMPro.TMP_Text>(fullScreen, "capacity");
                string inventoryCapacity = capacity.text;
                Assert.AreEqual("3 / 24", inventoryCapacity);
                Assert.IsFalse(Field<GameObject>(fullScreen.GetComponent<ContainerView>().SlotViews[0], "equippedLabel").activeSelf);
                Queue(keyboard, new KeyboardState(Key.Escape));
                yield return null;
                Queue(keyboard, new KeyboardState());
                yield return null;

                Queue(keyboard, new KeyboardState(Key.E));
                yield return null;
                Queue(keyboard, new KeyboardState());
                yield return null;
                var screen = Object.FindFirstObjectByType<InventoryScreenView>();
                Assert.IsNotNull(screen);
                var slots = Field<ContainerView>(screen, "lockerView").SlotViews;
                Assert.AreSame(slots[0].gameObject, EventSystem.current.currentSelectedGameObject);
                var action = Field<Button>(screen, "transferAction");
                var popover = Field<RectTransform>(screen, "itemPopover");
                Assert.IsFalse(popover.gameObject.activeInHierarchy, "Selection alone leaves the grids unobscured.");
                var actionLabel = action.GetComponentInChildren<TMPro.TMP_Text>();
                var loot = locker.Container.GetSlot(0).Stack;
                var carriedSlots = screen.GetComponent<ContainerView>().SlotViews;
                var weaponCard = Field<Button>(screen, "weaponCard");
                Assert.AreEqual(inventoryCapacity, capacity.text);
                Assert.IsFalse(Field<GameObject>(carriedSlots[0], "equippedLabel").activeSelf);
                Assert.IsTrue(carriedSlots[0].GetComponent<Button>().interactable);
                for (int i = 0; i < 3; i++) Assert.IsTrue(commands.Remove(inventory.GetSlot(i).Address, 1).Succeeded);
                int carriedBefore = equipment.Quantity(loot.ItemId);
                yield return null;
                Assert.AreEqual("0 / 24", capacity.text);
                Assert.IsTrue(carriedSlots[0].GetComponent<Button>().interactable);
                Canvas.ForceUpdateCanvases();
                Assert.Less(SlotPoint(slots[0]).x, SlotPoint(carriedSlots[0]).x);
                Assert.Less(SlotPoint(carriedSlots[0]).x, SlotPoint(weaponCard).x);
                yield return Drag(mouse, slots[0], SlotPoint(carriedSlots[2]));
                Assert.AreEqual(loot.ItemId, inventory.GetSlot(2).Stack?.ItemId, "Mouse drag takes a stack into the chosen carried cell.");
                Assert.IsTrue(locker.Container.GetSlot(0).IsEmpty);
                yield return Drag(mouse, carriedSlots[2], SlotPoint(slots[0]));
                Assert.AreEqual(loot.ItemId, locker.Container.GetSlot(0).Stack?.ItemId, "Mouse drag also stores a carried stack.");
                Assert.IsTrue(inventory.GetSlot(2).IsEmpty);
                EventSystem.current.SetSelectedGameObject(slots[0].gameObject);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter));
                yield return null;
                Queue(keyboard, new KeyboardState());
                yield return null;
                Assert.AreSame(action.gameObject, EventSystem.current.currentSelectedGameObject);
                Assert.AreEqual("Take to Carried", actionLabel.text);
                Assert.IsTrue(popover.gameObject.activeInHierarchy);
                Queue(keyboard, new KeyboardState(Key.Escape));
                yield return null;
                Queue(keyboard, new KeyboardState());
                yield return null;
                Assert.IsFalse(popover.gameObject.activeInHierarchy);
                Assert.AreSame(slots[0].gameObject, EventSystem.current.currentSelectedGameObject);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter));
                yield return null;
                Queue(keyboard, new KeyboardState());
                yield return null;
                Assert.AreSame(action.gameObject, EventSystem.current.currentSelectedGameObject);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter));
                yield return null;
                Queue(keyboard, new KeyboardState());
                yield return null;
                Assert.AreEqual(carriedBefore + loot.Quantity, equipment.Quantity(loot.ItemId));
                Assert.IsTrue(locker.Container.GetSlot(0).IsEmpty);

                Assert.AreEqual(wrench, equipment.GetSlot(PlayerEquipmentSlot.Weapon).Stack.ItemId);
                Assert.AreEqual(0, equipment.Quantity(wrench));
                EventSystem.current.SetSelectedGameObject(carriedSlots[5].gameObject);
                InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.DpadRight));
                yield return null;
                Queue(pad, new GamepadState());
                yield return null;
                Assert.AreSame(weaponCard.gameObject, EventSystem.current.currentSelectedGameObject);
                Assert.AreEqual("1 / 24", capacity.text);
                InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.South));
                yield return null;
                Queue(pad, new GamepadState());
                yield return null;
                Assert.AreSame(action.gameObject, EventSystem.current.currentSelectedGameObject);
                Assert.AreEqual("Weapon  ·  In hand", Field<TMPro.TMP_Text>(screen, "metadata").text);
                Assert.AreEqual("Store equipped", actionLabel.text);
                InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.South));
                yield return null;
                Queue(pad, new GamepadState());
                yield return null;
                Assert.IsNull(equipment.State.weaponItemId);
                Assert.AreEqual(wrench, locker.Container.GetSlot(0).Stack.ItemId);
                Assert.AreEqual("1 / 24", capacity.text);
                Assert.IsFalse(Field<GameObject>(carriedSlots[0], "equippedLabel").activeSelf);

                yield return Drag(mouse, slots[0], SlotPoint(carriedSlots[2]));
                Assert.AreEqual(1, equipment.Equip(inventory.GetSlot(2).Address, PlayerEquipmentSlot.Weapon).Moved);
                yield return null;
                Assert.AreEqual(wrench, equipment.State.weaponItemId);
                yield return Drag(mouse, weaponCard, SlotPoint(slots[0]));
                Assert.IsNull(equipment.State.weaponItemId);
                Assert.AreEqual(wrench, locker.Container.GetSlot(0).Stack.ItemId);

                Queue(keyboard, new KeyboardState(Key.Escape));
                yield return null;
                Queue(keyboard, new KeyboardState());
                Assert.IsFalse(screen.gameObject.activeInHierarchy);
                Queue(keyboard, new KeyboardState(Key.Tab));
                yield return null;
                Queue(keyboard, new KeyboardState());
                yield return null;
                Assert.IsTrue(screen.gameObject.activeInHierarchy);
                Assert.IsFalse(Field<ContainerView>(screen, "lockerView").gameObject.activeInHierarchy);
                Assert.IsFalse(action.gameObject.activeInHierarchy);
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
                InputSystem.RemoveDevice(mouse);
                InputSystem.RemoveDevice(pad);
                InputSystem.settings.backgroundBehavior = background;
                InputSystem.settings.editorInputBehaviorInPlayMode = editor;
            }
        }

        [UnityTest]
        public IEnumerator ScrubberStartsWithWeaponAndSwitchAndUseReachIndependentActions()
        {
            var oldRoot = Object.FindFirstObjectByType<GameCompositionRoot>();
            if (oldRoot != null) { Object.Destroy(oldRoot.gameObject); yield return null; }
            var database = AssetDatabase.LoadAssetAtPath<GameRuntimeConfig>(
                "Assets/_Project/Features/World/ScrubberEncounter/Demo/ScrubberRuntimeConfig.asset").database;
            var items = Field<List<ItemDefinition>>(database, "items");
            var wrench = AssetDatabase.LoadAssetAtPath<ItemDefinition>(
                "Assets/_Project/Features/Items/Weapons/MaintenanceWrench/MaintenanceWrench.asset");
            var tool = Object.Instantiate(wrench);
            tool.DisplayName = "Test tool"; SetId(tool, Tool);
            var medical = ScriptableObject.CreateInstance<ItemDefinition>();
            medical.DisplayName = "Test medical"; medical.ConsumableKind = VitalConsumableKind.Medical;
            medical.RestorationAmount = 10; SetId(medical, Medical);
            items.Add(tool); items.Add(medical);
            var background = InputSystem.settings.backgroundBehavior;
            var editor = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var pad = InputSystem.AddDevice<Gamepad>();
            var mouse = InputSystem.AddDevice<Mouse>();
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Demo, new LoadSceneParameters(LoadSceneMode.Single));
                yield return null;
                var root = Object.FindFirstObjectByType<GameCompositionRoot>();
                Assert.AreEqual(RuntimeStartupState.Ready, root.State);
                Assert.IsTrue(root.UsesDisposableSession);
                var session = Field<ContainerSaveSession>(root, "session");
                var scope = Object.FindFirstObjectByType<SceneBindings>();
                var equipment = Field<CarriedEquipment>(scope, "equipment");
                var inventory = Object.FindFirstObjectByType<PlayerInventory>().Container;
                Assert.AreEqual(wrench.ItemId, equipment.State.weaponItemId);
                Assert.IsTrue(inventory.GetSlot(0).IsEmpty);
                Assert.IsFalse(GameObject.Find("PersistentHotbar").transform.Find("Tray").gameObject.activeInHierarchy);
                Assert.IsTrue(session.Commands.Grant(inventory.Handle, Tool, 1).Succeeded);
                Assert.IsTrue(session.Commands.Transfer(inventory.GetSlot(0).Address, inventory.GetSlot(1).Address).Succeeded);
                Assert.IsTrue(session.Commands.Grant(inventory.Handle, Medical, 1).Succeeded);
                Assert.IsTrue(session.Commands.Transfer(inventory.GetSlot(0).Address, inventory.GetSlot(2).Address).Succeeded);

                Queue(keyboard, new KeyboardState(Key.Tab));
                yield return null;
                Queue(keyboard, new KeyboardState());
                var screen = Object.FindFirstObjectByType<InventoryScreenView>();
                Assert.IsNotNull(screen);
                var slots = screen.GetComponent<ContainerView>().SlotViews;
                var weaponCard = Field<Button>(screen, "weaponCard");
                var equipWeapon = Field<Button>(screen, "equipButton");
                Assert.IsTrue(weaponCard.IsInteractable());
                Assert.IsFalse(Field<Image>(slots[0], "icon").enabled, "The equipped copy is visible only in Equipment.");
                Canvas.ForceUpdateCanvases();
                yield return Drag(mouse, weaponCard, SlotPoint(slots[5]));
                Assert.IsNull(equipment.State.weaponItemId, "Dragging the wrench to Carried unequips it.");
                Assert.AreEqual(wrench.ItemId, inventory.GetSlot(5).Stack?.ItemId);
                yield return Drag(mouse, slots[5], new Vector2(2, 2));
                Assert.AreEqual(wrench.ItemId, inventory.GetSlot(5).Stack?.ItemId, "Releasing outside the inventory leaves the wrench in place.");
                Assert.IsFalse(screen.IsHandling);
                yield return Drag(mouse, slots[5], SlotPoint(slots[0]));
                Assert.AreEqual(wrench.ItemId, inventory.GetSlot(0).Stack?.ItemId, "Dragging between carried slots moves the wrench.");
                yield return Drag(mouse, slots[0], SlotPoint(weaponCard));
                Assert.AreEqual(wrench.ItemId, equipment.State.weaponItemId, "Dropping onto Weapon equips it again.");
                weaponCard.onClick.Invoke();
                Assert.AreEqual("Unequip weapon", equipWeapon.GetComponentInChildren<TMPro.TMP_Text>().text);
                equipWeapon.onClick.Invoke();
                Assert.IsNull(equipment.State.weaponItemId);
                Assert.IsTrue(Field<Image>(slots[0], "icon").enabled, "Unequip returns the copy to Carried.");
                equipWeapon.onClick.Invoke();
                Assert.AreEqual(wrench.ItemId, equipment.State.weaponItemId);
                Assert.IsFalse(Field<Image>(slots[0], "icon").enabled);
                Assert.IsTrue(session.Commands.Grant(inventory.Handle, wrench.ItemId, 1).Succeeded);
                Assert.IsTrue(Field<Image>(slots[0], "icon").enabled, "A spare copy still appears in Carried.");
                EventSystem.current.SetSelectedGameObject(slots[1].gameObject);
                var equipTool = Field<Button>(screen, "assignButton");
                Assert.IsTrue(equipTool.gameObject.activeInHierarchy);
                equipTool.onClick.Invoke();
                Assert.AreEqual(Tool, equipment.State.toolItemId);
                EventSystem.current.SetSelectedGameObject(slots[2].gameObject);
                var pin = Field<Button>(screen, "useButton");
                Assert.IsTrue(pin.gameObject.activeInHierarchy);
                pin.onClick.Invoke();
                Assert.AreEqual(Medical, equipment.State.pinnedItemId);
                var quickUse = Field<ExplorationHudView.QuickUseSlot[]>(screen, "inventoryQuickUseSlots");
                Assert.AreEqual(5, quickUse.Length);
                Canvas.ForceUpdateCanvases();
                yield return Drag(mouse, slots[2], SlotPoint(weaponCard));
                Assert.AreEqual("Cannot equip Test medical as a weapon.", Field<TMPro.TMP_Text>(screen, "context").text);
                Assert.AreEqual(1, equipment.Quantity(Medical));
                Assert.AreEqual(wrench.ItemId, equipment.State.weaponItemId);

                EventSystem.current.SetSelectedGameObject(slots[20].gameObject);
                InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.DpadDown));
                yield return null;
                Assert.AreSame(quickUse[2].Button.gameObject, EventSystem.current.currentSelectedGameObject);
                Assert.IsTrue(quickUse[2].Button.transform.Find("FocusCorners").gameObject.activeSelf);
                Assert.IsFalse(quickUse[0].Button.transform.Find("FocusCorners").gameObject.activeSelf);
                Assert.AreEqual(0, equipment.SelectedQuickUse);
                Queue(pad, new GamepadState());
                yield return null;
                yield return Drag(mouse, slots[2], SlotPoint(quickUse[4].Button));
                Assert.IsNull(equipment.State.QuickUseItem(0));
                Assert.AreEqual(Medical, equipment.State.QuickUseItem(4));
                Assert.AreEqual(1, equipment.Quantity(Medical), "Assigning a shortcut leaves the item carried.");
                EventSystem.current.SetSelectedGameObject(quickUse[2].Button.gameObject);
                var quickFocus = quickUse[2].Button.transform.Find("FocusCorners").gameObject;
                Assert.IsTrue(quickFocus.activeSelf);
                Queue(keyboard, new KeyboardState(Key.Escape));
                yield return null;
                Queue(keyboard, new KeyboardState());
                Assert.IsFalse(screen.gameObject.activeInHierarchy);
                Assert.IsFalse(quickFocus.activeSelf, "Closing the inventory clears the visible focus.");

                var actions = Object.FindFirstObjectByType<PlayerItemActionComposition>();
                Queue(pad, new GamepadState { rightTrigger = 1 });
                yield return null;
                Assert.IsTrue(actions.Runner.IsExecuting);
                Queue(pad, new GamepadState().WithButton(GamepadButton.LeftShoulder));
                Assert.AreEqual(PlayerEquipmentSlot.Tool, equipment.State.activeSlot);
                Assert.IsFalse(actions.Runner.IsExecuting);
                Queue(pad, new GamepadState());
                yield return null;
                Queue(keyboard, new KeyboardState(Key.Q));
                Assert.AreEqual(PlayerEquipmentSlot.Weapon, equipment.State.activeSlot);
                Queue(keyboard, new KeyboardState());
                yield return null;

                var vitals = Object.FindFirstObjectByType<PlayerVitalsController>();
                vitals.Damage(25);
                double before = vitals.Vitals.Health.Current;
                Queue(keyboard, new KeyboardState(Key.Digit1));
                Assert.IsNull(equipment.SelectedQuickUseItem);
                Queue(keyboard, new KeyboardState(Key.F));
                Assert.AreEqual(before, vitals.Vitals.Health.Current);
                Queue(keyboard, new KeyboardState());
                yield return null;
                Queue(keyboard, new KeyboardState(Key.Digit5));
                Assert.AreEqual(Medical, equipment.SelectedQuickUseItem);
                Assert.AreEqual(before, vitals.Vitals.Health.Current, "Selecting a slot leaves the item unused.");
                Queue(keyboard, new KeyboardState(Key.F));
                Assert.Greater(vitals.Vitals.Health.Current, before);
                Assert.AreEqual(wrench.ItemId, equipment.State.ActiveItemId);
                Assert.AreEqual(Tool, equipment.State.toolItemId);
                Assert.IsNull(equipment.State.pinnedItemId);
                Assert.IsNull(equipment.State.QuickUseItem(4));
                Assert.AreEqual(0, equipment.Quantity(Medical));
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
                InputSystem.RemoveDevice(mouse);
                InputSystem.RemoveDevice(pad);
                InputSystem.settings.backgroundBehavior = background;
                InputSystem.settings.editorInputBehaviorInPlayMode = editor;
                items.Remove(tool); items.Remove(medical);
                Object.Destroy(tool); Object.Destroy(medical);
                var root = Object.FindFirstObjectByType<GameCompositionRoot>();
                if (root != null) Object.Destroy(root.gameObject);
            }
        }
    }
}
#endif
