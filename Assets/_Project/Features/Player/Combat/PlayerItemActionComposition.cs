using System;
using SOLITUDE.Application;
using SOLITUDE.Combat;
using SOLITUDE.Containers;
using SOLITUDE.Core.Animations;
using SOLITUDE.Items;
using SOLITUDE.Movement;
using SOLITUDE.Npcs.Sec0;
using UnityEngine;
namespace SOLITUDE.Player
{
    // Scene-owned adapter; primary input is connected separately by scene composition.
    public sealed class PlayerItemActionComposition : MonoBehaviour
    {
        [SerializeField] private float unitsPerTile = 1;
        private ItemActionRunner runner;
        private EquippedItemView equipment;
        private ItemCapabilityCatalog capabilities;
        private InputPolicyCoordinator policy;
        private MovementBehavior movement;
        private PlayerVitalsController vitals;
        private PlayerStunReceiver stun;
        private MeleeSwingView view;
        public event Action<MeleeAttackExecution> ActiveSample;
        public ItemActionRunner Runner => runner;
        private bool ActorAvailable => isActiveAndEnabled && vitals != null && vitals.isActiveAndEnabled &&
            vitals.Vitals != null && vitals.Vitals.Health.PreciseCurrent > 0 && (stun == null || !stun.IsStunned);
        public void Bind(ContainerCommandService commands, IContainerReader hotbar, HotbarSelectionState selection,
            ItemCapabilityCatalog catalog, InputPolicyCoordinator gameplay)
        {
            Release();
            if (catalog == null || gameplay == null || float.IsNaN(unitsPerTile) || float.IsInfinity(unitsPerTile) || unitsPerTile <= 0)
                throw new ArgumentException("Invalid player action binding.");
            equipment = new EquippedItemView(commands, hotbar, selection); capabilities = catalog; policy = gameplay;
            movement = GetComponent<MovementBehavior>(); vitals = GetComponent<PlayerVitalsController>();
            stun = GetComponent<PlayerStunReceiver>(); view = GetComponent<MeleeSwingView>();
            if (movement == null || vitals == null) { Release(); throw new InvalidOperationException("Actions require existing movement and vitals."); }
            runner = new ItemActionRunner(); equipment.Changed += Invalidate; policy.Changed += PolicyChanged;
            vitals.PresentationChanged += ActorChanged;
        }
        public bool Request(ItemActionSlot slot = ItemActionSlot.Primary)
        {
            if (runner == null || !ActorAvailable || !policy.Gameplay || !equipment.TryCapture(out var binding)) return false;
            var item = capabilities.Resolve(binding.ItemId); var action = item?.Resolve(slot);
            if (action == null) return false;
            var context = new ItemActionContext(gameObject, binding, slot, movement.FacingDirection, unitsPerTile,
                movement, GetComponent<HumanAnimationController>(), view, item.HeldSprite,
                () => runner != null && ActorAvailable && policy.Ready && equipment.IsCurrent(binding), EvaluateActive);
            return runner.TryStart(new ConfiguredItemActionRequest(action, context));
        }
        private void EvaluateActive(MeleeAttackExecution execution) => ActiveSample?.Invoke(execution);
        private void Invalidate() => runner?.Cancel();
        private void PolicyChanged() { if (policy != null && !policy.Ready) Invalidate(); }
        private void ActorChanged() { if (!ActorAvailable) Invalidate(); }
        public void Advance(double seconds) => runner?.Advance(seconds, ActorAvailable, policy != null && policy.Gameplay);
        private void Update() => Advance(Time.deltaTime);
        private void OnDisable() => Invalidate();
        private void OnDestroy() => Release();
        public void Release()
        {
            runner?.Dispose(); runner = null;
            if (policy != null) policy.Changed -= PolicyChanged;
            if (vitals != null) vitals.PresentationChanged -= ActorChanged;
            if (equipment != null) { equipment.Changed -= Invalidate; equipment.Dispose(); equipment = null; }
            capabilities = null; policy = null;
        }
    }
}
