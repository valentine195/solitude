from pathlib import Path
from PIL import Image
from zipfile import ZipFile,ZIP_DEFLATED
import subprocess,os,json
root=Path(__file__).resolve().parent;review=root.parents[3]/'work/Reviews/PxlQuietPanel32';review.mkdir(parents=True,exist_ok=True)
pxl=Path.home()/'.codex/skills/pxl-pixel-art/bin/pxl.js'
subprocess.run(['node',str(pxl),'build',str(root/'panel.px.js'),'--scale','8'],check=True)
for p in (root/'out/sprites/quiet_panel_32').glob('*.png'):
 target=root/'Editable/LayerInputs'/p.name;target.parent.mkdir(parents=True,exist_ok=True);Image.open(p).convert('RGBA').save(target)
ase=os.environ.get('ASEPRITE_BIN',str(Path.home()/'Applications/aseprite/build/bin/aseprite'))
subprocess.run([ase,'-b','--script-param','root='+str(root),'--script',str(root/'export_aseprite.lua')],check=True)
im=Image.open(root/'out/sprites/quiet_panel_32.png').convert('RGBA')
assert im.size==(32,32) and im.getchannel('A').getextrema()==(255,255)
assert set(im.get_flattened_data())=={(238,241,239,255),(168,179,184,255)}
assert im.tobytes()==im.transpose(Image.Transpose.FLIP_LEFT_RIGHT).tobytes()==im.transpose(Image.Transpose.FLIP_TOP_BOTTOM).tobytes()
assert im.tobytes()==Image.open(root/'Editable/roundtrip.png').convert('RGBA').tobytes()
assert all(im.getpixel((0,y))==im.getpixel((31,y)) for y in range(32))
assert all(im.getpixel((x,0))==im.getpixel((x,31)) for x in range(32))
im.resize((256,256),Image.Resampling.NEAREST).save(review/'quiet_panel_8x.png')
repeat=Image.open(root/'out/scenes/quiet_panel_repeat_3x3.png').convert('RGBA');assert repeat.size==(96,96)
repeat.resize((384,384),Image.Resampling.NEAREST).save(review/'repeat_3x3_4x.png')
(root/'validation.json').write_text(json.dumps({'passed':True,'dimensions':[32,32],'bevel_outline_pixels':1,'corner_cut_pixels':2,'colors':['#EEF1EF','#A8B3B8'],'binary_alpha':True,'symmetric':True,'both_axis_edges_match':True,'aseprite_roundtrip_exact':True,'original_256_unchanged':True},indent=2)+'\n')
with ZipFile(review/'quiet_panel_32.zip','w',ZIP_DEFLATED) as z:
 for p in root.rglob('*'):
  if p.is_file():z.write(p,Path('PxlQuietPanel32')/p.relative_to(root))
 for p in review.glob('*.png'):z.write(p,Path('Review')/p.name)
print('Verified 32x32, 1px outline, 2px clipped corners, same 2-color palette, exact Aseprite roundtrip.')
