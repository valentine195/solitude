using UnityEngine;
using SOLITUDE.Modals;

namespace SOLITUDE.Modals
{
    public abstract class ModalView : MonoBehaviour, IModalView
    {
        [SerializeField] protected GameObject root;

        public bool IsOpen => root.activeSelf;

        public virtual void Open() => root.SetActive(true);
        public virtual void Close() => root.SetActive(false);
        public virtual void Toggle() => root.SetActive(!root.activeSelf);
    }
}