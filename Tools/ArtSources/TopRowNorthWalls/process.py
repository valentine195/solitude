from PIL import Image, ImageDraw
from pathlib import Path
import json, zipfile
P=Path(__file__).parent
R=P.parents[2]/'work/Reviews/TopRowNorthWalls'
R.mkdir(parents=True,exist_ok=True)
im=Image.open(P/'Source/adapted_raw.png').convert('RGB')
# Integer 6:1 reduction only. Narrow blank margins are extended, never device artwork.
modules=[('plain',0,180,32),('window',186,336,32),('service',630,804,32),('control_alert',810,1092,64),('system',1098,1404,64),('alert',1410,1716,64),('door',1722,2004,64),('switch',2010,2172,32)]
palette=im.quantize(colors=256,method=Image.Quantize.MEDIANCUT)
items=[]
atlas=Image.new('RGBA',(384,64)); x=0
# Reuse a source-derived cap/base profile, identical across modules.
cap=im.crop((30,174,36,210)).resize((1,6),Image.Resampling.NEAREST).convert('RGBA')
base=im.crop((30,524,36,548)).resize((1,4),Image.Resampling.NEAREST).convert('RGBA')
for name,a,b,w in modules:
 raw=im.crop((a,210,b,522))
 face=raw.resize(((b-a)//6,52),Image.Resampling.NEAREST).convert('RGBA')
 tile=Image.new('RGBA',(w,64))
 for xx in range(w):
  tile.paste(cap,(xx,0));tile.paste(cap.crop((0,4,1,6)),(xx,6));tile.paste(base,(xx,60))
 # Add source panel margin columns on either side; no feature is stretched.
 extra=w-face.width; left=extra//2
 margin=im.crop((60,210,66,522)).resize((1,52),Image.Resampling.NEAREST).convert('RGBA')
 expanded=Image.new('RGBA',(w,52))
 expanded.paste(face,(left,0))
 for xx in range(left): expanded.paste(margin,(xx,0))
 for xx in range(left+face.width,w): expanded.paste(margin,(xx,0))
 tile.paste(expanded,(0,8))
 if name in ('plain','window','service'):
  detail_base=im.crop((a,524,b,548)).resize((face.width,4),Image.Resampling.NEAREST).convert('RGBA')
  tile.paste(detail_base,(left,60))
 tile.save(P/f'Tiles/north_{name}.png');atlas.paste(tile,(x,0))
 items.append({'name':'north_'+name,'size':[w,64],'atlas_rect':[x,0,w,64],'pivot':[0.5,0],'source_raw_box':[a,210,b,522]});x+=w
atlas.save(P/'north_wall_atlas.png')
atlas.resize((1536,256),Image.Resampling.NEAREST).save(R/'wall_strip_4x.png')
review=Image.new('RGB',(1024,640),'#20252d');d=ImageDraw.Draw(review)
for i,item in enumerate(items):
 tile=Image.open(P/f'Tiles/{item["name"]}.png');col=i%4;row=i//4
 px=col*256+(256-tile.width*3)//2;py=row*320+40
 review.paste(tile.resize((tile.width*3,192),Image.Resampling.NEAREST),(px,py),tile.resize((tile.width*3,192),Image.Resampling.NEAREST))
 d.text((col*256+16,py+204),f'{item["name"]}  {tile.width} x 64',fill='white')
 review.paste(tile,(col*256+96,py+225),tile)
review.save(R/'contact_sheet.png')
# Repetition evidence, no new room art.
repeat=Image.new('RGBA',(96,192))
tile=Image.open(P/'Tiles/north_plain.png')
for yy in range(3):
 for xx in range(3):repeat.paste(tile,(xx*32,yy*64))
repeat.resize((384,768),Image.Resampling.NEAREST).save(R/'plain_repeat_3x3.png')
assert x==384
for it in items:
 tile=Image.open(P/f'Tiles/{it["name"]}.png');assert tile.size==tuple(it['size']);assert tile.getextrema()[3]==(255,255)
 assert list(atlas.crop((it['atlas_rect'][0],0,it['atlas_rect'][0]+tile.width,64)).getdata())==list(tile.getdata())
(P/'manifest.json').write_text(json.dumps({'ppu':32,'cap_face_base':[8,52,4],'modules':items,'method':'Built-in image edit; integer 6:1 nearest-neighbor reduction; source-derived margin extension; source colors retained without palette reduction; shared source-derived cap/base','checks':{'dimensions':True,'opaque':True,'atlas_equivalence':True},'unity_validated':False},indent=2))
with zipfile.ZipFile(P/'north_walls.zip','w',zipfile.ZIP_DEFLATED) as z:
 for f in list((P/'Tiles').glob('*.png'))+[P/'north_wall_atlas.png',P/'manifest.json']:
  z.write(f,f.relative_to(P))
print(P/'north_walls.zip')
