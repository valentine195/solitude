using System;
using SOLITUDE.Containers;
using SOLITUDE.Items;
using SOLITUDE.SaveLoad;
using UnityEngine;
namespace SOLITUDE.Features.Interactables
{
    public interface IContainerScreenRequests { bool Open(LockerContainer source); void Close(LockerContainer source); }
    [RequireComponent(typeof(SaveableId))]
    public class LockerContainer : MonoBehaviour, IContainerSource, ISaveable<ContainerSaveData>
    {
        [SerializeField] private int capacity = 12;
        [SerializeField] private SaveableId saveableId;
        [SerializeField] private LootTable lootTable;
        private Func<LockerContainer, IContainerReader> initialize;
        private Action<ContainerHandle, ContainerSaveData> restore;
        public IContainerReader Container { get; private set; }
        public string Label => "Locker";
        public int Capacity => Container?.Capacity ?? capacity;
        public string PersistentId => (saveableId != null ? saveableId : GetComponent<SaveableId>()).Value;
        public LootTable Loot => lootTable;
        public void Initialize(Func<LockerContainer, IContainerReader> initialize, Action<ContainerHandle, ContainerSaveData> restore)
        { this.initialize = initialize; this.restore = restore; }
        public bool EnsureInitialized()
        {
            if (Container?.IsAvailable == true) return true;
            if (initialize == null) return false;
            try { Container = initialize(this); return Container?.IsAvailable == true; }
            catch (Exception error) { Debug.LogError("[LockerContainer] " + error.Message, this); return false; }
        }
        public ContainerSaveData CaptureState() => ContainerSaveSerializer.Capture(Container);
        public void RestoreState(ContainerSaveData state) { if (EnsureInitialized()) restore?.Invoke(Container.Handle, state); }
        public void Release() { Container = null; initialize = null; restore = null; }
    }
}
