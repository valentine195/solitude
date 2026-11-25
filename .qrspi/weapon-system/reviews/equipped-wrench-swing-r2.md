## Verdict: PASS

No actionable defects were established in the Phase 1 scope; the prior S15 successor-lockout finding is resolved by post-commit actor retirement.

## Scope

Reviewed all four completed Phase 1 steps and the approved succession repair: tag registry/rules, additive item capabilities, selected-hotbar equipment, actor-owned timed dry swings, movement/presentation cleanup, and real Primary input. Later damage/SEC-0, loadout/save, final encounter/performance/documentation work is excluded.

Exact diff command run from /Users/jeremyvalentine/SOLITUDE:
```sh
git diff 335c87a8d9e55dfbdd8f7b0ad3ab7e73952af306 -- $(git diff --name-only 38395596d28c5d063270d0276b2ce9f412a04b20 d303c994473075d4168c506afeb131a83deb9f66)
```

Snapshot:
- Default branch independently resolved: refs/remotes/origin/main.
- Resolved default-branch merge base: 335c87a8d9e55dfbdd8f7b0ad3ab7e73952af306.
- Fixed path-selection range: 38395596d28c5d063270d0276b2ce9f412a04b20..d303c994473075d4168c506afeb131a83deb9f66.
- HEAD: d303c994473075d4168c506afeb131a83deb9f66.
- Exact diff SHA-256: 06c1bcdf5a754531f9bc92bfb955b2f9e0500b76da0fc322b5716058c140b751.
- No .qrspi/ hunks occur in this selected product diff; workflow scaffolding is excluded from source/status scope.
- Spec: .qrspi/weapon-system/spec.md — SHA-256 78d09ac0e72bb413355cffdab28a21a5e501e69138731c6ad1f0863ff63a74f5.
- Plan overview: .qrspi/weapon-system/plan.md — normalized SHA-256 d825b24d16443e871862dd17eb230f025b3a4416f7e34469b3e3b522d87bd8dc.
- Phase plan: .qrspi/weapon-system/plans/plan-phase-1.md — normalized SHA-256 5d93d6546db58f9a493041c2a57e1ac61c37eb15549f62da1814ba3ad7b4360e.
- Normalization changes only status markers on status-marker lines and numbered phase-row status cells to [ ]; all other bytes are preserved.
- Upstream provenance: caller supplied successful digest-only checks; upstream request/query/research/approach contents were not read.
- Prior review evidence: .qrspi/weapon-system/reviews/equipped-wrench-swing.md; full current scope independently inspected.
- Output collision check: assigned report did not exist when review began.
- Working tree/staged status before and after diff: one tracked unstaged change outside selected paths, Assets/_Project/Features/World/SolitudeStart/Scenes/SOLITUDE_Start.unity. Preserved and not read. No staged or untracked product source.
- Source files read: full selected-file context, generated configuration/metadata and unchanged dependencies are fingerprinted below; all are working-tree origin.
- Snapshot verified unchanged immediately before verdict publication: HEAD, exact product diff, spec, normalized plans, all 132 listed source digests and product status.

No spec-reaching implementation deviation from the approved phase scope requires a separate note.

## Findings

| Severity | Blocking | Category | Location | Description | Suggested Fix |
| --- | --- | --- | --- | --- | --- |
| — | — | — | — | None established. | — |

## Evidence

### Prior F1 / S15 — resolved

- The previous failure was inherited committed lockout when the same Unity avatar awakened a successor.
- Current PlayerItemActionComposition.RetireActor disposes the old runner and creates a fresh runner while retaining scene input/equipment bindings. ItemActionRunner.Dispose clears execution and timers; its ordinary Cancel still preserves lockout.
- SurvivorSuccessionCoordinator.Run commits equipment, retires the departing survivor and clears Current before invoking TryActivate. SurvivorSuccessionComposition.TryActivate now invokes RetireActor before avatar awakening. Rejection in preparation/commit does not reach this call. Stale survivor requests return before Run. Post-commit retry stays gated and cannot admit successor gameplay before activation finishes.
- PlayerItemActionComposition.Release/OnDisable, equipment changes and policy invalidation retain their existing cleanup routes. The fresh runner is not shared through an action asset.
- EquippedWrenchSwingTests.CommittedSuccessionDiscardsDepartedLockoutButRejectedRetirementPreservesIt covers windup/active/recovery with stopped frame-driven gameplay time, a rejected pre-commit transition, actual recovery-container transfer of the wrench, held-input rejection, fresh real Primary acceptance before old commitment expires and stale-retirement rejection. This independently inspected test source targets the original causal defect; it is not a claim that this review executed Unity tests.

### Independent checks and limits

