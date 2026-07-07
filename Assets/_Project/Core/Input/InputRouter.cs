using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace SOLITUDE.Core.Input
{

    public class InputRouter : MonoBehaviour
    {
        public static InputRouter Instance { get; private set; }

        public InputMode Mode => Instance.Mode;

        private Dictionary<InputMode, IInputRouter> routers = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject); return;
            }
            Instance = this;

            var available = GetComponentsInChildren<IInputRouter>(true);

            foreach (var router in available)
            {
                if (routers.ContainsKey(router.Mode))
                {
                    Debug.LogError($"[InputRouter] A router by that type already exists: {router.Mode}");
                    continue;
                }
                routers.Add(router.Mode, router);
            }
        }

        public void SetMode(InputMode mode)
        {
            foreach (var router in routers.Values)
            {
                if (router.Mode == mode)
                {
                    Debug.Log($"[InputRouter] Mode set to {mode}");
                    router.Enable();
                }
                else router.Disable();
            }
        }
    }

}