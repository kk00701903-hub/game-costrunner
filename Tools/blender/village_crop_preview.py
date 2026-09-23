# 169차: 작물 키트 미리보기 시트 — 가로 = 성장 2~5단계, 세로 = 작물 5종.
import bpy, os, math
bpy.ops.wm.read_factory_settings(use_empty=True)
SRC = r"C:\dev\game\Assets\Resources\CoastRun\Models"
kinds = ["Tomato", "Potato", "Rice", "Rose", "Lavender"]
cols = {"CropStem":(0.35,0.60,0.26),"CropLeaf":(0.42,0.72,0.32),"CropLeafGray":(0.55,0.68,0.50),
        "CropStake":(0.62,0.46,0.28),"CropSoil":(0.44,0.31,0.20),"CropTomato":(0.93,0.22,0.18),
        "CropPotato":(0.80,0.64,0.38),"CropRice":(0.92,0.80,0.32),"CropRose":(0.97,0.42,0.60),
        "CropLavender":(0.70,0.52,0.92),"CropBloomWhite":(0.97,0.96,0.92),"CropBloomYellow":(1.0,0.86,0.25)}
for r, k in enumerate(kinds):
    for c, st in enumerate((2, 3, 4, 5)):
        bpy.ops.import_scene.fbx(filepath=os.path.join(SRC, "VCrop_%s_%d.fbx" % (k, st)))
        for o in bpy.context.selected_objects:
            o.location = (c * 1.05 - 1.6, -(r * 1.15) + 2.3, 0)
            for m in o.data.materials:
                if m is None: continue
                m.use_nodes = True; b = m.node_tree.nodes.get("Principled BSDF")
                key = [x for x in cols if m.name.startswith(x)]
                if b and key:
                    b.inputs["Base Color"].default_value = (*cols[key[0]], 1)
                    b.inputs["Roughness"].default_value = 0.85
# 바닥(흙)
bpy.ops.mesh.primitive_plane_add(size=12, location=(0, 0, -0.005))
gm = bpy.data.materials.new("G"); gm.use_nodes = True
gm.node_tree.nodes.get("Principled BSDF").inputs["Base Color"].default_value = (0.50, 0.37, 0.25, 1)
bpy.context.active_object.data.materials.append(gm)
sc = bpy.context.scene
try: sc.render.engine = 'BLENDER_EEVEE_NEXT'
except Exception: sc.render.engine = 'BLENDER_EEVEE'
cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("C")); sc.collection.objects.link(cam); sc.camera = cam
cam.location = (0, -6.0, 4.2); cam.rotation_euler = (math.radians(56), 0, 0)
cam.data.type = "ORTHO"; cam.data.ortho_scale = 6.4
sun = bpy.data.objects.new("Sun", bpy.data.lights.new("S", 'SUN')); sc.collection.objects.link(sun)
sun.rotation_euler = (math.radians(48), math.radians(18), math.radians(25)); sun.data.energy = 3.2
w = bpy.data.worlds.new("W"); sc.world = w; w.use_nodes = True
w.node_tree.nodes["Background"].inputs[0].default_value = (0.82, 0.88, 0.95, 1)
w.node_tree.nodes["Background"].inputs[1].default_value = 0.45
sc.render.resolution_x = 1280; sc.render.resolution_y = 1000
sc.render.filepath = r"C:\dev\game\Tools\_shots\vcrop_kit.png"
bpy.ops.render.render(write_still=True); print("rendered")
