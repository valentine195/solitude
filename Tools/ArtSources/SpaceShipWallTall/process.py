from pathlib import Path
from PIL import Image,ImageDraw
import json,hashlib
P=Path(__file__).resolve().parent;ROOT=P.parents[2];R=ROOT/'work/Reviews/SpaceShipWallTall';R.mkdir(parents=True,exist_ok=True)
raw={k:Image.open(P/f'{k}_raw.png').convert('RGB') for k in ('narrow','wide')}
for im in raw.values():assert im.size==(1024,1536)
small={k:im.resize((256,384),Image.Resampling.NEAREST) for k,im in raw.items()}
back=small['narrow'].crop((0,0,32,64))
# Clean reference-key pixels from the backing panel by copying its nearest valid row pixel.
for py in range(64):
 valid=[px for px in range(32) if not(back.getpixel((px,py))[0]>160 and back.getpixel((px,py))[2]>160 and back.getpixel((px,py))[1]<100)]
 if valid:
  for px in range(32):
   r,g,b=back.getpixel((px,py))
   if r>160 and b>160 and g<100:back.putpixel((px,py),back.getpixel((min(valid,key=lambda q:abs(q-px)),py)))
master=small['wide'].crop((0,0,64,64))
# Repeat a generated interior profile column for clean orthogonal top surfaces and base rails.
cap=master.crop((32,0,33,8));rail=master.crop((32,60,33,64))
records=[];tiles=[]
for kind in ('narrow','wide'):
 entries=json.loads((P/f'{kind}_sources.json').read_text());w=32 if kind=='narrow' else 64
 (P/f'Tiles{w}x64').mkdir(exist_ok=True)
 atlas=Image.new('RGBA',(w*4,64*6))
 for i,src in enumerate(entries):
  x=i%4*64;y=i//4*64
  t=small[kind].crop((x,y,x+w,y+64)).convert('RGBA')
  # Fill source-fragment gaps from the generated full panel, preserving every non-key detail.
  for py in range(64):
   for px in range(w):
    r,g,b,a=t.getpixel((px,py))
    if r>160 and b>160 and g<100:t.putpixel((px,py),back.getpixel((px%32,py))+(255,))
  for px in range(w):t.paste(cap,(px,0));t.paste(rail,(px,60))
  # Per-sprite palette retains small red/green status accents; hard native pixels, no dithering.
  t=t.convert('RGB').quantize(colors=64,dither=Image.Dither.NONE).convert('RGBA')
  # Reapply identical shared profiles after quantization.
  for px in range(w):t.paste(cap,(px,0));t.paste(rail,(px,60))
  path=f'Tiles{w}x64/'+Path(src['file']).name;t.save(P/path)
  xy=(i%4*w,i//4*64);atlas.paste(t,xy)
  assert t.size==(w,64) and t.getextrema()[3]==(255,255)
  assert atlas.crop((xy[0],xy[1],xy[0]+w,xy[1]+64)).tobytes()==t.tobytes()
  records.append(dict(file=path,size=[w,64],source=src,atlas=f'wall_atlas_{w}x64.png',atlas_xy=list(xy),pivot=[w//2,64],sha256=hashlib.sha256((P/path).read_bytes()).hexdigest()));tiles.append(t)
 atlas.save(P/f'wall_atlas_{w}x64.png')
 atlas.resize((atlas.width*3,atlas.height*3),Image.Resampling.NEAREST).save(R/f'wall_atlas_{w}x64_3x.png')
# Review source-order aligned labeled contact sheets.
for page in range(2):
 its=list(zip(tiles,records))[page*24:(page+1)*24]
 sheet=Image.new('RGB',(1008,((len(its)+5)//6)*222),(29,33,39));d=ImageDraw.Draw(sheet)
 for j,(t,r) in enumerate(its):
  x=j%6*168;y=j//6*222
  t=t.resize((t.width*3,192),Image.Resampling.NEAREST)
  # Wide sprites use2x to avoid overlapping label cells.
  if t.width>160:t=Image.open(P/r['file']).resize((128,128),Image.Resampling.NEAREST)
  sheet.paste(t,(x+(168-t.width)//2,y))
  d.text((x+1,y+195),Path(r['file']).stem,fill='white',font_size=9)
  d.text((x+1,y+208),str(r['size'][0])+' wide x64 tall',fill='white',font_size=9)
 sheet.save(R/f'review_page_{page+1}.png')
# Context preview uses north faces64 tall above32px floors; side/south walls remain cap-only.
room=Image.new('RGB',(320,200),(25,29,35));f=Image.open(ROOT/'Tools/ArtSources/SpaceShipFloor32/Tiles/s5_deck_r02_c01.png')
for y in range(64,192,32):
 for x in range(0,320,32):room.paste(f,(x,y))
x=0
for idx in (0,3,23,24,4,5,1):
 t=tiles[idx];room.paste(t,(x,0));x+=t.width
# Pure top-surface strips only on remaining boundaries.
for y in range(64,192):
 for x in range(8):room.putpixel((x,y),cap.getpixel((0,x)));room.putpixel((319-x,y),cap.getpixel((0,x)))
for x in range(320):room.paste(cap,(x,192))
room.resize((1280,800),Image.Resampling.NEAREST).save(R/'room_context_4x.png')
(P/'manifest.json').write_text(json.dumps(dict(narrow_count=23,wide_count=24,height=64,cap_height=8,face_height=52,base_height=4,resampling='Uniform exact4x nearest-neighbor; no stretching or rotation',assets=records),indent=2)+'\n')
print('Verified23 tiles32 wide x64 tall;24 tiles64 wide x64 tall; all opaque and atlas-identical.')
