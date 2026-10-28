using System;
using SOLITUDE.Application;
using SOLITUDE.Player.Vitals;
using SOLITUDE.Vitals;
using SOLITUDE.Survivors;
using UnityEngine;

namespace SOLITUDE.Player
{
    public class PlayerVitalsController : MonoBehaviour, IVitalEffectTarget
    {
        private static PlayerVitalsController _instance;
        public static PlayerVitalsController Instance
        {
            get
            {
                if (_instance == null) _instance = FindAnyObjectByType<PlayerVitalsController>();
                return _instance;
            }
        }

        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float maxOxygen = 100f;
        [SerializeField] private float currentHealth = 100f;
        [SerializeField] private float currentOxygen = 100f;
        [SerializeField] private float maxHunger = 100f;
        [SerializeField] private float currentHunger = 100f;
        [SerializeField] private int healthSegments = 4;
        [SerializeField] private double fedThresholdFraction = .2;
        [SerializeField] private double hungerDepletionRate = .1;
        [SerializeField] private double recoveryDelay = 5;
        [SerializeField] private double recoveryRate = 2;

        private VitalsRecoverySettings settings;
        private VitalsRecoveryState timing;
        private InputPolicyCoordinator policy;
        private object bindingOwner;
        private Func<bool> bindingAvailable;
        private bool publishing;
        private PreparedVitalsChange reserved;
        private bool reservedApplied;
        private Vital.Change healthChange, hungerChange, oxygenChange;
        private double observedHealthMaximum, observedHungerMaximum;

        public PlayerVitals Vitals { get; private set; }
        public VitalsRecoverySettings Settings => settings;
        public VitalsRecoveryState RecoveryState => timing;
        public long Revision { get; private set; }
        public event Action PresentationChanged;
        private Action cachedPresentation;
        private Delegate[] presentationObservers = Array.Empty<Delegate>();
        public event Action<Exception> ObserverError;
        public bool GameplayEligible => bindingOwner != null && policy != null && policy.Gameplay
            && isActiveAndEnabled && (bindingAvailable?.Invoke() ?? false);
        public bool IsConfigurationValid => settings != null && settings.IsValid
            && settings.MaxHealth <= float.MaxValue && settings.MaxHunger <= float.MaxValue
            && Finite(currentHealth) && Finite(currentHunger);
        public string ConfigurationError => IsConfigurationValid ? null : "Invalid player vitals configuration";
        public RecoveryReason RecoveryStatus => VitalsRecoveryRules.Describe(RulesSettings, ReadValues(), timing, GameplayEligible);
        public double PassiveRecoveryCeiling => VitalsRecoveryRules.Ceiling(RulesSettings, Vitals?.Health.PreciseCurrent ?? 0);
        private VitalsRecoverySettings RulesSettings => IsConfigurationValid ? settings : null;
        private VitalsValues ReadValues() => Vitals == null ? default
            : new VitalsValues(Vitals.Health.PreciseCurrent, Vitals.Hunger.PreciseCurrent);
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static float SafeMaximum(float value) => Finite(value) && value > 0 ? value : 100;
        private static double Authored(float value, Vital vital) => Finite(value)
            ? Math.Max(0, Math.Min(vital.PreciseMax, value)) : vital.PreciseCurrent;

        private VitalsRecoverySettings AuthoredSettings() => new VitalsRecoverySettings(maxHealth, maxHunger,
            healthSegments, fedThresholdFraction, hungerDepletionRate, recoveryDelay, recoveryRate);

        private void EnsureVitals()
        {
            if (Vitals != null) return;
            settings = AuthoredSettings();
            // Invalid authoring remains invalid; existing safe values are not corrupted by it.
            Vitals = new PlayerVitals(SafeMaximum(maxHealth), SafeMaximum(maxOxygen), SafeMaximum(maxHunger));
            ObserveMaximums();
            Vitals.Health.Changed += ValuesChanged;
            Vitals.Hunger.Changed += ValuesChanged;
            Vitals.Oxygen.Changed += ValuesChanged;
            Vitals.Health.ObserverError += Report;
            Vitals.Hunger.ObserverError += Report;
            Vitals.Oxygen.ObserverError += Report;
        }

