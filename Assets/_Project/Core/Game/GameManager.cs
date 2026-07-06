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

        [Header("Game State")]
        public bool IsPaused { get; private set; }

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

        public void PauseGame()
        {
            IsPaused = true;
            Time.timeScale = 0f;
        }

        public void ResumeGame()
        {
            IsPaused = false;
            Time.timeScale = 1f;
        }

        public void TogglePause()
        {
            if (IsPaused) ResumeGame();
            else PauseGame();
        }
    }
}