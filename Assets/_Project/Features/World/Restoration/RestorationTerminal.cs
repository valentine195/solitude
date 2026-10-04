using SOLITUDE.Core.Interaction;
using SOLITUDE.Player;
using SOLITUDE.Restoration;
using UnityEngine;

namespace SOLITUDE.World.Restoration
{
    public sealed class RestorationTerminal : InteractableBase
    {
        [SerializeField] private RestorableSystem system;
        [SerializeField] private SpriteRenderer screen;
        [SerializeField] private Color unpoweredColor = new Color(0.08f, 0.08f, 0.08f, 1f);
        [SerializeField] private Color poweredColor = Color.cyan;
        [SerializeField, TextArea] private string message = "Auxiliary systems online. Awaiting instructions.";
        private RestorableSystemState subscribed;
        private bool powered;
        public bool IsPowered => isActiveAndEnabled && system != null && screen != null && powered;

        public void Configure(RestorableSystem target, SpriteRenderer display, string content, Color off, Color on)
        {
            Unsubscribe(); system = target; screen = display; message = content;
            unpoweredColor = off; poweredColor = on;
            if (isActiveAndEnabled) Subscribe(); else Render();
        }
        public override string GetPrompt() => IsPowered ? "Read terminal" : "Terminal offline";
        public override bool CanInteract(PlayerInteractor player) => isActiveAndEnabled;
        public override InteractionResult Interact(PlayerInteractor player) => IsPowered
            ? InteractionResult.Success(string.IsNullOrWhiteSpace(message) ? "Terminal online." : message)
            : InteractionResult.Blocked("Terminal offline — restore power");

        private void OnEnable() => Subscribe();
        private void Subscribe()
        {
            Unsubscribe();
            subscribed = system != null ? system.State : null;
            if (subscribed != null) subscribed.Changed += Reconcile;
            Reconcile();
        }
        private void Reconcile()
        {
            powered = system != null && subscribed != null && subscribed.IsRestored;
            Render();
        }
        private void Render() { if (screen != null) screen.color = IsPowered ? poweredColor : unpoweredColor; }
        // Unity destruction does not emit a domain state change.
        private void LateUpdate() { if (powered && system == null) Reconcile(); }
        private void Unsubscribe()
        {
            if (subscribed != null) subscribed.Changed -= Reconcile;
            subscribed = null; powered = false;
        }
        private void OnDisable() { Unsubscribe(); Render(); }
    }
}
