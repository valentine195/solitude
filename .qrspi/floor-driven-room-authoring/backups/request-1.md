# SOLITUDE — Floor-Driven Room Authoring System
### QRSPI Implementation Requirements — SOL-ENV-001

**Status:** Proposed  
**Priority:** High — foundational level-design tooling  
**Target:** Unity Editor / 2D Tilemap  
**Implementation strategy:** Incremental, editor-first  
**Initial scope:** Phase 1 — Floor painting and automatic architectural generation

## 1. Objective

Implement a modular, floor-driven room authoring system for SOLITUDE that allows level designers to construct ship interiors by painting floor regions within the Unity Editor.

**The designer should only need to select a room style and paint the desired floor area. The system must automatically generate the surrounding walls, wall caps, and corners using the assets associated with that style.**

The system must support the long-term development of a large generation ship containing distinct architectural environments, including MedBay, Engineering, Navigation, Habitation, Cargo, and other ship sections.

The implementation must be extensible without requiring significant changes to existing gameplay architecture.

### 1.1 Target authoring workflow

The desired interaction is:

1. Select a room style, such as `Standard`, `MedBay`, or `Engineering`.
2. Paint or box-fill a region of floor cells using Unity's Tile Palette.
3. Automatically generate the corresponding floor tiles.
4. Detect the boundaries of the painted region.
5. Generate north-facing wall panels along the northern boundary.
6. Generate thin structural wall caps along the west, east, and south boundaries.
7. Generate corners and architectural junctions where boundaries meet.
8. Allow the designer to resize, extend, or erase sections of the room, automatically updating the generated architecture.

The designer must not need to paint walls or caps separately.

### 1.2 Core architectural principle

**Painted room geometry is authoritative. All architectural geometry is derived from it.**

The system must not implement recursive tile generation where floors create walls, which subsequently create caps.

Instead, a single deterministic generation process must calculate all required architecture from the authored room layout.

Conceptually:

```text
Room Layout (Authoritative)
           │
           ▼
     Room Geometry
           │
     Boundary Detection
           │
    ┌──────┼─────────┐
    ▼      ▼         ▼
  Floors  Walls    Wall Caps
           │         │
           └────┬────┘
                ▼
         Corners/Junctions
```

Generated architecture must never become the authoritative source for subsequent generation.

---

## 2. Existing project context

SOLITUDE is a 2D top-down pixel-art game developed in Unity.

### 2.1 Existing visual conventions

| Property | Convention |
|---|---|
| Logical tile size | 32×32 pixels |
| Player sprite | Approximately 32×48 pixels |
| North-facing wall panels | Typically 128×128 pixels |
| Narrow wall variations | Potentially 64×128 pixels |
| Wall caps | Thin horizontal/vertical structural elements |
| Projection | Stardew Valley-inspired interior perspective |
| Rendering | Pixel-perfect, point-filtered sprites |

The intended perspective is asymmetric:

- **North boundaries:** Full-height visible wall faces.
- **South boundaries:** Thin structural caps viewed from above.
- **West/east boundaries:** Thin structural caps viewed from above.
- **Corners:** Purpose-built connecting pieces.

The north wall is fundamentally different from the other boundaries.

Do not generate four full-height walls around each room.

### 2.2 Existing assets

The project contains existing wall, floor, and wall-cap artwork.

The implementation must first identify their actual import settings, sprite dimensions, pivots, and availability in the repository.

Do not assume all sprites use uniform dimensions or that sprite sheets are already production-ready.

Do not regenerate, redraw, resize, or otherwise modify existing artwork as part of this requirement.

Missing architectural sprites must produce clear configuration warnings rather than trigger automatic asset creation.

### 2.3 Existing architecture

SOLITUDE already contains established systems for scene composition, gameplay, inventory, interactions, doors, and ship equipment.

This requirement introduces **editor tooling**, not a new gameplay framework.

The implementation must preserve:

- `GameCompositionRoot` as the persistent game/session composition scope.
- `SceneBindings` as the scene-scoped composition boundary.
- Existing `SOLITUDE.Domain` and `SOLITUDE.Application` assembly boundaries.
- Existing player movement, interactions, door logic, and UI.
- Existing scene, prefab, and ScriptableObject serialized references.

Do not introduce a global service locator, singleton-based room manager, or unrelated architectural refactoring.

