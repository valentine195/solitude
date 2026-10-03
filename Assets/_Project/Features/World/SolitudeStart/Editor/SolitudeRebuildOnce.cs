using System;
using UnityEditor;
using UnityEngine;

namespace SOLITUDE.World.SolitudeStart.Editor
{
    [InitializeOnLoad]
    internal static class SolitudeRebuildOnce
    {
        static SolitudeRebuildOnce() => EditorApplication.delayCall += Run;

        private static void Run()
        {
            if (UnityEngine.Application.isBatchMode) return;
            if (SessionState.GetBool("SolitudeStartRebuild29", false)) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += Run;
                return;
            }
            SessionState.SetBool("SolitudeStartRebuild29", true);
            try { SolitudeStartBuilder.Build(); }
            catch (Exception ex) { Debug.LogException(ex); }
        }
    }
}
