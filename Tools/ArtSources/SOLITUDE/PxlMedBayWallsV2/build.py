"""Native PXL generation, lossless editable handoff, pixel QC and review sheets."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
from zipfile import ZipFile,ZIP_DEFLATED
import subprocess,os,json,hashlib,re
root=Path(__file__).resolve().parent
old=root.parent/'PxlMedBayWalls'
review=root.parents[3]/'work/Reviews/PxlMedBayWallsV2';review.mkdir(parents=True,exist_ok=True)
pxl=Path.home()/'.codex/skills/pxl-pixel-art/bin/pxl.js'
ase=os.environ.get('ASEPRITE_BIN',str(Path.home()/'Applications/aseprite/build/bin/aseprite'))
subprocess.run(['node',str(pxl),'build',str(root/'med_bay_walls.px.js'),'--scale','3'],check=True)
for p in (root/'out/sprites').rglob('*.png'):
 target=root/'Editable/LayerInputs'/p.relative_to(root/'out/sprites');target.parent.mkdir(parents=True,exist_ok=True)
 Image.open(p).convert('RGBA').save(target)
subprocess.run([ase,'-b','--script-param','root='+str(root),'--script',str(root/'export_aseprite.lua')],check=True)
def load(p):return Image.open(p).convert('RGBA')
states=['access_a','access_b','utility','supply'];images={};results={}
original=load(old/'out/sprites/wall_med_bay_plain.png')
original_base=load(old/'out/sprites/wall_med_bay_plain/wall_face.png')
allowed_colors={tuple(bytes.fromhex(h))+(255,) for h in re.findall(r"#([0-9a-fA-F]{6})",(root/'med_bay_walls.px.js').read_text())}
for name in states:
 im=load(root/f'out/sprites/wall_med_bay_{name}.png');images[name]=im
 assert im.size==(128,256) and im.getchannel('A').getextrema()==(255,255)
 assert set(im.get_flattened_data())<=allowed_colors
 layers=[load(root/f'out/sprites/wall_med_bay_{name}/{part}.png') for part in ['wall_face','architectural_depth','fixtures']]
 assert layers[0].tobytes()==original_base.tobytes()
 composite=layers[0].copy()
 for layer in layers[1:]:
  assert layer.size==(128,256)
  assert set(layer.getchannel('A').get_flattened_data())<={0,255}
  composite.alpha_composite(layer)
 assert composite.tobytes()==im.tobytes()
 assert load(root/f'Editable/roundtrip_{name}.png').tobytes()==im.tobytes()
 # Entire 12px connection bands remain byte-identical to V1, not just exterior columns.
 for box in [(0,0,12,256),(116,0,128,256)]:assert im.crop(box).tobytes()==original.crop(box).tobytes()
 # The original modular division and stripe joining rows remain locked.
 for box in [(0,153,128,156)]:assert im.crop(box).tobytes()==original.crop(box).tobytes()
 for nx,ny,label in [(3,1,'repeat'),(3,3,'grid')]:
  repeat=load(root/f'out/scenes/{label}_{name}.png');assert repeat.size==(128*nx,256*ny)
  for xx in range(nx):
   for yy in range(ny):assert repeat.crop((128*xx,256*yy,128*(xx+1),256*(yy+1))).tobytes()==im.tobytes()
 cp=subprocess.run(['node',str(pxl),'ascii',str(root/'med_bay_walls.px.js'),'wall_med_bay_'+name],capture_output=True,text=True,check=True)
 (review/f'inspect_{name}.txt').write_text(cp.stdout)
 results[name]={'dimensions':[128,256],'fully_opaque':True,'binary_overlay_alpha':True,'authored_palette_only':True,'original_wall_base_unchanged':True,'original_12px_join_bands_unchanged':True,'original_panel_joint_unchanged':True,'overlay_recomposition_exact':True,'saved_aseprite_roundtrip_exact':True,'palette_colors':len(set(im.get_flattened_data()))}
for a in states:
 for b in states:assert all(images[a].getpixel((127,y))==images[b].getpixel((0,y)) for y in range(256))
player=load(root/'out/sprites/player_scale_reference.png');assert player.size==(32,48)
assert set(player.getchannel('A').get_flattened_data())=={0,255}
sequence=['access_a','access_b','utility','access_a','supply','access_b']
run=load(root/'out/scenes/six_panel_wall_run.png');scale=load(root/'out/scenes/six_panel_scale_review.png')
assert run.size==(768,256) and scale.size==(808,256)
for i,name in enumerate(sequence):assert run.crop((i*128,0,(i+1)*128,256)).tobytes()==images[name].tobytes()
assert scale.crop((776,208,808,256)).tobytes()==player.tobytes()
assert scale.crop((0,0,768,256)).tobytes()==run.tobytes()
report={'passed':True,'variants':results,'horizontal_pairings_checked':16,'horizontal_edge_mismatches':0,'scale_reference':{'dimensions':[32,48],'kind':'PXL human scale stand-in, not the existing 64x96 Caretaker artwork','scale':1},'six_panel_sequence':sequence,'ordinary_panel_count':4,'specialty_panel_count':2,'vertical_tiling':'Not an intended connection; full wall height and separate caps.','unity_imported':False}
(root/'validation.json').write_text(json.dumps(report,indent=2)+'\n')
font='/System/Library/Fonts/Supplemental/Arial.ttf'
def sheet(w,h):
 c=Image.new('RGB',(w,h),'#141b23');return c,ImageDraw.Draw(c)
def text(d,x,y,s,size=18,color='#c9d0da'):d.text((x,y),s,font=ImageFont.truetype(font,size),fill=color)
def paste(c,im,x,y,mul=1):
 if mul!=1:im=im.resize((im.width*mul,im.height*mul),Image.Resampling.NEAREST)
 # Composite transparent player on review background without changing source pixels.
 c.paste(im,(x,y),im)
# Main review: 2x art for inspection, but native composition and player comparison below.
c,d=sheet(1280,1040)
text(d,32,22,'SOLITUDE / MED BAY / ITERATION 02',30,'#eff2ee')
text(d,32,65,'128 x 256 wall faces  |  original join bands + construction retained  |  separate caps',18)
for i,(name,title) in enumerate(zip(states,['ACCESS A','ACCESS B','DIGITAL BEDSIDE STATION','SUPPLY CABINET'])):
 x=32+i*312;text(d,x,111,title,18,'#e3d3ae');paste(c,images[name],x,147,2)
text(d,32,684,'2x nearest-neighbor inspection. Gas ports differ by shape; restrained status colors, no bloom.',17)
text(d,32,738,'NATIVE 1:1 / FOUR ORDINARY + TWO SPECIALTY PANELS / 32 x 48 HUMAN REFERENCE',19,'#eff2ee')
paste(c,scale,236,778)
# Baseline marker occupies review margin only; never source art.
d.line((236,1035,1044,1035),fill='#667879');c.save(review/'contact_sheet.png')
# Exact native-size before/after pairs, including a 32x48 reference beside each V2.
c,d=sheet(1328,800)
text(d,24,20,'MED BAY / V1 TO V2 / NATIVE 1:1 COMPARISON',28,'#eff2ee')
text(d,24,62,'Player-size figures are 32 x 48 scale stand-ins. All artwork below is pasted at native size.',17)
for i,(name,title) in enumerate(zip(states,['ORDINARY ACCESS A','ORDINARY ACCESS B','BEDSIDE UTILITIES','SUPPLY MODULE'])):
 x=24+i*324;v1='plain' if name.startswith('access') else name
 text(d,x,108,title,18,'#e3d3ae');text(d,x,139,'V1',15);text(d,x+136,139,'V2',15)
 paste(c,load(old/f'out/sprites/wall_med_bay_{v1}.png'),x,166)
 paste(c,images[name],x+136,166);paste(c,player,x+272,374)
text(d,24,465,'SIX-PANEL REVIEW / 1:1 / ACCESS A - ACCESS B - UTILITY - ACCESS A - SUPPLY - ACCESS B',18,'#eff2ee')
paste(c,scale,260,510);c.save(review/'comparison_sheet.png')
# Store a native transparent scale plate independently of the labeled sheets.
scale.save(review/'native_scale_review.png')
with ZipFile(review/'med_bay_walls_v2.zip','w',ZIP_DEFLATED) as z:
 for p in root.rglob('*'):
  if p.is_file():z.write(p,Path('PxlMedBayWallsV2')/p.relative_to(root))
 for p in review.glob('*.png'):z.write(p,Path('Review')/p.name)
print(json.dumps(report,indent=2))
