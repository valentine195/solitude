## Verdict: PASS

All S1–S12 behaviors are established for the reviewed snapshot; no blocking or conditional findings remain.

## Scope

Final adversarial review of the entire survivor-succession feature, all three phases including Phase 3 Step 3, against S1–S12. The plan was used only to determine scope. No implementation reports, explanation artifacts, or upstream feature-intent artifacts were read. The caller supplied upstream provenance validation; allowed spec and normalized plan revisions were independently checked.

Exact command run from `/Users/jeremyvalentine/SOLITUDE`:

```sh
git diff 4de49500a28f6506f18eb25eda65437cf4e6715c 64ae846597a0d2c13815d4f7a23ed6fb5b583374 -- . ':(exclude).qrspi'
```

Snapshot: resolved base `4de49500a28f6506f18eb25eda65437cf4e6715c`; HEAD `64ae846597a0d2c13815d4f7a23ed6fb5b583374`; exact product diff SHA-256 `44380cc234b252b32b8d7f2a11140a40a6396ea0489ef4c48e3436eb40117a0e`. QRSPI hunks were explicitly excluded.

Spec: `.qrspi/survivor-succession/spec.md`, SHA-256 `f2253ab3429d7c5a34c8e9275f9efac61be9e3f4c30781393ad7d86e68ba85a4`.

Plan digests, normalized only on status-marker tokens and phase-table Status cells to `[ ]`:

| Path | Normalized SHA-256 |
| --- | --- |
| `.qrspi/survivor-succession/plan.md` | `2263c441d5c4d9e7c5a0df14e076cce4caf14668514c7906c8fd6686e902c54f` |
| `.qrspi/survivor-succession/plans/plan-phase-1.md` | `1721ef5a35512f2703c61c3ff82e7bf3f59f607a411549dca59f16fe8f86e952` |
| `.qrspi/survivor-succession/plans/plan-phase-2.md` | `9174ac5760d00bbd53f98cbbed9c5a5cd06a6b62cca9f59b5bbbb5f5a8b9054c` |
| `.qrspi/survivor-succession/plans/plan-phase-3.md` | `383a4cc4068ed2c97fe249f40f37f8497828cb91394f63c53b661057c8945c13` |

Working tree/staged status: clean before and after the exact diff, during inspection, and before publication. No untracked product source. `git check-ignore .qrspi/` returned `.qrspi/`. Inspected source was full working-tree content matching HEAD; no product mutations were performed. Serialized scenes were parsed as complete document structures, with feature/player binding references and authored data inspected in context. No implementation-route deviation affecting the verdict was identified.

Source files read: the full changed-file and unchanged-dependency content digests appear below and in `final-r1-source.json`; origin is working tree / specified HEAD. The sidecar also records exact raw-evidence hashes. Snapshot verified unchanged immediately before verdict publication: HEAD, clean status, diff, spec, normalized plans, and every recorded source/dependency digest matched the captured values.

## Findings

None.

## Evidence

### Independent source review

Reviewed lifecycle selection, stale/reentrant requests, irreversible retirement, receipt ordering, both-source conservation, empty deaths, forward retry, cleanup and repeated scene lifetimes. The coordinator records the commit stage/receipt before activation and never repeats a committed handoff. Domain planning snapshots all three owners and rejects changed generations/revisions/capacities; it installs complete ownership before notifying observers and catches observer failures. Existing fixed player reader instances remain registered.

Reviewed transition policy, retained input asset, screen/gesture cancellation, held-control suppression, physics relocation, proximity rebuilding, vital object retention and view subscription readiness. Ship features keep their state and scene interaction scope; succession uses no scene reload, save reset or ship-state mutation. Engine-independent assemblies retain `noEngineReferences: true`; the ship source search found no survivor/succession dependency.

Reviewed recovery prefab authoring, inactive preparation, source identity/death location, scene registration, publication, generic container target presentation, capacity and ordinary transfer rules. Scroll content resets on bind and uses bounded viewport sizing. Exhaustion is observed through SlotChanged, processed in LateUpdate after command publication, and deactivates presentation without unregistering its owner. OnDisable closes that target modal and removes it from proximity; CanInteract also rejects empty/inactive targets. Nonempty partial caches retain ordinary interaction.

### Independently inspected raw qualification

This review did not rerun Unity tests, builds, player scenarios or the complete gate. It independently read the permitted machine outputs under `Logs/SurvivorSuccession/final-f1-repair`, inspected the verification runner source, and compared the isolated project at `/private/tmp/solitude-succession-f1-gate` with current tracked qualification inputs before relying on results.

