## Verdict: FAIL

Co-located recovery containers cannot reliably be selected independently through ordinary interaction; an emptied container retains focus and hides another survivor's remaining equipment (F1 / S5, S9).

## Scope

Final review of all three phases and the complete S1–S12 spec, independently of prior implementation reports. Exact command run from `/Users/jeremyvalentine/SOLITUDE`:

```sh
git diff 4de49500a28f6506f18eb25eda65437cf4e6715c 69afe43795ee8510dff48b745c70826e0bdc3d5c -- . ':(exclude).qrspi'
```

Snapshot: resolved base `4de49500a28f6506f18eb25eda65437cf4e6715c`; HEAD `69afe43795ee8510dff48b745c70826e0bdc3d5c`; exact product diff SHA-256 `dcc175d9a81bae0c2de2255d30dd9a17f88cdf324567de8c33e446b1385bca5d`. QRSPI hunks are excluded explicitly.

Spec: `.qrspi/survivor-succession/spec.md`, exact-byte SHA-256 `f2253ab3429d7c5a34c8e9275f9efac61be9e3f4c30781393ad7d86e68ba85a4`.

Approved normalized plan digests independently matched:

- `.qrspi/survivor-succession/plan.md`: `72abe1bd78455e5149be550338216d4dc513993736ca0c9f1b84014c09b5c827`
- `.qrspi/survivor-succession/plans/plan-phase-1.md`: `1721ef5a35512f2703c61c3ff82e7bf3f59f607a411549dca59f16fe8f86e952`
- `.qrspi/survivor-succession/plans/plan-phase-2.md`: `9174ac5760d00bbd53f98cbbed9c5a5cd06a6b62cca9f59b5bbbb5f5a8b9054c`
- `.qrspi/survivor-succession/plans/plan-phase-3.md`: `3791ec5801967555ce2f52345d742a4a609b5147d944d38b5a0ebb43275b5d22`

Normalization replaces only status tokens on `Status marker` lines and overview phase-table Status cells with `[ ]`. The plan overview's spec provenance matched. The caller supplied validated upstream provenance; request, queries, research, approach, implementation verification and explanation artifacts were not read.

Working tree/staged status: clean before/after scope capture, during inspection, and immediately before publication; no untracked product files. `git check-ignore .qrspi/` confirmed exclusion. All source inspected was working-tree content matching the specified HEAD. Full changed C# files, metadata and prefabs were inspected; full scene bytes were parsed into serialized-object structure (Wakeup: 277 unique records; scrubber: 326 unique records) and the complete feature/player binding blocks were inspected. No product files were modified.

Source files read: the table below records full changed-file context and unchanged dependencies. `final-source.json` additionally records 6,860 tracked Assets/Packages/ProjectSettings/verification-tool input fingerprints and machine-output hashes. Sidecar SHA-256: `6406f6d0b589b61b70bbf921c9e6cd471f717ef4bad6aa93d1305c01099f41a2`.

Snapshot verified unchanged: HEAD, status, exact diff, spec, normalized plans, every source digest and all qualification-input fingerprints matched immediately before publication. Disposable Unity source copies also matched every inspected Assets file after the authoring test suite; no authoring-test drift was present. No implementation-route divergence from the approved scope was identified.

