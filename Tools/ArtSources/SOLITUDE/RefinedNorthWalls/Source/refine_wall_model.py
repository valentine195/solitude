import bpy
root='/Users/jeremyvalentine/SOLITUDE/Tools/ArtSources/SOLITUDE/RefinedNorthWalls'
original=bpy.data.scenes['SOLITUDE North Wall Panels']
s=original.copy();s.name='SOLITUDE Refined North Walls';bpy.context.window.scene=s
colors={'dark':'202C39','graphite':'303C49','shadow':'465767','alloy':'91A0AA','blue':'687F92','ivoryshadow':'9BA6AE','ivory':'C9CCCA','light':'E0E3DF','amber':'CF9B43'}
mats={}
for key,h in colors.items():
 m=bpy.data.materials['Wall '+key].copy();m.name='Refined Wall '+key
 rgb=[int(h[i:i+2],16)/255 for i in (0,2,4)]
 linear=[v/12.92 if v<=0.04045 else ((v+0.055)/1.055)**2.4 for v in rgb]
 node=next(n for n in m.node_tree.nodes if n.type=='EMISSION');node.inputs[0].default_value=(*linear,1)
 mats[key]=m
objects={}
for old in list(s.objects):
 ob=old.copy()
 if old.data is not None:ob.data=old.data.copy()
 s.collection.objects.link(ob);s.collection.objects.unlink(old)
 objects[old.name]=ob
 if ob.type=='CAMERA':s.camera=ob
 if ob.type=='MESH':
  key=old.data.materials[0].name.replace('Wall ','')
  ob.data.materials.clear();ob.data.materials.append(mats[key])
  if 'joint highlight' in old.name:
   ob.data.materials.clear();ob.data.materials.append(mats['light'])
  if 'fastener' in old.name or 'hatch bolt' in old.name:
   ob.data.materials.clear();ob.data.materials.append(mats['ivoryshadow'])
  if 'small wear' in old.name:ob.hide_render=True
# Native-pixel-sized relief planes; camera projects one Blender unit per pixel.
def plate(name,x,y,w,h,z,material):
 mesh=bpy.data.meshes.new(name)
 mesh.from_pydata([(x,y,z),(x+w,y,z),(x+w,y+h,z),(x,y+h,z)],[],[(0,1,2,3)])
 mesh.update();ob=bpy.data.objects.new(name,mesh);s.collection.objects.link(ob);mesh.materials.append(mats[material]);return ob
for kind,ox in [('plain',0),('service',80),('vent',160),('rib',240)]:
 plate(kind+' face upper glint',ox+7,40,55,1,2.9,'light')
 plate(kind+' face lower recess',ox+7,10,55,1,2.9,'ivoryshadow')
 for yy in [13,39]:
  plate(kind+' right recessed screw',ox+60,yy,1,1,4.2,'ivoryshadow')
  plate(kind+' right screw glint',ox+60,yy-1,1,1,4.3,'light')
  plate(kind+' left screw glint',ox+9,yy-1,1,1,4.3,'light')
plate('plain central seam glint',35,11,1,29,3.5,'light')
plate('service hatch upper glint',95,36,38,1,5.5,'light')
plate('service hatch left glint',95,17,1,20,5.5,'light')
plate('service hatch lower seam',96,16,37,1,5.5,'ivoryshadow')
plate('vent frame upper glint',175,36,39,1,3.5,'light')
plate('vent frame left glint',175,18,1,18,3.5,'light')
plate('vent frame lower recess',176,18,38,1,3.5,'shadow')
s.render.filter_size=0.01;s.eevee.taa_render_samples=1;s.render.resolution_percentage=100
for name,ox in [('plain',0),('service',80),('vent',160),('rib',240)]:
 s.camera.location.x=ox+32
 for size in [64,256]:
  s.render.resolution_x=size;s.render.resolution_y=size
  s.render.filepath=root+'/PNG/north_wall_'+name+'_'+str(size)+'.png'
  bpy.ops.render.render(write_still=True)
s.camera.location.x=32;s.render.resolution_x=64;s.render.resolution_y=64
bpy.ops.wm.save_as_mainfile(filepath=root+'/Model/refined_north_walls.blend')
print('Refined geometry saved; four native64 and256px renders complete.')
