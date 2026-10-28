"""Deterministic pixel QC, contact sheet, and Aseprite round-trip comparison."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
from collections import Counter
import hashlib,json,subprocess,time,statistics
ROOT=Path(__file__).resolve().parent
PROJECT=ROOT.parents[3]
REVIEW=PROJECT/'work/Reviews/PxlWallPanel'
REVIEW.mkdir(parents=True,exist_ok=True)
def load(p): return Image.open(p).convert('RGBA')
def data(im): return list(im.get_flattened_data())
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
ref=load(ROOT/'Reference/wall_panel_1.png')
base=load(ROOT/'out/sprites/wall_pristine.png')
colors=set(data(ref)); results={}
for state in ['pristine','lightly_worn','heavily_damaged']:
 im=load(ROOT/f'out/sprites/wall_{state}.png')
 assert im.size==(128,256)
 assert set(im.getchannel('A').get_flattened_data())=={0,255}
 assert set(data(im))<=colors
 assert im.getchannel('A').tobytes()==ref.getchannel('A').tobytes()
 assert all(im.getpixel((0,y))==im.getpixel((127,y)) for y in range(256))
 # Lock 12 px each side and all cap/plinth pixels to the shared base.
 locked=[(x,y) for y in range(256) for x in range(128) if x<12 or x>=116 or y<45 or y>=233]
 assert all(im.getpixel(p)==base.getpixel(p) for p in locked)
 roundtrip=load(ROOT/f'Editable/roundtrip_{state}.png')
 assert roundtrip.tobytes()==im.tobytes()
 rebuilt=base.copy()
 if state!='pristine':rebuilt.alpha_composite(load(ROOT/'out/sprites/overlay_lightly_worn.png'))
 if state=='heavily_damaged':rebuilt.alpha_composite(load(ROOT/'out/sprites/overlay_heavy_damage.png'))
 assert rebuilt.tobytes()==im.tobytes()
 repeat=load(ROOT/f'out/scenes/repeat_{state}.png');assert repeat.size==(384,256)
 for x in range(3): assert repeat.crop((128*x,0,128*x+128,256)).tobytes()==im.tobytes()
 grid=load(ROOT/f'out/scenes/grid_{state}.png');assert grid.size==(384,768)
 for y in range(3):
  for x in range(3):assert grid.crop((128*x,256*y,128*x+128,256*y+256)).tobytes()==im.tobytes()
 changed=sum(a!=b for a,b in zip(data(base),data(im)))
 results[state]={'dimensions':[128,256],'opaque_colors':len(set(data(im)))-1,'binary_alpha':True,'reference_alpha_exact':True,'palette_subset_of_reference':True,'horizontal_edge_mismatches':0,'locked_border_mismatches':0,'overlay_recomposition_exact':True,'aseprite_roundtrip_exact':True,'changed_pixels':changed,'changed_opaque_percent':round(changed/30464*100,2),'png_sha256':sha(ROOT/f'out/sprites/wall_{state}.png')}
for name in ['overlay_lightly_worn','overlay_heavy_damage','overlay_heavily_damaged']:
 im=load(ROOT/f'out/sprites/{name}.png');assert im.size==(128,256)
 assert set(im.getchannel('A').get_flattened_data())=={0,255}
 assert all(im.getpixel((x,y))[3]==0 for y in range(256) for x in list(range(12))+list(range(116,128)))
 assert set(data(im))-{(0,0,0,0)}<=colors
# Built-in PXL ASCII inspection retained in full, plus targeted excerpts in report.
pxl=Path.home()/'.codex/skills/pxl-pixel-art/bin/pxl.js'
for state in ['pristine','heavily_damaged']:
 cp=subprocess.run(['node',str(pxl),'ascii',str(ROOT/'wall_panel.px.js'),'wall_'+state],capture_output=True,text=True,check=True)
 (REVIEW/f'inspect_{state}.txt').write_text(cp.stdout)
# Benchmark only rendering/PNG export after authored source is loaded anew each run.
times=[]
for _ in range(3):
 t=time.perf_counter();subprocess.run(['node',str(pxl),'build',str(ROOT/'wall_panel.px.js'),'--only','wall_pristine,wall_lightly_worn,wall_heavily_damaged','--no-preview','--out',str(REVIEW/'Benchmark')],capture_output=True,check=True);times.append(round(time.perf_counter()-t,3))
refp=data(ref);bp=data(base);opaque=[(a,b) for a,b in zip(refp,bp) if a[3]]
mae=sum(abs(a[i]-b[i]) for a,b in opaque for i in range(3))/(len(opaque)*3)
report={'passed':True,'reference':'Assets/_Project/Art/Tiles/Walls/Sprites/wall_panel_1.aseprite','reference_sha256':sha(PROJECT/'Assets/_Project/Art/Tiles/Walls/Sprites/wall_panel_1.aseprite'),'reference_size':list(ref.size),'reference_opaque_colors':len(colors)-1,'variant_results':results,'comparison':{'opaque_rgb_mean_absolute_error':round(mae,3),'note':'Comparison is a clean procedural reconstruction, not a pixel-exact trace. MAE is descriptive, not an aesthetic score.'},'benchmark_seconds':times,'benchmark_median_seconds':statistics.median(times),'tiling_scope':'Horizontal only. 3x1 and 3x3 generated. Top/bottom include terminal cap/plinth; vertical repeat intentionally not seamless.','unity_imported':False}
(ROOT/'validation.json').write_text(json.dumps(report,indent=2)+'\n')
# Review-only assembly: native sprites or exact 2x nearest-neighbor. No artwork filtering.
font_path='/System/Library/Fonts/Supplemental/Arial.ttf'
def font(n):return ImageFont.truetype(font_path,n)
canvas=Image.new('RGB',(1280,1050),'#141a23');draw=ImageDraw.Draw(canvas)
def label(x,y,t,n=18,color='#c7ceda'):draw.text((x,y),t,font=font(n),fill=color)
def present(im,x,y,scale=1):
 im=im.resize((im.width*scale,im.height*scale),Image.Resampling.NEAREST)
 bg=Image.new('RGBA',im.size);d=ImageDraw.Draw(bg)
 for yy in range(0,im.height,8):
  for xx in range(0,im.width,8):d.rectangle((xx,yy,xx+7,yy+7),fill='#303946' if (xx//8+yy//8)%2 else '#384353')
 bg.alpha_composite(im);canvas.paste(bg.convert('RGB'),(x,y))
label(40,25,'SOLITUDE / PXL WALL PANEL STUDY',30,'#f0f2f5')
label(40,68,'128 x 256 native   /   shared geometry + palette   /   independent damage overlays',18)
for i,(title,im) in enumerate([('EXISTING REFERENCE',ref),('PRISTINE',base),('LIGHTLY WORN',load(ROOT/'out/sprites/wall_lightly_worn.png')),('HEAVILY DAMAGED',load(ROOT/'out/sprites/wall_heavily_damaged.png'))]):
 x=40+i*306;label(x,112,title,18,'#e1bd82' if i==0 else '#f0f2f5');present(im,x,146,2)
 label(x,670,'2x nearest-neighbor / alpha shown',14)
label(40,712,'HORIZONTAL REPEAT / THREE ADJACENT COPIES EACH / NATIVE SIZE',20,'#f0f2f5')
for i,state in enumerate(['pristine','lightly_worn','heavily_damaged']):
 x=40+i*408;label(x,752,state.replace('_',' ').upper(),16);present(load(ROOT/f'out/scenes/repeat_{state}.png'),x,782)
 for xx in [128,256]:draw.line((x+xx,776,x+xx,781),fill='#e1bd82',width=1)
canvas.save(REVIEW/'contact_sheet.png')
print(json.dumps(report,indent=2))
