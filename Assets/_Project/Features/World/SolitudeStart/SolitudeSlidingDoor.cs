using SOLITUDE.Core.Interaction;
using SOLITUDE.Player;
using UnityEngine;

namespace SOLITUDE.World.SolitudeStart
{
    public sealed class SolitudeSlidingDoor : InteractableBase
    {
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private Sprite[] frames;
        [SerializeField] private Collider2D blocker;
        [SerializeField] private float frameSeconds = 0.08f;
        private float elapsed;
        private bool opening, open;
        public bool IsOpen => open;
        public override string GetPrompt() => open ? "Door open" : "Open door";
        public override bool CanInteract(PlayerInteractor player) => !opening && !open;
        public override InteractionResult Interact(PlayerInteractor player)
        {
            if (!CanInteract(player)) return InteractionResult.Blocked();
            opening = true; elapsed = 0f;
            if (frames != null && frames.Length == 9) visual.sprite = frames[0];
            return InteractionResult.Success("Door opening");
        }
        private void Awake()
        {
            if (visual != null && frames != null && frames.Length == 9) visual.sprite = frames[0];
            if (blocker != null) blocker.enabled = true;
        }
        private void Update()
        {
            if (!opening || frames == null || frames.Length != 9 || visual == null) return;
            elapsed += Time.deltaTime;
            int index = Mathf.Min(8, Mathf.FloorToInt(elapsed / frameSeconds));
            visual.sprite = frames[index];
            if (elapsed < frames.Length * frameSeconds) return;
            opening = false; open = true; visual.sprite = frames[8];
            if (blocker != null) blocker.enabled = false;
        }
        public void Configure(SpriteRenderer target, Sprite[] poses, Collider2D doorBlocker)
        {
            visual = target; frames = poses; blocker = doorBlocker;
        }
    }
}
