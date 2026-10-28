from pathlib import Path
from PIL import Image
import numpy as np,json,shutil
r=Path(__file__).parent
old=r.parent/'RefinedSteelFloor32/Final'
colors=['202C39','303C49','465767','91A0AA','687F92','9BA6AE','C9CCCA','E0E3DF','CF9B43','B3BDC2','7C8B98']
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
