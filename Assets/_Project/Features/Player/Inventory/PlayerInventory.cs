using SOLITUDE.Containers;
using UnityEngine;
namespace SOLITUDE.Player
{
    public class PlayerInventory : MonoBehaviour, IContainerSource
    {
        [SerializeField] private int capacity = 24;
        public IContainerReader Container { get; private set; }
        public string Label => "Inventory";
        public int Capacity => Container?.Capacity ?? capacity;
        public void Initialize(IContainerReader reader) => Container = reader;
        public void Release() => Container = null;
    }
}
