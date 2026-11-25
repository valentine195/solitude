using System;
using System.Collections.Generic;
using SOLITUDE.GameplayTags;
using SOLITUDE.Items;
using UnityEngine;

namespace SOLITUDE.Combat
{
    /// <summary>Shows the active item at the hand during locomotion and committed melee swings.</summary>
    public sealed class MeleeSwingView : MonoBehaviour
    {
        [Serializable]
        private struct HandPose
        {
            public Sprite frame;
            public Vector2 gripPixels;
            public float angle;
            [Range(0.1f, 1f)] public float lengthScale;
            public bool behindBody;
            // A cutout of this frame's hand, centered on gripPixels.
            public Sprite foregroundGrip;
            // Optional body with space for an equipped hand drawn in the foreground.
            public Sprite carryBody;
        }

        private const int FramesPerDirection = 4;
        [SerializeField] private Vector2 originOffsetTiles = new Vector2(0, 1);
        [SerializeField] private GameplayTagDefinition meleeTag;
        [SerializeField] private RuntimeAnimatorController bodyAnimationController;
        // South, north, west, east. Each row ends with the corresponding locomotion idle.
        [SerializeField] private Sprite[] bodyAnimationFrames;
        [SerializeField] private HandPose[] handPoses;

        private readonly Dictionary<Sprite, HandPose> posesByFrame = new();
        private SpriteRenderer held;
        private SpriteRenderer grip;
        private SpriteRenderer body;
        private SpriteRenderer actorSprite;
        private Sprite carrySource;
        private Sprite carryBody;
        private Animator animator;
        private ItemCapabilities equippedItem;
        private bool equippedItemIsMelee;
        private bool actorWasHidden;
        private int bodyFrameStart = -1;
        private MeleeAttackExecution owner;
        private MeleeImpactFeedback feedback;

        public SpriteRenderer HeldRenderer => held;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            actorSprite = GetComponentInChildren<SpriteRenderer>();
            if (handPoses == null) return;
            foreach (var pose in handPoses)
                if (pose.frame != null) posesByFrame[pose.frame] = pose;
        }

        public void SetEquippedItem(ItemCapabilities item, GameplayTagRegistry registry)
        {
            if (ReferenceEquals(equippedItem, item)) return;
            equippedItem = item;
            equippedItemIsMelee = item != null && meleeTag != null && registry != null &&
                registry.TryGetTag(meleeTag.TagId, out var tag) && item.Tags.HasTag(tag);
            if (owner == null) PresentCarry();
        }

        public void Begin(MeleeAttackExecution execution)
        {
            if (owner != null && owner != execution) throw new InvalidOperationException("Swing view is already owned.");
            owner = execution;
            feedback ??= GetComponent<MeleeImpactFeedback>() ?? gameObject.AddComponent<MeleeImpactFeedback>();
            feedback.Begin(execution);

            RestoreCarryFrame();
            EnsureHeldRenderer();
            held.sprite = execution.Context.HeldSprite;
            held.enabled = held.sprite != null;
            BeginBodyAnimation(execution);
            Present(execution);
        }

        private bool CanAttachToBody => actorSprite != null && actorSprite.enabled && bodyAnimationController != null &&
            animator != null && animator.runtimeAnimatorController == bodyAnimationController;

