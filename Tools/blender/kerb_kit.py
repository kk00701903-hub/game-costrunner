# 59차(사용자): 러닝 도로변(왼쪽 인도 가장자리)에 15 m 마다 똑같이 반복되던 「네모 박스 화분」을
#   제주 소품 6종 low-poly 키트로 교체 — Kerb_*.fbx. 코드(KerbProps.cs)가 종류·회전·크기를 섞어 놓는다.
# Blender headless:  blender -b --python kerb_kit.py   (build_kerb.bat)
# 축: Blender +X → Unity +X(도로 쪽), +Y → Unity +Z(진행), +Z → Unity +Y(위). 원점 = 바닥 중앙, 1 unit = 1 m.
# 재질 이름은 JejuKit.Build() 가 게임 머티리얼로 바꾼다(Stone/Wood/WoodDark/Orange/Leaf + 새 이름 Soil/Glaze/GlazeDark/
#   BuoyOrange/BuoyWhite/Rope/BloomBlue/BloomPink/BloomYellow/BloomWhite).
import bpy, bmesh, math, os, random
from mathutils import Vector

EXPORT_DIR = r"C:\dev\game\Assets\Resources\CoastRun\Models"
os.makedirs(EXPORT_DIR, exist_ok=True)
random.seed(59)

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
            b.inputs["Roughness"].default_value = 0.85
    return m

MATS = {n: mat(n, c) for n, c in {
    "Stone": (0.22, 0.22, 0.24), "Wood": (0.62, 0.44, 0.26), "WoodDark": (0.42, 0.28, 0.16),
    "Orange": (0.98, 0.60, 0.15), "Leaf": (0.25, 0.55, 0.28), "Soil": (0.30, 0.20, 0.13),
    "Glaze": (0.45, 0.28, 0.18), "GlazeDark": (0.30, 0.18, 0.12),
    "BuoyOrange": (0.98, 0.45, 0.15), "BuoyWhite": (0.96, 0.95, 0.90), "Rope": (0.80, 0.70, 0.45),
    "BloomBlue": (0.45, 0.60, 0.95), "BloomPink": (0.98, 0.55, 0.72), "BloomYellow": (1.0, 0.85, 0.20), "BloomWhite": (0.98, 0.97, 0.92),
}.items()}

def assign(ob, name):
    ob.data.materials.clear(); ob.data.materials.append(MATS[name]); return ob

def cube(name, cx, cy, cz, sx, sy, sz, m, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(size=1, location=(cx, cy, cz), rotation=rot)
    ob = bpy.context.active_object; ob.name = name; ob.scale = (sx, sy, sz)
    bpy.ops.object.transform_apply(scale=True, rotation=True)
    return assign(ob, m)

def cyl(name, cx, cy, cz, r, h, m, verts=16, r2=None, rot=(0, 0, 0)):
    if r2 is None:
        bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=r, depth=h, location=(cx, cy, cz), rotation=rot)
    else:
        bpy.ops.mesh.primitive_cone_add(vertices=verts, radius1=r, radius2=r2, depth=h, location=(cx, cy, cz), rotation=rot)
    ob = bpy.context.active_object; ob.name = name
    return assign(ob, m)

def sphere(name, cx, cy, cz, r, m, seg=12, ring=8, scale=(1, 1, 1)):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=seg, ring_count=ring, radius=r, location=(cx, cy, cz))
    ob = bpy.context.active_object; ob.name = name; ob.scale = scale
    bpy.ops.object.transform_apply(scale=True)
    return assign(ob, m)

