local root=app.params['root']
local sprite=Sprite(128,256,ColorMode.RGB)
sprite.layers[1].name='WallFace'
local depth=sprite:newLayer();depth.name='ArchitecturalDepth'
local fixtures=sprite:newLayer();fixtures.name='Fixtures'
for i=2,4 do sprite:newEmptyFrame() end
for i,name in ipairs({'access_a','access_b','utility','supply'}) do
 for j,layer in ipairs(sprite.layers) do
  local part=({'wall_face','architectural_depth','fixtures'})[j]
  sprite:newCel(layer,i,Image{fromFile=root..'/Editable/LayerInputs/wall_med_bay_'..name..'/'..part..'.png'},Point(0,0))
 end
 sprite.frames[i].duration=1
 local tag=sprite:newTag(i,i);tag.name=name
end
sprite:saveAs(root..'/Editable/med_bay_walls_v2.aseprite')
sprite:close();sprite=Sprite{fromFile=root..'/Editable/med_bay_walls_v2.aseprite'}
for i,name in ipairs({'access_a','access_b','utility','supply'}) do
 local image=Image(128,256,ColorMode.RGB);image:drawSprite(sprite,i)
 image:saveAs(root..'/Editable/roundtrip_'..name..'.png')
end
