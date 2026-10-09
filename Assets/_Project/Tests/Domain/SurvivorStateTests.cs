using System;
using NUnit.Framework;
using SOLITUDE.Survivors;

namespace SOLITUDE.Tests
{
    public class SurvivorStateTests
    {
        [Test] public void IdentityIsDistinctAndCannotBeEmpty()
        {
            var first = new SurvivorState();
            Assert.AreNotEqual(Guid.Empty, first.Id);
            Assert.AreNotEqual(first.Id, new SurvivorState().Id);
            Assert.Throws<ArgumentException>(() => new SurvivorState(Guid.Empty));
        }

        [Test] public void RetirementIsPermanentAndOnlyActiveSurvivorsCanRetire()
        {
            var survivor = new SurvivorState();
            Assert.AreEqual(SurvivorLifeStatus.Pending, survivor.Status);
            Assert.IsFalse(survivor.TryRetire());
            Assert.IsTrue(survivor.TryActivate());
            Assert.IsFalse(survivor.TryActivate());
            Assert.IsTrue(survivor.TryRetire());
            Assert.AreEqual(SurvivorLifeStatus.Dead, survivor.Status);
            Assert.IsFalse(survivor.TryActivate());
            Assert.IsFalse(survivor.TryRetire());
        }

        [Test] public void DomainHasNoUnityReferences()
        {
            foreach (var assembly in typeof(SurvivorState).Assembly.GetReferencedAssemblies())
                Assert.IsFalse(assembly.Name.StartsWith("Unity"));
        }
    }
}
