# Phase 2 Step 2 — fresh production Use input

## Scope and revisions

Single-step continuation, S6/S7/S8/S9/S10/S15. Spec SHA-256 `a2d29013720f27cf817b3e09f35a625519fc7a9cd75eb67f2f4602bd8df22b8f`; normalized overview `41535179a89ccdd3d5146a6e1b07a723b8727dfaa03cfc600f14a29e6346ffd8`; normalized Phase 2 `a74eb197ed164001df239fc832653a1d364356f1489503d8d84a8e0a8150039e`. All approved upstream/provenance hashes matched before implementation and after verification. Only progress markers changed. Base HEAD `7d23e01c6b6c95fc39ddce4f419d9e855ec0e665`; HEAD before commit `7d23e01c6b6c95fc39ddce4f419d9e855ec0e665`. Six scoped source files recorded in p2-s2/scoped-source-digests.json, stable through final verification. Existing asset/script GUIDs preserved.

Unrelated NPC/art/reference/export files appeared in the shared workspace mid-step. They remain untouched and excluded from staging/commit. The gate's raw source-digests.json includes 328 unrelated paths alongside the six scoped files; unrelated-snapshot-paths.json records that distinction. The gate copied the shared Assets snapshot, but no claim is made of qualifying the concurrent art work or its later changes.

## Implementation and criterion-linked evidence

- S15: added Gameplay Use as a Button, keyboard F/gamepad West. Authored through native InputActionAsset setup on a detached clone, ToJson and existing Unity importer/wrapper generation. All original maps, actions, bindings and control schemes remain identical as ordered dictionaries, including IDs, paths, interactions and groups. Legacy UI bindings contain repeated empty IDs, so comparison uses preserved order rather than treating ID as unique. Generated wrapper was not hand-edited.
- S6/S7: feature owns one leased route to the current selected-item coordinator. Real keyboard/gamepad events use the actual player controller/composition and inventory boundary with transient definitions. Food restores 25 and removes one; medical health 90/hunger 0 reaches 100 without hunger change. No selection/unsupported/full targets preserve selection and inventory.
- S8/S9: held controls require release before another use. Capture on policy/host-disable transitions prevents re-enable replay; after-update release checks also work while actions are disabled. GameplayAllowed and ConsumeUIActivation block the closing frame. Real event tests cover repeated held frames, unready, pause, actual modal, transition, nonzero-scale primary screen, disabled host/feature, repeated binding and released input. Fresh release/press then works.
- S10: shared survivor reset captures Use, and held keyboard/gamepad use across actual controller awakening requires release/fresh press. No replay after awakening; same closing-frame protection retained.
- S15 preservation: existing real keyboard/EventSystem regression passed; Move/Interact/Pause/Inventory and digit selection retain bindings and gating. Existing journal/input regressions ran in the complete suite.

## Verification and attempts

Final focused output: `/private/tmp/solitude-vitals-p2-s2-focused2-tq3mzc3b`. Exact argv in command.json: Unity CLI test EditMode filter PlayerVitalsInputTests and UnityInputCompositionTests, with isolated saves and nographics log. 2 Passed, no failures/skips. First focused run `/private/tmp/solitude-vitals-p2-s2-focused-m2dm7qkw` did not compile because its fixture called internal SetModal from Editor tests. Corrected fixture to use the public production ContainerModalCoordinator; policy API unchanged. Failed first attempt retained, not counted as a pass.

Input authoring script diagnostic attempts did not save changes: missing extension qualification, incorrect named parameter, then immutable enabled imported asset. Successful authoring uses a detached FromJson clone, qualified native setup, ToJson/ImportAsset. Final script and original input JSON retained beside this evidence.

Full command: `python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-vitals-repair-project-yjjp5q_x --output /private/tmp/solitude-vitals-p2-s2-gate-3kb8v5yr`. Exit 0; results.json Passed. 183 pure +287 EditMode +103 PlayMode =573 Passed, zero failures/skips. Both native ARM64 builds passed; authoring/metadata/shipping exclusion validation and all world/save/interrupted-write/recovery scenarios passed. Deliberate crash exits qualified by restart checks. Raw command/XML/log/scenario evidence retained at this output path. Scoped digests matched after verification and before staging.

## Limits and next gate

Implementation evidence, no independent review verdict. Native qualification macOS ARM64/Wakeup only. Production Ration/Medical Kit assets and Use hints remain Step 3; current successful input journey uses transient fixture consumables. No HUD geometry/visual changes or new visual claims. Step 4 will qualify the complete authored feature.

Next: Step 3, production definitions and contextual Use hints.

Committed scoped snapshot: `bb25ca0cb4d85d0458e86645c5f16e22d40c2f2c`. Six scoped source files only, verified staged bytes matched the qualification manifest. Unrelated NPC/art work remains untracked and untouched; whole working tree is not claimed clean.