| Source path (working tree / HEAD) | SHA-256 |
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
| `Assets/_Project/Domain/ContainerSaveSession.cs` | `8a0c8a90a3273ce0a5861f6371da2d75a1475be97017ef715a979bcf6e8d8981` |
| `Assets/_Project/Domain/IContainerReader.cs` | `1dcc8da7f6691599b372a818b6222ae86bee27db3a1dd88bbda4f4dc17f18d14` |
| `Assets/_Project/Domain/README.md` | `d3fbdce7507f3be23eb68873742aa8dc57c5fba0f6c61da0c2075f28858ebe50` |
| `Assets/_Project/Domain/SOLITUDE.Domain.asmdef` | `3c1c40ce9313167aca5938afdc5dac14631fe985dbc0bb51542845650861cc2d` |
| `Assets/_Project/Domain/Survivors.meta` | `bc897e6a2390a1e6757b9574810ed38f85ea18351b12d6de19872cbf79f5ed9a` |
| `Assets/_Project/Domain/Survivors/SurvivorState.cs` | `27fc0e0ff8da97c455528537cd0d8bdb6b0b3279af8a9b5c05789f0d59852b4c` |
| `Assets/_Project/Domain/Survivors/SurvivorState.cs.meta` | `308825d0fbfda5af2a345a6a396d20d44055718ab6cb1b46047905aecaf0b516` |
| `Assets/_Project/Editor/README.md` | `915af08ca58838ecfdceb281009acf266f0e5ead2721b3d1b6fb340c1433e381` |
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
| `Assets/_Project/Features/Player/Succession/SurvivorRecoveryContainer.cs` | `345501483c5a584c6b24acbbe0befb232dd2df8b78b2d173550c09cb63914c95` |
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
| `Assets/_Project/Systems/Containers/Prefabs/ContainerPanel.prefab` | `c0422a120c7a5103e09757fd2fd1d8b0f37b9bd4f619e8ca569dcd694b240cf0` |
| `Assets/_Project/Systems/Containers/Views/ContainerTransferModalView.cs` | `37392ce772f007e2dd37ae732664aea578cb3f3827a7417907cc8005ddd8e222` |
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
| `Assets/_Project/Tests/PlayMode/SurvivorRecoveryInteractionTests.cs` | `3d8be8dc158f7591eba3f4d52a8c908598bca9c9937ddcce066e70c12fa44002` |
| `Assets/_Project/Tests/PlayMode/SurvivorRecoveryInteractionTests.cs.meta` | `3337245bf40fed2cf84d90a418321a9dd3d203fdf6c6ddc7a5fa08b63ef14128` |
| `Assets/_Project/Tests/PlayMode/SurvivorSuccessionFailureTests.cs` | `766e0f2a23c0f862b13c5c51f1a1b5a75d2f2b50537a231ff189d54c323b3405` |
| `Assets/_Project/Tests/PlayMode/SurvivorSuccessionFailureTests.cs.meta` | `ef491cf9eff187dd220e68be60aecf863c0b3f786055fa3162078e0b8537a9c0` |
| `Assets/_Project/Tests/PlayMode/SurvivorSuccessionShipCompatibilityTests.cs` | `12225ab0514ac30198e3df0b1c018c9c9b531bed6c7c48169a24b96ae05cd7cb` |
| `Assets/_Project/Tests/PlayMode/SurvivorSuccessionShipCompatibilityTests.cs.meta` | `acc2cd3fa0ab3307caa3f173fe5fe7402d915e98f8e414d8f4a9e5abb392229e` |
| `Assets/_Project/Tests/PlayMode/SurvivorSuccessionSliceTests.cs` | `820a448189f167ac9476e2ae9f8cecec1678a0402f49de12efb0b0712649337d` |
| `Assets/_Project/Tests/PlayMode/SurvivorSuccessionSliceTests.cs.meta` | `8bbb829c6a75f40eb090956038cfe0fb9d8c4a4e403da7c5b647a5e8691ab063` |
| `Assets/_Project/Tests/PlayMode/VitalBarReadinessTests.cs` | `ef837ec4b3a344da3c88a2367c02d7300932efc12b5d3474b8b6b381848629a7` |
| `Assets/_Project/Tests/PlayMode/VitalBarReadinessTests.cs.meta` | `257415bb9552556d107f9d7ac512784b9df16e9e0dbdb528a1a00772fa7c47e9` |
| `ProjectSettings/ProjectVersion.txt` | `338824f489d7ef647583e723283307ae92b3bb241d2fe874ac759dbf2f091701` |
| `README.md` | `564b3e6fd0eaa012bf1a086bfb9813a8a3d1a2da2d5816373e23cfc5dcf351fc` |
| `Tools/Verification/README.md` | `01c8cead26b92cb314f0daeb35f337814acf7bf32d958a5609ca1ff941ab4320` |
| `Tools/Verification/verify.py` | `db83b85848a3b467565655cbd4c3c04fde15f378666f616fe0624d46f32f9f20` |

## Findings

