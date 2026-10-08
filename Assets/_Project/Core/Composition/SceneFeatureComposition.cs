using UnityEngine;

namespace SOLITUDE.Composition
{
    // SceneBindings owns this lifetime, including disabled/inactive authored components.
    public abstract class SceneFeatureComposition : MonoBehaviour
    {
        public abstract void Validate();
        public abstract void Initialize(SceneFeatureContext context);
        public abstract bool TryBack();
        public abstract void CloseForTransition();
        public abstract void Release();
    }
}
