using System;
using System.Collections.Generic;
using UnityEngine;

namespace SOLITUDE.World.RoomAuthoring
{
    public enum LogicalSpaceType { Room, Corridor }

    [DisallowMultipleComponent]
    public sealed class ShipLayoutAuthoring : MonoBehaviour
    {
        [SerializeField] private string layoutId;
        [SerializeField] private RoomStyleDefinition defaultStyle;
        [SerializeField] private ShipRoomAuthoring[] spaces = Array.Empty<ShipRoomAuthoring>();
        [SerializeField] private LayoutConnectionDefinition[] connections = Array.Empty<LayoutConnectionDefinition>();
        [SerializeField] private RoomGeneratedOutput generatedOutput;
        [SerializeField] private RoomGenerationResult generationResult;

        public string LayoutId => layoutId;
        public RoomStyleDefinition DefaultStyle { get => defaultStyle; set => defaultStyle = value; }
        public IReadOnlyList<ShipRoomAuthoring> Spaces => spaces ?? Array.Empty<ShipRoomAuthoring>();
        public IReadOnlyList<LayoutConnectionDefinition> Connections => connections ?? Array.Empty<LayoutConnectionDefinition>();
        public RoomGeneratedOutput GeneratedOutput => generatedOutput;
        public RoomGenerationResult GenerationResult => generationResult;

        public void Initialize(string identity, RoomStyleDefinition style, ShipRoomAuthoring space, RoomGeneratedOutput output)
        {
            if (!string.IsNullOrEmpty(layoutId)) throw new InvalidOperationException("Layout identity is already initialized.");
            if (string.IsNullOrWhiteSpace(identity) || space == null || output == null)
                throw new ArgumentException("Layout identity, space and output are required.");
            layoutId = identity;
            defaultStyle = style;
            spaces = new[] { space };
            generatedOutput = output;
        }

        // Editor operations validate the complete proposed set before replacing authored records.
        public void SetConnections(IEnumerable<LayoutConnectionDefinition> definitions) =>
            connections = new List<LayoutConnectionDefinition>(definitions).ToArray();

        public void SetGenerationResult(RoomGenerationResult result) => generationResult = result;

        public void AddSpace(ShipRoomAuthoring space)
        {
            if (space == null || space.Layout != this || space.gameObject.scene != gameObject.scene ||
                !space.transform.IsChildOf(transform) || space.GeneratedOutput == null ||
                space.GeneratedOutput.LayoutOwner != this || space.GeneratedOutput.Owner != space)
                throw new ArgumentException("Space must have consistent layout, hierarchy and output ownership.");
            foreach (var member in Spaces)
                if (member == space || member == null || member.RoomId == space.RoomId ||
                    member.Occupancy == space.Occupancy || member.GeneratedOutput == space.GeneratedOutput)
                    throw new InvalidOperationException("Space membership or identity is ambiguous.");
            var members = new List<ShipRoomAuthoring>(Spaces) { space };
            spaces = members.ToArray();
        }
    }
}
