using System;
using SOLITUDE.Application;
using UnityEngine;
namespace SOLITUDE.Core.Systems
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private TimeSystem timeSystem;
        private PauseCoordinator pause;
        private IDisposable manual;
        public bool IsPaused => pause?.IsPaused ?? false;
        public void Initialize(PauseCoordinator pause) => this.pause = pause;
        public void PauseGame() { if (manual == null && pause != null) manual = pause.Acquire(); }
        public void ResumeGame() { manual?.Dispose(); manual = null; }
        public void TogglePause() { if (manual == null) PauseGame(); else ResumeGame(); }
        public void Release() { ResumeGame(); pause = null; }
    }
}
