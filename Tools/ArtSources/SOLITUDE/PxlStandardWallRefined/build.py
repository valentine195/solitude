"""Refine existing PXL wall, export editable layers, verify and assemble native/4x review."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
from zipfile import ZipFile,ZIP_DEFLATED
import subprocess,json,os,hashlib,re
root=Path(__file__).resolve().parent
prior=root.parent/'PxlWall2Depth'
review=root.parents[3]/'work/Reviews/PxlStandardWallRefined';review.mkdir(parents=True,exist_ok=True)
pxl=Path.home()/'.codex/skills/pxl-pixel-art/bin/pxl.js'
ase=os.environ.get('ASEPRITE_BIN',str(Path.home()/'Applications/aseprite/build/bin/aseprite'))
subprocess.run(['node',str(pxl),'build',str(root/'wall_2.px.js'),'--scale','4'],check=True)
for p in (root/'out/sprites/standard_wall_refined').glob('*.png'):
 target=root/'Editable/LayerInputs'/p.name;target.parent.mkdir(parents=True,exist_ok=True);Image.open(p).convert('RGBA').save(target)
subprocess.run([ase,'-b','--script-param','root='+str(root),'--script',str(root/'export_aseprite.lua')],check=True)
def load(p):return Image.open(p).convert('RGBA')
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
im=load(root/'out/sprites/standard_wall_refined.png');old=load(root/'Reference/previous_pxl.png');ref=load(root/'Reference/wall_2_original.png')
assert im.size==old.size==ref.size==(256,256)
assert im.getchannel('A').getextrema()==(255,255)
source=(root/'wall_2.px.js').read_text();allowed={tuple(bytes.fromhex(h))+(255,) for h in re.findall(r'#([0-9a-fA-F]{6})',source)}
assert set(im.get_flattened_data())<=allowed
prior_allowed={tuple(bytes.fromhex(h))+(255,) for h in re.findall(r'#([0-9a-fA-F]{6})',(prior/'wall_2.px.js').read_text())}
assert set(im.get_flattened_data())-prior_allowed=={(238,241,239,255)}
composite=Image.new('RGBA',im.size)
for name in ['panel_material','structural_depth','upper_recess','blue_band','hardware']:
 layer=load(root/f'out/sprites/standard_wall_refined/{name}.png');assert layer.size==im.size;assert set(layer.getchannel('A').get_flattened_data())<={0,255};composite.alpha_composite(layer)
assert composite.tobytes()==im.tobytes()
assert load(root/'Editable/roundtrip.png').tobytes()==im.tobytes()
# Preserve the complete existing upper-recess layer, not merely its silhouette.
assert load(root/'out/sprites/standard_wall_refined/upper_recess.png').tobytes()==load(prior/'out/sprites/wall_2_clean/upper_recess.png').tobytes()
# Large face regions must be exactly flat off-white, with no diagonal gray bands.
for box in [(40,50,216,145),(40,180,216,230)]:assert set(im.crop(box).get_flattened_data())=={(238,241,239,255)}
seams=[y for y in range(256) if im.getpixel((0,y))!=im.getpixel((255,y))];assert not seams
for y in range(158,176):assert len({im.getpixel((x,y)) for x in range(256)})==1
repeat=load(root/'out/scenes/horizontal_repeat.png');assert repeat.size==(768,256)
for i in range(3):assert repeat.crop((i*256,0,(i+1)*256,256)).tobytes()==im.tobytes()
# Exact 4x nearest-neighbor artifacts, independently readable outside PXL grid previews.
im.resize((1024,1024),Image.Resampling.NEAREST).save(review/'standard_wall_refined_4x.png')
repeat.resize((3072,1024),Image.Resampling.NEAREST).save(review/'three_panels_4x.png')
repeat.save(review/'three_panels_native.png')
cp=subprocess.run(['node',str(pxl),'ascii',str(root/'wall_2.px.js'),'standard_wall_refined'],capture_output=True,text=True,check=True);(review/'pixel_inspection.txt').write_text(cp.stdout)
report={'passed':True,'dimensions':[256,256],'fully_opaque':True,'hard_alpha_layers':True,'only_palette_addition':'#EEF1EF (explicit user target)','main_face_flat_off_white':True,'upper_recess_layer_byte_identical_to_prior':True,'horizontal_edge_mismatches':0,'stripe_continuous_across_entire_width':True,'layer_recomposition_exact':True,'reopened_aseprite_roundtrip_exact':True,'prior_pxl_png_sha256':sha(root/'Reference/previous_pxl.png'),'original_asset_sha256':sha(root/'Reference/wall_2_original.png'),'review_enlargement':'4x nearest-neighbor','unity_imported':False}
(root/'validation.json').write_text(json.dumps(report,indent=2)+'\n')
# Review: all comparisons use native pixels; no renderer-dependent scale claims.
c=Image.new('RGB',(1040,1352),'#151c24');d=ImageDraw.Draw(c);font='/System/Library/Fonts/Supplemental/Arial.ttf'
def text(x,y,s,size=18,color='#c8d0d7'):d.text((x,y),s,font=ImageFont.truetype(font,size),fill=color)
text(32,24,'SOLITUDE / STANDARD WALL REFINEMENT',29,'#eff0ea')
text(32,67,'Existing construction refined in place / all artwork below at native 1:1',18)
for i,(title,img) in enumerate([('ORIGINAL ARTWORK',ref),('PREVIOUS PXL',old),('REFINED STANDARD',im)]):
 x=32+i*344;text(x,111,title,18,'#e3cfaa');c.paste(img,(x,148),img)
for title,img,y in [('ORIGINAL / THREE ADJACENT COPIES',ref,462),('PREVIOUS PXL / THREE ADJACENT COPIES',old,766),('REFINED / THREE ADJACENT COPIES',im,1070)]:
 text(32,y-34,title,18,'#e3cfaa')
 for i in range(3):c.paste(img,(136+256*i,y),img)
c.save(review/'comparison_sheet.png')
with ZipFile(review/'standard_wall_refined.zip','w',ZIP_DEFLATED) as z:
 for p in root.rglob('*'):
  if p.is_file():z.write(p,Path('PxlStandardWallRefined')/p.relative_to(root))
 for p in review.glob('*.png'):z.write(p,Path('Review')/p.name)
 z.write(review/'pixel_inspection.txt','Review/pixel_inspection.txt')
print(json.dumps(report,indent=2))
