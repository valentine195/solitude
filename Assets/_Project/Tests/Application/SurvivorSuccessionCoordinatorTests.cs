using System;
using System.Collections.Generic;
using NUnit.Framework;
using SOLITUDE.Survivors;
using SOLITUDE.Application.Survivors;

namespace SOLITUDE.Tests
{
    public class SurvivorSuccessionCoordinatorTests
    {
        private sealed class Ports : ISurvivorSuccessionPorts
        {
            public bool Gated, FailPreparation, FailCommit, FailActivation, FailAcknowledgment;
            public bool ThrowPreparation, ThrowActivation, ThrowAfterAcknowledgment, InvalidReceipt;
            public int Preparations, Cancellations, Commits, Activations, Acknowledgments;
            public Action OnCommit;
            private readonly HashSet<Guid> delivered = new HashSet<Guid>();
            public void SetTransitionGated(bool gated) => Gated = gated;
            public bool TryPrepare(SurvivorTransition transition, out string error)
            {
                Preparations++; error = "prepare rejected";
                if (ThrowPreparation) throw new InvalidOperationException("prepare exception");
                return !FailPreparation;
            }
            public void CancelPreparation(SurvivorTransition transition) => Cancellations++;
            public bool TryCommitEquipment(SurvivorTransition transition, out SurvivorEquipmentReceipt receipt, out string error)
            {
                receipt = null; error = "commit rejected";
                if (FailCommit) return false;
                Commits++;
                OnCommit?.Invoke();
                receipt = new SurvivorEquipmentReceipt(InvalidReceipt ? Guid.NewGuid() : transition.Id);
                return true;
            }
            public bool TryActivate(SurvivorTransition transition, SurvivorEquipmentReceipt receipt, out string error)
            {
                error = "activation rejected";
                if (ThrowActivation) throw new InvalidOperationException("activation exception");
                if (FailActivation) return false;
                Activations++; return true;
            }
            public bool TryAcknowledge(SurvivorTransition transition, out string error)
            {
                error = "acknowledgment rejected";
                if (FailAcknowledgment) return false;
                if (delivered.Add(transition.Id)) Acknowledgments++;
                if (ThrowAfterAcknowledgment) throw new InvalidOperationException("uncertain delivery");
                return true;
            }
        }

        private static SurvivorState Active()
        { var result = new SurvivorState(); result.TryActivate(); return result; }

        [Test] public void ConsecutiveDeathsRetireIdentitiesAndRejectDelayedRequests()
        {
            var ports = new Ports(); var a = Active();
            var flow = new SurvivorSuccessionCoordinator(a, ports);
            Assert.AreSame(a, flow.Current);
            Assert.AreEqual(SuccessionOutcome.Completed, flow.RequestDeath(a.Id));
            var b = flow.Current;
            Assert.AreEqual(SurvivorLifeStatus.Dead, a.Status);
            Assert.AreEqual(SurvivorLifeStatus.Active, b.Status);
            Assert.AreNotEqual(a.Id, b.Id);
            Assert.AreEqual(SuccessionOutcome.StaleSurvivor, flow.RequestDeath(a.Id));
            Assert.AreEqual(1, ports.Commits);
            Assert.AreEqual(SuccessionOutcome.Completed, flow.RequestDeath(b.Id));
            var c = flow.Current;
            Assert.AreEqual(SurvivorLifeStatus.Dead, b.Status);
            Assert.AreNotEqual(a.Id, c.Id); Assert.AreNotEqual(b.Id, c.Id);
            Assert.AreEqual(2, ports.Commits); Assert.AreEqual(2, ports.Acknowledgments);
            Assert.IsFalse(ports.Gated); Assert.IsNull(flow.Pending);
            Assert.IsFalse(a.TryActivate()); Assert.IsFalse(b.TryActivate());
        }

