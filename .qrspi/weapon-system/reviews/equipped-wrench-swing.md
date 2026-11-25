## Verdict: FAIL
Phase 1 diverges from S15 because successor awakening inherits the departed actor's remaining committed attack lockout.

## Scope
Reviewed weapon-system Phase 1, all four completed steps: gameplay tags, item capabilities/equipped binding, actor execution and visible dry swing, and real Primary input. Damage, SEC-0 health/feedback, production loadout/persistence, final encounter qualification, allocation measurement and final documentation are later-phase work.

Exact diff command, run at repository root:
```sh
git diff 335c87a8d9e55dfbdd8f7b0ad3ab7e73952af306 -- $(git diff --name-only 38395596d28c5d063270d0276b2ce9f412a04b20 06349bf6f51539fe7d5fd964926a81db85752d20)
```

Snapshot:
- Repository: /Users/jeremyvalentine/SOLITUDE
- Default branch independently resolved: refs/remotes/origin/main
- Resolved default-branch merge base: 335c87a8d9e55dfbdd8f7b0ad3ab7e73952af306
- Fixed path-selection range: 38395596d28c5d063270d0276b2ce9f412a04b20..06349bf6f51539fe7d5fd964926a81db85752d20
- HEAD: 06349bf6f51539fe7d5fd964926a81db85752d20
- Exact diff SHA-256: 1f965f640c942b651c11c2802e17c740c02cda2db4445ce3a5097074735a7728
- Spec: .qrspi/weapon-system/spec.md, SHA-256 78d09ac0e72bb413355cffdab28a21a5e501e69138731c6ad1f0863ff63a74f5
- Plan overview: .qrspi/weapon-system/plan.md, normalized SHA-256 3de5bf9dc968659266df2a1d2d521cf71ff9344d30bc45c38463665368f61b47
- Phase plan: .qrspi/weapon-system/plans/plan-phase-1.md, normalized SHA-256 b499d89078b2a5c615d983a7d061ec728ceacabec20c87045bbfd33316ae3c9d
- Normalization replaces only the supplied status-marker and numbered phase-row status tokens with [ ]; other bytes are preserved.
- Caller supplied a successful digest-only upstream provenance check; upstream artifacts were not read.
- Prior reviews: none present when review began. Output did not exist.
- Working tree before/after diff: one tracked unstaged edit outside scope, Assets/_Project/Features/World/SolitudeStart/Scenes/SOLITUDE_Start.unity. Preserved and not reviewed. No staged or untracked product files. QRSPI scaffolding excluded.
- Snapshot verified unchanged immediately before verdict publication: HEAD, exact diff, spec/normalized plans, every listed source digest and product status.
- Source files read: full changed-file context and unchanged dependencies are fingerprinted below; all are working-tree bytes, not index-only content.

## Findings

| Severity | Blocking | Category | Location | Description | Suggested Fix |
| --- | --- | --- | --- | --- | --- |
| HIGH | yes | Spec conformance | Assets/_Project/Features/Player/Combat/PlayerItemActionComposition.cs:38,52-61; Assets/_Project/Application/Items/ItemActionRunner.cs:16,42-50 | F1 — S15: retirement cancels the swing but does not retire its runner. The existing same-object succession path awakens a new survivor without releasing/rebinding PlayerItemActionComposition, so the new survivor retains the departed survivor's committed attack delay. | Connect action ownership to logical survivor identity/generation. Dispose/reset the departing runner on committed retirement and give the successor a fresh runner, while retaining lockout for cancellation on the same living actor. Add a real succession regression that equips the successor and requests Primary before the departed swing would have ended. |

## Evidence

