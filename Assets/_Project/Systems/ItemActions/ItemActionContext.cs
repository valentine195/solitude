using System;
using SOLITUDE.Application;
using SOLITUDE.Combat;
using SOLITUDE.Core.Animations;
using SOLITUDE.Movement;
using UnityEngine;
namespace SOLITUDE.Items
{
    public sealed class ItemActionContext : IItemActionContext
    {
        public GameObject ActorObject { get; }
        public object Actor => ActorObject;
        public string ItemId => Binding.ItemId;
        public ItemActionSlot Slot { get; }
        public float DirectionX => Direction.x;
        public float DirectionY => Direction.y;
        public Vector2 Direction { get; }
        public EquippedItemBinding Binding { get; }
        public float UnitsPerTile { get; }
        public MovementBehavior Movement { get; }
        public HumanAnimationController Animation { get; }
        public MeleeSwingView View { get; }
        public Sprite HeldSprite { get; }
        public Action<MeleeAttackExecution> EvaluateActive { get; }
        public MeleeHitResolver HitResolver { get; }
        public ItemActionDefinition SourceAction { get; }
        private readonly Vector2 originOffsetTiles;
        public Vector2 AttackOrigin => (Vector2)ActorObject.transform.position + originOffsetTiles * UnitsPerTile;
        private readonly Func<bool> valid;
        private readonly Func<bool> gameplay;
        public bool IsValid => ActorObject != null && ActorObject.activeInHierarchy && valid();
        public bool CanApplyEffects => IsValid && (gameplay?.Invoke() ?? true);
        public ItemActionContext(GameObject actor, EquippedItemBinding binding, ItemActionSlot slot, Vector2 direction,
            float unitsPerTile, MovementBehavior movement, HumanAnimationController animation, MeleeSwingView view,
            Sprite heldSprite, Func<bool> valid, Action<MeleeAttackExecution> evaluateActive,
            MeleeHitResolver hitResolver = null, ItemActionDefinition sourceAction = null, Vector2 originOffsetTiles = default,
            Func<bool> gameplay = null)
        {
            if (actor == null || valid == null || !float.IsFinite(unitsPerTile) || unitsPerTile <= 0 ||
                !float.IsFinite(direction.x) || !float.IsFinite(direction.y) || !float.IsFinite(originOffsetTiles.x)
                || !float.IsFinite(originOffsetTiles.y)) throw new ArgumentException("Invalid item action context.");
            ActorObject = actor; Binding = binding; Slot = slot;
            Direction = direction.sqrMagnitude > .0001f ? direction.normalized : Vector2.down;
            UnitsPerTile = unitsPerTile; Movement = movement; Animation = animation; View = view;
            HeldSprite = heldSprite; this.valid = valid; EvaluateActive = evaluateActive;
            HitResolver = hitResolver; SourceAction = sourceAction; this.originOffsetTiles = originOffsetTiles;
            this.gameplay = gameplay;
        }
    }
    internal sealed class ConfiguredItemActionRequest : IItemActionRequest
    {
        private readonly CompiledItemAction action;
        private readonly ItemActionContext context;
        public bool IsValid => context.IsValid;
        public double CommittedDuration => action.Definition.GetCommittedDuration(action.Configuration);
        public ConfiguredItemActionRequest(CompiledItemAction action, ItemActionContext context)
        { this.action = action; this.context = context; }
        public IItemActionExecution Start() => action.Definition.CreateExecution(context, action.Configuration);
    }
}
