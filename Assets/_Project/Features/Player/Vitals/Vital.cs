using System;

namespace SOLITUDE.Player.Vitals
{
    public sealed class Vital
    {
        public float Max { get; private set; }
        public float Current { get; private set; }
        public float Normalized => Max <= 0f ? 0f : Current / Max;

        public event Action<Vital> Changed;
        public event Action<Vital> Depleted; // fires once, on the frame it hits 0

        private bool _wasDepleted;

        public Vital(float max, float? startingCurrent = null)
        {
            Max = max;
            Current = startingCurrent ?? max;
        }

        public void Set(float value)
        {
            var clamped = value < 0f ? 0f : (value > Max ? Max : value);
            if (clamped == Current) return;

            Current = clamped;
            Changed?.Invoke(this);

            if (Current <= 0f && !_wasDepleted)
            {
                _wasDepleted = true;
                Depleted?.Invoke(this);
            }
            else if (Current > 0f)
            {
                _wasDepleted = false;
            }
        }

        public void Add(float delta) => Set(Current + delta);

        public void SetMax(float newMax, bool clampCurrent = true)
        {
            Max = newMax;
            if (clampCurrent) Set(Current);
        }
    }
}