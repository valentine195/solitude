from process_review import *
d=R/'Round2';(d/'Tiles32').mkdir(parents=True,exist_ok=True)
for src in (OLD/'Tiles32').glob('*.png'):shutil.copy2(src,d/'Tiles32'/src.name)
raw=Image.open(R/'Source/round2_generated.png')
n=quant(raw.crop((0,0,608,608)).resize((32,32),Image.Resampling.NEAREST),['6C7682','848C97','979EA7'])
a=np.array(n)
for source,target in [('6C7682','7F8995'),('848C97','8B929C'),('979EA7','969DA6')]:
 mask=np.all(a[:,:,:3]==list(bytes.fromhex(source)),axis=2);a[mask,:3]=list(bytes.fromhex(target))
n=Image.fromarray(a)
w=n.transpose(Image.Transpose.TRANSPOSE)
c=w.copy();c.paste(n.crop((0,0,32,1)),(0,0))
for name,im in [('joint_north',n),('joint_west',w),('joint_corner',c)]:im.save(d/'Tiles32'/f'{name}.png')
data=json.loads((OLD/'Room/room_layout.json').read_text())
for cell in data['floor_cells']:
 x,y=cell['cell']
 if not cell['tile'].startswith('base_'):continue
 north=(y-2)%2==0;west=(x-1)%3==0
 if north and west:cell['tile']='joint_corner'
 elif north:cell['tile']='joint_north'
 elif west:cell['tile']='joint_west'
build(2,[Image.open(OLD/'Tiles32'/f'{n}.png') for n in NAMES],data['floor_cells'])
