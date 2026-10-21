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
        [SerializeField] private RoomGeneratedOutput generatedOutput;
        [SerializeField] private RoomGenerationResult generationResult;

        public string LayoutId => layoutId;
        public RoomStyleDefinition DefaultStyle { get => defaultStyle; set => defaultStyle = value; }
        public IReadOnlyList<ShipRoomAuthoring> Spaces => spaces ?? Array.Empty<ShipRoomAuthoring>();
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

        public void SetGenerationResult(RoomGenerationResult result) => generationResult = result;
    }
}
