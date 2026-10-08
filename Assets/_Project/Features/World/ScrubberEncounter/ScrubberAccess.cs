using System;
using SOLITUDE.Core.Interaction;
using SOLITUDE.Player;
using UnityEngine;

namespace SOLITUDE.World.ScrubberEncounter
{
    public sealed class ScrubberAccess : InteractableBase
    {
        [SerializeField] private ScrubberScreenView screen;
        public event Action<PlayerInteractor> AccessSucceeded;
        public void Configure(ScrubberScreenView view) => screen = view;
        public override string GetPrompt() => "Access scrubber";
        public override bool CanInteract(PlayerInteractor player) => screen != null;
        public override InteractionResult Interact(PlayerInteractor player)
        {
            if (screen == null) return InteractionResult.Blocked("Scrubber interface unavailable");
            if (!screen.Open()) return InteractionResult.Blocked("Scrubber interface unavailable");
            if (AccessSucceeded != null)
                foreach (Action<PlayerInteractor> observer in AccessSucceeded.GetInvocationList())
                    try { observer(player); } catch (Exception error) { Debug.LogException(error); }
            return InteractionResult.Success();
        }
    }
}
