from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
from zipfile import ZipFile,ZIP_DEFLATED
import subprocess,os,json
root=Path(__file__).resolve().parent;review=root.parents[3]/'work/Reviews/PxlWallConnectorIntegrated';review.mkdir(parents=True,exist_ok=True)
pxl=Path.home()/'.codex/skills/pxl-pixel-art/bin/pxl.js'
ase=os.environ.get('ASEPRITE_BIN',str(Path.home()/'Applications/aseprite/build/bin/aseprite'))
subprocess.run(['node',str(pxl),'build',str(root/'connector.px.js'),'--only','wall_connector_128x256,connector_overlay,panel_connector_panel','--scale','4'],check=True)
for p in (root/'out/sprites/wall_connector_128x256').glob('*.png'):
 target=root/'Editable/LayerInputs'/p.name;target.parent.mkdir(parents=True,exist_ok=True);Image.open(p).convert('RGBA').save(target)
subprocess.run([ase,'-b','--script-param','root='+str(root),'--script',str(root/'export_aseprite.lua')],check=True)
def load(p):return Image.open(p).convert('RGBA')
im=load(root/'out/sprites/wall_connector_128x256.png');old=load(root/'Reference/previous_connector.png')
wall=load(root.parent/'PxlStandardWallGrid256/out/sprites/standard_wall_256.png');assembly=load(root/'out/scenes/panel_connector_panel.png')
assert im.size==(128,256) and im.getchannel('A').getextrema()==(255,255)
assert assembly.size==(640,256)
composite=Image.new('RGBA',im.size)
for name in ['wall_infill','mounting_recess','connector_spine','end_blocks','amber_emitter']:
 layer=load(root/f'out/sprites/wall_connector_128x256/{name}.png');assert set(layer.getchannel('A').get_flattened_data())<={0,255};composite.alpha_composite(layer)
 if name in ['connector_spine','end_blocks','amber_emitter']:
  assert layer.tobytes()==load(root.parent/f'PxlWallConnector/out/sprites/wall_connector_128x256/{name}.png').tobytes()
assert composite.tobytes()==im.tobytes()==load(root/'Editable/roundtrip.png').tobytes()
assert load(root/'out/sprites/wall_connector_128x256/wall_infill.png').crop((44,0,84,256)).getchannel('A').getextrema()==(0,0)
assert all(im.getpixel((0,y))==wall.getpixel((255,y)) and im.getpixel((127,y))==wall.getpixel((0,y)) for y in range(256))
assert assembly.crop((0,0,256,256)).tobytes()==wall.tobytes()
assert assembly.crop((256,0,384,256)).tobytes()==im.tobytes()
assert assembly.crop((384,0,640,256)).tobytes()==wall.tobytes()
im.resize((512,1024),Image.Resampling.NEAREST).save(review/'connector_4x.png')
assembly.save(review/'panel_connector_panel_native.png');assembly.resize((1280,512),Image.Resampling.NEAREST).save(review/'assembly_2x.png')
cp=subprocess.run(['node',str(pxl),'ascii',str(root/'connector.px.js'),'wall_connector_128x256'],capture_output=True,text=True,check=True);(review/'pixel_inspection.txt').write_text(cp.stdout)
report={'passed':True,'dimensions':[128,256],'fully_opaque':True,'hard_layer_alpha':True,'existing_connector_spine_end_blocks_and_emitter_exact':True,'cladding_cutout':[44,0,40,256],'wall_material_behind_connector':False,'both_panel_join_mismatches':0,'saved_aseprite_roundtrip_exact':True,'assembly_dimensions':[640,256],'unity_modified':False}
(root/'validation.json').write_text(json.dumps(report,indent=2)+'\n')
c=Image.new('RGB',(832,990),'#151c24');d=ImageDraw.Draw(c);font='/System/Library/Fonts/Supplemental/Arial.ttf'
def text(x,y,s,n=18):d.text((x,y),s,font=ImageFont.truetype(font,n),fill='#dfe5e5')
text(32,20,'SOLITUDE / CONNECTOR SEATED INTO WALL',27)
text(32,63,'128 x 256 native / unchanged connector core / recessed mounting and fitted cladding',16)
text(96,104,'PREVIOUS',18);text(480,104,'INTEGRATED',18)
c.paste(old.resize((256,512),Image.Resampling.NEAREST),(96,140))
c.paste(im.resize((256,512),Image.Resampling.NEAREST),(480,140))
text(32,680,'PANEL - CONNECTOR - PANEL / NATIVE 1:1',18);c.paste(assembly,(96,718));c.save(review/'comparison_sheet.png')
with ZipFile(review/'wall_connector_integrated.zip','w',ZIP_DEFLATED) as z:
 for p in root.rglob('*'):
  if p.is_file():z.write(p,Path('PxlWallConnectorIntegrated')/p.relative_to(root))
 # Retain the existing shared PXL dependency at its sibling path for source portability.
 shared=root.parent/'PxlStandardWallGrid256/wall.px.js';z.write(shared,'PxlStandardWallGrid256/wall.px.js')
 for p in review.glob('*.png'):z.write(p,Path('Review')/p.name)
 z.write(review/'pixel_inspection.txt','Review/pixel_inspection.txt')
print(json.dumps(report,indent=2))