Actual raw results:

- `results.json`: `status: Passed`, macOS ARM64, full copy/authoring/test/build/player stages present; both verification and shipping builds exit 0. No partial-gate status.
- `pure-tests.runner.log`: 121 passed, zero failed.
- `editmode.xml`: 221 passed, zero failed/inconclusive/skipped.
- `playmode.xml`: 71 passed, zero failed/inconclusive/skipped. Every test-case result is Passed.
- `validation/validation.json`: success true, findings empty.
- The expected deliberate interruption stages report exit -9; their restart stages report exit 0. The inspected runner requires each interruption checkpoint, preserved backup bytes and complete resumed snapshots before writing Passed.

All tracked Assets gameplay/test source and authored feature assets, plus Packages, match the isolated source copy. Five settings assets differ through Editor/import/build serialization: DefaultVolumeProfile, UniversalRP, UniversalRenderPipelineGlobalSettings, GraphicsSettings and ProjectSettings. Independently inspected diffs show serialized/new render fields, generated prefilter/runtime lists, and verification shipping company/product/identifier/backend settings. These differences limit byte-identical settings qualification; they do not alter the feature scripts, test scripts, scenes or recovery/UI prefabs. Tools are intentionally run from the repository root rather than copied into the isolated project. The full gate qualifies macOS ARM64/Wakeup; additional source-matching succession, restoration and scrubber PlayMode cases provide their respective coverage. Durable survivor/cache persistence after unload remains explicitly out of scope.

Source-matching PlayMode results include SamePositionDeathsRemainRecoverableThroughOrdinaryFocusAfterExhaustion, full-capacity last-slot scrolling/transfer, partial/full/repeated recovery, retained Wakeup pickup/locker journey, restored terminal/door/subsequent repair and traversal, authored scrubber progression/readers, held movement/open inventory/journal/transfer, stale gesture callbacks/held pointer, activation failure/receipt retry and uncertain acknowledgment deduplication. The test source was independently read to assess its assertions; passing labels alone were not treated as proof.

### Historical visual evidence and limits

Independently viewed historical `final-independent-review/manual-awakening.png`, `manual-succession.png` and `manual-recovery-ui.png`: cryobay placement, retained HUD/camera framing, recovery prompt/target label and visible AI acknowledgment are visible. These are explicitly historical images from the prior snapshot, not new observations or proof of the repaired exhaustion behavior. The corresponding scene, prefab, avatar, vital/HUD and feedback source remains unchanged; current repaired interaction is established by source trace and the new source-matching ordinary-focus/UI regression. No new live manual Unity observation is claimed.

### Other checks

Ran the exact product diff and clean-status checks; independently recomputed spec and normalized plan revisions and source hashes; parsed scene documents and inspected authored references; compared tracked qualification source bytes with the isolated gate; parsed every raw XML test-case result; searched ship/Domain/Application source for succession dependencies. `git diff --check` exited 2 for Unity-generated empty serialized fields with trailing whitespace. These are not a behavioral defect or a documented style violation.

## Open Questions

None preventing conformance verification. The execution and historical-visual limits above are not claimed as new executed passes.

## Supplemental Reviews

None provided. Permitted prior reviews were read after the independent whole-feature inspection.

- `reviews/final.md`: prior F1 / S5, S9 is resolved. Previously exhausted co-located owners stayed ordinary focus targets. Current code withdraws exhausted presentation after publication, closes its modal and removes proximity while retaining ownership and death location. The new actual same-position A→B→C test naturally selects and exhausts one cache through production UI handlers, then naturally focuses/opens/recovers the other; it asserts exact total 8, retained independent owners and unchanged locations. The current raw PlayMode case passes.
- `reviews/phase-1.md`: prior F1 / S7 coverage gap is resolved by composed positive post-awakening recovery and ship interaction tests. In particular, the same-position case includes an earlier published cache at the awakening position and ordinary focus/interaction after succession; restoration recovery and subsequent repair/traversal also exercise B.
- `reviews/final-source.json` was inspected only as permitted historical provenance context; its older findings/results were not used as current approval.

## Spec Conformance

PRESENT S1 — Survivor initialization exposes a distinct active Guid and awakens the retained avatar at the configured scene-local point. Wakeup authors CryobayAwakeningPoint at (-0.89, -2.4, 0); scrubber uses (23, 4.5, 0). Authoring and composed initial-placement assertions pass in source-matching results; historical cryobay image supports presentation.

