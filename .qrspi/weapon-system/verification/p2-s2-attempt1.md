# Phase 2 Step 2 — attempt 1

Outcome: BLOCKED on one timing-fixture assumption; no staging or commit. Human approval required before repair under qrspi-implement Error Handling.

## Inputs and source

Spec SHA-256 78d09ac0e72bb413355cffdab28a21a5e501e69138731c6ad1f0863ff63a74f5; normalized overview d825b24d16443e871862dd17eb230f025b3a4416f7e34469b3e3b522d87bd8dc; Phase 2 13c7a7863b44b470e010260cd5c3a53a5711f4195b1fb29bc1adf5113c50110d. Provenance and all approved plan revisions checked before work and verification. Current committed base c5892ea7ea434cb1ae1aa61ef87171d0adee1a12. Ten-file p2-s2-attempt1-source.json snapshot exactly matches working tree and isolated gate copy after failure. Unrelated SOLITUDE_Start.unity edit preserved/outside scope. No staged changes.

## Implemented pending verification

Reusable per-actor MeleeHitResolver gathers real Physics2D contacts during the existing active port, deduplicates recipients across the swing, excludes self/triggers, evaluates configured masks with current collision layers, and delegates typed damage/eligibility. Source item identity/action and attack origin are carried through ItemActionContext. Resolver uses buffered native shape contours (circle/capsule/polygon/edge) to select silhouette, range-boundary and boundary-intersection ray directions; traces those directions and intervening intervals rather than relying on object pivots or a fixed angular grid. Physics2D supplies actual contact points and nearest world obstruction; recognized recipients are ignored as cleave blockers. Whole-sample preflight rejects incomplete coverage before applying any result, with a once-per-swing diagnostic. Geometry, direction, candidate, ray-hit and recipient-tracking buffers are bounded/reused. Reentrant binding/actor cancellation and gameplay freeze are rechecked between deliveries. Successful result event is available for later feedback; no feedback assets are added.

Live source Editor compilation passed. Local installed API probes confirmed shape coordinates/matrices and native circle minimum radius; arc fixtures use an actual circle silhouette with radius .05 rather than an unrepresentable tiny shape. Authoring data defaults TargetLayers/BlockingLayers to Physics2D.DefaultRaycastLayers (-5); no existing item asset was rewritten.

## Criterion-linked verification and failure

Command: python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-weapon-system-p1-s2-attempt2 --output /Users/jeremyvalentine/SOLITUDE/Logs/WeaponSystemP2S2Attempt1.
Expected: complete isolated gate passes. Actual: 269 pure tests passed, authoring passed, 516 Editor passed/0 failed/skipped; 202 PlayMode passed, 1 failed, 0 skipped. Gate stopped at PlayMode exit 8; builds and standalone scenarios did not run. Retained results/XML/pure logs alongside this report.

S3/S17/S18/S19/S20: all 24 new geometry/rule/saturation cases passed, including inclusive range and ±45-degree arc, large/offset/closest-point-outside-sector shapes, circle/capsule/polygon/rounded/rotated/edge contacts, enclosing origin with start-inside raycasts disabled, self/child exclusion, compound deduplication, multi-target cleave, complete/partial cover, a narrow offset opening, triggers/layers and required/excluded queries. Every bounded coverage failure rejected the whole sample and diagnosed once, including recipient tracking across samples.

S12/S13/S14: coarse/subdivided static-target delivery, reentrant cancellation/lockout, pause during delivery, actor invalidation, active observer interruption, origin/tile scaling, masks and independent actors sharing configuration passed. One additional timing case failed: MeleeActiveWindowTests.WindupAndRecoveryNeverDeliverAndActiveSamplesDeduplicate, line 22, expected late-target calls 0 but actual 1.

Source trace: the fixture adds .119 + .001 + .02 + .08 before creating its late target. Double accumulation leaves elapsed approximately .21999999999999997, strictly before the .22 active end. The following advance legitimately processes that remaining active interval, including the newly present target. The fixture had not established Recovery; this is not evidence that recovery itself deals damage. Existing pure exact-boundary timing tests passed.

## Proposed decision

Change only that fixture to advance clearly inside Recovery (e.g. use .081 in place of the final .08), assert Timing.Phase == Recovery before spawning the late target, and retain the no-recovery-hit/once-per-target assertions. Do not relax geometry tolerances, change the production timing model, or weaken other tests. Rerun the complete gate against a new source snapshot. Step 2 and Phase 2 marked blocked; no later step started.
