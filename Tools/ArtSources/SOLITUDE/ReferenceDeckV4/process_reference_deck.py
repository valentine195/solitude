from pathlib import Path
from PIL import Image
from zipfile import ZipFile, ZIP_DEFLATED
import json
import shutil
import sys

ROOT = Path(__file__).parent
source = Path(sys.argv[1])
raw = Image.open(source).convert('RGB')
assert raw.width == raw.height, raw.size
shutil.copy2(source, ROOT / 'Raw' / 'generated_floor_atlas.png')
# Only deterministic processing: controlled nearest-neighbor sampling, palette
# reduction without dithering, slicing, and assembly of generated artwork.
native = raw.resize((256, 256), Image.Resampling.NEAREST)
# Reserve signal/material accents so the mostly steel-gray sheet does not
# absorb amber, copper, or blue into its grayscale palette.
base_palette = native.quantize(colors=24, method=Image.Quantize.MEDIANCUT,
                               dither=Image.Dither.NONE).getpalette()[:72]
accents = ['D1953B','9C6627','EDB65C','BE6C3D','854725','3C668D','5E83A2','29313E']
for color in accents:
    base_palette.extend(bytes.fromhex(color))
# Fill unused entries with an existing steel color, not black.
base_palette += base_palette[:3] * (256 - len(base_palette) // 3)
palette_image = Image.new('P', (1, 1))
palette_image.putpalette(base_palette)
native = native.quantize(palette=palette_image, dither=Image.Dither.NONE).convert('RGBA')
p = ROOT / 'PNG'
t = ROOT / 'Tiles32'
t.mkdir(exist_ok=True)
native.save(p / 'reference_deck_atlas_64.png')
native.resize((2048, 2048), Image.Resampling.NEAREST).save(p / 'reference_deck_atlas_8x.png')
names = [
    'steel_plate', 'steel_worn', 'interlocking_plate', 'maintenance_hatch',
    'grate_horizontal', 'grate_vertical', 'perforated_plate', 'mesh_grate',
    'grip_plate', 'blue_identifier', 'amber_service_hatch', 'plain_latched_plate',
    'raceway_horizontal', 'raceway_vertical', 'door_threshold', 'plain_infill',
]
tiles = []
manifest = []
rebuilt = Image.new('RGBA', native.size)
for index, name in enumerate(names):
    x, y = index % 4 * 64, index // 4 * 64
    tile = native.crop((x, y, x + 64, y + 64))
    tiles.append(tile)
    tile.save(p / f'{name}_64.png')
    tile.resize((512, 512), Image.Resampling.NEAREST).save(p / f'{name}_8x.png')
    rebuilt.paste(tile, (x, y))
    parts = Image.new('RGBA', (64, 64))
    for qy in range(2):
        for qx in range(2):
            part = tile.crop((qx * 32, qy * 32, qx * 32 + 32, qy * 32 + 32))
            filename = f'{name}_{qx}_{qy}.png'
            part.save(t / filename)
            parts.paste(part, (qx * 32, qy * 32))
            manifest.append({'file': filename, 'module': name, 'quadrant_xy': [qx, qy]})
    assert parts.tobytes() == tile.tobytes()
    repeat = Image.new('RGBA', (192, 192))
    for ry in range(3):
        for rx in range(3):
            repeat.paste(tile, (rx * 64, ry * 64))
    repeat.resize((576, 576), Image.Resampling.NEAREST).save(p / f'{name}_repeat_3x3.png')
assert rebuilt.tobytes() == native.tobytes()
assert set(native.getchannel('A').get_flattened_data()) == {255}
enlarged = native.resize((2048, 2048), Image.Resampling.NEAREST)
assert enlarged.resize((256, 256), Image.Resampling.NEAREST).tobytes() == native.tobytes()

# A quiet central walking lane with more detailed grating/service tiles at
# the perimeter. No new artwork is drawn into the generated tile pixels.
layout = [
    [4, 0, 11, 0, 1, 5],
    [6, 0, 1, 11, 0, 6],
    [4, 1, 0, 11, 3, 5],
    [6, 0, 11, 1, 10, 6],
    [4, 0, 0, 11, 1, 5],
]
field = Image.new('RGBA', (384, 320))
for y, row in enumerate(layout):
    for x, index in enumerate(row):
        field.paste(tiles[index], (x * 64, y * 64))
field.save(p / 'reference_deck_room_native.png')
field.resize((1152, 960), Image.Resampling.NEAREST).save(p / 'reference_deck_room_3x.png')
context = field.copy()
w = ROOT.parent / 'NorthWallPanels' / 'PNG'
for i, name in enumerate(['plain', 'plain', 'service', 'plain', 'vent', 'plain']):
    context.alpha_composite(Image.open(w / f'north_wall_{name}_64.png'), (i * 64, 0))
context.alpha_composite(Image.open(ROOT.parent / 'VentilationUnit' / 'PNG' /
                                  'ventilation_unit_64.png'), (160, 136))
context.save(p / 'reference_deck_context_native.png')
context.resize((1152, 960), Image.Resampling.NEAREST).save(p / 'reference_deck_context_3x.png')
report = {
    'raw_dimensions': list(raw.size), 'native_atlas_dimensions': [256, 256],
    'native_module_dimensions': [64, 64], 'world_cell_dimensions': [32, 32],
    'module_count': 16, 'slice_count': 64,
    'opaque_palette_colors': len(set(native.get_flattened_data())),
    'palette_rgb': sorted({pixel[:3] for pixel in native.get_flattened_data()}),
    'alpha_values': [255], 'slice_reassembly_pixel_exact': True,
    'nearest_neighbor_roundtrip_pixel_exact': True,
    'independent_module_frames': True,
    'arbitrary_edge_identity_claimed': False,
    'tiles': manifest, 'unity_imported': False,
}
(ROOT / 'tile_manifest.json').write_text(json.dumps(report, indent=2) + '\n')
with ZipFile(ROOT / 'reference_deck_png_outputs.zip', 'w', ZIP_DEFLATED) as archive:
    for folder in [p, t]:
        for file in sorted(folder.glob('*.png')):
            archive.write(file, str(file.relative_to(ROOT)))
    archive.write(ROOT / 'tile_manifest.json', 'tile_manifest.json')
    archive.write(ROOT / 'brief.md', 'brief.md')
print(json.dumps({k: v for k, v in report.items() if k not in ['tiles', 'palette_rgb']}, indent=2))
