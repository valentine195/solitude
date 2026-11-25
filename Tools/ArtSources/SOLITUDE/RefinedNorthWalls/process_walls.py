from pathlib import Path
from PIL import Image
import numpy as np,json,shutil
r=Path(__file__).parent
old=r.parent/'RefinedSteelFloor32/Final'
colors=['202C39','303C49','465767','91A0AA','687F92','9BA6AE','C9CCCA','E0E3DF','CF9B43']
p=np.array([tuple(bytes.fromhex(h)) for h in colors],dtype=int)
ims=[];names=['plain','service','vent','rib','wide']
for n in names:
 path=r/f'PNG/north_wall_{n}_64.png'
 a=np.array(Image.open(path).convert('RGBA'));rgb=a[:,:,:3].astype(int)
 a[:,:,:3]=p[((rgb[:,:,None,:]-p)**2).sum(3).argmin(2)]
 assert set(np.unique(a[:,:,3]))=={0,255};a[a[:,:,3]==0]=0
 im=Image.fromarray(a);im.save(path);ims.append(im)
 im.resize((512,512),Image.Resampling.NEAREST).save(r/f'PNG/north_wall_{n}_8x.png')
 for y in range(2):
  for x in range(2):im.crop((x*32,y*32,(x+1)*32,(y+1)*32)).save(r/f'Tiles32/north_wall_{n}_{x}_{y}.png')
for a in ims:
 for b in ims:assert np.array_equal(np.array(a)[:,-1],np.array(b)[:,0])
atlas=Image.new('RGBA',(320,64))
for i,im in enumerate(ims):atlas.paste(im,(i*64,0))
atlas.save(r/'PNG/north_wall_atlas.png');atlas.resize((2560,512),Image.Resampling.NEAREST).save(r/'PNG/north_wall_atlas_8x.png')
strip=Image.new('RGBA',(512,64))
for i,k in enumerate([0,4,2,4,3,0,1,4]):strip.paste(ims[k],(i*64,0))
strip.save(r/'PNG/continuous_wall_native.png');strip.resize((1536,192),Image.Resampling.NEAREST).save(r/'PNG/continuous_wall_3x.png')
# Assemble the existing room with revised walls only.
shutil.copytree(old/'Room/Assets',r/'Room/Assets',dirs_exist_ok=True)
for n in names:shutil.copy2(r/f'PNG/north_wall_{n}_64.png',r/f'Room/Assets/Wall/north_wall_{n}_64.png')
cap=ims[0].crop((0,2,64,10));cap.save(r/'Room/Assets/Wall/cap_horizontal_64x8.png');cap.transpose(Image.Transpose.ROTATE_90).save(r/'Room/Assets/Wall/cap_vertical_8x64.png')
shutil.copy2(old/'Room/room_layout.json',r/'Room/room_layout.json');shutil.copy2(old/'Room/assemble.py',r/'Room/assemble.py')
for n in ['floor','props']:shutil.copy2(old/f'Room/PNG/service_room_{n}_layer.png',r/f'Room/PNG/service_room_{n}_layer.png')
data=json.loads((r/'Room/room_layout.json').read_text())
for o in data['wall_objects']:
 if o['role']=='north_face' and o['position_px'][0] in [160,416]:o['asset']='Assets/Wall/north_wall_wide_64.png'
walls=Image.new('RGBA',(512,352))
for o in data['wall_objects']:
 im=Image.open(r/'Room'/o['asset']).convert('RGBA');w,h=o['size_px'];walls.alpha_composite(im.crop((0,0,w,h)),tuple(o['position_px']))
walls.save(r/'Room/PNG/service_room_walls_layer.png')
data['wall_revision']='RefinedNorthWalls';(r/'Room/room_layout.json').write_text(json.dumps(data,indent=2))
qa={'module_pixels':[64,64],'tile_slice_pixels':[32,32],'module_count':5,'slice_count':20,'all_25_ordered_horizontal_edge_pairs_match':True,'alpha':[0,255],'palette':colors,'native_render':True,'unity_imported':False,'room_geometry_preserved':True,'floor_and_props_unchanged':True}
(r/'validation.json').write_text(json.dumps(qa,indent=2));print(json.dumps(qa))
