local root=app.params['root']
local sprite=Sprite(32,32,ColorMode.RGB)
for i,name in ipairs({'surface','panel_outline'}) do
 local layer=i==1 and sprite.layers[1] or sprite:newLayer();layer.name=name
 sprite:newCel(layer,1,Image{fromFile=root..'/Editable/LayerInputs/'..name..'.png'},Point(0,0))
end
sprite:saveAs(root..'/Editable/quiet_panel_32.aseprite')
sprite:close();sprite=Sprite{fromFile=root..'/Editable/quiet_panel_32.aseprite'}
local image=Image(32,32,ColorMode.RGB);image:drawSprite(sprite,1)
image:saveAs(root..'/Editable/roundtrip.png')
