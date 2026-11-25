from pathlib import Path
from PIL import Image
import json
r=Path(__file__).parent;d=json.loads((r/'room_layout.json').read_text())
floor=Image.new('RGBA',tuple(d['grid']['canvas_pixels']))
for c in d['floor_cells']:floor.alpha_composite(Image.open(r/d['floor_assets'][c['tile']]).convert('RGBA'),tuple(v*32 for v in c['cell']))
walls=Image.new('RGBA',floor.size);props=Image.new('RGBA',floor.size)
for layer,objects in [(walls,d['wall_objects']),(props,d['props'])]:
 for o in objects:
  im=Image.open(r/o['asset']).convert('RGBA');w,h=o['size_px'];layer.alpha_composite(im.crop((0,0,w,h)),tuple(o['position_px']))
for name,layer in [('floor',floor),('walls',walls),('props',props)]:layer.save(r/f'PNG/service_room_{name}_layer.png')
im=floor.copy()
im.alpha_composite(Image.open(r/'PNG/contact_shadows_layer.png').convert('RGBA'));im.alpha_composite(walls);im.alpha_composite(props)
im.save(r/'PNG/service_room_native.png');im.resize((im.width*3,im.height*3),Image.Resampling.NEAREST).save(r/'PNG/service_room_3x.png')
