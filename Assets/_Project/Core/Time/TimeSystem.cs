using SOLITUDE.Application;
using UnityEngine;
namespace SOLITUDE.Core.Systems
{
    public class TimeSystem : MonoBehaviour
    {
        private PauseCoordinator pause;
        public float CurrentTimeScale => pause?.Scale ?? 1;
        public bool IsPaused => CurrentTimeScale == 0;
        public void Initialize(PauseCoordinator pause) { Release(); this.pause = pause; pause.Changed += Apply; Apply(); }
        private void Apply() => Time.timeScale = pause.Scale;
        public void SetTimeScale(float scale) => pause?.SetBaseScale(scale);
        public void SlowMotion(float scale = 0.2f) => SetTimeScale(scale);
        public void Release() { if (pause != null) pause.Changed -= Apply; pause = null; Time.timeScale = 1; }
    }
}
