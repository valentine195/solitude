using UnityEngine;
using SOLITUDE.Vitals;

namespace SOLITUDE.Player
{
    public class PlayerVitalsController : MonoBehaviour
    {
        private static PlayerVitalsController _instance;
        public static PlayerVitalsController Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindAnyObjectByType<PlayerVitalsController>();
                return _instance;
            }
        }

        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float maxOxygen = 100f;

        [SerializeField] private float currentHealth = 100f;
        [SerializeField] private float currentOxygen = 100f;

        public PlayerVitals Vitals { get; private set; }

        public void ResetForAwakening()
        {
            if (Vitals == null) Vitals = new PlayerVitals(maxHealth, maxOxygen);
            // Retain the Vital objects to preserve HUD subscriptions.
            Vitals.Health.Set(currentHealth);
            Vitals.Oxygen.Set(currentOxygen);
        }

        private void OnValidate()
        {
            if (Vitals != null)
            {
                Vitals.Health.Set(currentHealth);
                Vitals.Oxygen.Set(currentOxygen);
            }
        }

        private void Awake()
        {
            if (Vitals == null) ResetForAwakening();
        }
    }

}