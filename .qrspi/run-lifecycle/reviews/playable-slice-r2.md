## Verdict: PASS

No independently verified defects remain in the complete Phase 4 checkpoint; the prior live-encounter refresh defect is resolved.

## Scope

Reviewed `git diff 1892df57bc02362c11d81291dcace071d2010798 -- . ':(exclude).qrspi'` for Phase 4, complete checkpoint, steps 1–3. Read the spec first, then the phase plan as scope assistance. The entire supplied product diff includes the NPC prerequisite, its runtime/navigation/movement support, demo, art and tests, as well as the encounter adapter, consumables, production route and regression integration. All were inspected independently. Phase 5 qualification is outside this checkpoint. No material alternate implementation route was identified.

Snapshot:
- Resolved base: `1892df57bc02362c11d81291dcace071d2010798`.
- HEAD: `c6c9272765920e284740d05fe575aedd0b790612`.
- Exact diff command: `git diff 1892df57bc02362c11d81291dcace071d2010798 -- . ':(exclude).qrspi'`.
- Product diff SHA-256: `1c7d293935babd25a5d79ae2e00b54129232ebf3dafa2bdf093330d6e4c48d0d`; excludes all QRSPI scaffolding.
- Spec: `.qrspi/run-lifecycle/spec.md`, SHA-256 `a9dc706c5f1628759fbfaed64ffc89258aeaead70a1d24987b9b4ce6b9882922`.
- Caller supplied digest-only upstream provenance validation. Independent hashes of spec and all approved normalized plans match the envelope.
- Working tree/staged status: clean before and after diff inspection and at final verification; no untracked product source. QRSPI scaffolding excluded.
- Source origin: working-tree bytes matching clean HEAD. The 313 changed/context/dependency file fingerprints below identify the inspected bytes. Serialized assets received full-file structural inspection; added PNGs received signature/chunk CRC checks.
- Snapshot verified unchanged immediately before publication: HEAD, exact product diff, spec, all normalized plans and every listed source fingerprint.
- Review restriction was contractual broad-tool discipline, not a hard filesystem sandbox. No source fixes, commits, additional agents or implementation reports were used.

Approved normalized plan SHA-256 (normalize only status-marker tokens and overview Status cells to `[ ]`):
```text
dc6e3dec5d02179bc7c8fa565ab91e422de6afbaf5d6ad6d8e4c633eff4444cb  .qrspi/run-lifecycle/plan.md
961eefaf4366bc834b4061d3ce7d4d01b8f7c231fc3fbe742c04e7afb07bf089  .qrspi/run-lifecycle/plans/plan-phase-1.md
bc733c6ee01970b3a5a493d42292baf0f4b886ae3d4f1953f0546fd43b2ef5a5  .qrspi/run-lifecycle/plans/plan-phase-2.md
5729e01c88632a53a843e24478178c0dda4c7a6b7a533d1ac0e68ed293ac2d29  .qrspi/run-lifecycle/plans/plan-phase-3.md
6b38838e9a2aaf438e002807ae98b055f7aa269faec39c727e257e73a52e0f26  .qrspi/run-lifecycle/plans/plan-phase-4.md
4d16e3c19852cade8e15dff7f830414a5dd8f1c540bad49393311a660ff5aeeb  .qrspi/run-lifecycle/plans/plan-phase-5.md
```

## Findings

None.

## Evidence

### Encounter and expedition refresh

`Sec0ExpeditionEncounter.Bind/CanInteract/Interact/Reconcile` validates stable identity and composition ownership, checks current living production state and gameplay policy, records the defeated fact in the cloned session expedition before stopping presentation, and restores it on rebind. Defeated actors have disabled collider, rigidbody simulation and visual; `Sec0Controller.Tick` rejects defeated actors before hostile logic.

Reconciliation now compares the session expedition identity with the last reconciled identity. A changed identity passes `resetForExpedition` to `SetEncounterDefeated`, so even a still-live false-to-false actor returns to authored position/rotation, waypoint, facing and transient brain/navigation/effect/counter state. The stored identity is advanced after reconciliation; ordinary command notifications and replayed receipts do not reset it again. Initial binding and release reset adapter ownership correctly. This resolves F1 from the completed prior `playable-slice.md` review.

`RunLifecycleEncounterTests` independently covers normal interaction, physical/visual suspension, duplicate interaction, scene recreation, codec resume, defeated-to-live death refresh and the newly added still-live death case. The latter explicitly displaces and rotates a live actor, observes attack state, verifies reset, then verifies a replayed receipt and same-expedition notification do not reset it again. Test source is evidence of coverage, not an executed pass.

### Supply, component and ship integration

Bandage and ration definitions are Medical/Food respectively, restoration 25, stack limit 10; pickup definitions grant one. Reusable prefab identities are empty and production instances override them to the two designated supply IDs. Production catalog retains its prior entries and adds those two definitions plus the existing stable fuse.

`WorldPickup` registers the explicit supply marker through the existing collection service and refreshes inactive loaded supply presentation on command completion. `ContainerSaveSession.Collect` commits inventory grant and the cloned allowance fact together through the command; an insufficient-space result commits neither. Death replaces the expedition allowance, while ordinary permanent collection facts remain separate. New consumable tests exercise the selected-hotbar transaction and distinguish health from hunger restoration.

The authored critical source points at `route.scrubber` and lists the actual shared locker identity, with no source/storage repair prerequisites. `CriticalComponentSourceState` checks all saved container records, excluding departing equipment on death, suppresses replacement when a retained component exists, and never replenishes an installed objective. `CommitDeath` builds post-loss reconciliation before publishing equipment discard. Scrubber installation prepares permanent repair state with consumption, and scene binding restores scrubber and door state before the root enables gameplay. The shortcut's restored open state immediately disables its collider.

### Production route and compatibility

Wakeup contains the production route, one identified hostile adapter, two designated supply instances, the critical fuse, scrubber/access/screen, one shared locker, powered shortcut and a 50-damage-per-second conduit using the existing gated vitals damage API. The SceneBindings pickup and feature arrays include the added endpoints. The normal input journey tests use keyboard movement/interaction and screen submission, reach the conduit and await automatic succession; the separate component journey covers carried loss and shared-locker retention.

Compared every original Wakeup serialized object against the base. No original object was removed. All four original painted Tilemap documents are byte-identical, preserving manual layout. Existing UI instance changes are layout-driven RectTransform values; the inspected HUD prefab supplies layout groups/elements and the container view uses its existing grid/layout behavior. The production route authoring command refuses to rebuild an existing route.

The generated disposable succession fixture removes only the production route and refreshes endpoint arrays before adding its demo marker. The resume fixture is saved first. Regression fixture changes select injected ship records by stable identity, reuse the scene's single scrubber composition, preserve explicit missing-fuse failure coverage with a catalog lacking that item, and avoid duplicating a catalog fuse. The separate SEC-0 demo retains its disposable session and is not a production entry.

### Independent checks and limits

- Exact supplied diff and repeated `git status --short --untracked-files=all`: successful; clean product tree.
- Python SHA-256 checks: spec, all six normalized plans, HEAD and diff matched envelope and final comparison.
- Full changed-file scan: 295 product files, including 26 C# files, 145 metadata files, 72 PNGs, two scenes, six prefabs, 34 animation clips and three controllers. No duplicate local object IDs in serialized files, no duplicate GUIDs among changed metadata, all PNG signatures and chunk CRCs valid.
- Wakeup: 352 serialized objects. SEC-0 demo: 336 serialized objects. All original Wakeup objects retained and all four Tilemaps unchanged.
- Animation-controller state names include all directional idle/move, alert and charge names generated by the controller.
- Read changed C# files in full context and relevant lifecycle, storage, pickup, scrubber, door, HUD/layout dependencies. Checked correctness, edge cases, error paths, test coverage, security surface and resource/ownership lifecycle. No confirmed finding.
- No Unity tests, builds, standalone process restart/interruption or manual gameplay were run in this review. This source/serialized-data review does not claim Phase 5 runtime qualification and does not adopt implementer test claims.

