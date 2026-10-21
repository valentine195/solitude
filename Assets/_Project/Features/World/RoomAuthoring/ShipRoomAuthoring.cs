using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SOLITUDE.World.RoomAuthoring
{
    [DefaultExecutionOrder(-32000)]
    [DisallowMultipleComponent]
    public sealed class ShipRoomAuthoring : MonoBehaviour
    {
        [SerializeField] private string roomId;
        [SerializeField] private ShipLayoutAuthoring layout;
        [SerializeField] private LogicalSpaceType spaceType;
        [SerializeField, TextArea] private string authoringMetadata;
        [SerializeField] private RoomStyleDefinition style;
        [SerializeField] private Tilemap occupancy;
        [SerializeField] private RoomGeneratedOutput generatedOutput;
        [SerializeField, TextArea] private string generationDiagnostic;
        [SerializeField] private RoomGenerationResult generationResult;

        public string RoomId => roomId;
        public ShipLayoutAuthoring Layout => layout;
        public LogicalSpaceType SpaceType { get => spaceType; set => spaceType = value; }
        public string AuthoringMetadata { get => authoringMetadata; set => authoringMetadata = value; }
        public RoomStyleDefinition Style { get => style != null ? style : layout == null ? null : layout.DefaultStyle; set => style = value; }

        public void InitializeLayout(ShipLayoutAuthoring owner, LogicalSpaceType type)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (layout != null) throw new InvalidOperationException("Space already belongs to a layout.");
            layout = owner;
            spaceType = type;
        }
        public Tilemap Occupancy => occupancy;
        public RoomGeneratedOutput GeneratedOutput => generatedOutput;
        public string GenerationDiagnostic => generationDiagnostic;
        public RoomGenerationResult GenerationResult => generationResult;

        public void SetGenerationResult(RoomGenerationResult result)
        { generationResult = result; SetGenerationDiagnostic(result?.Summary); }

        public void Initialize(string identity, RoomStyleDefinition roomStyle, Tilemap authoredCells, RoomGeneratedOutput output)
        {
            if (!string.IsNullOrEmpty(roomId)) throw new InvalidOperationException("Room identity is already initialized.");
            if (string.IsNullOrWhiteSpace(identity) || authoredCells == null || output == null)
                throw new ArgumentException("Room identity, occupancy, and generated output are required.");
            roomId = identity;
            style = roomStyle;
            occupancy = authoredCells;
            generatedOutput = output;
        }

        private void Awake()
        {
            if (UnityEngine.Application.isPlaying && occupancy != null && occupancy.transform.parent == transform)
                occupancy.gameObject.SetActive(false);
        }

        public void SetGenerationDiagnostic(string message) => generationDiagnostic = message ?? string.Empty;
    }
}
