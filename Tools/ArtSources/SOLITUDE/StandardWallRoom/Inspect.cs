var scenes = new System.Collections.Generic.List<object>();
for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++){var s=UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);scenes.Add(new {s.name,s.path,s.isDirty});}
var paths=new[]{"Assets/AI_Sprites/Walls/plain-walls/wall_caps.png","Assets/AI_Sprites/Walls/plain-walls/wall_caps.aseprite","Assets/_Project/Art/SOLITUDE/Tiles/solitude_floor_quiet_normal_v01.png","Assets/_Project/Art/Tiles/Walls/Rules/RT_Wall_Caps.asset"};
var list=new System.Collections.Generic.List<object>();
foreach(var p in paths){var a=UnityEditor.AssetDatabase.LoadAllAssetsAtPath(p);var sprites=new System.Collections.Generic.List<object>();foreach(var x in a)if(x is UnityEngine.Sprite s)sprites.Add(new {s.name,rect=s.rect.ToString(),ppu=s.pixelsPerUnit,pivot=s.pivot.ToString(),bounds=s.bounds.ToString()});list.Add(new {path=p,sprites});}
return new {scenes,assets=list,sorting=System.Array.ConvertAll(UnityEngine.SortingLayer.layers,x=>x.name)};