PRESENT S2 — RequestDeath binds the expected departing identity, rejects stale successor/deceased requests and reentrancy, permanently retires A after one equipment boundary, activates distinct B, and clears the pending transition only after acknowledgment/ungating. Pure and composed stale/reentrant/retry cases establish one successor/cache.

PRESENT S3 — One handoff empties both retained carried readers; awakening zeros movement/linear/angular velocity, selects -1 and resets existing vital objects to authored values. Screen close, gesture cancellation and held-control suppression remove transient A actions. Composed reset and readiness/input cases pass.

PRESENT S4 — Recovery receives the exact inventory/hotbar immutable stacks in one transaction, with unique independently registered ownership, source survivor Guid and captured physics death position. Full/mixed sources, scene death and multi-cache quantities are asserted; committed ownership survives activation interruption.

PRESENT S5 — Existing proximity, interaction, transfer modal and production gesture/UI handlers recover exact quantities. Partial merge/full target/repeated attempts preserve remaining contents; full-capacity last slot is reachable through scroll events. Exhausted targets withdraw without deleting owners, permitting recovery of the other same-position cache. Current source and raw tests establish these paths.

PRESENT S6 — Succession retains scene/root/player readers and does not touch ship state, reload scenes or reset saves. Wakeup collection/locker facts, restored systems/terminals/powered-open doors/subsequent repair and traversal, scrubber installed/running/progress/readers remain live in passing composed cases.

PRESENT S7 — Retained avatar/input/camera/vital display and reconstructed proximity serve the successor. Existing screens close, gestures cancel, held controls require release and stale presenter generations cannot act after unbind. Positive ordinary interaction, new movement and powered-door traversal resume in source-matching cases.

PRESENT S8 — A transition-ID acknowledgment set reserves delivery once, visible feedback displays the brief AI message, and repeated/stale/precommit or activation-failed requests cannot deliver success again. Scene first-contact Heard state is neither recreated nor reset. Feedback visibility, first-contact continuity and uncertain-delivery retry assertions pass; historical screenshot shows the visible message.

PRESENT S9 — Unique identities and terminal death status hold across successive/empty deaths; older recovery handles and quantities survive later deaths. Empty equipment creates no cache. Partial-cache and same-position ordinary recovery tests prove independent continued recovery with exact totals.

PRESENT S10 — Wrong/missing/inactive authoring and preparation/commit rejection leave A/equipment unchanged. Postcommit failure retains a pending successor/receipt, no active Current, conserved recovery equipment and gated gameplay; retry skips committed transfer. Uncertain acknowledgment retains one active successor gated for forward completion. Pure and real Unity fault-port cases pass.

PRESENT S11 — State/choice rules live in Domain/Application through narrow ports, with no Unity or ship-feature references; ship features do not depend on succession. Existing asmdefs, GUIDs, retained avatar/player readers and SceneFeatureComposition boundary remain valid. Authoring/assembly and compatibility checks pass.

PRESENT S12 — The independently inspected complete source-corresponding macOS ARM64/Wakeup gate reports Passed, with 121 pure, 221 EditMode and 71 PlayMode cases, validation, both native builds and production save/recovery interruption scenarios. Existing inventory/hotbar/pickup/locker/restoration/scrubber/door regressions remain asserted. Settings and historical visual limits are explicitly recorded above.

## Source content digests

