
import bpy, sys, math
inp, out_png, soft = sys.argv[sys.argv.index("--")+1:][:3]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=inp)
objs=[o for o in bpy.context.scene.objects if o.type=="MESH"]
if soft=="1":
    sys.argv=[sys.argv[0]]
    exec(open(r"C:\dev\game\Tools\blender\cutify_assets.py",encoding="utf-8").read())
# frame objects
from mathutils import Vector
mn=Vector((1e9,)*3); mx=Vector((-1e9,)*3)
for o in objs:
    for c in o.bound_box:
        w=o.matrix_world @ Vector(c); mn=Vector(map(min,mn,w)); mx=Vector(map(max,mx,w))
ctr=(mn+mx)/2; size=max(mx-mn)
cam=bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam")); bpy.context.scene.collection.objects.link(cam)
cam.location=ctr+Vector((size*1.4, -size*1.6, size*1.1)); d=ctr-cam.location; cam.rotation_euler=d.to_track_quat('-Z','Y').to_euler()
bpy.context.scene.camera=cam
sun=bpy.data.objects.new("Sun", bpy.data.lights.new("Sun","SUN")); bpy.context.scene.collection.objects.link(sun); sun.data.energy=3; sun.rotation_euler=(math.radians(50),0,math.radians(40))
w=bpy.context.scene.world or bpy.data.worlds.new("W"); bpy.context.scene.world=w; w.use_nodes=True; w.node_tree.nodes["Background"].inputs[0].default_value=(0.85,0.92,1,1); w.node_tree.nodes["Background"].inputs[1].default_value=1.0
sc=bpy.context.scene; sc.render.engine="BLENDER_EEVEE"
sc.render.resolution_x=640; sc.render.resolution_y=480; sc.render.filepath=out_png; sc.render.image_settings.file_format="PNG"
bpy.ops.render.render(write_still=True); print("rendered", out_png)