        public void ResetForAwakening()
        {
            if (publishing) return;
            EnsureVitals(); BeginMutation();
            healthChange = Vitals.Health.ApplySilently(Vitals.Health.PreciseMax, Authored(currentHealth, Vitals.Health));
            oxygenChange = Vitals.Oxygen.ApplySilently(Vitals.Oxygen.PreciseMax, Authored(currentOxygen, Vitals.Oxygen));
            hungerChange = Vitals.Hunger.ApplySilently(Vitals.Hunger.PreciseMax, Authored(currentHunger, Vitals.Hunger));
            timing = default;
            FinishMutation(true);
        }

        public void CaptureVitals(SurvivorSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            EnsureVitals();
            if (publishing) throw new InvalidOperationException("Cannot capture vitals during a mutation/publication.");
            snapshot.health = new SavedVital { current = Vitals.Health.PreciseCurrent, max = Vitals.Health.PreciseMax };
            snapshot.oxygen = new SavedVital { current = Vitals.Oxygen.PreciseCurrent, max = Vitals.Oxygen.PreciseMax };
            snapshot.hunger = new SavedVital { current = Vitals.Hunger.PreciseCurrent, max = Vitals.Hunger.PreciseMax };
            snapshot.recoveryDelayRemaining = timing.DelayRemaining;
        }

        public void ValidateRestore(SurvivorSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            EnsureVitals(); snapshot.Validate();
            if (publishing) throw new InvalidOperationException("Cannot restore vitals during a mutation/publication.");
            if (!IsConfigurationValid || snapshot.health.max > float.MaxValue || snapshot.oxygen.max > float.MaxValue
                || snapshot.hunger.max > float.MaxValue)
                throw new System.IO.InvalidDataException("Saved vitals are incompatible with this avatar.");
        }

        public void RestoreVitals(SurvivorSnapshot snapshot)
        {
            ValidateRestore(snapshot);
            // Preserve the scene's recovery rules, restore saved maxima/values and timing together.
            var next = new VitalsRecoverySettings(snapshot.health.max, snapshot.hunger.max, settings.SegmentCount,
                settings.FedThresholdFraction, settings.HungerDepletionRate, settings.DamageDelay, settings.RecoveryRate);
            if (!next.IsValid) throw new System.IO.InvalidDataException("Saved vitals settings are invalid.");
            BeginMutation(); settings = next;
            healthChange = Vitals.Health.ApplySilently(snapshot.health.max, snapshot.health.current);
            oxygenChange = Vitals.Oxygen.ApplySilently(snapshot.oxygen.max, snapshot.oxygen.current);
            hungerChange = Vitals.Hunger.ApplySilently(snapshot.hunger.max, snapshot.hunger.current);
            timing = new VitalsRecoveryState(snapshot.recoveryDelayRemaining);
            FinishMutation(true);
        }

        public bool Configure(VitalsRecoverySettings next)
        {
            if (publishing) return false;
            EnsureVitals(); BeginMutation(); settings = next;
            if (IsConfigurationValid)
            {
                healthChange = Vitals.Health.ApplySilently(settings.MaxHealth, Math.Min(settings.MaxHealth, Vitals.Health.PreciseCurrent));
                hungerChange = Vitals.Hunger.ApplySilently(settings.MaxHunger, Math.Min(settings.MaxHunger, Vitals.Hunger.PreciseCurrent));
            }
            FinishMutation(true);
            return IsConfigurationValid;
        }

        public VitalsRecoveryResult Damage(double amount)
        {
            EnsureVitals();
            var result = VitalsRecoveryRules.Damage(RulesSettings, ReadValues(), timing,
                publishing ? double.NaN : amount, GameplayEligible);
            if (result.Outcome == VitalsRecoveryOutcome.Applied) Apply(result);
            return result;
        }

        public VitalsRecoveryResult Advance(double scaledElapsedSeconds)
        {
            EnsureVitals();
            var result = VitalsRecoveryRules.Advance(RulesSettings, ReadValues(), timing,
                publishing ? double.NaN : scaledElapsedSeconds, GameplayEligible);
            if (result.Outcome == VitalsRecoveryOutcome.Applied) Apply(result);
            return result;
        }

