local root=app.params['root']
local sprite=Sprite(256,256,ColorMode.RGB)
for i,name in ipairs({'panel_material','structural_depth','upper_recess','blue_band','hardware'}) do
 local layer=i==1 and sprite.layers[1] or sprite:newLayer();layer.name=name
 sprite:newCel(layer,1,Image{fromFile=root..'/Editable/LayerInputs/'..name..'.png'},Point(0,0))
end
sprite:saveAs(root..'/Editable/standard_wall_panel_breaks.aseprite')
sprite:close();sprite=Sprite{fromFile=root..'/Editable/standard_wall_panel_breaks.aseprite'}
local image=Image(256,256,ColorMode.RGB);image:drawSprite(sprite,1)
image:saveAs(root..'/Editable/roundtrip.png')
