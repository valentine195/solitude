from process_review import *
f=R/'Final';data=json.loads((f/'Room/room_layout.json').read_text())
lookup={(c['cell'][0]*32,c['cell'][1]*32):str(f/'Room/Assets/Floor'/f"{c['tile']}.png") for c in data['floor_cells']}
code='import bpy\n'+f'floor_paths={lookup!r}\nroot={str(f)!r}\n'+'''
original=bpy.data.scenes.get('SOLITUDE Service Room - Light Steel Floor')
scene=original.copy()
scene.name='SOLITUDE Service Room - Refined Steel'
bpy.context.window.scene=scene
materials={}
for original_obj in list(scene.objects):
    obj=original_obj.copy()
    if original_obj.data is not None:obj.data=original_obj.data.copy()
    scene.collection.objects.link(obj)
    for col in list(original_obj.users_collection):
        if col==scene.collection:col.objects.unlink(original_obj)
    if obj.type=='CAMERA':
        scene.camera=obj
    if obj.type!='MESH':continue
    oldpath=obj.get('asset_path','')
    pos=(obj.get('top_left_pixel_x'),obj.get('top_left_pixel_y'))
    if '/Floor/' in oldpath:
        path=floor_paths[pos]
    else:
        path=root+'/Room/Assets/'+oldpath.split('/Assets/')[-1]
    obj['asset_path']=path
    if path not in materials:
        im=bpy.data.images.load(path,check_existing=True)
        im.pack()
        mat=bpy.data.materials.new('Refined steel '+path.split('/')[-1])
        mat.use_nodes=True
        nodes=mat.node_tree.nodes;nodes.clear()
        tex=nodes.new('ShaderNodeTexImage');tex.image=im
        tex.interpolation=next(i.identifier for i in tex.bl_rna.properties['interpolation'].enum_items if i.identifier.lower()=='closest')
        emission=nodes.new('ShaderNodeEmission')
        transparent=nodes.new('ShaderNodeBsdfTransparent')
        mix=nodes.new('ShaderNodeMixShader');out=nodes.new('ShaderNodeOutputMaterial')
        links=mat.node_tree.links
        links.new(tex.outputs['Color'],emission.inputs['Color'])
        links.new(tex.outputs['Alpha'],mix.inputs[0])
        links.new(transparent.outputs[0],mix.inputs[1])
        links.new(emission.outputs[0],mix.inputs[2])
        links.new(mix.outputs[0],out.inputs['Surface'])
        materials[path]=mat
    obj.data.materials.clear();obj.data.materials.append(materials[path])
scene.render.filepath=root+'/Room/PNG/service_room_blender.png'
bpy.ops.wm.save_as_mainfile(filepath=root+'/Model/service_room_refined_steel.blend')
bpy.ops.render.render(write_still=True)
print('Saved refined steel scene, packed materials: '+str(len(materials)))
'''
(R/'Source/update_blender.py').write_text(code)
