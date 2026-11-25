from pathlib import Path
from PIL import Image,ImageDraw
import json,hashlib
P=Path(__file__).resolve().parent;ROOT=P.parents[2]
R=ROOT/'work/Reviews/SpaceShipSampleRoom20';R.mkdir(parents=True,exist_ok=True)
F=P.parent/'SpaceShipFloor32/Tiles';W=P.parent/'SpaceShipWallTall'
(P/'Layers').mkdir(exist_ok=True);(P/'Tiles').mkdir(exist_ok=True)
placements=[];assets={}
def asset(path):
 key=path.stem
 if key not in assets:
  im=Image.open(path).convert('RGBA');im.save(P/'Tiles'/path.name)
  assets[key]=dict(path='Tiles/'+path.name,size=list(im.size),source=str(path.relative_to(ROOT)),sha256=hashlib.sha256(path.read_bytes()).hexdigest())
 return key
floor=Image.new('RGBA',(640,640));walls=Image.new('RGBA',(640,640));bounds=Image.new('RGBA',(640,640))
def place(layer,path,x,y):
 key=asset(path);im=Image.open(P/assets[key]['path']).convert('RGBA')
 assert x%32==0 and y%32==0
 assert x+im.width<=640 and y+im.height<=640
 layer.alpha_composite(im,(x,y));placements.append(dict(layer='floors' if layer is floor else 'north_walls',asset=key,x=x,y=y))
# Quiet working deck: steel field, central light service bay and a pair of grating service runs.
for y in range(2,20):
 for x in range(20):
  name='s5_deck_r02_c01.png'
  if 5<=x<=14 and 6<=y<=15:name='s3_steel_r02_c02.png'
  if x in (3,16) and 5<=y<=16:name='s5_deck_r02_c03.png'
  place(floor,F/name,x*32,y*32)
#20 grid columns exactly: narrow hulls frame four larger window/control modules.
wall_specs=[('Tiles32x64/s1_hull_r01_c01.png',1),('Tiles32x64/s1_hull_r01_c02.png',1),('Tiles32x64/s1_hull_r01_c04.png',1),('Tiles32x64/s1_hull_r01_c05.png',1),('Tiles32x64/s1_hull_r01_c01.png',1),('Tiles64x64/s1_control_left.png',2),('Tiles64x64/s1_control_system.png',2),('Tiles32x64/s1_hull_r01_c02.png',1),('Tiles32x64/s1_hull_r01_c03.png',1),('Tiles64x64/s3_medical_r01_c07.png',2),('Tiles32x64/s1_hull_r01_c01.png',1),('Tiles32x64/s1_hull_r01_c06.png',1),('Tiles32x64/s1_hull_r01_c07.png',1),('Tiles32x64/s1_hull_r01_c02.png',1),('Tiles32x64/s1_hull_r01_c01.png',1),('Tiles64x64/s2_hull_r01_c02.png',2)]
x=0
for name,width in wall_specs:place(walls,W/name,x*32,0);x+=width
assert x==20
# Reuse corrected wall top-surface pixels for cap-only side/south boundaries.
source=Image.open(W/'Tiles32x64/s1_hull_r01_c01.png').convert('RGBA')
h=source.crop((0,0,32,8));v=h.transpose(Image.Transpose.ROTATE_90)
for side in ('west','east','south'):
 t=Image.new('RGBA',(32,32))
 t.paste(v if side!='south' else h,(0,0) if side=='west' else ((24,0) if side=='east' else (0,24)))
 name=f'room_cap_{side}.png';t.save(P/'Tiles'/name)
 key=Path(name).stem;assets[key]=dict(path='Tiles/'+name,size=[32,32],source='Derived by cropping/rotating corrected north-wall top surface only')
 spots=[(0,y*32) for y in range(2,20)] if side=='west' else ([(608,y*32) for y in range(2,20)] if side=='east' else [(x*32,608) for x in range(20)])
 for px,py in spots:
  bounds.alpha_composite(t,(px,py));placements.append(dict(layer='boundary_caps',asset=key,x=px,y=py))
for name,im in [('floors',floor),('north_walls',walls),('boundary_caps',bounds)]:im.save(P/f'Layers/{name}.png')
merged=Image.new('RGBA',(640,640),(24,28,34,255))
for layer in (floor,walls,bounds):merged.alpha_composite(layer)
merged.save(P/'sample_room_20x20.png');merged.save(R/'sample_room_native.png')
merged.resize((1280,1280),Image.Resampling.NEAREST).save(R/'sample_room_2x.png')
grid=merged.copy();d=ImageDraw.Draw(grid)
for a in range(0,641,32):d.line((a,0,a,639),fill=(110,180,190,100));d.line((0,a,639,a),fill=(110,180,190,100))
grid.save(R/'sample_room_grid.png')
blocked=[[1 if y<2 or x in (0,19) or y==19 else 0 for x in range(20)] for y in range(20)]
data=dict(name='SpaceShipSampleRoom20',mode='layered_tilemap',purpose='Editable art assembly sample; no Unity integration',width_tiles=20,height_tiles=20,tile_size=32,pixel_size=[640,640],origin='top-left',footprint='Walls included in20x20 overall footprint',assets=assets,placements=placements,collision=dict(grid=blocked,policy='Conservative whole-cell boundary collision;1 blocked0 walkable',walkable_cell_count=sum(v==0 for row in blocked for v in row)),scene_hooks=dict(player_spawn_cell=[10,17],exits=[]),layer_order=['floors','north_walls','boundary_caps'])
(P/'room.json').write_text(json.dumps(data,indent=2)+'\n')
assert merged.size==(20*32,20*32)
assert sum(p['layer']=='floors' for p in placements)==360
assert len(blocked)==20 and all(len(row)==20 for row in blocked)
# Re-render from exported portable data rather than trusting the assembly buffers.
check=Image.new('RGBA',(640,640),(24,28,34,255))
for layer in data['layer_order']:
 for rec in placements:
  if rec['layer']==layer:check.alpha_composite(Image.open(P/assets[rec['asset']]['path']).convert('RGBA'),(rec['x'],rec['y']))
assert check.tobytes()==merged.tobytes()
print(f'20x20 cells;640x640 px;{len(placements)} grid-aligned placements;306 walkable cells; export reconstruction matches preview.')
