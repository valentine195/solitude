from pathlib import Path
from PIL import Image, ImageDraw
import json, shutil, hashlib
import numpy as np
P=Path(__file__).resolve().parent
ROOT=P.parents[2]
raw=P/'restored_raw.png'
entries=json.loads((P/'source_order.json').read_text())
im=Image.open(raw).convert('RGB')
(P/'Tiles').mkdir(exist_ok=True)
tiles=[]; manifest=[]
for i,a in enumerate(entries):
 x,y=i%16,i//16
 box=(round(x*im.width/16),round(y*im.height/16),round((x+1)*im.width/16),round((y+1)*im.height/16))
 tile=im.crop(box)
 # Integer2 nearest sampling, followed by removal of low-information interior rows/columns.
 # This preserves the outer frame without fractional resampling or padded gutters.
 tile=tile.crop((0,0,tile.width-tile.width%2,tile.height-tile.height%2))
 small=tile.resize((tile.width//2,tile.height//2),Image.Resampling.NEAREST)
 arr=np.array(small)
 removed=[]
 for axis in (1,0):
  while arr.shape[axis]>32:
   dif=np.abs(np.diff(arr.astype(float),axis=axis)).mean(axis=(0,2) if axis==1 else (1,2))
   # Keep outer three pixels and remove only redundant interior strips.
   dif[:3]=1e9;dif[-3:]=1e9
   j=int(np.argmin(dif))+1
   arr=np.delete(arr,j,axis=axis);removed.append([axis,j])
 small=Image.fromarray(arr)
 tiles.append(small)
 manifest.append(dict(file='Tiles/'+Path(a['file']).name,source=a,restored_box=list(box),removed_interior_strips=removed,atlas_xy=[x*32,y*32]))
# Shared palette and no dithering remove near-identical AI edge shades without adding subpixel blends.
atlas=Image.new('RGB',(512,320),(255,0,255))
for i,t in enumerate(tiles):atlas.paste(t,((i%16)*32,(i//16)*32))
palette=atlas.quantize(colors=64,method=Image.Quantize.MEDIANCUT,dither=Image.Dither.NONE)
review=ROOT/'work/Reviews/SpaceShipFloor32'; review.mkdir(parents=True,exist_ok=True)
proof=Image.new('RGB',(8*210,19*230),(32,35,41));d=ImageDraw.Draw(proof)
for i,rec in enumerate(manifest):
 t=tiles[i].quantize(palette=palette,dither=Image.Dither.NONE).convert('RGBA')
 assert t.size==(32,32) and t.getextrema()[3]==(255,255)
 t.save(P/rec['file']);atlas.paste(t,tuple(rec['atlas_xy']))
 repeat=Image.new('RGBA',(96,96))
 for ry in range(3):
  for rx in range(3):repeat.paste(t,(rx*32,ry*32))
 # Integer2x repeat preview.
 px=(i%8)*210;py=(i//8)*230
 proof.paste(repeat.resize((192,192),Image.Resampling.NEAREST),(px,py))
 d.text((px,py+195),Path(rec['file']).stem,fill='white',font_size=11)
 rec['sha256']=hashlib.sha256((P/rec['file']).read_bytes()).hexdigest()
atlas=atlas.convert('RGBA')
# Unoccupied slots are transparent.
for i in range(len(tiles),160):atlas.paste((0,0,0,0),((i%16)*32,(i//16)*32,(i%16+1)*32,(i//16+1)*32))
atlas.save(P/'floor_atlas_32.png')
atlas.resize((2048,1280),Image.Resampling.NEAREST).save(review/'floor_atlas_4x.png')
proof.save(review/'repeat_checks_2x.png')
# Split review into manageable pages.
for page in range(4):
 y=page*5*230
 proof.crop((0,y,1680,min(y+1150,proof.height))).save(review/f'repeat_page_{page+1}.png')
(P/'manifest.json').write_text(json.dumps({'cell_size':[32,32],'atlas_size':[512,320],'columns':16,'rows':10,'margin':0,'spacing':0,'count':151,'processing':'AI reference restoration; integer2 nearest-neighbor reduction; low-information interior strip removal to32x32 preserving outer frame; shared64-color palette, no dithering. No fractional resampling.','assets':manifest},indent=2)+'\n')
print('Verified151 opaque32x32 tiles; atlas512x320; no gutters; source filenames preserved.')
