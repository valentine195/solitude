from pathlib import Path
from PIL import Image
import json
r=Path(__file__).parent
d=json.loads((r/'room_layout.json').read_text())
im=Image.new('RGBA',tuple(d['grid']['canvas_pixels']))
for c in d['floor_cells']:im.alpha_composite(Image.open(r/d['floor_assets'][c['tile']]).convert('RGBA'),tuple(v*32 for v in c['cell']))
for o in d['wall_objects']+d['props']:
 s=Image.open(r/o['asset']).convert('RGBA');w,h=o['size_px'];im.alpha_composite(s.crop((0,0,w,h)),tuple(o['position_px']))
im.save(r/'PNG/service_room_native.png')
im.resize((im.width*3,im.height*3),Image.Resampling.NEAREST).save(r/'PNG/service_room_3x.png')
