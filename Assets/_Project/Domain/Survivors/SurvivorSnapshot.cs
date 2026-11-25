using System;
using System.IO;

namespace SOLITUDE.Survivors
{
    /// <summary>Detached save data. Scene binding supplies authored values only for a new colonist.</summary>
    [Serializable]
    public sealed class SurvivorSnapshot
    {
        public string id;
        public SurvivorLifeStatus status = SurvivorLifeStatus.Active;
        public string scenePath;
        public double x, y, z;
        public SavedVital health = new SavedVital();
        public SavedVital oxygen = new SavedVital();
        public SavedVital hunger = new SavedVital();
        public double recoveryDelayRemaining;

        public SurvivorSnapshot Clone() => new SurvivorSnapshot
        {
            id = id, status = status, scenePath = scenePath, x = x, y = y, z = z,
            health = health?.Clone(), oxygen = oxygen?.Clone(), hunger = hunger?.Clone(),
            recoveryDelayRemaining = recoveryDelayRemaining
        };

        public void Validate()
        {
            SnapshotValidation.Identity(id);
            if (!Enum.IsDefined(typeof(SurvivorLifeStatus), status))
                throw new InvalidDataException("Invalid survivor status.");
            if (string.IsNullOrWhiteSpace(scenePath) || !scenePath.StartsWith("Assets/", StringComparison.Ordinal)
                || !scenePath.EndsWith(".unity", StringComparison.Ordinal) || scenePath.Contains("\\")
                || scenePath.Contains("//") || scenePath.Contains("/../") || scenePath.Contains("/./"))
                throw new InvalidDataException("Invalid saved scene path.");
            SnapshotValidation.Coordinate(x); SnapshotValidation.Coordinate(y); SnapshotValidation.Coordinate(z);
            if (health == null || oxygen == null || hunger == null) throw new InvalidDataException("Missing saved vitals.");
            health.Validate(); oxygen.Validate(); hunger.Validate();
            SnapshotValidation.Nonnegative(recoveryDelayRemaining);
        }
    }

    [Serializable]
    public sealed class SavedVital
    {
        public double current;
        public double max;
        public SavedVital Clone() => new SavedVital { current = current, max = max };
        public void Validate()
        {
            SnapshotValidation.Nonnegative(current); SnapshotValidation.Nonnegative(max);
            if (max <= 0 || current > max) throw new InvalidDataException("Invalid saved vital range.");
        }
    }

    internal static class SnapshotValidation
    {
        internal static void Identity(string id)
        {
            if (!Guid.TryParse(id, out var value) || value == Guid.Empty)
                throw new InvalidDataException("Invalid lifecycle identity.");
        }
        internal static void Key(string key)
        {
            if (string.IsNullOrWhiteSpace(key) || key.Trim() != key)
                throw new InvalidDataException("Missing or invalid saved world identity.");
        }
        internal static void Nonnegative(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
                throw new InvalidDataException("Invalid saved numeric value.");
        }
        internal static void Coordinate(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || Math.Abs(value) > float.MaxValue)
                throw new InvalidDataException("Invalid saved coordinate.");
        }
    }
}
