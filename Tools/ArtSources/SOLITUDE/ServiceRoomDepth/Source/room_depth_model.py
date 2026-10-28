import bpy
root='/Users/jeremyvalentine/SOLITUDE/Tools/ArtSources/SOLITUDE/ServiceRoomDepth'
rects=[(32, 62, 448, 2, '667583'), (32, 64, 448, 2, '7E8C99'), (32, 66, 448, 1, '87939F'), (32, 64, 2, 256, '6C7B89'), (34, 66, 1, 254, '7E8C99'), (479, 64, 1, 256, '6C7B89'), (478, 66, 1, 254, '7E8C99'), (32, 319, 192, 1, '6C7B89'), (288, 319, 192, 1, '6C7B89'), (32, 318, 192, 1, '7E8C99'), (288, 318, 192, 1, '7E8C99')]

s=bpy.data.scenes['SOLITUDE Service Room - Refined Steel'].copy();s.name='SOLITUDE Service Room - Depth Pass';bpy.context.window.scene=s
materials={}
for old in list(s.objects):
 ob=old.copy()
 if old.data is not None:ob.data=old.data.copy()
 s.collection.objects.link(ob);s.collection.objects.unlink(old)
 if ob.type=='CAMERA':s.camera=ob
 if ob.type!='MESH':continue
 oldpath=ob.get('asset_path','')
 path=root+'/Room/Assets/'+oldpath.split('/Assets/')[-1]
 if 'north_wall_plain_64.png' in path and ob.get('top_left_pixel_x') in (160,416):path=root+'/Room/Assets/Wall/north_wall_wide_64.png'
 ob['asset_path']=path
 if path not in materials:
  im=bpy.data.images.load(path,check_existing=True);im.pack()
  m=bpy.data.materials.new('Room depth '+path.split('/')[-1]);m.use_nodes=True
  ns=m.node_tree.nodes;ns.clear();t=ns.new('ShaderNodeTexImage');t.image=im
  t.interpolation=next(i.identifier for i in t.bl_rna.properties['interpolation'].enum_items if i.identifier.lower()=='closest')
  em=ns.new('ShaderNodeEmission');tr=ns.new('ShaderNodeBsdfTransparent');mix=ns.new('ShaderNodeMixShader');out=ns.new('ShaderNodeOutputMaterial');ln=m.node_tree.links
  ln.new(t.outputs['Color'],em.inputs['Color']);ln.new(t.outputs['Alpha'],mix.inputs[0]);ln.new(tr.outputs[0],mix.inputs[1]);ln.new(em.outputs[0],mix.inputs[2]);ln.new(mix.outputs[0],out.inputs['Surface']);materials[path]=m
 ob.data.materials.clear();ob.data.materials.append(materials[path])
shadow_objects=[]
for i,(x,y,w,h,color) in enumerate(rects):
 key='Contact '+color
 if key not in materials:
  m=bpy.data.materials.new(key);m.use_nodes=True;ns=m.node_tree.nodes;ns.clear();em=ns.new('ShaderNodeEmission');out=ns.new('ShaderNodeOutputMaterial')
  rgb=[int(color[j:j+2],16)/255 for j in (0,2,4)]
  em.inputs[0].default_value=tuple(v/12.92 if v<=0.04045 else ((v+0.055)/1.055)**2.4 for v in rgb)+(1,)
  m.node_tree.links.new(em.outputs[0],out.inputs['Surface']);materials[key]=m
 z=0.1+i*0.0001
 mesh=bpy.data.meshes.new('Contact shadow '+str(i));mesh.from_pydata([(x,352-y-h,z),(x+w,352-y-h,z),(x+w,352-y,z),(x,352-y,z)],[],[(0,1,2,3)]);mesh.update()
 ob=bpy.data.objects.new('Contact shadow '+str(i),mesh);s.collection.objects.link(ob);mesh.materials.append(materials[key]);shadow_objects.append(ob)
s.render.filter_size=0.01;s.eevee.taa_render_samples=1
others=[o for o in s.objects if o.type=='MESH' and o not in shadow_objects]
for o in others:o.hide_render=True
s.render.filepath=root+'/Room/PNG/contact_shadows_layer.png';bpy.ops.render.render(write_still=True)
for o in others:o.hide_render=False
s.render.filepath=root+'/Room/PNG/service_room_blender.png';bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=root+'/Model/service_room_depth.blend')
print('Depth room and separate contact layer saved')
