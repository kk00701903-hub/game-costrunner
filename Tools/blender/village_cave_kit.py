# 206차(사용자: 「동굴 등 UI 가 아직 모자란 부분은 파이어플라이·블렌더 등 수정해서 만들어줘」): 광산 던전(깊은 갱도) 키트.
#   VCaveWall_A/B/C   벽 바위 덩어리(원점 = 바닥 중앙, 지름 ≈ 2 m, 높이 ≈ 1 m — 런타임에 칸 크기·높이로 늘림)
#   VCaveStalag_A/B   석순 무리(장식)
#   VCaveCrystal_A/B  광맥(바위 + 결정 기둥) — 결정 재질 CaveCrystal 은 런타임에 광석 색으로 바꾼다
#   VMineBeam         갱도 버팀목(기둥 둘 + 들보)
#   VMineLamp         갱도 등불(나무 기둥 + 매단 랜턴)
#   VMineCart         광차(바퀴 + 나무 상자 + 광석 더미)
#   재질 이름 → JejuKit.MaterialFor: CaveRock(Firefly 타일 Tex_CaveRock) / Moss / Ore / CaveCrystal / MineWood / MineMetal / LampGlow
# 실행: Blender 에서 exec(open(r"C:\dev\game\Tools\blender\village_cave_kit.py", encoding="utf-8").read()) 또는 blender -b --python
import bpy, bmesh, math, os, random
from mathutils import Vector, noise

OUT = r"C:\dev\game\Assets\Resources\CoastRun\Models"
os.makedirs(OUT, exist_ok=True)
if bpy.app.background: bpy.ops.wm.read_factory_settings(use_empty=True)
else:
    for o in list(bpy.data.objects): bpy.data.objects.remove(o, do_unlink=True)   # 열린 Blender(MCP)에선 초기화 대신 비우기

def mat(name, rgb):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True; b = m.node_tree.nodes.get("Principled BSDF")
    if b: b.inputs["Base Color"].default_value = (*rgb, 1.0)
    return m
ROCK = mat("CaveRock", (0.46, 0.40, 0.36)); MOSS = mat("Moss", (0.42, 0.66, 0.30)); ORE = mat("Ore", (1.0, 0.82, 0.25))
CRYS = mat("CaveCrystal", (0.72, 0.52, 0.98)); WOOD = mat("MineWood", (0.58, 0.40, 0.24)); METAL = mat("MineMetal", (0.40, 0.42, 0.46)); GLOW = mat("LampGlow", (1.0, 0.82, 0.45))

def new_obj(name, bm, mats):
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    for m in mats: me.materials.append(m)
    ob = bpy.data.objects.new(name, me); bpy.context.scene.collection.objects.link(ob); return ob

def rock_bm(seed, rx, ry, rz, facets=2, rough=0.22, moss=True, flat_bottom=True):
    """이코스피어를 노이즈로 울퉁불퉁하게 + 아래 납작 + 위쪽 면은 이끼(재질 1)."""
    random.seed(seed); bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=facets, radius=1.0)
    off = Vector((random.random() * 50, random.random() * 50, random.random() * 50))
    for v in bm.verts:
        n = noise.noise(v.co * 1.6 + off) * rough + noise.noise(v.co * 4.0 + off) * rough * 0.35
        v.co = v.co * (1.0 + n)
        v.co.x *= rx; v.co.y *= ry; v.co.z *= rz
        if flat_bottom and v.co.z < -rz * 0.35: v.co.z = -rz * 0.35 + (v.co.z + rz * 0.35) * 0.15
    # 바닥을 0 으로
    zmin = min(v.co.z for v in bm.verts)
    for v in bm.verts: v.co.z -= zmin
    bm.faces.ensure_lookup_table()
    for f in bm.faces:
        f.smooth = False
        f.material_index = 1 if (moss and f.normal.z > 0.55 and random.random() < 0.85) else 0
    return bm

def crystals(bm_parent_ob, seed, n, height):
    random.seed(seed + 7); bm = bmesh.new()
    for i in range(n):
        a = i / n * math.pi * 2 + random.random() * 0.6; r = 0.35 + random.random() * 0.25
        base = Vector((math.cos(a) * r, math.sin(a) * r, height * (0.45 + random.random() * 0.3)))
        sz = 0.10 + random.random() * 0.07
        # 6각 기둥 결정(위로 뾰족)
        verts = [bm.verts.new(base + Vector((math.cos(k * math.pi / 3) * sz, math.sin(k * math.pi / 3) * sz, 0))) for k in range(6)]
        tip = bm.verts.new(base + Vector((0, 0, sz * 2.6))); bot = bm.verts.new(base - Vector((0, 0, sz * 0.5)))
        for k in range(6):
            bm.faces.new((verts[k], verts[(k + 1) % 6], tip)); bm.faces.new((verts[(k + 1) % 6], verts[k], bot))
        # 바깥으로 살짝 기울이기
        tilt = Vector((math.cos(a), math.sin(a), 0)) * 0.12
        tip.co += tilt
    for f in bm.faces: f.smooth = False; f.material_index = 0
    return bm

