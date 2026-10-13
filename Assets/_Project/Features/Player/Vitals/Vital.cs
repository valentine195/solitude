using System;

namespace SOLITUDE.Player.Vitals
{
    public sealed class Vital
    {
        // One authoritative value, exposed as float for the existing gameplay/UI API.
        private double maximum, current;
        public float Max => (float)maximum;
        public float Current => (float)current;
        public float Normalized => maximum <= 0 ? 0 : (float)(current / maximum);
        internal double PreciseMax => maximum;
        internal double PreciseCurrent => current;

        public event Action<Vital> Changed;
        public event Action<Vital> Depleted; // fires once, on the frame it hits 0
        public event Action<Exception> ObserverError;

        private bool _wasDepleted;
        private int publicationDepth;

        internal readonly struct Change
        {
            public bool Changed { get; }
            public bool Depleted { get; }
            public Change(bool changed, bool depleted) { Changed = changed; Depleted = depleted; }
        }

        public Vital(float max, float? startingCurrent = null)
        {
            maximum = max;
            current = startingCurrent ?? max;
        }

        public void Set(float value)
        {
            if (publicationDepth != 0 || float.IsNaN(value) || float.IsInfinity(value)) return;
            Publish(ApplySilently(maximum, Clamp(value, maximum)));
        }

        public void Add(float delta)
        {
            if (publicationDepth != 0 || float.IsNaN(delta) || float.IsInfinity(delta)) return;
            Publish(ApplySilently(maximum, Clamp(current + delta, maximum)));
        }

        public void SetMax(float newMax, bool clampCurrent = true)
        {
            if (publicationDepth != 0 || float.IsNaN(newMax) || float.IsInfinity(newMax) || newMax < 0) return;
            Publish(ApplySilently(newMax, clampCurrent ? Clamp(current, newMax) : current));
        }

        private static double Clamp(double value, double max) => value < 0 ? 0 : value > max ? max : value;

        // Trusted prepared mutation: no callbacks or user code inside this boundary.
        internal Change ApplySilently(double max, double value)
        {
            bool currentChanged = value != current;
            bool changed = currentChanged || max != maximum;
            bool depleted = currentChanged && value <= 0 && !_wasDepleted;
            maximum = max; current = value;
            if (value > 0) _wasDepleted = false;
            else if (depleted) _wasDepleted = true;
            return new Change(changed, depleted);
        }
        internal void BeginPublication() => publicationDepth++;
        internal void EndPublication() => publicationDepth--;
        internal void Publish(Change change)
        {
            BeginPublication();
            try
            {
                if (change.Changed) Invoke(Changed);
                if (change.Depleted) Invoke(Depleted);
            }
            finally { EndPublication(); }
        }
        private void Invoke(Action<Vital> observers)
        {
            if (observers == null) return;
            foreach (Action<Vital> observer in observers.GetInvocationList())
                try { observer(this); }
                catch (Exception error)
                {
                    if (ObserverError == null) continue;
                    foreach (Action<Exception> diagnostic in ObserverError.GetInvocationList())
                        try { diagnostic(error); } catch { }
                }
        }
    }
}
