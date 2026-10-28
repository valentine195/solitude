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
 // A 2px boundary on each panel replaces the previous 20px-wide structural rails.
 // Pale returns keep the join visible without a repeated dark column.
 c.vline(0,0,256,'t');c.vline(1,0,256,'i');
 c.vline(254,0,256,'i');c.vline(255,0,256,'t');
 // Restrained edge-focused depth at the lower return; no shading over the face.
 c.hline(35,239,186,'i');c.hline(35,240,186,'h');
 c.hline(35,241,186,'t');c.rect(35,242,186,9,'O');
 c.hline(34,251,188,'i');c.hline(32,252,192,'t');
 c.hline(30,253,196,'m');c.hline(0,255,256,'i');
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
 // Corner mounting joints connect the top rail to the side channels.
 for(const flip of [false,true]){
  const pts=(values)=>values.map((v,index)=>index%2===0&&flip?256-v:v);
  c.poly(pts([0,1,16,1,20,15,27,20,31,30,25,38,17,39,10,34,0,30]),'g');
  c.poly(pts([0,2,13,2,16,16,24,21,27,29,22,34,17,33,10,29,0,27]),'c');
  c.poly(pts([1,3,11,3,14,18,22,23,24,28,19,30,11,25,1,23]),flip?'c':'h');
  // Each corner has an upper-left exposed glint and a lower-right shadow edge.
  if(flip){c.hline(244,3,9,'h');c.line(230,28,237,34,'s');}
  else{c.hline(1,3,9,'i');c.line(23,30,19,34,'g');}
 }
 // Directional top-left edge highlight; consistent stair-step junctions.
 c.line(2,28,14,35,'t');c.line(14,35,19,35,'g');c.vline(20,37,14,'m');
 c.line(253,28,241,35,'g');c.line(241,35,236,35,'s');c.vline(235,37,14,'s');
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
 // Matched manufactured fittings: regular chamfers, flat faces, no ragged tips.
 for(const flip of [false,true]){
  const pts=values=>values.map((v,index)=>index%2===0&&flip?256-v:v);
  c.poly(pts([21,231,25,231,37,239,40,239,40,247,31,247,21,239]),'m');
  c.poly(pts([23,232,25,232,36,240,38,240,38,244,32,244,23,238]),'i');
  c.line(flip?231:23,232,flip?220:36,240,'h');
  c.line(flip?224:32,245,flip?217:39,245,'g');
 }
}

sprite('standard_wall_refined',{w:256,h:256,layers:true},
 layer('panel_material',material),layer('structural_depth',structure),
 layer('upper_recess',upperRecess),layer('blue_band',band),layer('hardware',hardware));
scene('horizontal_repeat',{w:768,h:256},tile('standard_wall_refined',0,0,3,1));
scene('repeat_grid',{w:768,h:768},tile('standard_wall_refined',0,0,3,3));
