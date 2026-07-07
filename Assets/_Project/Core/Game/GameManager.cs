using SOLITUDE.Core.Input;
using UnityEngine;

namespace SOLITUDE.Core.Systems
{
    /// <summary>
    /// Root bootstrap for SOLITUDE runtime systems.
    /// Keep this extremely lightweight.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private TimeSystem timeSystem;

        [Header("Game State")]
        public bool IsPaused => timeSystem.IsPaused;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Start()
        {
            InputRouter.Instance.SetMode(InputMode.Gameplay);
        }

        public void PauseGame()
        {
            timeSystem.Pause();
        }

        public void ResumeGame()
        {
            timeSystem.Resume();
        }

        public void TogglePause()
        {
            if (IsPaused) ResumeGame();
            else PauseGame();
        }
    }
}