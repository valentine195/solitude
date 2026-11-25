from PIL import Image
from pathlib import Path
import json
root=Path(__file__).parent
p=root/'PNG'
names=['plain','service','vent','rib']
hexes=['182B35','293F4A','3C5561','657F85','82999A','A2A38E','CAC5AF','E0D8BF','CF9B43']
palette=[tuple(bytes.fromhex(h)) for h in hexes]
ims=[]
for name in names:
 im=Image.open(p/f'north_wall_{name}_256.png').convert('RGBA').resize((64,64),Image.Resampling.NEAREST)
 out=Image.new('RGBA',im.size)
 out.putdata([(*min(palette,key=lambda c:sum((c[i]-v[i])**2 for i in range(3))),255) if v[3]>=128 else (0,0,0,0) for v in im.getdata()])
 out.save(p/f'north_wall_{name}_64.png');out.resize((512,512),Image.Resampling.NEAREST).save(p/f'north_wall_{name}_8x.png');ims.append(out)
# Exact side-edge agreement across all 16 ordered pairs.
for a in ims:
 for b in ims:assert list(a.crop((63,0,64,64)).getdata())==list(b.crop((0,0,1,64)).getdata())
strip=Image.new('RGBA',(512,64))
for i,k in enumerate([0,0,1,0,2,0,3,0]):strip.paste(ims[k],(i*64,0))
strip.save(p/'north_wall_continuous_64.png');strip.resize((2048,256),Image.Resampling.NEAREST).save(p/'north_wall_continuous_4x.png')
# Both high-resolution contact sheet and 8x pixel sheet have the same order.
for res,scale,suffix in [(256,1,'renders_256'),(64,8,'pixels_8x')]:
 sheet=Image.new('RGBA',(res*4,res))
 for i,name in enumerate(names):sheet.paste(Image.open(p/f'north_wall_{name}_{res}.png'),(i*res,0))
 if scale>1:sheet=sheet.resize((sheet.width*scale,sheet.height*scale),Image.Resampling.NEAREST)
 sheet.save(p/f'north_wall_{suffix}.png')
grid=Image.new('RGBA',(192,192))
for y in range(3):
 for x in range(3):grid.paste(ims[[0,1,2,3][(x+y)%4]],(64*x,64*y))
grid.resize((768,768),Image.Resampling.NEAREST).save(p/'north_wall_repeat_3x3.png')
report={'module_size':[64,64],'world_grid':32,'horizontal_stride_pixels':64,'wall_opaque_rows':[2,61],'transparent_margin_rows':2,'anchor_pixels':[32,2],'palette':hexes,'alpha_values':sorted(set(ims[0].getchannel('A').getdata())),'all_16_ordered_edge_pairs_match':True,'variants':names,'projection':'Top orthographic, XY relief authored as visible north-wall face','unity_imported':False}
(root/'validation.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report,indent=2))
