using System;
using SOLITUDE.Composition;
using SOLITUDE.Containers;
using SOLITUDE.Core.Interaction;
using SOLITUDE.Items;
using SOLITUDE.Player;
using SOLITUDE.Restoration;
using UnityEngine;

namespace SOLITUDE.World.CrewBackgrounds
{
    [DisallowMultipleComponent]
    public sealed class CrewBackgroundsRepairControl : InteractableBase
    {
        [Serializable]
        public sealed class ActionBinding
        {
            public CrewBackgroundsFault Fault;
            public CrewRepairOperation Operation;
            public ItemDefinition Material;
            public Transform AccessPoint;
            public string InspectionLabel;
            public CrewProficiency Skill;
            public CrewProficiencyRank MinimumRank;
            public string Clue;
            public string TrainedClue;
            public string ActionLabel;
            public string Completion;
        }

        [SerializeField] private ActionBinding[] actions;
        [SerializeField] private SpriteRenderer indicator;
        [SerializeField, Min(.1f)] private float workSeconds = .65f;
        private SceneFeatureContext context;
        private CrewBackgroundsPlaytestSetup setup;
        private CrewBackgroundsEncounterState target;
        private ActionBinding primary;
        private ActionBinding reclaim;
        private ActionBinding inspected;

        private Work work;

        private ActionBinding Action
        {
            get
            {
                if (target == null) return null;
                if (reclaim != null && target.JumperInstalled && target.NormalFeedAvailable) return reclaim;

                var player = context.Interactor;
                if (player == null) return primary;
                var position = player.transform.position;
                var selected = primary;
                float nearest = primary != null ? (position - transform.position).sqrMagnitude : float.PositiveInfinity;
                foreach (var candidate in actions)
                {
                    if (candidate.Fault != target.Fault || candidate.AccessPoint == null) continue;
                    float distance = (position - candidate.AccessPoint.position).sqrMagnitude;
                    if (distance >= nearest) continue;
                    selected = candidate;
                    nearest = distance;
                }
                return selected;
            }
        }

        public bool IsWorking => work != null;

        public void Configure(ActionBinding[] bindings, SpriteRenderer renderer)
        {
            actions = bindings;
            indicator = renderer;
        }

        public void ValidateAuthoring()
        {
            if (indicator == null || !float.IsFinite(workSeconds) || workSeconds <= 0 || actions == null || actions.Length == 0)
                throw new InvalidOperationException("Missing repair control authoring at " + name);

            var choices = new System.Collections.Generic.HashSet<(CrewBackgroundsFault, Transform, bool)>();
            foreach (var binding in actions)
            {
                if (binding == null || !choices.Add((binding.Fault, binding.AccessPoint, binding.Operation == CrewRepairOperation.ReclaimJumper))
                    || string.IsNullOrWhiteSpace(binding.Clue)
                    || binding.AccessPoint != null && (!binding.AccessPoint.IsChildOf(transform)
                        || binding.Operation == CrewRepairOperation.ReclaimJumper || string.IsNullOrWhiteSpace(binding.InspectionLabel))
                    || binding.Operation == CrewRepairOperation.FitIntakeLinkage
                        && string.IsNullOrWhiteSpace(binding.Material?.ItemId)
                    || binding.Operation != CrewRepairOperation.Inspect
                        && (string.IsNullOrWhiteSpace(binding.ActionLabel) || string.IsNullOrWhiteSpace(binding.Completion)))
                    throw new InvalidOperationException("Invalid repair action authoring at " + name);
            }
        }

        public void Bind(SceneFeatureContext actor, CrewBackgroundsPlaytestSetup profile, CrewBackgroundsEncounterState equipment)
        {
            Release();
            ValidateAuthoring();
            context = actor ?? throw new ArgumentNullException(nameof(actor));
            setup = profile;
            target = equipment ?? throw new ArgumentNullException(nameof(equipment));
            primary = Array.Find(actions, binding =>
                binding.Fault == equipment.Fault && binding.AccessPoint == null && binding.Operation != CrewRepairOperation.ReclaimJumper);
            reclaim = Array.Find(actions, binding =>
                binding.Fault == equipment.Fault && binding.Operation == CrewRepairOperation.ReclaimJumper);
        }

        public override string GetPrompt()
        {
            if (IsWorking) return "Working…";
            var action = Action;
            if (action == null || inspected != action || action.Operation == CrewRepairOperation.Inspect)
                return string.IsNullOrWhiteSpace(action?.InspectionLabel) ? "Inspect " + name : action.InspectionLabel;
            if (action.Operation == CrewRepairOperation.ResetBreaker && target.NormalFeedAvailable) return "Normal feed restored";
            if (action.Operation == CrewRepairOperation.FitIntakeLinkage && target.IntakeLinkageIntact) return "Airflow running";
            if (action.Operation == CrewRepairOperation.InstallJumper && target.JumperInstalled) return "Jumper installed";
            if (action.Operation == CrewRepairOperation.InstallJumper && target.NormalFeedAvailable) return "Normal feed available";
            return action.ActionLabel;
        }

        public override bool CanInteract(PlayerInteractor player) => isActiveAndEnabled;

