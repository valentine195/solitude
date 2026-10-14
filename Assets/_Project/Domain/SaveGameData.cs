using System;
using System.Collections.Generic;
using System.IO;
using SOLITUDE.Survivors;
using SOLITUDE.Expeditions;

namespace SOLITUDE.SaveLoad
{
    [Serializable]
    public class SaveGameData
    {
        public const int CurrentVersion = 3;

        public int version = CurrentVersion;
        public int worldSeed;
        public List<ContainerSaveRecord> containers = new();
        public List<string> collectedPickupIds = new();
        // Null together until authored scene binding initializes the first colonist/expedition.
        public SurvivorSnapshot survivor;
        public ExpeditionState expedition;
        public ShipStateSnapshot ship = new ShipStateSnapshot();
        public SavedDeathTransition pendingDeath;

        public static SaveGameData Migrate(SaveGameData data)
        {
            if (data == null || data.version < 1 || data.version > CurrentVersion)
                throw new System.IO.InvalidDataException("Unsupported or invalid save version.");
            if (data.version == 1)
            {
                // Version 1 did not record world collections; they cannot be reconstructed.
                data.collectedPickupIds = new List<string>();
            }
            if (data.version < 3)
            {
                // Old saves know containers/collections, never character or permanent ship facts.
                // Do not invent identities, vitals or historical repairs while decoding.
                data.survivor = null; data.expedition = null; data.pendingDeath = null;
                data.ship = new ShipStateSnapshot();
                data.version = CurrentVersion;
            }
            data.collectedPickupIds ??= new List<string>();
            foreach (var id in data.collectedPickupIds)
                if (string.IsNullOrWhiteSpace(id))
                    throw new System.IO.InvalidDataException("A collected pickup identity is empty.");
            data.collectedPickupIds = new List<string>(new HashSet<string>(data.collectedPickupIds, StringComparer.Ordinal));
            data.collectedPickupIds.Sort(StringComparer.Ordinal);
            data.ValidateLifecycle();
            return data;
        }

        public void ValidateLifecycle()
        {
            if ((survivor == null) != (expedition == null) || ship == null)
                throw new InvalidDataException("Incomplete saved lifecycle.");
            survivor?.Validate(); expedition?.Validate(); ship.Validate();
            if (pendingDeath != null)
            {
                pendingDeath.Validate(survivor, expedition);
                foreach (var record in containers ?? new List<ContainerSaveRecord>())
                    if (record != null && (record.saveableId == ContainerSaveSession.PlayerInventoryId
                        || record.saveableId == ContainerSaveSession.PlayerHotbarId) && record.state?.slots?.Count > 0)
                        throw new InvalidDataException("Pending succession cannot retain departing equipment.");
            }
            else if (survivor != null && survivor.status != SurvivorLifeStatus.Active)
                throw new InvalidDataException("A saved living colonist must be active.");
        }
    }

    [Serializable]
    public class ContainerSaveRecord
    {
        public string saveableId;
        public ContainerSaveData state;
    }
}
