"""Deterministic palette cleanup, status swaps, frame assembly and review exports."""
from PIL import Image, ImageDraw
from pathlib import Path
import json
ROOT = Path(__file__).resolve().parents[4]
SOURCE = Path(__file__).resolve().parent
DEST = ROOT/'Assets/_Project/Features/Npcs/Sec0/Sprites'
REVIEW = ROOT/'work/Reviews/Sec0'
DEST.mkdir(parents=True, exist_ok=True); REVIEW.mkdir(parents=True, exist_ok=True)
PALETTE = ['171921','2b2736','39434b','52616c','6c7b83','889aa1','a6afb0','b5ad9c','d0c8b6','ebe6e2','fff4df','29486b','3f70b8','5b91bc','146c8e','26c9f2','c3f3ff','95511b','d1842b','f0a830','ffdb7c','6b272d','ab3438','e5534b','ff9581']
COLORS = [tuple(bytes.fromhex(s)) for s in PALETTE]
def clean(im):
 out=Image.new('RGBA',im.size)
 for y in range(im.height):
  for x in range(im.width):
   r,g,b,a=im.getpixel((x,y))
   if a<128 or (r>100 and b>100 and g<100 and r+b>g*3):continue
   rgb=min(COLORS,key=lambda p:sum((p[i]-v)**2 for i,v in enumerate((r,g,b))))
   out.putpixel((x,y),(*rgb,255))
 return out