## Open Questions

None preventing this Phase 4 source review. Runtime, manual-journey and standalone fault qualification remain Phase 5 work.

## Supplemental Reviews

None provided. Enumerated completed prior review artifacts; read `playable-slice.md` as permitted background. Its F1 was independently traced against current source and is resolved. No other input envelope or implementation verification report was read.

## Spec Conformance

PRESENT entries describe independently inspected Phase 4 implementation/integration, not executed qualification.

NOT IN SCOPE S1 — Initial session initialization belongs to Phase 1; this phase preserves it and supplies authored world content without awakening grants.

PRESENT S2 — Phase 4 integration: conduit calls existing Damage; lethal observer queues session succession; root processes it outside vital publication, discards equipment, activates the retained avatar at cryobay and resets selection/vitals. Shared owners are separate. Route tests exercise actual hazard damage without debug lifecycle calls.

NOT IN SCOPE S3 — Transition preparation/fault authority was delivered in Phase 3. The adapter adds no lifecycle authority; its reset is idempotent by expedition identity. Comprehensive fault qualification remains outside this checkpoint.

PRESENT S4 — Normal disable interaction records expedition defeat before suspending actor physics/hostility; rebind respects the fact; changed expedition restores both defeated and still-live actors once. Encounter regression source covers both cases.

PRESENT S5 — Phase 4 encounter/supply facts live in the session expedition snapshot and bind without refresh; codec/recreation tests preserve identity and defeat/collection. Broader colonist/restart qualification is outside this checkpoint.

PRESENT S6 — Authored one-bandage/one-ration sources, correct existing consumable kinds, quantity-one registration and command-bound allowance; full-capacity failures leave sources available. New tests cover selected hotbar use, rebinding and death renewal.

PRESENT S7 — Ordinary locker and permanent pickups retain their existing owners and collection path; new supply definitions are not added to ordinary locker loot. Route test preserves collected ordinary oxygen after death; no locker-clearing path is introduced.

PRESENT S8 — Production fuse source maps to unfinished scrubber and reachable persistent locker; post-loss reconciliation excludes departing gear, checks retained saved shared storage, and suppresses completed objectives. Route tests cover carried loss and locker retention; persistence fixtures retain unloaded-owner policy checks.

PRESENT S9 — Production scrubber binds the existing permanent repair/consumption/recovery implementation. Route and persistence test sources verify install/restart/operational state, repeat-consumption prevention and partial recovery restoration. Standalone qualification remains outside this checkpoint.

PRESENT S10 — Production socket references the persistent shortcut; saved permission/open state restores through door binding and immediately applies collider state. Route/persistence tests assert open collision after death/rebind.

NOT IN SCOPE S11 — Full saved-location resume and invalid-location fallback were Phase 1 behavior; standalone qualification is Phase 5. This diff does not replace that authority.

NOT IN SCOPE S12 — Durable transition/fault protocol was Phase 3; isolated built-player fault qualification is Phase 5. Adapter notification handling does not commit a second death or allowance.

NOT IN SCOPE S13 — Migration/codec recovery was delivered by prior phases; this checkpoint introduces no schema or migration change.

PRESENT S14 — Existing command/readers, retained-avatar succession and scene composition remain the authorities. Narrow encounter adapter, movement lease and explicit demo-marker isolation preserve boundaries. Relevant regression tests retain fault, missing-catalog, storage and disposable assertions.

PRESENT S15 — Saved production Wakeup connects existing cryobay floor to maintenance actor/supplies/component, shared locker, scrubber and permanent shortcut with an ordinary lethal gameplay conduit. Existing painted layout is preserved; authoring validates route endpoints and composition. Input-journey test source covers the complete production route without debug death.

NOT IN SCOPE S16 — Transition-cause and New Save authority were delivered earlier; no expedition-completion trigger or public player refresh is added in this phase.

## Source files read

