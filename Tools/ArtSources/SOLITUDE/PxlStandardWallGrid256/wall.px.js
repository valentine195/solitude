// Existing SOLITUDE panel vocabulary aligned to the supplied reference at native 256x256.
// Geometry is authored directly on the native grid; the 1254px JPEG is never resized into art.
pal({k:'#26353d',d:'#3b505b',s:'#5a6e79',g:'#84959e',m:'#a8b3b8',
 t:'#c1c9cc',b:'#d8ddde',h:'#e5e9e8',O:'#eef1ef',i:'#f7f8f4',
 u:'#416e7c',v:'#61a7b8',w:'#83bdc8',q:'#a8d1d6'});
ramps('kdsgmtbhOi','duvwq');
function material(c){c.rect(0,0,256,256,'O');}
function structure(c){
 // Off-white outer strips and thin dark inset breaks, not bolted corner frames.
 for(const x of [0,238]){
  c.rect(x,0,18,256,'h');c.vline(x+1,0,256,'i');c.vline(x+17,0,256,'t');
 }
 c.vline(0,0,256,'t');
 // Recessed vertical panel breaks extend to the upper channel; 1px depth steps.
 c.rect(18,0,5,256,'t');c.vline(18,0,256,'b');c.vline(20,0,256,'s');
 c.vline(21,0,256,'k');c.vline(22,0,256,'g');
 c.rect(233,0,5,256,'t');c.vline(233,0,256,'g');c.vline(234,0,256,'k');
 c.vline(235,0,256,'s');c.vline(237,0,256,'b');
 // Paired shallow service-channel interruptions retained from the supplied image.
 for(const x of [18,233]){c.rect(x,34,5,19,'k');c.rect(x+1,36,3,15,'d');c.vline(x+1,36,15,'s');}
 // Lower plinth is separate cladding, with a dark top joint and bright return.
 c.rect(34,240,188,16,'t');c.rect(35,242,186,12,'b');
 c.hline(35,241,186,'i');c.hline(35,254,186,'g');c.hline(35,255,186,'s');
}
function upperRecess(c){
 // Straight, inset channel and its upper painted cover, as in the latest reference.
 c.rect(27,0,202,20,'k');c.rect(28,1,200,17,'d');
 c.rect(29,9,198,9,'k');c.rect(32,11,192,6,'d');
 c.rect(38,12,180,5,'u');c.rect(58,12,140,5,'v');
 c.hline(58,12,140,'w');c.hline(59,16,138,'u');
 c.rect(30,1,196,7,'m');c.rect(42,1,172,5,'h');c.hline(42,1,172,'i');
 c.hline(30,6,196,'g');c.hline(30,8,196,'k');
 c.vline(35,10,8,'k');c.vline(220,10,8,'k');
 c.hline(28,19,200,'g');c.hline(28,20,200,'i');c.hline(30,21,196,'h');
}
function panelLines(c){
 // Small chamfers belong to the panel outline, never separate corner fittings.
 c.vline(27,22,118,'t');c.vline(28,22,118,'i');
 c.vline(227,22,118,'g');c.vline(226,22,118,'h');
 c.line(28,140,34,146,'g');c.line(29,140,35,145,'t');
 c.line(227,140,221,146,'g');c.line(226,140,220,145,'t');
 c.hline(35,145,185,'h');c.hline(34,146,188,'g');c.hline(34,147,188,'b');
 // Lower panel has the same straight vertical breaks with a deeper lower return.
 c.hline(28,167,200,'i');c.vline(27,168,59,'t');c.vline(28,168,59,'i');
 c.vline(227,168,59,'g');c.vline(226,168,59,'h');
 c.line(27,227,35,235,'g');c.line(28,227,36,234,'t');
 c.line(227,227,219,235,'g');c.line(226,227,218,234,'t');
 c.hline(36,234,182,'t');c.hline(35,235,185,'g');
 c.line(22,229,33,240,'k');c.line(23,229,34,239,'b');
 c.line(233,229,222,240,'k');c.line(232,229,221,239,'b');
 c.hline(34,239,188,'k');c.hline(35,240,186,'t');
}
function stripe(c){
 // Single consistent teal face, exact full-width light and shadow rows.
 c.rect(0,149,256,16,'v');c.hline(0,149,256,'u');
 c.hline(0,150,256,'q');c.hline(0,151,256,'w');
 c.hline(0,163,256,'u');c.hline(0,164,256,'d');c.hline(0,165,256,'t');
}
sprite('standard_wall_256',{w:256,h:256,layers:true},
 layer('panel_material',material),layer('panel_breaks',structure),layer('upper_recess',upperRecess),
 layer('panel_outlines',panelLines),layer('teal_stripe',stripe));
scene('three_panels',{w:768,h:256},tile('standard_wall_256',0,0,3,1));
scene('grid_3x3',{w:768,h:768},tile('standard_wall_256',0,0,3,3));
