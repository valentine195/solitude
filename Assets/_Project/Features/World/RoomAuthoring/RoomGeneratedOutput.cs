using System;
using UnityEngine;

namespace SOLITUDE.World.RoomAuthoring
{
    [DisallowMultipleComponent]
    public sealed class RoomGeneratedOutput : MonoBehaviour
    {
        [SerializeField] private ShipRoomAuthoring owner;
        [SerializeField] private ShipLayoutAuthoring layoutOwner;
        [SerializeField, TextArea] private string adoptionRecord;
        // Scene-local ownership proof, recorded by the editor writer in the same Undo transaction.
        // Missing references are disposable deletions; surviving references must still match.
        [Serializable]
        public sealed class GeneratedChild
        {
            public Transform Transform;
            public string Fingerprint;
        }
        [SerializeField, HideInInspector] private string rootFingerprint;
        [SerializeField, HideInInspector] private GeneratedChild[] generatedChildren = Array.Empty<GeneratedChild>();
        [SerializeField, HideInInspector] private RoomFloorPanelOutput preparedPanels;
        public string RootFingerprint => rootFingerprint;
        public System.Collections.Generic.IReadOnlyList<GeneratedChild> GeneratedChildren => generatedChildren;
        public RoomFloorPanelOutput PreparedPanels => preparedPanels;

        public void RecordGeneratedChildren(string root, GeneratedChild[] children, RoomFloorPanelOutput panels)
        {
            rootFingerprint = root;
            generatedChildren = children;
            preparedPanels = panels;
        }

        public ShipRoomAuthoring Owner => owner;
        public ShipLayoutAuthoring LayoutOwner => layoutOwner;
        public string AdoptionRecord => adoptionRecord;

        public void RecordAdoption(string previousHierarchy)
        {
            if (owner == null || layoutOwner == null)
                throw new InvalidOperationException("Record adoption after assigning room and layout ownership.");
            adoptionRecord = "Room " + owner.RoomId + " at " + previousHierarchy +
                ": independent output → layout " + layoutOwner.LayoutId + ". Existing room, occupancy, style and output references retained.";
        }

        public void Initialize(ShipRoomAuthoring room) => Initialize(room, null);

        public void Initialize(ShipRoomAuthoring room, ShipLayoutAuthoring layout)
        {
            if (room == null) throw new ArgumentNullException(nameof(room));
            if (owner != null && owner != room) throw new InvalidOperationException("Generated output already belongs to another room.");
            if (layoutOwner != null && layoutOwner != layout)
                throw new InvalidOperationException("Generated output already belongs to another layout.");
            if (layout != null && room.Layout != layout)
                throw new InvalidOperationException("Output room must belong to its layout.");
            owner = room;
            layoutOwner = layout;
        }
    }
}
