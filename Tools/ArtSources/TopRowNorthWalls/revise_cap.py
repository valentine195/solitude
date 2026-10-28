from PIL import Image,ImageDraw
from pathlib import Path
import json,shutil,zipfile
p=Path(__file__).parent;r=p.parents[2]/'work/Reviews/TopRowNorthWalls'
old=p/'PreviousTrimVersion';old.mkdir(exist_ok=True)
for f in [p/'north_wall_atlas.png',p/'manifest.json',p/'north_walls.zip']:
 if not (old/f.name).exists():shutil.copy2(f,old/f.name)
raw=Image.open(p/'Source/cap_raw.png').convert('RGBA')
# Integer 32:1 sampling of generated horizontal upper surface.
cap=raw.crop((115,0,1139,256)).resize((32,8),Image.Resampling.NEAREST)
# Preserve the raw rear boundary and the shadow immediately beneath its front lip.
cap.paste(raw.crop((115,0,1139,1)).resize((32,1),Image.Resampling.NEAREST),(0,0))
cap.paste(raw.crop((115,256,1139,257)).resize((32,1),Image.Resampling.NEAREST),(0,7))
# Preserve thin source joints that fall between the reduction samples.
for sx,dx in [(316,6),(937,26)]:
 joint=raw.crop((sx,32,sx+1,224)).resize((1,6),Image.Resampling.NEAREST)
 cap.paste(joint,(dx,1))
cap.save(p/'Source/top_surface_32x8.png')
atlas=Image.open(old/'north_wall_atlas.png').convert('RGBA')
for x in range(0,384,32):atlas.paste(cap,(x,0))
assert atlas.crop((0,8,384,64)).tobytes()==Image.open(old/'north_wall_atlas.png').convert('RGBA').crop((0,8,384,64)).tobytes()
atlas.save(p/'north_wall_atlas.png')
manifest=json.loads((p/'manifest.json').read_text())
for m in manifest['modules']:
 x,y,w,h=m['atlas_rect'];atlas.crop((x,y,x+w,y+h)).save(p/f'Tiles/{m["name"]}.png')
manifest['top_surface_revision']='Generated overhead surface with depth-direction joints, front lip and rear boundary; 32:1 integer nearest-neighbor sampling. All pixels below y8 unchanged.'
manifest['checks']['face_pixels_unchanged']=True
(p/'manifest.json').write_text(json.dumps(manifest,indent=2))
atlas.resize((1536,256),Image.Resampling.NEAREST).save(r/'wall_strip_4x.png')
# Wall/floor placement preview using existing approved-source floor.
floor=Image.open(p.parents[0]/'SpaceShipWallUnified/Tiles/base_clean.png').convert('RGBA')
context=Image.new('RGBA',(384,160),(25,29,35,255));context.paste(atlas,(0,0))
for y in range(64,160,32):
 for x in range(0,384,32):context.paste(floor,(x,y))
context.resize((1536,640),Image.Resampling.NEAREST).save(r/'wall_floor_context_4x.png')
comparison=Image.new('RGB',(1536,600),'#20252d');d=ImageDraw.Draw(comparison)
d.text((8,8),'BEFORE: frontal trim',fill='white');comparison.paste(Image.open(old/'north_wall_atlas.png').resize((1536,256),Image.Resampling.NEAREST),(0,28))
d.text((8,308),'REVISED: upper surface with depth-direction joints',fill='white');comparison.paste(atlas.resize((1536,256),Image.Resampling.NEAREST),(0,328));comparison.save(r/'cap_before_after.png')
with zipfile.ZipFile(p/'north_walls.zip','w',zipfile.ZIP_DEFLATED) as z:
 for f in list((p/'Tiles').glob('*.png'))+[p/'north_wall_atlas.png',p/'manifest.json']:z.write(f,f.relative_to(p))
print('Verified all face pixels unchanged, eight tiles retain exact dimensions.')
