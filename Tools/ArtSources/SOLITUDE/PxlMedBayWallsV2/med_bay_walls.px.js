// Native 128x256 wall faces. Wall caps belong to the separate foreground asset.
// Geometry fills every row. All drawing coordinates are native integer pixels.
pal({k:'#30373e',s:'#4c565e',g:'#737f86',m:'#97a2a5',t:'#adb6b5',
 b:'#c4c9c2',h:'#dce0d4',l:'#ced3c9',i:'#e4e7dc',
 u:'#5d7b85',v:'#78949a',a:'#a9bdbb',
 e:'#405b58',f:'#698d79',w:'#b5c9ae',o:'#c2aa79'});
ramps('ksgmtblhi','eufvw');
function screw(c,x,y){c.rect(x,y,3,3,'m');c.hline(x,y,2,'s');c.px(x,y+2,'h');}
function medicalMark(c,x,y,size=1){c.rect(x+2*size,y,3*size,7*size,'a');c.rect(x,y+2*size,7*size,3*size,'a');}
function face(c){
 c.rect(0,0,128,256,'g');
 // Tall construction rails carry uninterrupted to the top and bottom, with no cap.
 for(const x of [2,118]){c.rect(x,0,8,256,'m');c.vline(x,0,256,'l');c.vline(x+7,0,256,'s');}
 c.vline(0,0,256,'s');c.vline(127,0,256,'s');
 for(const [y,height] of [[0,154],[156,100]]){
  c.rect(11,y,106,height,'g');c.rect(12,y,104,height,'l');c.rect(14,y,100,height,'b');
  c.vline(14,y,height,'h');c.vline(113,y,height,'t');
 }
 c.hline(11,152,106,'m');c.hline(11,153,106,'s');c.hline(11,155,106,'h');
 // Continuous desaturated blue clinical wayfinding stripe, readable between fixtures.
 c.rect(0,88,128,15,'u');c.hline(0,88,128,'v');c.hline(0,103,128,'a');
 for(const x of [18,107])for(const y of [12,140,168,242])screw(c,x,y);
}
// V1 rail, stripe and joint positions retained exactly; depth touches the interior only.
function architecture(c){
 for(const [y,hgt] of [[0,150],[156,100]]){
  c.vline(15,y,hgt,'l');c.vline(112,y,hgt,'m');c.vline(111,y,hgt,'t');
 }
 // Restrained molded channels and a shallow lower shadow on the shared joint.
 c.hline(25,23,77,'t');c.hline(25,24,77,'l');
 c.hline(26,132,75,'t');c.hline(26,133,75,'l');
 c.hline(15,150,97,'t');c.hline(15,151,97,'m');
 c.hline(15,156,97,'i');c.hline(15,157,97,'l');
}
// Recess: light upper/left lip, dark inner upper/left occlusion, narrow lower return.
function recess(c,x,y,wid,hgt){
 c.rect(x,y,wid,hgt,'m');c.hline(x,y,wid,'h');c.vline(x,y,hgt,'h');
 c.rect(x+1,y+1,wid-2,hgt-2,'s');c.rect(x+3,y+3,wid-5,hgt-5,'t');
 c.hline(x+3,y+3,wid-6,'g');c.vline(x+3,y+3,hgt-6,'g');
 c.hline(x+3,y+hgt-3,wid-5,'l');c.vline(x+wid-3,y+3,hgt-5,'l');
}
function hatch(c,x,y,wid,hgt){
 recess(c,x,y,wid,hgt);c.rect(x+5,y+5,wid-9,hgt-10,'b');
 c.hline(x+5,y+5,wid-9,'h');c.vline(x+5,y+5,hgt-10,'l');
 c.hline(x+5,y+hgt-6,wid-9,'m');
 c.rect(x+wid-13,y+hgt-13,7,3,'s');c.hline(x+wid-13,y+hgt-14,7,'h');
}
function identity(c){c.rect(86,43,15,15,'u');medicalMark(c,90,47);}
// Ordinary A: offset upper maintenance hatch, single small lower inspection door.
sprite('overlay_med_bay_access_a',{w:128,h:256},c=>{
 hatch(c,28,37,40,37);c.rect(34,44,13,3,'u');
 hatch(c,78,196,23,31);
});
// Ordinary B: paired lower service covers and a narrow high inspection slot.
sprite('overlay_med_bay_access_b',{w:128,h:256},c=>{
 recess(c,82,39,17,31);c.rect(87,45,6,18,'g');c.vline(87,45,17,'s');
 hatch(c,27,194,35,33);hatch(c,65,194,35,33);
 c.hline(33,202,13,'t');c.hline(71,202,13,'t');
});
// Patient station: one sealed digital terminal at human reach, keyed outlets beneath.
sprite('overlay_med_bay_utility',{w:128,h:256},c=>{
 identity(c);
 recess(c,23,169,82,68);
 c.rect(29,175,70,28,'k');c.hline(29,175,70,'g');
 // Information is carried by grouped bars and icons, not illegible micro text.
 medicalMark(c,33,179);c.hline(45,179,22,'a');c.hline(45,182,15,'v');
 c.hline(33,195,55,'u');
 const trace=[[33,191],[46,191],[49,188],[51,197],[54,184],[57,193],[60,191],[66,191]];
 for(let n=1;n<trace.length;n++)c.line(...trace[n-1],...trace[n],'a');
 c.rect(91,179,3,3,'w');c.rect(91,185,3,3,'o');c.hline(72,189,15,'v');c.hline(72,193,11,'v');
 // O2: round keyed socket, pale green ring; vacuum: square/keyed; data: shuttered slot.
 c.rect(29,207,20,22,'l');c.ring(39,216,7,'g');c.ring(39,216,6,'h');c.ring(39,216,4,'f');c.circ(39,216,2,'k');c.px(39,210,'w');
 c.rect(55,207,19,22,'l');c.rect(58,210,13,13,'g');c.rect(59,211,11,11,'h');c.rect(61,213,7,7,'s');c.rect(63,213,3,5,'k');c.hline(61,224,7,'u');
 c.rect(79,208,19,19,'m');c.hline(79,208,19,'h');c.rect(82,213,13,6,'k');
 for(const x of [84,87,90])c.vline(x,214,3,'t');c.rect(82,222,3,2,'w');c.hline(88,222,6,'g');
 // Subtle embossed legends distinguish connections without adding luminous accents.
 c.hline(34,231,10,'u');c.hline(59,231,10,'u');c.hline(83,231,10,'g');
});
// Supply module: accessible lower cabinet, recessed opposing grips and flush doors.
sprite('overlay_med_bay_supply',{w:128,h:256},c=>{
 identity(c);recess(c,23,166,82,72);
 for(const x of [28,65]){
  c.rect(x,171,35,61,'l');c.hline(x,171,35,'i');c.vline(x,171,61,'h');c.hline(x,231,35,'m');
 }
 c.rect(33,178,24,12,'u');medicalMark(c,41,181);
 c.rect(70,178,23,12,'u');for(const x of [75,81,87])c.rect(x,181,3,6,'a');
 for(const x of [54,68]){c.rect(x,207,6,17,'m');c.hline(x,207,6,'h');c.rect(x+1,209,4,12,'s');c.vline(x+4,210,10,'k');}
});
const states=['access_a','access_b','utility','supply'];
for(const name of states){
 sprite('wall_med_bay_'+name,{w:128,h:256,layers:true},
  layer('wall_face',face),layer('architectural_depth',architecture),layer('fixtures',stamp('overlay_med_bay_'+name,0,0)));
 scene('repeat_'+name,{w:384,h:256},tile('wall_med_bay_'+name,0,0,3,1));
 scene('grid_'+name,{w:384,h:768},tile('wall_med_bay_'+name,0,0,3,3));
}
// Native 32x48 human scale stand-in. Not the game's 64x96 Caretaker artwork.
pal({z:'#252e36',j:'#c29b7b',n:'#7f604b',c:'#879d9e',p:'#b4c3bc'});
sprite('player_scale_reference',{w:32,h:48},c=>{
 c.rect(11,1,10,3,'z');c.rect(9,4,14,10,'z');c.rect(11,6,10,9,'j');c.hline(11,13,10,'n');
 c.rect(13,15,6,3,'j');c.rect(8,18,16,16,'c');c.hline(8,18,16,'p');
 c.rect(5,20,3,16,'c');c.rect(24,20,3,16,'c');c.rect(5,35,3,3,'j');c.rect(24,35,3,3,'j');
 c.rect(9,33,6,12,'z');c.rect(17,33,6,12,'z');c.rect(8,45,7,3,'s');c.rect(17,45,7,3,'s');c.rect(19,21,3,4,'h');
});
const run=['access_a','access_b','utility','access_a','supply','access_b'];
scene('six_panel_wall_run',{w:768,h:256},...run.map((name,i)=>stamp('wall_med_bay_'+name,i*128,0)));
scene('six_panel_scale_review',{w:808,h:256},...run.map((name,i)=>stamp('wall_med_bay_'+name,i*128,0)),stamp('player_scale_reference',776,208));
sheet('med_bay_walls_v2',states.map(n=>'wall_med_bay_'+n),{cols:4,pad:0});
