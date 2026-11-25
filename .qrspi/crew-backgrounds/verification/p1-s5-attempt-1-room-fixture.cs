using System.Linq;
using NUnit.Framework;
using SOLITUDE.World.CrewBackgrounds;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SOLITUDE.Tests
{
    public sealed class CrewBackgroundsFinalRoomAuthoringTests
    {
        [Test]
        public void PaintedFloorAndBoundariesSupportNativeEditAndUndo()
        {
            var scene = EditorSceneManager.OpenScene(CrewBackgroundsPlaytestSetup.ScenePath, OpenSceneMode.Additive);
            try
            {
                var maps = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Tilemap>()).ToArray();
                Assert.That(maps.Select(map => map.name), Is.EquivalentTo(new[] { "Floor", "Boundary Caps", "North Wall Faces" }));
                foreach (string name in new[] { "Floor", "Boundary Caps" })
                {
                    var map = maps.Single(candidate => candidate.name == name);
                    Assert.IsNotNull(map.layoutGrid);
                    Vector3Int cell = default;
                    foreach (var candidate in map.cellBounds.allPositionsWithin)
                    {
                        if (!map.HasTile(candidate)) continue;
                        cell = candidate;
                        break;
                    }
                    var original = map.GetTile(cell);
                    Assert.IsNotNull(original);
                    Undo.IncrementCurrentGroup();
                    Undo.RegisterCompleteObjectUndo(map, "Temporary Tilemap acceptance edit");
                    map.SetTile(cell, null);
                    Assert.IsFalse(map.HasTile(cell));
                    Undo.FlushUndoRecordObjects();
                    Undo.PerformUndo();
                    Assert.AreEqual(original, map.GetTile(cell));
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
