# 14차-13: 파트별로 분해한 상가 키트 (Shop_A~F). 골드런/서브웨이 서퍼식 '덩어리 + 트림' 건물:
# 벽(Wall) / 흰 트림(Trim: 코니스·기둥·난간·간판) / 창틀(Frame) / 유리(Glass) / 문(Door) /
# 차양 슬랫(AwningA=포인트, AwningB=흰색) / 지붕(Roof). 재질별로 Unity 에서 팔레트 색이 들어간다.
# 원점 = 정면 바닥 중앙, 정면 = +X, 폭 = Y, 높이 = Z. 1 unit = 1 m.
import bpy, bmesh, math, os
from mathutils import Vector

EXPORT_DIR = r"C:\dev\game\Assets\Resources\CoastRun\Models"
os.makedirs(EXPORT_DIR, exist_ok=True)
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)

def mat(name, rgb):
    m = bpy.data.materials.get(name)
    if m is None:
        m = bpy.data.materials.new(name); m.use_nodes = True
        b = m.node_tree.nodes.get("Principled BSDF")
        if b: b.inputs["Base Color"].default_value = (*rgb, 1.0); b.inputs["Roughness"].default_value = 0.85
    return m
MATS = {n: mat(n, c) for n, c in {
    "Wall": (0.98, 0.94, 0.85), "Trim": (0.99, 0.99, 0.97), "Frame": (0.9, 0.5, 0.45), "Glass": (0.45, 0.66, 0.78),
    "Door": (0.7, 0.35, 0.3), "AwningA": (0.9, 0.5, 0.45), "AwningB": (0.99, 0.99, 0.97), "Roof": (0.85, 0.45, 0.4),
    "Concrete": (0.72, 0.72, 0.70), "Dark": (0.12, 0.10, 0.12),
}.items()}

def cube(name, cx, cy, cz, sx, sy, sz, m):
    bpy.ops.mesh.primitive_cube_add(size=1, location=(cx, cy, cz))
    ob = bpy.context.active_object; ob.name = name; ob.scale = (sx, sy, sz)
    bpy.ops.object.transform_apply(scale=True)
    ob.data.materials.clear(); ob.data.materials.append(MATS[m]); return ob

def join(objs, name):
    for o in bpy.context.selected_objects: o.select_set(False)
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]; bpy.ops.object.join()
    ob = bpy.context.view_layer.objects.active; ob.name = name; return ob

def export(ob, filename):
    for o in bpy.context.selected_objects: o.select_set(False)
    ob.select_set(True); bpy.context.view_layer.objects.active = ob
    path = os.path.join(EXPORT_DIR, filename + ".fbx")
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_ALL', bake_space_transform=True, axis_forward='-Z', axis_up='Y',
        object_types={'MESH'}, mesh_smooth_type='OFF', use_mesh_modifiers=True, add_leaf_bones=False,
        path_mode='STRIP', embed_textures=False)
    print("exported", path)
    for o in bpy.context.scene.objects: o.select_set(True)
    bpy.ops.object.delete(use_global=False)

FLOOR = 3.2

def window(parts, x, y, z, w=1.3, h=1.5, frame=0.10, sill=True):
    """정면(+X 면, x=면 위치)에 붙는 창: 튀어나온 틀 + 오목한 유리 + 창턱."""
    parts.append(cube("Frame", x + 0.05, y, z, 0.10, w + frame * 2, h + frame * 2, "Frame"))
    parts.append(cube("Glass", x + 0.06, y, z, 0.02, w, h, "Glass"))
    # 십자 창살
    parts.append(cube("Bar", x + 0.09, y, z, 0.02, 0.05, h, "Trim"))
    parts.append(cube("Bar", x + 0.09, y, z, 0.02, w, 0.05, "Trim"))
    if sill: parts.append(cube("Sill", x + 0.10, y, z - h * 0.5 - frame - 0.04, 0.22, w + frame * 2 + 0.16, 0.08, "Trim"))

def side_window(parts, x, y, z, side, w=1.1, h=1.4):
    """옆면(±Y) 창."""
    parts.append(cube("Frame", x, y + side * 0.05, z, w + 0.2, 0.10, h + 0.2, "Frame"))
    parts.append(cube("Glass", x, y + side * 0.06, z, w, 0.02, h, "Glass"))

