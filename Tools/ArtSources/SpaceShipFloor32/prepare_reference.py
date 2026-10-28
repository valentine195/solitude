from PIL import Image
from pathlib import Path
import json
root=Path(__file__).resolve().parent
src=root.parent/'SpaceShipExtraction'
entries=[a for a in json.loads((src/'manifest.json').read_text())['assets'] if a['file'].startswith('Floors/')]
canvas=Image.new('RGB',(2048,2048),(255,0,255))
for i,a in enumerate(entries):
 im=Image.open(src/a['file']).convert('RGB').resize((128,128),Image.Resampling.NEAREST)
 canvas.paste(im,((i%16)*128,(i//16)*128))
canvas.save(root/'reference_layout.png')
(root/'source_order.json').write_text(json.dumps(entries,indent=2))