        public sealed class PreparedVitalsChange
        {
            internal readonly PlayerVitalsController Owner;
            internal readonly long Revision;
            internal readonly VitalsRecoveryResult Result;
            internal PreparedVitalsChange(PlayerVitalsController owner, long revision, VitalsRecoveryResult result)
            { Owner = owner; Revision = revision; Result = result; }
            public VitalsValues Values => Result.Values;
        }

        public PreparedVitalsChange PrepareFood(double amount) => Prepare(amount, true);
        public PreparedVitalsChange PrepareMedical(double amount) => Prepare(amount, false);
        private PreparedVitalsChange Prepare(double amount, bool food)
        {
            EnsureVitals();
            if (publishing) return null;
            var result = food ? VitalsRecoveryRules.RestoreFood(RulesSettings, ReadValues(), timing, amount, GameplayEligible)
                : VitalsRecoveryRules.RestoreHealth(RulesSettings, ReadValues(), timing, amount, GameplayEligible);
            return result.Outcome == VitalsRecoveryOutcome.Applied ? new PreparedVitalsChange(this, Revision, result) : null;
        }

        public bool TryApplyPrepared(PreparedVitalsChange change)
        {
            if (!TryReservePrepared(change)) return false;
            ApplyReservedValues(); CompleteReserved(); return true;
        }

        public VitalUseOutcome TryReserve(VitalConsumableSpec effect, out IVitalEffectReservation reservation)
        {
            reservation = null; EnsureVitals();
            if (publishing) return VitalUseOutcome.Busy;
            if (effect == null || !effect.Supported) return VitalUseOutcome.Unsupported;
            if (!effect.IsValid) return VitalUseOutcome.InvalidEffect;
            if (!GameplayEligible) return VitalUseOutcome.Unavailable;
            if (!IsConfigurationValid) return VitalUseOutcome.InvalidConfiguration;
            if (Vitals.Health.PreciseCurrent <= 0) return VitalUseOutcome.Dead;
            var vital = effect.Kind == VitalConsumableKind.Food ? Vitals.Hunger : Vitals.Health;
            if (vital.PreciseCurrent >= vital.PreciseMax) return VitalUseOutcome.Full;
            var prepared = effect.Kind == VitalConsumableKind.Food ? PrepareFood(effect.Amount) : PrepareMedical(effect.Amount);
            if (prepared == null) return VitalUseOutcome.InvalidEffect;
            // Allocate before acquiring the non-publishing mutation boundary.
            var next = new ConsumableReservation(this);
            if (!TryReservePrepared(prepared)) return VitalUseOutcome.Unavailable;
            reservation = next; return VitalUseOutcome.Ready;
        }
        private sealed class ConsumableReservation : IVitalEffectReservation
        {
            private PlayerVitalsController owner;
            public ConsumableReservation(PlayerVitalsController owner) { this.owner = owner; }
            public void ApplySilently() => owner?.ApplyReservedValues();
            public void Complete()
            {
                var target = owner; owner = null;
                if (target != null) target.CompleteReserved();
            }
        }

        // Reserve before inventory mutation, apply without callbacks in its commit, then
        // publish after inventory publication. Phase 2 supplies the inventory adapter.
        internal bool TryReservePrepared(PreparedVitalsChange change)
        {
            if (publishing || !GameplayEligible || change == null || change.Owner != this
                || change.Revision != Revision || !change.Result.Accepted) return false;
            BeginMutation(); reserved = change; return true;
        }
        internal void ApplyReservedValues()
        {
            if (reserved == null || reservedApplied) return;
            SetPreparedValues(reserved.Result); reservedApplied = true;
        }
        internal void CompleteReserved()
        {
            if (reserved == null) return;
            bool applied = reservedApplied; reserved = null; reservedApplied = false;
            FinishMutation(applied);
        }

        internal void BindGameplay(object owner, InputPolicyCoordinator source, Func<bool> available)
        {
            if (owner == null || source == null || available == null) throw new ArgumentNullException(nameof(source));
            if (bindingOwner != null && !ReferenceEquals(bindingOwner, owner))
                throw new InvalidOperationException("Player vitals already have a gameplay owner.");
            EnsureVitals();
            if (policy != null) policy.Changed -= RefreshPresentation;
            bindingOwner = owner; policy = source; bindingAvailable = available;
            policy.Changed += RefreshPresentation; RefreshPresentation();
        }
        internal void UnbindGameplay(object owner)
        {
            if (!ReferenceEquals(bindingOwner, owner)) return;
            if (policy != null) policy.Changed -= RefreshPresentation;
            bindingOwner = null; policy = null; bindingAvailable = null; RefreshPresentation();
        }

