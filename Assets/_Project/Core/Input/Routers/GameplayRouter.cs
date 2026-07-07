using UnityEngine;

namespace SOLITUDE.Core.Input
{
    public class GameplayRouter : MonoBehaviour, IInputRouter
    {
        public InputMode Mode => InputMode.Gameplay;

        public bool ActiveInMode(InputMode mode) => mode == Mode;

        public void Enable() => GameInput.Actions.Gameplay.Enable();
        public void Disable() => GameInput.Actions.Gameplay.Disable();
    }
}