using System;

namespace SOLITUDE.Survivors
{
    public enum SurvivorLifeStatus { Pending, Active, Dead }

    // Identity outlives the avatar representing this survivor. Death is terminal.
    public sealed class SurvivorState
    {
        public Guid Id { get; }
        public SurvivorLifeStatus Status { get; private set; }

        public SurvivorState() : this(Guid.NewGuid()) { }
        public SurvivorState(Guid id)
        {
            if (id == Guid.Empty) throw new ArgumentException("Survivor identity is required.", nameof(id));
            Id = id;
        }

        public bool TryActivate()
        {
            if (Status != SurvivorLifeStatus.Pending) return false;
            Status = SurvivorLifeStatus.Active;
            return true;
        }

        public bool TryRetire()
        {
            if (Status != SurvivorLifeStatus.Active) return false;
            Status = SurvivorLifeStatus.Dead;
            return true;
        }
    }
}
