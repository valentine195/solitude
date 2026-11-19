using System;
using SOLITUDE.World.CrewBackgrounds;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SOLITUDE.Editor
{
    [InitializeOnLoad]
    public static class CrewBackgroundsPlaytestLaunches
    {
        private const string Menu = "SOLITUDE/Demos/Crew Backgrounds/";
        private const string PendingBackground = "SOLITUDE.CrewBackgrounds.PendingBackground";
        private const string PendingFault = "SOLITUDE.CrewBackgrounds.PendingFault";

        static CrewBackgroundsPlaytestLaunches()
        {
            CrewBackgroundsPlaytestSetup.ConsumeEditorLaunch = Consume;
            EditorApplication.playModeStateChanged += PlayModeChanged;
        }

        [MenuItem(Menu + "Engineer - Loss of Power", priority = 10)]
        private static void EngineerPower() => Launch(CrewBackground.Engineer, CrewBackgroundsFault.LossOfPower);

        [MenuItem(Menu + "Engineer - Broken Intake Linkage", priority = 11)]
        private static void EngineerLinkage() => Launch(CrewBackground.Engineer, CrewBackgroundsFault.BrokenIntakeLinkage);

        [MenuItem(Menu + "Technician - Loss of Power", priority = 12)]
        private static void TechnicianPower() => Launch(CrewBackground.Technician, CrewBackgroundsFault.LossOfPower);

        [MenuItem(Menu + "Technician - Broken Intake Linkage", priority = 13)]
        private static void TechnicianLinkage() => Launch(CrewBackground.Technician, CrewBackgroundsFault.BrokenIntakeLinkage);

        private static void Launch(CrewBackground background, CrewBackgroundsFault fault)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("Exit Play mode before launching a crew backgrounds preset.");
                return;
            }

            Clear();
            try
            {
                var setup = new CrewBackgroundsPlaytestSetup(background, fault);
                var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(CrewBackgroundsPlaytestSetup.ScenePath);
                if (asset == null)
                    throw new InvalidOperationException("The crew backgrounds maintenance scene is missing.");
                if (EditorSceneManager.playModeStartScene != null && EditorSceneManager.playModeStartScene != asset)
                    throw new InvalidOperationException("Clear the current Play Mode Start Scene override before starting a crew backgrounds preset.");
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

                EditorSceneManager.OpenScene(CrewBackgroundsPlaytestSetup.ScenePath, OpenSceneMode.Single);
                Stage(setup);
                EditorApplication.EnterPlaymode();
            }
            catch (Exception error)
            {
                Clear();
                Debug.LogError("Crew backgrounds launch failed. " + error.Message);
            }
        }

        private static void Stage(CrewBackgroundsPlaytestSetup setup)
        {
            SessionState.SetInt(PendingBackground, (int)setup.Background);
            SessionState.SetInt(PendingFault, (int)setup.Fault);
        }

        private static CrewBackgroundsPlaytestSetup? Consume(string scenePath)
        {
            if (!string.Equals(scenePath, CrewBackgroundsPlaytestSetup.ScenePath, StringComparison.Ordinal))
                return null;

            int background = SessionState.GetInt(PendingBackground, -1);
            int fault = SessionState.GetInt(PendingFault, -1);
            Clear();
            if (background == -1 && fault == -1) return null;
            return new CrewBackgroundsPlaytestSetup((CrewBackground)background, (CrewBackgroundsFault)fault);
        }

        private static void PlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredEditMode || change == PlayModeStateChange.ExitingPlayMode)
                Clear();
            if (change == PlayModeStateChange.EnteredPlayMode
                && (SessionState.GetInt(PendingBackground, -1) != -1 || SessionState.GetInt(PendingFault, -1) != -1))
            {
                Clear();
                Debug.LogError("Crew backgrounds launch failed. The maintenance scene did not consume its preset.");
            }
        }

        private static void Clear()
        {
            SessionState.EraseInt(PendingBackground);
            SessionState.EraseInt(PendingFault);
        }
    }
}
