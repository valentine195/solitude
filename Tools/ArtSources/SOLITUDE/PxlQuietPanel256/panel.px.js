// Native cleanup of the supplied quiet panel. No resizing or added decoration.
// Both colors are reused from the existing standard wall palette.
pal({O:'#eef1ef',m:'#a8b3b8'});
function surface(c){c.rect(0,0,256,256,'O');}
function outline(c){
 // 2px inset, matching 6px corner cuts, exactly 1 native pixel throughout.
 const points=[[8,2],[247,2],[253,8],[253,247],[247,253],[8,253],[2,247],[2,8]];
 for(let i=0;i<points.length;i++)c.line(...points[i],...points[(i+1)%points.length],'m');
}
sprite('quiet_panel_256',{w:256,h:256,layers:true},layer('surface',surface),layer('panel_outline',outline));
scene('quiet_panel_repeat_3x3',{w:768,h:768},tile('quiet_panel_256',0,0,3,3));
