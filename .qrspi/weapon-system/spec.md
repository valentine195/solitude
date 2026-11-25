# QRSPI Spec: weapon-system

## Inputs

Project root: `/Users/jeremyvalentine/SOLITUDE`. Digests identify exact input bytes, not approval of this specification.

| Input | Repository-relative path | SHA-256 |
| --- | --- | --- |
| Settled request | `.qrspi/weapon-system/request.md` | `35729bb8e0e5ac0785a8ae8e8dfeb55ffd621bb5af35836f7fcc4fbfcf5461c2` |
| Refined questions | `.qrspi/weapon-system/queries.md` | `0786e3ab55ca32645e579cfbd6c8bcbee234af6dd21be2fdd1a8212e348698c6` |
| Existing source research | `.qrspi/weapon-system/research.md` | `044c7f54d3dccd138a523de9472922db4a9dc50d1df950bc2f6e5395dc18567d` |
| Approved approach, including recorded decision | `.qrspi/weapon-system/approach.md` | `366905bdc4e5d90108ba79c0cc0897bb7bd0f9d583b9652b0eb1968d2101f74a` |

The human selected Option A from proposal revision `ccc0047006097ec159049ab7221b4858716f5506148832da590d27d2ff8792fd` and authorized Spec. The approach digest above includes only the subsequent decision record in addition to that reviewed proposal.

The research's historical New Questions entry asks which enemy is intended. The request's Clarifications explicitly answers SEC-0. Query refinement changed only two enemy references to SEC-0; it introduced no additional factual question. The human explicitly directed moving to proposal using the existing research. This specification therefore uses that research without pretending a second Research pass occurred. The earlier Query ambient-history limitation was accepted by explicit human override; it does not establish hard tool isolation.

## Revision

Initial specification. Criteria S1–S32 are new; none are changed or retired. This document defines the behavioral delta and public responsibilities, not implementation steps. No source changes, tests, Unity gameplay checks or performance measurements have run for this feature.

## Selected scope and before/after

Before: items have stable IDs, presentation, stacking and consumable metadata; containers own inventory/hotbar contents and selection is transient. Use activates a selected consumable and Interact activates environmental targets. Player vitals expose damage, but there is no shared damage-recipient boundary. SEC-0 pursues and fires a nonlethal stun; it has no incoming health or defeat route.

After: the selected hotbar item exposes configured item actions and gameplay tags. A fresh production run starts with a selected maintenance wrench. A separate primary-action input performs its directional melee attack. SEC-0 receives configured damage, provides restrained feedback, and becomes defeated at zero health. Existing inventory ownership, consumable use, interaction, player vitals and SEC-0 outgoing stun retain their responsibilities.

Equipment means the selected hotbar item for this slice. Selecting another slot changes equipment; selecting a consumable does not leave an independently equipped wrench. Independent equipment state, persisted selected-slot restoration and a full equipment UI are outside scope.

## Gameplay-tag contract

The registry is a general SOLITUDE capability, independent of combat ownership. A definition has stable serialized identity, an inspectable name, and at most one parent. The hierarchy is a rooted forest. Readable full names use dot-separated segments, for example `Weapon.Melee`; names are case-sensitive and unambiguous within the registry. Segments begin with a letter and contain letters, digits or underscores. Serialized assignments refer to definition identity, not display-name comparisons or Unity GameObject.tag. Runtime-local lookup indices are not durable identities.

Changing a display label, asset filename or asset location while preserving identity must preserve references. Changing a parent deliberately changes hierarchical matching, not which definition an assignment identifies. Registry validation reports null/unregistered references, empty or duplicate identities, malformed names, duplicate full names, self-parenting and parent cycles. Duplicate registry entries and duplicate authored set assignments are reported rather than accepted as separate meanings. Invalid required registry/action authoring prevents the affected feature from becoming gameplay-ready with an actionable diagnostic.

An item or optional tagged entity can hold multiple definitions. A damageable entity does not need tags. A tag used in an action's damage classification must be authored for that action; item classifications do not automatically propagate into damage events.

Matching direction is explicit:

| Owned tag | Requested tag | Exact | Hierarchical |
| --- | --- | --- | --- |
| `Weapon.Melee` | `Weapon.Melee` | true | true |
| `Weapon.Melee` | `Weapon` | false | true |
| `Weapon` | `Weapon.Melee` | false | false |
| `Tool.Mechanical` | `Weapon` | false | false |

