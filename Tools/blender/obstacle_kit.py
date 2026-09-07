# 14차-11: 장애물·차량 3D 키트 (low-poly) — 그림 빌보드 대신 진짜 두께가 있는 모델.
# Blender headless:  blender -b --python obstacle_kit.py
# Unity 축(FBX 스왑 후): Blender +X → Unity +X(도로 쪽), +Y → Unity +Z(진행), +Z → Unity +Y(위).
# 원점 = 바닥 중앙, 1 unit = 1 m. 재질 이름은 JejuKit.Build() 가 게임 머티리얼로 바꾼다.
import bpy, bmesh, math, os
from mathutils import Vector

EXPORT_DIR = r"C:\dev\game\Assets\Resources\CoastRun\Models"
os.makedirs(EXPORT_DIR, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

def mat(name, rgb):
    m = bpy.data.materials.get(name)
    if m is None:
        m = bpy.data.materials.new(name)
        m.use_nodes = True
        b = m.node_tree.nodes.get("Principled BSDF")
        if b:
            b.inputs["Base Color"].default_value = (*rgb, 1.0)
            b.inputs["Roughness"].default_value = 0.8
    return m

MATS = {n: mat(n, c) for n, c in {
    "SlimeBody": (0.95, 0.36, 0.30), "SlimeDark": (0.70, 0.18, 0.16), "Eye": (0.08, 0.06, 0.08), "EyeWhite": (1, 1, 1),
    "ConeOrange": (0.98, 0.45, 0.12), "ConeWhite": (0.97, 0.97, 0.95), "ConeBase": (0.12, 0.12, 0.14),
    "BarrierOrange": (0.98, 0.50, 0.15), "BarrierWhite": (0.97, 0.97, 0.95), "Metal": (0.45, 0.47, 0.50),
    "Wood": (0.62, 0.44, 0.26), "WoodDark": (0.42, 0.28, 0.16),
    "BusBody": (0.18, 0.55, 0.80), "BusRoof": (0.92, 0.93, 0.95), "Glass": (0.55, 0.75, 0.85), "Tire": (0.10, 0.10, 0.11),
    "VanBody": (0.95, 0.92, 0.85), "Light": (1.0, 0.95, 0.6), "Chrome": (0.75, 0.78, 0.82),
}.items()}

def assign(ob, name):
    ob.data.materials.clear()
    ob.data.materials.append(MATS[name])
    return ob

def prim_cube(name, cx, cy, cz, sx, sy, sz, m):
    bpy.ops.mesh.primitive_cube_add(size=1, location=(cx, cy, cz))
    ob = bpy.context.active_object; ob.name = name; ob.scale = (sx, sy, sz)
    bpy.ops.object.transform_apply(scale=True)
    return assign(ob, m)

def prim_cyl(name, cx, cy, cz, r, h, m, verts=20, rot=(0,0,0), r2=None):
    if r2 is None:
        bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=r, depth=h, location=(cx, cy, cz), rotation=rot)
    else:
        bpy.ops.mesh.primitive_cone_add(vertices=verts, radius1=r, radius2=r2, depth=h, location=(cx, cy, cz), rotation=rot)
    ob = bpy.context.active_object; ob.name = name
    return assign(ob, m)

def prim_sphere(name, cx, cy, cz, r, m, seg=20, ring=12, scale=(1,1,1)):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=seg, ring_count=ring, radius=r, location=(cx, cy, cz))
    ob = bpy.context.active_object; ob.name = name; ob.scale = scale
    bpy.ops.object.transform_apply(scale=True)
    return assign(ob, m)

def join(objs, name):
    for o in bpy.context.selected_objects: o.select_set(False)
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.join()
    ob = bpy.context.view_layer.objects.active; ob.name = name
    return ob

def smooth(ob, angle=50):
    for o in bpy.context.selected_objects: o.select_set(False)
    ob.select_set(True); bpy.context.view_layer.objects.active = ob
    bpy.ops.object.shade_smooth()
    try:
        bpy.ops.object.shade_auto_smooth(angle=math.radians(angle))
    except Exception:
        pass
    return ob

