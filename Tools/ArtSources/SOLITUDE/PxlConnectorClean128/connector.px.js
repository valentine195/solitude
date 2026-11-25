// Standalone reference cleanup. Native 128x256 canvas; transparent margins are intentional.
// Fixed shared steel/graphite ramp plus a single consistent amber-emitter ramp.
pal({k:'#26353d',d:'#3b505b',s:'#5a6e79',g:'#84959e',m:'#a8b3b8',
 o:'#ae631e',r:'#e9952c',n:'#f8bd52',j:'#ffe99a'});
ramps('kdsgm','ornj');
function spine(c){
 c.rect(50,39,28,177,'k');c.rect(51,40,26,175,'d');
 c.vline(52,42,171,'s');c.vline(53,43,169,'g');
 c.rect(55,43,18,169,'d');c.vline(55,43,169,'s');
 c.vline(74,43,169,'s');c.vline(75,42,171,'k');
 // Existing upper and lower recesses, with one crisp inner shadow and return edge.
 for(const [y,h] of [[43,24],[178,32]]){
  c.rect(59,y,10,h,'k');c.rect(61,y+2,6,h-4,'d');c.vline(61,y+2,h-4,'k');
  c.hline(60,y+h,9,'g');c.hline(61,y+h+1,7,'s');
 }
 // Lamp socket retains the long narrow opening and clipped ends.
 c.poly([60,74,68,74,70,78,70,162,68,167,60,167,58,162,58,78],'k');
 c.hline(60,73,8,'s');c.hline(60,168,8,'g');c.hline(61,169,6,'s');
}
function endBlocks(c){
 for(const y of [10,214]){
  c.poly([50,y,78,y,80,y+2,80,y+29,78,y+32,50,y+32,48,y+29,48,y+2],'k');
  c.rect(50,y+2,28,28,'d');c.hline(51,y+2,26,'g');c.vline(50,y+3,25,'s');
  c.rect(52,y+5,24,23,'d');c.hline(52,y+5,24,'s');c.vline(52,y+5,22,'g');
  c.hline(52,y+28,24,'k');c.vline(77,y+6,22,'k');
  // Square inset blocks stay geometric, matching in size and native placement.
  c.rect(59,y+16,10,10,'k');c.rect(61,y+18,6,6,'s');
  c.hline(61,y+18,6,'g');c.vline(66,y+19,5,'d');
 }
}
function emitter(c){
 c.poly([61,76,67,76,69,80,69,161,67,165,61,165,59,161,59,80],'o');
 c.rect(61,79,6,83,'r');c.rect(62,83,4,75,'n');
 c.rect(63,84,3,73,'j');c.hline(63,83,2,'j');c.hline(63,157,2,'n');
}
sprite('connector_clean_128x256',{w:128,h:256,layers:true},layer('spine',spine),layer('end_blocks',endBlocks),layer('amber_emitter',emitter));