Queries have required, any-of and excluded groups and an explicit exact/hierarchical match mode. A query succeeds when every required condition matches, at least one any-of condition matches when that group is nonempty, and no excluded condition matches. Conflicting required/excluded conditions yield false; they do not select an alternative behavior.

Direct collection predicates and whole-query empty behavior are distinguished:

| Operation on an empty requested list | Result |
| --- | --- |
| HasAll | true |
| HasAny | false |
| HasNone | true |
| Whole query with all groups empty | true, including for an empty entity tag set |

An empty any-of group adds no restriction to a whole query. An empty entity set fails any nonempty required or any-of group and passes excluded-only groups. Missing tag references are invalid configuration, not empty conditions. Tags may gate eligibility but never choose melee versus another concrete action implementation.

## Item and action contracts

Existing ItemDefinition identity, name, description, icon, pickup visual, stacking and consumable data remain usable. Additive configuration exposes multiple gameplay tags, authored action bindings, and optional held/swing presentation. An action binding identifies a logical slot: Primary, with space for Secondary and Contextual bindings. Only Primary melee is implemented or bound to gameplay input now. Environmental Interact does not fall back to Primary.

An item without actions remains a valid existing item. Requesting an absent slot safely returns unavailable; it neither attacks nor consumes the item. A second test-only melee item may reuse the same behavior with different numeric configuration. It is not an additional playable weapon.

Public responsibilities are deliberately small:

| Boundary | Observable contract |
| --- | --- |
| Equipped-item view | Resolves the selected hotbar slot to its current item/actions and an ownership/revision-aware binding; no item means no available action. |
| Action definition/configuration | Describes behavior and eligibility without mutable per-actor execution state; it is independent of concrete item IDs. |
| Execution context | Identifies actor, equipped item/binding, requested action slot and normalized action direction. |
| Actor action runner | Accepts or rejects a request, owns at most one execution, advances it during gameplay and interrupts/releases it idempotently. It does not branch on item names/tags to choose behavior. |
| Action execution | Owns its own progress, results and cleanup; melee owns its phases. A future non-melee execution may have a different lifecycle without changes to the runner, movement controller or melee implementation. |
| Damage delivery | Accepts a typed damage context, resolves eligibility/amount separately from geometry, and reports Applied or a rejection without inventing a second health store. |

Shared ScriptableObjects contain configuration only. Running actors do not share elapsed time, hit recipients, facing, current equipment binding, feedback instances or cleanup resources through an asset. Releasing one actor cannot change another actor's attack. Exact C# names and signatures are implementation design, subject to these contracts and existing assembly boundaries.

## Input, actor eligibility and movement

Add Primary Action on left mouse button and gamepad right trigger. Use remains F/gamepad west; Interact remains E/gamepad east. Existing hotbar selection controls remain. Primary obeys the existing readiness, UI-consumption, modal/pause and fresh-input rules. A press consumed by UI does not also attack. Holding the button does not auto-repeat; a new accepted press is required for each attack. Controls held across policy/lifecycle resets must be released and pressed again.

The player cannot start or continue an attack when health is zero, the current survivor/actor is unavailable, or the existing stun receiver reports incapacitation. Appropriate configured state-tag exclusions can additionally reject an actor without replacing these existing authorities. The attack adds no player damage source or new automatic succession trigger.

Direction is the actor's last nonzero movement direction using the current movement convention, with an authored initial direction of down. There is no new mouse aim or separate aiming mode. A swing captures direction at start; subsequent movement can change facing for the next swing but does not rotate the current attack arc. Movement and sprite orientation remain coherent with that choice.

The wrench applies a configurable movement speed factor, initially 0.65, while its execution is active. Existing full movement blocks take precedence. Normal movement resumes after completion/interruption without overriding another movement restriction. The movement component receives generic restrictions and contains no wrench-specific combat behavior.

## Wrench configuration and melee lifecycle

The maintenance wrench is a nonstackable item asset with one Primary melee action. It has no ammunition or durability state/cost. Its six item tags are `Item.Weapon`, `Item.Tool`, `Item.Improvised`, `Weapon.Melee`, `Damage.Blunt`, and `Tool.Mechanical`. Required parent definitions are registered. The action's damage tags explicitly contain `Damage.Blunt`.

