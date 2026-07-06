using UnityEngine;
using SOLITUDE.Modals;

namespace SOLITUDE.Containers
{
    public class ContainerModalView : ModalView
    {

        override public void Toggle()
        {
            Debug.Log("ContainerModalView Toggle called");
            base.Toggle();
        }

    }
}