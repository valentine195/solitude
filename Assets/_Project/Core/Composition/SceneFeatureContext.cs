using SOLITUDE.Application;
using SOLITUDE.Core.Events;
using SOLITUDE.Containers;
using SOLITUDE.Core.Input;
using SOLITUDE.Items;
using SOLITUDE.Player;
using SOLITUDE.Tasks;

namespace SOLITUDE.Composition
{
    public sealed class SceneFeatureContext
    {
        public ContainerCommandService Commands { get; }
        public IContainerReader Inventory { get; }
        public ItemPresentationCatalog Presentations { get; }
        public InputPolicyCoordinator Policy { get; }
        public UnityInputHost Input { get; }
        public TaskJournal Tasks { get; }
        public string TaskScope { get; }
        public PlayerInteractor Interactor { get; }
        public SceneInteractionEvents InteractionEvents { get; }

        public SceneFeatureContext(ContainerCommandService commands, IContainerReader inventory,
            ItemPresentationCatalog presentations, InputPolicyCoordinator policy, UnityInputHost input,
            TaskJournal tasks, string taskScope, PlayerInteractor interactor, SceneInteractionEvents interactionEvents)
        {
            Commands = commands; Inventory = inventory; Presentations = presentations;
            Policy = policy; Input = input; Tasks = tasks; TaskScope = taskScope; Interactor = interactor; InteractionEvents = interactionEvents;
        }
    }
}
