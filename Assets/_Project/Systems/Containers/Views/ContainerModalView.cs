using UnityEngine;
using SOLITUDE.Modals;
using SOLITUDE.Containers;
using SOLITUDE.Containers.Views;
namespace SOLITUDE.Containers
{
    public class ContainerModalView : ModalView, IContainerView
    {

        [SerializeField] ContainerUIType type = ContainerUIType.Generic;
        public ContainerUIType Type => type;


        public void Open(Container container)
        {
            base.Open();
        }
        override public void Toggle()
        {
            base.Toggle();
        }

    }
}
