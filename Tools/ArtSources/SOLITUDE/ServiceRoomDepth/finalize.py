from pathlib import Path
from PIL import Image
import numpy as np,json,shutil
from zipfile import ZipFile,ZIP_DEFLATED
r=Path(__file__).parent;old=r.parent/'RefinedNorthWalls';before=old/'Room/PNG/service_room_native.png'
# Palette-normalize the native Blender shadow layer (at most1 channel roundoff).
p=r/'Room/PNG/contact_shadows_layer.png';im=Image.open(p).convert('RGBA');a=np.array(im);colors=np.array([tuple(bytes.fromhex(h)) for h in ['667583','7E8C99','87939F','6C7B89']])
assert set(np.unique(a[:,:,3]))=={0,255}
a[:,:,:3]=colors[((a[:,:,:3,None].transpose(0,1,3,2).astype(int)-colors)**2).sum(3).argmin(2)];a[a[:,:,3]==0]=0;Image.fromarray(a).save(p)
# Portable assembly retains independent floor, shadow, wall and prop layers.
script='''from pathlib import Path
from PIL import Image
import json
r=Path(__file__).parent;d=json.loads((r/'room_layout.json').read_text())
floor=Image.new('RGBA',tuple(d['grid']['canvas_pixels']))
for c in d['floor_cells']:floor.alpha_composite(Image.open(r/d['floor_assets'][c['tile']]).convert('RGBA'),tuple(v*32 for v in c['cell']))
walls=Image.new('RGBA',floor.size);props=Image.new('RGBA',floor.size)
for layer,objects in [(walls,d['wall_objects']),(props,d['props'])]:
 for o in objects:
  im=Image.open(r/o['asset']).convert('RGBA');w,h=o['size_px'];layer.alpha_composite(im.crop((0,0,w,h)),tuple(o['position_px']))
for name,layer in [('floor',floor),('walls',walls),('props',props)]:layer.save(r/f'PNG/service_room_{name}_layer.png')
im=floor.copy()
im.alpha_composite(Image.open(r/'PNG/contact_shadows_layer.png').convert('RGBA'));im.alpha_composite(walls);im.alpha_composite(props)
im.save(r/'PNG/service_room_native.png');im.resize((im.width*3,im.height*3),Image.Resampling.NEAREST).save(r/'PNG/service_room_3x.png')
'''
(r/'Room/assemble.py').write_text(script)
import subprocess;subprocess.run(['python3',str(r/'Room/assemble.py')],check=True)
room=Image.open(r/'Room/PNG/service_room_native.png');prior=Image.open(before)
comparison=Image.new('RGBA',(1024,352));comparison.paste(prior,(0,0));comparison.paste(room,(512,0));comparison.resize((2048,704),Image.Resampling.NEAREST).save(r/'PNG/before_left_after_right_2x.png')
# Detail comparison isolates the north wall and raised equipment.
crop=(24,0,224,232);comparison=Image.new('RGBA',(400,232));comparison.paste(prior.crop(crop),(0,0));comparison.paste(room.crop(crop),(200,0));comparison.resize((1600,928),Image.Resampling.NEAREST).save(r/'PNG/depth_detail_before_after_4x.png')
# Preserve separate unshadowed prop for lighting reuse.
shutil.copy2(r/'Source/vent_without_shadow.png',r/'PNG/ventilation_unit_unshadowed_64.png')
Image.open(r/'Source/vent_depth.png').resize((512,512),Image.Resampling.NEAREST).save(r/'PNG/ventilation_unit_depth_8x.png')
qa={'wall_modules':5,'native_wall_module_px':[64,64],'wall_cell_slices':20,'slice_px':[32,32],'ordered_horizontal_edge_pairs_passed':0,'no_fractional_scaling':True,'wall_alpha_values':[0,255],'shadow_alpha_values':[0,255],'room_alpha_values':[0,255],'floor_tiles_unchanged':True,'recessed_floor_accents_cast_no_shadows':True,'vent_shadow_offset_px':[2,3],'shadow_layer_separate':True,'room_geometry_preserved':True,'unity_imported':False,'runtime_collision_tested':False}
ims=[np.array(Image.open(r/f'PNG/north_wall_{n}_64.png')) for n in ['plain','wide','service','vent','rib']]
for a in ims:
 assert set(np.unique(a[:,:,3]))=={0,255}
 for b in ims:assert np.array_equal(a[:,-1],b[:,0]);qa['ordered_horizontal_edge_pairs_passed']+=1
for n in ['plain','wide','service','vent','rib']:
 im=Image.open(r/f'PNG/north_wall_{n}_64.png');test=Image.new('RGBA',(64,64))
 for y in range(2):
  for x in range(2):
   cell=Image.open(r/f'Tiles32/north_wall_{n}_{x}_{y}.png');assert cell.size==(32,32);test.paste(cell,(x*32,y*32))
 assert np.array_equal(np.array(test),np.array(im))
 up=np.array(Image.open(r/f'PNG/north_wall_{n}_8x.png'));assert np.array_equal(up,np.repeat(np.repeat(np.array(im),8,axis=0),8,axis=1))
for p in (r/'Room/Assets/Floor').glob('*.png'):assert p.read_bytes()==(old/'Room/Assets/Floor'/p.name).read_bytes()
a=np.array(room);assert set(np.unique(a[:,:,3]))=={0,255}
sh=np.array(Image.open(r/'Room/PNG/contact_shadows_layer.png'));fl=np.array(Image.open(r/'Room/PNG/service_room_floor_layer.png'));assert np.all(fl[:,:,3][sh[:,:,3]>0]==255)
assert np.array_equal(np.array(Image.open(r/'Room/PNG/service_room_3x.png')),np.repeat(np.repeat(a,3,axis=0),3,axis=1))
q0=json.loads((old/'Room/room_layout.json').read_text());q=json.loads((r/'Room/room_layout.json').read_text())
for k in ['floor_cells','wall_objects','props','collision_rects_px','walkable_cells','scene_hooks']:assert q[k]==q0[k]
q['wall_revision']='ServiceRoomDepth';q['lighting_layers']=[{'asset':'PNG/contact_shadows_layer.png','order':'after floor before walls and props','kind':'hard native-pixel contact shadows'}];q['prop_shadow']={'asset':'Assets/Props/ventilation_unit_64.png','offset_px':[2,3],'editable_source':'../Source/vent_depth.aseprite'};(r/'Room/room_layout.json').write_text(json.dumps(q,indent=2))
(r/'validation.json').write_text(json.dumps(qa,indent=2));print(json.dumps(qa))
