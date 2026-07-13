using System;
using System.Collections.Generic;

namespace SOLITUDE.SaveLoad
{
    [Serializable]
    public class SaveGameData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public int worldSeed;
        public List<ContainerSaveRecord> containers = new();
    }

    [Serializable]
    public class ContainerSaveRecord
    {
        public string saveableId;
        public ContainerSaveData state;
    }
}
