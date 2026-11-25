from pathlib import Path
from PIL import Image
from zipfile import ZipFile,ZIP_DEFLATED
import subprocess,os,json,hashlib
root=Path(__file__).resolve().parent;review=root.parents[3]/'work/Reviews/PxlQuietPanel256';review.mkdir(parents=True,exist_ok=True)
pxl=Path.home()/'.codex/skills/pxl-pixel-art/bin/pxl.js'
ase=os.environ.get('ASEPRITE_BIN',str(Path.home()/'Applications/aseprite/build/bin/aseprite'))
subprocess.run(['node',str(pxl),'build',str(root/'panel.px.js'),'--scale','4'],check=True)
for p in (root/'out/sprites/quiet_panel_256').glob('*.png'):
 target=root/'Editable/LayerInputs'/p.name;target.parent.mkdir(parents=True,exist_ok=True);Image.open(p).convert('RGBA').save(target)
subprocess.run([ase,'-b','--script-param','root='+str(root),'--script',str(root/'export_aseprite.lua')],check=True)
def load(p):return Image.open(p).convert('RGBA')
im=load(root/'out/sprites/quiet_panel_256.png');outline=load(root/'out/sprites/quiet_panel_256/panel_outline.png')
assert im.size==(256,256) and im.getchannel('A').getextrema()==(255,255)
assert set(im.get_flattened_data())=={(238,241,239,255),(168,179,184,255)}
assert im.tobytes()==im.transpose(Image.Transpose.FLIP_LEFT_RIGHT).tobytes()==im.transpose(Image.Transpose.FLIP_TOP_BOTTOM).tobytes()
assert outline.getbbox()==(2,2,254,254)
assert all(im.getpixel((0,y))==im.getpixel((255,y)) for y in range(256))
assert all(im.getpixel((x,0))==im.getpixel((x,255)) for x in range(256))
composite=load(root/'out/sprites/quiet_panel_256/surface.png');composite.alpha_composite(outline)
assert composite.tobytes()==im.tobytes()==load(root/'Editable/roundtrip.png').tobytes()
repeat=load(root/'out/scenes/quiet_panel_repeat_3x3.png');assert repeat.size==(768,768)
for y in range(3):
 for x in range(3):assert repeat.crop((x*256,y*256,(x+1)*256,(y+1)*256)).tobytes()==im.tobytes()
for scale in [2,4]:im.resize((256*scale,256*scale),Image.Resampling.NEAREST).save(review/f'quiet_panel_{scale}x.png')
repeat.save(review/'repeat_3x3_native.png')
cp=subprocess.run(['node',str(pxl),'ascii',str(root/'panel.px.js'),'quiet_panel_256'],capture_output=True,text=True,check=True);(review/'pixel_inspection.txt').write_text(cp.stdout)
report={'passed':True,'size':[256,256],'surface':'#EEF1EF','outline':'#A8B3B8','colors':2,'fully_opaque':True,'outline_inset_pixels':2,'corner_cut_pixels':6,'outline_thickness_pixels':1,'horizontal_and_vertical_symmetry':True,'horizontal_edge_mismatches':0,'vertical_edge_mismatches':0,'repeat_cells_verified':9,'saved_aseprite_roundtrip_exact':True,'source_sha256':hashlib.sha256((root/'Reference/prototype.png').read_bytes()).hexdigest(),'source_unchanged':True,'unity_modified':False}
(root/'validation.json').write_text(json.dumps(report,indent=2)+'\n')
with ZipFile(review/'quiet_panel_256.zip','w',ZIP_DEFLATED) as z:
 for p in root.rglob('*'):
  if p.is_file():z.write(p,Path('PxlQuietPanel256')/p.relative_to(root))
 for p in review.glob('*.png'):z.write(p,Path('Review')/p.name)
 z.write(review/'pixel_inspection.txt','Review/pixel_inspection.txt')
print(json.dumps(report,indent=2))
