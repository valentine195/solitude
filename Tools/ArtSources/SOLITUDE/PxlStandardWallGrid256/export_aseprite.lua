local root=app.params['root']
local sprite=Sprite(256,256,ColorMode.RGB)
for i,name in ipairs({'panel_material','panel_breaks','upper_recess','panel_outlines','teal_stripe'}) do
 local layer=i==1 and sprite.layers[1] or sprite:newLayer();layer.name=name
 sprite:newCel(layer,1,Image{fromFile=root..'/Editable/LayerInputs/'..name..'.png'},Point(0,0))
end
sprite:saveAs(root..'/Editable/standard_wall_256.aseprite')
sprite:close();sprite=Sprite{fromFile=root..'/Editable/standard_wall_256.aseprite'}
local image=Image(256,256,ColorMode.RGB);image:drawSprite(sprite,1)
image:saveAs(root..'/Editable/roundtrip.png')
