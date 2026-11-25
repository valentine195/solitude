# Phase 2 Step 1 — collect replacement fuse

Commit: `fac96d38d0adbcfdeb6724ae31df6da714fb1ad2`. Source snapshot: `phase2-step1-source-snapshot.json`.
Spec SHA-256: `b487402c716f8e7d191e6cac3b4624368e3a3376dccadb7cd495c683e25b012f`; normalized phase plan SHA-256: `228139f6c11f67c0ee7e0cabac57b39e3282f208f089ab5b1867ac85915c170b`. Approved inputs unchanged.

Authored one GUID-identified replacement fuse definition, a one-item pickup definition and prefab, and one reachable scene instance with an independent SaveableId. Reused the existing SOLITUDE electrical component sprite at native scale and existing WorldPickup/SceneBindings collection. Local catalog gained one entry; production database unchanged. Scene/catalog metadata GUIDs preserved. Source asset artwork is reused; no new raster art or manual visual-quality verdict.

| Criteria | Verification | Expected and actual |
| --- | --- | --- |
| S1/S3 | `unity run /private/tmp/solitude-scrubber-phase1 -- -executeMethod SOLITUDE.Editor.ScrubberEncounterBuilder.AddFuse -nographics`; log `phase2-step1-authoring.log` | Unity asset authoring completed, exit 0; scene/catalog GUIDs verified unchanged during copy-back |
| S1/S3; preserve S2/S7/S8/S10 | `unity test /private/tmp/solitude-scrubber-phase1 --mode PlayMode --filter SOLITUDE.Tests.ScrubberEncounterSliceTests` with isolated save directory; `phase2-step1-playmode.xml` | Unobstructed spawn-to-pickup path; actual keyboard focus collection; full inventory preserves pickup and displays Inventory full; free slot permits exactly one fuse; repeat direct/input activation grants none; save sentinel unchanged. 3 passed, 0 failed, 0 skipped |
| S3 | Same isolated project, `--mode EditMode --filter 'SOLITUDE.Tests.ScrubberEncounterAuthoringTests;SOLITUDE.Tests.PickupPersistenceTests'`; `phase2-step1-editor.xml` | Definition GUID identity, icon/name/prefab, local-only membership, unique scene identity and registered endpoint checked. Existing pickup regression tests passed. See XML for exact test count; zero failures/skips |
| Hygiene | C# staged `git diff --check` and serialized check with `core.whitespace=-blank-at-eol` | Passed; Unity's conventional empty-field trailing spaces preserved |

Phase 2 Step 1 is complete. Step 2 (atomic repair/failure controls) remains unstarted. Phase 2 is not complete. Full player-build/regression qualification remains scheduled for Phase 3; the earlier human waiver covers only Phase 1 manual checks.