### F1 / S15 — successor inherits the departed actor's timer
- Trigger: in a composed scene with the Phase 1 manually equipped wrench, start a swing, advance .05 gameplay seconds, and invoke the existing SurvivorSuccessionComposition.RequestDeath for the current survivor. Complete succession on the existing avatar, return an actionable item to its hotbar through the existing command/transfer route and select it before another .50 gameplay seconds pass. Opening recovery inventory can pause gameplay, so time spent transferring does not necessarily exhaust the delay.
- Expected: the spec's melee lifecycle says retiring an actor discards its timers rather than transferring a swing or lockout to a successor. Same-living-actor cancellation must still preserve commitment.
- Actual: the execution is canceled, but the same ItemActionRunner remains with approximately .50 seconds of LockoutRemaining. The successor's valid request is rejected by TryStart until that inherited timer expires.
- Support: PlayerItemActionComposition creates its runner only in Bind (line 38), implements policy/equipment invalidation using runner.Cancel (lines 52-54), and disposes it only in Release (lines 59-61). ItemActionRunner.Cancel clears execution/request without clearing clock/lockoutUntil (lines 42-45); TryStart rejects positive LockoutRemaining (line 16); only Dispose clears its timers (line 50).
- Existing lifecycle source closes the upstream-guard question: SurvivorSuccessionComposition.SetTransitionGated sets policy transition and resets input (lines 100-103); that invokes cancellation, not retirement. TryActivate calls avatar.Awaken and clears selection on the same avatar (lines 159-163). SurvivorAvatarAdapter.Awaken relocates and resets vitals (lines 30-37), with no action-runner retirement. SurvivorSuccessionCoordinator commits retirement, activates the successor and ungates the scene within Run without rebuilding SceneBindings. SceneBindings creates/binds actions once during scene initialization (lines 191-198), and releases them only on scene Release (lines 325-329). Neither selection clearing nor hotbar handoff creates a new runner.
- Test coverage: ItemActionRunnerTests proves pure Dispose clears timers, but the scene never calls Dispose for this logical actor transition. PlayerItemActionRuntimeTests.DeathStunAndDisableCancelInEveryPhase advances .55 after ResetForAwakening before requesting again, so it cannot expose inheritance. Existing Phase 1 tests do not exercise an immediate post-succession request.
- Evidence method: independently read complete implementation and lifecycle dependencies and inspected numbered source lines. This is a complete source trace, not a claimed executed Unity reproduction.

### Independent checks and limits
- Ran the supplied exact diff command, with git status --short --untracked-files=all before and after; fingerprints and status match the supplied envelope.
- Resolved default branch with git symbolic-ref refs/remotes/origin/HEAD and merge base with git merge-base HEAD refs/remotes/origin/main; result matches the scoped base.
- Independently hashed exact spec bytes and normalized approved plans; all three match approved revisions.
- Parsed the generated wrapper's InputActionAsset.FromJson payload and the authored .inputactions JSON using Python json.loads and compared complete objects: equal. Inspected all action names/types/bindings; Primary is a Button bound to mouse left/gamepad right trigger; Use and Interact retain their existing routes.
- Read all selected code, configuration/assets, test source and metadata; inspected the placeholder image. Source tracing covers tag ownership/matching/validation, selected-slot revisions, generic execution ownership, timing crossings, movement/facing leases, optional presentation and deferred UI-aware input dispatch.
- Ran scoped git diff --check: exit 2, with 66 trailing-whitespace diagnostics, mainly serialized Unity empty fields plus an empty line in ItemCatalogValidator. These are not reported as behavioral findings.
- Did not run builds, pure suites, Unity suites, gameplay capture, allocation measurements or the Wakeup gate. Reading tests is not evidence they passed; this review does not certify those executions. No implementer evidence reports or logs were read. Required final runtime/visual/performance qualification remains later-phase work.

## Open Questions
None preventing this source-based Phase 1 verdict.

## Supplemental Reviews
None provided or read.

## Spec Conformance
The PRESENT entries below mean the Phase 1 source responsibility is implemented, established by the stated source trace; they do not claim unexecuted Unity or regression suites passed. Partial criteria explicitly distinguish later-phase responsibilities.

