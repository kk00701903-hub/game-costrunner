# 64차(사용자): 거리에 「제주스러운 건물」을 간간이 — 초가(현무암 돌집 + 둥근 초가지붕 + 정낭) 2종, 기와집(한옥, 들린 처마) 2종.
#   JHouse_Thatch_A/B, JHouse_Tile_A/B → Resources/CoastRun/Models/*.fbx. JejuLots.House 가 섞어 놓는다.
# Blender headless:  blender -b --python jeju_house_kit.py   (build_jeju_house.bat)
# 축: Blender +X → Unity +X(도로 쪽 = 정면), +Y → Unity +Z(길 방향), +Z → Unity +Y(위). 원점 = 정면 바닥 중앙 근처, 1 unit = 1 m.
# 재질 이름은 JejuKit.Build() 가 게임 머티리얼로: Stone(현무암 텍스처)/WoodDark/Wood/Rope + 새 이름 Thatch/Tile/TileRidge/Paper/Plaster.
import bpy, bmesh, math, os, random
from mathutils import Vector

EXPORT_DIR = r"C:\dev\game\Assets\Resources\CoastRun\Models"
os.makedirs(EXPORT_DIR, exist_ok=True)
random.seed(64)

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
    "Stone": (0.22, 0.22, 0.24), "Wood": (0.62, 0.44, 0.26), "WoodDark": (0.42, 0.28, 0.16), "Rope": (0.55, 0.45, 0.28),
    "Thatch": (0.80, 0.66, 0.38), "Tile": (0.32, 0.34, 0.38), "TileRidge": (0.18, 0.19, 0.22),
    "Paper": (0.96, 0.92, 0.80), "Plaster": (0.93, 0.89, 0.78),
}.items()}

def assign(ob, name):
    ob.data.materials.clear(); ob.data.materials.append(MATS[name]); return ob

def cube(name, cx, cy, cz, sx, sy, sz, m, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(size=1, location=(cx, cy, cz), rotation=rot)
    ob = bpy.context.active_object; ob.name = name; ob.scale = (sx, sy, sz)
    bpy.ops.object.transform_apply(scale=True, rotation=True)
    return assign(ob, m)

def cyl(name, cx, cy, cz, r, h, m, verts=12, r2=None, rot=(0, 0, 0)):
    if r2 is None:
        bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=r, depth=h, location=(cx, cy, cz), rotation=rot)
    else:
        bpy.ops.mesh.primitive_cone_add(vertices=verts, radius1=r, radius2=r2, depth=h, location=(cx, cy, cz), rotation=rot)
    ob = bpy.context.active_object; ob.name = name
    return assign(ob, m)

def torus(name, cx, cy, cz, R, r, m, scale=(1, 1, 1)):
    bpy.ops.mesh.primitive_torus_add(major_segments=24, minor_segments=6, major_radius=R, minor_radius=r, location=(cx, cy, cz))
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

def smooth(ob, angle=40):
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

# ── 둥근 초가지붕: 타원구를 z=cut 평면으로 잘라 위쪽만(캡) ─────────────────────────
def dome_cap(name, cx, cy, cz, rx, ry, rz, cut, m):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=28, ring_count=14, radius=1.0, location=(cx, cy, cz))
    ob = bpy.context.active_object; ob.name = name; ob.scale = (rx, ry, rz)
    bpy.ops.object.transform_apply(scale=True)
    bm = bmesh.new(); bm.from_mesh(ob.data)
    geom = bm.verts[:] + bm.edges[:] + bm.faces[:]
    bmesh.ops.bisect_plane(bm, geom=geom, dist=0.0001, plane_co=(0, 0, cut - cz), plane_no=(0, 0, -1), clear_outer=True)
    bmesh.ops.holes_fill(bm, edges=bm.edges[:], sides=0)
    bm.to_mesh(ob.data); bm.free()
    return assign(ob, m)

