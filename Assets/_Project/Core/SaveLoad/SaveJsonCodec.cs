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
        public SaveGameData Decode(string json)
        {
            try
            {
                using var reader = new JsonTextReader(new StringReader(json)) { DateParseHandling = DateParseHandling.None };
                var obj = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
                if (reader.Read()) throw new SaveCandidateException("Trailing save content.");
                int version = Required(obj, "version", JTokenType.Integer).Value<int>();
                if (version < 1 || version > SaveGameData.CurrentVersion) throw new SaveCandidateException("Unsupported save version.", true);
                Required(obj, "worldSeed", JTokenType.Integer).Value<int>();
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
                if (version == 2)
                    foreach (var id in (JArray)Required(obj, "collectedPickupIds", JTokenType.Array))
                        if (id.Type != JTokenType.String) throw new SaveCandidateException("Invalid pickup fact.");
                var data = obj.ToObject<SaveGameData>();
                return new ContainerSaveSession(data, catalog).Capture();
            }
            catch (SaveCandidateException) { throw; }
            catch (Exception error) when (error is JsonException || error is InvalidDataException || error is OverflowException || error is ArgumentException || error is FormatException)
            { throw new SaveCandidateException("Invalid save snapshot: " + error.Message); }
        }
        public string Encode(SaveGameData data) => JsonConvert.SerializeObject(data, Formatting.Indented);
    }
}
