using SOLITUDE.Composition;
using UnityEngine;
namespace SOLITUDE.Editor
{
    public sealed class WakeupBuildManifest : ScriptableObject
    {
        public const string AssetPath = "Assets/_Project/Editor/WakeupBuildManifest.asset";
        public string[] scenes = { "Assets/Scenes/Wakeup.unity" };
        public GameRuntimeConfig runtimeConfig;
        public string target = "StandaloneOSX";
        public string architecture = "ARM64";
        public string backend = "Mono2x";
        public string saveWorld = "Wakeup";
        // Context fixtures share this save world but are excluded from player builds.
        public string[] verificationContextScenes = { PhaseFiveFixtureBuilder.Context, PhaseFiveFixtureBuilder.Unrelated };
    }
}