| Authored setting | Initial value |
| --- | --- |
| Base damage | 10 |
| Range | 1.25 tiles; 1.25 Unity units in the current one-unit grid encounter |
| Arc | 90 degrees total, centered on captured direction |
| Windup | 0.12 seconds |
| Active duration | 0.10 seconds |
| Recovery | 0.33 seconds |
| Total committed duration | 0.55 seconds |
| Movement factor | 0.65 |
| Knockback distance | 0.10 tile |
| Knockback duration | 0.08 seconds |
| Enemy flash duration | 0.08 seconds |

These are configuration, not constants embedded in item-specific behavior. Damage/range must be finite and positive; arc must be finite and in (0, 360]; windup/recovery must be finite and nonnegative; active duration must be finite and positive; movement factor must be in [0, 1]; knockback distance must be finite and nonnegative and its nonzero displacement requires positive finite duration. Invalid authored configuration is diagnosed before execution. Range retains explicit tile/world scaling rather than assuming every later scene uses one-unit cells.

For the default swing, elapsed gameplay time in [0, .12) is Windup; [.12, .22) is Active; [.22, .55) is Recovery; at .55 it is Idle. Damage evaluation occurs only while processing the active interval. Windup never damages; Recovery never damages. Animation follows these phases and does not authorize hits.

The same elapsed gameplay duration yields the same phase regardless of subdivision. An update crossing the whole active interval must process that interval before completion, rather than silently skipping all hit evaluation. For a static valid target, one .55-second advance and sufficiently subdivided advances both result in one hit and Idle. Physics results use the sampled world state; this is not a guarantee of reconstructing targets' unsampled past motion.

Pause and gameplay-blocking screens freeze progress and hit evaluation and reject new requests. Owner/scene invalidation cancels instead of freezing. Death, incapacitation, disable, unequip, selected-slot replacement (even with another copy of the same definition), owner generation change and explicit interruption cancel execution, release presentation/movement resources, and prevent subsequent damage from that execution. Cancellation on the same living actor cannot erase the swing's remaining committed attack lockout; rapidly switching away and back cannot bypass the original .55-second commitment. Retiring an actor discards its timers rather than transferring a swing or lockout to a successor.

## Target geometry and damage

The attack origin is the configured actor attack point, defaulting to the actor movement/feet position used in the encounter. Reach is measured to eligible target collider geometry, not arbitrary sprite pivots. A target is geometrically eligible when an actual collider point is within the configured radius and forward sector and can be reached without crossing configured blocking geometry. A candidate's membership in a broad nearby set alone is not a hit. Range and arc boundaries are inclusive with tolerance no greater than 0.0001 world units and 0.001 degrees; a collider enclosing the origin may qualify without a meaningful angle. Tests include large/offset colliders so geometry cannot accidentally depend only on their centers or pivot positions.

Ignore trigger-only interaction sensors, self and child self-colliders. Multiple physical colliders resolving to one authoritative recipient count as one target. A swing can damage several distinct valid recipients, each at most once across the entire active interval. Rejected recipients do not produce successful-hit feedback. Multiple active samples never repeatedly damage one recipient.

Blocking checks use actual solid colliders and configured physics filters. World geometry between attack origin and the qualifying contact blocks that contact; a wholly occluded target is not hit. Ignore the attacker's shapes, the tested target's shapes and other recognized damage recipients as world blockers so cleaving multiple targets remains possible. Other solid world objects can block even when they are not damageable. Existing layer assignments are reused. Visual wall artwork without a blocking collider does not promise obstruction. Invalid/saturated query coverage produces a conservative no-hit result and a diagnostic, not an out-of-bounds error or an arbitrary claimed complete hit set.

The damage context identifies finite positive base amount, attacker, target recipient, source item and action, impact position, captured attack direction and damage tags. The wrench event carries `Damage.Blunt`; it does not carry all tool/item tags implicitly. Eligibility and amount resolution are separate from geometric detection. Default eligible untagged recipients receive configured damage; optional tag queries can restrict eligibility. No resistance, armor, status-effect or modifier framework is added.