def door(parts, x, y, z0, w=1.2, h=2.3):
    parts.append(cube("DoorFrame", x + 0.05, y, z0 + h * 0.5, 0.10, w + 0.24, h + 0.12, "Trim"))
    parts.append(cube("Door", x + 0.06, y, z0 + h * 0.5, 0.03, w, h, "Door"))
    parts.append(cube("Knob", x + 0.10, y + w * 0.32, z0 + 1.0, 0.05, 0.08, 0.08, "Trim"))
    parts.append(cube("Step", x + 0.25, y, z0 + 0.08, 0.5, w + 0.8, 0.16, "Concrete"))

def awning(parts, x, y, z, width, depth=1.1, slats=7):
    """줄무늬 차양: 슬랫을 번갈아 두 재질로, 20° 기울여 도로 쪽으로."""
    sw = width / slats
    for i in range(slats):
        yy = y - width * 0.5 + sw * (i + 0.5)
        s = cube("Slat", x + depth * 0.5 * 0.94, yy, z - depth * 0.5 * 0.34, depth, sw + 0.01, 0.06, "AwningA" if i % 2 == 0 else "AwningB")
        s.rotation_euler = (0, math.radians(20), 0); bpy.ops.object.transform_apply(rotation=True)   # 바깥쪽이 내려간다
        parts.append(s)
        # 스캘럽 단
        e = cube("Hem", x + depth * 0.94, yy, z - depth * 0.34 - 0.10, 0.05, sw + 0.01, 0.16, "AwningA" if i % 2 == 0 else "AwningB")
        parts.append(e)
    parts.append(cube("Rod", x + 0.08, y, z - 0.03, 0.06, width + 0.1, 0.06, "Trim"))

def sign(parts, x, y, z, width):
    parts.append(cube("SignEdge", x + 0.08, y, z, 0.12, width, 0.70, "Frame"))
    parts.append(cube("Sign", x + 0.15, y, z, 0.04, width - 0.16, 0.56, "Trim"))

def balcony(parts, x, y, z, width, depth=0.9):
    parts.append(cube("Slab", x + depth * 0.5, y, z - 0.08, depth, width, 0.16, "Trim"))
    parts.append(cube("Rail", x + depth - 0.03, y, z + 0.5, 0.06, width, 0.06, "Trim"))
    n = max(4, int(width / 0.35))
    for i in range(n + 1):
        yy = y - width * 0.5 + width / n * i
        parts.append(cube("Post", x + depth - 0.03, yy, z + 0.25, 0.04, 0.04, 0.5, "Trim"))
    for s in (-1, 1):
        parts.append(cube("Post", x + depth * 0.5, y + s * width * 0.5, z + 0.5, depth, 0.06, 0.06, "Trim"))

