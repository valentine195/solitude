local root=app.params['root']
local sprite=Sprite(128,128,ColorMode.RGB)
for i,name in ipairs({'surface','panel_outline'}) do
 local layer=i==1 and sprite.layers[1] or sprite:newLayer();layer.name=name
 sprite:newCel(layer,1,Image{fromFile=root..'/Editable/LayerInputs/'..name..'.png'},Point(0,0))
end
sprite:saveAs(root..'/Editable/quiet_panel_128.aseprite')
sprite:close();sprite=Sprite{fromFile=root..'/Editable/quiet_panel_128.aseprite'}
local image=Image(128,128,ColorMode.RGB);image:drawSprite(sprite,1)
image:saveAs(root..'/Editable/roundtrip.png')
