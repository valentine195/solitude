using System.Collections.Generic;
using SOLITUDE.Application.Persistence;
using SOLITUDE.Items;
using SOLITUDE.SaveLoad;
using UnityEngine;

namespace SOLITUDE.World.Restoration
{
    // Presence opts this scene into disposable state. No disk backend is constructed.
    public sealed class RestorationDemoSession : MonoBehaviour
    {
        public MemoryFiles Files { get; private set; }
        public SaveRecoveryStore CreateStore(IItemCatalog catalog)
        {
            Files = new MemoryFiles();
            return new SaveRecoveryStore("restoration-demo", Files, new SaveJsonCodec(catalog));
        }
        public sealed class MemoryFiles : ISaveFiles
        {
            private readonly Dictionary<string, string> data = new();
            public int Reads { get; private set; }
            public int Writes { get; private set; }
            public bool Exists(string path) { Reads++; return data.ContainsKey(path); }
            public string Read(string path) { Reads++; return data[path]; }
            public void WriteDurable(string path, string text) { Writes++; data[path] = text; }
            public void Delete(string path) => data.Remove(path);
            public void Move(string source, string destination) { data[destination] = data[source]; data.Remove(source); }
            public void Replace(string source, string destination, string backup)
            { if (data.ContainsKey(destination)) data[backup] = data[destination]; Move(source, destination); }
        }
    }
}
