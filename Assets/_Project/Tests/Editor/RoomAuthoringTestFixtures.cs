using System;
using System.Collections.Generic;
using System.Linq;
using SOLITUDE.Editor.RoomAuthoring;
using SOLITUDE.World.RoomAuthoring;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace SOLITUDE.Tests.RoomAuthoring
{
    public sealed class RoomAuthoringTestFixtures : IDisposable
    {
        public readonly Scene Scene = EditorSceneManager.NewPreviewScene();
        public readonly RoomStyleDefinition Style = ScriptableObject.CreateInstance<RoomStyleDefinition>();
        private readonly List<UnityEngine.Object> assets = new List<UnityEngine.Object>();
        public RoomAuthoringTestFixtures(int wallWidth = 64)
        {
            var floor = ScriptableObject.CreateInstance<Tile>();
            assets.Add(floor);
            floor.sprite = Sprite(32, 32);
            Style.FloorTile = floor;
            Style.NorthWalls = new[] { Module(wallWidth, 64, wallWidth) };
            Style.WestCap = Module(32, 32, 32); Style.EastCap = Module(32, 32, 32); Style.SouthCap = Module(32, 32, 32);
            Style.Northwest = Module(32, 32, 32); Style.Northeast = Module(32, 32, 32);
            Style.Southwest = Module(32, 32, 32); Style.Southeast = Module(32, 32, 32);
        }
        public Sprite Sprite(int width, int height)
        {
            var texture = new Texture2D(width, height) { filterMode = FilterMode.Point };
            texture.SetPixels(Enumerable.Repeat(Color.white, width * height).ToArray()); texture.Apply();
            var sprite = UnityEngine.Sprite.Create(texture, new Rect(0, 0, width, height), Vector2.zero, 32);
            assets.Add(sprite); assets.Add(texture); return sprite;
        }
        public RoomStyleDefinition.Module Module(int width, int height, int repeat) => new RoomStyleDefinition.Module
            { Sprite = Sprite(width, height), CanvasPixels = new Vector2Int(width, height), RepeatPixels = repeat };
        public ShipRoomAuthoring Room(int width = 8, int height = 6)
        {
            var room = RoomAuthoringCreation.Create(Scene, Style);
            foreach (var c in Cells(width, height)) room.Occupancy.SetTile(new Vector3Int(c.x, c.y, 0), Style.FloorTile);
            return room;
        }
        public static int FloorCount(ShipRoomAuthoring room)
        {
            var count = 0;
            foreach (var map in room.GeneratedOutput.GetComponentsInChildren<Tilemap>())
                foreach (var cell in map.cellBounds.allPositionsWithin)
                    if (map.HasTile(cell)) count++;
            return count;
        }
        public static Vector2Int[] Cells(int width = 8, int height = 6) => Enumerable.Range(0, height)
            .SelectMany(y => Enumerable.Range(0, width).Select(x => new Vector2Int(x, y))).ToArray();
        public void Dispose()
        {
            if (Scene.IsValid() && Scene.isLoaded) EditorSceneManager.ClosePreviewScene(Scene);
            UnityEngine.Object.DestroyImmediate(Style);
            foreach (var asset in assets) UnityEngine.Object.DestroyImmediate(asset);
        }
    }
}
