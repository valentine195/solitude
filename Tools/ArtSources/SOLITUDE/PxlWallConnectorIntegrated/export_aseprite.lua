local root=app.params['root']
local sprite=Sprite(128,256,ColorMode.RGB)
for i,name in ipairs({'wall_infill','mounting_recess','connector_spine','end_blocks','amber_emitter'}) do
 local layer=i==1 and sprite.layers[1] or sprite:newLayer();layer.name=name
 sprite:newCel(layer,1,Image{fromFile=root..'/Editable/LayerInputs/'..name..'.png'},Point(0,0))
end
sprite:saveAs(root..'/Editable/wall_connector_integrated.aseprite')
sprite:close();sprite=Sprite{fromFile=root..'/Editable/wall_connector_integrated.aseprite'}
local image=Image(128,256,ColorMode.RGB);image:drawSprite(sprite,1)
image:saveAs(root..'/Editable/roundtrip.png')
