using System;
using SOLITUDE.Composition;
using SOLITUDE.Player;
using UnityEngine;
namespace SOLITUDE.Npcs.Sec0
{
    [DisallowMultipleComponent]
    public sealed class Sec0Composition : SceneFeatureComposition
    {
        [SerializeField] private Sec0Controller[] bots=Array.Empty<Sec0Controller>();
        [SerializeField] private PlayerStunReceiver player;
        [SerializeField] private PlayerVitalsController vitals;
        private bool initialized;
        public void Configure(Sec0Controller[] units,PlayerStunReceiver target,PlayerVitalsController health)
        { Release();bots=units;player=target;vitals=health; }
        public override void Validate()
        {
            if(bots==null || bots.Length==0 || player==null || vitals==null || player.gameObject!=vitals.gameObject
                || player.gameObject.scene!=gameObject.scene) throw new InvalidOperationException("SEC-0 target bindings are incomplete.");
            var unique=new System.Collections.Generic.HashSet<Sec0Controller>();
            foreach(var bot in bots)
            {if(bot==null || bot.gameObject.scene!=gameObject.scene || !unique.Add(bot)) throw new InvalidOperationException("Invalid SEC-0 scene units.");bot.ValidateAuthoring();}
        }
        public override void Initialize(SceneFeatureContext context)
        {
            Release();Validate();
            if(context?.Policy==null || context.Interactor==null || context.Interactor.gameObject!=player.gameObject)
                throw new InvalidOperationException("SEC-0 must bind the scene's composed local player.");
            initialized=true;
            try
            {
                player.Bind(this,context.Policy,Available);
                if(player.GetComponent<PlayerDamageRecipient>()==null) player.gameObject.AddComponent<PlayerDamageRecipient>();
                foreach(var bot in bots)
                {
                    bot.Bind(this,player.transform,player,context.Policy,Available);
                    bot.GetComponent<Sec0DamageRecipient>().Bind(this,context.Policy);
                }
            }
            catch {Release();throw;}
        }
        private bool Available()=>initialized && isActiveAndEnabled && player!=null && player.isActiveAndEnabled
            && vitals!=null && vitals.isActiveAndEnabled && vitals.Vitals!=null && vitals.Vitals.Health.PreciseCurrent>0;
        public override bool TryBack()=>false;
        public override void CloseForTransition() { }
        public override void Release()
        {
            initialized=false;
            if(bots!=null) foreach(var bot in bots) if(bot!=null)
            {
                bot.GetComponent<Sec0DamageRecipient>()?.Release(this);
                bot.Release(this);
            }
            if(player!=null) player.Release(this);
        }
        private void OnDestroy()=>Release();
    }
}
