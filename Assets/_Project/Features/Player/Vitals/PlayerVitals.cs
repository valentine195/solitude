using SOLITUDE.Player.Vitals;

namespace SOLITUDE.Vitals
{
    public sealed class PlayerVitals
    {
        public Vital Health { get; }
        public Vital Oxygen { get; }
        public Vital Hunger { get; }

        public PlayerVitals(float maxHealth, float maxOxygen, float maxHunger = 100f)
        {
            Health = new Vital(maxHealth);
            Oxygen = new Vital(maxOxygen);
            Hunger = new Vital(maxHunger);
        }
    }
}
