// Shared standard wall source supplies identical palette and stripe geometry.
include('../PxlStandardWallGrid256/wall.px.js');
pal({o:'#ae631e',r:'#e9952c',j:'#ffe99a',n:'#f8bd52'});
function backing(c){
 c.rect(0,0,128,256,'h');c.vline(0,0,256,'t');c.vline(127,0,256,'t');
 // The connector's infill is ordinary wall cladding, not transparent empty width.
 c.rect(5,0,118,256,'O');c.vline(4,0,256,'i');c.vline(123,0,256,'i');
 // Short wall returns at the same native header / plinth baselines.
 c.hline(5,20,118,'i');c.hline(5,21,118,'h');
 c.hline(5,240,118,'t');c.rect(5,241,118,14,'b');c.hline(5,255,118,'s');
 // Exact standard stripe rows. Central connector will occlude them deliberately.
 c.rect(0,149,128,16,'v');c.hline(0,149,128,'u');c.hline(0,150,128,'q');
 c.hline(0,151,128,'w');c.hline(0,163,128,'u');c.hline(0,164,128,'d');c.hline(0,165,128,'t');
}
function spine(c){
 c.rect(49,28,30,200,'k');c.rect(51,29,26,198,'d');
 c.vline(51,31,194,'g');c.vline(52,31,194,'s');c.vline(76,31,194,'k');
 c.rect(56,32,16,192,'d');c.vline(56,32,192,'s');
 // Top and lower dark service recesses are distinct from the amber lamp well.
 for(const [y,hh] of [[34,27],[182,37]]){
  c.rect(59,y,10,hh,'k');c.rect(61,y+2,6,hh-4,'d');c.vline(61,y+2,hh-4,'k');
  c.hline(60,y+hh,9,'g');
 }
 c.rect(58,69,12,104,'k');c.hline(59,68,10,'s');c.hline(59,173,10,'g');
}
function endBlocks(c){
 for(const y of [0,224]){
  c.poly([48,y,80,y,82,y+3,82,y+29,79,y+32,49,y+32,46,y+29,46,y+3],'k');
  c.rect(48,y+2,32,28,'d');c.hline(49,y+2,30,'g');c.vline(48,y+3,25,'s');
  c.rect(51,y+5,26,23,'d');c.hline(51,y+5,26,'s');c.vline(51,y+5,22,'g');
  c.hline(51,y+28,26,'k');c.vline(78,y+6,22,'k');
  c.rect(59,y+15,10,10,'k');c.rect(61,y+17,6,6,'s');c.hline(61,y+17,6,'g');
 }
}
function amber(c){
 // Hard-edged amber emitter. No blurred glow, filtered alpha or off-grid strokes.
 c.rect(60,72,8,99,'o');c.rect(59,76,10,91,'o');
 c.rect(61,74,6,95,'r');c.rect(62,79,4,84,'n');c.rect(63,80,3,82,'j');
 c.hline(62,79,4,'j');c.hline(62,162,4,'n');
}
sprite('wall_connector_128x256',{w:128,h:256,layers:true},
 layer('wall_infill',backing),layer('connector_spine',spine),layer('end_blocks',endBlocks),layer('amber_emitter',amber));
sprite('connector_overlay',{w:128,h:256},spine,endBlocks,amber);
scene('panel_connector_panel',{w:640,h:256},stamp('standard_wall_256',0,0),stamp('wall_connector_128x256',256,0),stamp('standard_wall_256',384,0));
