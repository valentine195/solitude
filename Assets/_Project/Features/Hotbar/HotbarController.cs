using System.Collections.Generic;
using SOLITUDE.Containers;
using SOLITUDE.Core.Input;
using SOLITUDE.Hotbar;
using UnityEngine;
using UnityEngine.InputSystem;

public class HotbarController : MonoBehaviour
{
    private static HotbarController _instance;
    public static HotbarController Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindAnyObjectByType<HotbarController>();
            return _instance;
        }
    }

    [SerializeField] public Hotbar Hotbar;

    private readonly Dictionary<InputAction, int> _slotActionIndex = new();

    private void Awake()
    {

        var gameplay = GameInput.Actions.Gameplay;
        var slotActions = new[]
        {
            gameplay.Hotbar1, gameplay.Hotbar2, gameplay.Hotbar3,
            gameplay.Hotbar4, gameplay.Hotbar5, gameplay.Hotbar6,
            gameplay.Hotbar7, gameplay.Hotbar8, gameplay.Hotbar9
        };

        for (int i = 0; i < slotActions.Length; i++)
            _slotActionIndex[slotActions[i]] = i;
    }

    private void OnEnable()
    {
        foreach (var action in _slotActionIndex.Keys)
            action.performed += OnHotbarSlotPerformed;
    }

    private void OnDisable()
    {
        foreach (var action in _slotActionIndex.Keys)
            action.performed -= OnHotbarSlotPerformed;
    }

    private void OnHotbarSlotPerformed(InputAction.CallbackContext ctx)
    {
        if (_slotActionIndex.TryGetValue(ctx.action, out int index))
            Hotbar.SetActive(index);
    }
}