        [TestCase(false)] [TestCase(true)]
        public void PreparationFailureLeavesTheCurrentSurvivorAndEquipmentUntouched(bool throws)
        {
            var ports = new Ports { FailPreparation = true, ThrowPreparation = throws };
            var a = Active(); var flow = new SurvivorSuccessionCoordinator(a, ports);
            Assert.AreEqual(SuccessionOutcome.Failed, flow.RequestDeath(a.Id));
            Assert.AreSame(a, flow.Current); Assert.AreEqual(SurvivorLifeStatus.Active, a.Status);
            Assert.AreEqual(0, ports.Commits); Assert.AreEqual(0, ports.Activations);
            Assert.AreEqual(0, ports.Acknowledgments); Assert.AreEqual(1, ports.Cancellations);
            Assert.IsFalse(ports.Gated); Assert.IsNull(flow.Pending); Assert.IsNotEmpty(flow.LastError);
            ports.FailPreparation = false; ports.ThrowPreparation = false;
            Assert.AreEqual(SuccessionOutcome.Completed, flow.RequestDeath(a.Id));
        }

        [Test] public void RejectedCommitDoesNotRetireTheSurvivor()
        {
            var ports = new Ports { FailCommit = true }; var a = Active();
            var flow = new SurvivorSuccessionCoordinator(a, ports);
            Assert.AreEqual(SuccessionOutcome.Failed, flow.RequestDeath(a.Id));
            Assert.AreSame(a, flow.Current); Assert.AreEqual(SurvivorLifeStatus.Active, a.Status);
            Assert.AreEqual(0, ports.Activations); Assert.AreEqual(1, ports.Cancellations);
            Assert.IsFalse(ports.Gated); Assert.IsNull(flow.Receipt);
        }

        [TestCase(false)] [TestCase(true)]
        public void ActivationFailureRetriesTheSameSuccessorAndReceiptWithoutAnotherCommit(bool throws)
        {
            var ports = new Ports { FailActivation = true, ThrowActivation = throws };
            var a = Active(); int choices = 0;
            var flow = new SurvivorSuccessionCoordinator(a, ports, _ => { choices++; return new SurvivorState(); });
            Assert.AreEqual(SuccessionOutcome.Failed, flow.RequestDeath(a.Id));
            var pending = flow.Pending; var receipt = flow.Receipt;
            Assert.IsNull(flow.Current); Assert.AreEqual(SurvivorLifeStatus.Dead, a.Status);
            Assert.AreEqual(SurvivorLifeStatus.Pending, pending.Successor.Status);
            Assert.AreEqual(SuccessionStage.EquipmentCommitted, flow.Stage);
            Assert.IsTrue(ports.Gated); Assert.AreEqual(0, ports.Acknowledgments);
            Assert.AreEqual(SuccessionOutcome.StaleSurvivor, flow.RequestDeath(pending.Successor.Id));
            Assert.AreSame(receipt, flow.Receipt);
            ports.FailActivation = false; ports.ThrowActivation = false;
            Assert.AreEqual(SuccessionOutcome.Completed, flow.Retry());
            Assert.AreSame(pending.Successor, flow.Current); Assert.AreEqual(1, choices);
            Assert.AreEqual(1, ports.Commits); Assert.AreEqual(1, ports.Activations);
            Assert.AreEqual(1, ports.Acknowledgments); Assert.IsFalse(ports.Gated);
            Assert.AreEqual(SuccessionOutcome.StaleSurvivor, flow.Retry());
        }

        [Test] public void ReentrantRequestsCannotRunDuringEquipmentCommit()
        {
            var ports = new Ports(); var a = Active();
            var flow = new SurvivorSuccessionCoordinator(a, ports);
            ports.OnCommit = () =>
            {
                Assert.AreEqual(SuccessionOutcome.Busy, flow.RequestDeath(a.Id));
                Assert.AreEqual(SuccessionOutcome.Busy, flow.Retry());
            };
            Assert.AreEqual(SuccessionOutcome.Completed, flow.RequestDeath(a.Id));
            Assert.AreEqual(1, ports.Commits);
        }

        [Test] public void RetryAfterUncertainAcknowledgmentDoesNotReactivateOrDeliverTwice()
        {
            var ports = new Ports { ThrowAfterAcknowledgment = true }; var a = Active();
            var flow = new SurvivorSuccessionCoordinator(a, ports);
            Assert.AreEqual(SuccessionOutcome.Failed, flow.RequestDeath(a.Id));
            var b = flow.Current;
            Assert.AreEqual(SuccessionStage.AvatarActivated, flow.Stage); Assert.IsTrue(ports.Gated);
            ports.ThrowAfterAcknowledgment = false;
            Assert.AreEqual(SuccessionOutcome.Completed, flow.Retry());
            Assert.AreSame(b, flow.Current); Assert.AreEqual(1, ports.Activations);
            Assert.AreEqual(1, ports.Acknowledgments); Assert.AreEqual(1, ports.Commits);
            Assert.AreEqual(SuccessionOutcome.StaleSurvivor, flow.RequestDeath(a.Id));
        }

