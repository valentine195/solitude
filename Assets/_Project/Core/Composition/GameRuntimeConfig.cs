using SOLITUDE.Items;
using UnityEngine;
namespace SOLITUDE.Composition
{
    [CreateAssetMenu(menuName = "SOLITUDE/Game Runtime Config")]
    public sealed class GameRuntimeConfig : ScriptableObject
    {
        public ItemDatabase database;
        public StartingLoadoutDefinition startingLoadout;
        public LootTable[] lootTables = new LootTable[0];
        public bool useCarriedEquipment;
        public int worldSeed = 12345;
    }
}
