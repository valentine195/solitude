using UnityEngine;
using SOLITUDE.Modals;
using SOLITUDE.Containers;
using SOLITUDE.Containers.Views;

namespace SOLITUDE.Containers
{
    public class ContainerModalView : ModalView, IContainerView
    {
        [SerializeField] private ContainerUIType type = ContainerUIType.Generic;
        public ContainerUIType Type => type;

        [SerializeField] private ContainerController controller;

        private void Awake()
        {
            if (controller == null) controller = GetComponent<ContainerController>();
        }

        // Rebinds the shared panel to whichever source is opening it (a
        // specific Locker, Chest, etc.) before showing it - a modal reused
        // across many world objects can't rely on a single Inspector-wired
        // containerSource the way the player's own inventory panel can.
        public void Open(IContainerSource source)
        {
            if (controller == null)
            {
                Debug.LogError($"[{nameof(ContainerModalView)}] No {nameof(ContainerController)} is assigned.", this);
                return;
            }

            controller.Bind(source);
            base.Open();
        }

        public override void Toggle()
        {
            base.Toggle();
        }
    }
}