- Ran the supplied exact diff command and SHA-256 comparison: matched the envelope.
- Ran git status --short --untracked-files=all before/after scoped inspection: unchanged unrelated scene edit only.
- Independently resolved default branch/merge base and HEAD: matched the stated commits.
- Independently hashed spec and normalized overview/phase plan: all matched approved revisions, then matched again before publication.
- Parsed the generated InputActionAsset.FromJson payload and authored .inputactions JSON: complete objects equal.
- Compared existing map/action identities, all original action properties and binding objects against the review base: preserved. An initial strict object-equality assertion failed because regenerated old actions add priority:0; inspected those differences and verified that this is the only added old-action property. Primary binds mouse left/gamepad right trigger; Use/Interact preserve their original paths.
- Independently checked all 57 selected metadata files for a 32-digit hexadecimal GUID and an existing paired asset/folder: passed. Read their full content and the authored wrench/tag assets; visually inspected the placeholder PNG.
- Source trace checked each review category: tag ownership/directionality and authoring rejection; immutable capability snapshots; selected-slot registration/revisions; generic runner and reentrant cleanup; configured timing/coarse crossings; policy/lifecycle gates; facing/speed leases; optional presentation; deferred UI-aware input ownership; and succession commit ordering.
- No builds, pure suites, Unity suites, gameplay capture, allocation measurements or complete Wakeup gate were run by this reviewer. The verdict establishes reviewed Phase 1 source conformance, not runtime/visual/performance qualification. Test source is not evidence of execution. No implementer reports, logs, explanations or claimed test results were read.
- Only the assigned review artifact was written. No product files were changed.

## Open Questions

None preventing this source-based Phase 1 verdict. Final Unity visual, complete regression/build and allocation qualification remain assigned to later phases.

## Supplemental Reviews

None provided.

## Spec Conformance

PRESENT below concerns the delivered Phase 1 responsibility and independently inspected source/test design. It does not certify unexecuted suites.

- PRESENT S1 — GameplayTagDefinition stores stable serialized identity separately from segment/name; assignments are object references. Registry runtime tags resolve by identity. Authoring round-trip/move/rename test checks preserved identity and reference.
- PRESENT S2 — pure/Unity registry validation rejects missing/unregistered definitions/parents, duplicate identities/names/entries, malformed segments, self-parenting and cycles. Set compilation rejects duplicate/null/foreign assignments. Database compilation rejects invalid action slots/configuration before root readiness.
- PRESENT S3 — authored wrench holds all six tags and registry/set compilation supports them. Untagged damage-recipient behavior is NOT IN SCOPE until Phase 2.
- PRESENT S4 — exact membership and precompiled owned ancestors implement the matching table in the required direction.
- PRESENT S5 — required/all, any-of/nonempty and excluded/none loops compose explicitly; conflicts fail without action selection.
- PRESENT S6 — direct empty HasAll/HasAny/HasNone and whole-query empty semantics match the tables; full reference validation rejects invalid entries before predicates short-circuit.
- PRESENT S7 — additive item fields preserve legacy metadata/consumables. Runtime database references the nonstackable wrench, six authored tags, Primary action and stated default numeric configuration.
- PRESENT S8 — generic request/execution interfaces and runner contain no item-ID/tag dispatch. Execution owns timing and leases; compiled melee specifications copy authoring values. Shared-actor, alternate configuration and non-melee test execution source cover reuse.
- PRESENT S9 — EquippedItemView exclusively derives equipment from selected hotbar reader, current owner registration, slot address and revision. Empty/actionless selections reject requests; existing presentation route resolves wrench icon/highlight, with composed fixture assertions.
- PRESENT S10 — authored/generated input equality checked. Primary is a Button on required mouse/gamepad controls; existing Use/Interact bindings remain. Host defers Primary to LateUpdate, checks UI consumption/raycast, retains held controls across resets/rebinds and releases scene ownership.
- PRESENT S11 — actor checks require active component, live vitals and no stun. Runner cancels invalid actors even during policy freeze; gameplay pause/screens freeze clock/evaluation; readiness transition cancels. Runtime test source exercises these paths.
- PRESENT S12 — configured timing defines .12/.22/.55 boundaries; only Timing.Advance crossing/processing Active invokes the melee active port, independently of view animation.
- PRESENT S13 — coarse advance invokes active evaluation before completion; timing/runtime tests include .55 crossing and subdivision. Real static-target hit equivalence awaits Phase 2.
- PRESENT S14 — Cancel releases execution while retaining lockoutUntil; same-actor selection cancellation cannot reset commitment. Held input requires a new press.
- PRESENT S15 — current binding revision/generation checks reject stale equipment; disable/death/stun/readiness invalidation cancels and cleanup disposes owned resources. Prior F1 resolved: committed succession retires/disposes old runner before awakening and creates an empty runner. Failed pre-commit succession does not retire timers.
- PRESENT S16 — MovementBehavior captures normalized last nonzero direction with initial down; ItemActionContext captures it per swing. Facing override holds animation direction; generic minimum speed factors compose with full movement blocks and dispose independently. Actual-scene visual qualification remains later work.
- NOT IN SCOPE S17 — collider geometry/range/sector hit evaluation is Phase 2.
- NOT IN SCOPE S18 — cover, trigger filtering and saturated query handling are Phase 2.
- NOT IN SCOPE S19 — multi-recipient damage/deduplication is Phase 2.
- NOT IN SCOPE S20 — typed damage delivery and recipient eligibility are Phase 2.
- NOT IN SCOPE S21 — authoritative player damage adapter and rejected-hit feedback are Phase 2.
- NOT IN SCOPE S22 — SEC-0 incoming health/defeat is Phase 2.
- NOT IN SCOPE S23 — successful-hit feedback/knockback/audio is Phase 2.
- PRESENT S24 — Phase 1 nullable view/held sprite do not break execution; configuration compilation validates required numeric/action/tag fields. Hit/audio/knockback portions await Phase 2.
- NOT IN SCOPE S25 — production starting loadout is Phase 3.
- NOT IN SCOPE S26 — save v4 initialization/migration is Phase 3.
- NOT IN SCOPE S27 — production loadout resume/arrival/succession persistence is Phase 3; actor runner retirement itself is reviewed under S15.
- NOT IN SCOPE S28 — authored SEC-0 full journey is Phase 4 after damage integration.
- NOT IN SCOPE S29 — final complete regression/build/Wakeup qualification is Phase 4. Phase 1 test source and isolated verification tooling were inspected without inferring passed runs.
- PRESENT S30 — selected assets include metadata and actual catalog/runtime inclusion; existing item/input identities are preserved; Domain/Application new code is engine-independent and existing assembly boundaries remain. Shipping entry list is unchanged in selected paths. Final authoring/build validation awaits Phase 4.
- NOT IN SCOPE S31 — repeated-execution allocation measurement and final bounded physics coverage are Phase 4. Tag predicate hot paths use compiled sets and indexed loops.
- NOT IN SCOPE S32 — final architecture/Unity validation documentation is Phase 4.