- PRESENT S1 — serialized GameplayTagDefinition identities and object references are distinct from mutable name/segment; registry lookup uses identity. GameplayTagAuthoringTests contains asset move/rename/round-trip coverage.
- PRESENT S2 — registry validation rejects null/unregistered parents, duplicate identities/full names/entries, malformed segments, self-parenting/cycles; authored sets reject duplicates/null/foreign references. Database capability compilation runs before root readiness.
- PRESENT S3 — authored wrench has six tags and sets support multiple tags. Untagged damage-recipient behavior is NOT IN SCOPE until Phase 2.
- PRESENT S4 — exact/hierarchical membership compiles owned ancestors in the correct direction; GameplayTagTests covers the matching table.
- PRESENT S5 — required/any/excluded loops implement conjunction, nonempty disjunction, exclusions and conflict rejection.
- PRESENT S6 — direct empty predicates and all-empty whole query differ as specified; complete reference validation prevents invalid entries from becoming empty conditions.
- PRESENT S7 — additive item fields preserve existing metadata/consumables; the catalog references the nonstackable wrench and configured Primary with stated values.
- PRESENT S8 — runner is action-generic, execution owns mutable timing/leases, and definitions compile immutable numeric snapshots. Shared-actor and alternate pure execution tests exist; no item-ID dispatch branches.
- PRESENT S9 — EquippedItemView derives exclusively from selected hotbar reader, registration handle and slot revision; empty/actionless slots reject requests. Existing catalog/presenter route provides icon/highlight; composed fixture assertions cover it without claiming an executed HUD result.
- PRESENT S10 — input asset/wrapper equality independently checked; source defers Primary until UI processing, checks raycasts/consumption version, captures held controls across policy/rebind/reset, and preserves Use/Interact.
- PRESENT S11 — source checks active component/object, positive health and stun; gameplay policy freezes advances and transition readiness cancels. Phase/lifecycle tests exist; no Unity execution claimed.
- PRESENT S12 — MeleeAttackTiming uses configured .12/.10/.33 intervals and authorizes active processing only across Active; execution does not use animation as hit authority.
- PRESENT S13 — coarse advance reports crossing Active before completion, delivered through MeleeAttackExecution's same callback port. Actual static-target hit equivalence is NOT IN SCOPE until Phase 2.
- PRESENT S14 — runner preserves lockout across Cancel; selection invalidation uses Cancel; input hold cannot automatically repeat.
- DIVERGED S15 — F1: logical survivor retirement does not reset the scene-owned runner. Same-definition slot revisions, unavailable owner checks, repeat-safe cancellation and movement cleanup are otherwise implemented.
- PRESENT S16 — normalized last movement/default down is captured per context; animation facing lease preserves current swing facing; generic minimum speed restrictions compose with full movement blocks and dispose independently. Source and authored placeholder inspected; no actual-scene motion capture claimed.
- NOT IN SCOPE S17 — target collider/range/sector geometry is Phase 2.
- NOT IN SCOPE S18 — blockers, trigger filtering and saturated physics queries are Phase 2.
- NOT IN SCOPE S19 — authoritative recipient deduplication and damage are Phase 2.
- NOT IN SCOPE S20 — damage context/resolution and recipient eligibility are Phase 2.
- NOT IN SCOPE S21 — damage adapters and rejection feedback are Phase 2.
- NOT IN SCOPE S22 — SEC-0 incoming health/defeat is Phase 2.
- NOT IN SCOPE S23 — hit feedback, displacement and audio event delivery are Phase 2.
- PRESENT S24 — Phase 1 optional held art/view can be absent; required melee numeric ranges, action references and tags compile before affected runtime readiness. Hit/audio/knockback tolerance and actual feedback are later Phase 2 responsibilities.
- NOT IN SCOPE S25 — configurable production initial loadout is Phase 3.
- NOT IN SCOPE S26 — save v4/loadout migration and transactional initialization are Phase 3.
- NOT IN SCOPE S27 — loadout resume/arrival/succession grant semantics are Phase 3. Phase 1's own successor action isolation is judged under S15.
- NOT IN SCOPE S28 — authored SEC-0 composed four-hit journey is Phase 4 after Phase 2.
- NOT IN SCOPE S29 — complete final regression/Wakeup qualification is Phase 4; Phase 1 test source and verification flag change reviewed, with no suite result inferred.
- PRESENT S30 — scoped source adds assets with metadata, reuses catalog/runtime and current assemblies, and preserves existing item/input identity fields. No shipping entry-list edit is in scope. Final build/catalog validation remains Phase 4 qualification.
- NOT IN SCOPE S31 — allocation measurement and bounded physics processing qualification are Phase 4; Phase 1 tag hot-path source uses precompiled sets without transient collection construction.
- NOT IN SCOPE S32 — final extension-point/Unity validation documentation is Phase 4.

### Source files read and SHA-256
All rows are working-tree origin. Includes selected changed assets/metadata and unchanged context/dependencies; only content read during this review is listed.

