from PIL import Image,ImageDraw
from pathlib import Path
import json,zipfile,shutil
P=Path(__file__).parent;R=P.parents[2]/'work/Reviews/TopRowNorthWalls'
previous=P/'PreviousSegmentedVersion';previous.mkdir(exist_ok=True)
for f in [P/'north_wall_atlas.png',P/'manifest.json']:
 if not (previous/f.name).exists():shutil.copy2(f,previous/f.name)
ref=Image.open(P/'Source/user_cap_reference.png').convert('RGBA')
# Exact source cap, 4:1 integer reduction. Width adjusted only in its flat middle.
cap=ref.crop((6,5,250,37)).resize((61,8),Image.Resampling.NEAREST)
cap.save(P/'Source/reference_cap_native.png')
caps={}
for w in (32,64):
 out=Image.new('RGBA',(w,8));out.paste(cap.crop((0,0,16,8)),(0,0));out.paste(cap.crop((45,0,61,8)),(w-16,0))
 if w==64:
  out.paste(cap.crop((16,0,45,8)),(16,0))
  for x in range(45,48):out.paste(cap.crop((44,0,45,8)),(x,0))
 caps[w]=out;out.save(P/f'Source/reference_cap_{w}x8.png')
manifest=json.loads((P/'manifest.json').read_text())
atlas=Image.open(P/'PreviousTrimVersion/north_wall_atlas.png').convert('RGBA');before=atlas.copy()
for m in manifest['modules']:
 x,y,w,h=m['atlas_rect'];atlas.paste(caps[w],(x,0));tile=atlas.crop((x,0,x+w,64));tile.save(P/f'Tiles/{m["name"]}.png')
 assert tile.size==(w,64)
assert atlas.crop((0,8,384,64)).tobytes()==before.crop((0,8,384,64)).tobytes()
atlas.save(P/'north_wall_atlas.png')
manifest['top_surface_revision']='Direct extraction from user cap reference (6,5,250,37), reduced exactly 4:1 nearest-neighbor. Preserve cap ends; crop/extend flat center to 32/64 widths. No generated replacement.'
manifest['checks']['face_pixels_unchanged']=True
(P/'manifest.json').write_text(json.dumps(manifest,indent=2))
atlas.resize((1536,256),Image.Resampling.NEAREST).save(R/'wall_strip_4x.png')
items=manifest['modules'];s=(P/'process.py').read_text();exec(s[s.index('review=Image.new'):s.index('assert x==384')])
comparison=Image.new('RGB',(1536,600),'#20252d');d=ImageDraw.Draw(comparison)
d.text((8,8),'PREVIOUS',fill='white');comparison.paste(Image.open(previous/'north_wall_atlas.png').resize((1536,256),Image.Resampling.NEAREST),(0,28))
d.text((8,308),'CAP EXTRACTED FROM YOUR REFERENCE',fill='white');comparison.paste(atlas.resize((1536,256),Image.Resampling.NEAREST),(0,328));comparison.save(R/'cap_before_after.png')
# Replace previous context with current tiles, retaining its existing floor pixels.
context=Image.open(R/'wall_floor_context_4x.png');context.paste(atlas.resize((1536,256),Image.Resampling.NEAREST),(0,0));context.save(R/'wall_floor_context_4x.png')
with zipfile.ZipFile(P/'north_walls.zip','w',zipfile.ZIP_DEFLATED) as z:
 for f in list((P/'Tiles').glob('*.png'))+[P/'north_wall_atlas.png',P/'manifest.json']:z.write(f,f.relative_to(P))
print('8 tiles verified; all face/base pixels unchanged; reference cap applied at 4:1 integer reduction.')