Player damage, when exercised through the shared boundary, delegates to PlayerVitalsController's existing authoritative damage path and recovery rules. The feature adds no parallel player health. SEC-0 receives only the health needed for incoming damage and defeat, initially 40 maximum/current health. Four successful default hits produce 40 → 30 → 20 → 10 → 0. Invalid damage and damage to defeated/unavailable recipients are rejected. Health never goes negative; defeat is emitted once.

Defeated SEC-0 stops navigation, windup and firing, releases outgoing pulse resources, and ceases live collision/target participation. Surviving SEC-0 retains its existing detection, pursuit and nonlethal outgoing stun rules. No loot or invented drop behavior is added. Health is runtime state for the instantiated encounter; this feature does not add enemy persistence/respawn rules. The existing disposable demo resets on reload.

## Feedback and presentation

The wrench has an industrial dark-metal placeholder silhouette with restrained orange marking and compact phase-driven swing compatible with the current player sprites. Production artwork is not a prerequisite.

Every accepted swing completes its visual lifecycle even on a miss. Configured swing audio may play once per swing. Applied damage causes a brief recipient flash, a small contact effect at the reported impact point, optional configured impact audio and subtle supported knockback. SEC-0 supports the initial restrained displacement without crossing blockers or racing its normal movement authority. A recipient lacking knockback support still receives accepted damage.

Unconfigured visual/audio assets safely omit that optional presentation. Missing optional feedback never cancels valid gameplay or throws. At least the default demonstration provides a visible swing, enemy flash and small contact effect. Misses produce no hit feedback. No large screen shake, floating damage numbers, long hit pause, exaggerated particles or combo presentation is introduced. Effects and color overrides clean up on interruption, defeat and release.

The existing hotbar displays the selected wrench icon and active highlight. Switching slots updates the equipped view. A separate equipment screen or additional HUD hierarchy is not required.

## Starting loadout, persistence and compatibility

Runtime setup references a configurable starting-loadout asset describing initial hotbar grants and the initially selected slot. The default grants one maintenance wrench into the first hotbar slot and selects that slot after normal awakening selection reset. Invalid item references/quantities, insufficient configured capacity or an out-of-range initial selection are diagnosed before granting anything or opening gameplay. Future background selection may choose another loadout asset without background checks in combat. No background system is implemented.

A new production save has a pending initial-loadout status. Successful initialization persists the complete configured grants and completion status in the same valid saved snapshot before ordinary player control. Retries, scene arrivals, resume and successor awakening cannot reapply a completed initial loadout, even if the wrench was later moved, lost or consumed through a future system. A failed grant does not mark completion or open gameplay. A valid pending snapshot has not admitted gameplay and has no carried player contents; its lifecycle record may already exist. It resumes initialization safely without duplicate grants. A pending snapshot with partially granted/carried contents is inconsistent and uses the established save-recovery failure behavior rather than being silently topped up.

Use a narrowly scoped top-level boolean `startingLoadoutInitialized` as part of save format version 4. Existing versions 1–3 migrate to true/completed legacy status and retain their carried contents: loading an old save does not grant a retroactive wrench. Version-4 snapshots must contain this correctly typed fact; clone/capture/reset and validation preserve it. A genuinely new save/reset starts false/pending, distinct from migration. Existing IDs, GUIDs, collected items, vitals, survivor/expedition/ship records and recovery guarantees remain intact. No equipped-slot or attack-progress save fields are added.

Already completed saves retain existing transient-selection behavior on resume: contents restore, selection starts clear, and the player may select the saved wrench. Scene arrival and successor awakening do not silently refresh equipment. A disposable SEC-0 fixture explicitly opts into its loadout per fresh fixture session and never touches production saves.

## Integration and ownership boundaries

Extend existing item compilation, validation, input hosting, hotbar/container selection, scene feature lifecycle, new-save initialization and SEC-0 runtime composition. Keep pure rules/contracts in the current Domain/Application ownership boundaries and Unity-specific configuration, physics and presentation outside those assemblies. No additional inventory, container command, health-for-player, interaction, death/succession or loot system is introduced. Preserve existing assets' identities and the shipping entry scene list.

The maintenance wrench item/action assets must be referenced by the actual runtime configuration and catalog. The production new-run path and the disposable demonstration both exercise composed behavior; an isolated test-only runner does not satisfy integration acceptance.

