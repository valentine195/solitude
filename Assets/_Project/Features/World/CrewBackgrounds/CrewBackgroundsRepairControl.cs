using System;
using SOLITUDE.Composition;
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
        private ActionBinding action;
        private Work work;
        private bool inspected;

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

            var faults = new System.Collections.Generic.HashSet<CrewBackgroundsFault>();
            foreach (var binding in actions)
            {
                if (binding == null || !faults.Add(binding.Fault) || string.IsNullOrWhiteSpace(binding.Clue)
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
            action = Array.Find(actions, binding => binding.Fault == equipment.Fault);
        }

        public override string GetPrompt()
        {
            if (IsWorking) return "Working…";
            if (action == null) return "Inspect " + name;
            if (target.Airflow) return "Airflow running";
            if (!inspected || action.Operation == CrewRepairOperation.Inspect) return "Inspect " + name;
            return action.ActionLabel;
        }

        public override bool CanInteract(PlayerInteractor player) => isActiveAndEnabled;

        public override InteractionResult Interact(PlayerInteractor player)
        {
            if (!ActorAtControl(player) || !context.Policy.Gameplay)
                return InteractionResult.Blocked("Repair control unavailable");
            if (IsWorking) return InteractionResult.Blocked("Repair in progress");
            if (action == null) return InteractionResult.Success("This control has no active fault");
            if (!inspected || action.Operation == CrewRepairOperation.Inspect)
            {
                inspected = true;
                var clue = setup.Rank(action.Skill) >= CrewProficiencyRank.Trained
                    && !string.IsNullOrWhiteSpace(action.TrainedClue) ? action.TrainedClue : action.Clue;
                return InteractionResult.Success(clue);
            }

            var check = Eligibility();
            if (!check.IsSuccess) return check;
            var runner = player.GetComponent<PlayerItemActionComposition>()?.Runner;
            var request = new Work(this, player, runner);
            return runner?.TryStart(request) == true
                ? InteractionResult.Success(action.ActionLabel + "…")
                : InteractionResult.Blocked("Cannot start repair right now");
        }

        private bool ActorAtControl(PlayerInteractor player) => isActiveAndEnabled && target != null
            && context?.Policy?.Ready == true && player != null && player == context.Interactor
            && (player.transform.position - transform.position).sqrMagnitude <= player.interactRange * player.interactRange;

        private InteractionResult Eligibility()
        {
            if (setup.Rank(action.Skill) < action.MinimumRank)
                return InteractionResult.Blocked("Requires " + action.MinimumRank + " " + action.Skill);

            return Result(target.Check(action.Operation, context.Commands, context.Inventory, action.Material?.ItemId));
        }

        private InteractionResult Result(RestorationOutcome outcome)
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
            inspected = false;
            context = null;
            target = null;
            action = null;
        }

        private void OnDisable() => InstantCancel();

        private sealed class Work : IItemActionRequest, IItemActionExecution
        {
            private readonly CrewBackgroundsRepairControl control;
            private readonly PlayerInteractor player;
            private Color originalColor;
            private double elapsed;

            public Work(CrewBackgroundsRepairControl control, PlayerInteractor player, SOLITUDE.Application.ItemActionRunner runner)
            {
                this.control = control;
                this.player = player;
                Runner = runner;
            }

            public SOLITUDE.Application.ItemActionRunner Runner { get; }
            public bool IsValid => control != null && control.ActorAtControl(player);
            public bool IsComplete { get; private set; }
            public double CommittedDuration => 0;

            public IItemActionExecution Start()
            {
                originalColor = control.indicator.color;
                control.work = this;
                return this;
            }

            public void Advance(double gameplaySeconds)
            {
                elapsed += gameplaySeconds;
                control.indicator.color = Color.Lerp(originalColor, new Color(1f, .72f, .32f),
                    (float)(elapsed / control.workSeconds));
                if (elapsed < control.workSeconds) return;

                IsComplete = true;
                var check = control.Eligibility();
                var result = check.IsSuccess
                    ? control.Result(control.target.TryRepair(control.action.Operation, control.context.Commands,
                        control.context.Inventory, control.action.Material?.ItemId))
                    : check;
                control.context.Feedback?.Handle(result);
            }

            public void Interrupt()
            {
                if (control == null || control.work != this) return;
                control.work = null;
                control.indicator.color = originalColor;
                if (!IsComplete && control.context?.Policy?.Ready == true)
                    control.context.Feedback?.Handle(InteractionResult.Blocked("Repair interrupted"));
            }
        }
    }
}
