"""Native PXL floor export, palette/seam QC, editable handoff and review artifacts."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
from zipfile import ZipFile,ZIP_DEFLATED
import subprocess,os,json,re,hashlib
root=Path(__file__).resolve().parent;review=root.parents[3]/'work/Reviews/PxlStandardFloor256';review.mkdir(parents=True,exist_ok=True)
pxl=Path.home()/'.codex/skills/pxl-pixel-art/bin/pxl.js'
ase=os.environ.get('ASEPRITE_BIN',str(Path.home()/'Applications/aseprite/build/bin/aseprite'))
subprocess.run(['node',str(pxl),'build',str(root/'floor.px.js'),'--scale','4'],check=True)
for p in (root/'out/sprites/floor_standard_256').glob('*.png'):
 target=root/'Editable/LayerInputs'/p.name;target.parent.mkdir(parents=True,exist_ok=True);Image.open(p).convert('RGBA').save(target)
subprocess.run([ase,'-b','--script-param','root='+str(root),'--script',str(root/'export_aseprite.lua')],check=True)
def load(p):return Image.open(p).convert('RGBA')
im=load(root/'out/sprites/floor_standard_256.png');repeat=load(root/'out/scenes/floor_repeat_3x3.png')
wall_root=root.parent/'PxlStandardWallGrid256';wall=load(wall_root/'out/sprites/standard_wall_256.png')
wall_palette={tuple(bytes.fromhex(h))+(255,) for h in re.findall(r'#([0-9a-fA-F]{6})',(wall_root/'wall.px.js').read_text())}
assert im.size==(256,256) and im.width%32==0 and im.height%32==0
assert im.getchannel('A').getextrema()==(255,255)
assert set(im.get_flattened_data())<=wall_palette
assert set(im.crop((40,40,216,216)).get_flattened_data())=={(238,241,239,255)}
assert im.getpixel((128,128))==wall.getpixel((128,100))==(238,241,239,255)
hm=sum(im.getpixel((0,y))!=im.getpixel((255,y)) for y in range(256))
vm=sum(im.getpixel((x,0))!=im.getpixel((x,255)) for x in range(256));assert hm==vm==0
composite=Image.new('RGBA',im.size)
for name in ['surface','perimeter_frame','inset_panel']:
 layer=load(root/f'out/sprites/floor_standard_256/{name}.png');assert layer.size==im.size;assert set(layer.getchannel('A').get_flattened_data())<={0,255};composite.alpha_composite(layer)
assert composite.tobytes()==im.tobytes()==load(root/'Editable/roundtrip.png').tobytes()
assert repeat.size==(768,768)
for y in range(3):
 for x in range(3):assert repeat.crop((x*256,y*256,(x+1)*256,(y+1)*256)).tobytes()==im.tobytes()
for scale in [2,4]:im.resize((256*scale,256*scale),Image.Resampling.NEAREST).save(review/f'floor_standard_{scale}x.png')
repeat.save(review/'floor_repeat_3x3_native.png')
cp=subprocess.run(['node',str(pxl),'ascii',str(root/'floor.px.js'),'floor_standard_256'],capture_output=True,text=True,check=True);(review/'pixel_inspection.txt').write_text(cp.stdout)
report={'passed':True,'dimensions':[256,256],'world_grid_cells':[8,8],'source_reference_dimensions':list(Image.open(root/'Reference/prototype_floor.png').size),'source_reference_sha256':hashlib.sha256((root/'Reference/prototype_floor.png').read_bytes()).hexdigest(),'main_surface_color':'#EEF1EF','main_surface_matches_wall_exactly':True,'output_colors':len(set(im.get_flattened_data())),'all_colors_from_wall_palette':True,'fully_opaque':True,'layer_alpha_only_0_or_255':True,'horizontal_edge_mismatches':hm,'vertical_edge_mismatches':vm,'repeat_cells_verified':9,'layer_composition_exact':True,'reopened_aseprite_roundtrip_exact':True,'native_art_no_resampling':True,'unity_modified':False}
(root/'validation.json').write_text(json.dumps(report,indent=2)+'\n')
c=Image.new('RGB',(1408,1008),'#151c24');d=ImageDraw.Draw(c);font='/System/Library/Fonts/Supplemental/Arial.ttf'
def text(x,y,s,n=18,color='#dfe5e5'):d.text((x,y),s,font=ImageFont.truetype(font,n),fill=color)
text(32,24,'SOLITUDE / STANDARD FLOOR / 256 x 256',29)
text(32,68,'Same #EEF1EF surface and neutral ramp as the standard wall / native pixel geometry',19)
text(32,112,'FLOOR / 2x NEAREST-NEIGHBOR',18);c.paste(im.resize((512,512),Image.Resampling.NEAREST),(32,152))
text(608,112,'3 x 3 REPETITION / NATIVE 1:1',18);c.paste(repeat,(608,152))
text(32,702,'WALL COLOR COMPARISON / NATIVE 1:1',16);c.paste(wall,(32,738))
text(316,758,'Both main faces:',18);text(316,789,'#EEF1EF',22)
d.rectangle((316,832,492,880),fill='#eef1ef');text(316,899,'No new palette colors.',16)
c.save(review/'review_sheet.png')
with ZipFile(review/'floor_standard_256.zip','w',ZIP_DEFLATED) as z:
 for p in root.rglob('*'):
  if p.is_file():z.write(p,Path('PxlStandardFloor256')/p.relative_to(root))
 for p in review.glob('*.png'):z.write(p,Path('Review')/p.name)
 z.write(review/'pixel_inspection.txt','Review/pixel_inspection.txt')
 # Include the two existing wall files used by the color validation.
 for rel in ['wall.px.js','out/sprites/standard_wall_256.png']:z.write(wall_root/rel,Path('PxlStandardWallGrid256')/rel)
print(json.dumps(report,indent=2))
