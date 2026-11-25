from PIL import Image, ImageDraw
from pathlib import Path
import json,hashlib
P=Path(__file__).resolve().parent;ROOT=P.parents[2]
review=ROOT/'work/Reviews/SpaceShipWall64x32';review.mkdir(parents=True,exist_ok=True)
(P/'Tiles').mkdir(exist_ok=True);(P/'Boundaries').mkdir(exist_ok=True)
entries=json.loads((P/'source_order.json').read_text())
raw=Image.open(P/'restored_raw.png').convert('RGB')
assert raw.size==(1024,1536),'Requires exact4x source; never fractionally scale.'
small=raw.resize((256,384),Image.Resampling.NEAREST)
# One shared palette, without dithering; raw artwork retained separately.
pal=small.quantize(colors=128,method=Image.Quantize.MEDIANCUT,dither=Image.Dither.NONE)
small=small.quantize(palette=pal,dither=Image.Dither.NONE).convert('RGBA')
base=small.crop((0,0,64,32))
# Shared structural profiles are copied from the generated master, not redrawn.
# Standardize their end columns using an interior column so neighbors join exactly.
cap=base.crop((0,0,64,8));rail=base.crop((0,30,64,32))
for band in (cap,rail):
 col=band.crop((32,0,33,band.height))
 for x in (0,1,62,63):band.paste(col,(x,0))
records=[];tiles=[]
for i,src in enumerate(entries):
 x=(i%4)*64;y=(i//4)*32
 t=small.crop((x,y,x+64,y+32))
 t.paste(cap,(0,0));t.paste(rail,(0,30))
 name=Path(src['file']).stem
 path='Tiles/'+name+'.png';t.save(P/path);tiles.append(t)
 records.append(dict(file=path,role='north_wall',source=src,raw_box=[(i%4)*256,(i//4)*128,(i%4+1)*256,(i//4+1)*128],size=[64,32],cap_rows=[0,8],face_rows=[8,30],base_rows=[30,32]))
# Cap-only wall boundaries. Rotate only the generated top surface, never a wall face.
v=cap.crop((16,0,48,8)).transpose(Image.Transpose.ROTATE_90)
for name in ('north','south','west','east','north_west','north_east','south_west','south_east'):
 t=Image.new('RGBA',(64,32))
 if 'north' in name:t.paste(cap,(0,0))
 if 'south' in name:t.paste(cap,(0,24))
 if 'west' in name:t.paste(v,(0,0))
 if 'east' in name:t.paste(v,(56,0))
 path='Boundaries/wall_cap_'+name+'.png';t.save(P/path);tiles.append(t)
 records.append(dict(file=path,role='cap_only_'+name,derived_from='Tiles/'+Path(entries[0]['file']).name,size=[64,32]))
atlas=Image.new('RGBA',(512,224))
for i,(t,r) in enumerate(zip(tiles,records)):
 xy=((i%8)*64,(i//8)*32);atlas.paste(t,xy);r['atlas_xy']=list(xy)
 assert t.size==(64,32)
 assert set(t.getchannel('A').tobytes())<={0,255}
 assert t.tobytes()==atlas.crop((*xy,xy[0]+64,xy[1]+32)).tobytes()
 r['sha256']=hashlib.sha256((P/r['file']).read_bytes()).hexdigest()
atlas.save(P/'wall_atlas_64x32.png')
atlas.resize((2048,896),Image.Resampling.NEAREST).save(review/'wall_atlas_4x.png')
for page in range(2):
 items=list(zip(tiles,records))[page*28:(page+1)*28]
 sheet=Image.new('RGB',(1056,((len(items)+3)//4)*158),(31,35,41));d=ImageDraw.Draw(sheet)
 for j,(t,r) in enumerate(items):
  x=j%4*264;y=j//4*158
  scaled=t.resize((256,128),Image.Resampling.NEAREST)
  sheet.paste(scaled,(x,y),scaled)
  d.text((x+2,y+132),Path(r['file']).stem,fill='white',font_size=11)
 sheet.save(review/f'wall_review_page_{page+1}.png')
# Repetition evidence: north walls repeat horizontally; cap-only pieces use their authored orientation.
rep=Image.new('RGB',(800,24*92),(31,35,41));d=ImageDraw.Draw(rep)
for i,t in enumerate(tiles[:47]):
 x=i%2*400;y=i//2*92
 for j in range(3):rep.paste(t.resize((128,64),Image.Resampling.NEAREST),(x+j*128,y))
 d.text((x,y+66),Path(records[i]['file']).stem,fill='white',font_size=11)
rep.save(review/'horizontal_repeat_checks_2x.png')
for page in range(3):rep.crop((0,page*736,800,min((page+1)*736,rep.height))).save(review/f'repeat_page_{page+1}.png')
# Closed-room preview on the earlier32px floor pass, displayed4x; no generated geometry.
room=Image.new('RGBA',(384,176),(24,27,33,255))
floor=Image.open(ROOT/'Tools/ArtSources/SpaceShipFloor32/Tiles/s5_deck_r02_c01.png').convert('RGBA')
for y in range(32,160,32):
 for x in range(0,384,32):room.paste(floor,(x,y))
for x,t in zip(range(0,384,64),[tiles[n] for n in (0,3,8,9,4,1)]):room.paste(t,(x,0))
for y in range(32,160,32):
 for name,x in [('west',0),('east',320)]:
  t=Image.open(P/f'Boundaries/wall_cap_{name}.png');room.alpha_composite(t,(x,y))
for x in range(0,384,64):room.paste(cap,(x,160))
room.save(review/'room_context_native.png');room.resize((1536,704),Image.Resampling.NEAREST).save(review/'room_context_4x.png')
qc=dict(tile_count=55,north_walls=47,cap_boundaries=8,cell_size=[64,32],atlas_size=[512,224],columns=8,rows=7,margin=0,spacing=0,alpha='binary only',resampling='exact4x nearest-neighbor reduction',palette='128 shared colors; no dithering',shared_profiles='8px top cap;22px north face;2px base rail',all_cells_match_exports=True,north_cap_horizontal_edge_match=cap.crop((0,0,1,8)).tobytes()==cap.crop((63,0,64,8)).tobytes(),north_rail_horizontal_edge_match=rail.crop((0,0,1,2)).tobytes()==rail.crop((63,0,64,2)).tobytes())
assert qc['north_cap_horizontal_edge_match'] and qc['north_rail_horizontal_edge_match']
(P/'manifest.json').write_text(json.dumps(dict(contract=qc,assets=records),indent=2)+'\n')
(P/'qc.json').write_text(json.dumps(qc,indent=2)+'\n')
print(json.dumps(qc,indent=2))