The room builder should remain self-contained and feature-owned.

---

## 3. Functional requirements

### FR-001 — Room style definitions

The system shall support reusable room style definitions implemented as Unity `ScriptableObject` assets.

Proposed type:

`RoomStyleDefinition`

Each style shall reference its corresponding architectural assets.

**Required configuration:**

| Property | Description |
|---|---|
| Style ID | Stable identifier |
| Display Name | Designer-friendly name |
| Floor Tile | Default floor tile or Rule Tile |
| North Wall Modules | Available full-height wall sprites |
| West Wall Caps | Vertical structural pieces |
| East Wall Caps | Vertical structural pieces |
| South Wall Caps | Horizontal structural pieces |
| Corners | Appropriate corner sprites |
| Default Door | Optional reference for future integration |

**Recommended future configuration:**

- Floor variants and their relative frequencies.
- Decorative wall variants.
- Wall panel width and spacing rules.
- Alternate structural materials.
- Door frames and transitions.
- Specialty wall panels.
- Damaged or deteriorated visual variants.

Different room styles may use completely different materials and structural designs.

Do not assume a shared sprite palette or identical geometry between styles.

**Acceptance criteria:**

- [ ] Room styles are authored through ScriptableObject assets.
- [ ] A room references its style rather than duplicating asset configuration.
- [ ] Architectural asset changes do not require code modifications.
- [ ] Multiple rooms can reference the same style.
- [ ] Missing required assets are identified by validation.

### FR-002 — Room authoring data

Each logical room must have an explicitly identifiable authored region.

Proposed component:

`ShipRoomAuthoring`

Conceptual structure:

```text
ShipRoomAuthoring
├── RoomId
├── RoomStyleDefinition
├── AuthoredFloorRegion
└── GeneratedArchitecture
```

The room must maintain a stable identity independent of its appearance.

Two adjacent rooms using the same room style must remain distinct logical rooms.

The implementation may use a dedicated Tilemap per room for authoring its floor region. This is the preferred initial approach, provided the system can later coordinate boundaries between adjacent room regions.

**Important:** Room identity must not be inferred solely from floor tile type or visual appearance.

Store painted occupancy in a durable, serialized format suitable for Unity scene authoring.

The system must distinguish between:

- Authored room occupancy.
- Generated visual flooring.
- Generated architectural boundaries.

These concepts must not be conflated.

**Acceptance criteria:**

- [ ] Each room has an explicit identity and assigned style.
- [ ] Room occupancy survives scene save/reload.
- [ ] Generated architecture can be deleted and reconstructed from authored data.
- [ ] The data model does not require rooms to be rectangular.
- [ ] Multiple adjacent logical rooms can be represented without ambiguity.

### FR-003 — Floor painting

Provide a Unity Editor authoring experience using the existing Tile Palette workflow wherever practical.

The implementation should prefer Unity's existing `GridBrushBase` extensibility rather than creating a bespoke scene editor.

Supported operations for Phase 1:

| Operation | Expected behavior |
|---|---|
| Paint | Add a cell to the current room |
| Erase | Remove a cell from the current room |
| Box Fill | Add a rectangular region |
| Box Erase | Remove a rectangular region |
| Rebuild | Regenerate architecture from authored occupancy |
| Undo/Redo | Restore authored occupancy and corresponding geometry |

The authoring tool must work in Edit Mode without entering Play Mode.

Painting must update the architectural preview.

A box-fill operation must be treated as a batch operation rather than initiating a full rebuild for every individual cell.

The system must prevent edits to generated layers from becoming authoritative changes.

**Acceptance criteria:**

- [ ] Designer can create an 8×6 room using Box Fill.
- [ ] Generated architecture appears in Edit Mode.
- [ ] Painting additional cells updates the room.
- [ ] Erasing cells updates the room.
- [ ] Undo and Redo restore both the authored state and generated output.
- [ ] Existing non-room Tilemaps are unaffected.

### FR-004 — Boundary detection

Implement deterministic boundary extraction from authored floor occupancy.

Each occupied cell has four cardinal neighbors.

A boundary exists when an occupied cell's neighboring position does not belong to the same logical room.

Conceptually:

