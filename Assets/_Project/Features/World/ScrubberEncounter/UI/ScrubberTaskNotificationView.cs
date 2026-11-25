using UnityEngine;
namespace SOLITUDE.World.ScrubberEncounter
{
    public sealed class ScrubberTaskNotificationView : MonoBehaviour
    {
        [SerializeField] private ScrubberEquipment equipment;
        [SerializeField] private ScrubberScreenView screen;
        [SerializeField] private GameObject notice;
        private bool observedCompletion, queued, presented;
        private float remaining;
        public bool IsVisible => notice != null && notice.activeInHierarchy;
        public int PresentationCount { get; private set; }
        private void OnEnable()
        {
            observedCompletion=equipment?.State.Operational==true;
            if(equipment!=null) equipment.State.Changed+=Changed;
            if(notice!=null) notice.SetActive(false);
        }
        private void Changed()
        {
            if(equipment?.State.Operational!=true || observedCompletion) return;
            observedCompletion=true; queued=true; remaining=4;
        }
        private void Update()
        {
            if(!queued || notice==null) return;
            if(screen!=null && screen.IsOpen) { notice.SetActive(false); return; }
            if(!presented) { presented=true; PresentationCount++; }
            notice.SetActive(true); remaining-=Time.unscaledDeltaTime;
            if(remaining<=0) { notice.SetActive(false); queued=false; }
        }
        private void OnDisable()
        {
            if(equipment!=null) equipment.State.Changed-=Changed;
            queued=false; remaining=0;
            if(notice!=null) notice.SetActive(false);
        }
    }
}
