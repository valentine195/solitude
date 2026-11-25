var oldBackground = UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior;
var oldEditorInput = UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode;
UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode=UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var screen = UnityEngine.Object.FindAnyObjectByType<SOLITUDE.Containers.InventoryScreenView>(UnityEngine.FindObjectsInactive.Include);
Func<string,object> field = name => screen.GetType().GetField(name,flags).GetValue(screen);
Action<bool,string> check = (pass,message) => {if(!pass)throw new Exception(message);};
var keyboard = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
Action<UnityEngine.InputSystem.Key> press = key => { UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(key));UnityEngine.InputSystem.InputSystem.Update();UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState());UnityEngine.InputSystem.InputSystem.Update();};
press(UnityEngine.InputSystem.Key.Tab);
check(screen.isActiveAndEnabled,"Tab must open actual inventory");
var carried=(SOLITUDE.Containers.IContainerReader)field("carried");
var hotbar=(SOLITUDE.Containers.IContainerReader)field("hotbar");
var commands=(SOLITUDE.Containers.ContainerCommandService)field("commands");
var hotView=(SOLITUDE.Containers.ContainerView)field("hotbarView");
var tray=(UnityEngine.RectTransform)field("dockedHotbarTray");
check(tray.parent.name=="QuickAccessDock","Quick bar must dock inside inventory");
check(screen.GetComponent<SOLITUDE.Containers.ContainerView>().SlotViews.Count==carried.Capacity,"Carried grid capacity mismatch");
check(carried.GetSlot(0).IsEmpty && !hotbar.GetSlot(0).IsEmpty,"Expected original demo wrench in hotbar and empty carried slot");
string original=hotbar.GetSlot(0).Stack.ItemId;
check(commands.Transfer(hotbar.GetSlot(0).Address,carried.GetSlot(0).Address).Succeeded,"Fixture move failed");
var slots=screen.GetComponent<SOLITUDE.Containers.ContainerView>().SlotViews;
UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(slots[1].gameObject);
UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(slots[0].gameObject);
screen.SendMessage("LateUpdate");
check(((UnityEngine.UI.Image)field("weaponResponse")).color.a>0,"Destination response must brighten after selecting a tool");
check(!((UnityEngine.UI.Button)field("dropButton")).interactable,"Unsupported Drop must remain disabled");
check(!screen.transform.Find("Panel/All").gameObject.activeSelf,"Category tabs must remain hidden");
Func<string,string> capture=path=>{
var canvases=UnityEngine.Object.FindObjectsByType<Canvas>(UnityEngine.FindObjectsInactive.Exclude).Where(c=>c.isRootCanvas).ToArray();
var scales=canvases.Select(c=>c.scaleFactor).ToArray();var scalers=canvases.Select(c=>c.GetComponent<UnityEngine.UI.CanvasScaler>()).ToArray();var enabled=scalers.Select(s=>s!=null&&s.enabled).ToArray();
for(int i=0;i<canvases.Length;i++){if(scalers[i]!=null)scalers[i].enabled=false;canvases[i].scaleFactor=1;}
try{return SOLITUDE.Editor.InventoryScreenAuthoring.CaptureForReview(path);}finally{for(int i=0;i<canvases.Length;i++){canvases[i].scaleFactor=scales[i];if(scalers[i]!=null)scalers[i].enabled=enabled[i];}}
};
capture("/Users/jeremyvalentine/SOLITUDE/work/Reviews/InventoryImplemented/inventory-selected-final.png");
return "PASS: keyboard Tab open; live carried/quick bindings; destination response; disabled Drop; removed tabs; selected screenshot.";