def export(ob, filename):
    for o in bpy.context.selected_objects: o.select_set(False)
    ob.select_set(True); bpy.context.view_layer.objects.active = ob
    path = os.path.join(EXPORT_DIR, filename + ".fbx")
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_ALL', bake_space_transform=True,
        axis_forward='-Z', axis_up='Y', object_types={'MESH'},
        mesh_smooth_type='FACE', use_mesh_modifiers=True, add_leaf_bones=False,
        path_mode='STRIP', embed_textures=False)
    print("exported", path)
    for o in bpy.context.scene.objects: o.select_set(True)
    bpy.ops.object.delete(use_global=False)

# ── 말랑이(장애물 슬라임): 위가 볼록, 아래가 퍼진 돔 + 큰 눈 ─────────────────────
def slime():
    body = prim_sphere("Body", 0, 0, 0.42, 0.5, "SlimeBody", seg=24, ring=14, scale=(1.05, 1.05, 0.84))
    # 바닥을 평평하게: z<0 정점 제거
    bm = bmesh.new(); bm.from_mesh(body.data)
    for v in [v for v in bm.verts if v.co.z < 0.0]: bm.verts.remove(v)
    bmesh.ops.holes_fill(bm, edges=bm.edges)
    bm.to_mesh(body.data); bm.free()
    skirt = prim_sphere("Skirt", 0, 0, 0.10, 0.55, "SlimeDark", seg=24, ring=8, scale=(1.0, 1.0, 0.22))
    eyeL = prim_sphere("EyeL", 0.34, -0.17, 0.48, 0.10, "Eye", seg=12, ring=8, scale=(0.6, 1, 1.2))
    eyeR = prim_sphere("EyeR", 0.34, 0.17, 0.48, 0.10, "Eye", seg=12, ring=8, scale=(0.6, 1, 1.2))
    hlL = prim_sphere("HlL", 0.41, -0.20, 0.52, 0.035, "EyeWhite", seg=8, ring=6)
    hlR = prim_sphere("HlR", 0.41, 0.14, 0.52, 0.035, "EyeWhite", seg=8, ring=6)
    ob = join([body, skirt, eyeL, eyeR, hlL, hlR], "Obs3_Slime")
    return smooth(ob, 60)

# ── 콘 ────────────────────────────────────────────────────────────────────────
def cone():
    base = prim_cube("Base", 0, 0, 0.03, 0.56, 0.56, 0.06, "ConeBase")
    c1 = prim_cyl("C1", 0, 0, 0.06 + 0.22, 0.20, 0.44, "ConeOrange", verts=16, r2=0.135)
    band = prim_cyl("Band", 0, 0, 0.50 + 0.07, 0.14, 0.14, "ConeWhite", verts=16, r2=0.105)
    c2 = prim_cyl("C2", 0, 0, 0.64 + 0.09, 0.105, 0.18, "ConeOrange", verts=16, r2=0.05)
    tip = prim_sphere("Tip", 0, 0, 0.73, 0.05, "ConeOrange", seg=10, ring=6)
    ob = join([base, c1, band, c2, tip], "Obs3_Cone")
    return smooth(ob, 40)

# ── A형 바리케이드 ─────────────────────────────────────────────────────────────
def barrier():
    parts = []
    for s in (-1, 1):
        for x in (-0.16, 0.16):
            leg = prim_cube("Leg", x * 0.9, s * 0.62, 0.42, 0.06, 0.06, 0.84, "Metal")
            leg.rotation_euler = (0, math.radians(-x * 60), 0); bpy.ops.object.transform_apply(rotation=True)
            parts.append(leg)
        parts.append(prim_cube("Foot", 0, s * 0.62, 0.03, 0.44, 0.08, 0.06, "Metal"))
    parts.append(prim_cube("Plank1", 0, 0, 0.72, 0.06, 1.40, 0.20, "BarrierOrange"))
    for y in (-0.42, 0.0, 0.42):
        parts.append(prim_cube("Stripe", 0.035, y, 0.72, 0.01, 0.16, 0.21, "BarrierWhite"))
    parts.append(prim_cube("Plank2", 0, 0, 0.38, 0.05, 1.40, 0.12, "BarrierOrange"))
    ob = join(parts, "Obs3_Barrier")
    return ob

