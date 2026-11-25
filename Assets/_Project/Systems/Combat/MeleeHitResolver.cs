using System;
using SOLITUDE.Items;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SOLITUDE.Combat
{
    public enum MeleeQueryFailure { None, Candidates, Shapes, Directions, RayHits, Recipients }

    // Reused by one actor. Preflight the whole sample before delivering any damage.
    public sealed class MeleeHitResolver
    {
        private const double AngleTolerance = .0005 * Math.PI / 180;
        private const float RangeTolerance = .00005f;
        private readonly Collider2D[] candidates;
        private readonly RaycastHit2D[] hits;
        private readonly IDamageRecipient[] accepted, pending;
        private readonly Vector2[] contacts;
        private readonly Curve[] curves;
        private readonly double[] angles;
        private readonly PhysicsShapeGroup2D shapes;
        private readonly DamageDelivery delivery;
        private readonly Action<MeleeQueryFailure> diagnostic;
        private MeleeAttackExecution owner;
        private Vector2 origin;
        private double baseAngle, halfArc;
        private float range;
        private int curveCount, angleCount, acceptedCount, pendingCount;
        private bool warned;
        public MeleeQueryFailure LastFailure { get; private set; }

        // Boundary primitives only choose ray directions; Physics2D supplies actual contacts.
        private readonly struct Curve
        {
            public readonly Vector2 A, B;
            public readonly double Radius;
            public bool Circle => Radius >= 0;
            public Curve(Vector2 a, Vector2 b, double radius = -1) { A = a; B = b; Radius = radius; }
        }

        public MeleeHitResolver(DamageDelivery delivery, int colliderCapacity = 64, int geometryCapacity = 512,
            int directionCapacity = 2048, int rayCapacity = 64, Action<MeleeQueryFailure> diagnostic = null)
        {
            this.delivery = delivery ?? throw new ArgumentNullException(nameof(delivery));
            if (colliderCapacity < 1 || geometryCapacity < 2 || directionCapacity < 3 || rayCapacity < 1)
                throw new ArgumentOutOfRangeException(nameof(colliderCapacity));
            candidates = new Collider2D[colliderCapacity]; hits = new RaycastHit2D[rayCapacity];
            accepted = new IDamageRecipient[colliderCapacity]; pending = new IDamageRecipient[colliderCapacity];
            contacts = new Vector2[colliderCapacity]; curves = new Curve[geometryCapacity]; angles = new double[directionCapacity];
            shapes = new PhysicsShapeGroup2D(geometryCapacity, geometryCapacity * 2); this.diagnostic = diagnostic;
        }
        public void Begin(MeleeAttackExecution execution)
        {
            if (owner != null && owner != execution) throw new InvalidOperationException("Melee resolver already executing.");
            Clear(); owner = execution; warned = false; LastFailure = MeleeQueryFailure.None;
        }
        public void End(MeleeAttackExecution execution) { if (owner == execution) { Clear(); owner = null; } }
        private void Clear()
        {
            Array.Clear(accepted, 0, accepted.Length); Array.Clear(pending, 0, pending.Length);
            Array.Clear(candidates, 0, candidates.Length); Array.Clear(hits, 0, hits.Length);
            acceptedCount = pendingCount = 0;
        }
        private bool Valid(MeleeAttackExecution execution) => owner == execution && !execution.IsComplete
            && execution.IsEvaluatingActive && execution.Context.CanApplyEffects;
        private bool Fail(MeleeQueryFailure reason)
        {
            LastFailure = reason;
            if (!warned)
            {
                warned = true;
                if (diagnostic != null) diagnostic(reason);
                else Debug.LogWarning("Melee query coverage incomplete: " + reason + ". Sample rejected.");
            }
            return false;
        }
        private static bool Contains(IDamageRecipient[] values, int count, IDamageRecipient target)
        { for (int i = 0; i < count; i++) if (ReferenceEquals(values[i], target)) return true; return false; }
        private bool AddPending(IDamageRecipient target, Vector2 point)
        {
            if (Contains(accepted, acceptedCount, target) || Contains(pending, pendingCount, target)) return true;
            if (pendingCount == pending.Length) return Fail(MeleeQueryFailure.Recipients);
            pending[pendingCount] = target; contacts[pendingCount++] = point; return true;
        }
        private static bool Self(Collider2D shape, GameObject actor) => shape.transform.IsChildOf(actor.transform)
            || actor.transform.IsChildOf(shape.transform);
        private static IDamageRecipient Recipient(Collider2D shape) => shape.GetComponentInParent<IDamageRecipient>();
        private static bool Self(IDamageRecipient recipient, GameObject actor) => recipient?.Entity != null
            && (recipient.Entity.transform.IsChildOf(actor.transform) || actor.transform.IsChildOf(recipient.Entity.transform));

        public void Sample(MeleeAttackExecution execution)
        {
            if (!Valid(execution)) return;
            var context = execution.Context; var config = execution.Configuration;
            origin = context.AttackOrigin; range = config.RangeWorld(context.UnitsPerTile) + RangeTolerance;
            baseAngle = Math.Atan2(context.Direction.y, context.Direction.x);
            halfArc = Math.Min(Math.PI, config.ArcDegrees * Math.PI / 360 + AngleTolerance);
            int targetMask = config.TargetLayers & Physics2D.GetLayerCollisionMask(context.ActorObject.layer);
            int blockerMask = config.BlockingLayers & Physics2D.GetLayerCollisionMask(context.ActorObject.layer);
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = targetMask | blockerMask, useTriggers = false };
            var physics = context.ActorObject.scene.GetPhysicsScene2D();
            int count = physics.OverlapCircle(origin, range, filter, candidates);
            if (count == candidates.Length) { Fail(MeleeQueryFailure.Candidates); return; }
            pendingCount = curveCount = angleCount = 0;
            Array.Clear(pending, 0, pending.Length);
            bool hasTarget = false;
            for (int i = 0; i < count; i++)
            {
                var shape = candidates[i];
                if (shape.gameObject.scene != context.ActorObject.scene || Self(shape, context.ActorObject)) continue;
                var recipient = Recipient(shape);
                if (recipient != null)
                {
                    if (Self(recipient, context.ActorObject) || (targetMask & (1 << shape.gameObject.layer)) == 0
                        || Contains(accepted, acceptedCount, recipient)) continue;
                    hasTarget = true;
                    if (shape.OverlapPoint(origin) && !AddPending(recipient, origin)) return;
                }
                else if ((blockerMask & (1 << shape.gameObject.layer)) == 0) continue;
                else if (shape.OverlapPoint(origin)) return; // Origin inside solid cover has no clear path.
                if (!ReadCurves(shape)) return;
            }
            if (!hasTarget) return;
            if (!AddAngle(-halfArc) || !AddAngle(0) || !AddAngle(halfArc)
                || !AddCurve(new Curve(Vector2.zero, default, range))) return;
            // Silhouettes, range crossings and boundary intersections partition visibility.
            // Sampling each boundary and open interval also finds partially exposed shapes.
            for (int i = 0; i < curveCount; i++)
                for (int j = i + 1; j < curveCount; j++) if (!Intersect(curves[i], curves[j])) return;
            Array.Sort(angles, 0, angleCount);
            double previous = angles[0];
            if (!Trace(previous, physics, filter, targetMask, blockerMask, context)) return;
            for (int i = 1; i < angleCount; i++)
            {
                double next = angles[i];
                if (next - previous <= 1e-10) continue;
                if (!Trace((previous + next) * .5, physics, filter, targetMask, blockerMask, context)
                    || !Trace(next, physics, filter, targetMask, blockerMask, context)) return;
                previous = next;
            }
            if (acceptedCount + pendingCount > accepted.Length) { Fail(MeleeQueryFailure.Recipients); return; }
            for (int i = 0; i < pendingCount && Valid(execution); i++)
            {
                var target = pending[i];
                var damage = new DamageContext(config.BaseDamage, context.ActorObject, target, context.ItemId,
                    context.SourceAction, contacts[i], context.Direction, config.DamageTags);
                var result = delivery.Deliver(damage, config.TargetEligibility);
                if (!result.WasApplied && result.Outcome != DamageOutcome.Resisted) continue;
                if (owner == execution) accepted[acceptedCount++] = target;
                execution.ReportContact(damage, result);
            }
        }
        private bool Trace(double angle, PhysicsScene2D physics, ContactFilter2D filter, int targetMask, int blockerMask, ItemActionContext context)
        {
            var direction = new Vector2((float)Math.Cos(baseAngle + angle), (float)Math.Sin(baseAngle + angle));
            int count = physics.Raycast(origin, direction, range, filter, hits);
            if (count == hits.Length) return Fail(MeleeQueryFailure.RayHits);
            float cover = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                var shape = hits[i].collider;
                if (shape.gameObject.scene == context.ActorObject.scene && !Self(shape, context.ActorObject)
                    && (blockerMask & (1 << shape.gameObject.layer)) != 0 && Recipient(shape) == null)
                    cover = Mathf.Min(cover, hits[i].distance);
            }
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i]; var shape = hit.collider;
                if (shape.gameObject.scene != context.ActorObject.scene || Self(shape, context.ActorObject)
                    || (targetMask & (1 << shape.gameObject.layer)) == 0 || hit.distance > cover + RangeTolerance) continue;
                var target = Recipient(shape);
                if (target != null && !Self(target, context.ActorObject) && !AddPending(target, hit.point)) return false;
            }
            return true;
        }
        private bool ReadCurves(Collider2D collider)
        {
            if (collider.shapeCount > curves.Length) return Fail(MeleeQueryFailure.Shapes);
            shapes.Clear();
            if (collider.GetShapes(shapes) != collider.shapeCount || shapes.shapeCount == 0 || shapes.vertexCount > curves.Length * 2)
                return Fail(MeleeQueryFailure.Shapes);
            var matrix = shapes.localToWorldMatrix;
            for (int s = 0; s < shapes.shapeCount; s++)
            {
                var shape = shapes.GetShape(s);
                if (shape.vertexCount == 0) return Fail(MeleeQueryFailure.Shapes);
                if (shape.shapeType != PhysicsShapeType2D.Circle && shape.shapeType != PhysicsShapeType2D.Capsule
                    && shape.shapeType != PhysicsShapeType2D.Polygon && shape.shapeType != PhysicsShapeType2D.Edges)
                    return Fail(MeleeQueryFailure.Shapes);
                for (int v = 0; v < shape.vertexCount; v++)
                {
                    Vector2 a = matrix.MultiplyPoint3x4(shapes.GetShapeVertex(s, v)); a -= origin;
                    if (shape.radius > 0 && !AddCurve(new Curve(a, default, shape.radius))) return false;
                    bool closed = shape.shapeType == PhysicsShapeType2D.Polygon;
                    if (v + 1 == shape.vertexCount && !closed) continue;
                    Vector2 b = matrix.MultiplyPoint3x4(shapes.GetShapeVertex(s, (v + 1) % shape.vertexCount)); b -= origin;
                    if (shape.radius <= 0) { if (!AddCurve(new Curve(a, b))) return false; }
                    else
                    {
                        var delta = (b - a).normalized; var normal = new Vector2(-delta.y, delta.x) * shape.radius;
                        if (!AddCurve(new Curve(a + normal, b + normal)) || !AddCurve(new Curve(a - normal, b - normal))) return false;
                    }
                }
            }
            return true;
        }
        private bool AddCurve(Curve curve)
        {
            if (curveCount == curves.Length) return Fail(MeleeQueryFailure.Shapes);
            curves[curveCount++] = curve;
            if (!AddPoint(curve.A)) return false;
            if (!curve.Circle) return AddPoint(curve.B);
            double distance = Math.Sqrt(Dot(curve.A, curve.A));
            if (distance <= curve.Radius) return true;
            double middle = Math.Atan2(curve.A.y, curve.A.x) - baseAngle, tangent = Math.Asin(curve.Radius / distance);
            return AddAngle(middle - tangent) && AddAngle(middle + tangent);
        }
        private bool AddPoint(Vector2 point) => point.sqrMagnitude < 1e-20f || AddAngle(Math.Atan2(point.y, point.x) - baseAngle);
        private bool AddAngle(double angle)
        {
            if (angle > Math.PI) angle -= 2 * Math.PI;
            else if (angle < -Math.PI) angle += 2 * Math.PI;
            if (angle < -halfArc || angle > halfArc) return true;
            if (angleCount == angles.Length) return Fail(MeleeQueryFailure.Directions);
            angles[angleCount++] = angle; return true;
        }
        private static double Cross(Vector2 a, Vector2 b) => (double)a.x * b.y - (double)a.y * b.x;
        private static double Dot(Vector2 a, Vector2 b) => (double)a.x * b.x + (double)a.y * b.y;
        private bool Intersect(Curve first, Curve second)
        {
            if (first.Circle && second.Circle)
            {
                var delta = second.A - first.A; double d2 = Dot(delta, delta), d = Math.Sqrt(d2);
                if (d < 1e-12 || d > first.Radius + second.Radius || d < Math.Abs(first.Radius - second.Radius)) return true;
                double x = (first.Radius * first.Radius - second.Radius * second.Radius + d2) / (2 * d);
                double h = Math.Sqrt(Math.Max(0, first.Radius * first.Radius - x * x));
                var direction = delta / (float)d; var middle = first.A + direction * (float)x;
                var normal = new Vector2(-direction.y, direction.x) * (float)h;
                return AddPoint(middle + normal) && AddPoint(middle - normal);
            }
            if (first.Circle) return Intersect(second, first);
            var edge = first.B - first.A;
            if (second.Circle)
            {
                var offset = first.A - second.A; double a = Dot(edge, edge);
                if (a < 1e-20) return true;
                double b = 2 * Dot(offset, edge), c = Dot(offset, offset) - second.Radius * second.Radius;
                double disc = b * b - 4 * a * c;
                if (disc < 0) return true;
                double root = Math.Sqrt(disc);
                return Along(first.A, edge, (-b - root) / (2 * a)) && Along(first.A, edge, (-b + root) / (2 * a));
            }
            var other = second.B - second.A; double denominator = Cross(edge, other);
            if (Math.Abs(denominator) < 1e-12) return true;
            var difference = second.A - first.A;
            double t = Cross(difference, other) / denominator, u = Cross(difference, edge) / denominator;
            return u < 0 || u > 1 || Along(first.A, edge, t);
        }
        private bool Along(Vector2 start, Vector2 edge, double t) => t < 0 || t > 1 || AddPoint(start + edge * (float)t);
    }
}
