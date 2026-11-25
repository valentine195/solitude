local root = app.params['root']
local sprite = Sprite(128,256,ColorMode.RGB)
sprite.layers[1].name = 'Structure'
local structure = sprite.layers[1]
local upper = sprite:newLayer(); upper.name = 'UpperPanel'
local lower = sprite:newLayer(); lower.name = 'LowerPanel'
local wear = sprite:newLayer(); wear.name = 'WearOverlay'
local damage = sprite:newLayer(); damage.name = 'DamageOverlay'
sprite:newEmptyFrame(); sprite:newEmptyFrame()
local function put(layer,frame,path)
 sprite:newCel(layer,frame,Image{fromFile=root..'/Editable/LayerInputs/'..path..'.png'},Point(0,0))
end
for frame=1,3 do
 put(structure,frame,'wall_pristine/structure')
 put(upper,frame,'wall_pristine/upper_panel')
 put(lower,frame,'wall_pristine/lower_panel')
 if frame>=2 then put(wear,frame,'overlay_lightly_worn') end
 if frame==3 then put(damage,frame,'overlay_heavy_damage') end
 sprite.frames[frame].duration=1
end
for i,name in ipairs({'Pristine','LightlyWorn','HeavilyDamaged'}) do
 local tag=sprite:newTag(i,i);tag.name=name
end
sprite:saveAs(root..'/Editable/wall_panel_variants.aseprite')
-- Reopen the saved document before independent native rendering.
sprite:close()
sprite=Sprite{fromFile=root..'/Editable/wall_panel_variants.aseprite'}
for i,name in ipairs({'pristine','lightly_worn','heavily_damaged'}) do
 local img=Image(128,256,ColorMode.RGB);img:drawSprite(sprite,i)
 img:saveAs(root..'/Editable/roundtrip_'..name..'.png')
end
print('Exported 128x256, 3 frames, 5 named layers, 3 state tags')
