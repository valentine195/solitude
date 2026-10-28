from PIL import Image
from pathlib import Path
from zipfile import ZipFile,ZIP_DEFLATED
import random,json
root=Path(__file__).parent;p=root/'PNG'
names=['plain','soft_patch','wear_a','wear_b','soft_lift','worn_patch']
hexes=['A2A38E','9EA18D','989B88','A5A590'];pal=[tuple(bytes.fromhex(h)) for h in hexes]
ims=[]
for n in names:
 im=Image.open(p/f'floor_{n}_256.png').convert('RGB').resize((32,32),Image.Resampling.NEAREST)
 out=Image.new('RGBA',(32,32));out.putdata([(*min(pal,key=lambda c:sum((c[i]-v[i])**2 for i in range(3))),255) for v in im.get_flattened_data()])
 out.save(p/f'floor_{n}_32.png');out.resize((64,64),Image.Resampling.NEAREST).save(p/f'floor_{n}_64.png');out.resize((512,512),Image.Resampling.NEAREST).save(p/f'floor_{n}_64_8x.png');ims.append(out)
for a in ims:
 for b in ims:
  assert list(a.crop((31,0,32,32)).get_flattened_data())==list(b.crop((0,0,1,32)).get_flattened_data())
  assert list(a.crop((0,31,32,32)).get_flattened_data())==list(b.crop((0,0,32,1)).get_flattened_data())
atlas=Image.new('RGBA',(96,64))
for i,im in enumerate(ims):atlas.paste(im,(i%3*32,i//3*32))
atlas.save(p/'floor_atlas_32.png');atlas.resize((768,512),Image.Resampling.NEAREST).save(p/'floor_tiles_8x_native.png');atlas.resize((1536,1024),Image.Resampling.NEAREST).save(p/'floor_tiles_8x_64.png')
sheet=Image.new('RGBA',(768,512))
for i,n in enumerate(names):sheet.paste(Image.open(p/f'floor_{n}_256.png'),(i%3*256,i//3*256))
sheet.save(p/'floor_original_renders.png')
rng=random.Random(41);patch=Image.new('RGBA',(384,256))
for y in range(8):
 for x in range(12):patch.paste(ims[rng.choices(range(6),weights=[65,8,7,7,8,5])[0]],(x*32,y*32))
patch.save(p/'floor_mixed_native.png');patch.resize((1152,768),Image.Resampling.NEAREST).save(p/'floor_mixed_3x.png')
# Repeat each individual tile in a 3x3 field.
repeat=Image.new('RGBA',(288,192))
for i,im in enumerate(ims):
 for y in range(3):
  for x in range(3):repeat.paste(im,((i%3)*96+x*32,(i//3)*96+y*32))
repeat.resize((864,576),Image.Resampling.NEAREST).save(p/'floor_repeat_3x3.png')
# Static review composite using the previous study assets; no Unity import.
review=patch.copy()
w=Path('/Users/jeremyvalentine/SOLITUDE/Tools/ArtSources/SOLITUDE/NorthWallPanels/PNG')
for i,n in enumerate(['plain','plain','service','plain','vent','plain']):review.alpha_composite(Image.open(w/f'north_wall_{n}_64.png'),(i*64,0))
v=Image.open('/Users/jeremyvalentine/SOLITUDE/Tools/ArtSources/SOLITUDE/VentilationUnit/PNG/ventilation_unit_64.png')
review.alpha_composite(v,(160,120));review.resize((1152,768),Image.Resampling.NEAREST).save(p/'floor_context_review_3x.png')
report={'native_size':[32,32],'compatibility_size':[64,64],'palette':hexes,'variants':names,'all_36_horizontal_pairs_match':True,'all_36_vertical_pairs_match':True,'opaque_floor':True,'plain_weight_percent':65,'unity_imported':False}
(root/'validation.json').write_text(json.dumps(report,indent=2)+'\n')
(root/'README.md').write_text('''# Quiet floor tiles

Six interchangeable, opaque 32x32 floor tiles, rendered in Blender from directly above. Native stride is 32px, matching SOLITUDE. 64px versions are exact 2x nearest-neighbor copies: use those only at the corresponding pixel density. All horizontal and vertical edge pairings match exactly. Floor tiles fill their frames and intentionally have no transparent gaps.

Order in the 3-column, 2-row atlas: plain, soft_patch, wear_a; wear_b, soft_lift, worn_patch. Favor plain (65% in the mixed preview). Palette extends the existing warm neutral-gray floor ramp with very close neighboring tones. Avoid rotating the full atlas during slicing; each cell is exactly 32x32 without gutters.

Improvement pass reduced rectangular tone-patch size and color contrast and softened wear. The result intentionally recedes; no tile frames, bolts, grates, bright highlights, or raised bevels. Original V1 retained separately. Each tile was checked in 3x3 repetition and in a mixed patch. Context review is a static composite with earlier wall/vent studies, not Unity validation.

Model/quiet_floor_tiles.blend opens on the floor scene. Earlier Blender scenes were retained to preserve existing work. Floor models use shallow pixel-coordinate XY relief with flat emission colors, not physical meter dimensions. PNG outputs and Blender source are separate. No Unity imports.
''')
with ZipFile(root/'quiet_floor_png_outputs.zip','w',ZIP_DEFLATED) as z:
 for f in sorted(p.glob('*.png')):z.write(f,'PNG/'+f.name)
 for f in ['README.md','validation.json']:z.write(root/f,f)
print(json.dumps(report,indent=2))
