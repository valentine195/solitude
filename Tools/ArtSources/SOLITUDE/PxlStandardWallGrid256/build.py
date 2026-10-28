from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
from zipfile import ZipFile,ZIP_DEFLATED
import subprocess,json,os,re,hashlib
root=Path(__file__).resolve().parent;review=root.parents[3]/'work/Reviews/PxlStandardWallGrid256';review.mkdir(parents=True,exist_ok=True)
pxl=Path.home()/'.codex/skills/pxl-pixel-art/bin/pxl.js'
ase=os.environ.get('ASEPRITE_BIN',str(Path.home()/'Applications/aseprite/build/bin/aseprite'))
subprocess.run(['node',str(pxl),'build',str(root/'wall.px.js'),'--scale','4'],check=True)
for p in (root/'out/sprites/standard_wall_256').glob('*.png'):
 target=root/'Editable/LayerInputs'/p.name;target.parent.mkdir(parents=True,exist_ok=True);Image.open(p).convert('RGBA').save(target)
subprocess.run([ase,'-b','--script-param','root='+str(root),'--script',str(root/'export_aseprite.lua')],check=True)
def load(p):return Image.open(p).convert('RGBA')
im=load(root/'out/sprites/standard_wall_256.png');repeat=load(root/'out/scenes/three_panels.png');grid=load(root/'out/scenes/grid_3x3.png')
assert im.size==(256,256) and repeat.size==(768,256) and grid.size==(768,768)
assert im.getchannel('A').getextrema()==(255,255)
allowed={tuple(bytes.fromhex(h))+(255,) for h in re.findall(r'#([0-9a-fA-F]{6})',(root/'wall.px.js').read_text())}
assert set(im.get_flattened_data())<=allowed
assert all(im.getpixel((0,y))==im.getpixel((255,y)) for y in range(256))
for y in range(149,166):assert len({im.getpixel((x,y)) for x in range(256)})==1
composite=Image.new('RGBA',im.size)
for name in ['panel_material','panel_breaks','upper_recess','panel_outlines','teal_stripe']:
 layer=load(root/f'out/sprites/standard_wall_256/{name}.png');assert layer.size==im.size;assert set(layer.getchannel('A').get_flattened_data())<={0,255};composite.alpha_composite(layer)
assert composite.tobytes()==im.tobytes()==load(root/'Editable/roundtrip.png').tobytes()
for y in range(3):
 for x in range(3):assert grid.crop((x*256,y*256,(x+1)*256,(y+1)*256)).tobytes()==im.tobytes()
for x in range(3):assert repeat.crop((x*256,0,(x+1)*256,256)).tobytes()==im.tobytes()
for scale in [2,4]:im.resize((256*scale,256*scale),Image.Resampling.NEAREST).save(review/f'standard_wall_{scale}x.png')
repeat.save(review/'three_panels_native.png');repeat.resize((3072,1024),Image.Resampling.NEAREST).save(review/'three_panels_4x.png')
cp=subprocess.run(['node',str(pxl),'ascii',str(root/'wall.px.js'),'standard_wall_256'],capture_output=True,text=True,check=True);(review/'pixel_inspection.txt').write_text(cp.stdout)
report={'passed':True,'dimensions':[256,256],'world_grid_cells':[8,8],'world_cell_size':[32,32],'source_reference_dimensions':list(Image.open(root/'Reference/supplied_wall.jpg').size),'source_reference_sha256':hashlib.sha256((root/'Reference/supplied_wall.jpg').read_bytes()).hexdigest(),'native_drawing_no_reference_resampling':True,'colors':len(set(im.get_flattened_data())),'declared_palette_only':True,'fully_opaque':True,'overlay_alpha_values':[0,255],'horizontal_edge_mismatches':0,'teal_stripe_continuous':True,'layer_recomposition_exact':True,'saved_aseprite_roundtrip_exact':True,'vertical_repetition':'Not intended; upper channel and bottom plinth are terminal features','unity_imported':False}
(root/'validation.json').write_text(json.dumps(report,indent=2)+'\n')
c=Image.new('RGB',(832,942),'#151c24');d=ImageDraw.Draw(c);font='/System/Library/Fonts/Supplemental/Arial.ttf'
def text(x,y,s,n=18):d.text((x,y),s,font=ImageFont.truetype(font,n),fill='#e0e6e6')
text(32,22,'SOLITUDE / STANDARD WALL / NATIVE 256',25);text(32,62,'Native pixel geometry / 14-color palette / preserved reference features',17)
c.paste(im.resize((512,512),Image.Resampling.NEAREST),(160,103));text(32,636,'2x nearest-neighbor / no smoothing',16)
text(32,681,'THREE ADJACENT PANELS / NATIVE 1:1',18);c.paste(repeat,(32,718));
# Ensure the native repeat fits completely in the review canvas.
full=Image.new('RGB',(832,998),'#151c24');full.paste(c,(0,0));full.paste(repeat,(32,718));full.save(review/'review_sheet.png')
with ZipFile(review/'standard_wall_256.zip','w',ZIP_DEFLATED) as z:
 for p in root.rglob('*'):
  if p.is_file():z.write(p,Path('PxlStandardWallGrid256')/p.relative_to(root))
 for p in review.glob('*.png'):z.write(p,Path('Review')/p.name)
 z.write(review/'pixel_inspection.txt','Review/pixel_inspection.txt')
print(json.dumps(report,indent=2))
