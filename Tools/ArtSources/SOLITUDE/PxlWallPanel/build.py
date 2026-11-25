"""Rebuild native art, layered handoff, and pixel QC. All outputs stay outside Assets."""
from pathlib import Path
from PIL import Image
import subprocess,os,sys
root=Path(__file__).resolve().parent
pxl=Path.home()/'.codex/skills/pxl-pixel-art/bin/pxl.js'
aseprite=os.environ.get('ASEPRITE_BIN',str(Path.home()/'Applications/aseprite/build/bin/aseprite'))
subprocess.run(['node',str(pxl),'build',str(root/'wall_panel.px.js'),'--scale','3'],check=True)
# Image{fromFile} may retain indexed mode; convert losslessly before creating RGB cels.
for source in (root/'out/sprites').rglob('*.png'):
 target=root/'Editable/LayerInputs'/source.relative_to(root/'out/sprites')
 target.parent.mkdir(parents=True,exist_ok=True)
 Image.open(source).convert('RGBA').save(target)
subprocess.run([aseprite,'-b','--script-param','root='+str(root),'--script',str(root/'export_aseprite.lua')],check=True)
subprocess.run([sys.executable,str(root/'validate.py')],check=True)
