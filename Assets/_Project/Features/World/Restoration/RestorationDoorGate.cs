using SOLITUDE.Restoration;
using SOLITUDE.World.SolitudeStart;
using UnityEngine;

namespace SOLITUDE.World.Restoration
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SolitudeSlidingDoor))]
    public sealed class RestorationDoorGate : MonoBehaviour
    {
        [SerializeField] private RestorableSystem system;
        private RestorableSystemState subscribed;
        private bool restored;
        public bool AllowsAccess => isActiveAndEnabled && system != null && restored;
        public void Configure(RestorableSystem target)
        {
            Unsubscribe(); system = target;
            if (isActiveAndEnabled) Subscribe();
        }
        private void OnEnable()
        {
            GetComponent<SolitudeSlidingDoor>().SetAccessGate(this);
            Subscribe();
        }
        private void Subscribe()
        {
            Unsubscribe();
            subscribed = system != null ? system.State : null;
            if (subscribed != null) subscribed.Changed += Reconcile;
            Reconcile();
        }
        private void Reconcile() => restored = system != null && subscribed != null && subscribed.IsRestored;
        private void Unsubscribe()
        {
            if (subscribed != null) subscribed.Changed -= Reconcile;
            subscribed = null; restored = false;
        }
        private void OnDisable() => Unsubscribe();
    }
}
