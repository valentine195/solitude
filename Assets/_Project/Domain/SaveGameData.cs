using System;
using System.Collections.Generic;

namespace SOLITUDE.SaveLoad
{
    [Serializable]
    public class SaveGameData
    {
        public const int CurrentVersion = 2;

        public int version = CurrentVersion;
        public int worldSeed;
        public List<ContainerSaveRecord> containers = new();
        public List<string> collectedPickupIds = new();

        public static SaveGameData Migrate(SaveGameData data)
        {
            if (data == null || data.version < 1 || data.version > CurrentVersion)
                throw new System.IO.InvalidDataException("Unsupported or invalid save version.");
            if (data.version == 1)
            {
                // Version 1 did not record world collections; they cannot be reconstructed.
                data.collectedPickupIds = new List<string>();
                data.version = CurrentVersion;
            }
            data.collectedPickupIds ??= new List<string>();
            foreach (var id in data.collectedPickupIds)
                if (string.IsNullOrWhiteSpace(id))
                    throw new System.IO.InvalidDataException("A collected pickup identity is empty.");
            data.collectedPickupIds = new List<string>(new HashSet<string>(data.collectedPickupIds, StringComparer.Ordinal));
            data.collectedPickupIds.Sort(StringComparer.Ordinal);
            return data;
        }
    }

    [Serializable]
    public class ContainerSaveRecord
    {
        public string saveableId;
        public ContainerSaveData state;
    }
}
