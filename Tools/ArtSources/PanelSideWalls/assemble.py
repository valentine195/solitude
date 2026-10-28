from pathlib import Path
from PIL import Image,ImageOps,ImageDraw
import json,zipfile
P=Path(__file__).parent;R=P.parents[2]/'work/Reviews/PanelSideWalls'
(P/'Strips').mkdir(exist_ok=True);(P/'Tiles32x64').mkdir(exist_ok=True)
processed=Image.open(P/'Processed/single-1.png').convert('RGBA')
# A straight cap has no lengthwise features: use one generated cross-section
# to remove incidental shading noise and guarantee exact repeat joins.
profile=processed.crop((12,48,20,49))
repeat=Image.new('RGBA',(8,32))
for y in range(32):repeat.paste(profile,(0,y))
raw=Image.open(P/'Source/side_walls_raw.png').convert('RGBA')
ends={
 'top':raw.crop((313,155,409,156)).resize((8,1),Image.Resampling.NEAREST),
 'bottom':raw.crop((313,1098,409,1099)).resize((8,1),Image.Resampling.NEAREST)
}
# Snap terminal colors to the generated cap palette, eliminating dark chroma fringe.
colors=list(profile.getdata())
for name,img in ends.items():
 cleaned=Image.new('RGBA',img.size)
 cleaned.putdata([min(colors,key=lambda c:sum((c[i]-v[i])**2 for i in range(3))) for v in img.getdata()])
 ends[name]=cleaned
atlas=Image.new('RGBA',(64,96));manifest=[]
for side in ('west','east'):
 for row,part in enumerate(('top','repeat','bottom')):
  strip=repeat.copy()
  if part=='top':strip.paste(ends['top'],(0,0))
  if part=='bottom':strip.paste(ends['bottom'],(0,31))
  if side=='east':strip=ImageOps.mirror(strip)
  name=f'wall_{side}_{part}'
  strip.save(P/f'Strips/{name}_8x32.png')
  tile=Image.new('RGBA',(32,32));x=24 if side=='west' else 0;tile.paste(strip,(x,0))
  tile.save(P/f'Tiles32/{name}.png')
  ax=0 if side=='west' else 32;atlas.paste(tile,(ax,row*32))
  manifest.append({'name':name,'size':[32,32],'visible_width':8,'opaque_x':[x,x+8],'atlas_rect':[ax,row*32,32,32],'pivot':[.5,.5]})
 for_native=Image.open(P/f'Tiles32/wall_{side}_repeat.png')
 tall=Image.new('RGBA',(32,64));tall.paste(for_native,(0,0));tall.paste(for_native,(0,32));tall.save(P/f'Tiles32x64/wall_{side}_repeat_32x64.png')
atlas.save(P/'side_wall_atlas_32.png')
# Pixel and seam checks across all valid end/repeat joins.
for side in ('west','east'):
 top=Image.open(P/f'Tiles32/wall_{side}_top.png');middle=Image.open(P/f'Tiles32/wall_{side}_repeat.png');bottom=Image.open(P/f'Tiles32/wall_{side}_bottom.png')
 for a,b in [(top,middle),(middle,middle),(middle,bottom)]:assert a.crop((0,31,32,32)).tobytes()==b.crop((0,0,32,1)).tobytes()
 for t in (top,middle,bottom):
  assert t.size==(32,32) and set(t.getchannel('A').getdata())=={0,255}
  assert not any(r>180 and b>160 and g<90 and a for r,g,b,a in t.getdata())
# Exact mirrors, including padding positions.
for part in ('top','repeat','bottom'):
 west=Image.open(P/f'Tiles32/wall_west_{part}.png');east=Image.open(P/f'Tiles32/wall_east_{part}.png');assert ImageOps.mirror(west).tobytes()==east.tobytes()
# Context composition uses the user's unchanged north sprite at integer 4:1 scale.
north=Image.open(P/'Source/north_wall_reference.png').convert('RGBA').resize((64,64),Image.Resampling.NEAREST)
context=Image.new('RGBA',(128,160),(29,34,43,255))
# Neutral review background only; no new floor artwork.
bg=Image.new('RGBA',(64,96),(110,119,129,255));context.paste(bg,(32,64));context.paste(north,(32,0))
for side,xx in [('west',0),('east',96)]:
 for y in range(0,160,32):
  part='top' if y==0 else ('bottom' if y==128 else 'repeat')
  tile=Image.open(P/f'Tiles32/wall_{side}_{part}.png');context.alpha_composite(tile,(xx,y))
context.save(R/'side_walls_context_native.png');context.resize((768,960),Image.Resampling.NEAREST).save(R/'side_walls_context_6x.png')
# Labeled exports show the transparency and grid placement.
board=Image.new('RGB',(768,480),'#222832');d=ImageDraw.Draw(board)
for i,m in enumerate(manifest):
 col=i//3;row=i%3;xx=col*384+24;yy=row*160+24
 tile=Image.open(P/f'Tiles32/{m["name"]}.png');large=tile.resize((128,128),Image.Resampling.NEAREST)
 board.paste(large,(xx,yy),large);d.text((xx+144,yy+38),m['name'],fill='white');d.text((xx+144,yy+56),'32 x 32 / 8px wall',fill='#b4bfcb')
board.save(R/'contact_sheet.png')
# Three parallel repeat runs, each three cells long.
for side in ('west','east'):
 rep=Image.new('RGBA',(96,96));tile=Image.open(P/f'Tiles32/wall_{side}_repeat.png')
 for y in range(3):
  for x in range(3):rep.paste(tile,(x*32,y*32))
 rep.resize((384,384),Image.Resampling.NEAREST).save(R/f'{side}_repeat_3x3.png')
meta={'ppu':32,'filter':'Point','compression':'None','mipmaps':False,'atlas_size':[64,96],'slice_grid':[32,32],'atlas_layout':'columns west/east; rows top/repeat/bottom','modules':manifest,'processing':'Built-in image edit; exact integer 12:1 nearest-neighbor reduction; source cross-section repeated for seamless straight wall; source-derived end rows; mirrored pair','checks':{'dimensions':True,'binary_alpha':True,'no_magenta':True,'vertical_joins':True,'mirrored_pair':True},'unity_integrated':False,'unity_validated':False}
(P/'manifest.json').write_text(json.dumps(meta,indent=2))
with zipfile.ZipFile(P/'panel_side_walls.zip','w',zipfile.ZIP_DEFLATED) as z:
 for folder in ('Tiles32','Strips','Tiles32x64'):
  for f in sorted((P/folder).glob('*.png')):z.write(f,f.relative_to(P))
 for f in [P/'side_wall_atlas_32.png',P/'manifest.json',P/'README.md']:z.write(f,f.name)
print('Six 32x32 tiles, two 32x64 repeats, six tight strips, atlas. All checks passed.')
