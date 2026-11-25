var path="Assets/_Project/Core/Input/SOLITUDE_InputActions.inputactions";
var asset=UnityEngine.InputSystem.InputActionAsset.FromJson(System.IO.File.ReadAllText(path));
if(asset==null||asset.FindAction("Gameplay/Use",false)!=null)throw new System.Exception("Expected original input without Use");
try {
var map=asset.FindActionMap("Gameplay",true);var use=UnityEngine.InputSystem.InputActionSetupExtensions.AddAction(map,"Use",UnityEngine.InputSystem.InputActionType.Button);
UnityEngine.InputSystem.InputActionSetupExtensions.AddBinding(use,"<Keyboard>/f");UnityEngine.InputSystem.InputActionSetupExtensions.AddBinding(use,"<Gamepad>/buttonWest");
System.IO.File.WriteAllText(path,asset.ToJson());
UnityEditor.AssetDatabase.ImportAsset(path,UnityEditor.ImportAssetOptions.ForceUpdate);
return "Added Use F/gamepad West through input asset API; importer regenerates enabled wrapper";

} finally {UnityEngine.Object.DestroyImmediate(asset);}
