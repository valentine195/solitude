using System;
using UnityEngine;

namespace SOLITUDE.World.RoomAuthoring
{
    // Prepared, immutable visual data only. No runtime generation or floor/collision authority.
    public sealed class RoomFloorPanelOutput : ScriptableObject
    {
        [Serializable] public sealed class Chunk
        {
            public Vector2Int Origin;
            public Texture2D Texture;
            public Sprite Sprite;
        }
        [SerializeField] private string ownerId;
        [SerializeField] private int version;
        [SerializeField] private string contentId;
        [SerializeField] private Chunk[] chunks = Array.Empty<Chunk>();
        public string OwnerId => ownerId;
        public int Version => version;
        public string ContentId => contentId;
        public System.Collections.Generic.IReadOnlyList<Chunk> Chunks => Array.AsReadOnly(chunks);
        public void Initialize(string owner, int generatorVersion, string identity, Chunk[] prepared)
        {
            if (!string.IsNullOrEmpty(contentId)) throw new InvalidOperationException("Prepared panel revisions are immutable.");
            ownerId = owner; version = generatorVersion; contentId = identity; chunks = (Chunk[])prepared.Clone();
        }
    }
}
