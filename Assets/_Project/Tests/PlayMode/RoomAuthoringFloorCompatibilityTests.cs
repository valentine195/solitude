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
        [UnityTest] public IEnumerator ProductionSafePointAndLandingExcludeMaskAndArchitectureAndRetainSolidBlockers() => Verify(false);
        [UnityTest] public IEnumerator LayoutMembersExcludeMaskAndArchitectureFromRuntimeFloorQueries() => Verify(true);

        private static IEnumerator Verify(bool layoutMember)
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
            floorMap.SetTile(new Vector3Int(6, 4, 0), null);
            room.Occupancy.SetTile(new Vector3Int(6, 4, 0), null);
            room.Occupancy.SetTile(new Vector3Int(9, 0, 0), tile);
            var issue = new RoomGenerationIssue(RoomIssueKind.Span, "North", "Unresolved short hole span", true, new Vector2Int(6, 4), new Vector2Int(7, 4));
            issue.LocateInRoom(room.RoomId);
            room.SetGenerationResult(new RoomGenerationResult { Status = RoomGenerationStatus.IncompletePreview, Issues = new[] { issue } });
            var missingRoot = new GameObject("Missing Floor Preview", typeof(Grid), typeof(ShipRoomAuthoring));
            SceneManager.MoveGameObjectToScene(missingRoot, scene);
            var missingMask = new GameObject("Authored Occupancy", typeof(Tilemap)); missingMask.transform.SetParent(missingRoot.transform, false);
            missingMask.tag = "EditorOnly"; missingMask.SetActive(false);
            missingMask.GetComponent<Tilemap>().SetTile(new Vector3Int(12, 0, 0), tile);
            var missingOutput = new GameObject("Generated Architecture", typeof(RoomGeneratedOutput)); missingOutput.transform.SetParent(missingRoot.transform, false);
            var missingStyle = ScriptableObject.CreateInstance<RoomStyleDefinition>();
            var missingRoom = missingRoot.GetComponent<ShipRoomAuthoring>();
            missingRoom.Initialize(Guid.NewGuid().ToString("N"), missingStyle, missingMask.GetComponent<Tilemap>(), missingOutput.GetComponent<RoomGeneratedOutput>());
            missingOutput.GetComponent<RoomGeneratedOutput>().Initialize(missingRoom);
            missingRoom.SetGenerationResult(new RoomGenerationResult { Status = RoomGenerationStatus.IncompletePreview,
                Issues = new[] { new RoomGenerationIssue(RoomIssueKind.Floor, "floorTile", "Missing floor") } });
            ShipLayoutAuthoring Attach(ShipRoomAuthoring space)
            {
                var layoutRoot = new GameObject("Ship Layout", typeof(ShipLayoutAuthoring));
                SceneManager.MoveGameObjectToScene(layoutRoot, scene);
                space.transform.SetParent(layoutRoot.transform, false);
                var layout = layoutRoot.GetComponent<ShipLayoutAuthoring>();
                space.InitializeLayout(layout, LogicalSpaceType.Room);
                space.GeneratedOutput.Initialize(space, layout);
                layout.Initialize(Guid.NewGuid().ToString("N"), space.Style, space, space.GeneratedOutput);
                return layout;
            }
            var layout = layoutMember ? Attach(room) : null;
            var missingLayout = layoutMember ? Attach(missingRoom) : null;
            var architecture = new GameObject("North", typeof(SpriteRenderer)); architecture.transform.SetParent(output.transform, false);
            architecture.transform.localPosition = new Vector3(8.5f, 6.5f, 0); architecture.GetComponent<SpriteRenderer>().sprite = sprite;
            var panels = new GameObject("Prepared transparent panel seams", typeof(SpriteRenderer));
            panels.transform.SetParent(output.transform, false); panels.transform.localPosition = new Vector3(9.5f, .5f, 0);
            panels.GetComponent<SpriteRenderer>().sprite = sprite; panels.GetComponent<SpriteRenderer>().sortingOrder = 1;
            var floorDetail = new GameObject("Prepared detail over walkable floor", typeof(SpriteRenderer));
            floorDetail.transform.SetParent(output.transform, false); floorDetail.transform.localPosition = new Vector3(2.5f, 2.5f, 0);
            floorDetail.GetComponent<SpriteRenderer>().sprite = sprite;
            floorDetail.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, .5f);
            floorDetail.GetComponent<SpriteRenderer>().sortingOrder = 1;
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
                point.transform.position = new Vector3(6.5f, 4.5f, 0);
                Assert.Throws<InvalidOperationException>(() => adapter.ValidateResumePoint(point));
                Assert.IsFalse(CanLand(point.transform.position), "An erased hole is not floor.");
                point.transform.position = new Vector3(12.5f, .5f, 0);
                Assert.Throws<InvalidOperationException>(() => adapter.ValidateResumePoint(point));
                Assert.IsFalse(CanLand(point.transform.position), "Missing-floor editor-guide positions cannot become runtime floor.");
                Assert.IsFalse(missingMask.activeInHierarchy);
                Assert.IsEmpty(missingOutput.GetComponentsInChildren<TilemapRenderer>());
                Assert.AreEqual(RoomGenerationStatus.IncompletePreview, missingRoom.GenerationResult.Status);
                Assert.AreEqual(RoomGenerationStatus.IncompletePreview, room.GenerationResult.Status);
                if (layoutMember)
                {
                    Assert.AreSame(room, layout.Spaces[0]);
                    Assert.AreSame(layout, output.LayoutOwner);
                    Assert.AreSame(missingLayout, missingRoom.GeneratedOutput.LayoutOwner);
                }
                point.transform.position = new Vector3(9.5f, .5f, 0);
                Assert.Throws<InvalidOperationException>(() => adapter.ValidateResumePoint(point));
                Assert.IsFalse(CanLand(point.transform.position), "Inactive mask-only cells cannot count as floor.");
                Assert.IsEmpty(panels.GetComponents<Collider2D>());
                Assert.IsFalse(CanLand(panels.transform.position), "Prepared seam sprites cannot supply runtime floor candidates.");
                Assert.IsEmpty(floorDetail.GetComponents<Collider2D>());
                Assert.IsTrue(CanLand(floorDetail.transform.position), "Prepared detailing leaves its base floor walkable.");
                floorDetail.SetActive(false);
                Assert.IsTrue(CanLand(floorDetail.transform.position), "Hiding detailing cannot change floor-query results.");
                floorDetail.SetActive(true);
                point.transform.position = architecture.transform.position;
                Assert.Throws<InvalidOperationException>(() => adapter.ValidateResumePoint(point));
                Assert.IsFalse(CanLand(point.transform.position), "Sprite architecture cannot count as floor.");
                var blocker = new GameObject("Solid", typeof(BoxCollider2D)); SceneManager.MoveGameObjectToScene(blocker, scene);
                blocker.transform.position = new Vector3(2.5f, 2.5f, 0);
                Physics2D.SyncTransforms();
                Assert.IsFalse(CanLand(blocker.transform.position), "Same-scene solid geometry blocks otherwise valid floor.");
                floorDetail.SetActive(false);
                Assert.IsFalse(CanLand(blocker.transform.position), "Hiding detailing cannot remove a solid blocker.");
                floorDetail.SetActive(true);
                blocker.GetComponent<BoxCollider2D>().isTrigger = true;
                Assert.IsTrue(CanLand(blocker.transform.position), "Trigger geometry retains existing landing semantics.");
                Assert.AreEqual(48, room.Occupancy.GetTilesBlock(new BoundsInt(0, 0, 0, 10, 6, 1)).CountNonNull());
            }
            finally
            {
                Object.Destroy(root); Object.Destroy(missingRoot); Object.Destroy(missingStyle); Object.Destroy(player); Object.Destroy(point.gameObject);
                Object.Destroy(style); Object.Destroy(tile); Object.Destroy(sprite); Object.Destroy(texture);
                if (layout != null) Object.Destroy(layout.gameObject);
                if (missingLayout != null) Object.Destroy(missingLayout.gameObject);
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
