using SOLITUDE.Core.Interaction;
using SOLITUDE.Player;
using UnityEngine;

namespace SOLITUDE.World.ScrubberEncounter
{
    public sealed class ScrubberAccess : InteractableBase
    {
        [SerializeField] private ScrubberScreenView screen;
        public void Configure(ScrubberScreenView view) => screen = view;
        public override string GetPrompt() => "Access scrubber";
        public override bool CanInteract(PlayerInteractor player) => screen != null;
        public override InteractionResult Interact(PlayerInteractor player)
        {
            if (screen == null) return InteractionResult.Blocked("Scrubber interface unavailable");
            return screen.Open() ? InteractionResult.Success() : InteractionResult.Blocked("Scrubber interface unavailable");
        }
    }
}