def shop(name, width, depth, storeys, roof, balcony_on, win_per_floor, shopfront=True):
    H = storeys * FLOOR
    parts = [cube("Wall", -depth * 0.5, 0, H * 0.5, depth, width, H, "Wall")]
    # 코니스(층 사이·꼭대기) + 모서리 기둥
    for f in range(1, storeys + 1):
        z = f * FLOOR
        parts.append(cube("Cornice", 0.06, 0, z - 0.06, 0.24, width + 0.3, 0.14, "Trim"))
        for s in (-1, 1):
            parts.append(cube("Cornice", -depth * 0.5, s * (width * 0.5 + 0.06), z - 0.06, depth + 0.1, 0.24, 0.14, "Trim"))
    for s in (-1, 1):
        parts.append(cube("Pilaster", 0.04, s * (width * 0.5 - 0.12), H * 0.5, 0.16, 0.26, H, "Trim"))
    # 지붕
    if roof == "parapet":
        parts.append(cube("Roof", -depth * 0.5, 0, H + 0.12, depth + 0.4, width + 0.4, 0.24, "Roof"))
        parts.append(cube("Parapet", -depth * 0.5, 0, H + 0.42, depth + 0.4, width + 0.4, 0.36, "Trim"))
        parts.append(cube("RoofTop", -depth * 0.5, 0, H + 0.66, depth + 0.1, width + 0.1, 0.12, "Roof"))
    elif roof == "gable":
        parts.append(cube("Roof", -depth * 0.5, 0, H + 0.12, depth + 0.5, width + 0.5, 0.24, "Roof"))
        for i in range(4):
            t = i / 4.0
            parts.append(cube("Roof", -depth * 0.5, 0, H + 0.24 + 0.32 * i + 0.16, (depth + 0.5) * (1 - t * 0.9), width + 0.5 - 0.3 * i, 0.32, "Roof"))
    else:  # awning roof band
        parts.append(cube("Roof", -depth * 0.5, 0, H + 0.14, depth + 0.6, width + 0.6, 0.28, "Roof"))
        parts.append(cube("RoofTrim", 0.3, 0, H + 0.02, 0.1, width + 0.6, 0.18, "Trim"))
    # 창: 2층부터(1층은 상가), 정면 win_per_floor 개, 옆면 2개
    for f in range(1, storeys):
        z = f * FLOOR + FLOOR * 0.55
        n = win_per_floor
        for i in range(n):
            y = -width * 0.5 + width / (n + 1) * (i + 1)
            window(parts, 0, y, z)
        for s in (-1, 1):
            for i in range(2):
                x = -depth * (0.3 + 0.4 * i)
                side_window(parts, x, s * width * 0.5, z, s)
        if balcony_on and f == 1:
            balcony(parts, 0, 0, f * FLOOR + 0.02, width * 0.6)
    # 1층 상가: 큰 유리 + 문 + 차양 + 간판 (또는 일반 창)
    if shopfront:
        door(parts, 0, -width * 0.25, 0)
        parts.append(cube("Frame", 0.05, width * 0.18, 1.35, 0.10, width * 0.42, 2.0, "Frame"))
        parts.append(cube("Glass", 0.06, width * 0.18, 1.35, 0.02, width * 0.42 - 0.2, 1.8, "Glass"))
        parts.append(cube("Bar", 0.09, width * 0.18, 1.35, 0.02, width * 0.42 - 0.2, 0.05, "Trim"))
        awning(parts, 0.12, 0, 2.55, width * 0.86)
        sign(parts, 0, 0, 3.05 - 0.2, width * 0.7)
    else:
        door(parts, 0, 0, 0)
        for y in (-width * 0.3, width * 0.3):
            window(parts, 0, y, FLOOR * 0.55)
        awning(parts, 0.12, 0, 2.5, width * 0.5, slats=5)
    # 1층 옆면 창
    for s in (-1, 1):
        side_window(parts, -depth * 0.5, s * width * 0.5, FLOOR * 0.55, s, w=1.3, h=1.2)
    return join(parts, name)

# ── 제주 낮은 집: 현무암 돌담 기단 + 낮은 벽 + 기와/슬레이트 우진각 지붕 + 나무 문. 실제 제주 해안 마을의 집. ──
def jeju_house(name, width=8.5, depth=6.0, two_storey=False):
    H = 3.0 if not two_storey else 5.6
    parts = [cube("Wall", -depth * 0.5, 0, H * 0.5 + 0.35, depth, width, H - 0.35, "Wall")]
    parts.append(cube("Stone", -depth * 0.5, 0, 0.35, depth + 0.16, width + 0.16, 0.7, "Stone"))      # 현무암 기단
    # 지붕: 우진각(4단 계단 근사) — 처마가 넓게 나온다
    for i in range(5):
        t = i / 5.0
        parts.append(cube("Roof", -depth * 0.5, 0, H + 0.35 + 0.26 * i, (depth + 1.2) * (1 - t * 0.85), (width + 1.2) * (1 - t * 0.7), 0.28, "Roof"))
    parts.append(cube("Trim", 0.62, 0, H + 0.42, 0.12, width + 1.2, 0.16, "Trim"))                    # 처마 밑 흰 띠
    door(parts, 0, 0, 0.35, w=1.0, h=2.0)
    for y in (-width * 0.3, width * 0.3):
        window(parts, 0, y, H * 0.5 + 0.55, w=1.1, h=1.1)
    for s in (-1, 1):
        side_window(parts, -depth * 0.5, s * width * 0.5, H * 0.5 + 0.55, s, w=1.0, h=1.0)
    if two_storey:
        for y in (-width * 0.3, 0, width * 0.3):
            window(parts, 0, y, 4.2, w=1.0, h=1.1)
    # 마당 쪽 돌담 한 토막(문 옆) + 감귤 상자
    parts.append(cube("Stone", 1.2, -width * 0.42, 0.45, 0.4, 2.2, 0.9, "Stone"))
    parts.append(cube("Crate", 1.0, width * 0.36, 0.25, 0.5, 0.7, 0.5, "Wood"))
    return join(parts, name)