def torus(name, cx, cy, cz, R, r, m, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_torus_add(major_segments=16, minor_segments=8, major_radius=R, minor_radius=r, location=(cx, cy, cz), rotation=rot)
    ob = bpy.context.active_object; ob.name = name
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
    try: bpy.ops.object.shade_auto_smooth(angle=math.radians(angle))
    except Exception: pass
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

# 꽃 덤불: 잎 구 + 꽃 구 여러 개
def bush(parts, cx, cy, cz, r, bloom, n=7, leaf_r=None):
    leaf_r = leaf_r or r
    parts.append(sphere("Leaf", cx, cy, cz, leaf_r, "Leaf", seg=12, ring=8, scale=(1.15, 1.15, 0.8)))
    for i in range(n):
        a = random.uniform(0, math.tau); e = random.uniform(0.05, 1.2)
        rr = leaf_r * 0.95
        p = (cx + rr * math.cos(a) * math.cos(e), cy + rr * math.sin(a) * math.cos(e), cz + rr * math.sin(e) * 0.8)
        parts.append(sphere("Bloom", *p, r * 0.34, bloom, seg=10, ring=6))

# ── 1. 현무암 화분 + 수국(파랑) ────────────────────────────────────────────────
def planter_basalt():
    parts = [cyl("Pot", 0, 0, 0.24, 0.40, 0.48, "Stone", verts=10, r2=0.34)]
    parts.append(cyl("Rim", 0, 0, 0.47, 0.42, 0.06, "Stone", verts=10))
    parts.append(cyl("Soil", 0, 0, 0.49, 0.36, 0.04, "Soil", verts=10))
    bush(parts, 0, 0, 0.72, 0.30, "BloomBlue", n=9, leaf_r=0.34)
    return smooth(join(parts, "Kerb_PlanterBasalt"), 45)

# ── 2. 나무 화단 상자 + 유채(노랑) ──────────────────────────────────────────────
def planter_wood():
    parts = [cube("Box", 0, 0, 0.26, 0.60, 0.90, 0.46, "Wood")]
    for z in (0.10, 0.42):
        parts.append(cube("Band", 0.31, 0, z, 0.03, 0.94, 0.06, "WoodDark"))
        parts.append(cube("Band", -0.31, 0, z, 0.03, 0.94, 0.06, "WoodDark"))
    for sx in (-1, 1):
        for sy in (-1, 1):
            parts.append(cube("Post", sx * 0.30, sy * 0.45, 0.26, 0.07, 0.07, 0.52, "WoodDark"))
    parts.append(cube("Soil", 0, 0, 0.48, 0.54, 0.84, 0.03, "Soil"))
    for y in (-0.28, 0.0, 0.28):
        bush(parts, 0, y, 0.62, 0.17, "BloomYellow", n=6, leaf_r=0.20)
    return join(parts, "Kerb_PlanterWood")

# ── 3. 귤 상자 두 개 쌓기(살 상자) + 귤 ────────────────────────────────────────
def crate(parts, cx, cy, cz, w, d, h, yaw=0.0):
    rot = (0, 0, yaw)
    parts.append(cube("Floor", cx, cy, cz + 0.03, w, d, 0.05, "WoodDark", rot))
    for s in (-1, 1):
        for zz in (cz + 0.14, cz + h - 0.10):
            parts.append(cube("SlatX", cx + s * (w * 0.5 - 0.02), cy, zz, 0.04, d, 0.11, "Wood", rot))
            parts.append(cube("SlatY", cx, cy + s * (d * 0.5 - 0.02), zz, w, 0.04, 0.11, "Wood", rot))
    for sx in (-1, 1):
        for sy in (-1, 1):
            parts.append(cube("Post", cx + sx * (w * 0.5 - 0.03), cy + sy * (d * 0.5 - 0.03), cz + h * 0.5, 0.06, 0.06, h, "WoodDark", rot))

def crate_stack():
    parts = []
    crate(parts, 0, 0, 0.0, 0.62, 0.62, 0.40)
    crate(parts, 0.02, 0.03, 0.40, 0.62, 0.62, 0.40, yaw=math.radians(8))
    for i in range(9):
        a = random.uniform(0, math.tau); rr = random.uniform(0, 0.20)
        parts.append(sphere("Orange", rr * math.cos(a), rr * math.sin(a), 0.84 + random.uniform(0, 0.05), 0.085, "Orange", seg=10, ring=6))
    parts.append(sphere("Orange", 0.36, -0.30, 0.085, 0.085, "Orange", seg=10, ring=6))
    return smooth(join(parts, "Kerb_CrateStack"), 60)

# ── 4. 옹기 항아리 두 개(큰 것·작은 것) ────────────────────────────────────────
def onggi(parts, cx, cy, h, r):
    # 몸통: 아래 좁고 배 부르고 입 좁은 실루엣 — 원뿔대 3단 + 구
    parts.append(cyl("Base", cx, cy, h * 0.12, r * 0.55, h * 0.24, "GlazeDark", verts=14, r2=r * 0.95))
    parts.append(sphere("Belly", cx, cy, h * 0.50, r, "Glaze", seg=16, ring=10, scale=(1, 1, h * 0.60 / r)))
    parts.append(cyl("Neck", cx, cy, h * 0.86, r * 0.62, h * 0.16, "Glaze", verts=14, r2=r * 0.55))
    parts.append(torus("Lip", cx, cy, h * 0.94, r * 0.55, 0.035, "GlazeDark"))
    parts.append(cyl("Lid", cx, cy, h * 0.99, r * 0.60, 0.05, "GlazeDark", verts=14, r2=r * 0.30))

def onggi_pair():
    parts = [cyl("Plate", 0, 0, 0.03, 0.62, 0.06, "Stone", verts=10)]
    onggi(parts, -0.12, -0.14, 0.95, 0.34)
    onggi(parts, 0.16, 0.30, 0.62, 0.23)
    return smooth(join(parts, "Kerb_Onggi"), 60)

# ── 5. 부표 더미 + 밧줄 코일(어촌 골목) ──────────────────────────────────────────
def buoys():
    parts = [torus("Rope", 0.0, -0.18, 0.05, 0.28, 0.05, "Rope"), torus("Rope2", 0.03, -0.16, 0.13, 0.25, 0.045, "Rope")]
    spots = [(-0.02, 0.22, 0.26, 0.26, "BuoyOrange"), (0.30, 0.02, 0.22, 0.22, "BuoyWhite"),
             (-0.30, 0.05, 0.20, 0.20, "BuoyOrange"), (0.10, 0.12, 0.62, 0.22, "BuoyWhite")]
    for x, y, z, r, m in spots:
        parts.append(sphere("Buoy", x, y, z, r, m, seg=14, ring=9, scale=(1, 1, 1.15)))
        parts.append(cube("Band", x, y, z, r * 2.05, r * 2.05, 0.05, "Rope"))
    parts.append(cyl("Stake", -0.22, -0.30, 0.45, 0.03, 0.9, "WoodDark", verts=8))
    return smooth(join(parts, "Kerb_Buoys"), 60)

# ── 6. 낮은 돌 화단 + 동백/무궁화(분홍) ───────────────────────────────────────────
def stone_bed():
    parts = []
    n = 9
    for i in range(n):
        a = i / n * math.tau
        parts.append(cube("Rock", 0.44 * math.cos(a), 0.44 * math.sin(a), 0.14, 0.24, 0.22, 0.26 + random.uniform(-0.04, 0.04), "Stone", rot=(0, 0, a)))
    parts.append(cyl("Soil", 0, 0, 0.20, 0.40, 0.04, "Soil", verts=12))
    parts.append(cyl("Trunk", 0, 0, 0.40, 0.04, 0.40, "WoodDark", verts=8))
    bush(parts, 0, 0, 0.80, 0.32, "BloomPink", n=10, leaf_r=0.36)
    return smooth(join(parts, "Kerb_StoneBed"), 45)

for fn in (planter_basalt, planter_wood, crate_stack, onggi_pair, buoys, stone_bed):
    ob = fn()
    export(ob, ob.name)
print("kerb kit done")
