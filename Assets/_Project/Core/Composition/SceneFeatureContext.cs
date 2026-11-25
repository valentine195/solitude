using SOLITUDE.Application;
using SOLITUDE.Core.Events;
using SOLITUDE.Containers;
using SOLITUDE.Core.Input;
using SOLITUDE.Core.UI;
using System;
using SOLITUDE.Items;
using SOLITUDE.Player;
using SOLITUDE.Tasks;
using SOLITUDE.SaveLoad;

namespace SOLITUDE.Composition
{
    public sealed class SceneFeatureContext
    {
        public ContainerCommandService Commands { get; }
        public IContainerReader Inventory { get; }
        public CarriedEquipment Equipment { get; }
        public IContainerReader Hotbar { get; }
        public HotbarSelectionState Selection { get; }
        public Action ClosePlayerScreens { get; }
        public InteractionFeedbackService Feedback { get; }
        public ItemPresentationCatalog Presentations { get; }
        public ItemCapabilityCatalog Capabilities { get; }
        public InputPolicyCoordinator Policy { get; }
        public UnityInputHost Input { get; }
        public TaskJournal Tasks { get; }
        public string TaskScope { get; }
        public PlayerInteractor Interactor { get; }
        public SceneInteractionEvents InteractionEvents { get; }
        public Func<IContainerReader, string, bool> OpenContainer { get; }
        public Action<ContainerHandle> CloseContainer { get; }
        // Null for disposable/demo bindings; production lifecycle belongs to this session.
        public ContainerSaveSession LifecycleSession { get; }
        public bool IsSceneArrival { get; }

        public SceneFeatureContext(ContainerCommandService commands, IContainerReader inventory,
            ItemPresentationCatalog presentations, InputPolicyCoordinator policy, UnityInputHost input,
            TaskJournal tasks, string taskScope, PlayerInteractor interactor, SceneInteractionEvents interactionEvents,
            IContainerReader hotbar = null, HotbarSelectionState selection = null,
            Action closePlayerScreens = null, InteractionFeedbackService feedback = null,
            Func<IContainerReader, string, bool> openContainer = null, Action<ContainerHandle> closeContainer = null,
            ContainerSaveSession lifecycleSession = null, bool isSceneArrival = false, ItemCapabilityCatalog capabilities = null, CarriedEquipment equipment = null)
        {
            Capabilities = capabilities; Equipment = equipment;
            LifecycleSession = lifecycleSession; IsSceneArrival = isSceneArrival;
            OpenContainer = openContainer; CloseContainer = closeContainer;
            Hotbar = hotbar; Selection = selection; ClosePlayerScreens = closePlayerScreens; Feedback = feedback;
            Commands = commands; Inventory = inventory; Presentations = presentations;
            Policy = policy; Input = input; Tasks = tasks; TaskScope = taskScope; Interactor = interactor; InteractionEvents = interactionEvents;
        }
    }
}
