using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SOLITUDE.Application.Persistence;
using SOLITUDE.Items;
namespace SOLITUDE.SaveLoad
{
    public sealed class SaveJsonCodec : ISnapshotCodec
    {
        private readonly IItemCatalog catalog;
        public SaveJsonCodec(IItemCatalog catalog) => this.catalog = catalog;
        private static JToken Required(JObject obj, string field, JTokenType type)
        {
            var token = obj[field];
            if (token == null || token.Type != type) throw new SaveCandidateException("Missing or invalid " + field);
            return token;
        }
        private static JObject OptionalObject(JObject obj, string field)
        {
            var token = obj[field];
            if (token == null || (token.Type != JTokenType.Null && token.Type != JTokenType.Object))
                throw new SaveCandidateException("Missing or invalid " + field);
            return token.Type == JTokenType.Null ? null : (JObject)token;
        }
        private static void Number(JObject obj, string field)
        {
            var token = obj[field];
            if (token == null || (token.Type != JTokenType.Integer && token.Type != JTokenType.Float))
                throw new SaveCandidateException("Missing or invalid " + field);
            double value = token.Value<double>();
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new SaveCandidateException("Invalid " + field);
        }
        private static void Keys(JObject obj, string field)
        {
            foreach (var key in (JArray)Required(obj, field, JTokenType.Array))
                if (key.Type != JTokenType.String) throw new SaveCandidateException("Invalid " + field);
        }
        private static JObject Record(JToken token)
        {
            if (!(token is JObject record)) throw new SaveCandidateException("Invalid saved world record.");
            Required(record, "id", JTokenType.String);
            return record;
        }
        private static void Lifecycle(JObject obj)
        {
            var survivor = OptionalObject(obj, "survivor");
            if (survivor != null)
            {
                Required(survivor, "id", JTokenType.String); Required(survivor, "status", JTokenType.Integer);
                Required(survivor, "scenePath", JTokenType.String);
                foreach (var field in new[] { "x", "y", "z", "recoveryDelayRemaining" }) Number(survivor, field);
                foreach (var field in new[] { "health", "oxygen", "hunger" })
                {
                    var vital = (JObject)Required(survivor, field, JTokenType.Object);
                    Number(vital, "current"); Number(vital, "max");
                }
            }
            var expedition = OptionalObject(obj, "expedition");
            if (expedition != null)
            {
                Required(expedition, "id", JTokenType.String);
                Keys(expedition, "defeatedEncounterIds"); Keys(expedition, "collectedSupplySourceIds");
            }
            var ship = (JObject)Required(obj, "ship", JTokenType.Object);
            foreach (var token in (JArray)Required(ship, "repairs", JTokenType.Array))
            {
                var record = Record(token); Required(record, "installed", JTokenType.Boolean);
                Required(record, "running", JTokenType.Boolean); Number(record, "recoverySeconds");
            }
            foreach (var token in (JArray)Required(ship, "doors", JTokenType.Array))
            {
                var record = Record(token);
                foreach (var field in new[] { "powered", "unlocked", "open" }) Required(record, field, JTokenType.Boolean);
            }
            foreach (var token in (JArray)Required(ship, "criticalSources", JTokenType.Array))
            {
                var record = Record(token); Required(record, "objectiveId", JTokenType.String);
                Required(record, "available", JTokenType.Boolean);
            }
            Keys(ship, "permanentDiscoveryIds");
            var pending = OptionalObject(obj, "pendingDeath");
            if (pending != null)
            {
                foreach (var field in new[] { "id", "departingSurvivorId", "successorId", "departingExpeditionId", "expeditionId" })
                    Required(pending, field, JTokenType.String);
                Required(pending, "stage", JTokenType.Integer);
            }
        }
        public SaveGameData Decode(string json)
        {
            try
            {
                using var reader = new JsonTextReader(new StringReader(json)) { DateParseHandling = DateParseHandling.None };
                var obj = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
                if (reader.Read()) throw new SaveCandidateException("Trailing save content.");
                int version = Required(obj, "version", JTokenType.Integer).Value<int>();
                if (version < 1 || version > SaveGameData.CurrentVersion) throw new SaveCandidateException("Unsupported save version.", true);
                int worldSeed = Required(obj, "worldSeed", JTokenType.Integer).Value<int>();
                var records = (JArray)Required(obj, "containers", JTokenType.Array);
                foreach (var entry in records)
                {
                    if (!(entry is JObject record)) throw new SaveCandidateException("Invalid container record.");
                    Required(record, "saveableId", JTokenType.String);
                    var state = (JObject)Required(record, "state", JTokenType.Object);
                    foreach (var item in (JArray)Required(state, "slots", JTokenType.Array))
                    {
                        if (!(item is JObject slot)) throw new SaveCandidateException("Invalid slot.");
                        Required(slot, "index", JTokenType.Integer).Value<int>();
                        Required(slot, "quantity", JTokenType.Integer).Value<int>();
                        var id = Required(slot, "itemId", JTokenType.String).Value<string>();
                        if (!catalog.TryGet(id, out _)) throw new SaveCandidateException("Saved item is not in the current catalog: " + id, true);
                    }
                }
                if (version >= 2) Keys(obj, "collectedPickupIds");
                if (version >= 3) Lifecycle(obj);
                bool initialized = version >= 4 && Required(obj, "startingLoadoutInitialized", JTokenType.Boolean).Value<bool>();
                // Use the validated, case-sensitive wire fact; aliases cannot override it during conversion.
                foreach (var property in new System.Collections.Generic.List<JProperty>(obj.Properties()))
                    if (string.Equals(property.Name, "startingLoadoutInitialized", StringComparison.OrdinalIgnoreCase)) property.Remove();
                var equipmentToken = obj["playerEquipment"];
                PlayerEquipmentRecord equipment = null;
                if (equipmentToken != null && equipmentToken.Type != JTokenType.Null)
                {
                    if (version < 4 || equipmentToken is not JObject record)
                        throw new SaveCandidateException("Invalid saved equipment record.");
                    equipment = new PlayerEquipmentRecord
                    {
                        weaponItemId = EquipmentId(record, "weaponItemId"),
                        toolItemId = EquipmentId(record, "toolItemId"),
                        pinnedItemId = EquipmentId(record, "pinnedItemId"),
                        activeSlot = (PlayerEquipmentSlot)Required(record, "activeSlot", JTokenType.Integer).Value<int>(),
                        separateStorage = record["separateStorage"] == null ? false :
                            Required(record, "separateStorage", JTokenType.Boolean).Value<bool>()
                    };
                    var quickUse = record["additionalQuickUseItems"];
                    if (quickUse != null && quickUse.Type != JTokenType.Null)
                    {
                        if (quickUse is not JArray slots || slots.Count != PlayerEquipmentRecord.QuickUseCount - 1)
                            throw new SaveCandidateException("Invalid additional Quick Use slots.");
                        for (int i = 0; i < slots.Count; i++) equipment.SetQuickUseItem(i + 1, EquipmentId(slots[i], "additionalQuickUseItems"));
                    }
                }
                foreach (var property in new System.Collections.Generic.List<JProperty>(obj.Properties()))
                    if (string.Equals(property.Name, "playerEquipment", StringComparison.OrdinalIgnoreCase)) property.Remove();
                var source = obj.ToObject<SaveGameData>();
                source.playerEquipment = equipment;
                source.version = version; source.worldSeed = worldSeed; source.startingLoadoutInitialized = initialized;
                return new ContainerSaveSession(source, catalog).Capture();
            }
            catch (SaveCandidateException) { throw; }
            catch (Exception error) when (error is JsonException || error is InvalidDataException || error is OverflowException || error is ArgumentException || error is FormatException)
            { throw new SaveCandidateException("Invalid save snapshot: " + error.Message); }
        }
        private string EquipmentId(JObject record, string field) => EquipmentId(record[field], field);

        private string EquipmentId(JToken token, string field)
        {
            if (token == null || (token.Type != JTokenType.Null && token.Type != JTokenType.String))
                throw new SaveCandidateException("Missing or invalid " + field);
            if (token.Type == JTokenType.Null) return null;
            string id = token.Value<string>();
            if (string.IsNullOrWhiteSpace(id)) throw new SaveCandidateException("Empty equipment identity.");
            return catalog.TryGet(id, out var item) ? item.ItemId : id;
        }

        public string Encode(SaveGameData data)
        {
            if (data == null) return JsonConvert.SerializeObject(data, Formatting.Indented);
            var obj = JObject.FromObject(data);
            if (data.version < 4) obj.Remove("startingLoadoutInitialized");
            if (data.playerEquipment == null || data.version < 4) obj.Remove("playerEquipment");
            else if (obj["playerEquipment"] is JObject equipment) equipment.Remove("ActiveItemId");
            return obj.ToString(Formatting.Indented);
        }
    }
}