```csharp
foreach (var cell in occupiedCells)
{
    foreach (var direction in CardinalDirections)
    {
        var neighbor = cell + direction;

        if (!IsSameRoom(cell, neighbor))
        {
            AddBoundary(cell, direction);
        }
    }
}
```

The resulting boundary representation must contain enough information to identify:

- Boundary location.
- Boundary orientation.
- Owning room.
- Adjacent room or empty space.
- Continuous wall segments.
- Corners and connections.

**Important:** Boundary detection must be independent of Unity sprite rendering.

Prefer a small, testable geometry implementation using grid coordinates.

The generated boundary geometry must not depend on sprite dimensions, transparency, or imported sprite pivots.

**Acceptance criteria:**

- [ ] A rectangular 8×6 region produces the expected closed perimeter.
- [ ] Adjacent occupied cells do not produce internal boundaries.
- [ ] Erasing a boundary cell updates affected edges.
- [ ] Regeneration produces deterministic results.
- [ ] Geometry calculations are independently unit-testable.

### FR-005 — North-wall generation

The north boundary shall generate full-height north-facing wall panels.

These panels shall be selected from the room's `RoomStyleDefinition`.

The system must support wall modules with dimensions larger than a single logical floor tile.

For example:

```text
Logical grid: 32×32 pixels

North wall:
┌───────────────┬───────────────┐
│               │               │
│  128×128      │  128×128      │
│  Wall Module  │  Wall Module  │
│               │               │
└───────────────┴───────────────┘

Floor:
┌───┬───┬───┬───┬───┬───┬───┬───┐
│   │   │   │   │   │   │   │   │
```

The wall modules must anchor to the northern boundary of the floor region.

Their visible faces should extend northward, outside the playable floor footprint.

**Required behavior:**

1. Identify continuous north-facing boundary runs.
2. Determine the available width.
3. Select compatible modules from the current room style.
4. Align module boundaries to the logical grid.
5. Position wall sprites using explicitly configured pivots or offsets.
6. Preserve native sprite proportions.

The generator must never stretch sprites to fill arbitrary widths.

For example, a 256px wall run could use two 128px modules.

A 320px run could use two 128px modules and one 64px module.

If the configured modules cannot exactly cover a run, the system must report the unsupported width.

Do not silently overlap or distort modules to make them fit.

**Phase 1 limitation:** Support 128px wall modules and room widths that are exact multiples of the configured module width. Smaller filler modules can be introduced in a subsequent phase.

**Acceptance criteria:**

- [ ] An eight-cell-wide room produces two correctly aligned 128px north-wall modules.
- [ ] Wall modules retain their native proportions.
- [ ] There are no overlapping modules.
- [ ] Wall positioning is independent of decorative sprite contents.
- [ ] Unsupported widths produce visible validation errors rather than distorted artwork.

### FR-006 — Wall-cap generation

West, east, and south boundaries shall generate the appropriate thin structural caps.

These elements must match SOLITUDE's established interior perspective.

**West/east:**

Generate narrow vertical cap sections running along the room boundaries.

**South:**

Generate horizontal cap sections.

**Corners:**

Select the appropriate structural connectors where boundaries intersect.

The existing `Wall_Caps` sprite sheet should be evaluated as the initial asset source.

The implementation must support assets whose source sprites contain substantial transparent padding.

Sprite placement must not assume that the visible artwork fills the sprite rectangle.

Store explicit visual alignment information where required.

For Phase 1, support the following corner types:

| Corner | Required |
|---|---|
| Northwest | Yes |
| Northeast | Yes |
| Southwest | Yes |
| Southeast | Yes |
| Concave corners | Later |
| T-junctions | Later |
| Four-way junctions | Later |

The implementation must distinguish between **structural corners** and decorative elements.

Do not duplicate north-facing wall faces when generating corner geometry.

**Acceptance criteria:**

- [ ] West/east caps align with room boundaries.
- [ ] South caps form a continuous edge.
- [ ] All four rectangular-room corners connect cleanly.
- [ ] North-wall and cap geometry do not overlap incorrectly.
- [ ] Corners update when the room changes dimensions.

### FR-007 — Floor generation

Generate visible flooring from the authored room region.

The initial implementation may use an existing ordinary Tile or Rule Tile.

Do not introduce a custom procedural floor-art system.

Floor generation must preserve the project's established pixel density and tile alignment.

For Phase 1:

- One default floor asset.
- No random decorative placement required.
- No additional floor material transitions required.

Floor rendering must be independent of the occupancy representation.

This separation allows future replacement of the visual flooring without changing room geometry.

### FR-008 — Regeneration

The generated architecture must be fully reproducible.

The system must provide an explicit `Rebuild Room` operation.

Regeneration must:

1. Read authored occupancy.
2. Calculate boundaries.
3. Resolve architectural modules.
4. Update generated Tilemaps and objects.
5. Remove obsolete generated geometry.
6. Preserve unrelated authored scene objects.

**Regeneration must be idempotent.**

Running regeneration twice without changing the inputs must produce identical output.

The builder must not accumulate duplicate wall sprites, corners, or Tilemap cells.

The implementation should eventually support rebuilding only affected regions.

For Phase 1, rebuilding the entire current room after a completed editing operation is acceptable.

### FR-009 — Editor integration

Provide a practical editor workflow.

At minimum:

- A way to create a `ShipRoomAuthoring` object.
- An Inspector field for its `RoomStyleDefinition`.
- A clearly identifiable authoring Tilemap.
- A usable paint/box-fill/erase workflow.
- A `Rebuild Room` button.
- A `Validate Room` action.
- Clear errors for missing assets and invalid geometry.

Generated objects must be grouped under clearly named containers.

The builder must not modify unrelated scene objects.

Avoid building an elaborate custom editor window unless the existing Inspector and Tile Palette cannot provide a satisfactory experience.

---

## 4. Proposed technical architecture

These names are recommendations, not mandatory class names.

Codex should examine existing project conventions before selecting their final locations.

```text
Features/
└── World/
    └── RoomAuthoring/
        │
        ├── RoomStyleDefinition.cs
        ├── ShipRoomAuthoring.cs
        │
        ├── Geometry/
        │   ├── RoomBoundaryExtractor.cs
        │   ├── RoomBoundary.cs
        │   └── WallRun.cs
        │
        ├── Generation/
        │   ├── RoomGeometryGenerator.cs
        │   ├── WallModuleResolver.cs
        │   └── RoomGeometryWriter.cs
        │
        └── Editor/
            ├── ShipRoomBrush.cs
            ├── ShipRoomAuthoringEditor.cs
            └── RoomAuthoringValidator.cs
```

### 4.1 Separation of responsibilities

| Component | Responsibility |
|---|---|
| `RoomStyleDefinition` | References architectural assets |
| `ShipRoomAuthoring` | Owns room identity, style, and occupancy |
| `RoomBoundaryExtractor` | Determines boundaries from occupied cells |
| `WallModuleResolver` | Selects compatible wall modules for continuous runs |
| `RoomGeometryGenerator` | Produces a description of required architecture |
| `RoomGeometryWriter` | Applies generated geometry to Unity objects and Tilemaps |
| `ShipRoomBrush` | Handles designer painting actions |
| `RoomAuthoringValidator` | Validates references and geometry constraints |

The most important architectural boundary is between **geometry computation and Unity rendering**.

The geometry generator should produce a deterministic description of the required floor and wall geometry.

The writer should then apply that description to the scene.

This makes it possible to test the generation rules without loading or rendering an entire Unity scene.

### 4.2 Suggested scene hierarchy

```text
ShipArea
│
├── RoomAuthoring
│   └── StandardRoom_01
│       ├── ShipRoomAuthoring
│       └── Occupancy
│
├── Generated
│   └── StandardRoom_01
│       ├── Floor
│       ├── NorthWalls
│       ├── WallCaps
│       └── Corners
│
├── Doors
│
└── Decorations
```

Exact hierarchy may be adjusted to follow current SOLITUDE conventions.

Generated objects must have explicit ownership so one room cannot accidentally delete another room's generated output.

### 4.3 Coordinate conventions

**This must be resolved before implementation begins.**

The system should use a consistent logical grid independent of individual sprite dimensions.

SOLITUDE's intended logical grid is 32×32 pixels.

At 32 Pixels Per Unit:

| Sprite | World-space dimensions |
|---|---|
| 32×32 | 1×1 units |
| 64×128 | 2×4 units |
| 128×128 | 4×4 units |

The room builder must account for the actual footprint and positioning of these sprites.

Do not change Pixels Per Unit to 128 merely to make a 128×128 wall sprite occupy a single Tilemap cell.