Architecture documentation explains responsibilities, the tag matching truth table, how to configure another melee item, how another action type joins the execution boundary, how damage tags differ from item tags, and the selected-hotbar equipment tradeoff. It includes exact Unity validation instructions and known limitations. Retained documentation and verification artifacts remain outside imported Assets according to repository ownership rules.

## Acceptance criteria

| ID | Testable outcome | Expected evidence |
| --- | --- | --- |
| S1 | Registered tag identities and authored references survive serialization and asset/name changes that preserve GUID/identity; runtime-local indices are never durable IDs. | EditMode round-trip and fixture rename/move checks. |
| S2 | Null/unregistered definitions, duplicate identity/full-name/registry/set entries, malformed names, self-parenting and cycles are diagnosed deterministically before affected gameplay is ready. | Focused pure/Editor invalid-configuration cases. |
| S3 | One item/entity can hold all six wrench tags; optional untagged entities remain valid recipients under an unrestricted rule. | Tag-set tests and untagged-recipient PlayMode test. |
| S4 | Exact and hierarchical predicates produce every result in the matching table, including ancestor directionality and unrelated roots. | Domain matching tests. |
| S5 | Required, any-of and excluded groups combine as specified; conflicting requirements do not select behavior and yield false. | Domain query matrix. |
| S6 | Direct empty predicates, empty whole queries and empty entity sets follow the explicit empty-behavior table; invalid references are not treated as empty. | Domain/Editor boundary tests. |
| S7 | Existing item IDs, metadata and consumable definitions remain usable; wrench configuration includes its six tags, Primary action and the stated configurable initial statistics. | Catalog/asset validation and existing item tests. |
| S8 | Two actors sharing item/action assets have independent executions; a second test-only melee configuration works without item-ID branches or player/melee-code changes, and another execution type can use the generic boundary without melee phases. | Shared-definition tests, test-only alternate execution and scoped architecture review. |
| S9 | The selected hotbar slot is the sole equipped-item source; icon/highlight reflect it, and empty/no-action slots reject primary requests safely. | Application/PlayMode selection and actual HUD checks. |
| S10 | Mouse left/gamepad right trigger issue Primary; F/gamepad west still consume the selected supported consumable and E/gamepad east still interact. UI-consumed or held-across-reset input does not attack. | Production input-asset keyboard/mouse/gamepad tests. |
| S11 | Zero-health, inactive and stunned actors cannot start or continue attacks; pause/screens prevent requests and freeze progress without hits, while transition/owner invalidation cancels. | PlayMode policy, vitals, stun and lifecycle cases. |
| S12 | Default phase boundaries are .12, .22 and .55 gameplay seconds; windup and recovery never authorize damage. Configured timing changes those boundaries. | Deterministic timing and instrumented hit-delivery tests. |
| S13 | Large elapsed steps crossing Active and subdivided steps give equivalent phases and one hit for a static target; no entire active interval disappears under a coarse update. | Boundary-crossing timing/Physics2D tests. |
| S14 | Repeated/held requests cannot bypass recovery; cancelling and re-equipping on the same actor cannot bypass the original committed lockout. | Request-spam and swap/cancel timing tests. |
| S15 | Death, stun, unequip, occupied-slot replacement including same-definition replacement, owner generation change and repeated release prevent stale hits and release owned movement/effect state. Successor actors inherit no execution. | Application/PlayMode lifecycle matrix. |
| S16 | Last movement direction and authored initial down determine a committed swing; default movement factor is .65, generic full blocks prevail, and ordinary movement resumes without clearing other restrictions. | Facing/movement PlayMode tests and visual scene check. |
| S17 | Targets with qualifying collider geometry inside range/arc are hit; outside targets are ignored, including boundary, offset and large-collider cases. | Physics2D geometry tests with numerical tolerances stated in tests. |
| S18 | Solid cover blocks wholly occluded targets; self/target shapes and damage recipients do not act as world blockers; trigger-only sensors do not create hits. Incomplete saturated queries fail conservatively with a diagnostic. | Physics2D cover/filter/saturation cases. |
| S19 | One swing hits multiple distinct recipients, never its attacker, and applies damage no more than once per recipient despite several colliders and active samples. | Multi-target/multi-collider/self-hit tests. |
| S20 | Applied/rejected damage results are separate from geometry; contexts identify amount, actor/target, item/action, impact/direction and Damage.Blunt, without copying every item tag. Eligibility queries support optional untagged targets. | Damage-contract and rule tests. |
| S21 | Player damage uses current vitals rules; invalid amounts and unavailable/defeated recipients do not mutate health or emit successful-hit feedback. | Existing vitals regressions and adapter tests. |
| S22 | SEC-0 begins at authored 40 health, reaches zero on the fourth default hit, defeats once, and ceases movement/fire/collision participation and outgoing pulse resources. Surviving units retain their current stun behavior. | Actual prefab/encounter PlayMode tests and manual defeat journey. |
| S23 | Default hit demonstrates enemy flash, restrained collision-aware knockback and a small contact effect; a miss completes its swing without hit effects. Configured audio plays at the appropriate swing/hit event. | Feedback-event tests and actual-scene visual/audio checks where clips are configured. |
| S24 | Missing optional art/audio/knockback support cannot break execution or damage; invalid required numeric/action configuration is reported before execution. Placeholder art is sufficient and follows the restrained visual scope. | Missing-asset/invalid-configuration tests and visual review. |
| S25 | A fresh production run grants one configured wrench, selects its hotbar slot after awakening, and exposes its Primary through actual composition/input. Configuring another initial loadout does not change combat behavior. | Fresh-run production-root PlayMode/input checks. |
| S26 | Save version 4 persists loadout completion with grants; retries/pending initialization complete safely without duplicates. Versions 1–3 migrate without retroactive grants; invalid v4 state is rejected through established recovery. | Save codec/session, failure/retry and isolated restart/recovery tests. |
| S27 | Resume, scene arrival, successor awakening and already-completed initialization do not grant or refresh equipment; existing contents/selection-reset semantics and production save isolation remain correct. | Lifecycle/succession/reload regressions and disposable-fixture checks. |
| S28 | The disposable SEC-0 scene demonstrates spawn equipped, out-of-range miss, in-range hit with feedback, receiving existing enemy stun and four-hit defeat through real composed input. | Authored-scene PlayMode journey plus retained manual Unity evidence. |
| S29 | Existing movement, vitals/recovery, consumables, SEC-0 AI/stun, inventory/transfer, saves and room generation pass relevant existing suites and the complete Wakeup verification gate. | Criterion-linked test XML and full gate results.json; no skip-build qualification. |
| S30 | Existing asset GUIDs/item identities/assembly boundaries and shipping entry list survive; new assets have valid metadata, references and catalog/runtime inclusion. | Authoring/catalog/metadata/build validation. |
| S31 | After warm-up, tag checks and normal melee candidate/deduplication/timing processing introduce no per-tick managed allocations; bounded buffers cannot corrupt or silently claim complete results. | Focused repeated-execution allocation measurement with optional effect creation measured separately; source review and saturation tests. |
| S32 | Architecture and Unity validation instructions describe the delivered extension points/tradeoffs; scope remains melee/wrench/SEC-0 integration without speculative gameplay or duplicated systems. | Documentation and final independent conformance review. |

## Verification limits and completion evidence

Reading existing tests is not evidence they passed. Implementation evidence must name criterion IDs and the exact source snapshot. Domain/Application tests cover pure tag/query, request lifecycle and timing rules; EditMode covers asset validation, stable serialization and authored composition; PlayMode covers actual 2D physics, input, movement, actor lifecycle and encounter feedback. Manual scene evidence covers visual readability that assertions alone cannot establish. The complete isolated `python3 Tools/Verification/verify.py` gate is required because Wakeup, inventory/hotbar and save recovery are affected; its `results.json` is authoritative for that gate. Additional weapon/SEC-0 scenarios remain required because the existing gate alone does not demonstrate melee combat. Reversible fixtures use disposable saves and evidence outside Assets.

## Out of scope

Additional playable weapons; ranged combat; ammunition/reload; durability degradation; crafting/upgrades; combos/charged attacks; stamina; blocking/parrying; new enemies; AI redesign; status-effect or resistance frameworks; full equipment UI; independent weapon selection; repair gameplay; background-specific bonuses/loadouts; general-purpose ability/effect machinery; new loot; enemy persistence/respawn rules; unrelated refactors. Test-only alternate item/action configurations establish reuse without becoming shipped gameplay. Production artwork is not a gate to the functional slice.