        private void ValuesChanged(Vital vital)
        {
            if (publishing) return;
            // Only an actual supported maximum change corrects that setting. A current
            // value or O2 notification must not replace rejected configuration.
            bool healthMaximumChanged = observedHealthMaximum != Vitals.Health.PreciseMax;
            bool hungerMaximumChanged = observedHungerMaximum != Vitals.Hunger.PreciseMax;
            if (settings != null && (healthMaximumChanged || hungerMaximumChanged))
                settings = new VitalsRecoverySettings(healthMaximumChanged ? Vitals.Health.PreciseMax : settings.MaxHealth,
                    hungerMaximumChanged ? Vitals.Hunger.PreciseMax : settings.MaxHunger,
                    settings.SegmentCount, settings.FedThresholdFraction, settings.HungerDepletionRate,
                    settings.DamageDelay, settings.RecoveryRate);
            ObserveMaximums();
            RefreshPresentation();
        }
        private void ObserveMaximums()
        {
            observedHealthMaximum = Vitals.Health.PreciseMax;
            observedHungerMaximum = Vitals.Hunger.PreciseMax;
        }
        internal void RefreshPresentation()
        {
            if (Vitals == null || publishing) return;
            BeginMutation(); FinishMutation(true);
        }
        private void Apply(VitalsRecoveryResult result)
        { BeginMutation(); SetPreparedValues(result); FinishMutation(true); }
        private void SetPreparedValues(VitalsRecoveryResult result)
        {
            healthChange = Vitals.Health.ApplySilently(Vitals.Health.PreciseMax, result.Values.Health);
            hungerChange = Vitals.Hunger.ApplySilently(Vitals.Hunger.PreciseMax, result.Values.Hunger);
            timing = result.State;
        }
        private void BeginMutation()
        {
            publishing = true; healthChange = default; hungerChange = default; oxygenChange = default;
            Vitals.Health.BeginPublication(); Vitals.Hunger.BeginPublication(); Vitals.Oxygen.BeginPublication();
        }
        private void FinishMutation(bool notify)
        {
            try
            {
                if (!notify) return;
                ObserveMaximums();
                Revision++;
                Vitals.Health.Publish(healthChange);
                Vitals.Hunger.Publish(hungerChange);
                Vitals.Oxygen.Publish(oxygenChange);
                if (!ReferenceEquals(cachedPresentation, PresentationChanged))
                { cachedPresentation = PresentationChanged; presentationObservers = PresentationChanged?.GetInvocationList() ?? Array.Empty<Delegate>(); }
                foreach (Action observer in presentationObservers)
                    try { observer(); } catch (Exception error) { Report(error); }
            }
            finally
            {
                Vitals.Health.EndPublication(); Vitals.Hunger.EndPublication(); Vitals.Oxygen.EndPublication();
                publishing = false;
            }
        }
        private void Report(Exception error)
        {
            if (ObserverError == null) { Debug.LogWarning("Vitals observer failed: " + error.Message, this); return; }
            foreach (Action<Exception> observer in ObserverError.GetInvocationList())
                try { observer(error); } catch { }
        }

        private void Awake() => ResetForAwakening();
        private void OnEnable() => RefreshPresentation();
        private void OnDisable() => RefreshPresentation();
        private void OnValidate() { if (Vitals != null) Configure(AuthoredSettings()); }
        private void OnDestroy()
        {
            if (policy != null) policy.Changed -= RefreshPresentation;
            bindingOwner = null; policy = null; bindingAvailable = null;
            if (Vitals != null)
            {
                Vitals.Health.Changed -= ValuesChanged; Vitals.Hunger.Changed -= ValuesChanged; Vitals.Oxygen.Changed -= ValuesChanged;
                Vitals.Health.ObserverError -= Report; Vitals.Hunger.ObserverError -= Report; Vitals.Oxygen.ObserverError -= Report;
            }
            if (_instance == this) _instance = null;
        }
    }
}
