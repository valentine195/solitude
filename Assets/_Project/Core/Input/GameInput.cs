using UnityEngine;

namespace SOLITUDE.Core.Input
{
    /// <summary>
    /// Single shared instance of the generated <see cref="SOLITUDE_InputActions"/>
    /// asset for the whole game.
    ///
    /// Unity's InputActionCodeGenerator produces a brand new set of
    /// InputActionMap/InputAction objects every time you call
    /// `new SOLITUDE_InputActions()`. Before this existed, ContainerInputBridge
    /// created one per ContainerController and PlayerInventoryModalController
    /// created its own on top of that - so opening two containers at once
    /// enabled the same "Container" action map twice from two different
    /// asset instances, and every Point/Click/Cancel callback fired twice.
    ///
    /// There should be exactly one InputActionAsset instance for the game.
    /// Anything that needs an action map gets it from here instead of
    /// constructing its own SOLITUDE_InputActions().
    /// </summary>
    public static class GameInput
    {
        private static SOLITUDE_InputActions actions;

        public static SOLITUDE_InputActions Actions => actions ??= new SOLITUDE_InputActions();

        // Defensive reset for Unity's "Enter Play Mode without domain reload"
        // option - without this, a static field like `actions` would survive
        // between play sessions in the editor and could hand out a disposed
        // InputActionAsset from a previous session.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            actions?.Dispose();
            actions = null;
        }
    }
}