Do not change existing project import settings without identifying their consequences.

The north wall's visible height must not inflate the room's logical floor footprint.

Define a consistent convention for sprite pivots, world-space anchors, and rendering order.

The cap assets must retain their intended visual thickness regardless of transparent padding in their source textures.

---

## 5. Nonfunctional requirements

### NFR-001 — Maintainability

Room generation must remain isolated from unrelated gameplay systems.

Do not add room-specific branches to `SceneBindings`, the inventory system, the interaction system, or existing ship equipment controllers.

Avoid broad abstractions intended to support hypothetical future generators.

Prefer a small, explicit implementation.

### NFR-002 — Extensibility

The architecture must support future additions of:

- Multiple room styles.
- Multiple rooms within one ship area.
- Irregularly shaped rooms.
- Room-to-room shared boundaries.
- Doorway openings.
- Decorative wall variants.
- Floor material transitions.
- Structural damage variations.
- Generated collision geometry.

These features do not need full implementations in Phase 1.

However, Phase 1 must not introduce assumptions that make them unnecessarily difficult to add later.

### NFR-003 — Determinism

Identical authored input and style configuration must generate identical output.

Asset selection must not depend on uncontrolled random state.

Future decorative variation may use deterministic seeded selection.

### NFR-004 — Editor performance

Generation must occur in Edit Mode without requiring Play Mode.

Box painting should batch changes.

Avoid unnecessary full-scene searches, repeated asset loading, or rebuilding unrelated rooms.

Do not introduce runtime update loops to keep editor-generated geometry synchronized.

### NFR-005 — Unity serialization

Preserve existing asset GUIDs and serialized references.

Generated output must survive scene save/reload.

The implementation must support Undo/Redo and mark modified scenes dirty using appropriate Unity Editor APIs.

Editor-only functionality must not introduce unintended UnityEditor dependencies into runtime assemblies.

### NFR-006 — Visual correctness

The builder must never:

- Resize sprites to resolve placement discrepancies.
- Rotate asymmetric sprites without explicit authorization.
- Apply filtering that changes pixel appearance.
- Generate replacement artwork.
- Overlap incompatible modules silently.
- Place full-height wall faces on every boundary.

Visual correctness is a hard requirement, not an optional polishing task.

---

# 6. Implementation phases

## Phase 0 — QRSPI research and architectural assessment

**Codex must perform this phase before editing implementation files.**

Inspect the current repository to determine:

1. Current Unity version and installed 2D Tilemap packages.
2. Existing Grid and Tilemap hierarchy.
3. Existing room, wall, floor, and door authoring systems.
4. Current sprite import settings and pixels-per-unit conventions.
5. Existing `Wall_Caps` sprites and their slicing/pivot configuration.
6. Any existing custom Tilemap brushes or editor utilities.
7. Relevant assembly definitions and test infrastructure.
8. How existing scene validation and editor tooling are organized.

Identify reusable components.

Determine whether the proposed architecture is compatible with the existing project.

**Required output:** A concise architectural assessment identifying the proposed implementation, existing components to reuse, expected file changes, unresolved asset constraints, and risks.

Do not begin a broad architectural refactor.

If a fundamental assumption in this specification conflicts with the repository, document the conflict and propose the smallest correction.

## Phase 1 — Basic floor-driven room generation

**This is the initial implementation target.**

### Required deliverables

**1. Room authoring component**

Implement a scene component that owns an authored room region and references a room style.

**2. Room style definition**

Implement the minimal ScriptableObject required to configure the Standard room's floor, north-wall modules, caps, and corners.

**3. Boundary extraction**

Implement deterministic room-boundary detection.

**4. Architectural generation**

Generate standard flooring, north-facing wall panels, west/east/south caps, and four corner types.

**5. Editor integration**

Provide painting, box fill, erasing, regeneration, validation, and Undo/Redo behavior.

**6. Test scene**

Create a small isolated demonstration scene or prefab showing an 8×6 Standard room.

Do not modify existing production scenes merely to demonstrate the feature.

### Phase 1 scope restrictions

Phase 1 does **not** require:

- Multiple architectural themes working simultaneously.
- Room-to-room connections.
- Doors or door animations.
- Procedural art creation.
- Specialized floor transitions.
- Decorative asset placement.
- Atmosphere or power integration.
- Runtime room construction.
- Runtime collision changes.
- A custom visual level-editor interface.

