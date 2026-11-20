using System;
using SOLITUDE.World.SolitudeStart;
using TMPro;
using UnityEngine;

namespace SOLITUDE.World.CrewBackgrounds
{
    [DisallowMultipleComponent]
    public sealed class CrewBackgroundsRoomFeedback : MonoBehaviour
    {
        [SerializeField] private AudioSource alert;
        [SerializeField] private AudioSource runningSound;
        [SerializeField] private TMP_Text status;
        [SerializeField] private SpriteRenderer indicator;
        [SerializeField] private SolitudeSlidingDoor returnDoor;
        [SerializeField] private GameObject corridorLight;
        public bool CorridorLightEnabled => corridorLight != null && corridorLight.activeSelf;

        public void ValidateAuthoring()
        {
            if (alert == null || alert.clip == null || runningSound == null || runningSound.clip == null
                || status == null || indicator == null || corridorLight == null || returnDoor == null)
                throw new InvalidOperationException("Crew room recovery feedback is missing an authored reference.");
        }

        internal void ResetRoom()
        {
            returnDoor.ResetClosed();
            Render(false, false, true);
        }

        internal void Render(bool airflow, bool recovered, bool corridorLit)
        {
            corridorLight.SetActive(corridorLit);
            status.text = recovered ? "AIRFLOW RESTORED" : airflow ? "AIR RECOVERING" : "NO AIRFLOW";
            indicator.color = airflow ? new Color(.55f, .95f, .65f) : Color.white;
            SetSound(alert, !recovered);
            SetSound(runningSound, airflow);
            if (recovered) returnDoor.TryOpenAutomatically();
        }

        internal void Release()
        {
            if (alert != null) alert.Stop();
            if (runningSound != null) runningSound.Stop();
        }

        private void OnDisable() => Release();

        private static void SetSound(AudioSource source, bool playing)
        {
            if (playing && !source.isPlaying) source.Play();
            else if (!playing && source.isPlaying) source.Stop();
        }
    }
}
