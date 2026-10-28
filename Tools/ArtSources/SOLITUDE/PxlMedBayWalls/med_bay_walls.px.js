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
sprite('_wall_face',{w:128,h:256},face);
// Plain panel: replaceable sanitary cladding and a small zone marker.
sprite('overlay_med_bay_plain',{w:128,h:256},c=>{
 c.rect(45,33,38,29,'m');c.rect(46,34,36,26,'l');c.hline(46,34,36,'h');
 c.rect(49,37,30,20,'u');medicalMark(c,57,40,2);
 // Two small physical identification bars, not essential tiny text.
 c.hline(52,67,24,'m');c.hline(56,70,16,'t');
});
// Bedside utility panel: recessed gas connections, call button and service access.
sprite('overlay_med_bay_utility',{w:128,h:256},c=>{
 c.rect(22,47,84,77,'s');c.rect(23,48,82,74,'m');c.rect(25,50,78,69,'l');
 c.hline(25,50,78,'i');c.vline(25,50,69,'h');c.hline(25,119,78,'g');
 // Outlet legend uses geometric identifiers; each port has a distinct keyed shape.
 c.rect(31,58,16,9,'u');c.rect(56,58,16,9,'e');c.rect(81,58,14,9,'g');
 c.hline(35,61,8,'a');c.vline(63,60,5,'w');c.hline(61,62,5,'w');c.hline(85,62,6,'l');
 for(const x of [39,64,88]){
  c.rect(x-8,75,17,19,'g');c.rect(x-7,76,15,16,'h');
  c.rect(x-5,78,11,12,'m');c.rect(x-3,80,7,8,'s');
  c.hline(x-5,78,10,'i');c.hline(x-3,88,7,'k');
 }
 c.rect(36,82,6,4,'k');c.rect(62,80,4,8,'e');c.rect(85,81,6,6,'k');
 c.rect(31,104,42,8,'t');c.hline(33,105,38,'h');
 c.rect(82,102,14,12,'s');c.rect(84,103,10,9,'u');c.rect(86,104,6,2,'a');
 for(const x of [27,98])for(const y of [53,113])screw(c,x,y);
 // Flush utility chase beneath the shared panel joint.
 c.rect(26,182,76,48,'m');c.rect(27,183,74,45,'l');c.hline(28,184,72,'h');
 c.rect(36,195,55,19,'g');
 for(let y=196;y<213;y+=4){c.hline(38,y,51,'s');c.hline(38,y+1,51,'t');}
 c.rect(89,219,5,3,'s');c.hline(89,219,5,'g');
});
// Medical supplies: inset twin access doors and a lower clean-pack drawer.
sprite('overlay_med_bay_supply',{w:128,h:256},c=>{
 c.rect(22,24,84,122,'s');c.rect(23,25,82,119,'g');c.hline(24,25,80,'m');
 for(const x of [25,65]){
  c.rect(x,28,38,112,'t');c.rect(x+1,29,35,108,'l');
  c.vline(x+1,29,108,'h');c.hline(x+1,137,35,'m');
 }
 // Shallow slate-blue label bands and simple inventory symbols.
 c.rect(29,37,29,19,'u');medicalMark(c,40,42,1);
 c.rect(69,37,28,19,'u');
 for(const x of [75,81,87]){c.rect(x,42,3,8,'a');c.hline(x,41,3,'h');}
 // Recessed opposing grips; light on upper rim, shadow inside.
 for(const x of [54,68]){c.rect(x,78,6,24,'m');c.hline(x,78,6,'h');c.rect(x+1,80,4,19,'s');c.vline(x+4,81,17,'g');}
 // Sealed drawer aligned to lower field; no floor skirt or baked cap.
 c.rect(24,177,80,53,'g');c.rect(25,178,78,49,'l');c.hline(25,178,78,'h');
 c.rect(43,189,42,9,'t');c.hline(44,189,40,'h');c.rect(46,192,36,4,'s');
 c.rect(33,211,18,7,'u');c.hline(36,214,12,'a');
 for(const x of [29,95])screw(c,x,221);
});
for(const name of ['plain','utility','supply']){
 sprite('wall_med_bay_'+name,{w:128,h:256,layers:true},layer('wall_face',stamp('_wall_face',0,0)),layer('medical_fixtures',stamp('overlay_med_bay_'+name,0,0)));
 scene('repeat_'+name,{w:384,h:256},tile('wall_med_bay_'+name,0,0,3,1));
 scene('grid_'+name,{w:384,h:768},tile('wall_med_bay_'+name,0,0,3,3));
}
scene('med_bay_wall_run',{w:768,h:256},...['plain','utility','plain','supply','plain','utility'].map((name,i)=>stamp('wall_med_bay_'+name,i*128,0)));
sheet('med_bay_walls',['wall_med_bay_plain','wall_med_bay_utility','wall_med_bay_supply'],{cols:3,pad:0});
