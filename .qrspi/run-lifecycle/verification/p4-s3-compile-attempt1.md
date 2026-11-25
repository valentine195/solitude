# Phase 4 Step 3 — compile attempt 1: FAILED / awaiting human decision

Spec SHA-256: `a9dc706c5f1628759fbfaed64ffc89258aeaead70a1d24987b9b4ce6b9882922`. Approved revised Phase 4 normalized SHA-256: `48444a15b8da8933e449efb2d7d9217cd682acec4aa0df3275f6afcb8a3cfe7a`; complete approved manifest: `p4-s3-approved-plans.json`. The human approved the proposed amendment with “continue.” Product base remains `6e42cdc`; no step/prerequisite commit was made.

## Implemented, not yet qualified

The committed primary Wakeup UI overrides were preserved. Unity Editor APIs added a guarded, one-time production route: SEC-0, one bandage/ration source each, existing fuse with critical-source policy, scrubber with existing equipment screen, battery-powered permanent shortcut, and a marked live-conduit trigger calling existing vitals damage. The existing shared locker and awakening point remain in place. All four original Tilemap blocks match the saved primary scene byte-for-byte, and the Wakeup scene GUID is unchanged. Existing source/catalog/scene baseline copies remain under `p4-s3-baselines/`.

The revised plan's regression fixtures now use explicit missing-fuse data, ID-scoped injected records, one composed scrubber endpoint set and deduplicated catalog entries. New editor binding validation and two input-driven gameplay journeys were written. They have not run. The first Unity authoring command succeeded before the screenshot helper was added; raw log: `p4-s3-authoring.log`.

## Failed check

Command: `unity run /Users/jeremyvalentine/.codex/worktrees/run-lifecycle-ship-persistence/SOLITUDE --timeout 300 --no-tail --log-file /private/tmp/solitude-p4-s3-import.log -- -nographics -executeMethod SOLITUDE.Editor.RunLifecycleAuthoring.InspectProductionRoute` (CLI at `/Users/jeremyvalentine/.unity/bin/unity`).
Expected: compile/import new test source and inspect authored scene.
Actual: CLI exit 6, Editor compiler error CS0234 at RunLifecycleAuthoring.cs:163. The screenshot helper refers to `UnityEngine.U2D.PixelPerfectCamera`, which is not the project's camera type namespace. Current package source `Library/PackageCache/com.unity.render-pipelines.universal@b0678dfc9e21/Runtime/2D/PixelPerfectCamera.cs` declares `UnityEngine.Rendering.Universal.PixelPerfectCamera`.

No full gate, new tests, builds, process checks or gameplay screenshots ran for this step. Source bytes are recorded in `p4-s3-compile-attempt1-source.json`.

## Proposed correction

Apply the exact one-line namespace replacement in `p4-s3-compile-repair-proposal.patch`, then rerun compilation and the planned verification. No assertions, behavior, scene data, criteria or approved plan changes are proposed. Active step remains blocked pending the human decision; no completed marker or implementation commit was made.
