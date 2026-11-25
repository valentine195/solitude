from PIL import Image
from pathlib import Path
import random,json
from zipfile import ZipFile,ZIP_DEFLATED
r=Path(__file__).parent;p=r/'PNG';t=r/'Tiles32'
names=['plate','worn','service','drain','infill'];hexes=['56656E','485861','5C6B73','3B4B55','7B8C91','617079'];pal=[tuple(bytes.fromhex(h)) for h in hexes];ims={};manifest=[]
for name in names:
 size=32 if name=='infill' else 64
 src=Image.open(p/f'deck_{name}_256.png').convert('RGB').resize((size,size),Image.Resampling.NEAREST)
 im=Image.new('RGBA',(size,size));im.putdata([(*min(pal,key=lambda c:sum((c[i]-v[i])**2 for i in range(3))),255) for v in src.get_flattened_data()])
 im.save(p/f'deck_{name}_{size}.png');im.resize((size*8,size*8),Image.Resampling.NEAREST).save(p/f'deck_{name}_8x.png');ims[name]=im
 rebuilt=Image.new('RGBA',im.size)
 for y in range(size//32):
  for x in range(size//32):
   tile=im.crop((x*32,y*32,(x+1)*32,(y+1)*32));fn=f'deck_{name}_{x}_{y}.png';tile.save(t/fn);rebuilt.paste(tile,(x*32,y*32));manifest.append({'file':fn,'plate':name,'quadrant':[x,y]})
 assert rebuilt.tobytes()==im.tobytes()
 assert set(im.getchannel('A').get_flattened_data())=={255}
 # Repetition check for all four full plates, plus infill.
 a=Image.new('RGBA',(size*3,size*3))
 for yy in range(3):
  for xx in range(3):a.paste(im,(xx*size,yy*size))
 a.resize((a.width*3,a.height*3),Image.Resampling.NEAREST).save(p/f'deck_{name}_repeat_3x3.png')
original=Image.new('RGBA',(512,512));sheet=Image.new('RGBA',(128,128))
for i,name in enumerate(names[:4]):original.paste(Image.open(p/f'deck_{name}_256.png'),(i%2*256,i//2*256));sheet.paste(ims[name],(i%2*64,i//2*64))
original.save(p/'deck_original_renders.png');sheet.save(p/'deck_plate_sheet_64.png');sheet.resize((1024,1024),Image.Resampling.NEAREST).save(p/'deck_plate_sheet_8x.png')
atlas=Image.new('RGBA',(128,160))
for i,m in enumerate(manifest):atlas.paste(Image.open(t/m['file']),(i%4*32,i//4*32))
atlas.save(p/'deck_tile_atlas_32.png')
rng=random.Random(12);field=Image.new('RGBA',(448,320))
for y in range(5):
 for x in range(-1,8):
  name=rng.choices(['plate','worn'],[80,20])[0]
  # Specialty tiles confined to maintenance side of the room.
  if y==3 and x==4:name='service'
  if y==4 and x in [0,5]:name='drain'
  field.paste(ims[name],(x*64+(32 if y%2 else 0),y*64))
field=field.crop((0,0,384,320));field.save(p/'deck_staggered_native.png');field.resize((1152,960),Image.Resampling.NEAREST).save(p/'deck_staggered_3x.png')
review=field.copy();w=r.parent/'NorthWallPanels'/'PNG'
for i,name in enumerate(['plain','plain','service','plain','vent','plain']):review.alpha_composite(Image.open(w/f'north_wall_{name}_64.png'),(i*64,0))
review.alpha_composite(Image.open(r.parent/'VentilationUnit'/'PNG'/'ventilation_unit_64.png'),(228,146))
review.save(p/'deck_context_native.png');review.resize((1152,960),Image.Resampling.NEAREST).save(p/'deck_context_3x.png')
report={'native_tile_pixels':32,'plate_pixels':64,'palette':hexes,'tile_count':len(manifest),'reassembly_pixel_exact':True,'all_floor_pixels_opaque':True,'tiling':'Repeat complete 2x2-cell plates; alternate rows may shift one cell. Quadrants are phase constrained, not randomly interchangeable.','tiles':manifest,'unity_imported':False}
(r/'tile_manifest.json').write_text(json.dumps(report,indent=2)+'\n')
(r/'README.md').write_text('''# SOLITUDE deck flooring V3

Research-inspired layout: larger replaceable deck plates, open circulation surfaces, and service detail concentrated at maintenance edges. Cool desaturated gray steel supports ivory equipment and walls.

Four 64x64 plate modules (plate, worn, service, drain), plus 32x32 plain infill. Each large plate spans 2x2 native world cells. Tiles32 contains the 17 native cells; the manifest identifies each cell quadrant. Assemble quadrants in order. They are not all randomly interchangeable. Randomize complete plate modules, favoring plain/worn; use service and drain only near corresponding equipment. The preview staggers alternate plate rows by one 32px cell to reduce the square-grid effect. Plain infill can extend plate interiors, but do not place it on a required seam.

Single-pixel seams are owned by the left and top of each plate, avoiding double-width joins. Seam highlights are restrained and uniform. No perspective, noisy texture, hazard striping, or per-tile bolts. Intended material joints remain visible; there are no transparent gaps. All five assets checked in 3x3 repetition. The 17 sliced tiles reassemble their source modules pixel-exactly. Nearest-neighbor exports use six colors and opaque alpha. Source renders are 256x256 per module; native art is 64x64 per plate and 32x32 infill.

PNG contains original renders, native modules, 8x enlargements, atlas, repetition checks, and a static composite with previous wall/vent assets. The context scene is an art review, not a Unity scene or runtime validation. No Unity import.

Model/deck_flooring_v3.blend opens on the deck scene, with earlier scenes preserved. Geometry uses pixel-coordinate shallow relief and flat emission colors under a directly-above orthographic camera. Previous floor studies remain untouched.
''')
with ZipFile(r/'deck_flooring_v3_png_outputs.zip','w',ZIP_DEFLATED) as z:
 for folder in [p,t]:
  for f in sorted(folder.glob('*.png')):z.write(f,str(f.relative_to(r)))
 for f in ['tile_manifest.json','README.md']:z.write(r/f,f)
print('Saved four macro plates, 17 native cells, previews, and PNG package. Exact slice reassembly and opaque-alpha checks passed.')
