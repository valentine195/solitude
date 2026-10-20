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
        [SerializeField] private RoomStyleDefinition style;
        [SerializeField] private Tilemap occupancy;
        [SerializeField] private RoomGeneratedOutput generatedOutput;
        [SerializeField, TextArea] private string generationDiagnostic;
        [SerializeField] private RoomGenerationResult generationResult;

        public string RoomId => roomId;
        public RoomStyleDefinition Style { get => style; set => style = value; }
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
