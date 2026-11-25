using System.Collections.Generic;
using SOLITUDE.Application.Persistence;
using SOLITUDE.Items;
using SOLITUDE.SaveLoad;
using UnityEngine;
namespace SOLITUDE.World.OpeningSlice
{
    // The art review is repeatable and must never read or overwrite the player's real save.
    [DefaultExecutionOrder(-3000)]
    public sealed class OpeningReviewSession : MonoBehaviour
    {
        public SaveGameService persistence;
        public ItemDatabase database;
        private void Awake() => persistence.Configure(new SaveRecoveryStore("opening-art-review", new MemoryFiles(), new SaveJsonCodec(database.BuildRuntimeCatalog())));
        private sealed class MemoryFiles : ISaveFiles
        {
            private readonly Dictionary<string,string> data = new();
            public bool Exists(string path) => data.ContainsKey(path);
            public string Read(string path) => data[path];
            public void WriteDurable(string path, string text) => data[path] = text;
            public void Delete(string path) => data.Remove(path);
            public void Move(string source, string destination) { data[destination] = data[source]; data.Remove(source); }
            public void Replace(string source, string destination, string backup) { if (data.ContainsKey(destination)) data[backup] = data[destination]; Move(source,destination); }
        }
    }
}
