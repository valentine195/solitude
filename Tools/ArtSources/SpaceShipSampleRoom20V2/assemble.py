from pathlib import Path
from PIL import Image,ImageDraw
import numpy as np,json
P=Path(__file__).resolve().parent;ROOT=P.parents[2];R=ROOT/'work/Reviews/SpaceShipSampleRoom20V2';R.mkdir(parents=True,exist_ok=True)
(P/'Tiles').mkdir(exist_ok=True);(P/'Layers').mkdir(exist_ok=True)
raw=Image.open(P/'corners_raw.png').convert('RGB')
# Exclude generated atlas dividers; uniformly reduce8x, then remove redundant interior strips.
boxes=[(6,4,614,596),(638,4,1246,596),(6,616,614,1248),(638,616,1246,1248)]
corners=[]
for box in boxes:
 im=raw.crop(box);im=im.resize((im.width//8,im.height//8),Image.Resampling.NEAREST);a=np.array(im)
 for axis in (1,0):
  while a.shape[axis]>64:
   energy=np.abs(np.diff(a.astype(float),axis=axis)).mean(axis=(0,2) if axis==1 else (1,2))
   energy[:10]=1e9;energy[-10:]=1e9
   a=np.delete(a,int(np.argmin(energy))+1,axis=axis)
 im=Image.fromarray(a).quantize(colors=32,dither=Image.Dither.NONE).convert('RGBA')
 for y in range(64):
  for x in range(64):
   rr,g,b,alpha=im.getpixel((x,y))
   if rr>75 and b>75 and rr>g*1.45 and b>g*1.45:im.putpixel((x,y),(0,0,0,0))
 corners.append(im)
nw,ne,sw,se=corners
# Source-derived strip profiles tie the cap/base of every north wall to the corner geometry.
cap=nw.crop((32,0,33,8));base=nw.crop((32,60,33,64))
west=nw.crop((0,16,8,48));east=ne.crop((56,16,64,48));south=sw.crop((16,56,48,64))
# Ensure the corner exits and straight runs use the very same generated pixels.
for t,left in [(nw,False),(ne,True)]:
 for x in range(8,56):t.paste(cap,(x,0));t.paste(base,(x,60))
 for x in (range(56,64) if not left else range(0,8)):t.paste(cap,(x,0));t.paste(base,(x,60))
 t.paste(west if not left else east,(0 if not left else 56,32))
# Cap-only south corners are assembled from their generated matching surface strips.
for t,iswest in [(sw,True),(se,False)]:
 t.paste(west if iswest else east,(0 if iswest else 56,0))
 for x in range(0,64,32):t.paste(south,(x,56))
assets={};placements=[]
def save(name,im,provenance):
 im.save(P/'Tiles'/f'{name}.png');assets[name]=dict(path=f'Tiles/{name}.png',size=list(im.size),source=provenance);return name
for name,t in zip(['corner_nw','corner_ne','corner_sw','corner_se'],corners):save(name,t,'Image-edited corner kit; native pixel cleanup')
for name,t in [('cap_west',west),('cap_east',east),('cap_south',south)]:save(name,t,'Derived from generated corner pixels')
F=ROOT/'Tools/ArtSources/SOLITUDE/RefinedSteelFloor32/Round3/Tiles32'
for n in ['base_clean','base_rubbed','joint_west','joint_north','joint_corner']:
 save(n,Image.open(F/f'{n}.png').convert('RGBA'),str((F/f'{n}.png').relative_to(ROOT)))
layers={n:Image.new('RGBA',(640,640)) for n in ['floors','north_walls','boundary_caps']}
def put(layer,key,x,y):
 im=Image.open(P/assets[key]['path']).convert('RGBA');layers[layer].alpha_composite(im,(x,y));placements.append(dict(layer=layer,asset=key,x=x,y=y))
for y in range(2,20):
 for x in range(20):
  vx=x in (4,8,12,16);hy=y in (6,10,14,18)
  n='joint_corner' if vx and hy else ('joint_west' if vx else ('joint_north' if hy else ('base_rubbed' if (x*19+y*7)%41==0 else 'base_clean')))
  put('floors',n,x*32,y*32)
put('north_walls','corner_nw',0,0);put('north_walls','corner_ne',576,0)
W=ROOT/'Tools/ArtSources/SpaceShipWallTall'
specs=['Tiles32x64/s1_hull_r01_c01.png','Tiles32x64/s1_hull_r01_c02.png','Tiles32x64/s1_hull_r01_c04.png','Tiles32x64/s1_hull_r01_c05.png','Tiles64x64/s2_hull_r01_c01.png','Tiles64x64/s1_control_system.png','Tiles64x64/s2_hull_r01_c01.png','Tiles32x64/s1_hull_r01_c06.png','Tiles32x64/s1_hull_r01_c07.png','Tiles64x64/s2_hull_r01_c01.png','Tiles64x64/s2_hull_r01_c01.png']
x=64
for f in specs:
 t=Image.open(W/f).convert('RGBA')
 for px in range(t.width):t.paste(cap,(px,0));t.paste(base,(px,60))
 key='wall_'+Path(f).stem
 save(key,t,str((W/f).relative_to(ROOT))+'; cap/base copied from corner kit')
 put('north_walls',key,x,0);x+=t.width
assert x==576
for y in range(64,576,32):put('boundary_caps','cap_west',0,y);put('boundary_caps','cap_east',632,y)
put('boundary_caps','corner_sw',0,576);put('boundary_caps','corner_se',576,576)
for x in range(64,576,32):put('boundary_caps','cap_south',x,632)
merged=Image.new('RGBA',(640,640),(24,28,34,255))
for name,im in layers.items():im.save(P/f'Layers/{name}.png');merged.alpha_composite(im)
merged.save(P/'sample_room_20x20.png');merged.resize((1280,1280),Image.Resampling.NEAREST).save(R/'sample_room_2x.png')
# Closeups prove both north turns and all four joins at native4x.
# Full corners in a2x2 review, each128x128 crop enlarged4x.
c=Image.new('RGBA',(1024,1024))
for i,box in enumerate([(0,0,128,128),(512,0,640,128),(0,512,128,640),(512,512,640,640)]):c.paste(merged.crop(box).resize((512,512),Image.Resampling.NEAREST),((i%2)*512,(i//2)*512))
c.save(R/'corner_checks_4x.png')
blocked=[[1 if y<2 or x in(0,19) or y==19 else 0 for x in range(20)] for y in range(20)]
(P/'room.json').write_text(json.dumps(dict(width_tiles=20,height_tiles=20,tile_size=32,pixel_size=[640,640],origin='top-left',layer_order=list(layers),assets=assets,placements=placements,collision=blocked,spawn_cell=[10,17],exits=[]),indent=2)+'\n')
check=Image.new('RGBA',(640,640),(24,28,34,255))
for layer in layers:
 for rec in placements:
  if rec['layer']==layer:check.alpha_composite(Image.open(P/assets[rec['asset']]['path']).convert('RGBA'),(rec['x'],rec['y']))
assert check.tobytes()==merged.tobytes()
assert merged.size==(640,640)
print('20x20 canvas; exported placements reconstruct preview exactly; generated corners and low-contrast floor.')
