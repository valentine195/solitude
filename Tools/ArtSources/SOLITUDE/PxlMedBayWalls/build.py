from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
from zipfile import ZipFile,ZIP_DEFLATED
import subprocess,os,json,hashlib
root=Path(__file__).resolve().parent
review=root.parents[3]/'work/Reviews/PxlMedBayWalls'
review.mkdir(parents=True,exist_ok=True)
pxl=Path.home()/'.codex/skills/pxl-pixel-art/bin/pxl.js'
ase=os.environ.get('ASEPRITE_BIN',str(Path.home()/'Applications/aseprite/build/bin/aseprite'))
subprocess.run(['node',str(pxl),'build',str(root/'med_bay_walls.px.js'),'--scale','3'],check=True)
for p in (root/'out/sprites').rglob('*.png'):
 target=root/'Editable/LayerInputs'/p.relative_to(root/'out/sprites');target.parent.mkdir(parents=True,exist_ok=True)
 Image.open(p).convert('RGBA').save(target)
subprocess.run([ase,'-b','--script-param','root='+str(root),'--script',str(root/'export_aseprite.lua')],check=True)
def load(p):return Image.open(p).convert('RGBA')
states=['plain','utility','supply']; images={};results={}
for name in states:
 im=load(root/f'out/sprites/wall_med_bay_{name}.png'); images[name]=im
 wall=load(root/f'out/sprites/wall_med_bay_{name}/wall_face.png');fixture=load(root/f'out/sprites/overlay_med_bay_{name}.png')
 assert im.size==(128,256) and wall.size==fixture.size==im.size
 assert im.getchannel('A').getextrema()==(255,255)
 assert set(fixture.getchannel('A').get_flattened_data())=={0,255}
 composite=wall.copy();composite.alpha_composite(fixture);assert composite.tobytes()==im.tobytes()
 assert load(root/f'Editable/roundtrip_{name}.png').tobytes()==im.tobytes()
 assert all(fixture.getpixel((x,y))[3]==0 for y in range(256) for x in list(range(12))+list(range(116,128)))
 for nx,ny,label in [(3,1,'repeat'),(3,3,'grid')]:
  repeat=load(root/f'out/scenes/{label}_{name}.png');assert repeat.size==(128*nx,256*ny)
  for xx in range(nx):
   for yy in range(ny):assert repeat.crop((128*xx,256*yy,128*(xx+1),256*(yy+1))).tobytes()==im.tobytes()
 cp=subprocess.run(['node',str(pxl),'ascii',str(root/'med_bay_walls.px.js'),'wall_med_bay_'+name],capture_output=True,text=True,check=True)
 (review/f'inspect_{name}.txt').write_text(cp.stdout)
 results[name]={'size':[128,256],'fully_opaque':True,'full_height_wall_face':True,'baked_wall_cap':False,'independent_fixture_overlay':True,'overlay_recomposition_exact':True,'aseprite_roundtrip_exact':True,'opaque_palette_colors':len(set(im.get_flattened_data())),'sha256':hashlib.sha256(im.tobytes()).hexdigest()}
for a in states:
 for b in states:
  assert all(images[a].getpixel((127,y))==images[b].getpixel((0,y)) for y in range(256))
results['all_variant_horizontal_pairings']={'pairings_checked':9,'edge_mismatches':0}
(root/'validation.json').write_text(json.dumps({'passed':True,'results':results,'unity_imported':False,'vertical_tiling':'Not intended: this is a full-height wall face, with independent caps layered above.'},indent=2)+'\n')
# Review artifact only; native image or exact 2x nearest-neighbor.
canvas=Image.new('RGB',(1080,1100),'#141b23');d=ImageDraw.Draw(canvas)
font='/System/Library/Fonts/Supplemental/Arial.ttf'
def text(x,y,s,size=18,color='#c9d0da'):d.text((x,y),s,font=ImageFont.truetype(font,size),fill=color)
text(40,25,'SOLITUDE / MED BAY WALLS',30,'#eff2ee')
text(40,70,'Full 128 x 256 wall faces  /  separate wall caps  /  native pixel grid',19)
for i,(name,title,subtitle) in enumerate([('plain','CLINICAL PANEL','Sanitary cladding + zone marker'),('utility','BEDSIDE UTILITIES','Gas ports + call control + service access'),('supply','MEDICAL SUPPLIES','Inset twin doors + sealed drawer')]):
 x=40+i*352;text(x,118,title,20,'#e3d3ae');canvas.paste(images[name].resize((256,512),Image.Resampling.NEAREST),(x,155));text(x,684,subtitle,15)
text(40,738,'MIXED WALL RUN / SIX ADJACENT PANELS / NATIVE SIZE',20,'#eff2ee')
run=load(root/'out/scenes/med_bay_wall_run.png');canvas.paste(run,(156,782))
text(40,1064,'All variants: opaque to every canvas edge. Fixtures exported as transparent overlays.',16)
canvas.save(review/'contact_sheet.png')
with ZipFile(review/'med_bay_walls.zip','w',ZIP_DEFLATED) as z:
 for p in root.rglob('*'):
  if p.is_file():z.write(p,Path('PxlMedBayWalls')/p.relative_to(root))
 z.write(review/'contact_sheet.png','Review/contact_sheet.png')
print(json.dumps(results,indent=2))
