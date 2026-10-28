// 32px adaptation: retain a full native pixel on the bevel instead of losing it to downsampling.
pal({O:'#eef1ef',m:'#a8b3b8'});
function surface(c){c.rect(0,0,32,32,'O');}
function bevel(c){
 const points=[[2,0],[29,0],[31,2],[31,29],[29,31],[2,31],[0,29],[0,2]];
 for(let i=0;i<points.length;i++)c.line(...points[i],...points[(i+1)%points.length],'m');
}
sprite('quiet_panel_32',{w:32,h:32,layers:true},layer('surface',surface),layer('panel_outline',bevel));
scene('quiet_panel_repeat_3x3',{w:96,h:96},tile('quiet_panel_32',0,0,3,3));