# ── 카페 테라스 세트: 둥근 탁자 + 의자 2 (파라솔은 그림 소품) ──
def cafe_set(name):
    parts = [cube("Trim", 0, 0, 0.72, 0.9, 0.9, 0.05, "Trim"), cube("Dark", 0, 0, 0.36, 0.06, 0.06, 0.7, "Dark"), cube("Dark", 0, 0, 0.02, 0.5, 0.5, 0.04, "Dark")]
    for s in (-1, 1):
        parts.append(cube("Frame", 0, s * 0.85, 0.45, 0.44, 0.44, 0.05, "Frame"))
        parts.append(cube("Frame", 0, s * 1.05, 0.72, 0.44, 0.05, 0.55, "Frame"))
        for a in (-1, 1):
            for b in (-1, 1):
                parts.append(cube("Dark", a * 0.18, s * 0.85 + b * 0.18, 0.22, 0.04, 0.04, 0.44, "Dark"))
    return join(parts, name)

# ── 공원 정자/벤치 파빌리온: 네 기둥 + 지붕 ──
def pavilion(name):
    parts = [cube("Concrete", 0, 0, 0.1, 3.2, 3.2, 0.2, "Concrete")]
    for a in (-1, 1):
        for b in (-1, 1):
            parts.append(cube("Wood", a * 1.3, b * 1.3, 1.4, 0.16, 0.16, 2.6, "Wood"))
    for i in range(4):
        t = i / 4.0
        parts.append(cube("Roof", 0, 0, 2.75 + 0.25 * i, 3.8 * (1 - t * 0.8), 3.8 * (1 - t * 0.8), 0.26, "Roof"))
    parts.append(cube("Wood", 0, 0, 0.45, 1.2, 2.6, 0.08, "Wood"))
    return join(parts, name)

MATS["Stone"] = mat("Stone", (0.22, 0.22, 0.24)); MATS["Wood"] = mat("Wood", (0.55, 0.38, 0.22))
MATS["FacadeFront"] = mat("FacadeFront", (0.9, 0.9, 0.9))

def uv_box(name, x0, x1, y0, y1, z0, z1, front_mat, other_mat):
    """정면(+X)만 UV 0..1 로 그림을 씌우는 상자. 나머지 면은 other_mat."""
    me = bpy.data.meshes.new(name); ob = bpy.data.objects.new(name, me); bpy.context.collection.objects.link(ob)
    bm = bmesh.new()
    V = [(x0,y0,z0),(x1,y0,z0),(x1,y1,z0),(x0,y1,z0),(x0,y0,z1),(x1,y0,z1),(x1,y1,z1),(x0,y1,z1)]
    bv = [bm.verts.new(Vector(v)) for v in V]; bm.verts.ensure_lookup_table()
    uvl = bm.loops.layers.uv.new("UVMap")
    # 15차-2: 옆면(±Y)에도 같은 그림을 씌운다(골드런식 '사방 그림 상자'). 뒤·위·아래만 단색.
    faces = [([1,2,6,5], front_mat, [(0,0),(1,0),(1,1),(0,1)]),   # +X 정면: y0→y1 = u 0→1, z = v
             ([0,1,5,4], front_mat, [(0,0),(1,0),(1,1),(0,1)]),   # -Y 옆: x0→x1 = u
             ([2,3,7,6], front_mat, [(0,0),(1,0),(1,1),(0,1)]),   # +Y 옆: x1→x0 = u
             ([3,0,4,7], other_mat, None),
             ([4,5,6,7], other_mat, None), ([3,2,1,0], other_mat, None)]
    ob.data.materials.append(MATS[front_mat]); ob.data.materials.append(MATS[other_mat])
    for idx, mname, uv in faces:
        f = bm.faces.new([bv[i] for i in idx]); f.material_index = 0 if mname == front_mat else 1
        if uv:
            for loop, t in zip(f.loops, uv): loop[uvl].uv = t
        else:
            for loop in f.loops:
                co = loop.vert.co; loop[uvl].uv = ((co.x + co.y) * 0.5, co.z * 0.5)
    bm.normal_update(); bm.to_mesh(me); bm.free()
    return ob

