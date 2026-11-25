namespace SOLITUDE.SaveLoad
{
    /// <summary>
    /// Derives a per-container seed from one shared world seed and that
    /// container's own SaveableId, so each container's first-ever roll is
    /// reproducible independent of scene-load order or how many other
    /// containers exist in the level. Sharing one sequential System.Random
    /// across every container instead would mean adding a single new locker
    /// earlier in load order silently reshuffles every other container's
    /// contents - a load-order dependency that's miserable to reason about
    /// or reproduce a bug against.
    ///
    /// Pure static function, no Unity dependency - directly NUnit-testable:
    /// same (worldSeed, saveableId) pair always produces the same int.
    /// </summary>
    public static class SeedUtility
    {
        public static int DeriveSeed(int worldSeed, string saveableId)
        {
            if (string.IsNullOrEmpty(saveableId))
                return worldSeed;

            unchecked
            {
                int hash = worldSeed;
                foreach (char c in saveableId)
                    hash = hash * 31 + c;
                return hash;
            }
        }
    }
}