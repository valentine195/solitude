using System;
using SOLITUDE.Containers.Views;
using SOLITUDE.Modals;
using UnityEngine;
namespace SOLITUDE.Containers
{
    public class ContainerModalView : ModalView, IContainerView
    {
        [SerializeField] private ContainerUIType type = ContainerUIType.Generic;
        [SerializeField] private ContainerController controller;
        private Action open, close;
        public ContainerUIType Type => type;
        public ContainerController Controller => controller != null ? controller : GetComponent<ContainerController>();
        public void Initialize(Action open, Action close) { this.open = open; this.close = close; }
        public void Open(IContainerSource source) => open?.Invoke();
        public override void Open() => open?.Invoke();
        public override void Close() => close?.Invoke();
        public override void Toggle() => open?.Invoke();
        public void RenderVisible(bool visible) { if (root == null) return; if (visible) base.Open(); else base.Close(); }
    }
}
