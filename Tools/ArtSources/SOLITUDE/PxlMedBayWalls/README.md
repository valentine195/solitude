# Med-bay walls / PXL

Three native 128×256 wall faces: clinical panel, bedside utilities, medical supplies. These extend the generic SOLITUDE wall construction into a restrained clinical family with pale aged cladding, graphite hardware and desaturated blue wayfinding. They fill every canvas pixel, including the uppermost and lowest rows. There is no baked cap and no transparent upper margin. Layer the existing separate wall cap above them in the eventual scene.

The shared wall face has tall steel rails, two replaceable cladding sections and a continuous clinical stripe. Medical identifiers, utility fittings and supply cabinetry are independent transparent overlays. The gas-port shapes and control marks are visual concepts, not a standardized medical installation specification.

## Files

- `med_bay_walls.px.js`: editable PXL generation source.
- `out/sprites/wall_med_bay_plain.png`: clinical panel.
- `out/sprites/wall_med_bay_utility.png`: recessed gas outlets, call control and service grille.
- `out/sprites/wall_med_bay_supply.png`: recessed twin cabinet doors and sealed drawer.
- `out/sprites/overlay_med_bay_*.png`: native transparent fixture overlays.
- `out/sprites/wall_med_bay_*/wall_face.png`: reusable fully opaque shared base.
- `out/med_bay_walls.png` + `.json`: native 384×256 sheet with cell metadata.
- `Editable/med_bay_walls.aseprite`: three alternative state frames/tags, two editable layers (WallFace and MedicalFixtures).
- `out/scenes/repeat_*.png`: three adjacent copies of each variant.
- `out/scenes/grid_*.png`: 3×3 inspection arrangements. Vertical stacking is not an intended wall connection.
- `out/scenes/med_bay_wall_run.png`: six panels in a mixed native wall run.
- `out/_preview`: integer-scale PXL previews.
- `validation.json`: dimension, alpha, overlay recomposition, edge and saved-Aseprite round-trip checks.
- `work/Reviews/PxlMedBayWalls/contact_sheet.png`: visual review outside Assets.

All nine horizontal variant pairings have identical joining columns. The medical fixtures stay clear of the side joining bands. The saved Aseprite document was reopened and its rendered frames matched each native PNG exactly. PXL previews and native ASCII inspection were used; inspection text is retained in `work/Reviews/PxlMedBayWalls/inspect_*.txt`.

The mixed run was reviewed visually. Repeating the small medical identifier on every plain panel creates a deliberate rhythm; use the unadorned shared wall-face layer when a quieter stretch is needed. The topmost 24 pixels of every variant remain clear of fixtures so a separate cap can overlap them. Actual cap overlap and gameplay-scale appearance remain to be checked once Unity integration is authorized.

## Rebuild

From the repository root:

```sh
python3 Tools/ArtSources/SOLITUDE/PxlMedBayWalls/build.py
```

Uses installed PXL, Python/Pillow and the existing Aseprite CLI. `ASEPRITE_BIN` can override the executable location. PNG conversion for the Aseprite handoff changes indexed storage to RGBA without changing pixel colors. No image filtering or arbitrary resizing is applied; review enlargement is exact 2× or 3× nearest-neighbor.

PXL is the generation source of truth. Manual edits to the Aseprite export are not transferred back to PXL. All generated material remains outside Assets; nothing has been imported into Unity.