Working-tree SHA-256 (full changed-file context, serialized assets and unchanged dependencies):
```text
071edc83056da4baca88f63b4bd6a30ba9d6750b38f691738eaf9323e86f01b6  AGENTS.md
0aaea25ee22b96f49cc2613427a61c0ee40d9b9111674f1f6850a3380c0bc37c  Assets/_Project/Core/Composition/GameCompositionRoot.cs
d28dd8a1eaba5eeee206b03e77ababcf6f1d97b76b127cfaa5fef5417023e02c  Assets/_Project/Core/Composition/SceneBindings.cs
688d7315a24849b019e10d7ace7f78eabedca65e01f365b903f75b436af5e588  Assets/_Project/Domain/ContainerSaveSession.cs
1e6418e988b976c8f642c92ea81343b7540cb18e1a84bef0bdbe073f5bde6aa9  Assets/_Project/Domain/Expeditions/ExpeditionState.cs
63296eeb442baef8c1f1b6cff913604a13660f0713fafd332caba3fed79e7bf6  Assets/_Project/Domain/Npcs.meta
49cc433c3e230d40eee99ffbfdeebad659be549c92271c11ad9afbdc872693c4  Assets/_Project/Domain/Npcs/SecurityBotBrain.cs
3a2cf30246383a574da8a1cc0a27457b44617d4706a3af555e0236f29c913f56  Assets/_Project/Domain/Npcs/SecurityBotBrain.cs.meta
b33b193b365ffaa49d859ef38601ce97ad8fd08798915d10cee3b8421475188f  Assets/_Project/Domain/Restoration/CriticalComponentSourceState.cs
3e63e6ecd2d61c4691d91264b2f718f3a0c6945a2dc4ac0feb7d44f168d777ce  Assets/_Project/Editor/ItemCatalogValidator.cs
37397f161e0a605828c0cffaa0f9693df7dd4846dc194b77e63a83b8f59627e0  Assets/_Project/Editor/PhaseFiveFixtureBuilder.cs
6a78994ff5fb62ac1b6b5123c3c124a9f44057686d98ec41fc9b78f9bd8d4faa  Assets/_Project/Editor/RunLifecycleAuthoring.cs
400ee824e95ea9553de192bdc4056695d433d966df69450eb1018e8e02b3caf5  Assets/_Project/Editor/RunLifecycleAuthoring.cs.meta
44135512ceffde62b420e711e71a1bd95b2b05c5608134b037c6864317fe0635  Assets/_Project/Editor/RunLifecycleValidator.cs
53a15186a4266f8639d3758366c3c946af84352a6d5fa4cecc0724dba8792ad0  Assets/_Project/Editor/Sec0NpcBuilder.cs
db21562191c6ffee4a57796b2b3c02a9be9d1630f1f3b0e63a83dcdd18d4bfa2  Assets/_Project/Editor/Sec0NpcBuilder.cs.meta
f17d00e4e39257f1312abe517165723954d224f3130be8dc4cfcff4a5288ff74  Assets/_Project/Editor/WakeupValidation.cs
646956edb8b0df5f101467d7fcda976c1be752ded7ded0efce4f5ef7456ea070  Assets/_Project/Features/Creatures/Movement.cs
190fb740efffdbf78ddeb847a643d9127b58b82108d80d190ab117fc11b0b6de  Assets/_Project/Features/Items/Core/WorldPickup.cs
b84646fc354b37fc0a7714720b628f77b96a5a395d2c8a0363e4a7d8ba4d9f4f  Assets/_Project/Features/Items/Definitions/Bandage.meta
2936c9c91a9f08f02048f4f41c850b46b1dee2973741bad0f7ac83ae06b2a8e1  Assets/_Project/Features/Items/Definitions/Bandage/Bandage.asset
a1895df66fcb761b4b6fca1829e848e1ed8a43a6e6022b5ce99bed91fb0b0ca7  Assets/_Project/Features/Items/Definitions/Bandage/Bandage.asset.meta
d4f934df58d365ca464b880ddd2aeebb62a5ceece959ae0df629077f42d69bb2  Assets/_Project/Features/Items/Definitions/Bandage/BandagePickup.asset
04cc66c46da67d6438811faacd9b56555b108ecadfd2f4da36aab2bc1ba6682d  Assets/_Project/Features/Items/Definitions/Bandage/BandagePickup.asset.meta
15b00f998e6ecfc108e3216ff436c274a8d4459c62f1bf239b7862f88751f323  Assets/_Project/Features/Items/Definitions/Bandage/PF_Bandage.prefab
7f2e4ff3762a3518d8bfaf14059f67bf6d19e7830cfa49196255cb40f70387c5  Assets/_Project/Features/Items/Definitions/Bandage/PF_Bandage.prefab.meta
9a7c26e1afaeaf9a49ad83106eb0c0c5c7b91cc684d451ac956165e535232ef2  Assets/_Project/Features/Items/Definitions/Ration.meta
25e603208c9ed3836e9206046344e67d94bc1e1bead74079d2dd3a211e10f2eb  Assets/_Project/Features/Items/Definitions/Ration/PF_Ration.prefab
f82563460d94d268bc601e35639a561bffcb40b4ea30e14f3dbc2ab16b72e3c7  Assets/_Project/Features/Items/Definitions/Ration/PF_Ration.prefab.meta
8cdaf3e68edc63024cdd0222b5ee77bb430335c2ac218ee544ea6ecbbf8257dc  Assets/_Project/Features/Items/Definitions/Ration/Ration.asset
719572b8904c4da0fd6fd7115eb09d7cb0c729ad9f51536ccb6f0758c1f50f96  Assets/_Project/Features/Items/Definitions/Ration/Ration.asset.meta
2a630b71b8d9b0e13bae555366acd4516d46a370f219945e53adae13e29103ae  Assets/_Project/Features/Items/Definitions/Ration/RationPickup.asset
d0f51dd730ff501c28de41f8e731ae23fd73613afb582dcb5b1272e7d17d4bde  Assets/_Project/Features/Items/Definitions/Ration/RationPickup.asset.meta
b56fe776010bca42f67f889d7244ccb5f81a8a5da5c79f487e4affc9d8c4c05d  Assets/_Project/Features/Items/Resources/Database.asset
001fa499e4a46d21cf9efef72a72a3ef19c7475c18f159de8ed93d01cbfa086d  Assets/_Project/Features/Npcs.meta
721f40e4dc9fd3bbabd63a94dced2825686fc58363b3a493511e4f7595fe4132  Assets/_Project/Features/Npcs/Sec0.meta
07c7887eb3536b1118b39cb6e88a0a996d8aabc39b90def1e4736ae6df4132f4  Assets/_Project/Features/Npcs/Sec0/Animations.meta
1bc624c4f94107b7e33016ac5ed15c72f9abebd3c7d7583c871cb66844759a1e  Assets/_Project/Features/Npcs/Sec0/Animations/AlertBlink_east.anim
c992005274316d2b0df64de2bcad7cbeae49036883c846959dcc9d840df021da  Assets/_Project/Features/Npcs/Sec0/Animations/AlertBlink_east.anim.meta
9842912752ce3e5cd63f11fc7351021baa61532758d723e3ad08feaf38e113cd  Assets/_Project/Features/Npcs/Sec0/Animations/AlertBlink_north.anim
28cca4205611bc4e45e8065767404add0b4bc9ad267005caeeb3f172a7ef1840  Assets/_Project/Features/Npcs/Sec0/Animations/AlertBlink_north.anim.meta
395e84f4baebeddeab4f1d3021a7681c3e5af7e60a4812ad5dd788a55cf0a331  Assets/_Project/Features/Npcs/Sec0/Animations/AlertBlink_south.anim
d40277ec2e23168576ab3a59321f4bb67c4a40deb6011fe485704ccb5a366561  Assets/_Project/Features/Npcs/Sec0/Animations/AlertBlink_south.anim.meta
f17e8d3e5e269b3e4d78e9cc00f296a200661f8151050ed54013b358f0768e39  Assets/_Project/Features/Npcs/Sec0/Animations/AlertBlink_west.anim
5f78dab69fcc17126d5a8f225288ae4b6de907619ce62a17bbde83345d88d461  Assets/_Project/Features/Npcs/Sec0/Animations/AlertBlink_west.anim.meta
6462be3ac4603808a9e188c2605e25fa678bfce097997eacd6c2cd8e51564611  Assets/_Project/Features/Npcs/Sec0/Animations/Charge_east.anim
3b5f5c85b97725ccb8a7cc42472d61cf7a6f263e09a211158ad7ac7da3ec4e36  Assets/_Project/Features/Npcs/Sec0/Animations/Charge_east.anim.meta
18c3ce62c85e6227043f8af327bd5c1d321e50cce1b7b1622a36ebe6d303014e  Assets/_Project/Features/Npcs/Sec0/Animations/Charge_north.anim
501dc0833ecb52346078cfd1fa41eb3ab8c52ce7fa5f95b14540d5991ec9688b  Assets/_Project/Features/Npcs/Sec0/Animations/Charge_north.anim.meta
25fc3ae3d6da308448a18e3ebf5ccf35309e79d9b3a7005b31966b8361438aa3  Assets/_Project/Features/Npcs/Sec0/Animations/Charge_south.anim
22f730bfcbe81c1a2366e0ea089917955240850f6010487fa99874cadd6b96eb  Assets/_Project/Features/Npcs/Sec0/Animations/Charge_south.anim.meta
8a4b26f0ecd1097ad7ff37d8f01f95ee4797b12be9fb97a505499d797202f917  Assets/_Project/Features/Npcs/Sec0/Animations/Charge_west.anim
f52a06b24ecc1e85e988ca3b37ee3a1dfafde7fb06ce6115dc639b05778608a3  Assets/_Project/Features/Npcs/Sec0/Animations/Charge_west.anim.meta
c3b998df3e3e94c51e413e36b61d0dfd47b5c7843ee6a5d7a48b71568bf0acd8  Assets/_Project/Features/Npcs/Sec0/Animations/Fx_backup.anim
65e017da27bf2d46c0646236ed8c14eae1f8bc7b070b00e7661ab6e5fd0b32f4  Assets/_Project/Features/Npcs/Sec0/Animations/Fx_backup.anim.meta
6d3a98188b549b5eeef6608a101ca924dde03eaa345ff2bad5490f586c4830f0  Assets/_Project/Features/Npcs/Sec0/Animations/Fx_stun.anim
b82cd93de28937d5851dde8329fdd19a07585d4096f13ceda3a5fbafe94018c5  Assets/_Project/Features/Npcs/Sec0/Animations/Fx_stun.anim.meta
e31e5a928facae6a22dc6fc67318d9bdf2b951a88e136b10c785460ced372645  Assets/_Project/Features/Npcs/Sec0/Animations/Idle_alert_east.anim
d6def2f702fd0a183ce5a97668b2f1bf123593e3bb49450fd1ab09184180617d  Assets/_Project/Features/Npcs/Sec0/Animations/Idle_alert_east.anim.meta
c806f54b4ab973193bbb86938aed6184fcf54df6ed1133069a5eaf71079cdf75  Assets/_Project/Features/Npcs/Sec0/Animations/Idle_alert_north.anim
4d5249c0d41d9d3a35c8ab9488a8cd9c9862e983b1f04fee84741d041795a67e  Assets/_Project/Features/Npcs/Sec0/Animations/Idle_alert_north.anim.meta
94abcb51add22ea9b5df51715d1bba18b96da46474759572243a7c8d013e5b9a  Assets/_Project/Features/Npcs/Sec0/Animations/Idle_alert_south.anim
27124b42d4ab19d01b8b3cb32a66dfc2acad56038fdff7550333da0c96710f87  Assets/_Project/Features/Npcs/Sec0/Animations/Idle_alert_south.anim.meta
13ec8fe86707b8f3a6ef4e0305ce90cf73ad18c4826ca31fa4d24e203b1ccdf0  Assets/_Project/Features/Npcs/Sec0/Animations/Idle_alert_west.anim
39547f233ed165e65c307784a70f622a51c101be25ca48c22922e2d47f86990a  Assets/_Project/Features/Npcs/Sec0/Animations/Idle_alert_west.anim.meta
ff5ed2be01da88fc2ed79d017d2f864b1d6b8234ff1c3d28440f6e133df250f5  Assets/_Project/Features/Npcs/Sec0/Animations/Idle_hostile_east.anim
381771478276878021d68ec42d1e6dbdfc64fa7dfe1064418c8d5bb41603d4e6  Assets/_Project/Features/Npcs/Sec0/Animations/Idle_hostile_east.anim.meta
559ca16702de54ee44c96ba9e6353a1256e47f1fb4acc0575f7c7ac25ba3cb3f  Assets/_Project/Features/Npcs/Sec0/Animations/Idle_hostile_north.anim
b3097c4ef001659ecf4ef23a772f7b90ff8373ed47d3c217d03e626ae6d95273  Assets/_Project/Features/Npcs/Sec0/Animations/Idle_hostile_north.anim.meta
e7d97eb778e0415998f9223d658b47dc91e48f746adde6feb13b2ae4904deb46  Assets/_Project/Features/Npcs/Sec0/Animations/Idle_hostile_south.anim
e7d3849c769a36a62b195a1d657652aaf4299128d6db081ea0d297522d79ca53  Assets/_Project/Features/Npcs/Sec0/Animations/Idle_hostile_south.anim.meta
d813150169898811c76d27c9a747f48d0c97a3c4ea5a87e677929bfca03ab64f  Assets/_Project/Features/Npcs/Sec0/Animations/Idle_hostile_west.anim
2dcb6477ed9e62214acfe98e5124f5e17f1fdcf267c0b9acf00547b5429bd373  Assets/_Project/Features/Npcs/Sec0/Animations/Idle_hostile_west.anim.meta
eff318ece330e6cb0155b1f08983a8d568c0bbab08d4d4328ea16284cfa07ad7  Assets/_Project/Features/Npcs/Sec0/Animations/Idle_routine_east.anim
d50acd5201c82c44dc0667fc63b3778bf1782414e0d4ab7691101b2cac692dc4  Assets/_Project/Features/Npcs/Sec0/Animations/Idle_routine_east.anim.meta
17997d658aeb5a9edd5f339f4516a3dd84c126cf75558c7a92f1b43be6469999  Assets/_Project/Features/Npcs/Sec0/Animations/Idle_routine_north.anim
3e73a20ec97a4e6f110363d0ebdde5d42a5b5130c71fe98c5cf3cd07a00bbe4d  Assets/_Project/Features/Npcs/Sec0/Animations/Idle_routine_north.anim.meta
ac58350d31562f0b4b07a9a47e31e9aaf272f03cbac41854bfbfc7ecb37c43b0  Assets/_Project/Features/Npcs/Sec0/Animations/Idle_routine_south.anim
dbe64961f89f0c5b980d9cf05c2031d5845465db3f1865c19994228b67ee7149  Assets/_Project/Features/Npcs/Sec0/Animations/Idle_routine_south.anim.meta
6688745848df838af8d3339d2e5c12646791a47c4abfe1b6fa08d33efc80ce0e  Assets/_Project/Features/Npcs/Sec0/Animations/Idle_routine_west.anim
77e04cd8af54a6a2bada8230817c8c02022f453b03a6fc6c40a2f5e71157286b  Assets/_Project/Features/Npcs/Sec0/Animations/Idle_routine_west.anim.meta
db42d7640ffd4c7cc76c49fb6487ad6b2a65b89b9f6bd696b248f9bd13caefe7  Assets/_Project/Features/Npcs/Sec0/Animations/Move_alert_east.anim
c765c816878db41fc35e3dd105b0e056564c21bed4463437292cadb0f17d8561  Assets/_Project/Features/Npcs/Sec0/Animations/Move_alert_east.anim.meta
5b465ec0f8a5cf0bf6edef4bd5ae421571a17a11576b260eb40e888a445d7426  Assets/_Project/Features/Npcs/Sec0/Animations/Move_alert_north.anim
2e789c5034e1a8f25dcae5b0ef02b39099683a4acbde6f2f3a8a5d72b37eaf0c  Assets/_Project/Features/Npcs/Sec0/Animations/Move_alert_north.anim.meta
0fb5c5ff3456e95e5aad3885c23511eda524fb73600ced4539f644f486241080  Assets/_Project/Features/Npcs/Sec0/Animations/Move_alert_south.anim
f59353975de88ddf8ec4e73433bcf41c97c9461de8d5ad335b80221d9ab01dee  Assets/_Project/Features/Npcs/Sec0/Animations/Move_alert_south.anim.meta
9f5a772dcb8d68f101565302ff91df15693711fb14cb3b023e6437aa7775d4e1  Assets/_Project/Features/Npcs/Sec0/Animations/Move_alert_west.anim
dff423462841095f31aa5199d37b835c648c89d2c28472b241a2a568c405817a  Assets/_Project/Features/Npcs/Sec0/Animations/Move_alert_west.anim.meta
ae8bf445ccb80fd4223e21f6def15cf2c7552f61af353dba872a13f05bb93831  Assets/_Project/Features/Npcs/Sec0/Animations/Move_hostile_east.anim
44eed2936945c75ffaad965398ef62eec1ca783db603a76c1bb059055374e88b  Assets/_Project/Features/Npcs/Sec0/Animations/Move_hostile_east.anim.meta
395db2aea44966e108bd4947e16f414448f6900bd2cec4ceefc58452a3efa629  Assets/_Project/Features/Npcs/Sec0/Animations/Move_hostile_north.anim
a2639f88bfe1b082a9f062eea10a6974d161f02edb4008cec3b68993e191f4a3  Assets/_Project/Features/Npcs/Sec0/Animations/Move_hostile_north.anim.meta
061ff2dde2ec6d28b01efe4cce0c2f3e249dfae9e89b19627cdf8d758ffadfc6  Assets/_Project/Features/Npcs/Sec0/Animations/Move_hostile_south.anim
01c46c7538ec4919673b243a5096c3a926a8e5c334389691a71575b44c377a70  Assets/_Project/Features/Npcs/Sec0/Animations/Move_hostile_south.anim.meta
4f16d8a8f5090d72bac6bdf70ee7ce69dbe8d5b65929b0e37202c393bf57d6e8  Assets/_Project/Features/Npcs/Sec0/Animations/Move_hostile_west.anim
f106c708c53011893c0a216de6cdda52766314f91be6c35a236b4c45563604d2  Assets/_Project/Features/Npcs/Sec0/Animations/Move_hostile_west.anim.meta
a0db90d4773a930742607bb9edc2b266bccc6d1d30d5d2ec1fad0bb21daa2560  Assets/_Project/Features/Npcs/Sec0/Animations/Move_routine_east.anim
a0e23766f418fd2fca03172aaaeea69c2a7779442a1262a77422f432cd93dc6a  Assets/_Project/Features/Npcs/Sec0/Animations/Move_routine_east.anim.meta
832ca0be9349a8c7d779d08880105ece818e6613a14102134cdd3723e3d77e0f  Assets/_Project/Features/Npcs/Sec0/Animations/Move_routine_north.anim
5f32e00a1b57254e038b5d3f3aa593281b4996e50014941b09a025f973ca6ee7  Assets/_Project/Features/Npcs/Sec0/Animations/Move_routine_north.anim.meta
f0885571d3823797bc6f8159fa87749f64eb0ab06be0aa8959185b4c32ea627e  Assets/_Project/Features/Npcs/Sec0/Animations/Move_routine_south.anim
744b1149c1a433c98d149f14af8dd8ba761fd2bf9029288e4deb2add6faf41bc  Assets/_Project/Features/Npcs/Sec0/Animations/Move_routine_south.anim.meta
d1c3037c6fa8742adef472b0d2967c09abeda9c9990dcedf068d2cde2e8cb0d9  Assets/_Project/Features/Npcs/Sec0/Animations/Move_routine_west.anim
72297d6cffda3b5ad58885d420927a38f52f9da1052206ee752905352f4928c8  Assets/_Project/Features/Npcs/Sec0/Animations/Move_routine_west.anim.meta
0fc53b4d6e83d060161ca397688d83b46dc0a8fbb95384db3430b17b30fbb098  Assets/_Project/Features/Npcs/Sec0/Animations/PF_Sec0BackupFx.controller
f08cbe5e5dbb157c2115096a616df4b0a78a9139e45d9506c3fee500f0583fed  Assets/_Project/Features/Npcs/Sec0/Animations/PF_Sec0BackupFx.controller.meta
57518fbef89395434f4b9447df3321291b34d5c8125dc9b6018d2f1125935db1  Assets/_Project/Features/Npcs/Sec0/Animations/PF_Sec0StunFx.controller
8f8203372d2c43bce55c2ac87df37b849ee606d2ce70cf09d585b38f9c0dca23  Assets/_Project/Features/Npcs/Sec0/Animations/PF_Sec0StunFx.controller.meta
5199ace283ffbbe7ca21b7fdc1b422259c41b2d7a881ee514005f6fd7a33c4d8  Assets/_Project/Features/Npcs/Sec0/Animations/PF_Sec0Visual.controller
9c7e93523a5c2e94a622d3f7e2bf116837abdce0a9e4f779d269ee8b09010a6b  Assets/_Project/Features/Npcs/Sec0/Animations/PF_Sec0Visual.controller.meta
8616a6f12a8dd66125a7f58c689163f128bf829bc48e03febe917559c7209b7e  Assets/_Project/Features/Npcs/Sec0/PF_Sec0.prefab
1ac94499b3e82b5d4f01280c389151631def74d2d39b545c7ae20fe39ccdad96  Assets/_Project/Features/Npcs/Sec0/PF_Sec0.prefab.meta
eff2d0b0bdcdb5f03b97c82013e141895d9165c9c2828759fc6ba3845d9c071a  Assets/_Project/Features/Npcs/Sec0/PF_Sec0BackupFx.prefab
d9893ed3317471b1d5e989ec336e4b515e511d3342f2b93f870d822d752fa394  Assets/_Project/Features/Npcs/Sec0/PF_Sec0BackupFx.prefab.meta
8db41cb910cf5e317740cdadc46400a9f157f819600c22c5533cdaa7e88b535d  Assets/_Project/Features/Npcs/Sec0/PF_Sec0StunFx.prefab
131944bcd9fb21ca88172f7f83d06f8e7aea57569db71158f94228a4fa12dc29  Assets/_Project/Features/Npcs/Sec0/PF_Sec0StunFx.prefab.meta
9c824fe416c899a10ff9b29b6a1c10ca4784432d0bce8976fb97231c0c2b6256  Assets/_Project/Features/Npcs/Sec0/PF_Sec0Visual.prefab
d5090179824fd8b7e731d9f83516d488be925b91bec9229b98e9553c87970320  Assets/_Project/Features/Npcs/Sec0/PF_Sec0Visual.prefab.meta
8b84ae5fe8f33f77c1f2972f3f9e4889be7317e96c78efe49d06fbd006ec3a13  Assets/_Project/Features/Npcs/Sec0/README.md
cf5bd745ad95680a240d17dab0c3fe3fd6b2c5306b0a0528d8089704899cb512  Assets/_Project/Features/Npcs/Sec0/README.md.meta
0c47b071c5846d74bd3c3e240b2bec64c26b4a3ee36498a1d1a13d7b99655219  Assets/_Project/Features/Npcs/Sec0/Runtime.meta
912ef96ca69da46849500715d1d71c4996471ffbd26f2a63a63ba63651a74d31  Assets/_Project/Features/Npcs/Sec0/Runtime/PlayerStunReceiver.cs
aba02587566925789cb27f227d045674633317e8a34f6786c6d88c226825e5c2  Assets/_Project/Features/Npcs/Sec0/Runtime/PlayerStunReceiver.cs.meta
df6359f34b142e9fa944b322fd77da9fe889b97db60cddb540951ce79bd9a434  Assets/_Project/Features/Npcs/Sec0/Runtime/Sec0Composition.cs
86a1635ab467cd8adcc87a7c1dbdab4c53af90a79e979e1d92385c83fdf9b6f1  Assets/_Project/Features/Npcs/Sec0/Runtime/Sec0Composition.cs.meta
0e9d8c868a58205b163a3de26f009446ab47814647dc2e080bafd4151439ce0f  Assets/_Project/Features/Npcs/Sec0/Runtime/Sec0Controller.cs
eeb157ef1c16158e829c8b74fb68049d67d0bb68bf84cdd3db76dbd507c1d041  Assets/_Project/Features/Npcs/Sec0/Runtime/Sec0Controller.cs.meta
b9085f53449ba6c6cbd4370c9c3071ca1df98fb8cf2e7cfa9293cbb2182fe6e8  Assets/_Project/Features/Npcs/Sec0/Runtime/Sec0ExpeditionEncounter.cs
c8e168e4080945e29c1d8cd9013aaf4cc008272bee64597e9116b6e1fb829962  Assets/_Project/Features/Npcs/Sec0/Runtime/Sec0ExpeditionEncounter.cs.meta
c017e3759dac8d8bdfa4ae0e2a10e555c7b50b31d26708a2033e49b67acb0606  Assets/_Project/Features/Npcs/Sec0/Runtime/Sec0Navigator.cs
986f531ea67465d151ab3b14e830c6bf08524a2bfac7d9ec989154b4deef8106  Assets/_Project/Features/Npcs/Sec0/Runtime/Sec0Navigator.cs.meta
59f833341cb3bf3fbf0f18c50128e99dedb0a02d3c6613737721495770fe6c0c  Assets/_Project/Features/Npcs/Sec0/Sec0Signal.mat
e903f3d37e81e85df7905979d3c135061e2ab4963269e9cc88cb0be54b61a5f0  Assets/_Project/Features/Npcs/Sec0/Sec0Signal.mat.meta
038b55cf2cb080dbdd7e32a25889dbad325e99dd7a71788c3e67e48bbe74231f  Assets/_Project/Features/Npcs/Sec0/Sprites.meta
62794194dd56d97a0b454185fc65960557c6b5b70628761be9ad293e3c6c30c6  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_east_0.png
67384f9d231e941fd20eb6d58a73882c10a6bde638464515fb86eb1a7312bfd4  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_east_0.png.meta
240805e0987bb3a9c3fb15eef22dbf1bc8adc4cb1bdec2d6f1732072ffd5bb95  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_east_1.png
271042f23e665ec23865eb96a9e690d2b051350574baabe8644a7e30a47a73db  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_east_1.png.meta
1d58cb7564a1bf9a1febfbf4c0a61345b01e731ce4a8438d6897b6d039547320  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_east_2.png
f6e5509f496f00cf15041ab3112ffefe65eca83b3ca344cd7c15ea93d3aef9e8  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_east_2.png.meta
dcb328836f0c029a983fab5102ebf8b3a9eb6457a8c182392ba39cceac64a743  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_east_3.png
420d553bf1f2120f6930f5c920b9e75a810fa8a8546a22d7dcefc49a78c722b3  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_east_3.png.meta
d5193de522b265f639713b5f0d578886e9e4ec17de2d08054c690a2e79f4de13  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_north_0.png
706213f7afab071d806f7587cbdd860dc8dac13fef1109c9c0320effbf6519f8  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_north_0.png.meta
4dc15ab4b258821475425ed85fa3be61bac6101aa1669fcdc93a44ab9a12a172  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_north_1.png
3b90db9ab75b35196718f8b9d7293c05261b46fb6c91e85b6af8c9b00e712459  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_north_1.png.meta
3507ec5570f42e54264333a32809f86b4e7b3507381f52979d275456d02f261b  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_north_2.png
b403b5c5f1c4117e0c8ce7d542d28b7f2d4164fe4c4015e11b3e4f7c9e3d3356  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_north_2.png.meta
d5d1e09445ce1871ea41c68039cc1fd5d9546122c24c68a144fe324e42cbaf84  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_north_3.png
dfc5b1d279a690fbad041162e7ba5db8d189ce89b35ed5b0c9224adffee5e28b  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_north_3.png.meta
1b6e3d9c79efb24af942a4dacffdc12d5ac32648967121b73d4a9ddbde43547f  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_south_0.png
1e76990f6ffe6375220cfd1bfd825860ece7cf0d166079ac02431d8b6c4538e4  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_south_0.png.meta
c42ba421111ece7e67c07c786e2de25fce05b27ce5354d2e7dd66a6872bef8cf  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_south_1.png
a0cf63ba5b0835acc78a561088d1a5f915ce50daa90f485ecec2948d751117de  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_south_1.png.meta
89dfe8bc0d29fb14bc8d5b4ee5febbbeacfde64e39a015918cbeceb2d8cd219e  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_south_2.png
aad47ce3dbe999343590afadd26003b77b17728990da683b2654a594139fb0e3  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_south_2.png.meta
106af2ee5bd3b2af53eb705b6916393d319b94c4b2b9c9ca243b02aa5b10cf7e  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_south_3.png
b60975d622d72738a64c8aa6784115fb5be0048e1e393d6b88a29869f56e459a  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_south_3.png.meta
20822a47998d3f254aedf17bd5af98cf9a4b23fceb65e0dc4261a40eb58df1ea  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_west_0.png
02677f54f3346541523c371a1ddc9e95ce5bb01e27589ee98153a4ea7ea19998  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_west_0.png.meta
57c164cf50ea4514aceda339939f26fbed47446b36e231c7156db19d7f993094  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_west_1.png
f89f7cbaf70c457badd3945d49fc77f2939d27ba85b37e2123d55b01cf41c6f7  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_west_1.png.meta
355d0309c5449c00b002eae64489bc42a2dd99a73557ada8f550759e93f6a445  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_west_2.png
7b4cafa21f74cf93d682a576f9f43acfdf0a1f54ec5d4c3c170b6a8287a19702  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_west_2.png.meta
f9622b258f155a204c1835ec7d550462f0c985d7bf15e2093790a61085a79e00  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_west_3.png
74d58fc4ec0dddc6e2c9e799cccd7fea29c12e350773496d85f54f089900772e  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_west_3.png.meta
b0b6d3fc7c990afa4fc0e8ccf547b72a525bec7b2086ecc53868004a93659d59  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_backup_fx_0.png
9a74b83c08bdbd0569a246fedddc9cd9ac1262f88b29288381a8c1ed6399c60c  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_backup_fx_0.png.meta
2deedd54ec93f904c96250233450bf43368791e29867bb1c3e68dd7b2f77e7b4  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_backup_fx_1.png
666c7105c901dd5a648d5b493a56adef040b5acf160a4fa2dee961a3fef07081  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_backup_fx_1.png.meta
14404e3fa8bf4684a7d33e01836e4cbb606c08c7741cae2ad01bfb2d2135de94  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_backup_fx_2.png
c6c641ca7b016a171b7613031f66f5a74c69c2f049dcf62dc0bebe0364706149  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_backup_fx_2.png.meta
7b76e461eca260f84646434dbd3b9635e4247a528134bcdae45c65b28d65b5b1  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_backup_fx_3.png
7b6afb5118f2b3ae7bc49f2e210fc0e83293902a717c76562b3f63283b07bf09  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_backup_fx_3.png.meta
62794194dd56d97a0b454185fc65960557c6b5b70628761be9ad293e3c6c30c6  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_east_0.png
f5d3a21ea0c5a6ed2cf37b7c54ecc36b4e420f0cfe464970d2ec6757585d16e3  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_east_0.png.meta
38027d7526628cb860c0e98684a5103eb19ea3cafa9ced9543df00ad1e160eaa  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_east_1.png
3ae7498f8ae14210e0181aaf636cf93d295b62643033bd5efaafc07eec5f2000  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_east_1.png.meta
62794194dd56d97a0b454185fc65960557c6b5b70628761be9ad293e3c6c30c6  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_east_2.png
441c980e31da3e26c9e0d906106f6a3fc307819837500c2603aa86cfbeb08fe9  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_east_2.png.meta
38027d7526628cb860c0e98684a5103eb19ea3cafa9ced9543df00ad1e160eaa  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_east_3.png
a55f38e664f1a0727f223739293cde400ed0cb205bf4136b2bbd459bfdbab46d  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_east_3.png.meta
d5193de522b265f639713b5f0d578886e9e4ec17de2d08054c690a2e79f4de13  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_north_0.png
c67a17b010031140a1825e05f966701d4674c8cd471d9498c5e04bd66cb82ee2  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_north_0.png.meta
ca270bd9ac63a47ddb8c363601be7d36eebd9879420794afeadd89f9182deac5  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_north_1.png
59031921851c829d0803b8c25147eb4b57e6e21f99e8520ab7cdece1323c49ef  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_north_1.png.meta
d5193de522b265f639713b5f0d578886e9e4ec17de2d08054c690a2e79f4de13  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_north_2.png
8f588cbe352207a0c8334e9163a193f379679ff1f8214405892a5268959224f9  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_north_2.png.meta
ca270bd9ac63a47ddb8c363601be7d36eebd9879420794afeadd89f9182deac5  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_north_3.png
9e201bc9e4f5f0126597f7765d2fe4aa418958cb787257a4c24f060afeb386c3  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_north_3.png.meta
1b6e3d9c79efb24af942a4dacffdc12d5ac32648967121b73d4a9ddbde43547f  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_south_0.png
f49450c05573107cf4286bf861a328b2281644ddb54d1564bec8653c300dd3f2  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_south_0.png.meta
b0d8d36a893735187ba17ce8c6b58fa33b543dced5cba28502d9a1c3dea48c71  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_south_1.png
6dafa1159312deb3c952e8da4c9d386e8eb7023fd0504f6afa82731a7496925b  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_south_1.png.meta
1b6e3d9c79efb24af942a4dacffdc12d5ac32648967121b73d4a9ddbde43547f  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_south_2.png
d5770f8c55da6d51996aa10b630a097f17e3dcf3c1244fdf0f74ae0e275b2f1f  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_south_2.png.meta
b0d8d36a893735187ba17ce8c6b58fa33b543dced5cba28502d9a1c3dea48c71  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_south_3.png
3c4cdc50d036d0404dc586e3fafcdcc463ba069af266f856071c38ac07661cae  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_south_3.png.meta
20822a47998d3f254aedf17bd5af98cf9a4b23fceb65e0dc4261a40eb58df1ea  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_west_0.png
598779a83b184839c83560f091ba07220ae603bc67c046d508b88b2ff9579e29  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_west_0.png.meta
2e362567818645810033474dde2cc0c897aabaf1ac1d5fd1375878d8194573fc  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_west_1.png
2c09c34f99a62445d1327dc9515ce5959ce6a85b39b74bcf30996d520fddbe03  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_west_1.png.meta
20822a47998d3f254aedf17bd5af98cf9a4b23fceb65e0dc4261a40eb58df1ea  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_west_2.png
b7142bfa1e20fa93377f3e789c9b9abccb563df7b9ada169c6c13304d8a57cc5  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_west_2.png.meta
2e362567818645810033474dde2cc0c897aabaf1ac1d5fd1375878d8194573fc  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_west_3.png
de2047b6b9826744fba42dd85161bc1aba88452b4a64e2b3a6b6220518f8640b  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_west_3.png.meta
ec382c3eef610eda2505efcb4b60f2a21c6647192fbd07ffde3523dac653179f  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_east_0.png
1792bc78a1c23ba604981d14535967f0f9fea288491f8a3e424e87098c1b7f95  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_east_0.png.meta
01cec20e816265635b5c7894acc131d59f38b461dda934e0c7129e9f1ce97c9a  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_east_1.png
7ec7fb60b491c359e8a89cd327d790d465f3fce2f410116c543240820d0304bd  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_east_1.png.meta
96761652ee899efffb2b93600f462dcaa84f54178cb8c626d44406eb7b400544  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_east_2.png
8e7227b22d5b2689c6f4f91e346bc68a8a827c88c57fb58d20bdd911f3f731bd  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_east_2.png.meta
9a4aa120e9c58fbbb172131076ffe7b27d1b16c1c38778b900099a1b77c4e5e3  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_east_3.png
58b4f9410a15bd38ffdbd3a98f76acc16bd8e583a343b5cacc89cc707c45dfbe  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_east_3.png.meta
85e307239f82d84fe42bc0f00df8e622825df3f706f2fc65d70c1f01a81d2f96  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_north_0.png
57ea27de2c8edb7a5ee37c016de18cfb7d1e1faa24c22f35013553052fd2232f  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_north_0.png.meta
9fd5a8a8dbe7fed89bb8b3b2fc6123f18aaa62e44c74215ef334bb22b77b799b  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_north_1.png
fa1a84378b1dad064c51c640e1da2e9debff14def9122bbb1eb4034216c85470  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_north_1.png.meta
006d796e239abfe401206da893d8db858fbf6a96c3dc0e9745a625ac8859bb71  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_north_2.png
19ce49374a33bf87ebd49c1a4c13e8394aba97b87736ebd23c6c774df08fcfb1  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_north_2.png.meta
0b3f10172d89b1a9dc96a76b739d3a175eab20560923d53d7bfebae04a38acf8  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_north_3.png
1205649ac722503fa28a113c7963db9f3f90970b7b3d5334deb7bcdf177c1b8a  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_north_3.png.meta
9c2fae14223bf1040724559f837cbe73f4dc59f56a15baafff834657f60f4ade  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_south_0.png
2ea60507b243b6d421886a8a59430af1943330048a507e8142acb142850e987a  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_south_0.png.meta
077c8c6fbc57ddd962855e52534b5bae375b01dda321a6f0b8bea43fce1ecd28  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_south_1.png
b06976da0d72f590c9ff69e8f61e5e6dce88b8b78c220db29d8caa96c5b21b4b  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_south_1.png.meta
68b9f2be869fb77fa7318a2ce74ff087bf6e11c986d632438b0599c00e97cf55  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_south_2.png
481290d9ddcd911057499564368744672c55c2e77101d8e7567e6f3d040b75ec  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_south_2.png.meta
d177579872fa3221a59203e97d720aa1304af78e905f26cd3ed1e42befec463b  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_south_3.png
7f8fb136870c8c9c0225d6634767184d4335a18f46e6df35165ff90bd0e8b6ec  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_south_3.png.meta
8058280c938aae5134ab6f4074aa612836e55f76900f3644c6cca017a9311769  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_west_0.png
e04d5a7a6920a2a681ffe2b75dcd2c56f2281ee0b792e119b8d1b7a782c1b2f0  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_west_0.png.meta
2c9baf2bbdb245169703d15dc0fccc53e2b2117b087b454e5222dcb8d8de6ac0  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_west_1.png
d8956e4325c0d099b067701a89785f7e7a5be5c77c0d64af178394a85babb399  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_west_1.png.meta
bf53415eaaa5e2db03f02c640cdf172a39b43eb38435fbea27e64beef80460a5  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_west_2.png
201cb7c7e56241b215a88863c03d051f86459c96101c05cdfc3a59cb612c6bf1  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_west_2.png.meta
1d32a2a795a05b04bff7d494220958010b2a9884f3427c967e37b12bea72d3f6  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_west_3.png
59cff2b8e29a1d834f69d3cda420cc3367eaf1a621aee89132a02cc9846c1ae4  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_west_3.png.meta
e6f714e3059ebd93e5b301a355dff32eb7af7685dbcfee3583c48d3bd974660d  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_east_0.png
12e21cf011ed80d19898b16f6a1045d6410f14cb5d1894fc03cfcf69fe850a23  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_east_0.png.meta
f2f8e16dda07a687f552e60b83221ea880de7b0448e131169b8e9c226150eb7a  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_east_1.png
68129d453800b24c5f684f6f40721b26ddda1b123e56958f36b3bf0455738f9a  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_east_1.png.meta
5ece372060a3906eb4261b928366d951a626f4876971cbe00b4f06f0106f99a1  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_east_2.png
696815c845c1474776e1d8467e87fdb0aacd7355484786ed8b9aedb4303cc709  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_east_2.png.meta
46c9d6fd78d471202b41fa7a765875675e9ef861cb5f63a085039c77a1f26551  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_east_3.png
3d4bb5025f6544d4defa870bd849cbaadaad9dfaa75e236497d3fc46dbe68e9f  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_east_3.png.meta
6c83e58a456aa4fb896e02a6724e4b6b0c827715e5fbf1f8c02f83ac0e688852  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_north_0.png
00801bb9d07651fa5b17dbcf33e53ab708e776969a533c75aabe1c2a623e2f3a  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_north_0.png.meta
acc6f76327476b59335032ce705b6b473868a6c0a067231b5b580c00ef483521  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_north_1.png
16fd3166fb60fd16dbb0ba3cb6ca50648f5789f47978ec68b64748451aad49fe  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_north_1.png.meta
231277021704839b2d5adadeb49d9e6252a6ea159e6e3fd4b3fcf4877f2ab3d9  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_north_2.png
8578d0073562d6895270c5986facfe2f1f4c2458f284688b1672019e2e3682b5  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_north_2.png.meta
ce0805c327d263efce4466f7594b969e7a20906441304091e73c35b24c951adb  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_north_3.png
28fcd680a8b75cb3227d6f0277d67bd80c9cbd30965caca610623b14395d715a  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_north_3.png.meta
f88dc3aa3ba56d0665edcdfb645a52b0f49b42e2fe6c1d8da1cadd3082f7ef3f  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_south_0.png
5ba897b9ebe82363cbbe67d5c9c60b7a02e8b102d07fd043a85f9052a30761b3  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_south_0.png.meta
ed512800536281510072ec1fac37f63a6a3b496295722669e068fcf1e6158a43  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_south_1.png
f540bc8f41a17569e70a7316deace26fa09b078043ea2ab049a02e3854d7d572  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_south_1.png.meta
781ad4a24fb015dcd80f22a9974c4fd9a2571e36a206a48638fca7acecefcbe3  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_south_2.png
d950f62164b6d0b6df4b7eb4ba574079a06f367533104c8499573d176c0c4e4f  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_south_2.png.meta
146b10945a842222ff93f986ed38d8d9ba7bbe37af06c8e58ac8a4a811631a83  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_south_3.png
65ca65522ff99bf313223774ca6a3dc001b5617a9c0e9df8d18f058aebd172bb  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_south_3.png.meta
eaa6b4bb4507b466186a72833256ec9dcb0f683bd181bba22a1f5334078ae64d  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_west_0.png
6bf23e8e9fbcdaee84627473e71b903510f6e6546ee13225d60bf562aa18b43e  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_west_0.png.meta
141ab31fa970d12a3449b8ae122795509994da6bf7a3053da007045fa5379f13  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_west_1.png
f65e0bbebcc9befd6734fab2960b1d176b49f3e38ea5c693bb3484a92c0699bb  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_west_1.png.meta
2f2bad5e803e7778f3e326e46d975c8548126ff09c10c366a1c399aabf76c6b0  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_west_2.png
ed836da3ee7b9c59414c9f4ff1a3b246d6ed9694a7f47d07e665e22419e88b19  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_west_2.png.meta
a28b061487d5a605fd3c6ab09124a9f409cbeca8a86771990aef45ffd8932076  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_west_3.png
e0b578e1b3ac5e11b6f263fde30f5487d06a82030b42f284543e7363e02ceba0  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_west_3.png.meta
bc9ac1296b5a58850774505f17f7cc107056ceafe18c6dbe8c3fe7327f957967  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_stun_fx_0.png
74871bb873cd924eda4713dae88f20cecbe345d4ce2d1b99f006896e6aa30a05  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_stun_fx_0.png.meta
ed6b6ba6b613586af60d3eda7578b8e00ab65ee6e2a4dc5af85c4a3d0c08f2a9  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_stun_fx_1.png
c0c9b3fb0e4d5a7afda6e0c49536232ac1133eea0e875b3c7467708792275d96  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_stun_fx_1.png.meta
b2f873459cffb405285aa358c928f35edb0a0dd99a63eff768dcf817c5cb0616  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_stun_fx_2.png
a5280cf0023a53316c2d58d9ba5fc8a7dc1f8979db2cb7ef782517a2b5280571  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_stun_fx_2.png.meta
6e50c7e149c660502039d558280c121d3a30f78a1af613c38219b6dddb69f97a  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_stun_fx_3.png
6e53665ffffd761c7aa92fb5261383f587f353200cee269a09d4531049273285  Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_stun_fx_3.png.meta
c5d35d7fdf74b06246edbf681cacd22678e305d93e610d0d5d168cfb6765be3e  Assets/_Project/Features/Player/Succession/SurvivorAvatarAdapter.cs
59b68114b684d76c3e1a198e3e75dae1e8cc754a4553702188078429867e369c  Assets/_Project/Features/Player/Succession/SurvivorSuccessionComposition.cs
28e02047bd64d56b49a0caaed17e978c33aed433c93925bbbe9a9710524bbe16  Assets/_Project/Features/Player/Vitals/PlayerVitalController.cs
c921862b833eb1e2802cfae484dbefbef743a5b4c388d47f439d5f1e8db23601  Assets/_Project/Features/Player/Vitals/Views/PlayerVitalsHud.prefab
f3b5bc2dabd06e4658e5f8ce48cccdb198c43323d7d092ae93c25250eaa0f055  Assets/_Project/Features/Player/Vitals/Views/PlayerVitalsHud.prefab.meta
0b3751897b33b88b01f26144011446498dba9f6d3ca493e96fbf9a77a13c8850  Assets/_Project/Features/World/Expedition/ExpeditionHazard.cs
d7f0f0307737c7086c0b0c60cc0c2dd57d182b2705c1550ed33ce5dc2bd9be24  Assets/_Project/Features/World/Expedition/ExpeditionHazard.cs.meta
51e0d2719423b9aedd1506cc52dd17641d39ac186988966c8de74be461b351d3  Assets/_Project/Features/World/Expedition/ExpeditionSupplySource.cs
c2f4a559f76289c886c86f678116d9b996b1423aa168238883a4e8ecc39ff716  Assets/_Project/Features/World/Restoration/CriticalComponentSource.cs
f33dd1541d49ae562c76733d43e1803427c9372381ee14f7df0a246f6e5dd8dd  Assets/_Project/Features/World/ScrubberEncounter/ScrubberComposition.cs
922e34d35b88ff279d40b5bda2b4969e544f2e4c8df075f6f2c89ba160f97f1c  Assets/_Project/Features/World/ScrubberEncounter/ScrubberEquipment.cs
103fc78f0bae1b5bc627e77641afeb335a7ff55e714745814d0e0b9257dd14a4  Assets/_Project/Features/World/SolitudeStart/SolitudeSlidingDoor.cs
4ce21ff4da9d422a9e3a3ee897921b0cf98b141614214db8a6d39bf7e01526c9  Assets/_Project/Scenes/Demos/Sec0Patrol.unity
d8970958bac7eb7199cac00c4d8c25ff91bfc214ccd6b8f76cff1ecd1cbbeee0  Assets/_Project/Scenes/Demos/Sec0Patrol.unity.meta
1732ec878ff74d452ebd9c637e11027d9b57444e0246f1aa14dd8161ebae2505  Assets/_Project/Scenes/Wakeup.unity
f0bced502c1ee0c959578ac8567e28fa696f4008d33899cdb498616d50fe268e  Assets/_Project/Systems/Containers/Views/ContainerView.cs
85a1ef21bc362d66d3810969a78f02619fd0163699d729f60540ad785ef33c2d  Assets/_Project/Tests/Domain/SecurityBotBrainTests.cs
5ad5aeb0908177d5155a45363dd537093cc18e2e28239ea54c4c20a6c20ca091  Assets/_Project/Tests/Domain/SecurityBotBrainTests.cs.meta
f40405717b08135894a126957efc8b19b0c40585b9373887c0d2bb1496dcec0d  Assets/_Project/Tests/Editor/RunLifecycleAuthoringTests.cs
0278c285068ce8dc292c91d7950bdb0cdcfee27300c6fd1454185262230cd81a  Assets/_Project/Tests/Editor/RunLifecycleAuthoringTests.cs.meta
514064ea4f676f8f1e006775a84e63e76b02b1104c288eb35cf94fdebcc15f04  Assets/_Project/Tests/Editor/ScrubberEncounterAuthoringTests.cs
20c1e7dacbfddd9c05a15d4b06273dedebc54465423d816af535c7788ce7c3e1  Assets/_Project/Tests/Editor/ScrubberTaskSourceTests.cs
c847775cac1fae70a7150246042ea00694f196003294e6112c37aff85dc1ddad  Assets/_Project/Tests/PlayMode/PlayerVitalsSliceTests.cs
de7f57320bebd9307b36a72c157464fbaee096cc5f6dbdad600eb44e7bc2e08b  Assets/_Project/Tests/PlayMode/RunLifecycleEncounterTests.cs
392fc1b10f31edb72521a79017b9ec62964bca8f1c79b67e68b8ff9b0beca4ce  Assets/_Project/Tests/PlayMode/RunLifecycleEncounterTests.cs.meta
a6da7bbc9db81e9ae5db4e9765da6d25bd697c2001418e65f733d15b781a61af  Assets/_Project/Tests/PlayMode/RunLifecycleRefreshTests.cs
36f6d15856d511d18a4306b26b34f29261a5df1e64f0afbb85e9664695e7a67a  Assets/_Project/Tests/PlayMode/RunLifecycleShipPersistenceTests.cs
be85f163a862a034062b24cae5a072a7d76af129f755db707aacffce6f789ac1  Assets/_Project/Tests/PlayMode/RunLifecycleSliceTests.cs
fd3ae8c8a1c9f1a8e44e4dfc2044a2554a42e01c27d2f35977c45ddafbc403c0  Assets/_Project/Tests/PlayMode/RunLifecycleSliceTests.cs.meta
6ac7ecd50c7905ed7374baeb94de130f5a78b1badde5f5bc3dc7a0a4d83f166f  Assets/_Project/Tests/PlayMode/Sec0EncounterTests.cs
dd8774231dc0ec3fb08c989bc64ad85bc5be2fb2ea47604945724a667c4747a2  Assets/_Project/Tests/PlayMode/Sec0EncounterTests.cs.meta
cb8284df7f76a5e944b3aa712cc22d560c4f2682caec3802f7ffe1af8684414a  Assets/_Project/Tests/PlayMode/Sec0RuntimeTests.cs
6c017708019dcee2feb0c714e14cad2ea5da15758606396f7ed4abbc422d4f8c  Assets/_Project/Tests/PlayMode/Sec0RuntimeTests.cs.meta
564b3e6fd0eaa012bf1a086bfb9813a8a3d1a2da2d5816373e23cfc5dcf351fc  README.md
```

