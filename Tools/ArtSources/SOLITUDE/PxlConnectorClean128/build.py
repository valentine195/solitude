from pathlib import Path
from PIL import Image,ImageDraw
from zipfile import ZipFile,ZIP_DEFLATED
import subprocess,os,json,re,hashlib
root=Path(__file__).resolve().parent;review=root.parents[3]/'work/Reviews/PxlConnectorClean128';review.mkdir(parents=True,exist_ok=True)
pxl=Path.home()/'.codex/skills/pxl-pixel-art/bin/pxl.js'
ase=os.environ.get('ASEPRITE_BIN',str(Path.home()/'Applications/aseprite/build/bin/aseprite'))
subprocess.run(['node',str(pxl),'build',str(root/'connector.px.js'),'--scale','4'],check=True)
for p in (root/'out/sprites/connector_clean_128x256').glob('*.png'):
 target=root/'Editable/LayerInputs'/p.name;target.parent.mkdir(parents=True,exist_ok=True);Image.open(p).convert('RGBA').save(target)
subprocess.run([ase,'-b','--script-param','root='+str(root),'--script',str(root/'export_aseprite.lua')],check=True)
def load(p):return Image.open(p).convert('RGBA')
im=load(root/'out/sprites/connector_clean_128x256.png')
assert im.size==(128,256)
assert set(im.getchannel('A').get_flattened_data())=={0,255}
assert im.getbbox()==(48,10,80,246)
allowed={tuple(bytes.fromhex(h))+(255,) for h in re.findall(r'#([0-9a-fA-F]{6})',(root/'connector.px.js').read_text())}
opaque={c for c in im.get_flattened_data() if c[3]};assert opaque<=allowed
composite=Image.new('RGBA',im.size)
for name in ['spine','end_blocks','amber_emitter']:
 layer=load(root/f'out/sprites/connector_clean_128x256/{name}.png');assert layer.size==(128,256);assert set(layer.getchannel('A').get_flattened_data())<={0,255};composite.alpha_composite(layer)
assert composite.tobytes()==im.tobytes()==load(root/'Editable/roundtrip.png').tobytes()
# Viewer-only checkerboard and integer nearest-neighbor enlargement.
preview=Image.new('RGBA',(512,1024));d=ImageDraw.Draw(preview)
for y in range(0,1024,16):
 for x in range(0,512,16):d.rectangle((x,y,x+15,y+15),fill='#303943' if (x//16+y//16)%2 else '#39444f')
preview.alpha_composite(im.resize((512,1024),Image.Resampling.NEAREST));preview.save(review/'connector_preview_4x.png')
im.resize((512,1024),Image.Resampling.NEAREST).save(review/'connector_transparent_4x.png')
cp=subprocess.run(['node',str(pxl),'ascii',str(root/'connector.px.js'),'connector_clean_128x256'],capture_output=True,text=True,check=True);(review/'pixel_inspection.txt').write_text(cp.stdout)
report={'passed':True,'dimensions':[128,256],'opaque_art_bounds':[48,10,80,246],'centered_horizontally':True,'colors':len(opaque),'hard_alpha_only':[0,255],'authored_palette_only':True,'transparent_margins':True,'no_wall_infill':True,'layer_recomposition_exact':True,'saved_aseprite_roundtrip_exact':True,'prototype_sha256':hashlib.sha256((root/'Reference/prototype_connector.png').read_bytes()).hexdigest(),'unity_modified':False}
(root/'validation.json').write_text(json.dumps(report,indent=2)+'\n')
with ZipFile(review/'connector_clean_128x256.zip','w',ZIP_DEFLATED) as z:
 for p in root.rglob('*'):
  if p.is_file():z.write(p,Path('PxlConnectorClean128')/p.relative_to(root))
 for p in review.glob('*.png'):z.write(p,Path('Review')/p.name)
 z.write(review/'pixel_inspection.txt','Review/pixel_inspection.txt')
print(json.dumps(report,indent=2))
