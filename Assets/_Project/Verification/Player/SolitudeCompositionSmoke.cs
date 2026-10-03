using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using SOLITUDE.Application;
using SOLITUDE.Composition;
using SOLITUDE.Containers;
using SOLITUDE.Items;
using SOLITUDE.Player;
using SOLITUDE.SaveLoad;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
public sealed class SolitudeCompositionSmoke : MonoBehaviour
{
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Boot()
    {
        if (UnityEngine.Application.isEditor || FindFirstObjectByType<SolitudeCompositionSmoke>() != null) return;
        var go = new GameObject("CompositionSmoke"); DontDestroyOnLoad(go); go.AddComponent<SolitudeCompositionSmoke>();
    }
    private static void Check(bool condition, string text) { if (!condition) throw new Exception(text); }
    [Serializable] private sealed class Expected { public string pickupId, lockerId, lockerJson; public int quantity; }
    private static readonly string manifest = System.IO.Path.Combine(DirectoryArgument, "world-expected.json");
    private static string DirectoryArgument
    {
        get { var args=Environment.GetCommandLineArgs(); int i=Array.IndexOf(args,"-solitudeSaveDirectory"); if(i<0)throw new Exception("Verification requires an isolated save directory."); return args[i+1]; }
    }
    private static bool Flag(string flag) => Environment.GetCommandLineArgs().Contains(flag);
    private static void Result(string text) => System.IO.File.WriteAllText(System.IO.Path.Combine(DirectoryArgument,"result.json"),"{\"success\":true,\"message\":\""+text+"\"}");
    private static bool restart => Environment.GetCommandLineArgs().Contains("-verifyRestart");
    private static Keyboard keyboard;
    private static void Press(Key key)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); InputSystem.Update();
        InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
    }
    private IEnumerator Start()
    {
        yield return null; yield return null;
        bool returnFromStart=false;
        try
        {
            if (Flag("-expectBlocked"))
            {
                Check(FindFirstObjectByType<GameCompositionRoot>().State==RuntimeStartupState.Failed,"Bad save did not block startup");
                Result("Blocked invalid save"); UnityEngine.Application.Quit(0); returnFromStart=true;
            }
            else if (Flag("-faultWrite"))
            {
                var graph=FindFirstObjectByType<GameCompositionRoot>();var data=Field<ContainerSaveSession>(graph,"session").Capture();data.worldSeed++;
                new SOLITUDE.Application.Persistence.SaveRecoveryStore(System.IO.Path.Combine(DirectoryArgument,"solitude-save.json"),new PhysicalSaveFiles(),new SaveJsonCodec(Field<ItemCatalog>(graph,"catalog"))).Write(data);
                throw new Exception("Expected process interruption did not occur");
            }
            
        } catch(Exception error) { Debug.LogException(error); UnityEngine.Application.Quit(1); yield break; }
        if (returnFromStart) yield break;
        var verification = Verify();
        while (true)
        {
            bool more;
            try { more = verification.MoveNext(); }
            catch(Exception error) { Debug.LogException(error); UnityEngine.Application.Quit(1); yield break; }
            if (!more) break;
            yield return verification.Current;
        }
        if (Flag("-verifySnapshotOnly")) { Result("Complete world snapshot verified"); UnityEngine.Application.Quit(0); yield break; }
        if (!restart) { Result("Controls and paused save passed"); Debug.Log("COMPOSITION_FIRST_PASS: controls, UI, pauses, transfers, saved while paused"); UnityEngine.Application.Quit(0); yield break; }
        yield return null; yield return null;
        try
        {
            var root = FindFirstObjectByType<GameCompositionRoot>();
            Check(root.State == RuntimeStartupState.Ready, "New-save reload was not ready");
            Check(Time.timeScale == 1, "Reload retained modal pause");
            var scope = FindFirstObjectByType<SceneBindings>();
            Check(Field<ContainerModalCoordinator>(scope, "modal").Kind == ContainerModalKind.Closed, "Reload retained screen");
            Check(Field<HotbarSelectionState>(scope, "selection").ActiveIndex == -1, "Reload retained hotbar selection");
            var previous=JsonUtility.FromJson<Expected>(System.IO.File.ReadAllText(manifest));
            var freshSession=Field<ContainerSaveSession>(root,"session");
            var freshPickup=FindObjectsByType<WorldPickup>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(p=>p.PickupId==previous.pickupId);
            Check(!freshSession.IsPickupCollected(previous.pickupId)&&freshPickup.gameObject.activeSelf,"New save retained collection facts");
            Check(FindFirstObjectByType<PlayerInventory>().Container.GetSlots().All(s=>s.IsEmpty),"New save retained carried items");
            Check(freshPickup.Interact(null).IsSuccess,"New save could not collect the authored pickup again");
            Press(Key.Tab); Check(Time.timeScale == 0, "Reload input was not rebound"); Press(Key.Escape);
            Result("Restart and new-save passed"); Debug.Log("COMPOSITION_SMOKE_PASS: input, UI asset, drag/Escape, pause ownership, locker replacement, pickup transfer, resize, new-save reload");
            UnityEngine.Application.Quit(0);
        }
        catch(Exception error) { Debug.LogException(error); UnityEngine.Application.Quit(1); }
    }
    private static IEnumerator Verify()
    {
        var root = FindFirstObjectByType<GameCompositionRoot>();
        Check(root != null && root.State == RuntimeStartupState.Ready, "Runtime startup failed");
        var scope = FindFirstObjectByType<SceneBindings>(); var modal = Field<ContainerModalCoordinator>(scope, "modal");
        var policy = Field<InputPolicyCoordinator>(root, "policy"); var gestures = Field<ContainerGestureCoordinator>(root, "gestures");
        var input = Field<SOLITUDE.Core.Input.UnityInputHost>(root, "input");
        var actions = Field<SOLITUDE_InputActions>(input, "actions");
        Check(FindFirstObjectByType<InputSystemUIInputModule>().actionsAsset == actions.asset, "UI uses a second input asset");
        var session = Field<ContainerSaveSession>(root, "session"); var inventory = FindFirstObjectByType<PlayerInventory>();
        var hotbar = FindFirstObjectByType<SOLITUDE.Hotbar.Hotbar>(); var selection = Field<HotbarSelectionState>(scope, "selection");
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        keyboard = InputSystem.AddDevice<Keyboard>();
        if (restart || Flag("-verifySnapshotOnly"))
        {
            var expected = JsonUtility.FromJson<Expected>(System.IO.File.ReadAllText(manifest));
            var restored = FindObjectsByType<WorldPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(p => p.PickupId == expected.pickupId);
            Check(!restored.gameObject.activeSelf && session.IsPickupCollected(expected.pickupId), "Restart duplicated pickup");
            int total = inventory.Container.GetSlots().Concat(hotbar.Container.GetSlots()).Where(s => !s.IsEmpty).Sum(s => s.Stack.Quantity);
            Check(total == expected.quantity, "Restart changed inventory quantities");
            var locker = FindObjectsByType<SOLITUDE.Features.Interactables.LockerContainer>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(l => l.PersistentId == expected.lockerId);
            Check(root.Open(locker), "Restart could not open locker");
            Check(JsonUtility.ToJson(ContainerSaveSerializer.Capture(locker.Container)) == expected.lockerJson, "Restart rerolled locker");
            if (Flag("-expectRecovery"))
            {
                var host=Field<SaveGameService>(root,"persistence");Check(host.RecoveryNoticePending,"Recovery receipt missing");
                var notice=FindObjectsByType<RecoveryNoticeView>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single();Check(notice.gameObject.activeInHierarchy,"Recovery notice was not visible");
                Field<UnityEngine.UI.Button>(notice,"dismiss").onClick.Invoke();Check(!host.RecoveryNoticePending,"Recovery dismissal was not acknowledged");
            }
            if (Flag("-verifySnapshotOnly")) yield break;
            root.StartNewSave(); yield break;
        }
        var pickup = FindObjectsByType<WorldPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(p => p.gameObject.activeSelf);
        Check(pickup.Interact(null).IsSuccess && !pickup.gameObject.activeSelf, "Pickup collection failed");
        int changes = 0; selection.Changed += _ => changes++;
        Press(Key.Digit1); Check(selection.ActiveIndex == 0 && changes == 1, "Hotbar input duplicated");
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W)); InputSystem.Update();
        var movement = Field<SOLITUDE.Player.PlayerMovement>(scope, "movement").GetComponent<SOLITUDE.Movement.MovementBehavior>();
        Check(Field<Vector2>(movement, "vector").sqrMagnitude > 0, "Movement input was not delivered");
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.Tab)); InputSystem.Update();
        Check(Field<Vector2>(movement, "vector") == Vector2.zero, "Modal did not clear movement");
        InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
        Check(modal.Kind == ContainerModalKind.Inventory && Time.timeScale == 0 && !policy.Gameplay && policy.Pointer, "Inventory policy failed");
        Press(Key.Digit2); Check(selection.ActiveIndex == 0 && changes == 1, "Modal allowed hotbar input");
        var view = Field<ContainerModalView>(scope, "inventoryModal");
        var source = inventory.Container.GetSlots().First(s => !s.IsEmpty);
        var slots = view.Controller.GetComponent<ContainerView>().GetComponentsInChildren<ContainerSlotView>();
        var slot = slots.First(s => s.Index == source.Address.Index);
        var pointer = new PointerEventData(EventSystem.current);
        yield return null; Canvas.ForceUpdateCanvases();
        var canvas = slot.GetComponentInParent<Canvas>(); var rect = (RectTransform)slot.transform;
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, rect.TransformPoint(rect.rect.center));
        var mouse = InputSystem.AddDevice<Mouse>(); var module = FindFirstObjectByType<InputSystemUIInputModule>();
        InputSystem.QueueStateEvent(mouse, new MouseState { position = screen }); InputSystem.Update(); yield return null; module.Process();
        var hits = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) {position=screen},hits);
        Check(hits.Any(h=>h.gameObject.GetComponentInParent<ContainerSlotView>()==slot),"Inventory slot was not hit by UI raycast: "+screen);
        var hovered = EventSystem.current.currentInputModule; Check(hovered == module, "EventSystem module was not active");
        InputSystem.QueueStateEvent(mouse, new MouseState { position = screen }.WithButton(MouseButton.Left)); InputSystem.Update(); yield return null; module.Process();
        InputSystem.QueueStateEvent(mouse, new MouseState { position = screen + new Vector2(24, 0) }.WithButton(MouseButton.Left)); InputSystem.Update(); yield return null; module.Process();
        Check(gestures.Interaction.IsHolding, "EventSystem raycast did not begin drag");
        Press(Key.Escape); InputSystem.QueueStateEvent(mouse, new MouseState { position = screen }); InputSystem.Update(); yield return null; module.Process(); Check(!gestures.Interaction.IsHolding && modal.Kind == ContainerModalKind.Inventory && Time.timeScale == 0, "Escape closed screen during drag");
        Press(Key.Escape); Check(modal.Kind == ContainerModalKind.Closed && policy.Gameplay && Time.timeScale == 1, "Second Escape did not close");
        gestures.Begin("smoke", source); gestures.Drop(hotbar.Container.GetSlot(0)); gestures.Cancel();
        Check(hotbar.Container.GetSlot(0).Stack != null, "Inventory/hotbar transfer failed");
        var lockers = FindObjectsByType<SOLITUDE.Features.Interactables.LockerContainer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Check(root.Open(lockers[0]) && Time.timeScale == 0 && modal.Kind == ContainerModalKind.Transfer, "Locker modal failed");
        if (lockers.Length > 1) { Check(root.Open(lockers[1]), "Locker replacement failed"); root.Close(lockers[0]); Check(modal.Kind == ContainerModalKind.Transfer, "Stale locker close affected replacement"); }
        var game = Field<SOLITUDE.Core.Systems.GameManager>(root, "game"); game.PauseGame(); Press(Key.Escape);
        Check(modal.Kind == ContainerModalKind.Closed && Time.timeScale == 0 && !policy.Gameplay, "Modal released independent pause");
        Press(Key.Escape); Check(Time.timeScale == 1 && policy.Gameplay, "Manual pause did not release");
        Field<SOLITUDE.Core.Systems.TimeSystem>(root, "clock").SetTimeScale(.35f); Press(Key.Tab); Press(Key.Escape);
        Check(Time.timeScale == .35f, "Modal lost slow-motion scale"); Field<SOLITUDE.Core.Systems.TimeSystem>(root, "clock").SetTimeScale(1);
        Press(Key.Tab); session.Commands.Resize(inventory.Container.Handle, inventory.Capacity + 1);
        Check(Field<System.Collections.Generic.List<ContainerSlotView>>(view.Controller.GetComponent<ContainerView>(), "slotViews").Count == inventory.Container.Capacity, "Presenter resize failed");
        var persisted = new Expected { pickupId = pickup.PickupId, lockerId = lockers[0].PersistentId,
            lockerJson = JsonUtility.ToJson(ContainerSaveSerializer.Capture(lockers[0].Container)),
            quantity = inventory.Container.GetSlots().Concat(hotbar.Container.GetSlots()).Where(s => !s.IsEmpty).Sum(s => s.Stack.Quantity) };
        Field<SaveGameService>(root, "persistence").Flush();
        Check(!session.IsDirty && Time.timeScale == 0, "Saving while paused failed");
        System.IO.File.WriteAllText(manifest, JsonUtility.ToJson(persisted));
    }
}
