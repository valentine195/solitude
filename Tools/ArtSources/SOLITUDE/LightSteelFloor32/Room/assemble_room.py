from pathlib import Path
from PIL import Image, ImageDraw
from collections import deque
from zipfile import ZipFile, ZIP_DEFLATED
import json
import random
import shutil

ROOT = Path(__file__).parent
SOURCES = ROOT.parent.parent
W, H, CELL = 512, 352, 32
floor_sources = ROOT.parent / 'Tiles32'
floor_names = ['base_clean', 'base_scuff_a', 'base_rubbed', 'base_scuff_b',
               'steel_plate', 'maintenance_hatch', 'grate_horizontal',
               'perforated_plate', 'amber_service_hatch', 'door_threshold']
images = {}
for name in floor_names:
    dest = ROOT / 'Assets' / 'Floor' / f'{name}.png'
    shutil.copy2(floor_sources / f'{name}.png', dest)
    images[name] = Image.open(dest).convert('RGBA')
    assert images[name].size == (32, 32)
for kind in ['plain', 'service', 'vent', 'rib']:
    dest = ROOT / 'Assets' / 'Wall' / f'north_wall_{kind}_64.png'
    shutil.copy2(SOURCES / 'NorthWallPanels' / 'PNG' / dest.name, dest)
    images['wall_' + kind] = Image.open(dest).convert('RGBA')
# Reuse just the existing wall-cap strip for side/south boundaries. A north
# face is never rotated onto the side walls.
cap = images['wall_plain'].crop((0, 2, 64, 10))
cap.save(ROOT / 'Assets' / 'Wall' / 'cap_horizontal_64x8.png')
vertical_cap = cap.transpose(Image.Transpose.ROTATE_90)
vertical_cap.save(ROOT / 'Assets' / 'Wall' / 'cap_vertical_8x64.png')
prop_path = ROOT / 'Assets' / 'Props' / 'ventilation_unit_64.png'
shutil.copy2(SOURCES / 'VentilationUnit' / 'PNG' / prop_path.name, prop_path)
vent = Image.open(prop_path).convert('RGBA')

floor_layer = Image.new('RGBA', (W, H))
walls_layer = Image.new('RGBA', (W, H))
props_layer = Image.new('RGBA', (W, H))
floor_cells, wall_objects, props = [], [], []
rng = random.Random(38)
accent_cells = {(2, 3): 'grate_horizontal', (3, 3): 'grate_horizontal',
                (2, 4): 'perforated_plate', (3, 4): 'perforated_plate',
                (12, 3): 'maintenance_hatch', (13, 3): 'amber_service_hatch',
                (7, 9): 'door_threshold', (8, 9): 'door_threshold'}
for y in range(1, 10):
    for x in range(1, 15):
        name = rng.choices(floor_names[:4], [84, 6, 5, 5])[0]
        if y >= 2:
            name = accent_cells.get((x, y), name)
        floor_layer.alpha_composite(images[name], (x * CELL, y * CELL))
        floor_cells.append({'cell': [x, y], 'tile': name,
                            'under_north_wall': y == 1})
# Open south connection; no door sprite is fabricated for this layout.
for x in [7, 8]:
    floor_layer.alpha_composite(images['door_threshold'], (x * CELL, 320))
    floor_cells.append({'cell': [x, 10], 'tile': 'door_threshold', 'exit_apron': True})
for i, kind in enumerate(['plain', 'vent', 'plain', 'rib', 'plain', 'service', 'plain']):
    x, y = 32 + i * 64, 0
    walls_layer.alpha_composite(images['wall_' + kind], (x, y))
    wall_objects.append({'asset': f'Assets/Wall/north_wall_{kind}_64.png',
                         'position_px': [x, y], 'size_px': [64, 64],
                         'role': 'north_face', 'render_order': 20})
for x in [24, 480]:
    for y in range(2, 320, 64):
        piece = vertical_cap.crop((0, 0, 8, min(64, 320 - y)))
        walls_layer.alpha_composite(piece, (x, y))
        wall_objects.append({'asset': 'Assets/Wall/cap_vertical_8x64.png',
                             'position_px': [x, y], 'size_px': list(piece.size),
                             'role': 'side_cap', 'render_order': 30})
for start, end in [(24, 224), (288, 488)]:
    for x in range(start, end, 64):
        piece = cap.crop((0, 0, min(64, end - x), 8))
        walls_layer.alpha_composite(piece, (x, 320))
        wall_objects.append({'asset': 'Assets/Wall/cap_horizontal_64x8.png',
                             'position_px': [x, 320], 'size_px': list(piece.size),
                             'role': 'south_cap', 'render_order': 30})