        private void BeginBodyAnimation(MeleeAttackExecution execution)
        {
            if (!CanAttachToBody || execution.Context.Animation == null || held.sprite == null ||
                !equippedItemIsMelee || equippedItem.ItemId != execution.Context.ItemId ||
                bodyAnimationFrames == null || bodyAnimationFrames.Length != 4 * FramesPerDirection)
                return;

            var facing = execution.Context.Animation.Facing;
            int direction = facing == Vector2.down ? 0 : facing == Vector2.up ? 1 : facing == Vector2.left ? 2 : 3;
            int firstFrame = direction * FramesPerDirection;
            for (int frame = firstFrame; frame < firstFrame + FramesPerDirection; frame++)
            {
                var sprite = bodyAnimationFrames[frame];
                if (sprite == null || !posesByFrame.ContainsKey(sprite)) return;
            }

            if (body == null)
            {
                var visual = new GameObject("MeleeBodyAnimation");
                visual.transform.SetParent(actorSprite.transform, false);
                visual.layer = actorSprite.gameObject.layer;
                body = visual.AddComponent<SpriteRenderer>();
            }
            body.sharedMaterial = actorSprite.sharedMaterial;
            body.color = actorSprite.color;
            body.sortingLayerID = actorSprite.sortingLayerID;
            body.sortingOrder = actorSprite.sortingOrder;
            body.spriteSortPoint = actorSprite.spriteSortPoint;
            body.maskInteraction = actorSprite.maskInteraction;
            actorWasHidden = actorSprite.forceRenderingOff;
            body.forceRenderingOff = actorWasHidden;
            actorSprite.forceRenderingOff = true;
            body.enabled = true;
            bodyFrameStart = firstFrame;
        }

        public void Present(MeleeAttackExecution execution)
        {
            if (owner != execution || held == null || !isActiveAndEnabled) return;
            var spec = execution.Configuration;
            var timing = execution.Timing;
            float progress = (float)timing.PhaseProgress;
            if (bodyFrameStart >= 0)
            {
                int phaseFrame = timing.Phase switch
                {
                    MeleeAttackPhase.Windup => 0,
                    MeleeAttackPhase.Active => 1,
                    MeleeAttackPhase.Recovery when progress < 0.4f => 2,
                    _ => 3
                };
                int frame = bodyFrameStart + phaseFrame;
                body.sprite = bodyAnimationFrames[frame];
                var pose = posesByFrame[body.sprite];
                if (phaseFrame == FramesPerDirection - 1)
                {
                    // Recover into the exact equipped idle, including its hand
                    // cutout, held sprite, orientation and normal carry scale.
                    body.sprite = pose.carryBody != null ? pose.carryBody : pose.frame;
                    held.sprite = execution.Context.HeldSprite;
                    AttachToHand(body, pose);
                    return;
                }
                var insert = spec.WeaponFrame(frame);
                held.sprite = insert != null ? insert : execution.Context.HeldSprite;
                if (insert != null)
                {
                    // The registered insert already contains the projected tool and
                    // the exact visibility boundary of the gripping fingers.
                    pose.angle = 0f;
                    pose.lengthScale = 1f;
                    pose.behindBody = false;
                    pose.foregroundGrip = null;
                }
                AttachToHand(body, pose, insert != null ? 1f : spec.SwingWidthScale);
                return;
            }

            float angle = timing.Phase == MeleeAttackPhase.Windup ? Mathf.Lerp(spec.RestAngle, spec.StartAngle, progress) :
                timing.Phase == MeleeAttackPhase.Active ? Mathf.Lerp(spec.StartAngle, spec.EndAngle, progress) :
                Mathf.Lerp(spec.EndAngle, spec.RestAngle, progress);
            var direction = execution.Context.Direction;
            float facing = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            var rotation = Quaternion.Euler(0, 0, facing + angle - 90);
            var offset = (Vector3)originOffsetTiles * execution.Context.UnitsPerTile +
                rotation * Vector3.up * (spec.HeldDistanceTiles * execution.Context.UnitsPerTile);
            held.transform.SetPositionAndRotation(transform.TransformPoint(offset), transform.rotation * rotation);
            held.transform.localScale = Vector3.one;
            held.sortingOrder = actorSprite != null ? actorSprite.sortingOrder + 1 : 0;
            held.forceRenderingOff = actorSprite != null && actorSprite.forceRenderingOff;
            if (grip != null) grip.enabled = false;
        }

        private void LateUpdate()
        {
            // The Animator assigns the current walk sprite after Update.
            if (owner == null) PresentCarry();
        }