def unwrap(ob, scale=1.6):
    """162차: 클링 타일 텍스처(Tex_Basalt/Moss/Leaf)용 UV — 스마트 프로젝션, 1 m ≈ scale 타일."""
    bpy.ops.object.select_all(action='DESELECT'); ob.select_set(True); bpy.context.view_layer.objects.active = ob
    if not ob.data.uv_layers: ob.data.uv_layers.new(name="UVMap")
    bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT')
    try: bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=0.02, scale_to_bounds=False)
    except Exception as e: print("[warn] unwrap", ob.name, e)
    bpy.ops.object.mode_set(mode='OBJECT')
    uv = ob.data.uv_layers.active
    if uv:
        for l in uv.data: l.uv = (l.uv[0] * scale, l.uv[1] * scale)

def bake_ao(ob):
    unwrap(ob)
    sc = bpy.context.scene
    sc.render.engine = 'CYCLES'; sc.cycles.samples = 24; sc.cycles.device = 'CPU'
    sc.render.bake.target = 'VERTEX_COLORS'
    if sc.world is None: sc.world = bpy.data.worlds.new("W")
    sc.world.use_nodes = True; bg = sc.world.node_tree.nodes.get("Background")
    if bg: bg.inputs[0].default_value = (1, 1, 1, 1)
    bpy.ops.object.select_all(action='DESELECT'); ob.select_set(True); bpy.context.view_layer.objects.active = ob
    me = ob.data
    col = me.color_attributes.get("Col") or me.color_attributes.new(name="Col", type='BYTE_COLOR', domain='CORNER')
    me.color_attributes.active_color = col
    try: bpy.ops.object.bake(type='AO')
    except Exception as e: print("[warn] bake", ob.name, e); return
    vn = [v.normal.copy() for v in me.vertices]
    for poly in me.polygons:
        for li in poly.loop_indices:
            lp = me.loops[li]; ao = col.data[li].color[0]
            edge = 1.0 - max(0.0, vn[lp.vertex_index].dot(poly.normal))
            k = 0.66 + 0.34 * (ao ** 0.8) + 0.08 * min(1.0, edge * 2.0)
            col.data[li].color = (k, k * 0.985, k * 0.97, 1.0)

def export(ob, name):
    bpy.ops.object.select_all(action='DESELECT'); ob.select_set(True); bpy.context.view_layer.objects.active = ob
    path = os.path.join(OUT, name + ".fbx")
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL',
        bake_space_transform=True, axis_forward='-Z', axis_up='Y', object_types={'MESH'}, colors_type='SRGB',
        mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False, path_mode='STRIP', embed_textures=False)
    print("exported", path)
    bpy.data.objects.remove(ob, do_unlink=True)


def join(obs, name):
    bpy.ops.object.select_all(action='DESELECT')
    for o in obs: o.select_set(True)
    bpy.context.view_layer.objects.active = obs[0]; bpy.ops.object.join()
    ob = bpy.context.view_layer.objects.active; ob.name = name; return ob

def prim_box(name, size, loc, m, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc, rotation=rot)
    ob = bpy.context.active_object; ob.scale = size; bpy.ops.object.transform_apply(scale=True, rotation=True, location=False)
    ob.data.materials.append(m); return ob
def prim_cyl(r, h, loc, m, rot=(0, 0, 0), verts=10, r2=None):
    if r2 is None: bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=r, depth=h, location=loc, rotation=rot)
    else: bpy.ops.mesh.primitive_cone_add(vertices=verts, radius1=r, radius2=r2, depth=h, location=loc, rotation=rot)
    ob = bpy.context.active_object; bpy.ops.object.transform_apply(rotation=True); ob.data.materials.append(m); return ob

def build_wall(name, seed, rx, ry, rz, rough):
    bm = rock_bm(seed, rx, ry, rz, 2, rough, moss=True)
    for f in bm.faces:   # 이끼는 꼭대기 일부만
        if f.material_index == 1 and random.random() < 0.55: f.material_index = 0
    ob = new_obj(name, bm, [ROCK, MOSS]); bake_ao(ob); export(ob, name)

