using System;

namespace SOLITUDE.Core.Events
{
    public static class InteractionEventBus
    {
        public static event Action<InteractionFocusChangedEvent> OnFocusChanged;
        public static event Action<InteractionTriggeredEvent> OnTriggered;

        public static void Publish(InteractionFocusChangedEvent evt)
        {
            OnFocusChanged?.Invoke(evt);
        }

        public static void Publish(InteractionTriggeredEvent evt)
        {
            OnTriggered?.Invoke(evt);
        }
    }
}