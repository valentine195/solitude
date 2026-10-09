using System;
using System.Collections.Generic;
using SOLITUDE.Survivors;

namespace SOLITUDE.Application.Survivors
{
    public enum SuccessionOutcome { Completed, StaleSurvivor, Busy, Failed }
    public enum SuccessionStage { Ready, Prepared, EquipmentCommitted, AvatarActivated }

    // One scene lifetime; no ship state, Unity endpoints, roster, or save schema.
    public sealed class SurvivorSuccessionCoordinator
    {
        private readonly ISurvivorSuccessionPorts ports;
        private readonly Func<SurvivorState, SurvivorState> chooseSuccessor;
        private readonly HashSet<Guid> usedIdentities = new HashSet<Guid>();
        private bool executing;
        public SurvivorState Current { get; private set; }
        public SurvivorTransition Pending { get; private set; }
        public SurvivorEquipmentReceipt Receipt { get; private set; }
        public SuccessionStage Stage { get; private set; }
        public string LastError { get; private set; }

        public SurvivorSuccessionCoordinator(SurvivorState initial, ISurvivorSuccessionPorts ports,
            Func<SurvivorState, SurvivorState> chooseSuccessor = null)
        {
            if (initial == null || initial.Status != SurvivorLifeStatus.Active)
                throw new ArgumentException("An active initial survivor is required.", nameof(initial));
            this.ports = ports ?? throw new ArgumentNullException(nameof(ports));
            this.chooseSuccessor = chooseSuccessor ?? (_ => new SurvivorState());
            Current = initial;
            usedIdentities.Add(initial.Id);
        }

        public SuccessionOutcome RequestDeath(Guid expectedSurvivor)
        {
            if (executing) return SuccessionOutcome.Busy;
            if (Pending != null)
            {
                if (Pending.Departing.Id != expectedSurvivor) return SuccessionOutcome.StaleSurvivor;
                return Run();
            }
            if (Current == null || Current.Id != expectedSurvivor || Current.Status != SurvivorLifeStatus.Active)
                return SuccessionOutcome.StaleSurvivor;
            return Run();
        }

        public SuccessionOutcome Retry()
        {
            if (executing) return SuccessionOutcome.Busy;
            return Pending == null ? SuccessionOutcome.StaleSurvivor : Run();
        }

        private SuccessionOutcome Run()
        {
            executing = true;
            LastError = null;
            try
            {
                ports.SetTransitionGated(true);
                if (Pending == null)
                {
                    var successor = chooseSuccessor(Current);
                    if (successor == null || successor.Status != SurvivorLifeStatus.Pending || usedIdentities.Contains(successor.Id))
                        throw new InvalidOperationException("Successor must have an unused pending identity.");
                    Pending = new SurvivorTransition(Current, successor);
                }
                if (Stage == SuccessionStage.Ready)
                {
                    if (!ports.TryPrepare(Pending, out var error)) return Fail(error, "Preparation failed.");
                    Stage = SuccessionStage.Prepared;
                }
                if (Stage == SuccessionStage.Prepared)
                {
                    if (!ports.TryCommitEquipment(Pending, out var receipt, out var error))
                        return Fail(error, "Equipment handoff failed.");
                    // Retain evidence before validating the port's result. Even a bad
                    // receipt must never cause a second transfer of committed contents.
                    Receipt = receipt;
                    Stage = SuccessionStage.EquipmentCommitted;
                    Pending.Departing.TryRetire();
                    Current = null;
                    usedIdentities.Add(Pending.Successor.Id);
                }
                if (Receipt == null || Receipt.TransitionId != Pending.Id)
                    return Fail("Equipment handoff returned an invalid receipt.", null);
                if (Stage == SuccessionStage.EquipmentCommitted)
                {
                    if (!ports.TryActivate(Pending, Receipt, out var error)) return Fail(error, "Avatar activation failed.");
                    if (!Pending.Successor.TryActivate()) throw new InvalidOperationException("Successor cannot become active.");
                    Current = Pending.Successor;
                    Stage = SuccessionStage.AvatarActivated;
                }
                if (!ports.TryAcknowledge(Pending, out var acknowledgmentError))
                    return Fail(acknowledgmentError, "Acknowledgment failed.");
                ports.SetTransitionGated(false);
                Pending = null;
                Receipt = null;
                Stage = SuccessionStage.Ready;
                return SuccessionOutcome.Completed;
            }
            catch (Exception error) { return Fail(error.Message, "Succession failed."); }
            finally { executing = false; }
        }

        private SuccessionOutcome Fail(string error, string fallback)
        {
            LastError = string.IsNullOrWhiteSpace(error) ? fallback : error;
            // Before commit, rejection leaves the existing survivor/equipment usable.
            // After commit, recovery ownership must remain live and retry goes forward.
            if (Stage == SuccessionStage.Ready || Stage == SuccessionStage.Prepared)
            {
                try
                {
                    Stage = SuccessionStage.Ready;
                    if (Pending != null) ports.CancelPreparation(Pending);
                    ports.SetTransitionGated(false);
                    Pending = null;
                    Receipt = null;
                    Stage = SuccessionStage.Ready;
                }
                catch (Exception cleanupError)
                { LastError += " Preparation cleanup failed: " + cleanupError.Message; }
            }
            return SuccessionOutcome.Failed;
        }
    }
}
