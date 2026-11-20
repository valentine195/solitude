using System;
using SOLITUDE.Containers;
using SOLITUDE.Items;
using UnityEngine;

namespace SOLITUDE.World.CrewBackgrounds
{
    [DisallowMultipleComponent]
    public sealed class CrewBackgroundsServicePanel : MonoBehaviour
    {
        [SerializeField] private string ownerId = "crew-backgrounds.service-panel";
        [SerializeField] private ItemDefinition jumper;
        private ContainerCommandService commands;

        public IContainerReader Installed { get; private set; }

        internal void ValidateAuthoring()
        {
            if (string.IsNullOrWhiteSpace(ownerId) || jumper == null || string.IsNullOrWhiteSpace(jumper.ItemId)
                || jumper.Stackable || jumper.MaxStackSize != 1)
                throw new InvalidOperationException("The service panel requires its own container ID and a single reusable jumper.");
        }

        internal void Bind(ContainerCommandService service)
        {
            Release();
            ValidateAuthoring();
            commands = service;
            Installed = commands.Register(ownerId, 1, filter: item => item.ItemId == jumper.ItemId);
        }

        internal void Release()
        {
            if (Installed != null) commands.Unregister(Installed.Handle);
            Installed = null;
            commands = null;
        }

        internal CrewBackgroundsEncounterState NewEncounter(CrewBackgroundsFault fault) =>
            new CrewBackgroundsEncounterState(fault, Installed, jumper.ItemId);
    }
}
