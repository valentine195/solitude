# Distilled inventory implementation

Target: `Assets/_Project/Scenes/Demos/AtmosphericScrubberEncounter.unity`, using its existing authored InventoryScreen prefab instance and composition. No scene file was saved by this task. Existing unrelated uncommitted scene, input, composition and art work was preserved.

## Changed by this task

- `Systems/Containers/Views/InventoryScreenView.cs`: fixed three-column presentation, live active-tool equipment, direct atomic Equip via existing transfer commands, explicitly unavailable Drop, larger preview support, 300 ms off-white destination response, 220 ms equip settle, quick-bar docking and close/reopen cleanup. Existing non-distilled compatibility remains.
- `Systems/Containers/Prefabs/InventoryScreen.prefab`: saved, editable equipment/carried/details/quick-access layout. Category tabs and metadata hidden, Close label, no speculative Destroy. Suit/armor explicitly unavailable.
- `Systems/Containers/Prefabs/InventoryCarriedSlot.prefab`: full item names restored with wrapping/autosizing in equal cells.
- `Editor/InventoryScreenAuthoring.cs`: made class partial to retain the existing authoring workflow.
- `Editor/InventoryScreenAuthoring.Distilled.cs` and its Unity metadata: explicit, repeatable migration to the approved layout. Normal ConfigureScene does not rerun the migration or overwrite subsequent manual layout edits.

These files except the new partial authoring file were already untracked or modified when this task started. The task did not originate the other working-tree changes. Baseline copies of the pre-existing view and authoring source are in `work/Temporary/InventoryDistilledBaseline/`.

## Verification

- Final compilation: completed, no C# compilation errors.
- ContainerBoundaryTests: 24 passed, 0 failed (`container-tests.json`).
- UnityInputCompositionTests: 1 passed, 0 failed (`input-tests.json`). These dependency results predate the final tray-reference cleanup; they do not themselves prove that cleanup.
- Corrected-source runtime checks: Tab opens through UnityInputHost; carried grid binds real capacity; selecting a wrench updates details and brightens the destination; Drop is disabled; categories hidden; keyboard E restores the original wrench through an actual transfer; Escape closes and restores the persistent tray; Tab reopens with exactly one tray. Destination response and equip scale return to rest. See `runtime-select.json`, `runtime-equip.json`, `runtime-close.json`, `runtime-reopen.json`, `runtime-motion.json`.
- `verify-select.cs` sets up the selected screenshot using a transient test keyboard and moves the demo wrench from hotbar slot 1 to carried slot 1. Subsequent equip check restores its original location; the final state has the original wrench in hotbar slot 1 and an empty carried inventory. Verification input settings were restored and Play mode stopped.
- `inventory-selected-final.png` and `inventory-equipped-final.png`: visually inspected at 1920×1080. Full names, stable details, enlarged preview, unavailable Drop and quick bar render without clipping or overlap. First capture in a newly changed camera/UI state can need a settling frame; final captures were taken after settling.
- Repository-wide `git diff --check` reports trailing whitespace in pre-existing uncommitted scene changes. Those unrelated scene lines were preserved.

## Limits

This Editor runs in batch mode without a Game view render target. Screenshots therefore render the actual running scene and authored UI offscreen using the existing capture helper, with transient CanvasScaler overrides for the 1920×1080 design resolution; they are not Game-view framebuffer captures. Native 1280×720, physical pointer/controller operation and full-backpack replacement were not verified. Existing console history includes Pipeline command-discovery errors, so this report does not claim an empty console history.

The weapon/tool area reflects the actual active hotbar tool; no separate persistent weapon equipment model was invented. Suit/armor and recoverable world dropping remain unsupported and unavailable, as requested. There is no Destroy action. Equipment icons remain existing placeholders. No commit, staging, push or publishing occurred.