# ── 기와 지붕(우진각, 처마 끝 들림): 밑변 8점(모서리 살짝 위로) + 용마루 2점 ─────────────
def hip_roof(name, cx, cy, cz, hx, hy, ridge_half, height, m, curl=0.22):
    bm = bmesh.new()
    def v(x, y, z): return bm.verts.new((x, y, z))
    b = [v(-hx, -hy, cz + curl), v(0, -hy, cz), v(hx, -hy, cz + curl), v(hx, 0, cz),
         v(hx, hy, cz + curl), v(0, hy, cz), v(-hx, hy, cz + curl), v(-hx, 0, cz)]
    r0 = v(0, -ridge_half, cz + height); r1 = v(0, ridge_half, cz + height)
    bm.faces.new([b[0], b[1], b[2], r0])              # −y 박공(우진각 끝)
    bm.faces.new([b[2], b[3], b[4], r1, r0])          # +x 긴 면(정면)
    bm.faces.new([b[4], b[5], b[6], r1])              # +y 끝
    bm.faces.new([b[6], b[7], b[0], r0, r1])          # −x 긴 면(뒷면)
    bm.faces.new([b[7], b[6], b[5], b[4], b[3], b[2], b[1], b[0]])   # 밑면
    bmesh.ops.triangulate(bm, faces=bm.faces[:])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    ob = bpy.data.objects.new(name, me); bpy.context.collection.objects.link(ob)
    ob.location = (cx, cy, 0)
    bpy.ops.object.select_all(action='DESELECT'); ob.select_set(True); bpy.context.view_layer.objects.active = ob
    return assign(ob, m)

# ── 정낭(제주 대문): 돌기둥 2개 + 가로 나무 3개 ────────────────────────────────────
def jeongnang(parts, x, y0, y1, logs=3):
    for y in (y0, y1):
        parts.append(cyl("Post", x, y, 0.55, 0.15, 1.1, "Stone", verts=8, r2=0.12))
    for i in range(logs):
        parts.append(cyl("Log", x, (y0 + y1) * 0.5, 0.30 + i * 0.27, 0.05, abs(y1 - y0) + 0.1, "WoodDark", verts=8, rot=(math.pi / 2, 0, 0)))

def door(parts, x, y, z, w=1.0, h=1.75):
    parts.append(cube("DoorFrame", x, y, z + h * 0.5, 0.08, w + 0.16, h + 0.10, "WoodDark"))
    parts.append(cube("Door", x + 0.03, y, z + h * 0.5, 0.06, w, h, "Wood"))
    parts.append(cube("DoorBar", x + 0.07, y, z + h * 0.5, 0.03, w - 0.15, 0.06, "WoodDark"))

def paper_window(parts, x, y, z, w=0.95, h=0.85, lattice=True):
    parts.append(cube("WinFrame", x, y, z, 0.08, w + 0.14, h + 0.14, "WoodDark"))
    parts.append(cube("Paper", x + 0.03, y, z, 0.06, w, h, "Paper"))
    if lattice:
        parts.append(cube("Lat", x + 0.07, y, z, 0.03, 0.04, h, "WoodDark"))
        parts.append(cube("Lat", x + 0.07, y, z, 0.03, w, 0.04, "WoodDark"))
        parts.append(cube("Lat", x + 0.07, y - w * 0.25, z, 0.03, 0.03, h, "WoodDark"))
        parts.append(cube("Lat", x + 0.07, y + w * 0.25, z, 0.03, 0.03, h, "WoodDark"))

# ══════════════════════════════════════════════════════════════════════════════
# 1. 초가 A — 한 채(폭 7.0 · 깊이 4.4 · 벽 2.2) + 정낭
def thatch_a():
    parts = []
    parts.append(cube("Wall", 0, 0, 1.1, 4.4, 7.0, 2.2, "Stone"))
    # 둥근 초가지붕(두 겹: 본체 + 두꺼운 처마)
    parts.append(dome_cap("Roof", 0, 0, 2.05, 3.15, 4.45, 1.55, 2.05, "Thatch"))
    parts.append(dome_cap("Eave", 0, 0, 2.05, 3.30, 4.60, 1.25, 1.72, "Thatch"))
    # 새끼줄(바람에 안 날리게 묶은 줄) 두 바퀴
    parts.append(torus("Rope", 0, 0, 2.30, 1.0, 0.035, "Rope", scale=(3.05, 4.35, 1)))
    parts.append(torus("Rope", 0, 0, 2.95, 1.0, 0.03, "Rope", scale=(2.35, 3.55, 1)))
    for a in range(0, 360, 45):   # 세로 줄 8가닥(눕힌 실린더 대신 짧은 상자로 근사)
        rad = math.radians(a)
        parts.append(cube("RopeV", math.cos(rad) * 2.4, math.sin(rad) * 3.6, 2.62, 0.05, 0.05, 0.75, "Rope", rot=(math.sin(rad) * 0.9, -math.cos(rad) * 0.55, 0)))
    door(parts, 2.20, 0.0, 0.0)
    paper_window(parts, 2.20, -2.3, 1.25); paper_window(parts, 2.20, 2.3, 1.25)
    # 부엌 굴뚝(낮은 돌 굴뚝)
    parts.append(cube("Chimney", -1.4, 2.6, 3.05, 0.5, 0.5, 1.3, "Stone"))
    jeongnang(parts, 3.55, -1.05, 1.05)
    return smooth(join(parts, "JHouse_Thatch_A"), 40)

