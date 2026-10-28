from pathlib import Path
from PIL import Image,ImageDraw
import json,shutil
P=Path(__file__).resolve().parent;ROOT=P.parents[2];R=ROOT/'work/Reviews/SpaceShipWallUnified';R.mkdir(parents=True,exist_ok=True)
(P/'Tiles').mkdir(exist_ok=True);(P/'Layers').mkdir(exist_ok=True)
raw=Image.open(P/'master_raw.png').convert('RGB');assert raw.size==(1024,1536)
s=raw.resize((256,384),Image.Resampling.NEAREST).quantize(colors=48,dither=Image.Dither.NONE).convert('RGBA')
# One construction shared by EVERY module. Preserve generated corner turns; extend only flat interior face rows.
def north(box):
 src=s.crop(box);dst=Image.new('RGBA',(64,64))
 dst.paste(src.crop((0,0,64,8)),(0,0))
 # Generated face has36 rows. Duplicate two plain bands to make the required52px face.
 yy=8
 for y in range(8,44):
  row=src.crop((0,y,64,y+1));count=9 if y in (17,32) else 1
  for _ in range(count):dst.paste(row,(0,yy));yy+=1
 assert yy==60
 dst.paste(src.crop((0,45,64,49)),(0,60));return dst
plain=north((69,5,133,69));nw=north((5,5,69,69));ne=north((187,5,251,69))
h=plain.crop((16,0,17,8)).resize((64,8),Image.Resampling.NEAREST)
v=s.crop((5,25,13,26)).resize((8,32),Image.Resampling.NEAREST)
ve=v.transpose(Image.Transpose.FLIP_LEFT_RIGHT);south=h.transpose(Image.Transpose.FLIP_TOP_BOTTOM)
base=plain.crop((16,60,17,64)).resize((64,4),Image.Resampling.NEAREST)
for t in (plain,nw,ne):
 t.paste(h,(0,0));t.paste(base,(0,60))
# Maintain square8px L silhouettes and continue the same vertical section through the corners.
for y in (0,32):nw.paste(v,(0,y));ne.paste(ve,(56,y))
# Overlay-only details: the underlying wall remains the master, pixel-for-pixel.
window=plain.copy();window.paste(s.crop((208,205,240,244)),(16,16))
vent=plain.copy();vent.paste(s.crop((82,211,110,233)),(18,26))
console=plain.copy();console.paste(s.crop((147,211,175,234)),(18,25))
# Reapply shared construction rails; these are never variant-specific.
for t in (window,vent,console):t.paste(h,(0,0));t.paste(base,(0,60))
# Plain single-column32x64 module retains the same panel cadence.
plain32=plain.crop((0,0,32,64))
sw=Image.new('RGBA',(64,64));se=sw.copy()
for y in (0,32):sw.paste(v,(0,y));se.paste(ve,(56,y))
sw.paste(south,(0,56));se.paste(south,(0,56))
kit={'north_plain':plain,'north_plain_32':plain32,'north_window':window,'north_vent':vent,'north_console':console,'corner_nw':nw,'corner_ne':ne,'corner_sw':sw,'corner_se':se,'cap_west':v,'cap_east':ve,'cap_south':south}
assets={}
for name,t in kit.items():
 t.save(P/'Tiles'/f'{name}.png');assets[name]=dict(path=f'Tiles/{name}.png',size=list(t.size),source='Unified generated master; shared source profiles and detail overlays')
# Keep the revised floor byte-for-byte.
old=ROOT/'Tools/ArtSources/SpaceShipSampleRoom20V2';data=json.loads((old/'room.json').read_text())
placements=[p for p in data['placements'] if p['layer']=='floors']
for key in {p['asset'] for p in placements}:
 assets[key]=data['assets'][key];shutil.copyfile(old/assets[key]['path'],P/assets[key]['path'])
layers={n:Image.new('RGBA',(640,640)) for n in ('floors','north_walls','boundary_caps')}
def put(layer,key,x,y):placements.append(dict(layer=layer,asset=key,x=x,y=y))
put('north_walls','corner_nw',0,0);put('north_walls','corner_ne',576,0)
sequence=['north_plain','north_window','north_plain','north_console','north_plain','north_window','north_plain','north_plain']
for i,n in enumerate(sequence):put('north_walls',n,64+i*64,0)
for y in range(64,576,32):put('boundary_caps','cap_west',0,y);put('boundary_caps','cap_east',632,y)
put('boundary_caps','corner_sw',0,576);put('boundary_caps','corner_se',576,576)
for x in range(64,576,64):put('boundary_caps','cap_south',x,632)
for p in placements:
 im=Image.open(P/assets[p['asset']]['path']).convert('RGBA');assert p['x']+im.width<=640 and p['y']+im.height<=640
 layers[p['layer']].alpha_composite(im,(p['x'],p['y']))
merged=Image.new('RGBA',(640,640),(24,28,34,255))
for n,t in layers.items():t.save(P/f'Layers/{n}.png');merged.alpha_composite(t)
merged.save(P/'sample_room_20x20.png');merged.resize((1280,1280),Image.Resampling.NEAREST).save(R/'sample_room_2x.png')
# Wall kit shown at4x alongside its joined northern strip.
proof=Image.new('RGBA',(1280,800),(28,32,38,255));d=ImageDraw.Draw(proof)
for i,name in enumerate(['corner_nw','north_plain','north_window','north_console','corner_ne']):
 t=kit[name];proof.paste(t.resize((256,256),Image.Resampling.NEAREST),(i*256,0));d.text((i*256+4,262),name,fill='white',font_size=13)
proof.paste(merged.crop((0,0,640,96)).resize((1280,192),Image.Resampling.NEAREST),(0,300))
for i,box in enumerate([(0,0,64,96),(576,0,640,96),(0,576,64,640),(576,576,640,640)]):
 t=merged.crop(box);t=t.resize((t.width*3,t.height*3),Image.Resampling.NEAREST);proof.paste(t,(i*300,500))
proof.save(R/'wall_family_and_joins.png')
# Verify shared constructions, unchanged floor, dimensions, and connected8px boundary silhouettes.
assert layers['floors'].tobytes()==Image.open(old/'Layers/floors.png').convert('RGBA').tobytes()
for t in (window,vent,console):
 assert t.crop((0,0,64,8)).tobytes()==plain.crop((0,0,64,8)).tobytes()
 assert t.crop((0,60,64,64)).tobytes()==plain.crop((0,60,64,64)).tobytes()
 assert t.crop((0,8,10,60)).tobytes()==plain.crop((0,8,10,60)).tobytes()
for t in (nw,ne,plain,window,vent,console):assert t.size==(64,64)
assert plain32.size==(32,64)
assert nw.crop((0,32,8,64)).tobytes()==v.tobytes()
assert ne.crop((56,32,64,64)).tobytes()==ve.tobytes()
data.update(assets=assets,placements=placements,revision='Single shared wall master; same floor as V2')
(P/'room.json').write_text(json.dumps(data,indent=2)+'\n')
(P/'qc.json').write_text(json.dumps(dict(native_room_size=[640,640],grid=[20,20],north_sizes=[[32,64],[64,64]],floor_unchanged=True,variant_cap_base_and_outer_face_pixels_identical=True,north_side_corner_exit_pixels_match=True,projection='Orthogonal; square silhouette corners; side and south caps only'),indent=2)+'\n')
print('20x20 room rebuilt with one master wall, shared overlay variants and matching corner exits; floor unchanged.')
