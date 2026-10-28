from PIL import Image
from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED
import json
import random
import shutil
import sys

root = Path(__file__).parent
source = Path(sys.argv[1])
shutil.copy2(source, root / 'Raw' / 'generated_base_source.png')
raw = Image.open(source).convert('RGB')
assert raw.width == raw.height
# Controlled point sampling establishes the native grid. All downstream
# sizing, extraction, display and tile placement uses integer pixels.
base = raw.resize((64, 64), Image.Resampling.NEAREST)
base_palette = ['748194', '697688', '7D899A', '8591A0']
palette = [v for h in base_palette for v in bytes.fromhex(h)]
palette += palette[:3] * (256 - len(base_palette))
palette_image = Image.new('P', (1, 1))
palette_image.putpalette(palette)
base = base.quantize(palette=palette_image, dither=Image.Dither.NONE).convert('RGBA')
base.save(root / 'PNG' / 'base_atlas_32.png')
base.resize((512, 512), Image.Resampling.NEAREST).save(root / 'PNG' / 'base_atlas_8x.png')
names = ['base_clean', 'base_scuff_a', 'base_rubbed', 'base_scuff_b']
tiles = []
for i in range(4):
    x, y = i % 2 * 32, i // 2 * 32
    tiles.append(base.crop((x, y, x + 32, y + 32)))
# Derive the truly clean base from an unmarked area of the generated source.
# This is source cropping and sampling, not code-painted replacement artwork.
quiet_crop = raw.crop((0, 0, raw.width // 5, raw.height // 5))
tiles[0] = quiet_crop.resize((32, 32), Image.Resampling.NEAREST).quantize(
    palette=palette_image, dither=Image.Dither.NONE).convert('RGBA')
base.paste(tiles[0], (0, 0))
base.save(root / 'PNG' / 'base_atlas_32.png')
base.resize((512, 512), Image.Resampling.NEAREST).save(root / 'PNG' / 'base_atlas_8x.png')
accent_names = ['steel_plate', 'maintenance_hatch', 'grate_horizontal',
                'perforated_plate', 'amber_service_hatch', 'door_threshold']
previous = root.parent / 'ReferenceDeckV4' / 'PNG'
for name in accent_names:
    # Whole accents become single 32x32 cells; no quadrant slices.
    tiles.append(Image.open(previous / f'{name}_64.png').resize(
        (32, 32), Image.Resampling.NEAREST).convert('RGBA'))
names += accent_names
atlas = Image.new('RGBA', (128, 96))
for i, (name, tile) in enumerate(zip(names, tiles)):
    assert tile.size == (32, 32)
    assert set(tile.getchannel('A').get_flattened_data()) == {255}
    tile.save(root / 'Tiles32' / f'{name}.png')
    tile.resize((256, 256), Image.Resampling.NEAREST).save(root / 'PNG' / f'{name}_8x.png')
    atlas.paste(tile, (i % 4 * 32, i // 4 * 32))
    repeat = Image.new('RGBA', (96, 96))
    for y in range(3):
        for x in range(3):
            repeat.paste(tile, (x * 32, y * 32))
    repeat.resize((384, 384), Image.Resampling.NEAREST).save(root / 'PNG' / f'{name}_repeat_3x3.png')
atlas.save(root / 'PNG' / 'floor_atlas_32.png')
atlas.resize((1024, 768), Image.Resampling.NEAREST).save(root / 'PNG' / 'floor_atlas_8x.png')
# A 16x10-cell floor, with just nine accent cells (5.625%).
rng = random.Random(23)
layout = [[rng.choices(range(4), [82, 6, 6, 6])[0]
           for x in range(16)] for y in range(10)]
accents = {(1, 2): 6, (1, 3): 6, (14, 6): 7, (14, 7): 7,
           (12, 2): 5, (12, 3): 8, (7, 8): 9, (8, 8): 9, (4, 6): 4}
for (x, y), tile in accents.items():
    layout[y][x] = tile
field = Image.new('RGBA', (512, 320))
for y, row in enumerate(layout):
    for x, tile in enumerate(row):
        field.paste(tiles[tile], (x * 32, y * 32))
field.save(root / 'PNG' / 'floor_mix_native.png')
field.resize((1536, 960), Image.Resampling.NEAREST).save(root / 'PNG' / 'floor_mix_3x.png')
context = Image.new('RGBA', (512, 384))
context.paste(field, (0, 64))
wall = root.parent / 'NorthWallPanels' / 'PNG'
for i, name in enumerate(['plain', 'plain', 'service', 'plain', 'plain', 'vent', 'plain', 'plain']):
    context.alpha_composite(Image.open(wall / f'north_wall_{name}_64.png'), (i * 64, 0))
context.alpha_composite(Image.open(root.parent / 'VentilationUnit' / 'PNG' /
                                  'ventilation_unit_64.png'), (352, 176))
context.save(root / 'PNG' / 'context_native.png')
context.resize((1536, 1152), Image.Resampling.NEAREST).save(root / 'PNG' / 'context_3x.png')

horizontal, vertical = [], []
for ia, a in enumerate(tiles[:4]):
    for ib, b in enumerate(tiles[:4]):
        horizontal.append(list(a.crop((31, 0, 32, 32)).get_flattened_data()) ==
                          list(b.crop((0, 0, 1, 32)).get_flattened_data()))
        vertical.append(list(a.crop((0, 31, 32, 32)).get_flattened_data()) ==
                        list(b.crop((0, 0, 32, 1)).get_flattened_data()))
report = {
    'tile_dimensions': [32, 32], 'tile_count': len(tiles),
    'base_variants': 4, 'accent_variants': 6,
    'complete_tiles_not_quadrants': True,
    'base_palette': base_palette,
    'horizontal_base_edge_pairs_exact': sum(horizontal),
    'vertical_base_edge_pairs_exact': sum(vertical),
    'base_edge_pair_count_per_axis': 16,
    'preview_base_percent': 100 * (160 - len(accents)) / 160,
    'preview_accent_percent': 100 * len(accents) / 160,
    'opaque_tile_alpha': [255], 'unity_imported': False,
    'atlas_cells': [{'name': name, 'column': i % 4, 'row': i // 4}
                    for i, name in enumerate(names)],
}
(root / 'tile_manifest.json').write_text(json.dumps(report, indent=2) + '\n')
with ZipFile(root / 'base_and_accent_floor_32_png.zip', 'w', ZIP_DEFLATED) as z:
    for folder in ['Tiles32', 'PNG']:
        for f in sorted((root / folder).glob('*.png')):
            z.write(f, str(f.relative_to(root)))
    z.write(root / 'tile_manifest.json', 'tile_manifest.json')
print(json.dumps(report, indent=2))
