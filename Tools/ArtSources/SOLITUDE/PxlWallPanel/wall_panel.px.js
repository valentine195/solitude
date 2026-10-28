// SOLITUDE generic wall: native 128x256. No resampling, noise, or soft alpha.
// Horizontal repetition only. Top margin/cap and bottom plinth are terminal geometry.
include('Reference/reference.px.js');
pal({k:'#282b30',d:'#323642',s:'#4c4e5b',g:'#6f7279',r:'#7e828e',m:'#8d919c',
     t:'#a3a5b1',b:'#b6bbc1',l:'#c0c4cd',h:'#d3d6dd',v:'#aeb5bd',q:'#c9ccd1'});
ramps('kdsg rmtblqh'.replace(/ /g,''));
const W=128,H=256;
function fastener(c,x,y){
  c.rect(x-2,y-1,5,4,'t'); c.rect(x-1,y,3,3,'g');
  c.hline(x-1,y,3,'s'); c.px(x-2,y-1,'q'); c.hline(x,y+3,2,'l');
}
function panel(c,y,height){
  c.rect(14,y,100,height,'g');
  c.rect(15,y+1,98,height-2,'q');
  c.rect(17,y+3,94,height-5,'b');
  c.vline(17,y+3,height-6,'l'); c.hline(18,y+3,92,'v');
  c.vline(110,y+3,height-5,'v'); c.hline(18,y+height-3,92,'t');
  for(const yy of [y+8,y+44,y+80]) for(const x of [21,106]) fastener(c,x,yy);
}
sprite('wall_pristine',{w:W,h:H,layers:true},
 layer('structure',c=>{
  c.rect(0,18,W,238,'k');
  // Continuous cap across horizontal joins; dark lower edge reads as a recess.
  c.rect(0,19,W,22,'r');c.hline(0,19,W,'g');c.hline(0,21,W,'m');
  c.hline(0,26,W,'t');c.hline(0,27,W,'l');c.hline(0,29,W,'m');
  c.rect(0,31,W,9,'r');c.hline(0,40,W,'s');c.hline(0,42,W,'d');
  for(const x of [37,92]){c.rect(x-2,30,6,10,'g');c.rect(x,31,3,8,'s');c.vline(x+3,31,8,'t');}
  // Side rails; same exterior edge profile on both sides.
  for(const x of [2,115]){
   c.rect(x,45,11,187,'g');c.rect(x+2,46,7,184,'v');
   c.vline(x+2,46,184,'q');c.vline(x+9,46,185,'m');
  }
  c.rect(2,235,124,19,'g');c.rect(4,237,120,15,'r');
  c.hline(2,235,124,'l');c.hline(4,238,120,'m');c.hline(4,251,120,'m');
  c.vline(4,237,15,'t');c.vline(123,237,15,'s');
  c.hline(0,255,W,'d');
 }),
 layer('upper_panel',c=>panel(c,45,96)),
 layer('lower_panel',c=>panel(c,143,90))
);
// Caused wear: fastener abrasion, a short dragged-object scuff, foot-level rubs.
function wear(c){
 for(const [x,y] of [[20,89],[106,187],[21,223]]){
  c.hline(x-2,y+4,5,'t');c.hline(x-1,y+5,3,'l');c.px(x-3,y+3,'g');
 }
 c.hline(76,118,14,'t');c.hline(79,119,10,'l');c.hline(85,117,5,'g');
 c.hline(31,214,9,'v');c.hline(34,215,12,'t');c.hline(36,216,8,'l');
 c.rect(18,151,2,9,'t');c.vline(20,154,5,'l');
}
sprite('overlay_lightly_worn',{w:W,h:H},wear);
// Impact exposes graphite substrate and alloy. Chips share top-left light, lower-right shadow.
function impact(c){
 c.poly([67,69,77,72,82,79,91,80,95,89,91,97,94,105,85,111,73,108,65,101,62,90,65,82],'t');
 c.poly([68,72,76,75,81,82,89,83,92,90,87,97,90,103,83,107,74,104,68,99,65,90,68,83],'s');
 c.poly([71,78,76,79,79,86,86,86,88,92,83,97,85,102,76,100,70,94,69,86],'d');
 c.line(67,83,70,74,'q');c.line(70,74,77,77,'l');c.line(65,90,69,100,'g');
 c.line(75,104,83,107,'k');c.line(84,106,89,102,'k');
 // Stepped fractures radiate from the same impact.
 for(const points of [[[69,79],[58,68],[51,65],[46,57]],[[89,87],[99,79],[104,68]],[[78,105],[72,116],[73,125],[67,134]]]){
  for(let i=1;i<points.length;i++){const [x,y]=points[i-1],[xx,yy]=points[i];c.line(x,y,xx,yy,'g');c.line(x+1,y,xx+1,yy,'l');}
 }
 // Lower panel coating delamination around a dent; no silhouette change.
 c.poly([32,179,42,176,52,180,60,183,65,190,61,199,49,201,40,197,31,191],'t');
 c.poly([34,180,43,178,51,182,59,185,62,191,57,197,49,198,41,194,34,190],'g');
 c.poly([38,182,44,181,52,185,57,187,59,192,52,194,44,190],'r');
 c.line(35,180,42,178,'q');c.line(42,178,50,181,'l');c.line(43,195,50,198,'s');c.line(50,198,57,197,'s');
 c.line(36,191,28,202,'g');c.line(29,201,26,211,'g');
 // Abrasion follows lower lip; a few coherent flakes rather than random speckle.
 for(const [x,y,w] of [[73,222,13],[85,220,9],[91,223,10]]){
  c.hline(x,y,w,'g');c.hline(x+1,y-1,w-3,'t');c.hline(x+2,y+1,w-4,'l');
 }
}
sprite('overlay_heavy_damage',{w:W,h:H},impact);
sprite('overlay_heavily_damaged',{w:W,h:H},stamp('overlay_lightly_worn',0,0),stamp('overlay_heavy_damage',0,0));
sprite('wall_lightly_worn',{w:W,h:H,base:'wall_pristine',layers:true},layer('wear',stamp('overlay_lightly_worn',0,0)));
sprite('wall_heavily_damaged',{w:W,h:H,base:'wall_pristine',layers:true},layer('wear',stamp('overlay_lightly_worn',0,0)),layer('damage',stamp('overlay_heavy_damage',0,0)));
for(const name of ['pristine','lightly_worn','heavily_damaged']){
 scene('repeat_'+name,{w:384,h:256},tile('wall_'+name,0,0,3,1));
 scene('grid_'+name,{w:384,h:768},tile('wall_'+name,0,0,3,3));
}
scene('mixed_repeat',{w:384,h:256},stamp('wall_pristine',0,0),stamp('wall_lightly_worn',128,0),stamp('wall_heavily_damaged',256,0));
scene('comparison_reference_pristine',{w:272,h:256},stamp('_reference',0,0),stamp('wall_pristine',144,0));
sheet('wall_variants',['wall_pristine','wall_lightly_worn','wall_heavily_damaged'],{cols:3,pad:0});
