using SOLITUDE.Items;
using UnityEngine;
namespace SOLITUDE.Composition
{
    [CreateAssetMenu(menuName = "SOLITUDE/Game Runtime Config")]
    public sealed class GameRuntimeConfig : ScriptableObject
    {
        public ItemDatabase database;
        public LootTable[] lootTables = new LootTable[0];
        public int worldSeed = 12345;
    }
}
