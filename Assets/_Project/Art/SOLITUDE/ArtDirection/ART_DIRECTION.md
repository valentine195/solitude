# SOLITUDE art direction — opening slice

Normal first, wrong second. SOLITUDE is a generation ship built for people to live and work aboard. The opening feels calm, functional, warm, inhabited but unexpectedly quiet. Curiosity and absence precede suspense. Red emergency light, severe damage and horror are reserved for later narrative states.

## Production contract
- 32 × 32 pixel tiles at 32 pixels per Unity unit. No fractional transform scale on new art.
- Target new player art: approximately 32 × 48 pixels, bottom/feet pivot. The existing taller caretaker is a replaceable placeholder, confirmed by the user. Replace its visual animation with four-direction 32 × 48 art while preserving movement and interaction behavior; fit the foot collider to the new footprint.
- Orthographic top-down room perspective, similar to Stardew interiors. North walls show a face; east, west and south boundaries primarily show caps. Horizontal and vertical edges, 90-degree corners. No isometric projection, diagonal room geometry or perspective convergence.
- Point filtering, no mipmaps, no compression, no antialiasing, binary alpha on final pixel assets. Tile pivots at (16,16); props at integer feet anchors. No blurred shadows, bloom or texture noise.
- Restrained warm ivory, graphite and desaturated steel blue. Amber means ordinary operation. Red is absent from the clean opening kit. Screens remain subdued and legible.
- Preserve the existing 640 × 360 reference resolution. Review at integer gameplay scale, with the moving player and UI present.

## Minimum opening kit
Reuse the existing SolitudeStart 32px atlas and nine-frame door; prefer its plain composite floor and plain ivory wall, with service details only near an interaction. One cryopod, one locker with open/closed states, battery, intercom, and a small shelf/blanket/mug grouping provide the missing human context. A maintenance clipboard may be reused as a small wall detail. Do not add extra asset families before this room passes review.

Layout sequence: cryobay → short service corridor → locker and battery → powered doorway → first fragmented AI contact. Maintain clear walking space and silhouettes. Repeat the same pod and structural pieces rather than generating unique decorations.

## Controlled states
NORMAL: clean base assets, steady amber, soft ivory and blue, a few personal belongings.
NEGLECTED: sparse dust/scuff overlays and one dormant indicator; preserve underlying geometry.
FAILING: localized open service panels, interrupted indicators, limited cable/damage overlays.
CRITICAL: localized severe damage and exceptionally rare red emergency accents. Do not recolor an entire biome red.
Only NORMAL is in this milestone. Future wear and damage are separate transparent overlays, not baked into every tile.

## Workflow and acceptance
1. Audit before replacing; retain original sources, GUIDs, animation, item definitions and game systems.
2. Read art-spec.json for every batch. Record generation prompt and source provenance.
3. Import the smallest family and validate dimensions, palette/alpha, pivots and import settings.
4. Assemble it in the opening review scene. Keep existing Wakeup intact.
5. Capture cryobay, service area and powered-door/AI state at the existing gameplay zoom. Verify collision, interaction, inventory and door passage.
6. Record failures explicitly. A concept preview does not count as an accepted production sprite. Expand only after the opening review passes.

The referenced ChatGPT conversation was read in full as returned by read_thread. It supplied written direction but no accessible image attachments. Board-image matching remains unverified.
