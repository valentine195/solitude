// Reference-guided cleanup at the source's actual 256x256 size.
// Pixel-aligned planes, recesses and joints. No filtering, random noise or resampling.
pal({k:'#34393e',d:'#4f585f',s:'#6b757b',g:'#899195',m:'#a4aaac',
 t:'#b4b9bb',a:'#bcbfc0',b:'#c0c2c3',c:'#c4c5c5',e:'#c8c8c7',f:'#cccbc9',
 h:'#d3d1cd',i:'#dedbd5',
 u:'#537584',v:'#6e9aa9',w:'#87acb5',q:'#b4c6c7'});
// Closely spaced material steps avoid flattening the reference's subtle face lighting.
pal({A:'#cecdcb',B:'#cccbca',C:'#cacac9',D:'#c8c9c8',E:'#c6c7c6',F:'#c4c6c6',G:'#c2c4c4',H:'#c0c2c3',I:'#bec1c2',J:'#bdbfc0'});
ramps('kdsgmtabcefhi','duvwq');
// Low-contrast light falloff belongs to the broad material plane, not surface noise.
// A stepped, ten-tone field preserves the warm upper / cooler lower reference shading.
function material(c){
 c.rect(0,0,256,256,'b');
 for(let y=20;y<242;y++){
  let previous='',start=0;
  for(let x=0;x<=256;x++){
   const distance=Math.max(0,Math.abs(x-100)-60);
   const tone=Math.min(9,Math.max(0,Math.floor((y-23)/24+distance/125)));
   const color=x===256?'': 'ABCDEFGHIJ'[tone];
   if(x===0){previous=color;continue;}
   if(color!==previous){c.hline(start,y,x-start,previous);start=x;previous=color;}
  }
 }
}
function structure(c){
 // Outer side frame: exposed face, bevel and a deep inner channel.
 // Construction is mirrored; illumination favors upper-left, not mirrored highlights.
 c.rect(0,0,21,256,'t');c.rect(235,0,21,256,'t');
 c.rect(2,0,14,256,'c');c.vline(2,0,256,'h');c.vline(16,0,256,'m');
 c.rect(240,0,13,256,'c');c.vline(239,0,256,'h');c.vline(253,0,256,'m');
 c.rect(19,27,5,207,'g');c.vline(19,29,202,'m');c.vline(23,34,196,'t');
 c.rect(232,27,5,207,'g');c.vline(232,34,196,'s');c.vline(237,29,202,'h');
 // Face return immediately inside channels: narrow shadow then lit internal surface.
 c.vline(24,35,119,'t');c.vline(25,36,118,'a');
 c.vline(230,36,118,'m');c.vline(231,35,119,'t');
 c.vline(24,179,50,'t');c.vline(25,179,50,'a');
 c.vline(230,179,50,'m');c.vline(231,179,50,'t');
 // Lower return retains the original recessed floor-level rail and corner shoulders.
 c.poly([20,229,27,235,35,240,221,240,229,235,236,229,236,242,225,250,31,250,20,242],'g');
 c.poly([24,230,34,238,222,238,232,230,232,239,223,246,33,246,24,239],'t');
 c.hline(35,240,186,'h');c.hline(35,241,186,'c');
 c.rect(34,243,188,8,'b');c.hline(33,251,190,'m');c.hline(31,252,194,'g');
 c.hline(28,253,200,'s');c.hline(0,255,256,'t');
}
function upperRecess(c){
 // Dark upper occlusion and a brighter return below make the header feel cut in.
 c.poly([17,0,239,0,236,12,230,19,26,19,20,12],'k');
 c.poly([21,1,235,1,233,10,229,15,27,15,23,10],'d');
 c.rect(27,3,202,3,'m');c.hline(28,3,200,'h');
 c.rect(28,7,200,4,'u');c.hline(28,7,200,'v');c.hline(28,11,200,'d');
 c.hline(29,14,198,'g');c.hline(28,16,200,'t');c.hline(28,18,200,'h');
 // Corner mounting joints connect the top rail to the side channels.
 for(const flip of [false,true]){
  const pts=(values)=>values.map((v,index)=>index%2===0&&flip?256-v:v);
  c.poly(pts([0,1,16,1,20,15,27,20,31,30,25,38,17,39,10,34,0,30]),'m');
  c.poly(pts([0,2,13,2,16,16,24,21,27,29,22,34,17,33,10,29,0,27]),'c');
  c.poly(pts([1,3,11,3,14,18,22,23,24,28,19,30,11,25,1,23]),'h');
 }
 // Directional top-left edge highlight; consistent stair-step junctions.
 c.line(2,28,14,35,'t');c.line(14,35,19,35,'g');c.vline(20,37,14,'m');
 c.line(253,28,241,35,'g');c.line(241,35,236,35,'s');c.vline(235,37,14,'s');
}
function band(c){
 // One continuous exact-height extrusion; no independent patchy segments.
 c.rect(0,158,256,17,'u');c.hline(0,158,256,'g');
 c.hline(0,159,256,'u');c.hline(0,160,256,'w');
 c.rect(0,161,256,11,'v');c.hline(0,172,256,'u');
 c.hline(0,173,256,'g');c.hline(0,174,256,'t');c.hline(0,175,256,'h');
 // Slim dividing joints align to existing inner frame channels.
 for(const x of [20,234]){c.vline(x,159,14,'u');c.vline(x+1,161,11,'w');}
}
function hardware(c){
 // Matching flush fasteners only where construction needs them.
 for(const [x,y] of [[29,149],[224,149],[29,233],[224,233]]){
  c.rect(x-2,y-2,5,5,'t');c.hline(x-1,y-2,3,'h');
  c.rect(x-1,y-1,3,3,'m');c.hline(x-1,y,3,'g');
 }
 // The lower corner connectors are regularized rather than removed.
 c.poly([21,233,27,236,35,240,39,241,38,247,30,247,24,242],'d');
 c.poly([23,233,29,236,36,240,35,243,29,242,24,239],'g');
 c.line(24,234,33,239,'t');
 c.poly([235,233,229,236,221,240,217,241,218,247,226,247,232,242],'s');
 c.poly([233,233,227,236,220,240,221,243,227,242,232,239],'m');
 c.line(232,234,223,239,'h');
}
sprite('wall_2_clean',{w:256,h:256,layers:true},
 layer('panel_material',material),layer('structural_depth',structure),
 layer('upper_recess',upperRecess),layer('blue_band',band),layer('hardware',hardware));
scene('horizontal_repeat',{w:768,h:256},tile('wall_2_clean',0,0,3,1));
scene('repeat_grid',{w:768,h:768},tile('wall_2_clean',0,0,3,3));