        public override InteractionResult Interact(PlayerInteractor player)
        {
            if (!ActorAtControl(player) || !context.Policy.Gameplay)
                return InteractionResult.Blocked("Repair control unavailable");
            if (IsWorking) return InteractionResult.Blocked("Repair in progress");
            var action = Action;
            if (action == null) return InteractionResult.Success("This control has no active fault");
            if (inspected != action || action.Operation == CrewRepairOperation.Inspect)
            {
                inspected = action;
                var clue = setup.Rank(action.Skill) >= CrewProficiencyRank.Trained
                    && !string.IsNullOrWhiteSpace(action.TrainedClue) ? action.TrainedClue : action.Clue;
                return InteractionResult.Success(clue);
            }

            var check = Eligibility(action);
            if (!check.IsSuccess) return check;
            var runner = player.GetComponent<PlayerItemActionComposition>()?.Runner;
            var request = new Work(this, player, runner, action);
            return runner?.TryStart(request) == true
                ? InteractionResult.Success(action.ActionLabel + "…")
                : InteractionResult.Blocked("Cannot start repair right now");
        }

        private bool ActorAtControl(PlayerInteractor player) => isActiveAndEnabled && target != null
            && context?.Policy?.Ready == true && player != null && player == context.Interactor
            && (player.transform.position - transform.position).sqrMagnitude <= player.interactRange * player.interactRange;

        private InteractionResult Eligibility(ActionBinding action)
        {
            if (setup.Rank(action.Skill) < action.MinimumRank)
            {
                string skill = action.Skill == CrewProficiency.FieldRepair ? "Field Repair" : action.Skill.ToString();
                return InteractionResult.Blocked("Requires " + action.MinimumRank + " " + skill);
            }

            return IsJumperAction(action)
                ? JumperResult(action, target.CheckJumper(action.Operation, context.Commands, context.Inventory))
                : Result(action, target.Check(action.Operation, context.Commands, context.Inventory, action.Material?.ItemId));
        }

        private InteractionResult Complete(ActionBinding action)
        {
            if (Action != action) return InteractionResult.Blocked("Repair conditions changed");
            var check = Eligibility(action);
            if (!check.IsSuccess) return check;
            return IsJumperAction(action)
                ? JumperResult(action, target.TryMoveJumper(action.Operation, context.Commands, context.Inventory))
                : Result(action, target.TryRepair(action.Operation, context.Commands, context.Inventory, action.Material?.ItemId));
        }

        private static bool IsJumperAction(ActionBinding binding) =>
            binding.Operation == CrewRepairOperation.InstallJumper || binding.Operation == CrewRepairOperation.ReclaimJumper;

        private InteractionResult JumperResult(ActionBinding action, CommandStatus status)
        {
            switch (status)
            {
                case CommandStatus.Applied: return InteractionResult.Success(action.Completion);
                case CommandStatus.InsufficientQuantity: return InteractionResult.Blocked("Requires 1 reusable jumper");
                case CommandStatus.InsufficientSpace: return InteractionResult.Blocked("Inventory full. Jumper stays in the panel.");
                default:
                    if (action.Operation == CrewRepairOperation.InstallJumper && target.JumperInstalled)
                        return InteractionResult.Blocked("Jumper already installed");
                    if (action.Operation == CrewRepairOperation.InstallJumper && target.NormalFeedAvailable)
                        return InteractionResult.Blocked("Normal feed is already available");
                    return InteractionResult.Blocked("Jumper action unavailable");
            }
        }

        private static InteractionResult Result(ActionBinding action, RestorationOutcome outcome)
        {
            switch (outcome)
            {
                case RestorationOutcome.Restored: return InteractionResult.Success(action.Completion);
                case RestorationOutcome.AlreadyRestored: return InteractionResult.Blocked("Repair already complete");
                case RestorationOutcome.MissingItem: return InteractionResult.Blocked("Requires 1 " + action.Material.DisplayName);
                default: return InteractionResult.Blocked("Repair unavailable");
            }
        }

        public bool InstantCancel()
        {
            if (work == null) return false;
            work.Runner.Cancel();
            return true;
        }

        public void Release()
        {
            InstantCancel();
            inspected = null;
            context = null;
            target = null;
            primary = null;
            reclaim = null;
        }

        private void OnDisable() => InstantCancel();

        private sealed class Work : IItemActionRequest, IItemActionExecution
        {
            private readonly CrewBackgroundsRepairControl control;
            private readonly PlayerInteractor player;
            private readonly ActionBinding action;
            private SpriteRenderer workIndicator;
            private Color originalColor;
            private double elapsed;

            public Work(CrewBackgroundsRepairControl control, PlayerInteractor player, SOLITUDE.Application.ItemActionRunner runner, ActionBinding action)
            {
                this.control = control;
                this.player = player;
                Runner = runner;
                this.action = action;
            }

            public SOLITUDE.Application.ItemActionRunner Runner { get; }
            public bool IsValid => control != null && control.ActorAtControl(player) && control.Action == action;
            public bool IsComplete { get; private set; }
            public double CommittedDuration => 0;

            public IItemActionExecution Start()
            {
                workIndicator = action.AccessPoint != null ? action.AccessPoint.GetComponent<SpriteRenderer>() : null;
                if (workIndicator == null) workIndicator = control.indicator;
                originalColor = workIndicator.color;
                control.work = this;
                return this;
            }

            public void Advance(double gameplaySeconds)
            {
                elapsed += gameplaySeconds;
                workIndicator.color = Color.Lerp(originalColor, new Color(1f, .72f, .32f),
                    (float)(elapsed / control.workSeconds));
                if (elapsed < control.workSeconds) return;

                IsComplete = true;
                var result = control.Complete(action);
                control.context?.Feedback?.Handle(result);
            }

            public void Interrupt()
            {
                if (control == null || control.work != this) return;
                control.work = null;
                if (workIndicator != null) workIndicator.color = originalColor;
                if (!IsComplete && control.context?.Policy?.Ready == true)
                    control.context.Feedback?.Handle(InteractionResult.Blocked("Repair interrupted"));
            }
        }
    }
}
