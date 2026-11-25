# Wall connector — recessed integration

Native 128×256 PXL revision of the previous connector. Its spine, square end blocks and amber emitter are preserved pixel for pixel. The change is how the wall meets it.

The wall infill now has a real 40px-wide opening at x=44–83. A continuous recessed steel mounting well fills that opening. Cladding terminates against the well's shallow lip, header/plinth returns meet the casing, and teal stripe ends are sealed into the same mounting profile. This removes the visual reading of a narrow post floating over a complete wall surface. No blurred glow or soft contact shadow was added.

- `connector.px.js`: editable PXL source, sharing the existing standard wall source.
- `out/sprites/wall_connector_128x256.png`: complete, opaque wall connector.
- `out/sprites/connector_overlay.png`: transparent mounting/core assembly for reuse.
- `out/sprites/wall_connector_128x256/*.png`: separate infill, recess, spine, end-block and emitter layers.
- `Editable/wall_connector_integrated.aseprite`: five named editable layers.
- `out/scenes/panel_connector_panel.png`: native 640×256 contextual assembly.
- `validation.json`: dimensions, alpha, original-core identity, true infill opening, seam and Aseprite round-trip checks.
- `work/Reviews/PxlWallConnectorIntegrated`: native and enlarged contextual previews, before/after sheet, 4× connector and PXL ASCII inspection.

Native and 4× previews and before/after assembly were visually inspected. Programmatic checks pass for exact size, full opacity, hard layer alpha, identical core layers, absent infill behind the recessed housing, matching edges against both neighboring wall panels and pixel-exact rendering after reopening the saved Aseprite file.

Rebuild from the repository root: `python3 Tools/ArtSources/SOLITUDE/PxlWallConnectorIntegrated/build.py`. Uses installed PXL, Python/Pillow and the existing Aseprite CLI; override its location with `ASEPRITE_BIN`. No source resizing or antialiasing. PXL is the source of truth; manual Aseprite edits do not update it.

Nothing was imported into or changed in Unity. The room-integration request has been stopped per the user's instruction. All earlier asset versions remain intact.
