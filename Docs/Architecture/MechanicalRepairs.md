# Mechanical repairs

The auxiliary socket repair requires a compatible tool carried in player inventory or hotbar and consumes exactly one Battery from inventory. The tool can be unselected; repair retains its slot, quantity and current hotbar selection. A Battery in hotbar alone does not qualify. Contextual interaction performs repair immediately; Primary continues through the equipped action path.

## Ownership and publication

`Domain/Restoration/MechanicalRepair.cs` contains immutable compiled requirements. `ContainerSaveSession.MechanicalRepairBinding` owns the completed fact and validates current inventory/hotbar ownership, registered capabilities, target identity and replacement ship snapshot before changing inventory. `ContainerCommandService.RemoveForMechanicalRepair` publishes one inventory charge and the prevalidated repair state within the existing command boundary. Capture/reentry are blocked while publication is busy; observer failures cannot undo completion. No public StoreShipState call belongs inside that commit callback.

`Features/World/Restoration/MechanicalRepairDefinition.cs` is configuration only. `MechanicalRepairComposition` binds it through the existing SceneFeatureComposition lifetime after carried owners exist, reconciles the saved fact before readiness, and releases ownership/subscriptions on unload or disable. Definitions hold no mutable progression. Duplicate active IDs and invalid requirements reject binding. SaveableId values must be authored once and remain stable across sessions.

Completion uses the existing ship repair record with installed/running true and zero recovery time. The panel and RestorationDoorGate derive restored state from that fact. Repair allows the existing door interaction; it does not open the door or persist animation position. Unrelated repair, door, collection and lifecycle records remain owned by their existing systems.

The session becomes dirty and urgent after commit. Save failure keeps the complete in-memory result and existing save-error feedback, and a later flush retries persistence without another repair charge. Restart can recover the prior complete checkpoint if persistence did not succeed. Existing codec validation and recovery remain authoritative for malformed saves; missing repair records mean unrepaired. Scene return and successor activation retain repair in the run; starting a new run clears it through the existing reset boundary without changing once-only starting equipment rules.

## Authoring and extension

Selected data: `Assets/_Project/Features/World/Restoration/AuxiliarySocketRepair.asset`; Battery reference GUID `c756a156ba7f34cb2b65e32264413a60`. Configure the registered Tool.Mechanical requirement (or an authored descendant), exact part definition, SaveableId, junction, system and linked gate. Include the composition in SceneBindings features and configure the junction through `ConfigureMechanicalRepair`. Keep linked components enabled and validation clean.

The compiled tag query is hierarchical: Tool.Mechanical descendants qualify, a parent-only Tool assignment does not satisfy the child requirement. To test another compatible item, give a test item registered mechanical capabilities and supply it through the existing catalog/capability lookup; `MechanicalRepairTests` uses a second spanner item. Do not introduce item-name or tag-driven behavior dispatch. Tags determine eligibility, while the configured contextual target determines the action.

Unconfigured RestorationJunctions continue their legacy behavior. The original ShipSystemRestoration demo retains in-memory storage and resets on reload. Shipping placement, timed repairs, tool durability, general requirement frameworks and live save-provider registration are outside this slice.

## Disposable playable fixture

`SOLITUDE.Editor.MechanicalRepairFixtureBuilder.Prepare` creates `Assets/_Project/Tests/Fixtures/MechanicalRepair.unity` only in a disposable project with `-solitudeMechanicalRepairFixture`; it refuses existing fixtures and Git checkouts. It copies the authored restoration arrangement, adds production-session composition and stable identity `verification.mechanical.auxiliary-socket`, and uses the normal starting wrench grant. It does not repaint Tilemaps or change shipping scenes.

From the repository root, prepare an isolated Unity project using the existing
[verification tooling](../../Tools/Verification/README.md), then run the builder
there (replace `<disposable-project>` with that project's path):

```sh
unity run <disposable-project> --timeout 300 --no-tail -- -nographics -executeMethod SOLITUDE.Editor.MechanicalRepairFixtureBuilder.Prepare -solitudeMechanicalRepairFixture
```

Open the generated scene in that disposable project for the journey below. Create
the save directory before launching Unity, and pass its **absolute** path as
`-solitudeSaveDirectory`. Relative paths resolve against the disposable project
and can fail before gameplay. Use a unique directory for each verification attempt.

Use an explicit `-solitudeSaveDirectory` under a disposable verification output. Native repair scenarios require a directory containing `step4-mechanical-`. Never use ordinary save data. Stop an existing Play session before opening the fixture.

1. Approach the auxiliary socket and press E before collecting a Battery. Expect requirement feedback and no consumption.
2. Collect a nearby Battery through the existing pickup. Keep the starting wrench carried, with another hotbar slot selected.
3. Focus the socket and press E. Expect “System restored”, one fewer inventory Battery, unchanged wrench/selection, and a closed door with access enabled.
4. Interact with the door separately to open it. Select the wrench and use Primary to exercise its ordinary swing.
5. Save and reload the same disposable run. Expect the restored panel and door permission before interaction readiness; the door starts closed. Repeating repair reports “System already restored” without another charge.

Prompt before completion: “Offline — requires mechanical tool and 1 Battery in inventory”. Missing tool takes priority over missing Battery; feedback is “Requires a mechanical tool in inventory or hotbar” or “Requires 1 Battery in inventory”. Disabled/unavailable bindings reject without cost.

## Build and focused qualification

`WakeupPlayerBuilder.BuildMechanicalRepairVerification` requires explicit disposable opt-in and temporarily selects exactly the repair fixture followed by Wakeup. Its nonpersistent manifest remains alive during BuildPipeline asset unloading, and the scope restores the previous settings on success or exception. The authored shipping manifest remains Wakeup-only; shipping mode rejects the verification scope. Fixtures are never shipping entries.

Focused evidence comes from MechanicalRepairTests, MechanicalRepairBindingTests, MechanicalRepairInteractionTests, MechanicalRepairFixtureTests, MechanicalRepairSaveTests, MechanicalRepairRecoveryTests and MechanicalRepairBuildValidationTests. The existing verification runner supports `--mechanical-repair-only --skip-build` against an explicitly rebuilt current verification player. It checks save failure/retry, strict old/new interrupted snapshots, preserved backup bytes, reload, scene return, successor, new run and invalid duplicate records. Native source changes require a new verification binary; documentation changes do not. Keep logs, source maps, saves and captures outside Assets. Run only affected legacy restoration/demo/loadout regressions.

The recorded journey uses synthetic production input and harness positioning; it is not a human playtest. Independent QRSPI conformance review remains the next gate after implementation.