| Severity | Blocking | Category | Location | Description | Suggested Fix |
| --- | --- | --- | --- | --- | --- |
| HIGH | yes | Spec conformance | `Assets/_Project/Features/Player/Succession/SurvivorRecoveryContainer.cs:79` | F1 / S5, S9: Two equipped deaths at the same position leave identical overlapping targets. After ordinary UI recovery empties the focused cache, it remains active and interactable, so the next ordinary interaction reopens the empty cache while the other retains items. There is no selection/cycling or exhaustion handling to make the remaining owner independently accessible. | Give overlapping recovery owners a deterministic accessible selection path, and handle exhausted targets so they do not monopolize focus. Preserve independent ownership and recorded death locations. Add an actual-interaction regression covering two same-position deaths and recovery from both without manually releasing a cache. |

## Evidence

### F1 / S5, S9

- Trigger: In authored Wakeup, grant A three battery items and retire A without moving from the awakening point; grant B five items and retire B at the same position; C opens the naturally focused recovery target, collects all its items through the existing UI handlers, closes, then interacts again.
- Expected: S5 requires ordinary interaction recovery, and S9 requires independent containers from successive deaths to remain recoverable. Emptying one cache must allow access to the other's contents.
- Actual: Both deaths completed; A/B became Dead and C Active. Two owners existed at `(-0.89, -2.40, 0.00)` with quantities `[3, 5]`. Focus selected A. Existing begin-drag/drop/end-drag handlers transferred its three items, yielding `[0, 5]` with both targets still active. After the next normal update, focus was still A; `CanInteract` was true and the ordinary interaction entry point reopened A with quantity zero while B retained five.
- Complete source path: `SurvivorSuccessionComposition.cs:83–88` captures the actual body position and prepares every cache there; `SurvivorRecoveryContainer.cs:53–58` gives each instance independent ownership at that exact position. The prefab gives both the same centered 1.25-radius sensor. `InteractableProximitySensor.cs:28–35` registers both. `PlayerInteractor.cs:131–152` chooses only the first strictly closer target; equal positions stay tied at every player position and contents do not affect focus. `SurvivorRecoveryContainer.cs:79–83` allows empty published targets to open; no slot-change/exhaustion callback disables or unregisters them. Modal close only closes presentation. Neither request guards nor transfer validation prevent two legitimate same-position deaths or resolve the focus tie.
- Independent live reproduction: `Logs/SurvivorSuccession/final-independent-review/manual-overlap-create.runner.log`, `manual-overlap-focus.runner.log`, `manual-overlap-transfer.runner.log`, and `manual-overlap-reopen.runner.log` contain exact evaluated code, parameters, successful command results and the quantities above. Executed only against `/private/tmp/solitude-succession-final-review`, with a disposable save directory. The interaction call was the production `PlayerInteractor.TryInteract` entry point; transfers used actual `ExecuteEvents` drag/drop handlers. No cache was manually released or disabled to obtain the failure.
- Coverage: The passing repeated-death journey deliberately uses different death positions. The two seeded targets in `RecoveryCleanupPreservesOtherOwnersAndLockerLazyLoot` are tested with explicit `cache.Release`, not exhaustion through ordinary recovery. The existing suite therefore does not detect this case. This is a demonstrated behavioral divergence, not a defect inferred from absent tests.

### Independent qualification and positive evidence

Executed from the repository root:

```sh
python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-succession-final-review --output Logs/SurvivorSuccession/final-independent-review
```

Actual outcome: exit 0; `results.json` status `Passed`. Pure Domain/Application suite: 121 passed, zero failed. Unity EditMode: 221 passed, zero failed. Unity PlayMode: 70 passed, zero failed. Authoring validation reported `success: true` and no findings. Both native ARM64 verification/shipping builds and every collection/restart/new-save, file-corruption, incompatibility, denied-read, durable-write interruption and backup-recovery interruption scenario completed successfully. This was the complete gate, without `--skip-build`; machine outputs were produced by this reviewer and source fingerprints remained stable.

Criterion-linked executed fixtures include distinct/stale/reentrant lifecycle requests and forward failure retry (S2/S9/S10); atomic mixed/max-capacity handoff, stale revisions/owners and observer failure (S4/S10); actual proximity/modal/UI recovery with capacity failure and scrolling to the final slot (S5); held movement/open journal/inventory/transfer, old gesture callbacks and held pointer suppression (S3/S7); unchanged restored systems, terminals/door and subsequent repair/traversal, scrubber progress/readers, lockers and pickup collection facts (S6/S11); first-contact preservation and visible acknowledgment deduplication (S8); delayed HUD binding and retained vital objects (S3/S7); and existing production save/recovery regressions (S12).

