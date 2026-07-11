using SOLITUDE.Player.Vitals;

namespace SOLITUDE.Vitals
{
    public sealed class PlayerVitals
    {
        public Vital Health { get; }
        public Vital Oxygen { get; }

        public PlayerVitals(float maxHealth, float maxOxygen)
        {
            Health = new Vital(maxHealth);
            Oxygen = new Vital(maxOxygen);
        }
    }
}