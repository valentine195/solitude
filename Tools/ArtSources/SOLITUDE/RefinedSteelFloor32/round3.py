from process_review import *
d=R/'Round3';(d/'Tiles32').mkdir(parents=True,exist_ok=True)
for src in (OLD/'Tiles32').glob('*.png'):shutil.copy2(src,d/'Tiles32'/src.name)
def remap(im,mapping):
 a=np.array(im.convert('RGBA'));b=a.copy()
 for s,t in mapping.items():b[np.all(a[:,:,:3]==list(bytes.fromhex(s)),axis=2),:3]=list(bytes.fromhex(t))
 return Image.fromarray(b)
main={'8B929C':'9299A2','7E8692':'838D99','959CA5':'9DA4AD','9EA5AE':'A6ADB5','7F8995':'838D99','969DA6':'969DA6'}
service={'8B929C':'87929E','7E8692':'7A8795','959CA5':'929DA8','9EA5AE':'9BA6B0','7F8995':'7A8795','969DA6':'8D98A3'}
for n in NAMES:
 im=Image.open(OLD/'Tiles32'/f'{n}.png')
 remap(im,main).save(d/'Tiles32'/f'{n}.png')
 remap(im,service).save(d/'Tiles32'/f'service_{n}.png')
# Assemble a two-tone joint using the generated dark seam and the earlier
# generated edge sheen. Normalize one missing seam pixel for continuity.
joint=Image.open(R/'Round2/Tiles32/joint_north.png').convert('RGBA')
a=np.array(joint);a[0,:]=a[0,0];joint=Image.fromarray(a)
sheen=Image.open(R/'Round1/Tiles32/base_clean.png').crop((1,0,31,1))
# Pad the two ends by repeating source edge pixels, not smooth filtering.
sheen=Image.fromarray(np.pad(np.array(sheen),((0,0),(1,1),(0,0)),mode='edge'))
sheen=remap(sheen,{'9CA4AE':'969DA6'});joint.paste(sheen,(0,1))
west=joint.transpose(Image.Transpose.TRANSPOSE)
corner=west.copy();corner.paste(joint.crop((0,0,32,2)),(0,0))
for name,im in [('joint_north',joint),('joint_west',west),('joint_corner',corner)]:
 remap(im,main).save(d/'Tiles32'/f'{name}.png')
 remap(im,service).save(d/'Tiles32'/f'service_{name}.png')
data=json.loads((OLD/'Room/room_layout.json').read_text())
for cell in data['floor_cells']:
 x,y=cell['cell']
 if not cell['tile'].startswith('base_'):continue
 service_zone=(x<=3 or x>=12)
 north=(y-2)%2==0
 if service_zone:
  west=x in (1,12)
 else:
  # Stagger central joints by a whole32px cell every pair of rows.
  west=x==4 or (x-4-((y-2)//2)%2)%4==0
 if north and west:name='joint_corner'
 elif north:name='joint_north'
 elif west:name='joint_west'
 else:name=cell['tile']
 cell['tile']=('service_' if service_zone else '')+name
# Keep room geometry and all props exactly as accepted. Floor only changes.
data['floor_assets']={p.stem:'Assets/Floor/'+p.name for p in sorted((d/'Tiles32').glob('*.png'))}
data['floor_revision']='RefinedSteelFloor32 Round3'
data['qa']['base_and_joint_percent']=100-data['qa']['accent_percent']
data['qa']['runtime_collision_tested']=False
floor=Image.new('RGBA',(512,352))
for c in data['floor_cells']:
 floor.alpha_composite(Image.open(d/'Tiles32'/f"{c['tile']}.png").convert('RGBA'),tuple(v*32 for v in c['cell']))
preview=floor.copy()
for n in ['walls','props']:preview.alpha_composite(Image.open(OLD/f'Room/PNG/service_room_{n}_layer.png').convert('RGBA'))
floor.save(d/'floor_layer.png');preview.save(d/'room_native.png');preview.resize((1536,1056),Image.Resampling.NEAREST).save(d/'room_3x.png')
(d/'room_layout.json').write_text(json.dumps(data,indent=2))
# Intentional atlas rows: plain, main joints, service plain, service joints, accents.
order=NAMES+['joint_north','joint_west','joint_corner','steel_plate']+['service_'+n for n in NAMES]+['service_joint_north','service_joint_west','service_joint_corner','maintenance_hatch']+['grate_horizontal','perforated_plate','amber_service_hatch','door_threshold']
atlas=Image.new('RGBA',(128,160))
for i,n in enumerate(order):atlas.alpha_composite(Image.open(d/'Tiles32'/f'{n}.png'),((i%4)*32,(i//4)*32))
atlas.save(d/'floor_atlas_32.png');atlas.resize((1024,1280),Image.Resampling.NEAREST).save(d/'floor_atlas_8x.png')
(d/'tile_manifest.json').write_text(json.dumps({'cell_pixels':32,'atlas_columns':4,'atlas_rows':5,'order':order,'base_variants':'Four seamless freely interchangeable variants per palette.','joints':'Optional north/west/corner construction tiles; place by panel pattern, not randomly.','layout':'Service side strips3cells wide; main plates4x2cells with staggered vertical joints.','source':'Generated references, integer-factor point sampling, palette cleanup, assembly of generated joint and edge sheen. Not hand-pixel-authored.'},indent=2))
