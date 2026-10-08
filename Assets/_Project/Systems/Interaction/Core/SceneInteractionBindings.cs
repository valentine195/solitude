using System;
using System.Linq;
using SOLITUDE.Composition;
using SOLITUDE.Player;
using UnityEngine;

namespace SOLITUDE.Core.Interaction
{
    // Only for standalone reference scenes without a GameCompositionRoot/SceneBindings.
    [DefaultExecutionOrder(-1500)]
    public sealed class SceneInteractionBindings : MonoBehaviour
    {
        private SceneInteractionScope scope;
        public void Validate()
        {
            var roots = gameObject.scene.GetRootGameObjects();
            if (roots.SelectMany(root => root.GetComponentsInChildren<SceneInteractionBindings>(true)).Count() != 1 ||
                roots.SelectMany(root => root.GetComponentsInChildren<SceneBindings>(true)).Any())
                throw new InvalidOperationException("Standalone interaction owner must be unique and cannot coexist with SceneBindings.");
            SceneInteractionScope.Validate(gameObject.scene, Player());
        }
        private PlayerInteractor Player() => gameObject.scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<PlayerInteractor>(true)).Single();
        public void Initialize()
        {
            Release(); Validate(); scope = new SceneInteractionScope();
            try { scope.Initialize(gameObject.scene, Player()); }
            catch { Release(); throw; }
        }
        public void Release() { scope?.Dispose(); scope = null; }
        private void Awake() => Initialize();
        private void OnDestroy() => Release();
    }
}