| Path | SHA-256 |
| --- | --- |
| .gitignore | 06191a79d474f31f919a009fb77d8322af037038e8b398f387ecafe930dcc1b1 |
| Assets/_Project/ARCHITECTURE.md | 855391ba68cdf06e7895883dca350a6c0e8bbe8b7589fa169bffafe3ff0f3c8e |
| Assets/_Project/Application/Items.meta | 7db93e43f38d5eb07e7f0ed2a54cc31ba45d653cd081e31221ea9f035902605c |
| Assets/_Project/Application/Items/EquippedItemView.cs | b5957723fe4656cad09504e1190e4c91cd42845662572f2ea707eb3ee3f64159 |
| Assets/_Project/Application/Items/EquippedItemView.cs.meta | cefc9b9c04d9e1bc6494c3d95a4fd6ee871f1954a76146ee8719d81ec617111a |
| Assets/_Project/Application/Items/ItemActionRunner.cs | 702e350455bb3a226fb8c8476bae6bf08a996574fda84832445bc3e5e0492148 |
| Assets/_Project/Application/Items/ItemActionRunner.cs.meta | 8c22e949e7af35ab9dd9b292ac73a78a1c7eb3314c1a6b16319c8b8781bf3b74 |
| Assets/_Project/Application/RuntimePolicy.cs | 3e1ac34b6c013fad11b50cb5b759584845c25cc18e1378c73981cd65329916a6 |
| Assets/_Project/Application/Survivors/SurvivorSuccessionCoordinator.cs | 3b8ef879895941dca7d64f1b888164ad8de91dc962a14a90609ba17e15f0cce9 |
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
| Assets/_Project/Editor/README.md | 915af08ca58838ecfdceb281009acf266f0e5ead2721b3d1b6fb340c1433e381 |
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
| Assets/_Project/Features/Items/Core/README.md | f7196f3fffbde2c4b167beaf842c9f74fe789044231144acc57640abd22b060b |
| Assets/_Project/Features/Items/Database/ItemCapabilityCatalog.cs | 1341ebf7fcef39803eec02129883278ac4acf183189a2f0da9477004823087d6 |
| Assets/_Project/Features/Items/Database/ItemCapabilityCatalog.cs.meta | 881f1e679d484828be50a0f8ffabd7e2cf4d59602cd7098080b61e353b86950f |
| Assets/_Project/Features/Items/Database/ItemDatabase.cs | c82266d3c7c4a1d1b42b2af4c0f1db837920dc6e8372625d5bab95d02875b3d6 |
| Assets/_Project/Features/Items/Resources/Database.asset | 5b83452e695011beaf737457cc35078894c229add6ed3eb30cc4408ab31b115d |
| Assets/_Project/Features/Items/Weapons.meta | 0919c72b3a25f3ef30a57d419b6691d0f686bb34e5223466441eb6fbccad4ee1 |
| Assets/_Project/Features/Items/Weapons/MaintenanceWrench.meta | 8dce33e31ae21ea6412f40d5962adf2be22f62d02cac9f27170d54143df64bd5 |
| Assets/_Project/Features/Items/Weapons/MaintenanceWrench/MaintenanceWrench.asset | f25de945fdc45247c6f29b1205d677c483953b889729a9c26479824ab8e93d55 |
| Assets/_Project/Features/Items/Weapons/MaintenanceWrench/MaintenanceWrench.asset.meta | db1ef04f2315ed1e5890c4c02a7d7cb56aae54a7be21c1eecb4a3a304c7c63e2 |
| Assets/_Project/Features/Items/Weapons/MaintenanceWrench/MeleeAttack.asset | 3abcbc56fb30d4f4d93306af02ca8dec03c18922b835e0e0080c5af640134016 |
| Assets/_Project/Features/Items/Weapons/MaintenanceWrench/MeleeAttack.asset.meta | 08fcc3c63247bf5beec13aba27ff282f50db2a9376928b25ab80e2e9e6bb6394 |
| Assets/_Project/Features/Items/Weapons/MaintenanceWrench/MeleeConfiguration.asset | 226a69be233c5c82a9097a6ce20d90a3092664fc8bf2aca60a65277a0c25b732 |
| Assets/_Project/Features/Items/Weapons/MaintenanceWrench/MeleeConfiguration.asset.meta | c4cf8d16173911a971940f3b3da211a91b82952ac935ad42e0f5615a1031e0fe |
| Assets/_Project/Features/Items/Weapons/MaintenanceWrench/maintenance_wrench_placeholder.png | 341c825a7bbb434bf51b1f6d3b827ada91695498cc73b54e107e0e01a6780790 |
| Assets/_Project/Features/Items/Weapons/MaintenanceWrench/maintenance_wrench_placeholder.png.meta | 93adb96a9575ab9e8062b6c7b235b4a7a2217770cc78d956b3b3f2f236762116 |
| Assets/_Project/Features/Npcs/Sec0/Runtime/PlayerStunReceiver.cs | 912ef96ca69da46849500715d1d71c4996471ffbd26f2a63a63ba63651a74d31 |
| Assets/_Project/Features/Player/Combat.meta | f3fdaf50ce713f9f83b1f688ad1e05151bfb3b73673814694ecb83d9f2609c93 |
| Assets/_Project/Features/Player/Combat/PlayerItemActionComposition.cs | 7325b60010c9064c67c54eecf871d4c69157cae9c78ac9a3bd2b855c895511a1 |
| Assets/_Project/Features/Player/Combat/PlayerItemActionComposition.cs.meta | e9dbb782d1dc815b8d71233c44cabe53c22b316d449d2ef05a3ed1851d23661d |
| Assets/_Project/Features/Player/Movement/PlayerMovement.cs | 3833c86292bbf776b53ca9a57da40f92a47183102c971a2d0563999795cb41d7 |
| Assets/_Project/Features/Player/Succession/SurvivorAvatarAdapter.cs | a6d50a4db12c9352c0418c6b9b17b4cc33959908d5e62bc09085de787334add1 |
| Assets/_Project/Features/Player/Succession/SurvivorSuccessionComposition.cs | fb82fcda907e04d88ec1dd3833018c502b3222dd4fe69a57952923d5bbb5cef4 |
| Assets/_Project/Features/Player/Vitals/PlayerVitalController.cs | efa6c878cf34261eb90ead40ecbb700ddb15fb7764d0010f6e14b9fcb84960ed |
| Assets/_Project/Features/Player/Vitals/PlayerVitalsComposition.cs | 62ebb61c30ede902a93a93d26bf7ba3686d58e0c740a2192e460c66273c0c6d9 |
| Assets/_Project/LEGACY_COMPONENTS.md | d0494c6a433594375252d345b937b8002f3532ebef83caeb02f95b966ee2fb07 |
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
| Assets/_Project/Tests/PlayMode/EquippedWrenchSwingTests.cs | 34f8c1822d241422e7b6e7eb0864dc040a1b4dcf8f43cfce3f7e0f77ce134669 |
| Assets/_Project/Tests/PlayMode/EquippedWrenchSwingTests.cs.meta | dd6a2505328d844963c1519cfef98b3113b498b1e5b11227e24b38043526a9c3 |
| Assets/_Project/Tests/PlayMode/PlayerItemActionRuntimeTests.cs | 2d4ff90f93f45c5e1596f290033c8d7a0fdbd5d3d6ba8c55c2f665d0703fad29 |
| Assets/_Project/Tests/PlayMode/PlayerItemActionRuntimeTests.cs.meta | f1334458998842b02860d3713558b53d40d4021c426d92a00f3e1216fe6e4131 |
| Assets/_Project/Tests/PlayMode/WeaponSystemTestFixtures.cs | d70cff08af70bccdc2c5718100cb07c7889aa61e6603e6a895c44190b860e0fa |
| Assets/_Project/Tests/PlayMode/WeaponSystemTestFixtures.cs.meta | 748ef939e6f2966eba31b544958d3b9887ab0898ca9ba2e1977400013795eeb6 |
| ProjectSettings/ProjectVersion.txt | 338824f489d7ef647583e723283307ae92b3bb241d2fe874ac759dbf2f091701 |
| README.md | 68c7705125916d35821d4066cc9cd6df4c533ee6e843c17b506e6b13e9b3b17d |
| Tools/Verification/PureTests.csproj | 91db9e545c7a4581fa6c8a2b41f4471eb105a58fb1823242f5bc414ae54743f2 |
| Tools/Verification/verify.py | 98baf3bd05845576a0284f3dc874307b039b68bd0be084766907e0dbbd83236b |
