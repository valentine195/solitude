using System;
using System.Collections.Generic;
using System.IO;
using SOLITUDE.Survivors;

namespace SOLITUDE.Expeditions
{
    /// <summary>Facts for one authored expedition; binding a scene does not replace these facts.</summary>
    [Serializable]
    public sealed class ExpeditionState
    {
        public string id;
        public List<string> defeatedEncounterIds = new List<string>();
        public List<string> collectedSupplySourceIds = new List<string>();

        public ExpeditionState Clone() => new ExpeditionState
        {
            id = id,
            defeatedEncounterIds = defeatedEncounterIds == null ? null : new List<string>(defeatedEncounterIds),
            collectedSupplySourceIds = collectedSupplySourceIds == null ? null : new List<string>(collectedSupplySourceIds)
        };
        public void Validate()
        {
            SnapshotValidation.Identity(id);
            ValidateKeys(defeatedEncounterIds); ValidateKeys(collectedSupplySourceIds);
        }
        internal static void ValidateKeys(List<string> keys)
        {
            if (keys == null) throw new InvalidDataException("Missing saved world facts.");
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var key in keys)
            {
                SnapshotValidation.Key(key);
                if (!seen.Add(key)) throw new InvalidDataException("Duplicate saved world identity.");
            }
        }
    }

    [Serializable]
    public sealed class ShipStateSnapshot
    {
        public List<RepairSaveRecord> repairs = new List<RepairSaveRecord>();
        public List<DoorSaveRecord> doors = new List<DoorSaveRecord>();
        public List<CriticalSourceSaveRecord> criticalSources = new List<CriticalSourceSaveRecord>();
        public List<string> permanentDiscoveryIds = new List<string>();

        public ShipStateSnapshot Clone() => new ShipStateSnapshot
        {
            repairs = repairs?.ConvertAll(record => record?.Clone()),
            doors = doors?.ConvertAll(record => record?.Clone()),
            criticalSources = criticalSources?.ConvertAll(record => record?.Clone()),
            permanentDiscoveryIds = permanentDiscoveryIds == null ? null : new List<string>(permanentDiscoveryIds)
        };
        public void Validate()
        {
            if (repairs == null || doors == null || criticalSources == null)
                throw new InvalidDataException("Missing ship state records.");
            var repaired = new HashSet<string>(StringComparer.Ordinal);
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var record in repairs)
            {
                if (record == null) throw new InvalidDataException("Missing repair record.");
                AddId(ids, record.id);
                SnapshotValidation.Nonnegative(record.recoverySeconds);
                if ((!record.installed && record.running) || (!record.running && record.recoverySeconds != 0))
                    throw new InvalidDataException("Contradictory repair state.");
                if (record.installed) repaired.Add(record.id);
            }
            ids.Clear();
            foreach (var record in doors)
            {
                if (record == null) throw new InvalidDataException("Missing door record.");
                AddId(ids, record.id);
                if (record.open && !record.unlocked) throw new InvalidDataException("An open shortcut must be unlocked.");
            }
            ids.Clear();
            foreach (var record in criticalSources)
            {
                if (record == null) throw new InvalidDataException("Missing critical source record.");
                AddId(ids, record.id); SnapshotValidation.Key(record.objectiveId);
                if (record.available && repaired.Contains(record.objectiveId))
                    throw new InvalidDataException("A completed objective cannot replenish its component.");
            }
            ExpeditionState.ValidateKeys(permanentDiscoveryIds);
        }
        private static void AddId(HashSet<string> ids, string id)
        {
            SnapshotValidation.Key(id);
            if (!ids.Add(id)) throw new InvalidDataException("Duplicate saved ship identity.");
        }
    }

    [Serializable]
    public sealed class RepairSaveRecord
    {
        public string id;
        public bool installed, running;
        public double recoverySeconds;
        public RepairSaveRecord Clone() => (RepairSaveRecord)MemberwiseClone();
    }

    [Serializable]
    public sealed class DoorSaveRecord
    {
        public string id;
        public bool powered, unlocked, open;
        public DoorSaveRecord Clone() => (DoorSaveRecord)MemberwiseClone();
    }

    [Serializable]
    public sealed class CriticalSourceSaveRecord
    {
        public string id, objectiveId;
        public bool available;
        public CriticalSourceSaveRecord Clone() => (CriticalSourceSaveRecord)MemberwiseClone();
    }

    public enum SavedDeathStage { Committed = 1, Activated = 2 }

    /// <summary>A durable forward-recovery receipt, not a second succession coordinator.</summary>
    [Serializable]
    public sealed class SavedDeathTransition
    {
        public string id, departingSurvivorId, successorId, departingExpeditionId, expeditionId;
        public SavedDeathStage stage;
        public SavedDeathTransition Clone() => (SavedDeathTransition)MemberwiseClone();
        public void Validate(SurvivorSnapshot survivor, ExpeditionState expedition)
        {
            SnapshotValidation.Identity(id); SnapshotValidation.Identity(departingSurvivorId);
            SnapshotValidation.Identity(successorId); SnapshotValidation.Identity(departingExpeditionId);
            SnapshotValidation.Identity(expeditionId);
            if (!Enum.IsDefined(typeof(SavedDeathStage), stage) || survivor == null || expedition == null
                || Guid.Parse(departingSurvivorId) == Guid.Parse(successorId)
                || Guid.Parse(departingExpeditionId) == Guid.Parse(expeditionId)
                || Guid.Parse(successorId) != Guid.Parse(survivor.id)
                || Guid.Parse(expeditionId) != Guid.Parse(expedition.id)
                || survivor.status != (stage == SavedDeathStage.Committed ? SurvivorLifeStatus.Pending : SurvivorLifeStatus.Active))
                throw new InvalidDataException("Contradictory saved death transition.");
        }
    }
}
