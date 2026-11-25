using System;
using SOLITUDE.Containers.Views;
using SOLITUDE.Modals;
using UnityEngine;
namespace SOLITUDE.Containers
{
    public sealed class ContainerTransferModalView : ModalView, IContainerView
    {
        [SerializeField] private ContainerUIType type = ContainerUIType.Locker;
        [SerializeField] private ContainerPanel playerInventoryPanel;
        [SerializeField] private ContainerPanel targetContainerPanel;
        private Func<IContainerSource, bool> open;
        private Action close;
        public ContainerUIType Type => type;
        public ContainerController InventoryController => playerInventoryPanel?.Controller;
        public ContainerController TargetController => targetContainerPanel?.Controller;
        public void Initialize(Func<IContainerSource, bool> open, Action close) { this.open = open; this.close = close; }
        public void Open(IContainerSource source) => open?.Invoke(source);
        public override void Open() { }
        public override void Close() => close?.Invoke();
        public override void Toggle() => close?.Invoke();
        public void RenderVisible(bool visible) { if (root == null) return; if (visible) base.Open(); else base.Close(); }
    }
}
