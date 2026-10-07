using TMPro;
using UnityEngine;
namespace SOLITUDE.World.ScrubberEncounter
{
    public sealed class ScrubberRoomFeedback : MonoBehaviour
    {
        [SerializeField] private AudioSource alert;
        [SerializeField] private AudioSource runningSound;
        [SerializeField] private TMP_Text warning;
        private ScrubberEquipment equipment;
        public bool IsWarningVisible => warning != null && warning.gameObject.activeInHierarchy;
        public AudioSource Alert => alert;
        public AudioSource RunningSound => runningSound;
        public void Configure(AudioSource source, TMP_Text label) { alert = source; warning = label; }
        private void OnEnable()
        {
            equipment=GetComponent<ScrubberEquipment>();
            if(equipment!=null) equipment.State.Changed+=Reconcile;
            Reconcile();
        }
        private void Reconcile()
        {
            var state=equipment?.State;
            if(warning!=null) { warning.text="CO₂ "+(state?.RoomCondition ?? "ELEVATED"); warning.gameObject.SetActive(state?.Operational!=true); }
            if(alert!=null && alert.clip!=null) { if(state?.Operational==true) alert.Stop(); else if(!alert.isPlaying) alert.Play(); }
            if(runningSound!=null && runningSound.clip!=null) { if(state?.Running==true) { if(!runningSound.isPlaying) runningSound.Play(); } else runningSound.Stop(); }
            var indicator=GetComponent<SpriteRenderer>();
            if(indicator!=null) indicator.color=state?.Running==true ? new Color(.3f,.85f,.42f) : new Color(.9f,.26f,.24f);
        }
        private void OnDisable()
        {
            if(equipment!=null) equipment.State.Changed-=Reconcile;
            if(alert!=null) alert.Stop(); if(runningSound!=null) runningSound.Stop();
        }
    }
}
