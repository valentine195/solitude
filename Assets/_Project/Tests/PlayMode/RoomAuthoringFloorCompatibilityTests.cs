using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using SOLITUDE.Movement;
using SOLITUDE.Player;
using SOLITUDE.Player.Succession;
using SOLITUDE.World.RoomAuthoring;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace SOLITUDE.Tests.RoomAuthoring
{
    public sealed class RoomAuthoringFloorCompatibilityTests
    {
        [UnityTest] public IEnumerator ProductionSafePointAndLandingExcludeMaskAndArchitectureAndRetainSolidBlockers()
        {
            var scene = SceneManager.CreateScene("RoomFloorCompatibility");
            var texture = new Texture2D(32, 32) { filterMode = FilterMode.Point };
            var sprite = Sprite.Create(texture, new Rect(0, 0, 32, 32), Vector2.one * .5f, 32);
            var tile = ScriptableObject.CreateInstance<Tile>(); tile.sprite = sprite;
            var style = ScriptableObject.CreateInstance<RoomStyleDefinition>(); style.FloorTile = tile;
            var root = new GameObject("ShipRoom", typeof(Grid), typeof(ShipRoomAuthoring));
            SceneManager.MoveGameObjectToScene(root, scene);
            var mask = new GameObject("Authored Occupancy", typeof(Tilemap)); mask.transform.SetParent(root.transform, false);
            mask.tag = "EditorOnly"; mask.SetActive(false);
            var outputObject = new GameObject("Generated Architecture", typeof(RoomGeneratedOutput)); outputObject.transform.SetParent(root.transform, false);
            var room = root.GetComponent<ShipRoomAuthoring>(); var output = outputObject.GetComponent<RoomGeneratedOutput>();
            room.Initialize(Guid.NewGuid().ToString("N"), style, mask.GetComponent<Tilemap>(), output); output.Initialize(room);
            var floor = new GameObject("Generated Floor", typeof(Tilemap), typeof(TilemapRenderer)); floor.transform.SetParent(output.transform, false);
            var floorMap = floor.GetComponent<Tilemap>();
            for (var y = 0; y < 6; y++) for (var x = 0; x < 8; x++)
            {
                var c = new Vector3Int(x, y, 0); floorMap.SetTile(c, tile); room.Occupancy.SetTile(c, tile);
            }
            room.Occupancy.SetTile(new Vector3Int(9, 0, 0), tile);
            var architecture = new GameObject("North", typeof(SpriteRenderer)); architecture.transform.SetParent(output.transform, false);
            architecture.transform.localPosition = new Vector3(8.5f, 6.5f, 0); architecture.GetComponent<SpriteRenderer>().sprite = sprite;
            var player = new GameObject("RoomFloorPlayer", typeof(PlayerMovement), typeof(PlayerInteractor), typeof(PlayerVitalsController), typeof(SurvivorAvatarAdapter));
            SceneManager.MoveGameObjectToScene(player, scene);
            player.GetComponent<MovementBehavior>().enabled = false;
            player.GetComponent<Rigidbody2D>().gravityScale = 0;
            player.transform.position = new Vector3(.5f, .5f, 0);
            var adapter = player.GetComponent<SurvivorAvatarAdapter>(); adapter.Configure(player.GetComponent<PlayerInteractor>());
            var point = new GameObject("SafePoint", typeof(SurvivorAwakeningPoint)).GetComponent<SurvivorAwakeningPoint>();
            SceneManager.MoveGameObjectToScene(point.gameObject, scene);
            var landing = typeof(SurvivorAvatarAdapter).GetMethod("CanLandAt", BindingFlags.Instance | BindingFlags.NonPublic);
            bool CanLand(Vector3 position) => (bool)landing.Invoke(adapter, new object[] { position });
            try
            {
                yield return null;
                point.transform.position = new Vector3(.5f, .5f, 0);
                Assert.DoesNotThrow(() => adapter.ValidateResumePoint(point));
                Assert.DoesNotThrow(() => adapter.Awaken(point));
                Assert.IsTrue(CanLand(point.transform.position));
                point.transform.position = new Vector3(9.5f, .5f, 0);
                Assert.Throws<InvalidOperationException>(() => adapter.ValidateResumePoint(point));
                Assert.IsFalse(CanLand(point.transform.position), "Inactive mask-only cells cannot count as floor.");
                point.transform.position = architecture.transform.position;
                Assert.Throws<InvalidOperationException>(() => adapter.ValidateResumePoint(point));
                Assert.IsFalse(CanLand(point.transform.position), "Sprite architecture cannot count as floor.");
                var blocker = new GameObject("Solid", typeof(BoxCollider2D)); SceneManager.MoveGameObjectToScene(blocker, scene);
                blocker.transform.position = new Vector3(2.5f, 2.5f, 0);
                Physics2D.SyncTransforms();
                Assert.IsFalse(CanLand(blocker.transform.position), "Same-scene solid geometry blocks otherwise valid floor.");
                blocker.GetComponent<BoxCollider2D>().isTrigger = true;
                Assert.IsTrue(CanLand(blocker.transform.position), "Trigger geometry retains existing landing semantics.");
                Assert.AreEqual(49, room.Occupancy.GetTilesBlock(new BoundsInt(0, 0, 0, 10, 6, 1)).CountNonNull());
            }
            finally
            {
                Object.Destroy(root); Object.Destroy(player); Object.Destroy(point.gameObject);
                Object.Destroy(style); Object.Destroy(tile); Object.Destroy(sprite); Object.Destroy(texture);
            }
            yield return SceneManager.UnloadSceneAsync(scene);
        }
    }

    internal static class RoomFloorTileCounts
    {
        internal static int CountNonNull(this TileBase[] tiles)
        {
            var count = 0;
            foreach (var tile in tiles) if (tile != null) count++;
            return count;
        }
    }
}
