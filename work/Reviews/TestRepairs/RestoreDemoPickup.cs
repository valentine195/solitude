using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SOLITUDE.Items;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RestoreDemoPickup
{
    public static void Run()
    {
        const string path = "Assets/_Project/Scenes/Demos/AtmosphericScrubberEncounter.unity";
        var original = File.ReadAllText(path);
        var rects = Regex.Matches(original, @"m_NormalizedViewPortRect:\s*\n\s*serializedVersion: \d+\s*\n\s*x: ([^\n]+)\n\s*y: ([^\n]+)\n\s*width: ([^\n]+)\n\s*height: ([^\n]+)");
        if (rects.Count != 1) throw new InvalidOperationException("Expected one authored camera viewport.");
        var values = Enumerable.Range(1, 4).Select(i => float.Parse(rects[0].Groups[i].Value, CultureInfo.InvariantCulture)).ToArray();
        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        var pickups = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<WorldPickup>(true)).ToArray();
        var cameras = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Camera>(true)).ToArray();
        if (pickups.Length != 1 || cameras.Length != 1) throw new InvalidOperationException("Unexpected demo pickup/camera authoring.");
        pickups[0].gameObject.SetActive(true);
        scene.GetRootGameObjects().Single(root => root.name == "UICanvas").SetActive(true);
        PrefabUtility.RecordPrefabInstancePropertyModifications(pickups[0].gameObject);
        // PixelPerfectCamera runs in Edit mode. Preserve the user's saved viewport while authoring the pickup.
        cameras[0].rect = new Rect(values[0], values[1], values[2], values[3]);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Demo scene save failed.");
        Debug.Log("Demo pickup restored; existing camera viewport retained.");
    }
}