| Path (working tree / HEAD) | SHA-256 |
| --- | --- |
| `Assets/_Project/ARCHITECTURE.md` | `855391ba68cdf06e7895883dca350a6c0e8bbe8b7589fa169bffafe3ff0f3c8e` |
| `Assets/_Project/Application/RuntimePolicy.cs` | `3e1ac34b6c013fad11b50cb5b759584845c25cc18e1378c73981cd65329916a6` |
| `Assets/_Project/Application/SOLITUDE.Application.asmdef` | `70c6b39b40506b847c15f8324b1f300c5ce802f2ceb2efa1887fe224bb347899` |
| `Assets/_Project/Application/Survivors.meta` | `a401ef35efa4e893c39f340175489a297ba8454c745e89647cce20a2865e292f` |
| `Assets/_Project/Application/Survivors/ISurvivorSuccessionPorts.cs` | `8d6963f4a691139ea622593ec2dc653fc94f08a8e37dafd8b9ff5ab284b84936` |
| `Assets/_Project/Application/Survivors/ISurvivorSuccessionPorts.cs.meta` | `1d4aa13875f1a7d47566d6208f19d12f2a17792f39d7f03e54fc7d2b6951f484` |
| `Assets/_Project/Application/Survivors/SurvivorSuccessionCoordinator.cs` | `3b8ef879895941dca7d64f1b888164ad8de91dc962a14a90609ba17e15f0cce9` |
| `Assets/_Project/Application/Survivors/SurvivorSuccessionCoordinator.cs.meta` | `4aed5276d56be45c7fadbb4e43e3eb7e7c1b1837d0f65c086327c8327af4fef6` |
| `Assets/_Project/Core/Composition/README.md` | `0c748deea4aa6bc0bd88b3c6b59a31da038849fc241a171508ec5a968f79d311` |
| `Assets/_Project/Core/Composition/SceneBindings.cs` | `221245238e2dac550238bff0d913ec855358c4af21715144b3d4f539179297e7` |
| `Assets/_Project/Core/Composition/SceneFeatureContext.cs` | `31e664e05d5b29986de039807efd9090cd7a4ccf9b6e9bc4b17000a17f4095ae` |
| `Assets/_Project/Core/Input/UnityInputHost.cs` | `1944e07bc38dbe29d50eb116c05702a1067ed94397f30fc6430cda29ef1db260` |
| `Assets/_Project/Domain/ContainerCommandService.cs` | `dd675c66d888cd0bda5bcfbea5f03ac3005b663d23ce4533c88cba0cf880f256` |
| `Assets/_Project/Domain/IContainerReader.cs` | `1dcc8da7f6691599b372a818b6222ae86bee27db3a1dd88bbda4f4dc17f18d14` |
| `Assets/_Project/Domain/SOLITUDE.Domain.asmdef` | `3c1c40ce9313167aca5938afdc5dac14631fe985dbc0bb51542845650861cc2d` |
| `Assets/_Project/Domain/Survivors.meta` | `bc897e6a2390a1e6757b9574810ed38f85ea18351b12d6de19872cbf79f5ed9a` |
| `Assets/_Project/Domain/Survivors/SurvivorState.cs` | `27fc0e0ff8da97c455528537cd0d8bdb6b0b3279af8a9b5c05789f0d59852b4c` |
| `Assets/_Project/Domain/Survivors/SurvivorState.cs.meta` | `308825d0fbfda5af2a345a6a396d20d44055718ab6cb1b46047905aecaf0b516` |
| `Assets/_Project/Editor/ScrubberEncounterBuilder.cs` | `9e56b6658071411fd6be21c27f262dec45e4364bdabf130996e19805863b8694` |
| `Assets/_Project/Editor/SurvivorSuccessionAuthoring.cs` | `3d8f7ea94442dae3422717516ad153e81beb7cbd210a320176ee507097098804` |
| `Assets/_Project/Editor/SurvivorSuccessionAuthoring.cs.meta` | `7e9ec9765a72b657b7f2460a7cd9e8536f0adc142092ba6607e6310d4685fb1d` |
| `Assets/_Project/Features/Player/Interaction/PlayerInteractor.cs` | `00b073b54c1a4c404184e9bc9d493fc7333ce9a2281cc4fde5b1b38f086de79e` |
| `Assets/_Project/Features/Player/Movement/PlayerMovement.cs` | `3833c86292bbf776b53ca9a57da40f92a47183102c971a2d0563999795cb41d7` |
| `Assets/_Project/Features/Player/Succession.meta` | `3d33bebe14d68a939f262bca35a82022f005f5d604093b6e12aa9c07d0cfa364` |
| `Assets/_Project/Features/Player/Succession/SurvivorAvatarAdapter.cs` | `947d50a8678dbf1aa10ea5a5d83df7de5f853b9e29222b5837f3743107c21c52` |
| `Assets/_Project/Features/Player/Succession/SurvivorAvatarAdapter.cs.meta` | `d9c8c75699e40ca6b3deefca256b0c60c6930570b055a149401eba45016a0201` |
| `Assets/_Project/Features/Player/Succession/SurvivorAwakeningPoint.cs` | `daea862966989015bc471838f80e785a38a63f27e7e952d6e24e1d065d5e7f89` |
| `Assets/_Project/Features/Player/Succession/SurvivorAwakeningPoint.cs.meta` | `02046a2c207c40ec01e656244fdef29bd96d4bd478ccbde0dc11d7c15e898ed2` |
| `Assets/_Project/Features/Player/Succession/SurvivorRecoveryContainer.cs` | `ae12457e38d6a30425b2c81afc3e65200722c8ea185d252a7f999ae35f506723` |
| `Assets/_Project/Features/Player/Succession/SurvivorRecoveryContainer.cs.meta` | `263424a2c4b12f4e6745687e0e36c0da85aebae419de7a55225057fb9b0c4795` |
| `Assets/_Project/Features/Player/Succession/SurvivorRecoveryContainer.prefab` | `dbeb4acac266c6f6fda6bc8e7c3f9c01a221bbd51fe6b24f7bb295ea6d4af176` |
| `Assets/_Project/Features/Player/Succession/SurvivorRecoveryContainer.prefab.meta` | `538712501021bf7cea3caafc21bff6928e4c1afd8e4869179ae311f01614c14f` |
| `Assets/_Project/Features/Player/Succession/SurvivorSuccessionComposition.cs` | `6ea349c608e40951a365a6fe33acdfc84468f3e19b2a58b1d4b77d8d9755d698` |
| `Assets/_Project/Features/Player/Succession/SurvivorSuccessionComposition.cs.meta` | `8d635e410076b4241d17eff0bfddbb46e0b7714fb093e3a2da7d02e5e7b0f33a` |
| `Assets/_Project/Features/Player/Vitals/PlayerVitalController.cs` | `a46aea639c579ce23776583124edb5b95e5586dfa47258cb1b191dcd5c171558` |
| `Assets/_Project/Features/Player/Vitals/Views/VitalBarView.cs` | `0faed626670eb647c16c53009dbaf0fe254846ea9f3eaebb1a27942be738b88d` |
| `Assets/_Project/Features/World/OpeningSlice/OpeningFirstContact.cs` | `ac639ac27c7b0533944d3719c4402b5c950695f21fa5d64cbe013b6cbccb4969` |
| `Assets/_Project/Features/World/ScrubberEncounter/ScrubberComposition.cs` | `edb628f2f282ab0c3b6ea825d09bf9249cdc6e67d637954657b507ef37561e41` |
| `Assets/_Project/SOLITUDE.Runtime.asmdef` | `679bbbb2658cbef340b98e5eef525e496e43298e752733c5ce71050584a45a06` |
| `Assets/_Project/Scenes/Demos/AtmosphericScrubberEncounter.unity` | `df689c643dbb43d450a2eefd015f9f96044424e1dfe78136f0108684f12791df` |
| `Assets/_Project/Scenes/Wakeup.unity` | `6d542c140c55f1455ea2f1a8eb5d33500a56a0d955927eeeec4c3f4689c00159` |
| `Assets/_Project/Systems/Containers/Controller/ContainerController.cs` | `990a7830b508e534e759e5c6b597028a6307edc955b2b373b34409cc44774e39` |
| `Assets/_Project/Systems/Containers/Prefabs/ContainerPanel.prefab` | `c0422a120c7a5103e09757fd2fd1d8b0f37b9bd4f619e8ca569dcd694b240cf0` |
| `Assets/_Project/Systems/Containers/Views/ContainerView.cs` | `f0bced502c1ee0c959578ac8567e28fa696f4008d33899cdb498616d50fe268e` |
| `Assets/_Project/Systems/Interaction/Core/InteractableProximitySensor.cs` | `58c20faeb02cb656ab693a74abac2ab746e2a718fc1ada13f4da3e929289d7a1` |
| `Assets/_Project/Systems/Interaction/Core/SceneInteractionScope.cs` | `14e99cb5c78ccb7b021fdae61b5df1e6471802ffe6bbec7d20616c12d3a435a0` |
| `Assets/_Project/Systems/Interaction/UI/Presenters/InteractionFeedbackService.cs` | `0b9c9a16d3cd0a1b0e2ce0106a1589f524d9bdb82c06bfe94c1e175a1e0243f3` |
| `Assets/_Project/Systems/Interaction/UI/Views/InteractionFeedbackView.cs` | `9b18a4e03735d5279c8635bf71d5254a163d36a7e58f47a29115a58b2f6f08c9` |
| `Assets/_Project/Tests/Application/SurvivorSuccessionCoordinatorTests.cs` | `16617077776cbb451021ccb9319d4710cb55d621e16f434f0ae740ad12318ac9` |
| `Assets/_Project/Tests/Application/SurvivorSuccessionCoordinatorTests.cs.meta` | `e045bde2b5568496bc3095cd39dba5e6a2a8a9caed0ef7dd51ca5e88804e697f` |
| `Assets/_Project/Tests/Domain/SurvivorEquipmentHandoffTests.cs` | `6d6fc975809ff34b3678ea17f42236626b68671cbe589679f5fe9c823c9140f4` |
| `Assets/_Project/Tests/Domain/SurvivorEquipmentHandoffTests.cs.meta` | `afd98f596deafce908acff66384d5c13b0bbc96cda217372874adbcb5c32b36d` |
| `Assets/_Project/Tests/Domain/SurvivorStateTests.cs` | `3da5bd4a7218dde7d3a614f1c8c399739d304bb80f3f025f52b601527be79793` |
| `Assets/_Project/Tests/Domain/SurvivorStateTests.cs.meta` | `7475fc06311de9b5132c31efdf940eaff5fbdcaa75a8d9ca923bb497eadd4da3` |
| `Assets/_Project/Tests/Editor/ScrubberEncounterAuthoringTests.cs` | `a430c7cc7075aa1caee01773780a7dd9816fe036bbf929158d2cd0a5f4a99d06` |
| `Assets/_Project/Tests/Editor/SurvivorSuccessionAuthoringTests.cs` | `c98e51921659b86a7f2f0b3884b62ba7644df80144271c26ce56f90a63da6ff8` |
| `Assets/_Project/Tests/Editor/SurvivorSuccessionAuthoringTests.cs.meta` | `e8d9d9d29196e584d81d9143e7e63d9d44c4c16a6001b1ef939a8b9c811f044e` |
| `Assets/_Project/Tests/PlayMode/ScrubberEncounterLifecycleTests.cs` | `6c18ddfbef522bbfa167be77da04189d73e5c9f67b98fb7464c93d7e01951ca5` |
| `Assets/_Project/Tests/PlayMode/SurvivorRecoveryInteractionTests.cs` | `02dc4258e07cd8f2ed577c4e706a1b2ddfa542535c5e3d78f83762fdb9baf0e9` |
| `Assets/_Project/Tests/PlayMode/SurvivorRecoveryInteractionTests.cs.meta` | `3337245bf40fed2cf84d90a418321a9dd3d203fdf6c6ddc7a5fa08b63ef14128` |
| `Assets/_Project/Tests/PlayMode/SurvivorSuccessionFailureTests.cs` | `766e0f2a23c0f862b13c5c51f1a1b5a75d2f2b50537a231ff189d54c323b3405` |
| `Assets/_Project/Tests/PlayMode/SurvivorSuccessionFailureTests.cs.meta` | `ef491cf9eff187dd220e68be60aecf863c0b3f786055fa3162078e0b8537a9c0` |
| `Assets/_Project/Tests/PlayMode/SurvivorSuccessionShipCompatibilityTests.cs` | `a983f111ff98be9316486fda511c1d3c2988e9f31597bdc5b9c0a750c141ca44` |
| `Assets/_Project/Tests/PlayMode/SurvivorSuccessionShipCompatibilityTests.cs.meta` | `acc2cd3fa0ab3307caa3f173fe5fe7402d915e98f8e414d8f4a9e5abb392229e` |
| `Assets/_Project/Tests/PlayMode/SurvivorSuccessionSliceTests.cs` | `820a448189f167ac9476e2ae9f8cecec1678a0402f49de12efb0b0712649337d` |
| `Assets/_Project/Tests/PlayMode/SurvivorSuccessionSliceTests.cs.meta` | `8bbb829c6a75f40eb090956038cfe0fb9d8c4a4e403da7c5b647a5e8691ab063` |
| `Assets/_Project/Tests/PlayMode/VitalBarReadinessTests.cs` | `ef837ec4b3a344da3c88a2367c02d7300932efc12b5d3474b8b6b381848629a7` |
| `Assets/_Project/Tests/PlayMode/VitalBarReadinessTests.cs.meta` | `257415bb9552556d107f9d7ac512784b9df16e9e0dbdb528a1a00772fa7c47e9` |
| `README.md` | `564b3e6fd0eaa012bf1a086bfb9813a8a3d1a2da2d5816373e23cfc5dcf351fc` |
| `Tools/Verification/README.md` | `01c8cead26b92cb314f0daeb35f337814acf7bf32d958a5609ca1ff941ab4320` |
| `Tools/Verification/verify.py` | `db83b85848a3b467565655cbd4c3c04fde15f378666f616fe0624d46f32f9f20` |
