using SOLITUDE.Restoration;
using UnityEngine;

namespace SOLITUDE.World.Restoration
{
    public sealed class RestorableSystem : MonoBehaviour
    {
        private readonly RestorableSystemState state = new RestorableSystemState();
        public RestorableSystemState State => state;
        public bool IsRestored => state.IsRestored;
        private void Awake() => state.ObserverError += Debug.LogException;
        private void OnDestroy() => state.ObserverError -= Debug.LogException;
    }
}
