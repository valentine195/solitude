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
        [SerializeField] private RoomFloorPanelSettings floorPanels = new RoomFloorPanelSettings();
        [SerializeField] private RoomFloorReservation[] floorReservations = Array.Empty<RoomFloorReservation>();

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
        public RoomFloorPanelSettings FloorPanels => floorPanels ?? new RoomFloorPanelSettings();
        public System.Collections.Generic.IReadOnlyList<RoomFloorReservation> FloorReservations =>
            Array.AsReadOnly(floorReservations ?? Array.Empty<RoomFloorReservation>());

        // Raw serialization boundary; supported editor operations validate before calling this.
        // Copy without pruning or correcting invalid external records.
        public void SetFloorPanelAuthoring(RoomFloorPanelSettings settings, System.Collections.Generic.IEnumerable<RoomFloorReservation> reservations)
        {
            if (settings == null || reservations == null) throw new ArgumentNullException();
            floorPanels = settings.Copy();
            var records = new System.Collections.Generic.List<RoomFloorReservation>();
            foreach (var record in reservations) records.Add(record?.Copy());
            floorReservations = records.ToArray();
        }

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
