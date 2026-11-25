#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using SOLITUDE.Application;
using SOLITUDE.Combat;
using SOLITUDE.Containers;
using SOLITUDE.GameplayTags;
using SOLITUDE.Items;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SOLITUDE.Tests
{
    public sealed class MeleeTestRecipient : MonoBehaviour, IDamageRecipient
    {
        public GameObject Entity => this != null ? gameObject : null;
        public bool IsAvailable => this != null && isActiveAndEnabled;
        public GameplayTagSet AssignedTags;
        public GameplayTagSet Tags => AssignedTags;
        public int Calls;
        public double Total;
        public DamageContext Last;
        public Action OnApplied;
        public DamageResult ApplyDamage(in DamageContext context, double amount)
        { Calls++; Total += amount; Last = context; OnApplied?.Invoke(); return new DamageResult(DamageOutcome.Applied, amount); }
    }

    public sealed class MeleeHitTestFixture : IDisposable
    {
        public readonly Scene Scene;
        public readonly GameObject Actor;
        public readonly ItemDatabase Database;
        public readonly GameplayTagRegistry Registry;
        public readonly MeleeAttackConfiguration Configuration;
        public readonly ItemActionRunner Runner = new();
        public readonly ContainerCommandService Commands;
        public readonly Container Hotbar;
        public readonly HotbarSelectionState Selection;
        public readonly EquippedItemView Equipment;
        public readonly DamageDelivery Delivery;
        public readonly List<MeleeQueryFailure> Failures = new();
        public MeleeHitResolver Resolver;
        public MeleeAttackExecution Execution;
        public bool Valid = true, Available = true, Gameplay = true;
        public float UnitsPerTile = 1;
        public Vector2 Direction = Vector2.right, OriginOffset;
        public Action<MeleeAttackExecution> ActiveSample;
        private readonly ItemActionDefinition action;
        private readonly bool ownsConfiguration;
        public MeleeHitTestFixture(MeleeAttackConfiguration sharedConfiguration = null)
        {
            Scene = SceneManager.CreateScene("Melee geometry " + Guid.NewGuid().ToString("N"), new CreateSceneParameters(LocalPhysicsMode.Physics2D));
            Actor = New("Attacker", Vector2.zero);
            Database = Resources.Load<ItemDatabase>("Database"); var capabilities = Database.BuildCapabilityCatalog(); Registry = capabilities.Registry;
            var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Features/Items/Weapons/MaintenanceWrench/MaintenanceWrench.asset");
            var compiled = capabilities.Resolve(item.ItemId).Resolve(ItemActionSlot.Primary); action = compiled.Definition;
            ownsConfiguration = sharedConfiguration == null;
            Configuration = sharedConfiguration ?? Object.Instantiate(((MeleeAttackAction)action).Configuration);
            Commands = new ContainerCommandService(Database.BuildRuntimeCatalog()); Hotbar = Commands.Register("geometry-hotbar", 2);
            Commands.Grant(Hotbar.Handle, item.ItemId, 1); Selection = new HotbarSelectionState(Hotbar); Selection.Select(0);
            Equipment = new EquippedItemView(Commands, Hotbar, Selection); Delivery = new DamageDelivery(Registry);
            Resolver = new MeleeHitResolver(Delivery, diagnostic: failure => Failures.Add(failure));
        }
        public GameObject New(string name, Vector2 position)
        { var go = new GameObject(name); SceneManager.MoveGameObjectToScene(go, Scene); go.transform.position = position; return go; }
        public MeleeTestRecipient Box(Vector2 position, Vector2 size)
        { var go = New("Recipient", position); go.AddComponent<BoxCollider2D>().size = size; return go.AddComponent<MeleeTestRecipient>(); }
        public GameObject Cover(Vector2 position, Vector2 size)
        { var go = New("World cover", position); go.AddComponent<BoxCollider2D>().size = size; return go; }
        public bool Start()
        {
            Equipment.TryCapture(out var binding);
            var context = new ItemActionContext(Actor, binding, ItemActionSlot.Primary, Direction, UnitsPerTile,
                null, null, null, null, () => Valid && Available && Equipment.IsCurrent(binding),
                sample => ActiveSample?.Invoke(sample), Resolver, action, OriginOffset, () => Gameplay);
            return Runner.TryStart(new Request(this, context, Configuration.Compile(Database.TagRegistry, Registry)));
        }
        private sealed class Request : IItemActionRequest
        {
            private readonly MeleeHitTestFixture fixture;
            private readonly ItemActionContext context;
            private readonly MeleeAttackSpec spec;
            public Request(MeleeHitTestFixture fixture, ItemActionContext context, MeleeAttackSpec spec)
            { this.fixture = fixture; this.context = context; this.spec = spec; }
            public bool IsValid => context.IsValid;
            public double CommittedDuration => spec.TotalDuration;
            public IItemActionExecution Start() => fixture.Execution = new MeleeAttackExecution(context, spec);
        }
        public void Advance(double seconds) { Physics2D.SyncTransforms(); Runner.Advance(seconds, Available, Gameplay); }
        public void Dispose()
        {
            Runner.Dispose(); Equipment.Dispose(); Selection.Dispose(); if (ownsConfiguration) Object.DestroyImmediate(Configuration);
            foreach (var root in Scene.GetRootGameObjects()) Object.DestroyImmediate(root);
        }
    }

    public sealed class MeleeHitDetectionTests
    {
        private MeleeHitTestFixture f;
        [SetUp] public void Prepare() => f = new MeleeHitTestFixture();
        [UnityTearDown] public IEnumerator Cleanup() { var scene = f.Scene; f.Dispose(); yield return SceneManager.UnloadSceneAsync(scene); }
        private void Swing() { Assert.IsTrue(f.Start()); f.Advance(.55); }
        private void Contact(MeleeTestRecipient target)
        {
            Assert.AreEqual(1, target.Calls);
            var point = target.Last.ImpactPosition;
            Assert.That(Vector2.Distance(point, target.GetComponent<Collider2D>().ClosestPoint(point)), Is.LessThanOrEqualTo(.0001f));
            Assert.That(point.magnitude, Is.LessThanOrEqualTo(1.2501f));
            Assert.That(Vector2.Angle(Vector2.right, point), Is.LessThanOrEqualTo(45.001f));
        }
        [TestCase(1.2599f, true)] [TestCase(1.26f, true)] [TestCase(1.2602f, false)]
        public void ReachUsesColliderBoundaryInclusively(float x, bool hit)
        { var target = f.Box(new Vector2(x, 0), new Vector2(.02f, .02f)); Swing(); Assert.AreEqual(hit ? 1 : 0, target.Calls); }
        [TestCase(45f, true)] [TestCase(-45f, true)] [TestCase(45.002f, false)] [TestCase(-45.002f, false)] [TestCase(180f, false)]
        public void ArcBoundariesAndBehindActorUseActualGeometry(float degrees, bool hit)
        {
            // Place the circle's angular silhouette, rather than its centre, at the boundary.
            const float radius = .05f;
            float centre = degrees + Mathf.Sign(degrees) * Mathf.Asin(radius) * Mathf.Rad2Deg;
            var go = f.New("Boundary circle", Quaternion.Euler(0, 0, centre) * Vector2.right);
            go.AddComponent<CircleCollider2D>().radius = radius; var target = go.AddComponent<MeleeTestRecipient>();
            Swing(); Assert.AreEqual(hit ? 1 : 0, target.Calls);
        }
        [Test] public void OffsetShapeCanBeHitWithItsEntityPivotOutOfRange()
        {
            var target = f.Box(new Vector2(10, 0), new Vector2(.2f, .2f)); target.GetComponent<BoxCollider2D>().offset = new Vector2(-9.2f, 0);
            Swing(); Contact(target);
        }
        [Test] public void ShapeCenterAndClosestOriginPointMayBeOutsideSector()
        {
            var target = f.Box(new Vector2(.1f, 1), new Vector2(2, .3f));
            Physics2D.SyncTransforms(); Assert.Greater(Vector2.Angle(Vector2.right, target.GetComponent<Collider2D>().ClosestPoint(Vector2.zero)), 45);
            Swing(); Contact(target);
        }
        [Test] public void CircleCapsulePolygonRoundedAndRotatedShapesUsePhysicalContacts()
        {
            var circle = f.New("Circle", new Vector2(1.35f, 0)); circle.AddComponent<CircleCollider2D>().radius = .2f;
            var a = circle.AddComponent<MeleeTestRecipient>();
            var capsule = f.New("Capsule", new Vector2(.9f, .7f)); capsule.AddComponent<CapsuleCollider2D>().size = new Vector2(.2f, .8f);
            var b = capsule.AddComponent<MeleeTestRecipient>();
            var polygon = f.New("Polygon", Vector2.zero); polygon.AddComponent<PolygonCollider2D>().points = new[] { new Vector2(.6f, -.8f), new Vector2(1.2f, -.8f), new Vector2(.9f, -.2f) };
            var c = polygon.AddComponent<MeleeTestRecipient>();
            var rounded = f.Box(new Vector2(.6f, .3f), new Vector2(.2f, .2f)); rounded.GetComponent<BoxCollider2D>().edgeRadius = .05f;
            rounded.transform.rotation = Quaternion.Euler(0, 0, 37); rounded.transform.localScale = new Vector3(1.2f, .8f, 1);
            var edge = f.New("Edge", Vector2.zero); edge.AddComponent<EdgeCollider2D>().points = new[] { new Vector2(.7f, -.3f), new Vector2(.7f, .3f) };
            var d = edge.AddComponent<MeleeTestRecipient>();
            Swing(); Contact(a); Contact(b); Contact(c); Contact(rounded); Contact(d);
        }
        [Test] public void EnclosingShapeQualifiesEvenWhenStartInsideRaycastsAreDisabled()
        {
            bool before = Physics2D.queriesStartInColliders;
            try { Physics2D.queriesStartInColliders = false; var target = f.Box(Vector2.zero, Vector2.one * 2); Swing(); Assert.AreEqual(1, target.Calls); Assert.AreEqual(Vector2.zero, target.Last.ImpactPosition); }
            finally { Physics2D.queriesStartInColliders = before; }
        }
        [Test] public void CompoundTargetsDeduplicateAndSelfHierarchyIsExcluded()
        {
            var target = f.Box(new Vector2(.8f, 0), new Vector2(.3f, .3f));
            var child = f.New("Second target shape", new Vector2(.9f, .1f)); child.transform.SetParent(target.transform, true); child.AddComponent<CircleCollider2D>().radius = .2f;
            var self = f.Actor.AddComponent<MeleeTestRecipient>(); f.Actor.AddComponent<BoxCollider2D>().size = Vector2.one;
            var ownChild = f.New("Child self collider", new Vector2(.5f, 0)); ownChild.transform.SetParent(f.Actor.transform, true); ownChild.AddComponent<BoxCollider2D>();
            f.Start(); f.Advance(.12); f.Advance(.02); f.Advance(.02); f.Advance(.39);
            Assert.AreEqual(1, target.Calls); Assert.AreEqual(0, self.Calls);
        }
        [Test] public void SeveralRecipientsCleaveThroughEachOther()
        {
            var a = f.Box(new Vector2(.5f, 0), Vector2.one * .2f); var b = f.Box(new Vector2(.8f, 0), Vector2.one * .2f);
            var c = f.Box(new Vector2(1.1f, 0), Vector2.one * .2f); Swing();
            Assert.AreEqual(1, a.Calls); Assert.AreEqual(1, b.Calls); Assert.AreEqual(1, c.Calls);
        }
        [Test] public void SolidCoverOccludesButAnExposedPortionStillQualifies()
        {
            var target = f.Box(new Vector2(.9f, 0), new Vector2(.2f, 1.2f)); var wall = f.Cover(new Vector2(.4f, 0), new Vector2(.1f, 2));
            Swing(); Assert.AreEqual(0, target.Calls);
            wall.GetComponent<BoxCollider2D>().size = new Vector2(.1f, .25f); Swing(); Contact(target);
            Assert.Greater(Mathf.Abs(target.Last.ImpactPosition.y), .2f);
        }
        [Test] public void TriggersAndExcludedLayersAreNotTargetsOrCover()
        {
            var target = f.Box(new Vector2(.9f, 0), Vector2.one * .2f);
            var sensor = f.Box(new Vector2(.4f, 0), Vector2.one); sensor.GetComponent<Collider2D>().isTrigger = true;
            var ignored = f.Cover(new Vector2(.6f, 0), Vector2.one); ignored.layer = 2;
            Swing(); Assert.AreEqual(1, target.Calls); Assert.AreEqual(0, sensor.Calls);
        }
        [Test] public void NarrowOffsetOpeningFindsAVisibleContactWithoutAFixedAngularGrid()
        {
            var target = f.Box(new Vector2(.9f, .25f), Vector2.one * .2f);
            f.Cover(new Vector2(.4f, -.45f), new Vector2(.0001f, 1.099f));
            f.Cover(new Vector2(.4f, .65f), new Vector2(.0001f, 1.098f));
            Swing(); Contact(target);
        }
        [Test] public void OriginInsideWorldCoverCannotHitAnEnclosingRecipient()
        { var target = f.Box(Vector2.zero, Vector2.one); f.Cover(Vector2.zero, Vector2.one * .1f); Swing(); Assert.AreEqual(0, target.Calls); }
        [Test] public void RequiredAndExcludedRulesRejectWithoutAppliedHitEvents()
        {
            var definition = AssetDatabase.LoadAssetAtPath<GameplayTagDefinition>("Assets/_Project/Systems/GameplayTags/Assets/ToolMechanical.asset");
            f.Configuration.TargetEligibility = new GameplayTagQueryAuthoring(GameplayTagMatch.Hierarchical,
                new GameplayTagAuthoring(new[] { definition }), new GameplayTagAuthoring(), new GameplayTagAuthoring());
            var near = f.Box(new Vector2(.5f, 0), Vector2.one * .2f); var far = f.Box(new Vector2(.9f, 0), Vector2.one * .2f);
            far.AssignedTags = new GameplayTagAuthoring(new[] { definition }).Compile(f.Database.TagRegistry, f.Registry);
            int applied = 0; f.ActiveSample = execution => execution.HitApplied += (_, _) => applied++;
            Swing(); Assert.AreEqual(0, near.Calls); Assert.AreEqual(1, far.Calls); Assert.AreEqual(1, applied);
            f.Configuration.TargetEligibility = new GameplayTagQueryAuthoring(GameplayTagMatch.Hierarchical,
                new GameplayTagAuthoring(), new GameplayTagAuthoring(), new GameplayTagAuthoring(new[] { definition }));
            Swing(); Assert.AreEqual(1, near.Calls); Assert.AreEqual(1, far.Calls); Assert.AreEqual(2, applied);
        }
        [TestCase(MeleeQueryFailure.Candidates)] [TestCase(MeleeQueryFailure.Shapes)]
        [TestCase(MeleeQueryFailure.Directions)] [TestCase(MeleeQueryFailure.RayHits)]
        public void IncompleteCoverageRejectsWholeSampleAndDiagnosesOnce(MeleeQueryFailure failure)
        {
            f.Resolver = new MeleeHitResolver(f.Delivery, colliderCapacity: failure == MeleeQueryFailure.Candidates ? 3 : 16,
                geometryCapacity: failure == MeleeQueryFailure.Shapes ? 2 : 512,
                directionCapacity: failure == MeleeQueryFailure.Directions ? 3 : 2048,
                rayCapacity: failure == MeleeQueryFailure.RayHits ? 2 : 64, diagnostic: reason => f.Failures.Add(reason));
            var a = f.Box(new Vector2(.5f, 0), Vector2.one * .1f); var b = f.Box(new Vector2(.8f, 0), Vector2.one * .1f); var c = f.Box(new Vector2(1.1f, 0), Vector2.one * .1f);
            f.Start(); f.Advance(.12); f.Advance(.02);
            Assert.AreEqual(0, a.Calls + b.Calls + c.Calls); CollectionAssert.AreEqual(new[] { failure }, f.Failures);
        }
        [Test] public void RecipientTrackingSaturationDoesNotDeliverAPartialNewSet()
        {
            f.Resolver = new MeleeHitResolver(f.Delivery, colliderCapacity: 4, diagnostic: reason => f.Failures.Add(reason));
            var first = new[] { f.Box(new Vector2(.5f, 0), Vector2.one * .1f), f.Box(new Vector2(.8f, 0), Vector2.one * .1f), f.Box(new Vector2(1.1f, 0), Vector2.one * .1f) };
            f.Start(); f.Advance(.12); foreach (var target in first) { Assert.AreEqual(1, target.Calls); target.transform.position += Vector3.up * 10; }
            var a = f.Box(new Vector2(.5f, 0), Vector2.one * .1f); var b = f.Box(new Vector2(.8f, 0), Vector2.one * .1f);
            f.Advance(.02); Assert.AreEqual(0, a.Calls + b.Calls); CollectionAssert.AreEqual(new[] { MeleeQueryFailure.Recipients }, f.Failures);
        }
    }
}
#endif
