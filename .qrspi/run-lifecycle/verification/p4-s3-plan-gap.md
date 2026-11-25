# Phase 4 Step 3 — plan gap / awaiting human decision

Spec remains unchanged: `a9dc706c5f1628759fbfaed64ffc89258aeaead70a1d24987b9b4ce6b9882922`. Step base: `6e42cdc`. The approved Step 3 integrates the existing fuse/scrubber in the production scene and preserves regression verification, but its file list omits the production catalog and three regression fixtures that rely on the old demo-only boundary.

## Evidence

- `Tests/Editor/ScrubberEncounterAuthoringTests.cs:145` explicitly asserts the production catalog does not contain `ScrubberEquipment.FuseItemId`.
- `Tests/Editor/ScrubberTaskSourceTests.cs:130–135` uses the production catalog as the deliberately missing-fuse case. Production registration makes this a valid fuse catalog and invalidates the negative fixture assumption.
- `Tests/PlayMode/RunLifecycleShipPersistenceTests.cs:61` selects a global single ScrubberEquipment; its Author callback at lines 93–111 adds its own equipment and second ScrubberComposition. ScrubberComposition.Validate requires exactly one composition per scene and owns every equipment/screen/source endpoint. The same fixture unconditionally appends the fuse at line 127, and several critical-source assertions select an unqualified single record at lines 376, 392, 414, 421–422. These assumptions cannot survive the specified production scrubber/source without fixture integration.
- Production Database.asset currently contains battery, O2 canister, bandage and ration, but not the existing fuse definition. The production ScrubberComposition requires the fuse presentation/catalog entry to bind the repair workflow.

No failing tests were run to discover this; these are complete static fixture/caller traces. Step 3 verification, gameplay, builds and manual route checks have not run.

## Proposed amendment

Add the production Database.asset and the three named regression test files to Step 3. Register the existing fuse without changing its GUID or demo catalog. Preserve the missing-catalog negative case using an explicit fuse-free fixture catalog. Reuse the scene's one scrubber composition and scope injected fixture endpoints/records by their existing IDs. Preserve all existing behavior assertions and fault checks. Product scope, criterion IDs, all other steps, spec, and the required full gate remain unchanged.

Concrete proposed phase file: `p4-s3-plan-proposal/plan-phase-4.md`; exact diff: `p4-s3-plan-proposal/plan.patch`; complete proposed plan revision manifest: `p4-s3-plan-proposal/proposed-plan-hashes.json`.

## Preserved execution state

Both the Phase 4 entry and committed-main Wakeup scenes were backed up under `p4-s3-baselines/`. The isolated worktree adopted the exact saved primary scene from commit `3839559`, preserving its UI overrides. RunLifecycleAuthoring gained only a read-only inspection method. A Unity Editor preview inspection completed successfully and logged existing tilemaps, colliders, player and awakening positions in `/private/tmp/solitude-p4-s3-inspect.log`. No route objects, floor, walls, supplies, hazard or scrubber were placed or modified. No implementation commit was made. The primary product tree is unchanged.

The active approved plan has not been amended. Its Step 3 marker is blocked pending human approval of the proposed exact revision.
