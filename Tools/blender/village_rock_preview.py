import bpy, os, math
bpy.ops.wm.read_factory_settings(use_empty=True)
SRC = r"C:\dev\game\Assets\Resources\CoastRun\Models"
names = ["VRock_A","VRock_B","VRock_C","VStone_A","VStone_B","VCliff_A","VCliff_B","VBush_A","VBush_B"]
cols = {"Basalt":(0.36,0.38,0.42),"Moss":(0.42,0.66,0.30),"Ore":(1.0,0.82,0.25),"LeafBush":(0.35,0.62,0.30),"Berry":(0.98,0.42,0.55)}
for i,n in enumerate(names):
    bpy.ops.import_scene.fbx(filepath=os.path.join(SRC,n+".fbx"))
    for o in bpy.context.selected_objects:
        o.location = ((i%5)*2.0-4.0, -(i//5)*2.2, 0)
        for m in o.data.materials:
            if m is None: continue
            m.use_nodes=True; b=m.node_tree.nodes.get("Principled BSDF")
            key=[k for k in cols if m.name.startswith(k)]
            if b and key:
                b.inputs["Base Color"].default_value=(*cols[key[0]],1); b.inputs["Roughness"].default_value=0.9
                # vertex color multiply
                nt=m.node_tree; vc=nt.nodes.new("ShaderNodeVertexColor"); vc.layer_name="Col"
                mix=nt.nodes.new("ShaderNodeMix"); mix.data_type='RGBA'; mix.blend_type='MULTIPLY'; mix.inputs[0].default_value=1.0
                mix.inputs[6].default_value=(*cols[key[0]],1); nt.links.new(vc.outputs[0], mix.inputs[7]); nt.links.new(mix.outputs[2], b.inputs["Base Color"])
sc=bpy.context.scene; sc.render.engine='BLENDER_EEVEE' if hasattr(bpy.types,'SCENE_OT_render') else 'BLENDER_EEVEE'
try: sc.render.engine='BLENDER_EEVEE_NEXT'
except Exception: pass
cam=bpy.data.objects.new("Cam", bpy.data.cameras.new("C")); sc.collection.objects.link(cam); sc.camera=cam
cam.location=(0,-9,6); cam.rotation_euler=(math.radians(55),0,0)
sun=bpy.data.objects.new("Sun", bpy.data.lights.new("S",'SUN')); sc.collection.objects.link(sun); sun.rotation_euler=(math.radians(50),math.radians(20),math.radians(30)); sun.data.energy=3
w=bpy.data.worlds.new("W"); sc.world=w; w.use_nodes=True; w.node_tree.nodes["Background"].inputs[0].default_value=(0.75,0.85,0.95,1); w.node_tree.nodes["Background"].inputs[1].default_value=1.0
sc.render.resolution_x=1200; sc.render.resolution_y=640; sc.render.filepath=r"C:\dev\game\Tools\_shots\vrock_kit.png"
bpy.ops.render.render(write_still=True); print("rendered")