props_layer.alpha_composite(vent, (64, 160))
props.append({'id': 'ventilation_unit', 'asset': 'Assets/Props/ventilation_unit_64.png',
              'position_px': [64, 160], 'size_px': [64, 64],
              'collision_rect_px': [64, 160, 64, 64], 'render_order': 25,
              'occlusionClass': 'solid_prop', 'occupantPolicy': 'blocked',
              'actorSafeArea': None})
for name, layer in [('floor', floor_layer), ('walls', walls_layer), ('props', props_layer)]:
    layer.save(ROOT / 'PNG' / f'service_room_{name}_layer.png')
preview = floor_layer.copy()
preview.alpha_composite(walls_layer)
preview.alpha_composite(props_layer)
preview.save(ROOT / 'PNG' / 'service_room_native.png')
preview.resize((W * 3, H * 3), Image.Resampling.NEAREST).save(ROOT / 'PNG' / 'service_room_3x.png')

walkable = {(x, y) for y in range(2, 10) for x in range(1, 15)} | {(7, 10), (8, 10)}
blocked_prop_cells = {(2, 5), (3, 5), (2, 6), (3, 6)}
walkable -= blocked_prop_cells
spawn, exit_cell = (7, 7), (7, 10)
queue = deque([spawn]);visited = {spawn};parents = {}
while queue:
    x, y = queue.popleft()
    for neighbor in [(x+1, y), (x-1, y), (x, y+1), (x, y-1)]:
        if neighbor in walkable and neighbor not in visited:
            visited.add(neighbor);parents[neighbor] = (x, y);queue.append(neighbor)
assert exit_cell in visited
assert visited == walkable
# Entire 4-cell-wide central aisle remains clear, down to the threshold.
assert all((x, y) in walkable for y in range(2, 10) for x in range(6, 10))
path = [exit_cell]
while path[-1] != spawn:
    path.append(parents[path[-1]])
path.reverse()
debug = preview.copy();draw = ImageDraw.Draw(debug)
for x in range(0, W, CELL):draw.line((x, 0, x, H), fill=(120, 170, 185, 90))
for y in range(0, H, CELL):draw.line((0, y, W, y), fill=(120, 170, 185, 90))
for x, y in path:draw.rectangle((x*32+12,y*32+12,x*32+19,y*32+19), fill='#cf963f')
debug.resize((W * 2, H * 2), Image.Resampling.NEAREST).save(ROOT / 'PNG' / 'service_room_grid_path_2x.png')

data = {
    'schema': 'solitude.room_layout.v1', 'name': 'ServiceRoom',
    'map_mode': 'room_chunk_mode', 'visual_model': 'layered_tilemap',
    'runtime_object_model': 'separate_props_and_scene_hooks',
    'collision_model': 'tile_walkability_and_rects', 'engine_target': 'portable_json',
    'grid': {'columns': 16, 'rows': 11, 'cell_pixels': 32,
             'canvas_pixels': [W, H], 'coordinates': 'top-left, x right, y down'},
    'interior': {'cell_rect': [1, 2, 14, 8], 'north_face_height_cells': 2},
    'floor_assets': {n: f'Assets/Floor/{n}.png' for n in floor_names},
    'floor_cells': floor_cells, 'wall_objects': wall_objects, 'props': props,
    'walkable_cells': sorted([list(v) for v in walkable], key=lambda v: (v[1], v[0])),
    'collision_rects_px': [[24, 2, 8, 318], [480, 2, 8, 318],
                           [32, 0, 448, 64], [24, 320, 200, 8],
                           [288, 320, 200, 8], [64, 160, 64, 64]],
    'scene_hooks': {'player_spawn_cell': list(spawn),
                    'exits': [{'id': 'south', 'cells': [[7, 10], [8, 10]],
                               'direction': 'south', 'width_cells': 2,
                               'door_asset': None, 'state': 'open_connection'}],
                    'zones': [{'id': 'ventilation_service', 'cell_rect': [2, 3, 2, 4]},
                              {'id': 'maintenance_access', 'cell_rect': [12, 3, 2, 2]}],
                    'camera_bounds_px': [0, 0, W, H]},
    'qa': {'accent_interior_cells': len(accent_cells), 'interior_cells': 112,
           'accent_percent': len(accent_cells) / 112 * 100,
           'spawn_to_exit_path_exists': True, 'all_walkable_cells_connected': True,
           'four_cell_central_aisle_clear': True, 'unity_imported': False,
           'runtime_collision_tested': False},
}
(ROOT / 'room_layout.json').write_text(json.dumps(data, indent=2) + '\n')
print(json.dumps(data['qa'], indent=2))
