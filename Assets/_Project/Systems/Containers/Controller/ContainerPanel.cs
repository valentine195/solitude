using UnityEngine;

namespace SOLITUDE.Containers
{
    /// <summary>
    /// One semantic endpoint in a transfer UI. It deliberately hides the
    /// view/controller pair from a modal: callers bind a source to a panel,
    /// never reach inside to coordinate slots, tooltips, or input callbacks.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ContainerController))]
    public sealed class ContainerPanel : MonoBehaviour
    {
        [SerializeField] private ContainerController controller;

        private void Awake()
        {
            if (controller == null) controller = GetComponent<ContainerController>();
        }

        private void OnValidate()
        {
            if (controller == null) controller = GetComponent<ContainerController>();
        }

        public bool Bind(IContainerSource source)
        {
            if (source == null)
            {
                Debug.LogError($"[{nameof(ContainerPanel)}] Cannot bind a null container source.", this);
                return false;
            }

            if (controller == null)
            {
                Debug.LogError($"[{nameof(ContainerPanel)}] No {nameof(ContainerController)} is assigned.", this);
                return false;
            }

            controller.Bind(source);
            return true;
        }

        public bool BindDefaultSource()
        {
            if (controller == null)
            {
                Debug.LogError($"[{nameof(ContainerPanel)}] No {nameof(ContainerController)} is assigned.", this);
                return false;
            }

            return controller.BindDefaultSource();
        }
    }
}