# 2. 초가 B — 본채 + 낮은 곁채(ㄱ자), 정낭 없이 낮은 돌담 문
def thatch_b():
    parts = []
    parts.append(cube("Wall", -0.4, -0.8, 1.15, 4.4, 5.6, 2.3, "Stone"))
    parts.append(dome_cap("Roof", -0.4, -0.8, 2.15, 3.15, 3.75, 1.5, 2.15, "Thatch"))
    parts.append(dome_cap("Eave", -0.4, -0.8, 2.15, 3.30, 3.90, 1.2, 1.82, "Thatch"))
    parts.append(torus("Rope", -0.4, -0.8, 2.42, 1.0, 0.035, "Rope", scale=(3.05, 3.65, 1)))
    # 곁채(작고 낮음, +y 쪽 앞으로 튀어나옴)
    parts.append(cube("Annex", 0.5, 3.0, 0.9, 3.4, 2.6, 1.8, "Stone"))
    parts.append(dome_cap("RoofS", 0.5, 3.0, 1.7, 2.45, 1.95, 1.1, 1.7, "Thatch"))
    parts.append(dome_cap("EaveS", 0.5, 3.0, 1.7, 2.55, 2.05, 0.9, 1.45, "Thatch"))
    parts.append(torus("Rope", 0.5, 3.0, 1.9, 1.0, 0.03, "Rope", scale=(2.35, 1.85, 1)))
    door(parts, 1.80, -0.8, 0.0)
    paper_window(parts, 1.80, -2.6, 1.2); paper_window(parts, 2.20, 3.0, 0.95, w=0.8, h=0.7)
    # 앞마당 낮은 돌담 + 항아리(장독)
    parts.append(cube("YardWall", 3.3, -3.2, 0.35, 0.4, 2.4, 0.7, "Stone"))
    for i in range(3):
        parts.append(cyl("Jar", 3.0, 1.4 + i * 0.75, 0.32, 0.30, 0.62, "WoodDark", verts=10, r2=0.22))
    return smooth(join(parts, "JHouse_Thatch_B"), 40)

