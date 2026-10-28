// Native 128x128 adaptation of the original 256px geometry, not an enlargement of 32px art.
pal({O:'#eef1ef',m:'#a8b3b8'});
function surface(c){c.rect(0,0,128,128,'O');}
function bevel(c){
 // Crisp 1px outline, 1px inset and matching 3px clipped corners.
 const points=[[4,1],[123,1],[126,4],[126,123],[123,126],[4,126],[1,123],[1,4]];
 for(let i=0;i<points.length;i++)c.line(...points[i],...points[(i+1)%points.length],'m');
}
sprite('quiet_panel_128',{w:128,h:128,layers:true},layer('surface',surface),layer('panel_outline',bevel));
scene('quiet_panel_repeat_3x3',{w:384,h:384},tile('quiet_panel_128',0,0,3,3));
