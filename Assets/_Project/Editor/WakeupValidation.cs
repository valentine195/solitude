using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SOLITUDE.Composition;
using SOLITUDE.Features.Interactables;
using SOLITUDE.Items;
using SOLITUDE.SaveLoad;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace SOLITUDE.Editor
{
    [Serializable] public sealed class ValidationFinding
    {
        public string code, severity="Error", path, field, message;
        public ValidationFinding(string code,string path,string field,string message) {this.code=code;this.path=path;this.field=field;this.message=message;}
    }
    [Serializable] public sealed class ValidationReport { public List<ValidationFinding> findings=new(); public bool success; }
    public sealed class WakeupValidation : IPreprocessBuildWithReport, IProcessSceneWithReport
    {
        public int callbackOrder => -100;
        public void OnPreprocessBuild(BuildReport report)
        {
            var manifest=LoadManifest();
            if (report.summary.platform.ToString()!=manifest.target || UnityEditor.OSXStandalone.UserBuildSettings.architecture.ToString()!=manifest.architecture || PlayerSettings.GetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone).ToString()!=manifest.backend)
                throw new BuildFailedException("BUILD_TARGET: Wakeup is qualified for macOS ARM64 only.");
            ValidateForCI();
        }
        public void OnProcessScene(Scene scene,BuildReport report)
        {
            if (report == null) return;
            var manifest=LoadManifest();
            if (!manifest.scenes.Contains(scene.path)) throw new BuildFailedException("BUILD_SCENE: Scene is not in the selected manifest: "+scene.path);
            var findings=ValidateScene(scene,true);
            if (findings.Count>0) throw new BuildFailedException(string.Join("\n",findings.Select(f=>f.code+": "+f.path+": "+f.message)));
        }
        public static WakeupBuildManifest LoadManifest()
        {
            var manifest=AssetDatabase.LoadAssetAtPath<WakeupBuildManifest>(WakeupBuildManifest.AssetPath);
            if (manifest==null) throw new BuildFailedException("BUILD_MANIFEST: Wakeup build manifest is missing.");
            return manifest;
        }
        [MenuItem("SOLITUDE/Validation/Validate Wakeup Build")]
        public static void ValidateForCI()
        {
            var report=Collect();string directory=Environment.GetEnvironmentVariable("SOLITUDE_VALIDATION_OUTPUT")??"Logs/SolitudeValidation";Directory.CreateDirectory(directory);
            string path=Path.Combine(directory,"validation.json");File.WriteAllText(path,JsonUtility.ToJson(report,true));
            File.WriteAllText(Path.Combine(directory,"validation.txt"),string.Join("\n",report.findings.Select(f=>$"{f.severity} {f.code} {f.path} [{f.field}]: {f.message}")));
            if (!report.success) throw new BuildFailedException("Wakeup validation failed. "+Path.GetFullPath(path)+"\n"+string.Join("\n",report.findings.Select(f=>f.code+": "+f.message)));
            Debug.Log("[WakeupValidation] All assembly, authoring, serialized and manifest checks passed. "+Path.GetFullPath(path));
        }
        public static ValidationReport Collect()
        {
            var report=new ValidationReport();var findings=report.findings;
            void Error(string code,string path,string field,string message)=>findings.Add(new ValidationFinding(code,path,field,message));
            var manifest=AssetDatabase.LoadAssetAtPath<WakeupBuildManifest>(WakeupBuildManifest.AssetPath);
            if (manifest==null) Error("BUILD_MANIFEST",WakeupBuildManifest.AssetPath,"manifest","Missing manifest.");
            else
            {
                if (manifest.scenes==null || manifest.scenes.Length!=1 || manifest.scenes[0]!="Assets/Scenes/Wakeup.unity") Error("BUILD_SCENES",WakeupBuildManifest.AssetPath,"scenes","Wakeup must be the required entry.");
                var selected=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray();
                if (!selected.SequenceEqual(manifest.scenes??Array.Empty<string>())) Error("BUILD_SETTINGS",WakeupBuildManifest.AssetPath,"scenes","Enabled build scenes must match the manifest.");
                if (manifest.runtimeConfig?.database==null) Error("CATALOG_CONFIG",WakeupBuildManifest.AssetPath,"runtimeConfig","Missing configured catalog.");
                else
                {
                    var config=manifest.runtimeConfig;
                    try
                    {
                        var catalog=config.database.BuildRuntimeCatalog();config.database.BuildPresentationCatalog();
                        var seen=new HashSet<LootTable>();
                        foreach(var table in config.lootTables)
                        {if(table==null||!seen.Add(table))throw new InvalidOperationException("Null/duplicate loot table.");table.Compile(catalog);}
                    }
                    catch(Exception error){Error("CATALOG_RULES",AssetDatabase.GetAssetPath(config),"database/lootTables",error.Message);}
                }
                foreach(string path in manifest.scenes??Array.Empty<string>())
                {
                    if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path)==null) {Error("SCENE_MISSING",path,"scene","Required scene missing.");continue;}
                    var scene=EditorSceneManager.OpenPreviewScene(path);
                    try {findings.AddRange(ValidateScene(scene,true));}
                    finally {EditorSceneManager.ClosePreviewScene(scene);}
                    foreach(string dependency in AssetDatabase.GetDependencies(path,true).Where(p=>p.EndsWith(".prefab")))
                    {
                        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(dependency);if(prefab!=null)InspectScripts(prefab,dependency,findings);
                        if(prefab!=null)foreach(var pickup in prefab.GetComponentsInChildren<WorldPickup>(true))
                            if(!string.IsNullOrEmpty(pickup.PickupId))Error("PREFAB_ID",dependency,"SaveableId","Prefab pickup template carries a world-instance identity.");
                    }
                }
            }
            foreach(var error in UnityMetaValidator.CollectErrors())Error("ASSET_METADATA","Assets","meta",error);
            foreach(var assembly in CompilationPipeline.GetAssemblies(AssembliesType.Editor).Where(a=>a.name.StartsWith("SOLITUDE.")))
            {
                if(assembly.name=="SOLITUDE.Domain"||assembly.name=="SOLITUDE.Application")
                {
                    if(AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name==assembly.name).GetReferencedAssemblies().Any(r=>r.Name.StartsWith("Unity")))Error("ASSEMBLY_ENGINE",assembly.name,"references","Pure assembly references Unity.");
                    if(assembly.assemblyReferences.Any(a=>a.name.StartsWith("SOLITUDE.")&&a.name!="SOLITUDE.Domain"))Error("ASSEMBLY_DIRECTION",assembly.name,"references","Forbidden project dependency.");
                }
                if(assembly.name=="SOLITUDE.Runtime"&&assembly.assemblyReferences.Any(a=>a.name.StartsWith("SOLITUDE.")&&(a.name.Contains("Editor")||a.name.Contains("Tests"))))Error("ASSEMBLY_DIRECTION",assembly.name,"references","Runtime references editor/test code.");
            }
            foreach(var assembly in CompilationPipeline.GetAssemblies(AssembliesType.Editor).Where(a=>a.name=="Assembly-CSharp"||a.name=="Assembly-CSharp-Editor"))
                foreach(var source in assembly.sourceFiles.Where(p=>p.Contains("Assets/_Project/")||p.Contains("Assets/Scenes/Wakeup/")))Error("ASSEMBLY_DEFAULT",source,"assembly","Project source remains in a default assembly.");
            findings.Sort((a,b)=>string.CompareOrdinal(a.path+"|"+a.code+"|"+a.field+"|"+a.message,b.path+"|"+b.code+"|"+b.field+"|"+b.message));report.success=findings.All(f=>f.severity!="Error");return report;
        }
        public static List<ValidationFinding> ValidateScene(Scene scene,bool directEntry)
        {
            var findings=new List<ValidationFinding>();var roots=scene.GetRootGameObjects();
            void Error(string code,string field,string message)=>findings.Add(new ValidationFinding(code,scene.path,field,message));
            var scopes=roots.SelectMany(r=>r.GetComponentsInChildren<SceneBindings>(true)).ToArray();
            var bootstrap=roots.SelectMany(r=>r.GetComponentsInChildren<GameCompositionRoot>(true)).ToArray();
            if(directEntry&&(scopes.Count(s=>s.HasPlayer)!=1||bootstrap.Length!=1))Error("SCENE_COMPOSITION","bootstrap/player","Direct entry requires exactly one bootstrap and one player scope.");
            try {CompositionValidator.ValidateScene(scene);}catch(Exception error){Error("SCENE_ENDPOINTS","SceneBindings",error.Message);}
            foreach(var root in roots)InspectScripts(root,scene.path,findings);
            var owners=new HashSet<string>(StringComparer.Ordinal);var pickupIds=new HashSet<string>(StringComparer.Ordinal);
            var manifest=AssetDatabase.LoadAssetAtPath<WakeupBuildManifest>(WakeupBuildManifest.AssetPath);var database=manifest?.runtimeConfig?.database;
            foreach(var root in bootstrap)
            {
                var fields=new SerializedObject(root);
                if(fields.FindProperty("config").objectReferenceValue!=manifest?.runtimeConfig)
                    Error("SCENE_CONFIG",root.name+"/config","Bootstrap configuration must match the build manifest.");
            }
            foreach(var locker in roots.SelectMany(r=>r.GetComponentsInChildren<LockerContainer>(true)))
            {
                if(string.IsNullOrWhiteSpace(locker.PersistentId)||locker.PersistentId==ContainerSaveSession.PlayerInventoryId||locker.PersistentId==ContainerSaveSession.PlayerHotbarId||!owners.Add(locker.PersistentId))Error("CONTAINER_ID",locker.name+"/SaveableId","Invalid, reserved or duplicate locker ID.");
                if(locker.Loot!=null && (manifest?.runtimeConfig?.lootTables==null || !manifest.runtimeConfig.lootTables.Contains(locker.Loot)))Error("LOCKER_LOOT",locker.name+"/loot","Locker loot table is absent from the manifest configuration.");
            }
            foreach(var pickup in roots.SelectMany(r=>r.GetComponentsInChildren<WorldPickup>(true)))
            {
                if(string.IsNullOrWhiteSpace(pickup.PickupId)||!pickupIds.Add(pickup.PickupId))Error("PICKUP_ID",pickup.name+"/SaveableId","Missing or duplicate pickup identity.");
                var definition=pickup.Definition;
                if(definition?.Item==null||definition.Quantity<1||database==null||database.Resolve(definition.Item.ItemId)!=definition.Item)Error("PICKUP_REWARD",pickup.name+"/definition","Invalid reward/catalog reference.");
            }
            var actions=AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>("Assets/_Project/Core/Input/SOLITUDE_InputActions.inputactions");
            for(int i=1;i<=9;i++)if(actions?.FindAction("Gameplay/Hotbar"+i,false)==null)Error("INPUT_ACTION","Hotbar"+i,"Required hotbar action missing.");
            if(actions!=null)foreach(var action in actions)
                if(action.bindings.Count==0)Error("INPUT_BINDING",action.name,"Action has no bindings.");
            return findings;
        }
        private static void InspectScripts(GameObject root,string path,List<ValidationFinding> findings)
        {
            foreach(var transform in root.GetComponentsInChildren<Transform>(true))
                if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject)>0)
                    findings.Add(new ValidationFinding("MISSING_SCRIPT",path,transform.name,"Missing MonoBehaviour script."));
        }
    }
}