def sheet(frames,cols):
 out=Image.new('RGBA',(cols*32,((len(frames)+cols-1)//cols)*32))
 for i,im in enumerate(frames):out.paste(im,((i%cols)*32,(i//cols)*32))
 return out
def status(im,state,dim=False):
 out=im.copy()
 for y in range(32):
  for x in range(32):
   r,g,b,a=im.getpixel((x,y))
   if not a:continue
   cyan=g>=145 and b>=180 and r<130
   rear=r>=140 and g<110 and b<110
   if cyan or rear:
    rgb={'routine':(38,201,242),'alert':(240,168,48),'hostile':(229,83,75)}[state]
    if dim:rgb=tuple(int(v*.45) for v in rgb)
    out.putpixel((x,y),(*rgb,255))
 return out
frames=[clean(Image.open(SOURCE/'Locomotion'/f'walk-{i+1}.png').convert('RGBA')) for i in range(16)]
# Align the wheel contact to y=29 without scaling; common bottom pivot is 3/32.
for i,im in enumerate(frames):
 b=im.getbbox(); aligned=Image.new('RGBA',(32,32));aligned.paste(im,(0,29-b[3]));frames[i]=aligned
DIRECTIONS=['south','west','east','north']
manifest={'frame_size':[32,32],'ppu':32,'pivot':[.5,3/32],'directions':DIRECTIONS,'clips':[],'generator':'built-in image_gen','sources':'Tools/ArtSources/SOLITUDE/Sec0/prompts.json'}
for state in ['routine','alert','hostile']:
 family=[status(im,state) for im in frames]
 sheet(family,4).save(REVIEW/f'sec0_{state}_sheet.png')
 for d,direction in enumerate(DIRECTIONS):
  walk=family[d*4:d*4+4]
  names=[]
  for i,im in enumerate(walk):
   name=f'sec0_{state}_{direction}_{i}.png';im.save(DEST/name);names.append(name)
  manifest['clips'].append({'name':f'Move_{state}_{direction}','frames':names,'fps':8,'loop':True})
  manifest['clips'].append({'name':f'Idle_{state}_{direction}','frames':[names[0]],'fps':1,'loop':True})
  walk[0].resize((256,256),Image.Resampling.NEAREST).save(REVIEW/f'{state}_{direction}.png')
  walk[0].save(REVIEW/f'{state}_{direction}_native.png')
  walk[0].resize((256,256),Image.Resampling.NEAREST).save(REVIEW/f'move_{state}_{direction}.gif',save_all=True,append_images=[p.resize((256,256),Image.Resampling.NEAREST) for p in walk[1:]],duration=125,loop=0,disposal=2)
for d,direction in enumerate(DIRECTIONS):
 names=[]
 for i,dim in enumerate([False,True,False,True]):
  im=status(frames[d*4],'alert',dim);name=f'sec0_blink_{direction}_{i}.png';im.save(DEST/name);names.append(name)
 manifest['clips'].append({'name':f'AlertBlink_{direction}','frames':names,'fps':6,'loop':True})
for action,folder,duration in [('backup','Backup',150),('stun','Stun',100)]:
 fx=[clean(Image.open(SOURCE/folder/f'{action}-{i+1}.png').convert('RGBA')) for i in range(4)]
 # Preserve emission/signal origin across frames; align the first leftmost component.
 import importlib.util
 spec=importlib.util.spec_from_file_location('processor',ROOT/'.agents/skills/generate2dsprite/scripts/generate2dsprite.py');m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
 for i,im in enumerate(fx):
  components=m.connected_components(im,min_area=2)
  if action=='backup':
   bar=min(components,key=lambda p:p['bbox'][0]);b=bar['bbox'];anchor=((b[0]+b[2])//2,b[1])
   offset=(7-anchor[0],10-anchor[1])
  else:
   # Pulse emitter is the pale core near the left third, rather than bbox center.
   white=[(x,y) for y in range(32) for x in range(32) if im.getpixel((x,y))[3] and min(im.getpixel((x,y))[:3])>200]
   left=min(x for x,y in white);near=[(x,y) for x,y in white if x<=left+2]
   anchor=(round(sum(x for x,y in near)/len(near)),round(sum(y for x,y in near)/len(near)))
   offset=(9-anchor[0],16-anchor[1])
  shifted=Image.new('RGBA',(32,32));shifted.paste(im,offset);assert sum(p[3]>0 for p in shifted.getdata())==sum(p[3]>0 for p in im.getdata()),'Clipped FX';fx[i]=shifted
 names=[]
 for i,im in enumerate(fx):
  name=f'sec0_{action}_fx_{i}.png';im.save(DEST/name);names.append(name)
 sheet(fx,2).save(REVIEW/f'sec0_{action}_fx_sheet.png')
 manifest['clips'].append({'name':f'Fx_{action}','frames':names,'fps':1000/duration,'loop':action=='backup','pivot':[7/32,22/32] if action=='backup' else [9/32,.5]})
 fx[0].resize((256,256),Image.Resampling.NEAREST).save(REVIEW/f'{action}.gif',save_all=True,append_images=[p.resize((256,256),Image.Resampling.NEAREST) for p in fx[1:]],duration=duration,loop=0,disposal=2)
(SOURCE/'asset_manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
# Review board, native grid plus integer zoom, beside the approved caretaker.
board=Image.new('RGB',(640,400),'#202833');draw=ImageDraw.Draw(board)
draw.text((16,12),'SEC-0 | 32x32 frames | 32 PPU | 4 directions',fill='#ebe6e2')
for s,state in enumerate(['routine','alert','hostile']):
 draw.text((16+s*208,42),state.upper(),fill='#ebe6e2')
 for d,direction in enumerate(DIRECTIONS):
  zoom=im.resize((96,96),Image.Resampling.NEAREST);x=12+s*208+(d%2)*100;y=66+(d//2)*120;board.paste(zoom,(x,y),zoom);draw.text((x,y+97),direction,fill='#a6afb0')
player=Image.open(ROOT/'Assets/_Project/Art/SOLITUDE/Characters/solitude_caretaker_south_0_v01.png').convert('RGBA')
board.paste(player,(20,335),player)
for d in range(4):board.paste(status(frames[d*4],'routine'),(72+d*40,350),frames[d*4])
draw.text((245,356),'Native scale beside caretaker',fill='#a6afb0')
board.save(REVIEW/'sec0_review.png')
# Composited state/animation preview uses the same native assets and no rescaling.
preview=[]
for i in range(12):
 canvas=Image.new('RGB',(288,96),'#202833')
 for d in range(4):
  bot=status(frames[d*4+i%4],['routine','alert','hostile'][i//4]);canvas.paste(bot,(8+d*42,50),bot)
 canvas.paste(player,(195,31),player)
 effect=Image.open(DEST/f'sec0_backup_fx_{i%4}.png');canvas.paste(effect,(230,6),effect)
 effect=Image.open(DEST/f'sec0_stun_fx_{i%4}.png');canvas.paste(effect,(228,52),effect)
 preview.append(canvas.resize((864,288),Image.Resampling.NEAREST))
preview[0].save(REVIEW/'sec0_preview.gif',save_all=True,append_images=preview[1:],duration=150,loop=0)
print(json.dumps({'sprites':len(list(DEST.glob('*.png'))),'clips':len(manifest['clips']),'review':str(REVIEW/'sec0_preview.gif')}))
