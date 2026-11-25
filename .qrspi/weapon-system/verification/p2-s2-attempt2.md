# Phase 2 Step 2 — attempt 2

Outcome: PASS for S3/S12/S13/S14/S17/S18/S19/S20; supports S31 through bounded/reused query infrastructure, not allocation measurement.

## Approval, inputs and snapshot

Human approved fixing the failed Recovery fixture and rerunning the full gate. Only MeleeActiveWindowTests.cs changed between attempts: .081 replaces .08 and the fixture asserts Recovery before spawning the late target. Production timing, geometry/tolerances and other assertions unchanged. Attempt 1 evidence retained.

Spec SHA-256 78d09ac0e72bb413355cffdab28a21a5e501e69138731c6ad1f0863ff63a74f5; normalized overview d825b24d16443e871862dd17eb230f025b3a4416f7e34469b3e3b522d87bd8dc; Phase 2 13c7a7863b44b470e010260cd5c3a53a5711f4195b1fb29bc1adf5113c50110d. Upstream provenance and all approved normalized plan revisions checked on resume and after verification. Ten-file snapshot p2-s2-attempt2-source.json matches working tree, isolated gate source and staged index after verification. Unrelated SOLITUDE_Start.unity edit remains excluded.

## Implementation

One reusable per-actor MeleeHitResolver owns bounded collider, ray, contour, direction, pending-contact and accepted-recipient buffers. Unity native shape contours provide line/circle boundary primitives for circles, capsules, polygons and edges, including compound/rounded/rotated shapes. Silhouettes, range crossings and boundary intersections partition angular visibility; actual Physics2D rays at boundaries and interval interiors provide contacts. This finds exposed portions without relying on sprite pivots, a closest point alone or a fixed angular sample grid. The private contour math is part of this resolver, not a separate collision/ability framework.

Physics queries are scoped to the actor's PhysicsScene2D and configured TargetLayers/BlockingLayers combined with current collision-layer rules; trigger/self hierarchy contacts are excluded. Default attack point is actor movement/feet transform, with configurable tile-relative offset and existing units-per-tile scaling. Boundary tolerances are .00005 world units and .0005 degrees, below spec maxima. Recognized recipients are ignored as world blockers for cleaves; actual solid cover remains authoritative. Enclosing target contacts work even with start-inside raycasts disabled; origin inside solid world cover rejects.

Each active sample preflights complete query/geometry coverage before delivering any contact. Candidate, contour, direction, ray-hit or cumulative recipient saturation rejects the whole new sample with one diagnostic per swing. Accepted recipients deduplicate across colliders and active samples. Execution/binding validity and gameplay eligibility are checked between recipients; reentrant cancellation stops further delivery and pause freezes remaining effects. Resolver ownership/target references clear on interruption/completion/retirement. ItemActionContext carries source action, configured origin and resolver; damage retains selected item identity and explicit action damage tags. HitApplied reports successful shared delivery only, reserved for later feedback. New actions still dispatch through their own ItemActionDefinition; no tag-to-behavior switch or movement-controller weapon logic added.

## Criterion-linked verification

Command: python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-weapon-system-p1-s2-attempt2 --output /Users/jeremyvalentine/SOLITUDE/Logs/WeaponSystemP2S2Attempt2.
Expected: corrected Recovery fixture and all geometry/timing/regression/build checks pass. Actual: results.json Passed; 46 stages/scenarios completed; 269 pure tests passed/0 failed; 516 Editor passed/0 failed/0 skipped; 203 PlayMode passed/0 failed/0 skipped. Both macOS ARM64 verification/shipping builds passed. Deliberate crash-injection recovery outcomes validated by the gate. Retained results/XML/pure logs alongside this report.

- S17: all geometry cases passed for just-inside/exact/outside range, ±45-degree inclusive arc and outside/behind positions; offset entity pivot, center/closest-origin point outside sector with another valid contact, actual circle/capsule/polygon/rounded/rotated/edge contacts and origin-enclosing collider. Installed Unity API probe established native contour coordinates/matrix use and minimum circle radius; boundary fixture uses actual circle silhouette at radius .05.
- S18: complete cover rejects; partially exposed target and narrow offset opening hit actual contacts. Triggers and excluded layers do not block or become targets; configured cover layer does block. Other recognized recipients do not block cleaves. All five bounded coverage-failure paths reject entire new sample and diagnose once, including cumulative recipient tracking across active samples.
- S19: multiple target colliders/samples receive one accepted delivery per swing; several recipients cleave; actor and child self shapes never receive damage.
- S3/S20: untagged recipients work with empty query; required/excluded queries reject without applied-hit events. Source identity/action/contact/direction and explicit compiled tags feed shared DamageDelivery; target availability remains shared recipient responsibility.
- S12/S13/S14: Windup/Recovery do not deliver. Corrected Recovery fixture passes with explicit phase assertion. One .55 advance, five advances and 55 advances each hit a static target once and finish Idle. Reentrant equipment invalidation prevents later cleave hits and preserves commitment; pause during delivery freezes further effects without canceling the action. Actor invalidation and active-observer interruption leave no later delivery. Captured origin/tile scaling, masks and shared-configuration independent actors pass.
- S31 support: buffer ownership/reuse and bounded diagnostics inspected and exercised; steady-state allocation/performance measurement remains Phase 4. No allocation-free claim is made.

All 24 MeleeHitDetectionTests cases and 13 MeleeActiveWindowTests cases passed. Existing damage/SEC-0, player lifecycle/input/movement/vitals, item/container, room and persistence regressions remain passing under full gate. Live source Editor compilation passed. After staging, ValidateMelee.Run passed metadata (approved review-archive exception), tags and presentation/capability compilation; authored wrench remains 10 damage, 1.25 tiles, 90 degrees, .55 seconds, one damage tag and default masks -5. Both transport and nested execution success checked; retained p2-s2-attempt2-index-validation.json. Staged C# whitespace check passed.

## Limits and next boundary

Real Physics2D/actual shared execution/recipient integration is verified in disposable local-physics fixtures. Actual-input SEC-0 composed encounter journey is Step 4; impact flash/effect/audio/knockback is Step 3. No new feedback assets, loadout, production saves or scene layout edits. Oversized/unsupported or saturated geometry conservatively misses with a diagnostic; buffer limits are explicit. Final actual-scene readability, performance measurements and production starting weapon remain later planned work. This attempt does not claim independent review or manual gameplay/visual/audio qualification.

Commit: `28a9bc1431ec3444e2f338b32d79e394460fd1ed`; committed bytes rechecked against the tested ten-file snapshot.
