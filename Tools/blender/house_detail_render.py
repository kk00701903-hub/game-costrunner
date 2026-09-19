
import bpy, sys, math
sys.argv = [sys.argv[0], "--", "--out", r"C:\dev\game\Tools\blender\cutify_out\House_Detail.fbx", "--wall", "#DDF3EA", "--roof", "#F2A7A0", "--brick", "0"]
bpy.ops.wm.read_factory_settings(use_empty=True)
exec(open(r"C:\dev\game\Tools\blender\house_detail.py", encoding="utf-8").read())
from mathutils import Vector
col = bpy.data.collections["House_Detail"]
mn = Vector((1e9,)*3); mx = Vector((-1e9,)*3)
for o in col.objects:
    for c in o.bound_box:
        wv = o.matrix_world @ Vector(c); mn = Vector(map(min, mn, wv)); mx = Vector(map(max, mx, wv))
ctr = (mn + mx) / 2; size = max(mx - mn)
cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam")); bpy.context.scene.collection.objects.link(cam)
cam.location = ctr + Vector((size * 1.1, size * 1.4, size * 0.38)); d = ctr - cam.location; cam.rotation_euler = d.to_track_quat('-Z', 'Y').to_euler()
bpy.context.scene.camera = cam
sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", "SUN")); bpy.context.scene.collection.objects.link(sun); sun.data.energy = 2.4; sun.rotation_euler = (math.radians(45), 0, math.radians(150))
w = bpy.data.worlds.new("W"); bpy.context.scene.world = w; w.use_nodes = True; w.node_tree.nodes["Background"].inputs[0].default_value = (0.78, 0.88, 1, 1); w.node_tree.nodes["Background"].inputs[1].default_value = 0.5
sc = bpy.context.scene; sc.render.engine = "BLENDER_EEVEE"; sc.view_settings.view_transform = "Standard"; sc.view_settings.look = "None"; sc.render.resolution_x = 900; sc.render.resolution_y = 640
sc.render.filepath = r"C:\dev\game\Tools\blender\cutify_out\House_Detail.png"; sc.render.image_settings.file_format = "PNG"
bpy.ops.render.render(write_still=True); print("rendered")
