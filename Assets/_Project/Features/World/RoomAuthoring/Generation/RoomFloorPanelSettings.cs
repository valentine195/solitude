using System;
using UnityEngine;

namespace SOLITUDE.World.RoomAuthoring
{
    [Serializable]
    public sealed class RoomFloorPanelSettings
    {
        public const int CurrentVersion = 1;
        public const int PixelsPerCell = 32;
        public const int Quantum = 8;
        public bool Enabled;
        public int Seed;
        public int Version = CurrentVersion;
        public Vector2Int PreferredMinPixels = new Vector2Int(32, 32);
        public Vector2Int PreferredMaxPixels = new Vector2Int(128, 128);
        public float Density = .5f;
        public int SeamThickness = 1;
        public Color[] SeamPalette = { new Color(.12f, .16f, .18f, 1) };

        public RoomFloorPanelSettings Copy()
        {
            var result = (RoomFloorPanelSettings)MemberwiseClone();
            result.SeamPalette = SeamPalette == null ? null : (Color[])SeamPalette.Clone();
            return result;
        }

        public string ValidationIssue()
        {
            if (Version != CurrentVersion) return "Unsupported panel configuration version; expected " + CurrentVersion + ".";
            if (PreferredMinPixels.x <= 0 || PreferredMinPixels.y <= 0 || PreferredMaxPixels.x < PreferredMinPixels.x ||
                PreferredMaxPixels.y < PreferredMinPixels.y || PreferredMinPixels.x % Quantum != 0 ||
                PreferredMinPixels.y % Quantum != 0 || PreferredMaxPixels.x % Quantum != 0 || PreferredMaxPixels.y % Quantum != 0)
                return "Preferred width/height ranges must be positive multiples of eight pixels, with minimum no greater than maximum.";
            if (!Finite(Density) || Density < 0 || Density > 1) return "Panel density must be finite and between zero and one.";
            if (SeamThickness < 1 || SeamThickness > 3) return "Seam thickness must be one, two or three native pixels.";
            if (SeamPalette == null || SeamPalette.Length == 0) return "Configure at least one seam palette color.";
            foreach (var color in SeamPalette)
                if (!Channel(color.r) || !Channel(color.g) || !Channel(color.b) || !Channel(color.a) || color.a == 0)
                    return "Seam palette colors must have finite zero-to-one channels and positive alpha.";
            return null;
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool Channel(float value) => Finite(value) && value >= 0 && value <= 1;
    }
}
