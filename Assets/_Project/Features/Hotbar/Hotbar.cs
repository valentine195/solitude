using System;
using SOLITUDE.Application;
using SOLITUDE.Containers;
using UnityEngine;
namespace SOLITUDE.Hotbar
{
    public class Hotbar : MonoBehaviour, IContainerSource
    {
        [SerializeField] private int capacity = 9;
        private HotbarSelectionState selection;
        public IContainerReader Container { get; private set; }
        public string Label => "Hotbar";
        public int Capacity => Container?.Capacity ?? capacity;
        public int ActiveIndex => selection?.ActiveIndex ?? -1;
        public void Initialize(IContainerReader reader, HotbarSelectionState selection) { Container = reader; this.selection = selection; }
        public void SetActive(int index) => selection?.Select(index);
        public void Release() { Container = null; selection = null; }
    }
}
