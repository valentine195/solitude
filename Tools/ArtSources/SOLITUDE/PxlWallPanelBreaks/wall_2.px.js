// Reference-guided cleanup at the source's actual 256x256 size.
// Pixel-aligned planes, recesses and joints. No filtering, random noise or resampling.
pal({k:'#34393e',d:'#4f585f',s:'#6b757b',g:'#899195',m:'#a4aaac',
 t:'#b4b9bb',a:'#bcbfc0',b:'#c0c2c3',c:'#c4c5c5',e:'#c8c8c7',f:'#cccbc9',
 h:'#d3d1cd',i:'#dedbd5',
 u:'#537584',v:'#6e9aa9',w:'#87acb5',q:'#b4c6c7'});
// Closely spaced material steps avoid flattening the reference's subtle face lighting.
pal({A:'#cecdcb',B:'#cccbca',C:'#cacac9',D:'#c8c9c8',E:'#c6c7c6',F:'#c4c6c6',G:'#c2c4c4',H:'#c0c2c3',I:'#bec1c2',J:'#bdbfc0'});
// Explicit requested off-white; all other output colors retain the approved palette.
pal({O:'#eef1ef'});
ramps('kdsgmtabcefhiO','duvwq');
// Existing plane refined in place: one clean off-white, no broad tonal gradient.
function material(c){c.rect(0,0,256,256,'O');}
function structure(c){
 // Straight, shallow panel breaks at the original inset positions. These are not columns.
 // The same cross section continues from the upper channel to the square lower break.
 for(const x of [20,232]){
  c.vline(x,19,223,'m');c.vline(x+1,19,223,'t');
  c.vline(x+2,19,223,'i');c.vline(x+3,19,223,'h');
 }
 // A square horizontal panel break, connected to the vertical seams with no fittings.
 c.hline(20,240,216,'m');c.hline(20,241,216,'t');
 c.hline(20,242,216,'i');c.hline(20,243,216,'h');
}
function upperRecess(c){
 // Dark upper occlusion and a brighter return below make the header feel cut in.
 c.poly([17,0,239,0,236,12,230,19,26,19,20,12],'k');
 c.poly([21,1,235,1,233,10,229,15,27,15,23,10],'d');
 c.rect(27,3,202,3,'m');c.hline(28,3,200,'h');
 c.rect(27,6,202,3,'k');c.rect(28,9,200,3,'u');c.hline(29,12,198,'v');c.vline(27,6,7,'k');
 c.hline(29,14,198,'s');c.hline(28,16,200,'m');c.hline(28,17,200,'i');c.hline(28,18,200,'h');
 // Raised upper lip casts a crisp shadow down onto the panel face.
 c.hline(30,19,196,'g');c.hline(31,20,194,'m');c.hline(32,21,192,'t');
}
function band(c){
 // Same continuous stripe position and thickness; subtle top light / underside shadow.
 c.rect(0,158,256,17,'v');c.hline(0,158,256,'q');
 c.hline(0,159,256,'w');c.hline(0,172,256,'u');
 c.hline(0,173,256,'g');c.hline(0,174,256,'t');
 c.hline(0,175,256,'i');
 // No upright rail intersections or little seams break the continuous teal face.
}
function hardware(c){
 // Matching flush fasteners only where construction needs them.
 for(const [x,y] of [[29,149],[224,149],[29,233],[224,233]]){
  c.rect(x-2,y-2,5,5,'t');c.hline(x-1,y-2,3,'h');
  c.rect(x-1,y-1,3,3,'m');c.hline(x-1,y,3,'g');
 }
}

sprite('standard_wall_panel_breaks',{w:256,h:256,layers:true},
 layer('panel_material',material),layer('structural_depth',structure),
 layer('upper_recess',upperRecess),layer('blue_band',band),layer('hardware',hardware));
scene('horizontal_repeat',{w:768,h:256},tile('standard_wall_panel_breaks',0,0,3,1));
scene('repeat_grid',{w:768,h:768},tile('standard_wall_panel_breaks',0,0,3,3));
