// SOLITUDE floor panel, authored at native 256x256 from the supplied prototype.
// All colors are reused from the approved standard wall; no image resampling or noise.
pal({s:'#5a6e79',g:'#84959e',m:'#a8b3b8',t:'#c1c9cc',b:'#d8ddde',h:'#e5e9e8',O:'#eef1ef',i:'#f7f8f4'});
ramps('sgmtbhOi');
// Shared symmetric geometry. Shading is directional, not mirrored illumination.
function oct(inset,cut){const r=256-inset;return [inset+cut,inset,r-cut,inset,r,inset+cut,r,r-cut,r-cut,r,inset+cut,r,inset,r-cut,inset,inset+cut];}
function surface(c){c.rect(0,0,256,256,'O');}
function perimeter(c){
 // Broad, shallow framing strips and four clipped corner blocks from the reference.
 c.rect(0,0,256,256,'h');
 c.poly(oct(12,14),'g');c.poly(oct(13,14),'t');c.poly(oct(14,14),'h');
 // Top-left exposed frame edges catch the light. Lower/right faces remain restrained.
 c.hline(27,1,202,'i');c.hline(27,2,202,'O');
 c.vline(1,27,202,'i');c.vline(2,27,202,'O');
 c.hline(27,253,202,'b');c.hline(27,254,202,'t');
 c.vline(253,27,202,'b');c.vline(254,27,202,'t');
 // Existing diagonal junctions only; no fasteners, greebles or decorative texture.
 c.line(26,1,26,11,'t');c.line(26,12,12,26,'g');c.line(11,26,1,26,'t');
 c.line(229,1,229,11,'t');c.line(229,12,243,26,'g');c.line(244,26,254,26,'t');
 c.line(1,229,11,229,'t');c.line(12,229,26,243,'g');c.line(26,244,26,254,'t');
 c.line(254,229,244,229,'t');c.line(243,229,229,243,'g');c.line(229,244,229,254,'t');
 // The corner blocks remain plain manufactured surfaces.
 c.hline(2,1,23,'i');c.vline(1,2,23,'i');
 c.hline(231,1,23,'i');c.vline(1,231,23,'i');
}
function inset(c){
 // Restrained inset boundary: narrow top/left contact shadow, lit lower/right return.
 c.poly(oct(20,16),'m');c.poly(oct(21,16),'t');c.poly(oct(22,16),'O');
 c.hline(36,20,184,'g');c.vline(20,36,184,'g');
 c.line(20,36,36,20,'m');c.line(220,20,235,35,'m');
 c.hline(37,21,182,'b');c.vline(21,37,182,'b');
 c.hline(36,235,184,'i');c.vline(235,36,184,'i');
 c.line(20,220,36,235,'t');c.line(220,235,235,220,'t');
 // Exact perimeter pixels ensure horizontal AND vertical joins are deterministic.
 // One quiet border row/column; depth lives immediately inside it.
 c.box(0,0,256,256,'t');
}
sprite('floor_standard_256',{w:256,h:256,layers:true},layer('surface',surface),layer('perimeter_frame',perimeter),layer('inset_panel',inset));
scene('floor_repeat_3x3',{w:768,h:768},tile('floor_standard_256',0,0,3,3));
scene('floor_horizontal_3',{w:768,h:256},tile('floor_standard_256',0,0,3,1));