The implementation should establish a clean foundation without prematurely implementing those features.

---

## Phase 2 — Advanced geometry

After Phase 1 is working:

- Support irregular room shapes.
- Support concave corners.
- Support T-junctions and shared boundaries.
- Resolve wall runs with multiple module widths.
- Avoid duplicate boundaries between adjacent rooms.
- Support corridors and narrow passages.

The geometry model established in Phase 1 should remain reusable.

## Phase 3 — Room styles

Introduce additional `RoomStyleDefinition` assets.

Initial targets:

- Standard
- MedBay
- Engineering
- Navigation

Validate that styles with different architectural materials and module designs work with the same generation system.

A style change should not modify room occupancy.

## Phase 4 — Door integration

Introduce a door-placement workflow.

Doors must be positioned on generated boundaries and replace the appropriate wall section.

Door placement must preserve the existing SOLITUDE door-state and interaction implementations.

Do not duplicate door gameplay logic within the room builder.

## Phase 5 — Production authoring improvements

Potential additions:

- Boundary previews.
- Generated collision boundaries.
- Incremental regeneration.
- Room selection and editing.
- Area-wide validation.
- Bulk room-style changes.
- Floor material painting.
- Integration with ship systems and atmosphere zones.

These are future requirements, not authorization to implement them immediately.

---

# 7. Automated testing requirements

Prioritize Edit Mode tests.

### T-001 — Rectangular boundary detection

Given an 8×6 rectangle of occupied cells:

- North boundary length must be eight cells.
- South boundary length must be eight cells.
- West boundary length must be six cells.
- East boundary length must be six cells.
- Exactly four convex corners must be identified.

No internal boundaries should be generated.

### T-002 — Wall-module resolution

Given a 256px north-wall run and available 128px modules:

- Exactly two modules must be selected.
- Both must be aligned.
- Neither may overlap.
- No scaling is permitted.

### T-003 — Idempotent regeneration

Given unchanged room occupancy and configuration:

- Rebuilding twice must produce equivalent output.
- No duplicate generated objects may remain.
- Authored occupancy must remain unchanged.

### T-004 — Room expansion

Given an 8×6 room:

Expand its eastern edge by four cells.

Verify:

- The new floor region is correct.
- The eastern boundary moves.
- The northern and southern boundaries extend.
- Relevant corners reposition.
- Obsolete geometry is removed.

### T-005 — Room erasure

Given an existing room:

Remove an exterior cell.

Verify that boundary extraction updates correctly.

If Phase 1's renderer cannot represent the resulting irregular perimeter, it must report that limitation without silently corrupting the room or leaving stale geometry.

### T-006 — Missing assets

Given a room style with a missing required wall module:

- Validation must identify the problem.
- Generation must not substitute unrelated sprites.
- No unrelated scene objects may be modified.

### T-007 — Undo/Redo

Given a completed Box Fill operation:

- Undo must restore the previous authored occupancy and generated output.
- Redo must restore the painted room and its generated output.
- Scene serialization must remain valid.

### T-008 — Scene persistence

Save and reopen the test scene.

Verify:

- Room identity persists.
- Style references remain valid.
- Authored occupancy persists.
- Generated architecture is intact or correctly reconstructable.

---

# 8. Phase 1 definition of done

Phase 1 is complete only when all the following criteria are satisfied.

| ID | Acceptance criterion |
|---|---|
| AC-01 | A designer can create a Standard room by painting or box-filling floor cells. |
| AC-02 | An 8×6 room automatically generates flooring, north walls, side caps, south caps, and corners. |
| AC-03 | The north wall uses existing full-height SOLITUDE artwork without resizing. |
| AC-04 | West/east/south caps use the established thin-cap perspective. |
| AC-05 | Wall and cap alignment is correct at native pixel resolution. |
| AC-06 | Expanding a rectangular room regenerates its architecture correctly. |
| AC-07 | Erasing cells removes obsolete generated geometry. |
| AC-08 | Rebuilding a room twice does not create duplicates. |
| AC-09 | Painting and Box Fill support Undo/Redo. |
| AC-10 | Room state persists across scene save/reload. |
| AC-11 | The builder operates in Edit Mode without Play Mode. |
| AC-12 | Generated geometry remains separate from authored room occupancy. |
| AC-13 | Existing gameplay systems, scenes, and serialized references remain intact. |
| AC-14 | Automated tests cover geometry, regeneration, and relevant editor behavior. |
| AC-15 | A short authoring guide explains how to create and edit a room. |

