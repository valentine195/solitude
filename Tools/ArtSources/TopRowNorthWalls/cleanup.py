from pathlib import Path
from PIL import Image,ImageDraw,ImageFilter
import numpy as np
import colorsys,json,shutil,zipfile
P=Path(__file__).parent;R=P.parents[2]/'work/Reviews/TopRowNorthWalls'
archive=P/'BeforeCleanup';archive.mkdir(exist_ok=True)
for f in [P/'north_wall_atlas.png',P/'manifest.json']:
 if not (archive/f.name).exists():shutil.copy2(f,archive/f.name)
manifest=json.loads((archive/'manifest.json').read_text())
source=Image.open(archive/'north_wall_atlas.png').convert('RGB');a=np.array(source)
# Continuous cap uses the flat central profile of the approved extracted cap.
cap=np.median(a[:8,8:24,:],axis=1).astype('uint8')
a[:8,:,:]=cap[:,None,:]
# Share the same narrow structural edge on every module. Devices remain untouched.
# Plain-wall edge samples establish a dark join and interior-facing light edge.
plain=a[:,0:32].copy()
edge=plain[8:60,0].copy()
inner=plain[8:60,2].copy()
for m in manifest['modules']:
 x,_,w,_=m['atlas_rect']
 a[8:60,x]=edge;a[8:60,x+w-1]=edge
 a[8:60,x+1]=inner;a[8:60,x+w-2]=inner
 # All junctions align to the common base rail.
 a[60:64,x:x+2]=plain[60:64,4:6]
 a[60:64,x+w-2:x+w]=plain[60:64,4:6]
# Quantize material and signal families separately, reserving distinct accent colors.
pixels=a.reshape(-1,3)
hsv=np.array([colorsys.rgb_to_hsv(*(v/255)) for v in pixels])
h,s,v=hsv.T
classes=np.zeros(len(pixels),dtype=int)
classes[(s>=.20)&((h<.055)|(h>=.94))]=1
classes[(s>=.20)&(h>=.055)&(h<.19)]=2
classes[(s>=.20)&(h>=.19)&(h<.46)]=3
classes[(s>=.20)&(h>=.46)&(h<.56)]=4
classes[(s>=.20)&(h>=.56)&(h<.94)]=5
out=np.zeros_like(pixels);palette=[]
for group,count in enumerate([16,7,5,5,7,8]):
 ix=np.where(classes==group)[0]
 if not len(ix):continue
 strip=Image.fromarray(pixels[ix][None,:,:])
 q=strip.quantize(colors=count,method=Image.Quantize.MEDIANCUT,dither=Image.Dither.NONE).convert('RGB')
 out[ix]=np.array(q).reshape(-1,3)
 palette.extend(sorted(set(map(tuple,out[ix].tolist()))))
# Remove isolated palette speckles only in low-contrast material interiors.
# Mode filtering selects existing palette entries; it never averages colors.
qrgb=Image.fromarray(out.reshape(a.shape))
hi=np.array(Image.fromarray(a).filter(ImageFilter.MaxFilter(3))).astype(int)
lo=np.array(Image.fromarray(a).filter(ImageFilter.MinFilter(3))).astype(int)
flat=np.max(hi-lo,axis=2)<24
flat[:8]=False;flat[60:]=False
for m in manifest['modules']:
 xx,_,ww,_=m['atlas_rect'];flat[:,xx:xx+2]=False;flat[:,xx+ww-2:xx+ww]=False
indexed=qrgb.quantize(colors=48,dither=Image.Dither.NONE)
mode=indexed.filter(ImageFilter.ModeFilter(3)).convert('RGB')
clean=np.array(qrgb);clean[flat]=np.array(mode)[flat]
atlas=Image.fromarray(clean).convert('RGBA');atlas.save(P/'north_wall_atlas.png')
for m in manifest['modules']:
 x,_,w,_=m['atlas_rect'];tile=atlas.crop((x,0,x+w,64));tile.save(P/f'Tiles/{m["name"]}.png')
 assert tile.size==(w,64) and tile.getextrema()[3]==(255,255)
# Every possible pair uses identical cap and matching boundary profiles.
arr=np.array(atlas)
profiles=[]
for m in manifest['modules']:
 x,_,w,_=m['atlas_rect']
 assert np.array_equal(arr[:,x],arr[:,x+w-1])
 profiles.append(arr[:,x])
assert all(np.array_equal(profiles[0],p) for p in profiles)
assert all(np.array_equal(arr[:8,0],arr[:8,x]) for x in range(384))
manifest['cleanup']={'palette_colors':len(set(map(tuple,out.tolist()))),'palette_method':'Separate neutral/red/amber/green/cyan/blue families; no dithering','shared_edge_profiles':True,'continuous_cap':True,'device_pixels_repositioned':False,'source':'BeforeCleanup/north_wall_atlas.png'}
manifest['checks']['face_pixels_unchanged']=False
manifest['checks']['all_64_ordered_module_joins_match']=True
(P/'manifest.json').write_text(json.dumps(manifest,indent=2))
atlas.resize((1536,256),Image.Resampling.NEAREST).save(R/'wall_strip_4x.png')
items=manifest['modules'];s=(P/'process.py').read_text();exec(s[s.index('review=Image.new'):s.index('assert x==384')])
comparison=Image.new('RGB',(1536,600),'#20252d');d=ImageDraw.Draw(comparison)
d.text((8,8),'BEFORE CLEANUP',fill='white');comparison.paste(source.resize((1536,256),Image.Resampling.NEAREST),(0,28))
d.text((8,308),'CLEANED - SHARED JOINS, CONTINUOUS CAP, 48-COLOR MAXIMUM PALETTE',fill='white');comparison.paste(atlas.resize((1536,256),Image.Resampling.NEAREST),(0,328));comparison.save(R/'cleanup_before_after.png')
context=Image.open(R/'wall_floor_context_4x.png');context.paste(atlas.resize((1536,256),Image.Resampling.NEAREST),(0,0));context.save(R/'wall_floor_context_4x.png')
# Render all ordered adjacency pairs for visual seam inspection.
seams=Image.new('RGB',(8*128,8*64),'#20252d')
for i,l in enumerate(items):
 for j,r in enumerate(items):
  lt=Image.open(P/f'Tiles/{l["name"]}.png');rt=Image.open(P/f'Tiles/{r["name"]}.png')
  seams.paste(lt,(j*128+64-lt.width,i*64));seams.paste(rt,(j*128+64,i*64))
seams.save(R/'all_module_pairs.png')
with zipfile.ZipFile(P/'north_walls.zip','w',zipfile.ZIP_DEFLATED) as z:
 for f in list((P/'Tiles').glob('*.png'))+[P/'north_wall_atlas.png',P/'manifest.json']:z.write(f,f.relative_to(P))
print(json.dumps(manifest['cleanup']));print('Dimensions, opacity, all 64 ordered join combinations verified.')