        [Test] public void InvalidCommittedReceiptRemainsGatedAndNeverRetransfers()
        {
            var ports = new Ports { InvalidReceipt = true }; var a = Active();
            var flow = new SurvivorSuccessionCoordinator(a, ports);
            Assert.AreEqual(SuccessionOutcome.Failed, flow.RequestDeath(a.Id));
            Assert.AreEqual(SuccessionOutcome.Failed, flow.Retry());
            Assert.AreEqual(1, ports.Commits); Assert.IsTrue(ports.Gated);
            Assert.IsNull(flow.Current); Assert.AreEqual(SurvivorLifeStatus.Dead, a.Status);
            Assert.AreEqual(0, ports.Cancellations); Assert.AreEqual(0, ports.Activations);
        }

        [Test] public void RejectedAcknowledgmentCanRetryWithoutRepeatingActivation()
        {
            var ports = new Ports { FailAcknowledgment = true }; var a = Active();
            var flow = new SurvivorSuccessionCoordinator(a, ports);
            Assert.AreEqual(SuccessionOutcome.Failed, flow.RequestDeath(a.Id));
            Assert.IsTrue(ports.Gated); Assert.AreEqual(0, ports.Acknowledgments);
            ports.FailAcknowledgment = false;
            Assert.AreEqual(SuccessionOutcome.Completed, flow.RequestDeath(a.Id));
            Assert.AreEqual(1, ports.Commits); Assert.AreEqual(1, ports.Activations);
            Assert.AreEqual(1, ports.Acknowledgments); Assert.IsFalse(ports.Gated);
        }

        [Test] public void SelectionCannotReuseAnEarlierRetiredIdentity()
        {
            var ports = new Ports(); var a = Active(); var b = new SurvivorState();
            var flow = new SurvivorSuccessionCoordinator(a, ports,
                previous => previous.Id == a.Id ? b : new SurvivorState(a.Id));
            Assert.AreEqual(SuccessionOutcome.Completed, flow.RequestDeath(a.Id));
            Assert.AreEqual(SuccessionOutcome.Failed, flow.RequestDeath(b.Id));
            Assert.AreSame(b, flow.Current); Assert.AreEqual(SurvivorLifeStatus.Active, b.Status);
            Assert.AreEqual(SurvivorLifeStatus.Dead, a.Status); Assert.AreEqual(1, ports.Commits);
            Assert.IsFalse(ports.Gated);
        }

        [Test] public void SuccessorSelectionCannotReuseActiveOrRetiredIdentities()
        {
            var ports = new Ports(); var a = Active();
            var flow = new SurvivorSuccessionCoordinator(a, ports, _ => new SurvivorState(a.Id));
            Assert.AreEqual(SuccessionOutcome.Failed, flow.RequestDeath(a.Id));
            Assert.AreSame(a, flow.Current); Assert.AreEqual(0, ports.Commits); Assert.IsFalse(ports.Gated);
            var retired = Active(); retired.TryRetire();
            flow = new SurvivorSuccessionCoordinator(a, ports, _ => retired);
            Assert.AreEqual(SuccessionOutcome.Failed, flow.RequestDeath(a.Id));
            Assert.AreSame(a, flow.Current); Assert.AreEqual(0, ports.Commits);
        }

        [Test] public void InitialSurvivorMustBeActiveAndApplicationHasNoUnityReferences()
        {
            Assert.Throws<ArgumentException>(() => new SurvivorSuccessionCoordinator(new SurvivorState(), new Ports()));
            Assert.Throws<ArgumentNullException>(() => new SurvivorSuccessionCoordinator(Active(), null));
            foreach (var assembly in typeof(SurvivorSuccessionCoordinator).Assembly.GetReferencedAssemblies())
                Assert.IsFalse(assembly.Name.StartsWith("Unity"));
        }
    }
}
