using UnityEngine;

namespace SOLITUDE.Core.Systems
{
    /// <summary>
    /// Centralized time control layer.
    /// Useful for pause, cutscenes, slow motion, etc.
    /// </summary>
    public class TimeSystem : MonoBehaviour
    {

        [Header("Time State")]
        public float CurrentTimeScale { get; private set; } = 1f;
        public bool IsPaused => CurrentTimeScale == 1f;

        private void Awake()
        {

        }

        public void SetTimeScale(float scale)
        {
            CurrentTimeScale = Mathf.Clamp(scale, 0f, 10f);
            Time.timeScale = CurrentTimeScale;
        }

        public void Pause()
        {
            SetTimeScale(0f);
        }

        public void Resume()
        {
            SetTimeScale(1f);
        }

        public void SlowMotion(float scale = 0.2f)
        {
            SetTimeScale(scale);
        }
    }
}