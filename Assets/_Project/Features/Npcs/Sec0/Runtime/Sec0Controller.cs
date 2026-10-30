using System;
using SOLITUDE.Application;
using UnityEngine;
namespace SOLITUDE.Npcs.Sec0
{
    [DisallowMultipleComponent, RequireComponent(typeof(Rigidbody2D),typeof(BoxCollider2D))]
    public sealed class Sec0Controller : MonoBehaviour
    {
        [SerializeField] private Animator visual;
        [SerializeField] private GameObject stunFxPrefab;
        [SerializeField] private Transform[] patrolPoints=Array.Empty<Transform>();
        [SerializeField] private LayerMask solidLayers=~0;
        [SerializeField,Min(.1f)] private float patrolSpeed=1.1f;
        [SerializeField,Min(.1f)] private float pursuitSpeed=2.5f;
        [SerializeField,Min(.1f)] private float sightRange=6f;
        [SerializeField,Range(10,180)] private float sightAngle=100f;
        [SerializeField,Min(.01f)] private float detectionTime=.35f;
        [SerializeField,Min(.1f)] private float investigationTime=3f;
        [SerializeField,Min(.1f)] private float attackRange=1.8f;
        [SerializeField,Min(.1f)] private float attackWindup=.65f;
        [SerializeField,Min(.1f)] private float attackRecovery=1.6f;
        [SerializeField,Min(.1f)] private float stunDuration=.8f;
        [SerializeField,Min(0)] private float patrolPause=.6f;
        private readonly RaycastHit2D[] hits=new RaycastHit2D[64];
        private readonly Collider2D[] overlaps=new Collider2D[64];
        private Rigidbody2D body;
        private BoxCollider2D footprint;
        private SecurityBotBrain brain;
        private Sec0Navigator navigator;
        private Transform target;
        private PlayerStunReceiver receiver;
        private InputPolicyCoordinator policy;
        private Func<bool> targetAvailable;
        private object owner;
        private Vector2 facing=Vector2.down,lastKnown,chargeDirection;
        private int waypoint;
        private float dwell;
        private string animationName;
        private readonly System.Collections.Generic.List<GameObject> effects=new();
        public SecurityBotState State=>brain?.State??SecurityBotState.Patrol;
        public Vector2 Facing=>facing;
        public bool HasBinding=>owner!=null;
        public bool IsDefeated { get; private set; }
        public int ActivePulseCount=>effects.Count;
        public int ShotsFired { get; private set; }
        public int StunsLanded { get; private set; }
        public void Configure(Animator animator,GameObject effect,Transform[] route)
        { visual=animator;stunFxPrefab=effect;patrolPoints=route??Array.Empty<Transform>(); }
        public void ValidateAuthoring()
        {
            var damage=GetComponent<Sec0DamageRecipient>();
            if(damage==null) throw new InvalidOperationException("SEC-0 requires its damage recipient.");
            damage.ValidateAuthoring();
            var physics=GetComponent<Rigidbody2D>();var shape=GetComponent<BoxCollider2D>();
            if(physics==null || physics.bodyType!=RigidbodyType2D.Kinematic || shape==null || shape.isTrigger
                || !Positive(shape.size.x) || !Positive(shape.size.y) || !Finite(shape.offset.x) || !Finite(shape.offset.y)
                || visual==null || visual.runtimeAnimatorController==null || !visual.transform.IsChildOf(transform) || stunFxPrefab==null
                || patrolPoints==null || patrolPoints.Length<2 || Array.Exists(patrolPoints,p=>p==null || p.gameObject.scene!=gameObject.scene)
                || !Positive(patrolSpeed) || !Positive(pursuitSpeed) || !Positive(sightRange) || !Positive(sightAngle) || sightAngle>180
                || !Positive(detectionTime) || !Positive(investigationTime) || !Positive(attackRange) || attackRange>sightRange
                || !Positive(attackWindup) || !Positive(attackRecovery) || !Positive(stunDuration) || patrolPause<0 || !Finite(patrolPause))
                throw new InvalidOperationException("SEC-0 requires valid same-scene patrol points, visuals, effects and tuning.");
        }
        private static bool Finite(float f)=>!float.IsNaN(f) && !float.IsInfinity(f);
        private static bool Positive(float f)=>Finite(f) && f>0;
        public void Bind(object bindingOwner,Transform player,PlayerStunReceiver stun,InputPolicyCoordinator gameplay,Func<bool> available)
        {
            if(bindingOwner==null || player==null || stun==null || gameplay==null || available==null) throw new ArgumentNullException(nameof(player));
            if(owner!=null && !ReferenceEquals(owner,bindingOwner)) throw new InvalidOperationException("SEC-0 already owned.");
            Release(bindingOwner);ValidateAuthoring();
            if(IsDefeated) return;
            body=GetComponent<Rigidbody2D>();footprint=GetComponent<BoxCollider2D>();
            brain=new SecurityBotBrain(detectionTime,investigationTime,attackWindup,attackRecovery,attackRange);
            navigator=new Sec0Navigator(ClearTravel);owner=bindingOwner;target=player;receiver=stun;policy=gameplay;targetAvailable=available;
            lastKnown=body.position;waypoint=0;dwell=0;facing=Vector2.down;ShotsFired=0;StunsLanded=0;
            Play(false);
        }
        private Vector2 TargetPoint => receiver.GetComponent<BoxCollider2D>() is BoxCollider2D collider ? (Vector2)collider.bounds.center : (Vector2)target.position;
        private ContactFilter2D Solids=>new ContactFilter2D{useLayerMask=true,layerMask=solidLayers,useTriggers=false};
        private bool Ignore(Collider2D collider)=>collider==null || collider.transform.IsChildOf(transform)
            || (target!=null && collider.transform.IsChildOf(target));
        public bool CanSeeTarget()
        {
            if(IsDefeated || target==null || targetAvailable==null || !targetAvailable()) return false;
            Vector2 delta=(Vector2)target.position-body.position;
            if(delta.sqrMagnitude>sightRange*sightRange) return false;
            bool tracking=State==SecurityBotState.Pursuing || State==SecurityBotState.Charging || State==SecurityBotState.Recovering;
            if(!tracking && delta.sqrMagnitude>.001f && Vector2.Angle(facing,delta)>sightAngle*.5f) return false;
            return ClearSight(body.position+new Vector2(0,.25f),TargetPoint);
        }
        private bool ClearSight(Vector2 from,Vector2 to)
        {
            Vector2 delta=to-from;int count=Physics2D.Raycast(from,delta.normalized,Solids,hits,delta.magnitude);
            if(count==hits.Length) return false;
            for(int i=0;i<count;i++) if(!Ignore(hits[i].collider)) return false;
            return true;
        }
#if UNITY_EDITOR
        internal int NavigationOverlapQueries { get; private set; }
#endif
        private bool FreeAt(Vector2 position,bool ignoreTarget)
        {
#if UNITY_EDITOR
            if(ignoreTarget) NavigationOverlapQueries++;
#endif
            int count=Physics2D.OverlapBox(position+footprint.offset,footprint.size+new Vector2(.008f,.008f),0,Solids,overlaps);
            if(count==overlaps.Length) return false;
            for(int i=0;i<count;i++)
            {
                var collider=overlaps[i];
                if(collider!=null && !collider.transform.IsChildOf(transform)
                    && !(ignoreTarget && target!=null && collider.transform.IsChildOf(target))) return false;
            }
            return true;
        }
        private bool ClearTravel(Vector2 from,Vector2 to)
        {
            Vector2 delta=to-from;
            // The authored box is 0.6x0.34 with centre 0.17 above floor contact.
            int count=Physics2D.BoxCast(from+footprint.offset,footprint.size,0,delta.normalized,Solids,hits,delta.magnitude);
            if(count==hits.Length) return false;
            for(int i=0;i<count;i++) if(!Ignore(hits[i].collider)) return false;
            // Sample the swept footprint as well: a tangent corner can be
            // omitted by a shape cast but must not be treated as a direct route.
            int samples=Mathf.Max(1,Mathf.CeilToInt(delta.magnitude/.25f));
            for(int i=1;i<=samples;i++) if(!FreeAt(Vector2.Lerp(from,to,(float)i/samples),true)) return false;
            return true;
        }
        private void FixedUpdate()=>Tick(Time.fixedDeltaTime);
        public void Tick(float elapsed)
        {
            if(IsDefeated || owner==null || !isActiveAndEnabled || !Finite(elapsed) || elapsed<=0) return;
#if UNITY_EDITOR
            NavigationOverlapQueries=0;
#endif
            effects.RemoveAll(e=>e==null);
            if(visual!=null) visual.speed=policy.Gameplay && targetAvailable()?1:0;
            if(!policy.Gameplay || !targetAvailable())
            {
                body.linearVelocity=Vector2.zero;Play(false);
                if(!targetAvailable()) {brain.Reset();navigator.Reset();}
                return;
            }
            bool visible=CanSeeTarget();if(visible) lastKnown=target.position;
            var previous=State;
            bool fire=brain.Advance(elapsed,visible,Vector2.Distance(body.position,target.position),Vector2.Distance(body.position,lastKnown)<.2f,true);
            if(State==SecurityBotState.Charging && previous!=State)
            {chargeDirection=(TargetPoint-(body.position+new Vector2(0,.25f))).normalized;Face(chargeDirection);}
            if(fire) Fire();
            if(State==SecurityBotState.Investigating && visible) Face((Vector2)target.position-body.position);
            Vector2 destination=body.position;
            if(State==SecurityBotState.Patrol)
            {
                if(Vector2.Distance(body.position,patrolPoints[waypoint].position)<.16f)
                {
                    dwell+=elapsed;
                    if(dwell>=patrolPause) {waypoint=(waypoint+1)%patrolPoints.Length;dwell=0;}
                }
                else {dwell=0;destination=patrolPoints[waypoint].position;}
            }
            else if(State==SecurityBotState.Pursuing) destination=lastKnown;
            else if(State==SecurityBotState.Investigating && !visible) destination=lastKnown;
            navigator.SetDestination(destination,.4f);
            Vector2 next=navigator.Next(body.position,elapsed),delta=next-body.position;
            bool moving=delta.sqrMagnitude>.0004f;
            if(moving)
            {
                Face(delta);Vector2 step=delta.normalized*Mathf.Min(delta.magnitude,(State==SecurityBotState.Pursuing?pursuitSpeed:patrolSpeed)*elapsed);
                int count=body.Cast(step.normalized,Solids,hits,step.magnitude+.02f);float distance=step.magnitude;
                for(int i=0;i<count;i++) distance=Mathf.Min(distance,Mathf.Max(0,hits[i].distance-.02f));
                if(count==hits.Length) distance=0;
                // Shape casts can graze a corner within physics contact tolerance.
                // Reject an occupied endpoint as well, preserving a small skin.
                if(!FreeAt(body.position+step.normalized*distance,false))
                {
                    float safe=0,blocked=distance;
                    for(int i=0;i<8;i++) {float mid=(safe+blocked)*.5f;if(FreeAt(body.position+step.normalized*mid,false))safe=mid;else blocked=mid;}
                    distance=safe;navigator.InvalidatePath();
                }
                body.MovePosition(body.position+step.normalized*distance);moving=distance>.001f;
            }
            else body.linearVelocity=Vector2.zero;
            Play(moving);
        }
        private void Face(Vector2 delta)
        { if(delta.sqrMagnitude>.001f) facing=Mathf.Abs(delta.x)>Mathf.Abs(delta.y)?new Vector2(Mathf.Sign(delta.x),0):new Vector2(0,Mathf.Sign(delta.y)); }
        private void Fire()
        {
            if(IsDefeated || owner==null) return;
            ShotsFired++;
            Vector2 from=body.position+new Vector2(0,.25f);
            var fx=Instantiate(stunFxPrefab,from+(chargeDirection*.35f),Quaternion.Euler(0,0,Mathf.Atan2(chargeDirection.y,chargeDirection.x)*Mathf.Rad2Deg));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(fx,gameObject.scene);effects.Add(fx);Destroy(fx,.4f);
            // Aimed direction is locked when charging begins, so stepping sideways
            // during the warning can dodge the non-lethal pulse.
            int count=Physics2D.Raycast(from,chargeDirection,Solids,hits,attackRange);
            float nearest=float.PositiveInfinity;Collider2D hit=null;
            if(count==hits.Length) return;
            for(int i=0;i<count;i++)
                if(hits[i].collider!=null && !hits[i].collider.transform.IsChildOf(transform) && hits[i].distance<nearest)
                {nearest=hits[i].distance;hit=hits[i].collider;}
            if(hit!=null && hit.transform.IsChildOf(target) && receiver.TryStun(stunDuration)) StunsLanded++;
        }
        private void Play(bool moving)
        {
            if(visual==null || !visual.isActiveAndEnabled) return;
            string direction=facing.x<0?"west":facing.x>0?"east":facing.y>0?"north":"south";
            string state=State==SecurityBotState.Patrol?"routine":State==SecurityBotState.Investigating?"alert":"hostile";
            string clip=State==SecurityBotState.Charging?"Charge_"+direction:State==SecurityBotState.Investigating && !moving?"AlertBlink_"+direction:(moving?"Move_":"Idle_")+state+"_"+direction;
            if(animationName==clip) return;
            int hash=Animator.StringToHash("Base Layer."+clip);
            if(visual.HasState(0,hash)) {visual.Play(hash,0,0);animationName=clip;}
            else Debug.LogError("SEC-0 animation state missing: "+clip,this);
        }
        public void Release(object bindingOwner)
        {
            if(!ReferenceEquals(owner,bindingOwner)) return;
            owner=null;target=null;receiver=null;policy=null;targetAvailable=null;
            if(body!=null) body.linearVelocity=Vector2.zero;
            brain?.Reset();navigator?.Reset();animationName=null;if(visual!=null) visual.speed=1;
            foreach(var effect in effects) if(effect!=null) {effect.SetActive(false);Destroy(effect);}effects.Clear();
            Play(false);
        }
        internal void Defeat()
        {
            if(IsDefeated) return;
            IsDefeated=true;
            Release(owner);
            body??=GetComponent<Rigidbody2D>();
            body.linearVelocity=Vector2.zero;body.angularVelocity=0;body.simulated=false;
            foreach(var shape in GetComponentsInChildren<Collider2D>(true)) shape.enabled=false;
            if(visual!=null) visual.speed=0;
        }
        private void OnDisable() { if(body!=null) body.linearVelocity=Vector2.zero; }
        private void OnDestroy() { if(owner!=null) Release(owner); }
        private void OnDrawGizmosSelected()
        {
            Gizmos.color=Color.cyan;Gizmos.DrawWireSphere(transform.position,sightRange);
            Gizmos.color=new Color(1,.25f,.2f);Gizmos.DrawWireSphere(transform.position,attackRange);
            if(patrolPoints==null) return;
            Gizmos.color=new Color(1,.7f,.2f);
            for(int i=0;i<patrolPoints.Length;i++) if(patrolPoints[i]!=null && patrolPoints[(i+1)%patrolPoints.Length]!=null)
            {Gizmos.DrawWireSphere(patrolPoints[i].position,.12f);Gizmos.DrawLine(patrolPoints[i].position,patrolPoints[(i+1)%patrolPoints.Length].position);}
        }
    }
}