# ── 그림 파사드 상가(FShop): 정면은 Kling 파사드 그림, 옆·뒤는 단색, 지붕·코니스·옆창·화분은 3D ──
def fshop(name, width, height, depth=6.0, storeys=2):
    parts = [uv_box("Body", -depth, 0.0, -width * 0.5, width * 0.5, 0.0, height, "FacadeFront", "Wall")]
    # 꼭대기 코니스(흰) + 계단식 기와 지붕(포인트색)
    parts.append(cube("Trim", 0.10, 0, height + 0.08, 0.34, width + 0.34, 0.18, "Trim"))
    for i in range(4):
        t = i / 4.0
        parts.append(cube("Roof", -depth * 0.5, 0, height + 0.17 + 0.24 * i + 0.12, (depth + 0.5) * (1 - t * 0.85), (width + 0.5) * (1 - t * 0.55), 0.24, "Roof"))
    # 현무암 기단(옆·뒤) — 정면 그림의 돌담이 옆면으로 이어진다
    for s in (-1, 1):
        parts.append(cube("Stone", -depth * 0.5, s * (width * 0.5 + 0.03), 0.55, depth + 0.02, 0.12, 1.1, "Stone"))
    parts.append(cube("Stone", -depth - 0.03, 0, 0.55, 0.12, width + 0.1, 1.1, "Stone"))
    # 모서리 기둥(흰)
    for s in (-1, 1):
        parts.append(cube("Pilaster", -0.02, s * (width * 0.5 + 0.04), height * 0.5 + 0.55, 0.14, 0.18, height - 1.1, "Trim"))
    # 바닥 화분 2개 + 문턱 콘크리트
    for s in (-1, 1):
        parts.append(cube("Frame", 0.35, s * (width * 0.5 - 0.7), 0.25, 0.5, 0.7, 0.5, "Frame"))
        parts.append(cube("Leaf", 0.35, s * (width * 0.5 - 0.7), 0.7, 0.7, 0.9, 0.5, "Leaf"))
    parts.append(cube("Concrete", 0.25, 0, 0.05, 0.5, width + 0.3, 0.10, "Concrete"))
    return join(parts, name)

MATS["Leaf"] = mat("Leaf", (0.25, 0.55, 0.28))
EXTRA_F = [("FShop_Sq", lambda: fshop("FShop_Sq", 6.4, 6.4, 6.0, 2)),
           ("FShop_Tall", lambda: fshop("FShop_Tall", 8.0, 8.0, 6.0, 3)),
           ("FShop_Wide", lambda: fshop("FShop_Wide", 9.6, 5.6, 6.0, 2)),
           ("FShop_Low", lambda: fshop("FShop_Low", 8.0, 4.0, 5.0, 1))]   # 15차-3: 1층 기와집(그림 Q/W)

EXTRA = [("House_A", lambda: jeju_house("House_A", 8.5, 6.0, False)),
         ("House_B", lambda: jeju_house("House_B", 8.0, 6.0, True)),
         ("Prop_CafeSet", lambda: cafe_set("Prop_CafeSet")),
         ("Prop_Pavilion", lambda: pavilion("Prop_Pavilion"))]

SPECS = [
    ("Shop_A", 9.6, 6.0, 2, "parapet", False, 3, True),
    ("Shop_B", 9.6, 6.0, 3, "gable",   True,  3, True),
    ("Shop_C", 9.6, 6.0, 2, "band",    True,  2, True),
    ("Shop_D", 9.6, 6.0, 3, "parapet", False, 4, True),
    ("Shop_E", 9.6, 6.0, 2, "gable",   False, 3, False),
    ("Shop_F", 9.6, 6.0, 3, "band",    True,  3, True),
]
for spec in SPECS:
    ob = shop(*spec)
    export(ob, spec[0])
for nm, fn in EXTRA + EXTRA_F:
    export(fn(), nm)
print("town kit done")
