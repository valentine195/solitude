using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SOLITUDE.World.RoomAuthoring
{
    [CreateAssetMenu(menuName = "SOLITUDE/Room Style", fileName = "RoomStyle")]
    public sealed class RoomStyleDefinition : ScriptableObject
    {
        public const int PixelsPerUnit = 32;

        [Serializable]
        public sealed class Module
        {
            public Sprite Sprite;
            public Vector2Int CanvasPixels = new Vector2Int(32, 32);
            [Min(1)] public int RepeatPixels = 32;
            [Tooltip("Structural anchor measured from the imported sprite rectangle's bottom-left, in pixels.")]
            public Vector2 AnchorPixels;
            public Vector2 OffsetPixels;
        }

        [SerializeField] private string styleId = Guid.NewGuid().ToString("N");
        [SerializeField] private string displayName = "Room Style";
        [SerializeField] private TileBase floorTile;
        [SerializeField] private Module[] northWalls = Array.Empty<Module>();
        [SerializeField] private Module westCap = new Module();
        [SerializeField] private Module eastCap = new Module();
        [SerializeField] private Module southCap = new Module();
        [SerializeField] private Module northwest = new Module();
        [SerializeField] private Module northeast = new Module();
        [SerializeField] private Module southwest = new Module();
        [SerializeField] private Module southeast = new Module();
        [Tooltip("Start/end reservations along each boundary, in pixels. North/south run west to east; sides run south to north.")]
        [SerializeField] private Vector2Int northReservationsPixels;
        [SerializeField] private Vector2Int westReservationsPixels;
        [SerializeField] private Vector2Int eastReservationsPixels;
        [SerializeField] private Vector2Int southReservationsPixels;

        [SerializeField] private Module concaveSouthwest = new Module();
        [SerializeField] private Module concaveSoutheast = new Module();
        [SerializeField] private Module concaveNorthwest = new Module();
        [SerializeField] private Module concaveNortheast = new Module();
        [Tooltip("Start/end coverage reservations when the corresponding run endpoint is concave. Same axis order as convex reservations.")]
        [SerializeField] private Vector2Int concaveNorthReservationsPixels;
        [SerializeField] private Vector2Int concaveWestReservationsPixels;
        [SerializeField] private Vector2Int concaveEastReservationsPixels;
        [SerializeField] private Vector2Int concaveSouthReservationsPixels;
        [Tooltip("Declared empty-cell clearance north of each north-facing boundary. Zero leaves visual fit unqualified; production artwork needs explicit verification.")]
        [Min(0), SerializeField] private int northWallClearanceCells;
        [Tooltip("Whether the configured artwork supports two separate branches at a diagonal-only cell contact.")]
        [SerializeField] private bool supportsDiagonalContact = true;

        public Module ConcaveSouthwest { get => concaveSouthwest; set => concaveSouthwest = value; }
        public Module ConcaveSoutheast { get => concaveSoutheast; set => concaveSoutheast = value; }
        public Module ConcaveNorthwest { get => concaveNorthwest; set => concaveNorthwest = value; }
        public Module ConcaveNortheast { get => concaveNortheast; set => concaveNortheast = value; }
        public Vector2Int ConcaveNorthReservationsPixels { get => concaveNorthReservationsPixels; set => concaveNorthReservationsPixels = value; }
        public Vector2Int ConcaveWestReservationsPixels { get => concaveWestReservationsPixels; set => concaveWestReservationsPixels = value; }
        public Vector2Int ConcaveEastReservationsPixels { get => concaveEastReservationsPixels; set => concaveEastReservationsPixels = value; }
        public Vector2Int ConcaveSouthReservationsPixels { get => concaveSouthReservationsPixels; set => concaveSouthReservationsPixels = value; }
        public int NorthWallClearanceCells { get => northWallClearanceCells; set => northWallClearanceCells = value; }
        public bool SupportsDiagonalContact { get => supportsDiagonalContact; set => supportsDiagonalContact = value; }

        public string StyleId => styleId;
        public string DisplayName { get => displayName; set => displayName = value; }
        public TileBase FloorTile { get => floorTile; set => floorTile = value; }
        public Module[] NorthWalls { get => northWalls; set => northWalls = value; }
        public Module WestCap { get => westCap; set => westCap = value; }
        public Module EastCap { get => eastCap; set => eastCap = value; }
        public Module SouthCap { get => southCap; set => southCap = value; }
        public Module Northwest { get => northwest; set => northwest = value; }
        public Module Northeast { get => northeast; set => northeast = value; }
        public Module Southwest { get => southwest; set => southwest = value; }
        public Module Southeast { get => southeast; set => southeast = value; }
        public Vector2Int NorthReservationsPixels { get => northReservationsPixels; set => northReservationsPixels = value; }
        public Vector2Int WestReservationsPixels { get => westReservationsPixels; set => westReservationsPixels = value; }
        public Vector2Int EastReservationsPixels { get => eastReservationsPixels; set => eastReservationsPixels = value; }
        public Vector2Int SouthReservationsPixels { get => southReservationsPixels; set => southReservationsPixels = value; }
    }
}
