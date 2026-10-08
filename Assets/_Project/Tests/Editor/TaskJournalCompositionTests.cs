using System.Reflection;
using NUnit.Framework;
using SOLITUDE.Composition;
using SOLITUDE.Containers;
using SOLITUDE.Items;
using SOLITUDE.SaveLoad;
using SOLITUDE.Tasks;
using SOLITUDE.World.ScrubberEncounter;
using UnityEngine;

namespace SOLITUDE.Tests
{
    public class TaskJournalCompositionTests
    {
        [Test] public void SceneReleaseRemovesOnlyItsScopeAndOptionalInitializerIsCompatible()
        {
            var host = new GameObject("Journal composition fixture"); var scope = host.AddComponent<SceneBindings>();
            var journal = new TaskJournal(); journal.RegisterSource("a", "first", out var first); journal.RegisterSource("b", "second", out var second);
            var snapshot = new TaskSnapshot(true, TaskOutcome.Active, "Task", "stage", nextStep: "Do it"); first.Publish("task", snapshot); second.Publish("task", snapshot);
            journal.SetPinned(new TaskKey("a", "task"));
            void Set(string field, object value) => typeof(SceneBindings).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(scope, value);
            Set("session", new ContainerSaveSession(new SaveGameData(), new ItemCatalog(new[] { new ItemSpec("item", true, 10) })));
            Set("tasks", journal); Set("taskScope", "first"); Set("taskSources", new ScrubberTaskSource[0]);
            try
            {
                scope.Release(); scope.Release(); Assert.AreEqual(1, journal.ReadSnapshot().Active.Count);
                Assert.AreEqual("b", journal.ReadSnapshot().Active[0].Key.SourceId); Assert.IsNull(journal.ReadSnapshot().PinnedKey);
                Assert.AreEqual(TaskWriteResult.Unavailable, first.Publish("task", snapshot));
                var args = typeof(SceneBindings).GetMethod("Initialize").GetParameters();
                Assert.IsTrue(args[args.Length - 1].IsOptional); Assert.AreEqual(typeof(TaskJournal), args[args.Length - 1].ParameterType);
            }
            finally { journal.Dispose(); Object.DestroyImmediate(host); }
        }
        [Test] public void IndependentSessionDoesNotCarryJournalStateThroughContainerSave()
        {
            using (var original = new TaskJournal())
            using (var replacement = new TaskJournal())
            {
                original.RegisterSource("a", "scene", out var writer); writer.Publish("task", new TaskSnapshot(true, TaskOutcome.Completed, "Task", result: "Done"));
                Assert.AreEqual(1, original.ReadSnapshot().Completed.Count); Assert.AreEqual(0, replacement.ReadSnapshot().Completed.Count);
                var save = JsonUtility.ToJson(new SaveGameData()); Assert.IsFalse(save.Contains("TaskJournal")); Assert.IsFalse(save.Contains("Completed"));
            }
        }
    }
}