### Source files read and SHA-256

All rows are working-tree bytes; includes unchanged dependencies and full changed-file context.

| Path | SHA-256 |
| --- | --- |
| .gitignore | 06191a79d474f31f919a009fb77d8322af037038e8b398f387ecafe930dcc1b1 |
| AGENTS.md | e7ad845d1544a988a490a56e13d6127263794a28efd3559f8fbe8fa32d0ab7af |
| Assets/_Project/Application/Items.meta | 7db93e43f38d5eb07e7f0ed2a54cc31ba45d653cd081e31221ea9f035902605c |
| Assets/_Project/Application/Items/EquippedItemView.cs | b5957723fe4656cad09504e1190e4c91cd42845662572f2ea707eb3ee3f64159 |
| Assets/_Project/Application/Items/EquippedItemView.cs.meta | cefc9b9c04d9e1bc6494c3d95a4fd6ee871f1954a76146ee8719d81ec617111a |
| Assets/_Project/Application/Items/ItemActionRunner.cs | 702e350455bb3a226fb8c8476bae6bf08a996574fda84832445bc3e5e0492148 |
| Assets/_Project/Application/Items/ItemActionRunner.cs.meta | 8c22e949e7af35ab9dd9b292ac73a78a1c7eb3314c1a6b16319c8b8781bf3b74 |
| Assets/_Project/Application/RuntimePolicy.cs | 3e1ac34b6c013fad11b50cb5b759584845c25cc18e1378c73981cd65329916a6 |
| Assets/_Project/Application/Survivors/SurvivorSuccessionCoordinator.cs | 3b8ef879895941dca7d64f1b888164ad8de91dc962a14a90609ba17e15f0cce9 |
| Assets/_Project/ARCHITECTURE.md | 855391ba68cdf06e7895883dca350a6c0e8bbe8b7589fa169bffafe3ff0f3c8e |
| Assets/_Project/Core/Animation/Human/HumanAnimationController.cs | 1188ec2fcba059fb0b6d53a35116bd52d7eae7d27891c16112604d96742ffd97 |
| Assets/_Project/Core/Composition/GameCompositionRoot.cs | e77a8f5e4a7e2df09ef3659bbd7cfe95ea4f44124a26c868ab782bfa5aab7c73 |
| Assets/_Project/Core/Composition/README.md | 0c748deea4aa6bc0bd88b3c6b59a31da038849fc241a171508ec5a968f79d311 |
| Assets/_Project/Core/Composition/SceneBindings.cs | cd90d4cb88e10558bc0142452c59685ec7730e621eb33e7d0428f252dad43f3b |
| Assets/_Project/Core/Composition/SceneFeatureContext.cs | 1bb999a33f240c0b9321f98cbf36cc629fa6ff90f2f3e60ea1ff32034070a73c |
| Assets/_Project/Core/Input/SOLITUDE_InputActions.cs | be596c286e36ffeb22182427bc88d3ab2d1d56b93a66303f7c64f45b1fea94ac |
| Assets/_Project/Core/Input/SOLITUDE_InputActions.inputactions | 8b6a7ee67d6857693a932d1a145d05953df22fc3ada667cbc0f2a1f7c804b4bd |
| Assets/_Project/Core/Input/UnityInputHost.cs | 74bfd2c6a4ceda9e8b6eacd85deaae3962ab7cebbfe7b30d7a69a844dc5bd9d7 |
| Assets/_Project/Domain/GameplayTags.meta | 1e87e92b1a66388ac856d4a97c493995ec799eb3439b74618a3e2d4d27ea4b18 |
| Assets/_Project/Domain/GameplayTags/GameplayTagQuery.cs | d26f4e976acae40f4934d856b5d18dca579b7129882832b926d505cf9c4ebced |
| Assets/_Project/Domain/GameplayTags/GameplayTagQuery.cs.meta | e6ebd65f6252c1440a44b4619004ffd102ea1f96a90a39b8677beae9048f3016 |
| Assets/_Project/Domain/GameplayTags/GameplayTagRegistry.cs | 62d8afd8e2d36cae565ec99050e754db6bbd23e40287d03da687c86729044b19 |
| Assets/_Project/Domain/GameplayTags/GameplayTagRegistry.cs.meta | 6e13aaee1cd2a5d42d85808fa4795655ca809d83131395a20d1b73c67d7f9d36 |
| Assets/_Project/Domain/GameplayTags/GameplayTagSet.cs | 84d96f6f7ddd2acb10a4d6684f9bc0a60d41f90d231ed1bb23ae1b5bc849c1a0 |
| Assets/_Project/Domain/GameplayTags/GameplayTagSet.cs.meta | 58dcfde3fd69dcc901a76f943c6340648603c214d6045f3e234c537622a98336 |
| Assets/_Project/Domain/ItemActions.meta | 1ac5733531404188a2da21ed324a529d2c7cbe8ac6b2151e16ef08ef2f55498f |
| Assets/_Project/Domain/ItemActions/ItemActionContracts.cs | 925b684ec0a1ed161f083ba9fc377023f71bb663c7da90fb83672b4557819dcd |
| Assets/_Project/Domain/ItemActions/ItemActionContracts.cs.meta | ff56cc07ac436dd174dad37fcb197368fb29f58c3f16d7b026fe022a850f1fb6 |
| Assets/_Project/Domain/ItemActions/MeleeAttackTiming.cs | 00200497e037fa69b874d034098730cec65b733357a1bbe6316605435a8a37dc |
| Assets/_Project/Domain/ItemActions/MeleeAttackTiming.cs.meta | 8eeb555b00b2339207dd9bd89fa55a197935010c7473016baf3dcfe39045fd77 |
| Assets/_Project/Domain/README.md | d3fbdce7507f3be23eb68873742aa8dc57c5fba0f6c61da0c2075f28858ebe50 |
| Assets/_Project/Editor/GameplayTagValidator.cs | b6e2827134d88694a4b90409797f309a139efb35c79b197fa6da11e909bd5ec2 |
| Assets/_Project/Editor/GameplayTagValidator.cs.meta | 7e29e280838b616afe738888b7a356e86122dda7fc531cec01640853d606716d |
| Assets/_Project/Editor/ItemCatalogValidator.cs | 2101a0f1567d37f27d59b0c9a63a0f63ae3a74e1392163c31b6d2b30bfd813b1 |
| Assets/_Project/Editor/UnityMetaValidator.cs | 3dffb4c17e6f054b0141c26a5f43d7b0eb7d3f7618907598d7f554856d7200e3 |
| Assets/_Project/Features/Creatures/Movement.cs | d5639f646758854a674ea8e91f9177260c4b34c58b5a3477ef138e821c8bda3d |
| Assets/_Project/Features/Items/Actions.meta | 2df8cac6bd49a446fdb0836a8df304d685c73194f0522bef96efb23c6b0afe37 |
| Assets/_Project/Features/Items/Actions/ItemActionDefinition.cs | b4451df937793fb96f58d794ae6c4e89ff56831cdd7def7dcbfbdeebd6add911 |
| Assets/_Project/Features/Items/Actions/ItemActionDefinition.cs.meta | ecaf3f028c8efa511f462ec025cd2ab4e24cccdf183ed43ebe1bfdcf933180a5 |
| Assets/_Project/Features/Items/Actions/MeleeAttackAction.cs | 99ba7750d3d0a64819d74d12f5ff7741f5e6930f92b39fb86c3502f09f68aab2 |
| Assets/_Project/Features/Items/Actions/MeleeAttackAction.cs.meta | 08530e9e19ee997b599d08730f655a38c15d595da32060c793c3953f9eba083e |
| Assets/_Project/Features/Items/Actions/MeleeAttackConfiguration.cs | 623b86115663e2ab9e8af7b7a0645215d90b066cb04fd2b981884fe796b10aed |
| Assets/_Project/Features/Items/Actions/MeleeAttackConfiguration.cs.meta | 3a072825b851a08316f557004fdd9b02630611ecb08c4df9c30c04ccb3d67870 |
| Assets/_Project/Features/Items/Core/ItemDefinition.cs | 571995b0ef8c64eb4058471a53dba7542e149f1f1a9d17ea835aefe8fd2ae283 |
| Assets/_Project/Features/Items/Database/ItemCapabilityCatalog.cs | 1341ebf7fcef39803eec02129883278ac4acf183189a2f0da9477004823087d6 |
| Assets/_Project/Features/Items/Database/ItemCapabilityCatalog.cs.meta | 881f1e679d484828be50a0f8ffabd7e2cf4d59602cd7098080b61e353b86950f |
| Assets/_Project/Features/Items/Database/ItemDatabase.cs | c82266d3c7c4a1d1b42b2af4c0f1db837920dc6e8372625d5bab95d02875b3d6 |
| Assets/_Project/Features/Items/Resources/Database.asset | 5b83452e695011beaf737457cc35078894c229add6ed3eb30cc4408ab31b115d |
| Assets/_Project/Features/Items/Weapons.meta | 0919c72b3a25f3ef30a57d419b6691d0f686bb34e5223466441eb6fbccad4ee1 |
| Assets/_Project/Features/Items/Weapons/MaintenanceWrench.meta | 8dce33e31ae21ea6412f40d5962adf2be22f62d02cac9f27170d54143df64bd5 |
| Assets/_Project/Features/Items/Weapons/MaintenanceWrench/maintenance_wrench_placeholder.png | 341c825a7bbb434bf51b1f6d3b827ada91695498cc73b54e107e0e01a6780790 |
| Assets/_Project/Features/Items/Weapons/MaintenanceWrench/maintenance_wrench_placeholder.png.meta | 93adb96a9575ab9e8062b6c7b235b4a7a2217770cc78d956b3b3f2f236762116 |
| Assets/_Project/Features/Items/Weapons/MaintenanceWrench/MaintenanceWrench.asset | f25de945fdc45247c6f29b1205d677c483953b889729a9c26479824ab8e93d55 |
| Assets/_Project/Features/Items/Weapons/MaintenanceWrench/MaintenanceWrench.asset.meta | db1ef04f2315ed1e5890c4c02a7d7cb56aae54a7be21c1eecb4a3a304c7c63e2 |
| Assets/_Project/Features/Items/Weapons/MaintenanceWrench/MeleeAttack.asset | 3abcbc56fb30d4f4d93306af02ca8dec03c18922b835e0e0080c5af640134016 |
| Assets/_Project/Features/Items/Weapons/MaintenanceWrench/MeleeAttack.asset.meta | 08fcc3c63247bf5beec13aba27ff282f50db2a9376928b25ab80e2e9e6bb6394 |
| Assets/_Project/Features/Items/Weapons/MaintenanceWrench/MeleeConfiguration.asset | 226a69be233c5c82a9097a6ce20d90a3092664fc8bf2aca60a65277a0c25b732 |
| Assets/_Project/Features/Items/Weapons/MaintenanceWrench/MeleeConfiguration.asset.meta | c4cf8d16173911a971940f3b3da211a91b82952ac935ad42e0f5615a1031e0fe |
| Assets/_Project/Features/Npcs/Sec0/Runtime/PlayerStunReceiver.cs | 912ef96ca69da46849500715d1d71c4996471ffbd26f2a63a63ba63651a74d31 |
| Assets/_Project/Features/Player/Combat.meta | f3fdaf50ce713f9f83b1f688ad1e05151bfb3b73673814694ecb83d9f2609c93 |
| Assets/_Project/Features/Player/Combat/PlayerItemActionComposition.cs | 59e04b5d00a1d5f02d25180357fb7e329efe70ca25742d728053c2a55e79bfd9 |
| Assets/_Project/Features/Player/Combat/PlayerItemActionComposition.cs.meta | e9dbb782d1dc815b8d71233c44cabe53c22b316d449d2ef05a3ed1851d23661d |
| Assets/_Project/Features/Player/Movement/PlayerMovement.cs | 3833c86292bbf776b53ca9a57da40f92a47183102c971a2d0563999795cb41d7 |
| Assets/_Project/Features/Player/Succession/SurvivorAvatarAdapter.cs | a6d50a4db12c9352c0418c6b9b17b4cc33959908d5e62bc09085de787334add1 |
| Assets/_Project/Features/Player/Succession/SurvivorSuccessionComposition.cs | c7de3bcb86157826a9f9acc265519ae9ce7520d755f32c3b17f1daae63a084e1 |
| Assets/_Project/Features/Player/Vitals/PlayerVitalController.cs | efa6c878cf34261eb90ead40ecbb700ddb15fb7764d0010f6e14b9fcb84960ed |
| Assets/_Project/Features/Player/Vitals/PlayerVitalsComposition.cs | 62ebb61c30ede902a93a93d26bf7ba3686d58e0c740a2192e460c66273c0c6d9 |
| Assets/_Project/Systems/Combat.meta | 5439b758ef62c3bbc550f583f46c0484069e57cab9427be498f1236074e0cb64 |
| Assets/_Project/Systems/Combat/MeleeAttackExecution.cs | d804c188d63702f143e5ee4a2e8a54a20c8051ebf38be8df22c0838d37f17cb4 |
| Assets/_Project/Systems/Combat/MeleeAttackExecution.cs.meta | 1bb27dffcad0a9d904a04be4c30a29f37b2b7f7e733541f7e7ac27c6bb4bd469 |
| Assets/_Project/Systems/Combat/MeleeSwingView.cs | a283c4d63d4f646ff6aef16cc41062e5892e13e9682d80b18d4ef7ed48ce516c |
| Assets/_Project/Systems/Combat/MeleeSwingView.cs.meta | eddd9eb91c6a9e70a6d9af66a30e4854fef36cc32aa416480ecb1250cb76653a |
| Assets/_Project/Systems/GameplayTags.meta | 3cf3749e0289e9e6c5843fe3bafeb98dd121dda6365889b380d9da0b4456a6f2 |
| Assets/_Project/Systems/GameplayTags/Assets.meta | a2f36a80cb50c2991e40440e17227d17c8f434f54234095ffbf5c8497514ba27 |
| Assets/_Project/Systems/GameplayTags/Assets/Damage.asset | c4c3da6538e0319a688e7081d3041b50365d4ca73192f9c4d868bc7bc0d2bedd |
| Assets/_Project/Systems/GameplayTags/Assets/Damage.asset.meta | baeff0a50ed71a33cb5883f12a163d519f2a48f533cae79de343ac67f4990479 |
| Assets/_Project/Systems/GameplayTags/Assets/DamageBlunt.asset | 123f3d179bf0cd3de1ea11edd19e4cf9f4d90c6e192fe90804ebc152396c5c9c |
| Assets/_Project/Systems/GameplayTags/Assets/DamageBlunt.asset.meta | da448735a321c5b33b6a1262c5ad07cbec0ffc7db78df796bec5021a3e133739 |
| Assets/_Project/Systems/GameplayTags/Assets/Item.asset | 4995c14a8332da3e66b39a6d1c4a8ab045fd0d2fcbbb2d5f391735945f28d3f7 |
| Assets/_Project/Systems/GameplayTags/Assets/Item.asset.meta | 8efe4fc20443aec8b256e80e9fbacf1092a79afbe4cb1feb8f1cf8f02483a711 |
| Assets/_Project/Systems/GameplayTags/Assets/ItemImprovised.asset | 30b2b608ccb2939c563227e4318a1070598adc15a4d6e1966142e241783a309e |
| Assets/_Project/Systems/GameplayTags/Assets/ItemImprovised.asset.meta | bb8a3d782dd224f6324cbdf7bafd46b2136d236ce8e0c68b96baa252cde7eae3 |
| Assets/_Project/Systems/GameplayTags/Assets/ItemTool.asset | 0fc5cbd0eea95ffd097f539cf05110758c3884d93c253ab879d982dcaa6a320f |
| Assets/_Project/Systems/GameplayTags/Assets/ItemTool.asset.meta | c294a6b9738e890171763ff99ff7b63ca1918d47c8eec75f4f2ccb28976a7879 |
| Assets/_Project/Systems/GameplayTags/Assets/ItemWeapon.asset | eb34a7945e7a63e122d06aa330dfba75ec26f5e5f13d93fb4ed75e81d7c29b24 |
| Assets/_Project/Systems/GameplayTags/Assets/ItemWeapon.asset.meta | 6f19c35ab6cab182c501c532d6fa668ba102a3f36a02e48259b4d2ea381b56b3 |
| Assets/_Project/Systems/GameplayTags/Assets/Registry.asset | ec8989def28090d36727873661089801cebc7dda7b38a99146074d5310b0cdd8 |
| Assets/_Project/Systems/GameplayTags/Assets/Registry.asset.meta | ddca2c711276461f4cdff71a7ba728049745d99fd2eccf54c741927d35324830 |
| Assets/_Project/Systems/GameplayTags/Assets/Tool.asset | b7a90bd662fafde589984b4f015f9034e7012edb36c1bab785c3b49051e2b7c2 |
| Assets/_Project/Systems/GameplayTags/Assets/Tool.asset.meta | 0164b3964a655efb5f5f19d2195cb5e934bc363313b326e2cfc0d1b4c3530b2e |
| Assets/_Project/Systems/GameplayTags/Assets/ToolMechanical.asset | 8074bdadcd72ec340b06b392434a52140cf9f3cd0d4b7bc546c0ab2751f89bd7 |
| Assets/_Project/Systems/GameplayTags/Assets/ToolMechanical.asset.meta | 2a11cd92f7a9a1081e256c43d9bfc7a4ee9fe1d0cfa562157ef9f6924635ae29 |
| Assets/_Project/Systems/GameplayTags/Assets/Weapon.asset | dc575f6afd71321ab8e28231024ca128933a3fa0f0e41794030e66a226648bf1 |
| Assets/_Project/Systems/GameplayTags/Assets/Weapon.asset.meta | 050dbe02123b6899746fdea8b74bb6b3a00b18366d72fcd41b0b88c748914938 |
| Assets/_Project/Systems/GameplayTags/Assets/WeaponMelee.asset | c08fe45748c5f9b073809a1f7f2fa7351f99d60c3da51437bb597c601781c29d |
| Assets/_Project/Systems/GameplayTags/Assets/WeaponMelee.asset.meta | 05ec848962dc141feb8c38ee9d4a967bbef35344d3755eeca5e05c445794eb41 |
| Assets/_Project/Systems/GameplayTags/GameplayTagAuthoring.cs | 6967028f52756b382733cb414a3419bdd903fef0a3e91eaac8f928b3d6fbe9d0 |
| Assets/_Project/Systems/GameplayTags/GameplayTagAuthoring.cs.meta | 1bdf309d62db91a71a13cfb82a34743d5dbe016eca86815b7b291d03e0ded649 |
| Assets/_Project/Systems/GameplayTags/GameplayTagDefinition.cs | 15b4b8b7948884d2fc9745cecc3bacc04b7da1337ab88f92cc5f6f5e68a10265 |
| Assets/_Project/Systems/GameplayTags/GameplayTagDefinition.cs.meta | ddb821e0aefd558158c50758ace9bf67305b92e5c08b797034b6e2216f35bea5 |
| Assets/_Project/Systems/GameplayTags/GameplayTagRegistryAsset.cs | ce25e968a683da38f8ee806de82cb042dd40b216c28677aa5f09e11a25408da7 |
| Assets/_Project/Systems/GameplayTags/GameplayTagRegistryAsset.cs.meta | be2f3643f686a543377abb11b3d940df38371dfbc8b9348acb48b4312dcbb8e2 |
| Assets/_Project/Systems/GameplayTags/GameplayTagSource.cs | 488e75dec83d976c56d05b7d4bb6556d418e415e91fcebded27eafdc9878108a |
| Assets/_Project/Systems/GameplayTags/GameplayTagSource.cs.meta | 060c4d568fa88c2b7f83087205008b75b99001060fd6342a799c7a8d47d93ab2 |
| Assets/_Project/Systems/ItemActions.meta | 7a0141b62645501e0e21ac6db0a56657c1e4ef728ad775cdec9acb0aa1dbfcff |
| Assets/_Project/Systems/ItemActions/ItemActionContext.cs | 30c53c0469d515577991de1f55af1c81e7bcd54512611620737c6da2a461957c |
| Assets/_Project/Systems/ItemActions/ItemActionContext.cs.meta | ccb91e6b9c3be3e67b2d250a5b18823be38ba3fc1c150cf6e6e4f85e01dd36c4 |
| Assets/_Project/Tests/Application/EquippedItemViewTests.cs | 72da40e08e0e8604cef1031b30ab4b573fea965ff4b8f539f7799b50c9ad3fc3 |
| Assets/_Project/Tests/Application/EquippedItemViewTests.cs.meta | e959ea52a7f758b3e34a7c651e0becf420104e43c20b24fea7faf210941d47b0 |
| Assets/_Project/Tests/Application/ItemActionRunnerTests.cs | a1f783dddbc040afbf34b832828a9536d8fc643a23a0cd75f3c4413ed5da525e |
| Assets/_Project/Tests/Application/ItemActionRunnerTests.cs.meta | ab319045fb934eebddf35776cdf614a9770936b117f3931f763caf166dc67a87 |
| Assets/_Project/Tests/Domain/GameplayTagTests.cs | 6ec2ffab986f0749dfc27fe53f90e9a1fec41bdc5ed5a388dfd2ce4893fa25ed |
| Assets/_Project/Tests/Domain/GameplayTagTests.cs.meta | 592e800fd8800772a4f9ec70c8ea23859df30da0c9c10383b4bf9caed07a1b78 |
| Assets/_Project/Tests/Domain/MeleeAttackTimingTests.cs | 5683303f42419a7c5d0678b41a601f8208ea6b136f7e510979dd377353eef549 |
| Assets/_Project/Tests/Domain/MeleeAttackTimingTests.cs.meta | 69b68def9bf3bdcd0da19fc291f115b6dfe4e6db6895df064f6a5431593ad501 |
| Assets/_Project/Tests/Editor/GameplayTagAuthoringTests.cs | 7fd317d12bfa944a23553670d0dd935237c336291c70bc580f2772639edd7d58 |
| Assets/_Project/Tests/Editor/GameplayTagAuthoringTests.cs.meta | dc3ba8ab536c71764be7652c4a830b28d9b07eb9cd3458a40535e66911175b2b |
| Assets/_Project/Tests/Editor/ItemActionAuthoringTests.cs | 47d6e4a2ee99d1b1ff5bb3aa6c0baaf6d8181e1c091bb1fc79b6e5596abb3bd2 |
| Assets/_Project/Tests/Editor/ItemActionAuthoringTests.cs.meta | 9d7d06ec5594dfed4a44059775a3390b686cf74fce83abb9e58a853a7a34868a |
| Assets/_Project/Tests/Editor/ItemActionInputTests.cs | 88df31f4d4ec4fd7a90f000faa6ff0241624d9dd6777fe3e5f59857e3d87bd02 |
| Assets/_Project/Tests/Editor/ItemActionInputTests.cs.meta | f34c8c9d8c54a9e4335f38dc9c12ed1bbef7952772e81d5c50262ab5b71edae7 |
| Assets/_Project/Tests/Editor/UnityMetaValidatorTests.cs | 98fb9320adf945e18204e5300a11e4dde2be1f7987e588e8f45b97ec964e5d7a |
| Assets/_Project/Tests/Editor/UnityMetaValidatorTests.cs.meta | 3ed039469434350743a38d9ad1152a3f3be416e9fa30f11676293e0c4b6c6c6d |
| Assets/_Project/Tests/PlayMode/EquippedWrenchSwingTests.cs | d7a3eb6fb7d317057e2fe2a8720b417dac6fdeabbc5471c45b43775220cecf89 |
| Assets/_Project/Tests/PlayMode/EquippedWrenchSwingTests.cs.meta | dd6a2505328d844963c1519cfef98b3113b498b1e5b11227e24b38043526a9c3 |
| Assets/_Project/Tests/PlayMode/PlayerItemActionRuntimeTests.cs | 2d4ff90f93f45c5e1596f290033c8d7a0fdbd5d3d6ba8c55c2f665d0703fad29 |
| Assets/_Project/Tests/PlayMode/PlayerItemActionRuntimeTests.cs.meta | f1334458998842b02860d3713558b53d40d4021c426d92a00f3e1216fe6e4131 |
| Assets/_Project/Tests/PlayMode/WeaponSystemTestFixtures.cs | d70cff08af70bccdc2c5718100cb07c7889aa61e6603e6a895c44190b860e0fa |
| Assets/_Project/Tests/PlayMode/WeaponSystemTestFixtures.cs.meta | 748ef939e6f2966eba31b544958d3b9887ab0898ca9ba2e1977400013795eeb6 |
| README.md | 68c7705125916d35821d4066cc9cd6df4c533ee6e843c17b506e6b13e9b3b17d |
| Tools/Verification/PureTests.csproj | 91db9e545c7a4581fa6c8a2b41f4471eb105a58fb1823242f5bc414ae54743f2 |
| Tools/Verification/verify.py | 98baf3bd05845576a0284f3dc874307b039b68bd0be084766907e0dbbd83236b |

