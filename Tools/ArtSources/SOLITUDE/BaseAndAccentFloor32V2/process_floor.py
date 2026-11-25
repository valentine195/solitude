from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
from zipfile import ZipFile, ZIP_DEFLATED
import hashlib
import json
import random
import shutil
import sys

root = Path(__file__).parent
previous = root.parent / 'BaseAndAccentFloor32'
raw_path = Path(sys.argv[1])
raw = Image.open(raw_path).convert('RGB')
shutil.copy2(raw_path, root / 'Raw' / 'redesigned_accent_source.png')
# Fresh designs requested for 32px readability. This is not a reduction of
# the previous 64px accent tiles. Establish the final grid by point sampling.
# Generated presentation slots drifted from exact thirds. Crop each complete
# authored tile frame explicitly so no cell inherits its neighbor's border.
source_boxes = [(10, 10, 504, 468), (520, 10, 1014, 468),
                (10, 484, 504, 973), (520, 484, 1014, 973),
                (10, 990, 504, 1482), (520, 990, 1014, 1482)]
assert raw.size == (1024, 1536), raw.size
native = Image.new('RGB', (64, 96))
for i, box in enumerate(source_boxes):
    tile_source = raw.crop(box).resize((32, 32), Image.Resampling.NEAREST)
    native.paste(tile_source, (i % 2 * 32, i // 2 * 32))
colors = ['1B2430', '28303D', '3C4657', '4C586A', '647184', '748194',
          '8290A1', 'A0ACBA', 'CF963F', 'E5AF55']
palette = [v for color in colors for v in bytes.fromhex(color)]
palette += palette[:3] * (256 - len(colors))
palette_image = Image.new('P', (1, 1))
palette_image.putpalette(palette)
native = native.quantize(palette=palette_image, dither=Image.Dither.NONE).convert('RGBA')
native.save(root / 'PNG' / 'redesigned_accents_native.png')
native.resize((512, 768), Image.Resampling.NEAREST).save(root / 'PNG' / 'redesigned_accents_8x.png')
base_names = ['base_clean', 'base_scuff_a', 'base_rubbed', 'base_scuff_b']
accent_names = ['steel_plate', 'maintenance_hatch', 'grate_horizontal',
                'perforated_plate', 'amber_service_hatch', 'door_threshold']
tiles = []
for name in base_names:
    src = previous / 'Tiles32' / f'{name}.png'
    dst = root / 'Tiles32' / f'{name}.png'
    shutil.copy2(src, dst)
    assert hashlib.sha256(src.read_bytes()).digest() == hashlib.sha256(dst.read_bytes()).digest()
    tiles.append(Image.open(dst).convert('RGBA'))
for i, name in enumerate(accent_names):
    x, y = i % 2 * 32, i // 2 * 32
    tile = native.crop((x, y, x + 32, y + 32))
    tile.save(root / 'Tiles32' / f'{name}.png')
    tiles.append(tile)
names = base_names + accent_names
atlas = Image.new('RGBA', (128, 96))
for i, (name, tile) in enumerate(zip(names, tiles)):
    assert tile.size == (32, 32)
    assert set(tile.getchannel('A').get_flattened_data()) == {255}
    atlas.paste(tile, (i % 4 * 32, i // 4 * 32))
    large = tile.resize((256, 256), Image.Resampling.NEAREST)
    large.save(root / 'PNG' / f'{name}_8x.png')
    # Explicitly check every 8x8 block, rather than checking just one sample.
    for y in range(32):
        for x in range(32):
            block = large.crop((x * 8, y * 8, x * 8 + 8, y * 8 + 8))
            assert set(block.get_flattened_data()) == {tile.getpixel((x, y))}
    repeat = Image.new('RGBA', (96, 96))
    for y in range(3):
        for x in range(3):
            repeat.paste(tile, (x * 32, y * 32))
    repeat.resize((384, 384), Image.Resampling.NEAREST).save(root / 'PNG' / f'{name}_repeat_3x3.png')
atlas.save(root / 'PNG' / 'floor_atlas_32.png')
atlas.resize((1024, 768), Image.Resampling.NEAREST).save(root / 'PNG' / 'floor_atlas_8x.png')

# Keep the previous placement and base weights for a fair comparison.
rng = random.Random(23)
layout = [[rng.choices(range(4), [82, 6, 6, 6])[0] for x in range(16)] for y in range(10)]
accents = {(1, 2): 6, (1, 3): 6, (14, 6): 7, (14, 7): 7,
           (12, 2): 5, (12, 3): 8, (7, 8): 9, (8, 8): 9, (4, 6): 4}
for (x, y), index in accents.items():
    layout[y][x] = index
field = Image.new('RGBA', (512, 320))
for y, row in enumerate(layout):
    for x, index in enumerate(row):
        field.paste(tiles[index], (x * 32, y * 32))
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

# Comparison is review-only: top row old whole-tile reductions; bottom row
# freshly redesigned 32px accents. All pixels displayed at exact 8x.
compare = Image.new('RGBA', (192, 64))
for i, name in enumerate(accent_names):
    compare.paste(Image.open(previous / 'Tiles32' / f'{name}.png'), (i * 32, 0))
    compare.paste(tiles[i + 4], (i * 32, 32))
compare.resize((1536, 512), Image.Resampling.NEAREST).save(root / 'PNG' / 'old_top_new_bottom_8x.png')

report = {
    'tile_size': [32, 32], 'base_tiles_unchanged_byte_for_byte': True,
    'base_count': 4, 'fresh_accent_count': 6,
    'accent_source': 'New generation with explicit 32px feature budgets; not previous 64px accent images',
    'accent_palette': colors, 'alpha_values': [255], 'source_crop_boxes': source_boxes,
    'exact_8x_blocks_checked': True, 'all_tiles_repeated_3x3': True,
    'preview_base_percent': 94.375, 'unity_imported': False,
    'atlas_cells': [{'name': name, 'column': i % 4, 'row': i // 4} for i, name in enumerate(names)],
}
(root / 'tile_manifest.json').write_text(json.dumps(report, indent=2) + '\n')
with ZipFile(root / 'base_and_accent_floor_32_v2_png.zip', 'w', ZIP_DEFLATED) as z:
    for folder in ['Tiles32', 'PNG']:
        for file in sorted((root / folder).glob('*.png')):
            z.write(file, str(file.relative_to(root)))
    z.write(root / 'tile_manifest.json', 'tile_manifest.json')
print(json.dumps({k: v for k, v in report.items() if k != 'atlas_cells'}, indent=2))
