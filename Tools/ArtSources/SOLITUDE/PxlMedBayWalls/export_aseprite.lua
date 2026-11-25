local root=app.params['root']
local sprite=Sprite(128,256,ColorMode.RGB)
local wall=sprite.layers[1];wall.name='WallFace'
local fixtures=sprite:newLayer();fixtures.name='MedicalFixtures'
sprite:newEmptyFrame();sprite:newEmptyFrame()
for i,name in ipairs({'plain','utility','supply'}) do
 for _,entry in ipairs({{wall,'wall_med_bay_'..name..'/wall_face'},{fixtures,'overlay_med_bay_'..name}}) do
  sprite:newCel(entry[1],i,Image{fromFile=root..'/Editable/LayerInputs/'..entry[2]..'.png'},Point(0,0))
 end
 local tag=sprite:newTag(i,i);tag.name=name
 sprite.frames[i].duration=1
end
sprite:saveAs(root..'/Editable/med_bay_walls.aseprite')
sprite:close();sprite=Sprite{fromFile=root..'/Editable/med_bay_walls.aseprite'}
for i,name in ipairs({'plain','utility','supply'}) do
 local image=Image(128,256,ColorMode.RGB);image:drawSprite(sprite,i)
 image:saveAs(root..'/Editable/roundtrip_'..name..'.png')
end
