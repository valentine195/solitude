using UnityEngine;
using System.Collections.Generic;
using SOLITUDE.Containers.Views;

namespace SOLITUDE.Containers
{
    public class ContainerUIController : MonoBehaviour
    {
        public static ContainerUIController Instance;

        private Dictionary<ContainerUIType, IContainerView> views = new();
        public IContainerView GetView(ContainerUIType type)
        {
            if (views.TryGetValue(type, out var view)) return view;

            Debug.LogError($"[ContainerUIController] No view registered for {type}.");
            return null;
        }

        private void Awake()

        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError("[ContainerUIController] An instance of the ContainerUIController already exists.");
                Destroy(gameObject);
                return;
            }

            Instance = this;

            var found = GetComponentsInChildren<IContainerView>(true);
            foreach (var view in found)
            {
                if (views.ContainsKey(view.Type))
                {
                    Debug.LogError($"[ContainerUIController] Duplicate views detected for view type: {view.Type}");
                    continue;
                }
                views.Add(view.Type, view);
            }

        }
    }
}