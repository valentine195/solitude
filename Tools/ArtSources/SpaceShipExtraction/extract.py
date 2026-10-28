from PIL import Image, ImageOps, ImageDraw
from pathlib import Path
import json, hashlib
ROOT=Path(__file__).resolve().parents[3]
OUT=Path(__file__).resolve().parent
SRC=ROOT/'Assets/_Project/Art/Imported/SpaceShip'
records=[]
images={i:Image.open(SRC/f'{i}.png').convert('RGBA') for i in range(1,6)}
def crop(i,kind,name,box):
    im=images[i]
    # Coordinates are curated against the supplied display; convert to native pixels once.
    dw,dh=(1568,1577) if i==4 else (1600,1600)
    b=tuple(round(v*im.size[j%2]/(dw if j%2==0 else dh)) for j,v in enumerate(box))
    part=im.crop(b)
    rel=f'{kind}/s{i}_{name}.png'
    (OUT/kind).mkdir(exist_ok=True)
    part.save(OUT/rel)
    assert part.tobytes()==im.crop(b).tobytes()
    records.append(dict(file=rel,source=f'{i}.png',box_xyxy=list(b),size=list(part.size)))
def grid(i,kind,prefix,xs,ys,exclude=()):
    for y in range(len(ys)-1):
        for x in range(len(xs)-1):
            if (x,y) not in exclude:
                crop(i,kind,f'{prefix}_r{y+1:02}_c{x+1:02}',(xs[x],ys[y],xs[x+1],ys[y+1]))
# Every section follows visible authored borders, not an assumed pixel-art grid.
grid(1,'Walls','hull',[0,94,188,282,376,470,564,658,752],[0,192])
grid(1,'Floors','deck',[0,94,188,282,376,470,564,658,752],[192,287,382,477,572,667,760])
for n,b in [('control_left',(754,27,944,192)),('control_system',(944,27,1134,192)),('control_alert',(1134,27,1320,192)),('switch',(1506,27,1598,192))]:crop(1,'Walls',n,b)
for n,b in [('airlock_wall',(1320,27,1506,192)),('airlock',(17,766,180,947)),('sliding',(204,761,360,947)),('window_panel',(391,768,549,947)),('hatch',(577,772,738,947))]:crop(1,'Doors',n,b)
grid(2,'Walls','hull',[0,160,320,480,640,800],[0,160])
grid(2,'Doors','hull',[800,960,1120,1280,1440,1600],[0,160])
grid(2,'Floors','deck',[0,160,320,480,640,800,960,1120,1280],[160,320,480])
crop(2,'Floors','clean_panel',(1280,320,1440,480))
# Occupied floor tiles at top right are deliberately excluded.
grid(3,'Walls','medical',[0,94,188,282,330,374,470,752],[0,193])
grid(3,'Floors','steel',[0,94,188,282,374],[480,574,668,760])
grid(3,'Floors','grate',[374,468,562,656,752],[480,574,668,760],exclude={(1,0),(2,0),(3,0),(1,1),(2,1),(3,1)})
grid(3,'Floors','glass',[0,94,188,282,374],[760,854,948,1042])
grid(3,'Floors','containment',[0,94,188,280],[1042,1136,1230,1324,1416])
# Sheet 4 is a wall construction kit. Preserve irregular shapes in bounded source chunks.
for n,b in [('upper_left',(0,21,271,277)),('upper_middle',(279,21,457,277)),('upper_right',(465,21,550,277)),('upper_end',(558,21,737,277)),('lit_left',(0,284,271,464)),('lit_middle',(279,284,457,464)),('lit_right',(465,284,550,464)),('lit_end',(558,284,737,464)),('lower_left',(0,472,87,649)),('jamb_left',(279,472,363,649)),('lower_panel',(371,472,457,649)),('jamb_right',(465,472,550,649)),('bottom_left',(0,657,87,742)),('bottom_middle',(279,657,363,742)),('bottom_panel',(371,657,457,742)),('bottom_right',(465,657,550,742)),('bottom_end',(650,657,737,742))]:crop(4,'Walls',n,b)
for n,b in [('closed',(94,561,271,742)),('open_upper',(558,472,737,649)),('open_lower',(558,657,643,742))]:crop(4,'Doors',n,b)
grid(4,'Floors','deck',[0,184,368],[747,929,1113,1298,1486])
grid(5,'Walls','armory',[0,187,374,562,752],[20,194])
grid(5,'Floors','deck',[0,94,187,281,374,468,562],[194,288,382,477])
grid(5,'Floors','lower',[0,94,187],[477,572,668])
crop(5,'Floors','landing',(187,477,374,668))
crop(5,'Floors','cross',(374,477,562,668))
grid(5,'Floors','conduit',[562,657,752],[382,477,572,668])
for n,b in [('door_panel',(0,763,187,949)),('lit_panel',(187,763,373,949))]:crop(5,'Walls',n,b)
grid(5,'Floors','secondary',[0,187,373],[949,1139,1327,1518])
manifest={'operation':'Lossless rectangular extraction only; no resizing, redrawing, quantization, alignment, perspective repair, or background removal.','sources':{f'{i}.png':{'size':list(images[i].size),'sha256':hashlib.sha256((SRC/f'{i}.png').read_bytes()).hexdigest()} for i in images},'assets':records}
(OUT/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
review=ROOT/'work/Reviews/SpaceShipExtraction';review.mkdir(parents=True,exist_ok=True)
for kind in ['Floors','Walls','Doors']:
    entries=[r for r in records if r['file'].startswith(kind+'/')]
    sheet=Image.new('RGB',(1000,((len(entries)+7)//8)*130),(35,38,44)); d=ImageDraw.Draw(sheet)
    for j,r in enumerate(entries):
        im=Image.open(OUT/r['file']); im.thumbnail((118,104),Image.Resampling.NEAREST)
        x=(j%8)*125;y=(j//8)*130
        sheet.paste(im,(x+(125-im.width)//2,y),im)
        d.text((x+3,y+106),Path(r['file']).stem,fill='white',font_size=9)
    sheet.save(review/f'{kind.lower()}_contact_sheet.png')
print({kind:sum(r['file'].startswith(kind+'/') for r in records) for kind in ['Floors','Walls','Doors']})