Manual visual observation in the same source-matching disposable Editor: `manual-awakening.png` shows the retained avatar beside the authored cryopod with full HP/O2 and empty hotbar. `manual-succession.png` shows the recovery prompt and visible AI acknowledgment. `manual-recovery-ui.png` shows the ordinary transfer screen labeled “Recovered equipment” and “Inventory”, retained HUD and AI feedback. Screenshots were inspected and retained outside product Assets. The generic `manual-overlap-empty.png` screenshot omits overlay UI and is not evidence of modal contents; the live reader/modal result logs establish those quantities.

Limits: Debug grants supplied reproduction equipment; the live interaction and UI events were invoked through the CLI rather than physical keyboard input. Normal held/fresh movement, production-input interaction and powered-door traversal are established by the independently executed PlayMode fixtures. No durable survivor/cache persistence, scene-transition continuity or non-macOS target qualification is claimed. A transient CLI connection failure during a later Play Mode reload resolved before visual UI capture; it did not affect the recorded reproduction or the completed test/build gate.

Prior review: `.qrspi/survivor-succession/reviews/phase-1.md` was read only after independent inspection. Its missing positive post-awakening interaction coverage is now addressed by the executed recovery and ship-compatibility interaction journeys. Its prior verdict was not reused as current evidence.

## Open Questions

None blocking this completed review.

## Supplemental Reviews

None provided.

## Spec Conformance

PRESENT S1 — Authored Wakeup/scrubber references validate; first identity is inspectably Active at the authored point. Independently executed authoring/slice fixtures and live position/status inspection agree; cryobay placement was observed visually.

PRESENT S2 — Identity-bound requests reject stale/reentrant calls; retirement is terminal and B uses a distinct identity. Coordinator Domain/Application fixtures and repeated composed death tests passed.

PRESENT S3 — Both carried readers are emptied atomically; existing vitals reset to authored baseline, selection resets to -1, and transient screens/gestures/input are canceled. Executed slice/failure/readiness fixtures establish this without replacing vital subscriptions.

PRESENT S4 — Both source containers are committed together into a unique independently registered recovery owner at the captured body position, preserving exact immutable stacks/quantities. Mixed/full sources and live `[3,5]` independent owners establish conservation.

DIVERGED S5 — Ordinary single-cache and capacity/scrolling behavior passes, but an empty co-located cache monopolizes ordinary focus and prevents access to another cache's remaining contents (F1).

PRESENT S6 — Succession retains scene, root, readers, repaired system state, powered/open door state, scrubber progress, locker contents and collected pickup facts. Executed compatibility fixtures repair/traverse afterward; source performs no scene reload or session/save reset.

PRESENT S7 — Retained avatar/input/camera/vital resources survive reset; held controls require release, old UI intents are rejected and ordinary focus/interaction resumes. Executed input/gesture/camera/traversal fixtures and live recovery interaction establish this; the multiple-cache accessibility divergence is recorded under S5/S9.

PRESENT S8 — Successful transitions deduplicate visible feedback by transition identity, preserve first-contact scene state, and failed/stale requests do not issue new success messages. Executed first-contact/uncertain-acknowledgment tests and live screenshot establish delivery.

DIVERGED S9 — Identity retirement and independent conserved owners across repeated/empty deaths pass, but same-position recovery owners cannot be accessed independently through ordinary interaction after the selected owner is exhausted (F1).

PRESENT S10 — Invalid/missing authoring and precommit failure preserve active survivor/equipment; committed activation interruption leaves gameplay gated with the same pending successor/receipt and forward retry skips handoff. Executed pure and composed fault fixtures passed.

PRESENT S11 — Domain/Application asmdefs retain noEngineReferences and narrow ports; ship runtime state contains no survivor/choice dependency. Existing player reader identities and scene bindings remain intact; executed assembly/authoring/ship fixtures passed.

PRESENT S12 — The complete independently run macOS ARM64/Wakeup gate passed, including existing container/pickup/locker/restoration/scrubber/door and production persistence/recovery coverage. Passing regressions do not waive F1.
