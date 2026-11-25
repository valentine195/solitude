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
        [SerializeField] private Vector2 attackOriginOffsetTiles;
        private ItemActionRunner runner;
        private EquippedItemView equipment;
        private ItemCapabilityCatalog capabilities;
        private InputPolicyCoordinator policy;
        private MovementBehavior movement;
        private PlayerVitalsController vitals;
        private PlayerStunReceiver stun;
        private MeleeSwingView view;
        private MeleeHitResolver hitResolver;
        public event Action<MeleeAttackExecution> ActiveSample;
        public ItemActionRunner Runner => runner;
        private bool ActorAvailable => isActiveAndEnabled && vitals != null && vitals.isActiveAndEnabled &&
            vitals.Vitals != null && vitals.Vitals.Health.PreciseCurrent > 0 && (stun == null || !stun.IsStunned);
        public void Bind(ContainerCommandService commands, IContainerReader hotbar, HotbarSelectionState selection,
            ItemCapabilityCatalog catalog, InputPolicyCoordinator gameplay, SOLITUDE.SaveLoad.ContainerSaveSession session = null)
        {
            Release();
            if (catalog == null || gameplay == null || float.IsNaN(unitsPerTile) || float.IsInfinity(unitsPerTile) || unitsPerTile <= 0
                || !float.IsFinite(attackOriginOffsetTiles.x) || !float.IsFinite(attackOriginOffsetTiles.y))
                throw new ArgumentException("Invalid player action binding.");
            equipment = session == null ? new EquippedItemView(commands, hotbar, selection) : new EquippedItemView(commands, hotbar, session); capabilities = catalog; policy = gameplay;
            movement = GetComponent<MovementBehavior>(); vitals = GetComponent<PlayerVitalsController>();
            stun = GetComponent<PlayerStunReceiver>(); view = GetComponent<MeleeSwingView>();
            if (movement == null || vitals == null) { Release(); throw new InvalidOperationException("Actions require existing movement and vitals."); }
            runner = new ItemActionRunner(); equipment.Changed += EquipmentChanged; policy.Changed += PolicyChanged;
            hitResolver = new MeleeHitResolver(new DamageDelivery(catalog.Registry));
            vitals.PresentationChanged += ActorChanged;
            RefreshHeldItem();
        }
        public bool Request(ItemActionSlot slot = ItemActionSlot.Primary)
        {
            if (runner == null || !ActorAvailable || !policy.Gameplay || !equipment.TryCapture(out var binding)) return false;
            var item = capabilities.Resolve(binding.ItemId); var action = item?.Resolve(slot);
            if (action == null) return false;
            var context = new ItemActionContext(gameObject, binding, slot, movement.FacingDirection, unitsPerTile,
                movement, GetComponent<HumanAnimationController>(), view, item.HeldSprite,
                () => runner != null && ActorAvailable && policy.Ready && equipment.IsCurrent(binding), EvaluateActive,
                hitResolver, action.Definition, attackOriginOffsetTiles, () => policy != null && policy.Gameplay);
            return runner.TryStart(new ConfiguredItemActionRequest(action, context));
        }
        private void EvaluateActive(MeleeAttackExecution execution) => ActiveSample?.Invoke(execution);
        private void Invalidate() => runner?.Cancel();
        private void EquipmentChanged()
        {
            Invalidate();
            RefreshHeldItem();
        }

        private void RefreshHeldItem()
        {
            if (view == null) return;
            ItemCapabilities item = null;
            if (isActiveAndEnabled && vitals != null && vitals.isActiveAndEnabled && vitals.Vitals != null &&
                vitals.Vitals.Health.PreciseCurrent > 0 && equipment != null && equipment.TryCapture(out var binding))
                item = capabilities.Resolve(binding.ItemId);
            view.SetEquippedItem(item, capabilities?.Registry);
        }

        private void PolicyChanged() { if (policy != null && !policy.Ready) Invalidate(); }
        private void ActorChanged()
        {
            if (!ActorAvailable) Invalidate();
            RefreshHeldItem();
        }
        public void Advance(double seconds) => runner?.Advance(seconds, ActorAvailable, policy != null && policy.Gameplay);
        private void Update() => Advance(Time.deltaTime);
        private void OnEnable() => RefreshHeldItem();
        private void OnDisable()
        {
            Invalidate();
            if (view != null) view.SetEquippedItem(null, null);
        }
        private void OnDestroy() => Release();
        internal void RetireActor()
        {
            if (runner == null) return;
            // The avatar survives succession; the departed actor's commitment does not.
            runner.Dispose();
            runner = new ItemActionRunner();
            if (view != null) view.SetEquippedItem(null, null);
        }
        public void Release()
        {
            runner?.Dispose(); runner = null;
            hitResolver = null;
            if (policy != null) policy.Changed -= PolicyChanged;
            if (vitals != null) vitals.PresentationChanged -= ActorChanged;
            if (equipment != null) { equipment.Changed -= EquipmentChanged; equipment.Dispose(); equipment = null; }
            if (view != null) view.SetEquippedItem(null, null);
            capabilities = null; policy = null;
        }
    }
}
