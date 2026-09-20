# 162차(사용자: 「바위하고 원형으로 된 장애물 등 블렌더로 조금 더 이쁘게」): 마을의 구(Sphere) 바위·덤불을 스타일라이즈드 저폴리 키트로.
#   VRock_A/B/C  캘 수 있는 바위(각진 현무암 + 이끼 캡 + 금빛 광석 결정)
#   VStone_A/B   작은 장식 바위(각진 현무암 + 이끼)
#   VCliff_A/B   절벽 바위(크고 납작, 물가)
#   VBush_A/B    둥근 덤불(잎 뭉치 3~5 + 열매) — 「원형 장애물」
#   원점 = 바닥 중앙, 1 unit = 1 m, 높이 ≈ 1 m(런타임 scale 로 조절). 재질 이름 → JejuKit.MaterialFor: Basalt / Moss / Ore / LeafBush / Berry
#   Cycles AO 를 정점색(Col)으로 구워 CoastToon _VertexColor 로 곱해진다(151차 파이프라인).
# 실행: blender -b --python village_rock_kit.py
import bpy, bmesh, math, os, random
from mathutils import Vector, noise

OUT = r"C:\dev\game\Assets\Resources\CoastRun\Models"
os.makedirs(OUT, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)

def mat(name, rgb):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True; b = m.node_tree.nodes.get("Principled BSDF")
    if b: b.inputs["Base Color"].default_value = (*rgb, 1.0)
    return m
BASALT = mat("Basalt", (0.36, 0.38, 0.42)); MOSS = mat("Moss", (0.42, 0.66, 0.30)); ORE = mat("Ore", (1.0, 0.82, 0.25))
LEAF = mat("LeafBush", (0.35, 0.62, 0.30)); BERRY = mat("Berry", (0.98, 0.42, 0.55))

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

def bush_bm(seed, clumps, size):
    random.seed(seed); bm = bmesh.new()
    for i in range(clumps):
        a = i / clumps * math.pi * 2; r = 0.0 if i == 0 else 0.28 * size
        c = Vector((math.cos(a) * r, math.sin(a) * r, 0)); s = size * (0.55 if i else 0.62) * (0.9 + random.random() * 0.25)
        g = bmesh.ops.create_icosphere(bm, subdivisions=2, radius=s)
        off = Vector((random.random() * 30,) * 3)
        for v in g['verts']:
            v.co = v.co * (1.0 + noise.noise(v.co * 3.0 + off) * 0.18); v.co.z *= 0.82; v.co += c + Vector((0, 0, s * 0.75))
    zmin = min(v.co.z for v in bm.verts)
    for v in bm.verts: v.co.z -= zmin
    for f in bm.faces: f.smooth = True; f.material_index = 0
    # 열매(작은 구) 재질 1
    top = max(v.co.z for v in bm.verts)
    for i in range(5):
        a = random.random() * math.pi * 2; r = 0.30 * size + random.random() * 0.25 * size
        p = Vector((math.cos(a) * r, math.sin(a) * r, top * (0.55 + random.random() * 0.35)))
        g = bmesh.ops.create_icosphere(bm, subdivisions=1, radius=0.06 * size)
        vs = set(v.index for v in g['verts']) if False else set(id(v) for v in g['verts'])
        for v in g['verts']: v.co += p
        for f in bm.faces:
            if all(id(v) in vs for v in f.verts): f.material_index = 1; f.smooth = True
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

def build_rock(name, seed, rx, ry, rz, ore, facets=2, rough=0.22, moss=True):
    bm = rock_bm(seed, rx, ry, rz, facets, rough, moss)
    ob = new_obj(name, bm, [BASALT, MOSS]); ob.name = name
    if ore:
        cb = crystals(ob, seed, ore, rz); cob = new_obj(name + "_Ore", cb, [ORE])
        bpy.ops.object.select_all(action='DESELECT'); ob.select_set(True); cob.select_set(True); bpy.context.view_layer.objects.active = ob
        # 결정은 재질 2 로 합침
        cob.data.materials.clear(); cob.data.materials.append(ORE)
        bpy.ops.object.join(); ob = bpy.context.view_layer.objects.active
        # join 뒤 재질 슬롯 정리: Basalt, Moss, Ore
    bake_ao(ob); export(ob, name)

def build_bush(name, seed, clumps, size):
    bm = bush_bm(seed, clumps, size); ob = new_obj(name, bm, [LEAF, BERRY]); bake_ao(ob); export(ob, name)

build_rock("VRock_A", 11, 0.62, 0.52, 0.50, ore=3)
build_rock("VRock_B", 12, 0.55, 0.60, 0.58, ore=4, rough=0.26)
build_rock("VRock_C", 13, 0.70, 0.48, 0.44, ore=3, rough=0.18)
build_rock("VStone_A", 21, 0.42, 0.34, 0.24, ore=0, facets=1, rough=0.20)
build_rock("VStone_B", 22, 0.36, 0.40, 0.30, ore=0, facets=2, rough=0.24)
build_rock("VCliff_A", 31, 0.75, 0.65, 0.55, ore=0, facets=2, rough=0.30, moss=False)
build_rock("VCliff_B", 32, 0.60, 0.80, 0.62, ore=0, facets=2, rough=0.28, moss=False)
build_bush("VBush_A", 41, 4, 0.55)
build_bush("VBush_B", 42, 5, 0.62)
print("done")
