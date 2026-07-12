using System;
using System.Collections.Generic;
using SOLITUDE.Containers;
using SOLITUDE.Core.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SOLITUDE.Hotbar
{
    /// <summary>
    /// Thin MonoBehaviour adapter over the shared Container model - same
    /// shape as PlayerInventory/LockerContainer - plus the two things unique
    /// to a hotbar: an "equipped" ActiveIndex, and the number-key input that
    /// drives it. Assign this component as the containerSource on the
    /// ContainerController driving the (always-visible, non-modal) hotbar UI.
    /// </summary>
    public class Hotbar : MonoBehaviour, IContainerSource
    {
        // Lazily resolved, not set only in Awake - matches
        // ContainerInteractionHub's reasoning: Unity doesn't guarantee this
        // object's Awake() has run before another object's OnEnable() (e.g.
        // a HotbarSlotDecorator instantiated elsewhere) asks for Instance.
        private static Hotbar instance;
        public static Hotbar Instance
        {
            get
            {
                if (instance == null)
                    instance = FindAnyObjectByType<Hotbar>();
                return instance;
            }
        }

        [SerializeField] private int capacity = 9;

        private Container container;

        public Container Container => container ??= new Container(capacity);
        public int Capacity => capacity;
        public string Label => "Hotbar";

        public event Action<int> SlotChanged
        {
            add => container.SlotChanged += value;
            remove => container.SlotChanged -= value;
        }

        public int ActiveIndex { get; private set; } = -1;
        public event Action<int> ActiveIndexChanged;

        // Maps each generated Hotbar1..Hotbar9 InputAction to its slot index,
        // so OnHotbarSlotPerformed can stay a single handler instead of nine
        // near-identical ones.
        private readonly Dictionary<InputAction, int> slotActionIndex = new();

        private void Awake()
        {
            container = new Container(capacity);
            container.SlotChanged += HandleSlotChanged;

            var gameplay = GameInput.Actions.Gameplay;
            var slotActions = new[]
            {
                gameplay.Hotbar1, gameplay.Hotbar2, gameplay.Hotbar3,
                gameplay.Hotbar4, gameplay.Hotbar5, gameplay.Hotbar6,
                gameplay.Hotbar7, gameplay.Hotbar8, gameplay.Hotbar9
            };

            for (int i = 0; i < slotActions.Length && i < capacity; i++)
                slotActionIndex[slotActions[i]] = i;
        }

        private void OnEnable()
        {
            foreach (var action in slotActionIndex.Keys)
                action.performed += OnHotbarSlotPerformed;
        }

        private void OnDisable()
        {
            foreach (var action in slotActionIndex.Keys)
                action.performed -= OnHotbarSlotPerformed;
        }

        public void SetActive(int index)
        {
            if (index == ActiveIndex) return;
            ActiveIndex = index;
            ActiveIndexChanged?.Invoke(index);
        }

        // If the equipped slot gets emptied out (dragged elsewhere, consumed
        // to zero), clear ActiveIndex rather than leaving it "equipped" on
        // nothing.
        private void HandleSlotChanged(int index)
        {
            if (index == ActiveIndex && container.IsEmpty(index))
            {
                ActiveIndex = -1;
                ActiveIndexChanged?.Invoke(-1);
            }
        }

        private void OnHotbarSlotPerformed(InputAction.CallbackContext ctx)
        {
            if (slotActionIndex.TryGetValue(ctx.action, out int index))
                SetActive(index);
        }

        // Defensive reset for Unity's "Enter Play Mode without domain
        // reload" option - same reasoning as ContainerInteractionHub.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
        }
    }
}