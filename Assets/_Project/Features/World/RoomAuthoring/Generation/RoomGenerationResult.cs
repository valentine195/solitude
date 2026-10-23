using System;
using System.Linq;
using UnityEngine;

namespace SOLITUDE.World.RoomAuthoring
{
    public enum RoomGenerationStatus { Complete, IncompletePreview, Empty, Refused }
    public enum RoomIssueKind { Style, Floor, Asset, Span, Geometry, Safety, Join, VisualFit, Connection }

    [Serializable]
    public sealed class RoomGenerationIssue
    {
        public string Id;
        public string RoomId;
        public RoomIssueKind Kind;
        public string Field;
        public bool HasRegion;
        public bool Inactive;
        public Vector2Int Start;
        public Vector2Int End;
        public string Reason;

        public RoomGenerationIssue(RoomIssueKind kind, string field, string reason,
            bool hasRegion = false, Vector2Int start = default, Vector2Int end = default)
        { Kind = kind; Field = field; Reason = reason; HasRegion = hasRegion; Start = start; End = end; }

        public void LocateInRoom(string roomId)
        {
            RoomId = roomId;
            Id = roomId + ":" + Kind + ":" + Field + (HasRegion ? ":" + Start.x + "," + Start.y + ":" + End.x + "," + End.y : "");
        }
    }

    // Informational decisions are separate from failures: a qualified fallback can be Complete.
    [Serializable]
    public sealed class RoomStyleResolution
    {
        public string OwnerId;
        public string Role;
        public Vector2Int Start;
        public Vector2Int End;
        public string StyleId;
        public bool UsedFallback;
    }

    [Serializable]
    public sealed class RoomGenerationResult
    {
        public RoomGenerationStatus Status;
        public RoomGenerationIssue[] Issues = Array.Empty<RoomGenerationIssue>();
        public RoomStyleResolution[] StyleResolutions = Array.Empty<RoomStyleResolution>();
        public string InputFingerprint;
        public string OutputFingerprint;
        public bool IsSuccessful => Status == RoomGenerationStatus.Complete || Status == RoomGenerationStatus.Empty;
        public string Summary => string.Join("\n", Issues.Select(i => "Room " + i.RoomId + ": " + i.Reason));
    }
}