A successful compilation alone does not satisfy these criteria.

---

# 9. Codex execution instructions

Execute this requirement through the established QRSPI workflow.

### Checkpoint 0 — Repository assessment

Inspect the project and verify the current architecture.

Produce:

- Relevant dependency map.
- Existing tooling inventory.
- Proposed implementation structure.
- Technical risks.
- Asset compatibility findings.

Identify the Unity Editor APIs and versions actually available in the repository.

For reference, Unity provides documented APIs for custom brushes through `GridBrushBase`, Tilemap modification notifications, and batched tile operations. Verify their availability before choosing an integration strategy. [Unity Documentation](https://docs.unity3d.com/kr/6000.0/ScriptReference/GridBrushBase.html?utm_source=chatgpt.com)

### Checkpoint 1 — Core geometry

Implement room occupancy representation, boundary extraction, and wall-run calculation.

Add focused unit tests.

These components should not depend on scene rendering.

### Checkpoint 2 — Architectural generation

Implement generation using existing Standard room artwork.

Prioritize correct placement over decorative complexity.

Verify north-wall dimensions and wall-cap alignment.

### Checkpoint 3 — Editor workflow

Implement the authoring component, brush integration, regeneration, and Undo/Redo.

Ensure generated architecture remains synchronized with authored occupancy.

### Checkpoint 4 — Demonstration and verification

Create the isolated test room.

Run available validation and tests.

Inspect output for missing modules, overlaps, alignment errors, and unexpected scene modifications.

### Execution constraints

- Implement in small, reviewable changes.
- Preserve existing Unity `.meta` GUIDs.
- Avoid unrelated architectural refactors.
- Do not modify existing art.
- Do not implement future phases opportunistically.
- Do not claim visual correctness solely because code compiles.
- Run available relevant tests.
- Report unavailable test environments as **Not run**, not Passed.
- Stop and report blockers if actual sprite geometry or serialized project configuration invalidates the proposed implementation.

After the initial assessment, proceed with Phase 1 unless a material architectural or asset constraint requires a decision.

Do not continue into Phase 2 without explicit approval.

---

# 10. Required final implementation report

At the conclusion of Phase 1, provide:

1. **Architecture summary:** Major components and their responsibilities.
2. **Files changed:** New and modified files with brief explanations.
3. **Designer workflow:** Exact steps to create and resize a Standard room.
4. **Verification:** Tests executed and their results.
5. **Asset constraints:** Missing sprites, supported module dimensions, or visual alignment issues.
6. **Known limitations:** Explicitly identify unsupported geometry or editor operations.
7. **Next steps:** Smallest recommended changes necessary for Phase 2.

### Exit question

Answer the following:

> Can a level designer select the Standard room style, paint an 8×6 floor region, and obtain a correctly assembled SOLITUDE room—with north-facing walls, west/east/south caps, and corners—without manually editing the generated architecture?

If not, identify exactly which steps still require manual intervention.

---

## Recommendation for running this in Codex

I would start with **Phase 0 and Phase 1 only**, using the above document as the requirement.

There are two implementation details I'd pay particular attention to when reviewing Codex's plan.

**First, the 128×128 wall assets.** These need to be positioned relative to the 32×32 logical grid. If Codex proposes making the entire room use 128×128 cells to accommodate the walls, I would reject that approach. Large sprites should not dictate the logical grid resolution.

**Second, room identity.** It's tempting to build a simple system that scans the floor Tilemap and surrounds every connected patch with walls. That would work for an isolated prototype but would fail when two adjacent rooms use the same flooring and still need a separating wall. Explicit room ownership is important to establish now.

Otherwise, I'd keep the implementation deliberately modest. A functioning, visually correct Standard room builder is a much better first deliverable than a sophisticated authoring framework that hasn't demonstrated it can assemble your existing sprites correctly.

**The architectural success criterion is simple: when you eventually add an Engineering room, Codex should primarily need to configure a new `RoomStyleDefinition`, not modify the geometry-generation algorithms.**