def build_stalag(name, seed, n):
    random.seed(seed); obs = []
    for i in range(n):
        a = random.random() * math.pi * 2; r = 0.0 if i == 0 else 0.25 + random.random() * 0.25
        h = (1.0 if i == 0 else 0.45 + random.random() * 0.4)
        obs.append(prim_cyl(0.20 * (1.0 if i == 0 else 0.7), h, (math.cos(a) * r, math.sin(a) * r, h / 2), ROCK, verts=7, r2=0.02))
    bm0 = rock_bm(seed + 3, 0.45, 0.42, 0.18, 1, 0.2, moss=False); base = new_obj(name + "_b", bm0, [ROCK]); obs.insert(0, base)
    ob = join(obs, name); bake_ao(ob); export(ob, name)

def build_crystal(name, seed, n):
    bm = rock_bm(seed, 0.60, 0.52, 0.42, 2, 0.22, moss=False); ob = new_obj(name, bm, [ROCK])
    cb = crystals(ob, seed, n, 0.42); cob = new_obj(name + "_c", cb, [CRYS])
    for v in cob.data.vertices: v.co *= 1.6; v.co.z -= 0.2
    ob = join([ob, cob], name); bake_ao(ob); export(ob, name)

def build_beam():
    obs = [prim_box("p1", (0.22, 0.22, 2.4), (-1.0, 0, 1.2), WOOD), prim_box("p2", (0.22, 0.22, 2.4), (1.0, 0, 1.2), WOOD),
           prim_box("t", (2.5, 0.26, 0.26), (0, 0, 2.45), WOOD),
           prim_box("b1", (0.12, 0.12, 0.7), (-0.78, 0, 2.15), WOOD, rot=(0, math.radians(45), 0)),
           prim_box("b2", (0.12, 0.12, 0.7), (0.78, 0, 2.15), WOOD, rot=(0, math.radians(-45), 0))]
    ob = join(obs, "VMineBeam"); bake_ao(ob); export(ob, "VMineBeam")

def build_lamp():
    obs = [prim_box("post", (0.16, 0.16, 2.2), (0, 0, 1.1), WOOD), prim_box("arm", (0.7, 0.12, 0.12), (0.3, 0, 2.15), WOOD),
           prim_cyl(0.015, 0.25, (0.6, 0, 2.0), METAL, verts=6),
           prim_cyl(0.13, 0.05, (0.6, 0, 1.87), METAL, verts=8), prim_cyl(0.11, 0.24, (0.6, 0, 1.73), GLOW, verts=10),
           prim_cyl(0.13, 0.05, (0.6, 0, 1.59), METAL, verts=8)]
    ob = join(obs, "VMineLamp"); bake_ao(ob); export(ob, "VMineLamp")

def build_cart():
    obs = [prim_box("box", (1.2, 0.8, 0.5), (0, 0, 0.55), WOOD), prim_box("rim", (1.26, 0.86, 0.08), (0, 0, 0.82), METAL)]
    for sx in (-0.4, 0.4):
        for sy in (-0.42, 0.42): obs.append(prim_cyl(0.18, 0.08, (sx, sy, 0.2), METAL, rot=(math.radians(90), 0, 0), verts=12))
    random.seed(77)
    for i in range(7):
        bm = rock_bm(80 + i, 0.18, 0.16, 0.14, 1, 0.3, moss=False)
        o = new_obj("ore%d" % i, bm, [ORE if i % 2 else ROCK])
        for v in o.data.vertices: v.co += Vector(((random.random() - 0.5) * 0.8, (random.random() - 0.5) * 0.5, 0.78))
        obs.append(o)
    ob = join(obs, "VMineCart"); bake_ao(ob); export(ob, "VMineCart")

CAVE_BUILDS = '''
build_wall("VCaveWall_A", 101, 1.0, 0.95, 1.0, 0.22)
build_wall("VCaveWall_B", 102, 0.95, 1.0, 1.1, 0.26)
build_wall("VCaveWall_C", 103, 1.05, 0.9, 0.9, 0.18)
build_stalag("VCaveStalag_A", 111, 4)
build_stalag("VCaveStalag_B", 112, 3)
build_crystal("VCaveCrystal_A", 121, 5)
build_crystal("VCaveCrystal_B", 122, 4)
build_beam(); build_lamp(); build_cart()

'''
if bpy.app.background:
    exec(CAVE_BUILDS); print("done cave kit")
