using System;
using SOLITUDE.Application;
using SOLITUDE.Movement;
using UnityEngine;
namespace SOLITUDE.Npcs.Sec0
{
    // A temporary movement lease; inventory/input ownership stays with the scene.
    [DisallowMultipleComponent, RequireComponent(typeof(MovementBehavior))]
    public sealed class PlayerStunReceiver : MonoBehaviour
    {
        private MovementBehavior movement;
        private IDisposable movementBlock;
        private InputPolicyCoordinator policy;
        private Func<bool> available;
        private object owner;
        public float Remaining { get; private set; }
        public bool IsStunned => movementBlock!=null;
        public void Bind(object bindingOwner, InputPolicyCoordinator gameplay, Func<bool> targetAvailable)
        {
            if(bindingOwner==null || gameplay==null || targetAvailable==null) throw new ArgumentNullException(nameof(bindingOwner));
            if(owner!=null && !ReferenceEquals(owner,bindingOwner)) throw new InvalidOperationException("Stun receiver already owned.");
            Release(bindingOwner);owner=bindingOwner;policy=gameplay;available=targetAvailable;movement=GetComponent<MovementBehavior>();
        }
        public bool TryStun(float duration)
        {
            if(!isActiveAndEnabled || owner==null || !policy.Gameplay || !available()
                || float.IsNaN(duration) || float.IsInfinity(duration) || duration<=0) return false;
            Remaining=Mathf.Max(Remaining,duration);movementBlock??=movement.BlockMovement();return true;
        }
        private void Update()
        {
            if(owner==null) return;
            if(!available()) { Clear();return; }
            if(policy.Gameplay) Advance(Time.deltaTime);
        }
        public void Advance(float elapsed)
        {
            if(owner==null || !policy.Gameplay || !isActiveAndEnabled || !available() || elapsed<=0 || float.IsNaN(elapsed) || float.IsInfinity(elapsed)) return;
            Remaining=Mathf.Max(0,Remaining-elapsed);if(Remaining==0) Clear();
        }
        private void Clear() { movementBlock?.Dispose();movementBlock=null;Remaining=0; }
        public void Release(object bindingOwner)
        {
            if(!ReferenceEquals(owner,bindingOwner)) return;
            Clear();owner=null;policy=null;available=null;
        }
        private void OnDisable()=>Clear();
        private void OnDestroy()=>Clear();
    }
}
