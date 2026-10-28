from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
from zipfile import ZipFile,ZIP_DEFLATED
import subprocess,json,os,hashlib,re
root=Path(__file__).resolve().parent;review=root.parents[3]/'work/Reviews/PxlWall2Cleanup';review.mkdir(parents=True,exist_ok=True)
pxl=Path.home()/'.codex/skills/pxl-pixel-art/bin/pxl.js'
ase=os.environ.get('ASEPRITE_BIN',str(Path.home()/'Applications/aseprite/build/bin/aseprite'))
subprocess.run(['node',str(pxl),'build',str(root/'wall_2.px.js'),'--scale','3'],check=True)
for p in (root/'out/sprites/wall_2_clean').glob('*.png'):
 target=root/'Editable/LayerInputs'/p.name;target.parent.mkdir(parents=True,exist_ok=True);Image.open(p).convert('RGBA').save(target)
subprocess.run([ase,'-b','--script-param','root='+str(root),'--script',str(root/'export_aseprite.lua')],check=True)
def load(p):return Image.open(p).convert('RGBA')
im=load(root/'out/sprites/wall_2_clean.png');ref=load(root/'Reference/wall_2_original.png')
assert im.size==ref.size==(256,256)
assert im.getchannel('A').getextrema()==(255,255)
allowed={tuple(bytes.fromhex(h))+(255,) for h in re.findall(r'#([0-9a-fA-F]{6})',(root/'wall_2.px.js').read_text())}
assert set(im.get_flattened_data())<=allowed
composite=Image.new('RGBA',im.size)
for name in ['panel_material','structural_depth','upper_recess','blue_band','hardware']:
 layer=load(root/f'out/sprites/wall_2_clean/{name}.png');assert layer.size==im.size;assert set(layer.getchannel('A').get_flattened_data())<={0,255};composite.alpha_composite(layer)
assert composite.tobytes()==im.tobytes()
assert load(root/'Editable/roundtrip.png').tobytes()==im.tobytes()
seams=[y for y in range(256) if im.getpixel((0,y))!=im.getpixel((255,y))]
assert not seams,seams
repeat=load(root/'out/scenes/horizontal_repeat.png');assert repeat.size==(768,256)
for i in range(3):assert repeat.crop((i*256,0,(i+1)*256,256)).tobytes()==im.tobytes()
cp=subprocess.run(['node',str(pxl),'ascii',str(root/'wall_2.px.js'),'wall_2_clean'],capture_output=True,text=True,check=True);(review/'pixel_inspection.txt').write_text(cp.stdout)
report={'passed':True,'dimensions':[256,256],'source_colors':len(set(ref.get_flattened_data())),'output_colors':len(set(im.get_flattened_data())),'alpha':'Fully opaque; one isolated source alpha-zero pixel filled','palette_only':True,'horizontal_edge_mismatches':len(seams),'layer_recomposition_exact':True,'reopened_aseprite_roundtrip_exact':True,'source_sha256':hashlib.sha256((root/'Reference/wall_2_original.png').read_bytes()).hexdigest(),'vertical_tiling':'Not intended: recessed header and lower return are terminal geometry','unity_imported':False}
(root/'validation.json').write_text(json.dumps(report,indent=2)+'\n')
c=Image.new('RGB',(1160,960),'#151c24');d=ImageDraw.Draw(c);font='/System/Library/Fonts/Supplemental/Arial.ttf'
def text(x,y,s,size=18,color='#c8d0d7'):d.text((x,y),s,font=ImageFont.truetype(font,size),fill=color)
text(32,20,'SOLITUDE / WALL 2 / PXL CLEANUP',29,'#eff0ea')
text(32,63,'256 x 256 native  /  consistent geometry  /  retained recesses, bevels and material depth',18)
text(32,106,'ORIGINAL',19,'#e3cfaa');text(608,106,'PXL CLEANUP',19,'#e3cfaa')
c.paste(ref.resize((512,512),Image.Resampling.NEAREST),(32,140),ref.resize((512,512),Image.Resampling.NEAREST))
c.paste(im.resize((512,512),Image.Resampling.NEAREST),(608,140))
text(32,674,'2x nearest-neighbor comparison',16)
text(32,716,'HORIZONTAL REPEAT / THREE COPIES / NATIVE 1:1',19,'#eff0ea')
c.paste(repeat,(196,752));
# Extend the review canvas to fit the full native repeat without clipping.
full=Image.new('RGB',(1160,1040),'#151c24');full.paste(c,(0,0));full.paste(repeat,(196,752));full.save(review/'comparison_sheet.png')
with ZipFile(review/'wall_2_pxl_cleanup.zip','w',ZIP_DEFLATED) as z:
 for p in root.rglob('*'):
  if p.is_file():z.write(p,Path('PxlWall2Cleanup')/p.relative_to(root))
 z.write(review/'comparison_sheet.png','Review/comparison_sheet.png');z.write(review/'pixel_inspection.txt','Review/pixel_inspection.txt')
print(json.dumps(report,indent=2))
