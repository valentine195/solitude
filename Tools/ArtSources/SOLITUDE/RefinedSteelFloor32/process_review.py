from pathlib import Path
from PIL import Image
import numpy as np
import json, shutil
R=Path(__file__).parent
OLD=R.parent/'LightSteelFloor32'
NAMES=['base_clean','base_scuff_a','base_rubbed','base_scuff_b']
def quant(im, colors):
 a=np.asarray(im.convert('RGB')).astype(np.int32)
 p=np.array([tuple(bytes.fromhex(c)) for c in colors],dtype=np.int32)
 out=p[((a[:,:,None,:]-p)**2).sum(3).argmin(2)].astype('uint8')
 return Image.fromarray(out).convert('RGBA')
def build(round_no, tiles, cells=None):
 d=R/f'Round{round_no}'
 (d/'Tiles32').mkdir(parents=True,exist_ok=True)
 for src in (OLD/'Tiles32').glob('*.png'): shutil.copy2(src,d/'Tiles32'/src.name)
 for n,im in zip(NAMES,tiles): im.save(d/'Tiles32'/f'{n}.png')
 data=json.loads((OLD/'Room/room_layout.json').read_text())
 if cells is not None:data['floor_cells']=cells
 floor=Image.new('RGBA',(512,352))
 for c in data['floor_cells']:
  im=Image.open(d/'Tiles32'/f"{c['tile']}.png").convert('RGBA')
  floor.alpha_composite(im,tuple(v*32 for v in c['cell']))
 preview=floor.copy()
 for n in ['walls','props']: preview.alpha_composite(Image.open(OLD/f'Room/PNG/service_room_{n}_layer.png').convert('RGBA'))
 preview.save(d/'room_native.png');preview.resize((1536,1056),Image.Resampling.NEAREST).save(d/'room_3x.png')
 floor.save(d/'floor_layer.png')
 (d/'room_layout.json').write_text(json.dumps(data,indent=2))
 atlas=Image.new('RGBA',(128,96))
 for i,n in enumerate(data['floor_assets']):atlas.alpha_composite(Image.open(d/'Tiles32'/f'{n}.png'),((i%4)*32,(i//4)*32))
 atlas.save(d/'floor_atlas_32.png');atlas.resize((1024,768),Image.Resampling.NEAREST).save(d/'floor_atlas_8x.png')
 return d
if __name__=='__main__':
 raw=Image.open(R/'Source/round1_generated.png')
 tiles=[]
 for x,y in [(0,0),(627,0),(0,627),(627,627)]:
  im=raw.crop((x+9,y+9,x+617,y+617)).resize((32,32),Image.Resampling.NEAREST)
  tiles.append(quant(im,['737F8B','8B929C','9CA4AE']))
 build(1,tiles)