# 3. 기와집 A — 현무암 기단 + 회벽 + 나무 기둥 + 들린 처마 기와지붕(폭 7.6 · 깊이 4.8)
def tile_a():
    parts = []
    parts.append(cube("Base", 0, 0, 0.30, 5.0, 7.8, 0.6, "Stone"))
    parts.append(cube("Wall", 0, 0, 1.55, 4.6, 7.4, 1.9, "Plaster"))
    # 기둥(정면 4개 + 뒷면 2개) 과 처마 밑 도리
    for y in (-3.6, -1.2, 1.2, 3.6):
        parts.append(cube("Col", 2.32, y, 1.55, 0.22, 0.22, 1.95, "WoodDark"))
    for y in (-3.6, 3.6):
        parts.append(cube("Col", -2.32, y, 1.55, 0.22, 0.22, 1.95, "WoodDark"))
    parts.append(cube("Beam", 2.32, 0, 2.55, 0.26, 7.6, 0.18, "WoodDark"))
    parts.append(cube("Beam", -2.32, 0, 2.55, 0.26, 7.6, 0.18, "WoodDark"))
    parts.append(cube("Beam", 0, 3.75, 2.55, 4.9, 0.26, 0.18, "WoodDark"))
    parts.append(cube("Beam", 0, -3.75, 2.55, 4.9, 0.26, 0.18, "WoodDark"))
    # 지붕: 밑판(서까래 색) + 기와 + 용마루 + 처마 끝 막새
    parts.append(hip_roof("Soffit", 0, 0, 2.62, 3.35, 4.95, 2.0, 1.55, "WoodDark", curl=0.20))
    parts.append(hip_roof("Roof", 0, 0, 2.70, 3.40, 5.00, 2.0, 1.60, "Tile", curl=0.22))
    parts.append(cube("Ridge", 0, 0, 4.36, 0.34, 4.3, 0.22, "TileRidge"))
    parts.append(cyl("RidgeEnd", 0, -2.15, 4.42, 0.17, 0.34, "TileRidge", verts=10, rot=(math.pi / 2, 0, 0)))
    parts.append(cyl("RidgeEnd", 0, 2.15, 4.42, 0.17, 0.34, "TileRidge", verts=10, rot=(math.pi / 2, 0, 0)))
    # 추녀마루(모서리 능선)
    for sy in (-1, 1):
        for sx in (-1, 1):
            parts.append(cube("HipRidge", sx * 1.7, sy * 3.55, 3.55, 0.16, 0.16, 0.16, "TileRidge", rot=(0, sx * 0.44, -sy * sx * 0.85)))
    door(parts, 2.30, 0.0, 0.6, w=1.2, h=1.55)
    paper_window(parts, 2.30, -2.4, 1.6, w=1.4, h=0.9); paper_window(parts, 2.30, 2.4, 1.6, w=1.4, h=0.9)
    # 툇마루(나무 마루)
    parts.append(cube("Maru", 2.75, 0, 0.52, 0.9, 6.6, 0.12, "Wood"))
    for y in (-3.1, -1.0, 1.0, 3.1):
        parts.append(cube("MaruLeg", 3.1, y, 0.25, 0.12, 0.12, 0.5, "WoodDark"))
    return smooth(join(parts, "JHouse_Tile_A"), 40)

# 4. 기와집 B — 낮고 넓은 한 채 + 오른쪽 곁채, 기단 낮음(민가·상점 겸용)
def tile_b():
    parts = []
    parts.append(cube("Base", -0.3, -1.0, 0.22, 4.6, 5.6, 0.44, "Stone"))
    parts.append(cube("Wall", -0.3, -1.0, 1.35, 4.2, 5.2, 1.85, "Plaster"))
    for y in (-3.4, -1.0, 1.4):
        parts.append(cube("Col", 1.82, y, 1.35, 0.2, 0.2, 1.9, "WoodDark"))
    parts.append(cube("Beam", 1.82, -1.0, 2.32, 0.24, 5.4, 0.16, "WoodDark"))
    parts.append(hip_roof("Soffit", -0.3, -1.0, 2.38, 3.05, 3.75, 1.4, 1.35, "WoodDark", curl=0.18))
    parts.append(hip_roof("Roof", -0.3, -1.0, 2.45, 3.10, 3.80, 1.4, 1.40, "Tile", curl=0.20))
    parts.append(cube("Ridge", -0.3, -1.0, 3.90, 0.30, 3.0, 0.2, "TileRidge"))
    # 곁채(낮은 창고, 현무암 벽 + 작은 기와지붕)
    parts.append(cube("Annex", 0.4, 3.0, 0.85, 3.2, 2.4, 1.7, "Stone"))
    parts.append(hip_roof("RoofS", 0.4, 3.0, 1.75, 2.15, 1.75, 0.6, 0.95, "Tile", curl=0.12))
    parts.append(cube("RidgeS", 0.4, 3.0, 2.72, 0.24, 1.3, 0.16, "TileRidge"))
    door(parts, 1.80, -1.0, 0.44, w=1.1, h=1.5)
    paper_window(parts, 1.80, -3.0, 1.45, w=1.1, h=0.85); paper_window(parts, 2.0, 3.0, 0.95, w=0.8, h=0.7, lattice=False)
    parts.append(cube("Maru", 2.2, -1.0, 0.40, 0.8, 4.6, 0.12, "Wood"))
    jeongnang(parts, 3.45, -3.0, -1.0, logs=2)
    return smooth(join(parts, "JHouse_Tile_B"), 40)

for fn in (thatch_a, thatch_b, tile_a, tile_b):
    ob = fn()
    export(ob, ob.name)
print("jeju house kit done")
