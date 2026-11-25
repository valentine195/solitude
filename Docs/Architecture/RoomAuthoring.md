# Room and ship-layout authoring

A ShipLayout coordinates aligned, painted occupancy for Room and Corridor spaces.
Each space owns its stable ID, occupancy Tilemap, effective style and optional
floor-panel inputs. The occupancy map is the sole spatial authority. Saved
Generated Floor Tilemaps and structural/detail renderers are disposable output;
ordinary gameplay objects stay outside that managed output.

## Structural generation

The layout builds shared cell ownership first, rejects overlapping owners, then
resolves exterior edges, shared partitions, openings and typed orthogonal
junctions. Different spaces keep a partition until explicitly connected, even
when both are corridors. Connections identify a canonical boundary span, its
owners, type and width; they do not implement runtime doors or locks. The shared
boundary resolver generates a partition once. Styles select compatible artwork
after topology resolution, including visible-side ownership and declared
transition compatibility.

ShipRoomBrush is the primary editing path. It batches a gesture into one authored
Undo operation and one generation. Room cell/box and corridor preflights reject
foreign ownership; a late conflicting Room sample rolls back the whole gesture.
Direct edits through other Tilemap tools need an explicit rebuild. Invalid
structural ownership refuses replacement; local artwork failures retain useful
floors and independent geometry with located diagnostics. Empty-space records
remain authored and inactive until explicitly removed or restored.

## Assets and refresh

A style binds a reusable floor Tile and structural Sprites with declared canvas,
PPU, repeat, anchor, footprint, orientation, layer and order. A logical cell is
one Unity unit / 32 logical pixels; artwork can span several cells or use a
compatible declared source PPU. Source sprites are not resized to force a fit.
Keep Unity metadata/GUIDs when replacing assets.

A palette arranges reusable assets for painting. Editing the Sprite of the
already bound floor Tile or reimporting a bound Sprite's source updates loaded
consumers. Moving palette entries or substituting an unbound Tile does not change
a style's structural role. Use Artwork bindings or the style asset to assign it.
Asset refresh coalesces effective changes, defers during strokes and refuses
modified/unrecognized generated output. It does not rewrite closed scenes.

## Procedural floor detail

1. Build a read-only native-pixel domain from the space's occupied cells minus
   explicit integral-cell equipment reservations and doorway/airlock thresholds.
   Each doorway excludes one cell of depth on each side across its full width;
   open passages add no automatic exclusion. Base floor and collision are intact.
2. Partition eligible pixels into seeded, positive orthogonal rectangles on an
   eight-pixel quantum. Version 1 uses preferred size ranges and density; constrained
   remnants can reach eight pixels with located advisories. Same-space footprint
   edits can repartition it. Other spaces retain their own inputs/results.
3. Extract canonical shared seam segments, independent of textures/renderers.
   Rasterize their union once into clipped RGBA32 buffers. Thickness 1–3 uses
   consistent pixel bias and joins. Palette/thickness/subdivision changes do not
   change the rectangles. Alpha does not accumulate at shared borders.
4. Publish immutable content revisions as prepared textures/sprites and attach
   verified owned renderers. Default chunks are 256×256 native pixels, Point,
   no mipmaps, 32 PPU and bottom-left anchored at integer pixel positions. Bounds
   are at most 128 chunks and 8,388,608 allocated pixels; rejection is located and
   does not authorize an unbounded bounding-box allocation.
5. Preserve old revisions for saved references and Undo. Equal content reuses
   resources. Cleanup is explicit and conservative. Play Mode/build consumes
   prepared assets; partitioning, rasterization and refresh remain editor-only.

Draft settings/reservations require Apply. Preview is read-only. Regenerate keeps
the seed; New Seed is explicit; Disable removes only owned detailing and retains
inputs. Invalid/orphaned records are diagnosed, never silently relocated.

## Rendering qualification and limits

Floor/detail/architecture orders must be strictly increasing on the style's
sorting layer. Higher player/prop/wall-top layers must retain their intended
occlusion. For pixel art, inspect native-size and integer-zoom Game output using
the pixel-perfect implementation matching the active render pipeline.

Phase 3 Step 3 uses the running Unity 6000.6.4f1 URP Editor, 32 PPU, a disposable
URP PixelPerfectCamera, Point/linear review targets and no post-processing,
HDR/MSAA/dynamic resolution. Source-pixel comparisons, texture borders and camera
motion passed. The player test copies its production Sprite, Sprite-Lit-Default
material and Player sorting layer into the disposable scene; it does not run
player gameplay or change a shipping camera. Details and evidence:
[VisualAlignment](../../work/Reviews/RoomAuthoring/VisualAlignment.md).

RA-TD-001 remains human-deferred: coordinate pending drafts manually by using one
panel Inspector per layout and finishing/cancelling other drafts before Apply
or asset import. This is not full S49 compliance or a final qualification waiver.
The Medbay remains an explicitly incomplete temporary preview with two missing
frame-art diagnostics and residual-size advisories. Final artwork is unnecessary;
an admitted instrumented standalone checkpoint now loads saved prepared pixels
without editor generator assemblies. This is a before-scene resource/data proof,
not a standalone rendered-camera or gameplay result. See
[ImplementationReport](../../work/Reviews/RoomAuthoring/ImplementationReport.md).
Native UI responsiveness observations, explicit human acceptance of measured
latency/immutable-retention limits and final independent review remain pending.
Phase 4 is incomplete; no performance threshold or cleanup-policy acceptance is
implied by passing API measurements or the headless resource proof.