# ── 크레이트 ───────────────────────────────────────────────────────────────────
def crate():
    parts = [prim_cube("Box", 0, 0, 0.36, 0.72, 0.72, 0.72, "Wood")]
    for z in (0.06, 0.66):
        for axis in ("x", "y"):
            for s in (-1, 1):
                if axis == "x": parts.append(prim_cube("Frame", s * 0.36, 0, z, 0.05, 0.76, 0.07, "WoodDark"))
                else: parts.append(prim_cube("Frame", 0, s * 0.36, z, 0.76, 0.05, 0.07, "WoodDark"))
    for s in (-1, 1):
        parts.append(prim_cube("Post", s * 0.36, s * 0.36, 0.36, 0.06, 0.06, 0.74, "WoodDark"))
        parts.append(prim_cube("Post", s * 0.36, -s * 0.36, 0.36, 0.06, 0.06, 0.74, "WoodDark"))
    return join(parts, "Obs3_Crate")

# ── 버스(정면이 -Y = 플레이어 쪽으로 다가옴; Unity에서 yaw 로 맞춘다) ────────────
def bus():
    L, W, H = 6.0, 2.2, 2.6
    parts = [prim_cube("Body", 0, 0, 0.55 + H * 0.5 - 0.2, W, L, H - 0.7, "BusBody")]
    parts.append(prim_cube("Roof", 0, 0, 0.55 + H - 0.65, W - 0.1, L - 0.1, 0.5, "BusRoof"))
    parts.append(prim_cube("Windshield", 0, -L * 0.5 - 0.01, 1.9, W - 0.4, 0.06, 0.9, "Glass"))
    for s in (-1, 1):
        parts.append(prim_cube("SideGlass", s * (W * 0.5 + 0.005), 0.4, 1.9, 0.04, L - 1.6, 0.8, "Glass"))
        parts.append(prim_cube("Head", s * 0.75, -L * 0.5 - 0.02, 1.0, 0.36, 0.08, 0.22, "Light"))
        for y in (-L * 0.32, L * 0.32):
            parts.append(prim_cyl("Wheel", s * (W * 0.5 - 0.15), y, 0.42, 0.42, 0.3, "Tire", verts=18, rot=(0, math.radians(90), 0)))
    parts.append(prim_cube("Bumper", 0, -L * 0.5 - 0.05, 0.5, W, 0.12, 0.25, "Chrome"))
    return join(parts, "Obs3_Bus")

def van():
    L, W = 4.4, 1.8
    parts = [prim_cube("Body", 0, 0.3, 0.9, W, L - 0.6, 0.9, "VanBody")]
    parts.append(prim_cube("Cabin", 0, 0.5, 1.55, W - 0.15, L - 1.6, 0.6, "VanBody"))
    parts.append(prim_cube("Hood", 0, -L * 0.5 + 0.5, 0.95, W - 0.1, 1.0, 0.5, "VanBody"))
    parts.append(prim_cube("Windshield", 0, -0.55, 1.5, W - 0.5, 0.06, 0.55, "Glass"))
    for s in (-1, 1):
        parts.append(prim_cube("Head", s * 0.62, -L * 0.5 - 0.01, 0.95, 0.3, 0.08, 0.18, "Light"))
        for y in (-1.3, 1.3):
            parts.append(prim_cyl("Wheel", s * (W * 0.5 - 0.1), y, 0.36, 0.36, 0.26, "Tire", verts=18, rot=(0, math.radians(90), 0)))
    parts.append(prim_cube("Bumper", 0, -L * 0.5 - 0.04, 0.5, W, 0.1, 0.2, "Chrome"))
    return join(parts, "Obs3_Van")

for fn in (slime, cone, barrier, crate, bus, van):
    ob = fn()
    export(ob, ob.name)
print("obstacle kit done")