        private void PresentCarry()
        {
            var frame = actorSprite != null ? actorSprite.sprite : null;
            if (carryBody != null && frame == carryBody) frame = carrySource;

            if (isActiveAndEnabled && equippedItem?.HeldSprite != null && CanAttachToBody &&
                frame != null && posesByFrame.TryGetValue(frame, out var pose))
            {
                if (pose.carryBody != null)
                {
                    carrySource = frame;
                    carryBody = pose.carryBody;
                    actorSprite.sprite = carryBody;
                }
                else
                    RestoreCarryFrame();

                EnsureHeldRenderer();
                held.sprite = equippedItem.HeldSprite;
                held.enabled = true;
                AttachToHand(actorSprite, pose);
            }
            else
            {
                RestoreCarryFrame();
                HideHeldItem();
            }
        }

        private void RestoreCarryFrame()
        {
            if (carryBody != null && actorSprite != null && actorSprite.sprite == carryBody)
                actorSprite.sprite = carrySource;
            carrySource = carryBody = null;
        }

        private void EnsureHeldRenderer()
        {
            if (held != null) return;
            var visual = new GameObject("HeldItem");
            visual.transform.SetParent(transform, false);
            held = visual.AddComponent<SpriteRenderer>();
            if (actorSprite == null) return;
            visual.layer = actorSprite.gameObject.layer;
            held.sharedMaterial = actorSprite.sharedMaterial;
            held.sortingLayerID = actorSprite.sortingLayerID;
            held.maskInteraction = actorSprite.maskInteraction;
        }

        private void AttachToHand(SpriteRenderer source, HandPose pose, float widthScale = 1f)
        {
            var offset = (pose.gripPixels - source.sprite.pivot) / source.sprite.pixelsPerUnit;
            var socket = actorSprite.transform;
            held.transform.SetPositionAndRotation(socket.TransformPoint(offset),
                socket.rotation * Quaternion.Euler(0, 0, pose.angle));
            held.transform.localScale = new Vector3(widthScale, pose.lengthScale, 1f);
            held.sortingLayerID = source.sortingLayerID;
            held.sortingOrder = source.sortingOrder + (pose.behindBody ? -1 : 1);
            held.forceRenderingOff = source.forceRenderingOff;
            held.color = source.color;
            PresentGrip(source, pose.foregroundGrip, offset);
        }

        private void PresentGrip(SpriteRenderer source, Sprite foreground, Vector2 offset)
        {
            if (foreground == null)
            {
                if (grip != null) grip.enabled = false;
                return;
            }

            if (grip == null)
            {
                var visual = new GameObject("ForegroundGrip");
                visual.layer = actorSprite.gameObject.layer;
                visual.transform.SetParent(actorSprite.transform, false);
                grip = visual.AddComponent<SpriteRenderer>();
            }
            grip.sprite = foreground;
            grip.transform.localPosition = offset;
            grip.sharedMaterial = source.sharedMaterial;
            grip.color = source.color;
            grip.sortingLayerID = source.sortingLayerID;
            grip.sortingOrder = Mathf.Max(source.sortingOrder, held.sortingOrder) + 1;
            grip.maskInteraction = source.maskInteraction;
            grip.forceRenderingOff = source.forceRenderingOff;
            grip.enabled = true;
        }

        private void HideHeldItem()
        {
            if (held != null) held.enabled = false;
            if (grip != null) grip.enabled = false;
        }

        public void End(MeleeAttackExecution execution)
        {
            if (owner != execution) return;
            feedback?.End(execution);
            owner = null;
            RestoreBody();
            PresentCarry();
        }

        private void RestoreBody()
        {
            if (bodyFrameStart < 0) return;
            bodyFrameStart = -1;
            if (actorSprite != null) actorSprite.forceRenderingOff = actorWasHidden;
            if (body != null) body.enabled = false;
        }

        private void OnDisable()
        {
            if (owner != null) feedback?.End(owner);
            RestoreBody();
            RestoreCarryFrame();
            HideHeldItem();
        }
    }
}
