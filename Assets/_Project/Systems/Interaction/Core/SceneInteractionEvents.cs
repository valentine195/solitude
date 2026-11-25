using System;
using SOLITUDE.Core.Interaction;

namespace SOLITUDE.Core.Events
{
    public sealed class SceneInteractionEvents : IDisposable
    {
        private bool disposed;
        public IInteractable CurrentFocus { get; private set; }
        public event Action<InteractionFocusChangedEvent> FocusChanged;
        public event Action<InteractionTriggeredEvent> Triggered;

        public void Publish(InteractionFocusChangedEvent evt)
        {
            if (disposed) return;
            CurrentFocus = evt.interactable;
            FocusChanged?.Invoke(evt);
        }
        public void Publish(InteractionTriggeredEvent evt)
        { if (!disposed) Triggered?.Invoke(evt); }
        public void Dispose()
        { disposed = true; CurrentFocus = null; FocusChanged = null; Triggered = null; }
    }
}
