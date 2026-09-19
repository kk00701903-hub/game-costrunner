# -*- coding: utf-8 -*-
"""하늘(주인공) 치비 3D 모델 — 첨부 시안(갈색 단발·별 머리핀·노랑 줄무늬 재킷·흰 티·청바지·흰 운동화·분홍 가방)
Humanoid 리그(Mixamo 뼈 이름)로 만들어 기존 러닝 클립(Anim_Run/Jump/Hit/Collect)을 그대로 리타겟한다.

Run:  blender -b --python haneul_chibi_rig.py -- <out_fbx> <preview_png>
Character faces -Y, Z up, ~1.05 m tall. 파츠별 강체 스키닝(관절은 캡슐 구로 겹침).
"""
import bpy, bmesh, math, sys, os
from mathutils import Vector

argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
OUT_FBX = argv[0] if argv else r'C:\dev\game\Assets\Resources\CoastRun\Rig\HaneulChibi.fbx'
OUT_PNG = argv[1] if len(argv) > 1 else r'C:\dev\game\Tools\blender\haneul_chibi_preview.png'
OUT_DIR = os.path.dirname(OUT_FBX)

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene

# ---------------------------------------------------------------- materials
MATS = {}
def mat(name, rgb, rough=0.5, metal=0.0, tex=None):
    if name in MATS: return MATS[name]
    m = bpy.data.materials.new(name); m.use_nodes = True
    nt = m.node_tree; bsdf = nt.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*rgb, 1.0)
    bsdf.inputs['Roughness'].default_value = rough
    bsdf.inputs['Metallic'].default_value = metal
    m.diffuse_color = (*rgb, 1.0)
    if tex is not None:
        img = nt.nodes.new('ShaderNodeTexImage'); img.image = tex; img.location = (-400, 300)
        nt.links.new(img.outputs['Color'], bsdf.inputs['Base Color'])
    MATS[name] = m
    return m

def stripe_image(name, w=256, h=256, base=(1.0, 0.80, 0.30), stripe=(1.0, 0.98, 0.94), period=20, width=7):
    """세로 줄무늬(u 방향) 텍스처 — UV 구/원기둥에 입히면 몸통·소매에 세로 줄이 생긴다."""
    img = bpy.data.images.new(name, w, h, alpha=False)
    px = [0.0] * (w * h * 4)
    for y in range(h):
        for x in range(w):
            c = stripe if (x % period) < width else base
            i = (y * w + x) * 4
            px[i:i + 4] = (c[0], c[1], c[2], 1.0)
    img.pixels = px
    img.filepath_raw = os.path.join(OUT_DIR, name + '.png'); img.file_format = 'PNG'; img.save()
    return img

def hair_image(name, w=1024, h=1024, base=(0.46, 0.27, 0.15)):
    """134차: 머리결 텍스처(다른 게임처럼 가닥은 지오메트리가 아니라 텍스처로) — UV 구의 u 방향(둘레)을 따라
    정수리(v=1)에서 내려오는 밝고 어두운 가닥 결 + 물결 무늬 천사링(하이라이트 띠). 색은 여기 다 넣고 재질 색은 흰색."""
    import random
    import numpy as np
    rnd = random.Random(134)
    U = (np.arange(w, dtype=np.float32) + 0.5) / w
    V = (np.arange(h, dtype=np.float32) + 0.5) / h
    uu, vv = np.meshgrid(U, V)                       # vv[y] = 행 y → v (0 = 아래, 1 = 정수리)
    mul = np.ones((h, w), dtype=np.float32)
    for _ in range(160):
        uc = rnd.random(); width = rnd.uniform(0.0025, 0.009)
        amp = rnd.choice((1, -1)) * rnd.uniform(0.05, 0.13)
        wob = rnd.uniform(0.004, 0.012); ph = rnd.uniform(0, 6.28); fr = rnd.uniform(1.5, 3.5)
        du = uu - (uc + wob * np.sin(vv * fr * 6.283 + ph))
        du = du - np.round(du)                       # u 는 둘레(랩)
        prof = np.exp(-(du / width) ** 2 * 2.0)
        fade = np.clip((0.985 - vv) / 0.05, 0, 1) * np.clip((vv - 0.05) / 0.15, 0, 1)
        mul += amp * prof * fade
    mul *= 1.0 - 0.10 * np.clip((0.45 - vv) / 0.45, 0, 1)   # 아래로 갈수록 살짝 어둡게
    col = np.stack([mul * base[0], mul * base[1], mul * base[2]], axis=-1)
    # 천사링: v0 근처 얇은 띠, 위·아래 가장자리가 지그재그(12 주기) — 크림빛 하이라이트
    v0 = 0.80; thick = 0.022
    zig = 0.012 * np.abs(((uu * 12.0) % 1.0) - 0.5) * 2.0    # 삼각파 0..0.012
    d = np.abs(vv - (v0 + zig - 0.006)) - thick
    ring = np.clip(1.0 - d / 0.010, 0, 1)
    hi = np.array((0.86, 0.66, 0.46), dtype=np.float32)
    col = col * (1 - 0.65 * ring[..., None]) + hi * (0.65 * ring[..., None])
    # 아래쪽 옅은 두 번째 띠
    d2 = np.abs(vv - (v0 - 0.075 + zig * 0.6)) - 0.010
    ring2 = np.clip(1.0 - d2 / 0.010, 0, 1)
    col = col * (1 - 0.22 * ring2[..., None]) + hi * (0.22 * ring2[..., None])
    col = np.clip(col, 0, 1)
    img = bpy.data.images.new(name, w, h, alpha=False)
    px = np.ones((h, w, 4), dtype=np.float32); px[..., :3] = col
    img.pixels = px.ravel().tolist()
    img.filepath_raw = os.path.join(OUT_DIR, name + '.png'); img.file_format = 'PNG'; img.save()
    return img

SKIN   = mat('HN_Skin',   (1.00, 0.86, 0.74), 0.6)
HAIR   = mat('HN_Hair',   (1.0, 1.0, 1.0), 0.78, tex=hair_image('HN_HairStrands'))   # 134차: 색·머리결은 텍스처에(재질색 흰색 — Unity 가 색×텍스처를 곱함)
HAIRD  = mat('HN_HairDark', (0.38, 0.21, 0.11), 0.55)
STRAND = mat('HN_Strand', (0.41, 0.235, 0.125), 0.7)   # 132차: 머리결 가닥(캡보다 살짝만 어둡게)
JACKET = mat('HN_Jacket', (1.00, 0.80, 0.30), 0.55, tex=stripe_image('HN_JacketStripes'))
YELLOW = mat('HN_Yellow', (1.00, 0.78, 0.25), 0.5)
TEE    = mat('HN_Tee',    (0.98, 0.97, 0.95), 0.6)
JEANS  = mat('HN_Jeans',  (0.24, 0.42, 0.74), 0.7)
JEANSD = mat('HN_JeansDark', (0.18, 0.32, 0.60), 0.7)
SHOE   = mat('HN_Shoe',   (0.97, 0.97, 0.98), 0.35)
SOLE   = mat('HN_Sole',   (0.92, 0.92, 0.94), 0.5)
PINK   = mat('HN_Pink',   (1.00, 0.45, 0.66), 0.5)
PINKD  = mat('HN_PinkDark', (0.90, 0.32, 0.55), 0.5)
BLUSH  = mat('HN_Blush',  (1.00, 0.62, 0.66), 0.7)
EYEW   = mat('HN_EyeWhite', (0.99, 0.99, 0.99), 0.3)
IRIS   = mat('HN_Iris',   (0.48, 0.29, 0.12), 0.3)
BLACK  = mat('HN_Black',  (0.05, 0.04, 0.04), 0.3)
WHITE  = mat('HN_White',  (1.0, 1.0, 1.0), 0.3)
MOUTH  = mat('HN_Mouth',  (0.85, 0.35, 0.40), 0.5)
SILVER = mat('HN_Silver', (0.85, 0.86, 0.90), 0.25, metal=0.8)
STAR   = mat('HN_Star',   (1.00, 0.85, 0.20), 0.5)
FLOWER = mat('HN_Flower', (1.00, 0.55, 0.70), 0.6)
LACE   = mat('HN_Lace',   (0.30, 0.50, 0.90), 0.5)

PARTS = []
def finish(ob, material, bone, smooth=True, blend=None):
    """blend=(bone2, fn): fn(world_co) -> 0..1 만큼 bone2, 나머지는 bone (머리카락 흩날림용 부분 가중치)."""
    ob.data.materials.clear(); ob.data.materials.append(material)
    if smooth:
        for p in ob.data.polygons: p.use_smooth = True
    PARTS.append((ob, bone, blend)); return ob

def sphere(name, center, radius, scale=(1, 1, 1), seg=32, rings=16, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=seg, ring_count=rings, radius=radius, location=center, rotation=rot)
    ob = bpy.context.object; ob.name = name; ob.scale = scale
    bpy.ops.object.transform_apply(scale=True, rotation=True); return ob

def cylinder(name, center, radius, depth, rot=(0, 0, 0), verts=32, scale=(1, 1, 1)):
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=radius, depth=depth, location=center, rotation=rot)
    ob = bpy.context.object; ob.name = name; ob.scale = scale
    bpy.ops.object.transform_apply(scale=True, rotation=True); return ob

def torus(name, center, major, minor, rot=(0, 0, 0), scale=(1, 1, 1), seg=48, ring=12):
    bpy.ops.mesh.primitive_torus_add(major_segments=seg, minor_segments=ring, major_radius=major, minor_radius=minor, location=center, rotation=rot)
    ob = bpy.context.object; ob.name = name; ob.scale = scale
    bpy.ops.object.transform_apply(scale=True, rotation=True); return ob

def box(name, center, size, rot=(0, 0, 0), bevel=0.0, segs=3):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=center, rotation=rot)
    ob = bpy.context.object; ob.name = name; ob.scale = size
    bpy.ops.object.transform_apply(scale=True, rotation=True)
    if bevel > 0:
        b = ob.modifiers.new('Bevel', 'BEVEL'); b.width = bevel; b.segments = segs
    return ob

def join(objs, name):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]; bpy.ops.object.join()
    ob = bpy.context.object; ob.name = name; return ob

def capsule(name, a, b, radius):
    a, b = Vector(a), Vector(b); d = b - a
    cyl = cylinder(name + '_c', (a + b) * 0.5, radius, d.length, rot=d.to_track_quat('Z', 'Y').to_euler())
    return join([cyl, sphere(name + '_a', a, radius), sphere(name + '_b', b, radius)], name)

def bisect(ob, co, no, clear_inner=True, fill=False):
    """평면(co, no) 기준으로 잘라 no 반대쪽(inner)을 지운다. fill=True 면 잘린 단면을 면으로 채운다."""
    bm = bmesh.new(); bm.from_mesh(ob.data)
    geom = bm.verts[:] + bm.edges[:] + bm.faces[:]
    res = bmesh.ops.bisect_plane(bm, geom=geom, plane_co=Vector(co) - ob.location, plane_no=Vector(no), clear_inner=clear_inner, clear_outer=not clear_inner)
    if fill:
        cut_edges = [e for e in res['geom_cut'] if isinstance(e, bmesh.types.BMEdge) and e.is_valid]
        if cut_edges: bmesh.ops.holes_fill(bm, edges=cut_edges, sides=0)
    bm.to_mesh(ob.data); bm.free()

def shrink_strip(name, center, size, target, offset=0.006, thick=0.012, subdiv=12, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_plane_add(size=1.0, location=center, rotation=rot)
    ob = bpy.context.object; ob.name = name; ob.scale = (size[0], size[1], 1)
    bpy.ops.object.transform_apply(scale=True, rotation=True)
    bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.mesh.subdivide(number_cuts=subdiv); bpy.ops.object.mode_set(mode='OBJECT')
    sw = ob.modifiers.new('Wrap', 'SHRINKWRAP'); sw.target = target; sw.wrap_method = 'NEAREST_SURFACEPOINT'; sw.offset = offset
    so = ob.modifiers.new('Solid', 'SOLIDIFY'); so.thickness = thick; so.offset = 1.0
    return ob

def swap_uv(ob):
    """UV u↔v 교환 — 원기둥에 입힌 세로줄(둘레 방향)을 축 방향 줄로 바꾼다."""
    uv = ob.data.uv_layers.active
    if uv is None: return ob
    for l in uv.data:
        u, v = l.uv; l.uv = (v, u)
    return ob

def star(name, center, r_out, r_in, thick, rot=(0, 0, 0), points=5):
    bm = bmesh.new(); vs = []
    for i in range(points * 2):
        a = math.pi / 2 + i * math.pi / points
        r = r_out if i % 2 == 0 else r_in
        vs.append(bm.verts.new((math.cos(a) * r, math.sin(a) * r, 0)))
    f = bm.faces.new(vs)
    res = bmesh.ops.extrude_face_region(bm, geom=[f])
    verts = [e for e in res['geom'] if isinstance(e, bmesh.types.BMVert)]
    bmesh.ops.translate(bm, verts=verts, vec=(0, 0, thick))
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    ob = bpy.data.objects.new(name, me); bpy.context.collection.objects.link(ob)
    ob.location = center; ob.rotation_euler = rot
    bpy.ops.object.select_all(action='DESELECT'); ob.select_set(True); bpy.context.view_layer.objects.active = ob
    bpy.ops.object.transform_apply(rotation=True)
    return ob

# ---------------------------------------------------------------- 비율 (m)
Z_HIP = 0.30
LEG_EXT = 0.05                                    # 122차: 다리를 5 cm 더 길게(다리·신발을 아래로 뻗고 전체를 올림)
HC = Vector((0, 0.0, 0.79)); HR = 0.245          # 머리 중심/반지름 (큰 치비 머리)
BODY_C = Vector((0, 0, 0.44))
SH = 0.535                                       # 어깨 높이

# ---------------------------------------------------------------- 머리·얼굴 (Head)
head = sphere('Head', HC, HR, scale=(1.0, 0.96, 1.0), seg=64, rings=32); finish(head, SKIN, 'Head')   # 122차: 세그먼트 ↑(실루엣 각짐 완화)
# 귀
for sx in (1, -1):
    e = sphere('Ear_%d' % sx, (sx * 0.235, 0.01, HC.z - 0.01), 0.035, scale=(0.6, 0.8, 1.0), seg=16, rings=10); finish(e, SKIN, 'Head')
# 눈: 흰자 + 갈색 홍채 + 검은 동공 + 하이라이트 2
for sx in (1, -1):
    ew = sphere('EyeW_%d' % sx, (sx * 0.092, -0.205, HC.z + 0.005), 0.05, scale=(1.0, 0.55, 1.2), seg=24, rings=14); finish(ew, EYEW, 'Head')
    ir = sphere('Iris_%d' % sx, (sx * 0.092, -0.236, HC.z + 0.0), 0.036, scale=(1.0, 0.45, 1.15), seg=20, rings=12); finish(ir, IRIS, 'Head')
    pu = sphere('Pupil_%d' % sx, (sx * 0.092, -0.252, HC.z - 0.004), 0.021, scale=(1.0, 0.4, 1.1), seg=16, rings=10); finish(pu, BLACK, 'Head')
    h1 = sphere('EyeHi_%d' % sx, (sx * 0.078, -0.262, HC.z + 0.022), 0.011, seg=10, rings=6); finish(h1, WHITE, 'Head')
    h2 = sphere('EyeHi2_%d' % sx, (sx * 0.105, -0.258, HC.z - 0.02), 0.006, seg=8, rings=6); finish(h2, WHITE, 'Head')
    # 눈썹 · 볼터치
    br = box('Brow_%d' % sx, (sx * 0.095, -0.222, HC.z + 0.085), (0.075, 0.012, 0.012), rot=(0, 0, math.radians(sx * -8)), bevel=0.004, segs=2); finish(br, HAIRD, 'Head', smooth=False)
    bl = sphere('Blush_%d' % sx, (sx * 0.16, -0.185, HC.z - 0.055), 0.038, scale=(1.0, 0.35, 0.7), seg=16, rings=10); finish(bl, BLUSH, 'Head')
nose = sphere('Nose', (0, -0.244, HC.z - 0.035), 0.012, seg=12, rings=8); finish(nose, SKIN, 'Head')
mouth = sphere('Mouth', (0, -0.238, HC.z - 0.085), 0.03, scale=(1.2, 0.35, 0.55), seg=16, rings=10); finish(mouth, MOUTH, 'Head')

# 머리카락: 뒤통수 캡(앞·아래는 얼굴이 보이게 기울여 잘라냄) + 앞머리 + 옆·뒷 단발
cap = sphere('HairCap', HC + Vector((0, 0.015, 0.02)), HR + 0.03, seg=64, rings=32)
# 이마 앞은 높게(눈썹 위), 뒤로 갈수록 낮게 자르는 기울인 평면
bisect(cap, HC + Vector((0, 0, 0.03)), Vector((0, 0.6, 1.0)).normalized())   # 앞(이마)은 높게, 뒤는 낮게
finish(cap, HAIR, 'Head')
bangs = sphere('Bangs', HC + Vector((0, 0.0, 0.02)), HR + 0.042, seg=64, rings=32)   # 캡보다 살짝 두껍게 → 그늘이 생겨 앞머리로 읽힘
bisect(bangs, HC + Vector((0, 0, 0.085)), Vector((0.10, 0, 1.0)).normalized())   # 눈썹 바로 위, 오른쪽(-X)이 살짝 더 길게(가르마)
bisect(bangs, HC + Vector((0, 0, 0.26)), (0, 0, -1))                 # 캡과 넉넉히 겹치게
bisect(bangs, HC + Vector((0, -0.02, 0)), (0, -1, 0))               # 앞쪽만
finish(bangs, HAIR, 'Head')
# 122차: 앞머리 끝이 헬멧 챙처럼 일직선이라 둥근 술 4개로 물결 지게
for i, tx in enumerate((-0.16, -0.055, 0.055, 0.16)):
    dz = 0.075 + (0.012 if i in (0, 3) else 0.0)
    ty = -math.sqrt(max(0.0, (HR + 0.035) ** 2 - tx * tx - dz * dz)) + 0.01
    tuft = sphere('BangTuft_%d' % i, (tx, ty, HC.z + dz), 0.05, scale=(1.0, 0.5, 0.72), seg=20, rings=12); finish(tuft, HAIR, 'Head')
back = sphere('HairBack', HC + Vector((0, 0.10, -0.05)), 0.24, scale=(1.05, 0.75, 1.0), seg=48, rings=24)
bisect(back, HC + Vector((0, 0, -0.185)), (0, 0, 1), fill=True)     # 122차: 뒷머리 단발을 턱선 위로 짧게(바닥 채움) — 126차: 125차의 20% 단축은 사용자 요청으로 원복
# 132차(사용자 「머리카락 흩날리게」): 뒷머리는 위(HC.z+0.10)에서 Head, 아래(HC.z-0.05)부터 HairBack 뼈 — 달릴 때 뒤로 날린다
def hair_ramp(top, bot):
    return lambda co: max(0.0, min(1.0, (top - co.z) / (top - bot)))
finish(back, HAIR, 'Head', blend=('HairBack', hair_ramp(HC.z + 0.16, HC.z + 0.00)))   # 134차: 더 위에서부터 흔들리게(흩날림 크게)
for sx in (1, -1):
    sd = sphere('HairSide_%d' % sx, (sx * 0.225, 0.05, HC.z - 0.07), 0.09, scale=(0.55, 1.0, 1.6), seg=24, rings=14)
    finish(sd, HAIR, 'Head', blend=('HairL' if sx > 0 else 'HairR', hair_ramp(HC.z + 0.08, HC.z - 0.10)))
# 134차: 132차의 캡슐 가닥(StrandB/Strand)은 툰 셰이더 림 때문에 검은 선으로 보여 제거 — 머리결은 HN_HairStrands 텍스처로.
# 별 머리핀(캐릭터 오른쪽 = -X) + 작은 삼각형
clip = star('HairClipStar', (-0.176, -0.203, HC.z + 0.144), 0.032, 0.014, 0.008, rot=(math.radians(70), math.radians(-25), math.radians(-10)))
finish(clip, SILVER, 'Head', smooth=False)
tri = star('HairClipTri', (-0.217, -0.175, HC.z + 0.106), 0.02, 0.012, 0.006, rot=(math.radians(70), math.radians(-30), math.radians(30)), points=3)
finish(tri, SILVER, 'Head', smooth=False)

# ---------------------------------------------------------------- 몸통 (Spine1) — 노랑 줄무늬 재킷 + 흰 티 + 옷깃
body = sphere('Body', BODY_C, 0.19, scale=(1.0, 0.85, 0.92), seg=48, rings=24); finish(body, JACKET, 'Spine1')
tee = shrink_strip('Tee', (0, -0.17, 0.44), (0.11, 0.30), body, offset=0.005, thick=0.012, subdiv=16, rot=(math.radians(90), 0, 0)); finish(tee, TEE, 'Spine1')
for sx in (1, -1):   # 재킷 앞섶(여밈 테두리)
    edge = shrink_strip('JacketEdge_%d' % sx, (sx * 0.062, -0.17, 0.44), (0.018, 0.30), body, offset=0.012, thick=0.014, subdiv=16, rot=(math.radians(90), 0, 0)); finish(edge, YELLOW, 'Spine1')
collar = torus('Collar', (0, -0.005, 0.585), 0.10, 0.026, scale=(1.05, 0.95, 0.6)); finish(collar, YELLOW, 'Spine1')
hem = torus('Hem', (0, 0, 0.305), 0.148, 0.018, scale=(1.0, 0.85, 0.7)); finish(hem, YELLOW, 'Hips')
# 작은 별·꽃 패치(가슴)
p1 = star('Patch_Star', (-0.10, -0.152, 0.50), 0.02, 0.009, 0.004, rot=(math.radians(85), 0, math.radians(15))); finish(p1, STAR, 'Spine1', smooth=False)
p2 = sphere('Patch_Flower', (0.10, -0.158, 0.47), 0.018, scale=(1, 0.3, 1), seg=12, rings=8); finish(p2, FLOWER, 'Spine1')

# ---------------------------------------------------------------- 팔 (T-포즈)
for sx, side in ((1, 'Left'), (-1, 'Right')):
    x0 = sx * 0.16; x1 = sx * 0.27; x2 = sx * 0.375
    up = capsule('UpperArm_' + side, (x0, 0, SH), (x1, 0, SH), 0.058); finish(up, JACKET, side + 'Arm')      # 원기둥 u=둘레 → 줄이 팔 방향(팔 내리면 세로줄)
    fo = capsule('ForeArm_' + side, (x1, 0, SH), (x2, 0, SH), 0.054); finish(fo, JACKET, side + 'ForeArm')
    cuff = torus('Cuff_' + side, (sx * 0.368, 0, SH), 0.055, 0.010, rot=(0, math.radians(90), 0)); finish(cuff, YELLOW, side + 'ForeArm')
    hand = sphere('Hand_' + side, (sx * 0.415, 0, SH), 0.052, scale=(1.1, 0.9, 0.95)); finish(hand, SKIN, side + 'Hand')

# ---------------------------------------------------------------- 다리·운동화
for sx, side in ((1, 'Left'), (-1, 'Right')):
    x = sx * 0.082
    E = LEG_EXT; kz = 0.18 - E * 0.5                          # 무릎은 절반만 내려 허벅지·정강이가 같이 길어진다
    th = capsule('Thigh_' + side, (x, 0, Z_HIP), (x, 0, kz), 0.066); finish(th, JEANS, side + 'UpLeg')
    sh = capsule('Shin_' + side, (x, 0, kz), (x, 0, 0.085 - E), 0.058); finish(sh, JEANS, side + 'Leg')
    cuffj = torus('JeanCuff_' + side, (x, 0, 0.095 - E), 0.058, 0.010); finish(cuffj, JEANSD, side + 'Leg')
    # 123차(사용자 「운동화가 네모같다」): 발등은 뒤꿈치→발끝 캡슐(바닥만 평평), 밑창은 타원 원기둥 — 위에서 봐도 네 귀퉁이가 없다
    shoe = capsule('Shoe_' + side, (x, 0.04, 0.054 - E), (x, -0.085, 0.046 - E), 0.052)
    bisect(shoe, (x, 0, 0.016 - E), (0, 0, 1), fill=True); finish(shoe, SHOE, side + 'Foot')
    sole = cylinder('Sole_' + side, (x, -0.025, 0.011 - E), 1.0, 0.022, verts=40, scale=(0.062, 0.118, 1.0))
    sb = sole.modifiers.new('Bevel', 'BEVEL'); sb.width = 0.009; sb.segments = 3
    finish(sole, SOLE, side + 'Foot')
    tongue = sphere('ShoeTop_' + side, (x, -0.035, 0.099 - E), 1.0, scale=(0.030, 0.046, 0.012), seg=24, rings=12); finish(tongue, YELLOW, side + 'Foot')   # 발등 위 노란 포인트
    band = torus('ShoeBand_' + side, (x, 0.032, 0.088 - E), 0.040, 0.011, scale=(1.0, 1.0, 0.6)); finish(band, YELLOW, side + 'Foot')                 # 발목 노란 띠
    lace = box('Lace_' + side, (x, -0.06, 0.104 - E), (0.044, 0.036, 0.010), bevel=0.004, segs=2); finish(lace, LACE, side + 'Foot', smooth=False)
    fl = sphere('ShoeFlower_' + side, (x + sx * 0.058, -0.02, 0.06 - E), 0.014, scale=(0.35, 1, 1), seg=12, rings=8); finish(fl, FLOWER, side + 'Foot')

# ---------------------------------------------------------------- 분홍 가방 (Spine1)
pack = box('Pack', (0, 0.205, 0.45), (0.21, 0.11, 0.25), bevel=0.05, segs=7); finish(pack, PINK, 'Spine1')   # 122차: 더 둥글게
pocket = box('PackPocket', (0, 0.268, 0.39), (0.145, 0.04, 0.09), bevel=0.019, segs=5); finish(pocket, PINKD, 'Spine1')
pflap = box('PackFlap', (0, 0.272, 0.425), (0.15, 0.036, 0.03), bevel=0.01, segs=3); finish(pflap, PINK, 'Spine1')
pstar = star('PackStar', (-0.035, 0.293, 0.40), 0.018, 0.008, 0.004, rot=(math.radians(-90), 0, 0)); finish(pstar, STAR, 'Spine1', smooth=False)
for (fx, fz, r) in ((0.05, 0.52, 0.02), (-0.05, 0.50, 0.016), (0.055, 0.37, 0.015), (-0.06, 0.55, 0.012)):
    f = sphere('PackFlower', (fx, 0.263, fz), r, scale=(1, 0.35, 1), seg=12, rings=8); finish(f, FLOWER, 'Spine1')
pom = sphere('PackPom', (-0.085, 0.24, 0.33), 0.018, seg=12, rings=8); finish(pom, PINKD, 'Spine1')
for sx in (1, -1):   # 어깨끈
    st = capsule('Strap_%d' % sx, (sx * 0.075, 0.12, 0.58), (sx * 0.085, -0.13, 0.50), 0.014); finish(st, PINK, 'Spine1')
    st2 = capsule('StrapBack_%d' % sx, (sx * 0.075, 0.12, 0.58), (sx * 0.075, 0.16, 0.36), 0.012); finish(st2, PINK, 'Spine1')

# ---------------------------------------------------------------- 모디파이어 적용 · 강체 그룹 · 결합
bpy.ops.object.select_all(action='DESELECT')
for ob, bone, blend in PARTS:
    bpy.context.view_layer.objects.active = ob
    for m in list(ob.modifiers):
        try: bpy.ops.object.modifier_apply(modifier=m.name)
        except Exception as e: print('modifier apply failed', ob.name, m.name, e)
    if blend is None:
        vg = ob.vertex_groups.new(name=bone); vg.add(list(range(len(ob.data.vertices))), 1.0, 'REPLACE')
    else:
        bone2, fn = blend
        vg1 = ob.vertex_groups.new(name=bone); vg2 = ob.vertex_groups.new(name=bone2)
        mw = ob.matrix_world
        for v in ob.data.vertices:
            w = fn(mw @ v.co)
            vg1.add([v.index], 1.0 - w, 'REPLACE'); vg2.add([v.index], w, 'REPLACE')
girl = join([ob for ob, _, _ in PARTS], 'HaneulChibi_Mesh')

# ---------------------------------------------------------------- 아마추어 (Mixamo 이름)
bpy.ops.object.armature_add(enter_editmode=True, location=(0, 0, 0))
arm = bpy.context.object; arm.name = 'HaneulChibi'; arm.data.name = 'HaneulChibiArmature'
eb = arm.data.edit_bones
for b in list(eb): eb.remove(b)
def bone(name, head, tail, parent=None, connect=False):
    b = eb.new(name); b.head = head; b.tail = tail
    if parent: b.parent = eb[parent]; b.use_connect = connect
    return b
bone('Hips',   (0, 0, Z_HIP), (0, 0, 0.38))
bone('Spine',  (0, 0, 0.38), (0, 0, 0.46), 'Hips', True)
bone('Spine1', (0, 0, 0.46), (0, 0, 0.53), 'Spine', True)
bone('Spine2', (0, 0, 0.53), (0, 0, 0.585), 'Spine1', True)
bone('Neck',   (0, 0, 0.585), (0, 0, 0.62), 'Spine2', True)
bone('Head',   (0, 0, 0.62), (0, 0, 0.95), 'Neck', True)
bone('HeadTop_End', (0, 0, 0.95), (0, 0, 1.06), 'Head', True)
# 132차: 머리카락 뼈(Humanoid 밖 — Unity SkaterRig 가 LateUpdate 에서 스프링으로 흔든다)
bone('HairBack', (0, 0.12, HC.z + 0.12), (0, 0.20, HC.z - 0.18), 'Head')
bone('HairL', (0.20, 0.05, HC.z + 0.05), (0.24, 0.05, HC.z - 0.20), 'Head')
bone('HairR', (-0.20, 0.05, HC.z + 0.05), (-0.24, 0.05, HC.z - 0.20), 'Head')
for sx, side in ((1, 'Left'), (-1, 'Right')):
    bone(side + 'Shoulder', (sx * 0.05, 0, 0.555), (sx * 0.16, 0, SH), 'Spine2')
    bone(side + 'Arm',      (sx * 0.16, 0, SH), (sx * 0.27, 0, SH), side + 'Shoulder', True)
    bone(side + 'ForeArm',  (sx * 0.27, 0, SH), (sx * 0.375, 0, SH), side + 'Arm', True)
    bone(side + 'Hand',     (sx * 0.375, 0, SH), (sx * 0.46, 0, SH), side + 'ForeArm', True)
    E = LEG_EXT; kz = 0.18 - E * 0.5
    bone(side + 'UpLeg',    (sx * 0.082, 0, Z_HIP), (sx * 0.082, 0.005, kz), 'Hips')
    bone(side + 'Leg',      (sx * 0.082, 0.005, kz), (sx * 0.082, 0, 0.07 - E), side + 'UpLeg', True)
    bone(side + 'Foot',     (sx * 0.082, 0, 0.07 - E), (sx * 0.082, -0.09, 0.02 - E), side + 'Leg', True)
    bone(side + 'ToeBase',  (sx * 0.082, -0.09, 0.02 - E), (sx * 0.082, -0.15, 0.02 - E), side + 'Foot', True)
bpy.ops.object.mode_set(mode='OBJECT')
# 122차: 다리를 아래로 늘린 만큼 전체를 올려 발바닥을 z=0 에 맞춘다(위치 적용으로 뼈·정점에 굽기)
for o in (girl, arm):
    bpy.ops.object.select_all(action='DESELECT'); o.select_set(True); bpy.context.view_layer.objects.active = o
    o.location.z += LEG_EXT; bpy.ops.object.transform_apply(location=True)
girl.parent = arm
md = girl.modifiers.new('Armature', 'ARMATURE'); md.object = arm
bpy.ops.object.select_all(action='DESELECT'); arm.select_set(True); girl.select_set(True); bpy.context.view_layer.objects.active = arm

# ---------------------------------------------------------------- export
os.makedirs(OUT_DIR, exist_ok=True)
bpy.ops.export_scene.fbx(filepath=OUT_FBX, use_selection=True, object_types={'ARMATURE', 'MESH'},
    apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y', add_leaf_bones=False, bake_anim=False,
    use_mesh_modifiers=True, mesh_smooth_type='FACE', path_mode='COPY', embed_textures=False, armature_nodetype='NULL',
    primary_bone_axis='Y', secondary_bone_axis='X', use_armature_deform_only=True)
print('FBX ->', OUT_FBX)

# ---------------------------------------------------------------- preview
bpy.ops.object.camera_add(location=(1.7, -2.5, 1.0), rotation=(math.radians(80), 0, math.radians(34)))
cam = bpy.context.object; cam.data.lens = 60; scene.camera = cam
bpy.ops.object.light_add(type='SUN', location=(2, -3, 5), rotation=(math.radians(45), math.radians(20), math.radians(30))); bpy.context.object.data.energy = 3.0
scene.render.engine = 'BLENDER_WORKBENCH'
scene.display.shading.light = 'STUDIO'; scene.display.shading.color_type = 'TEXTURE'
scene.display.shading.show_shadows = True; scene.display.shading.show_cavity = True
scene.render.resolution_x = 720; scene.render.resolution_y = 960
world = bpy.data.worlds.new('W'); scene.world = world; world.color = (0.82, 0.82, 0.84)
scene.render.filepath = OUT_PNG; bpy.ops.render.render(write_still=True)
cam.location = (0, -3.0, 0.64); cam.rotation_euler = (math.radians(87), 0, 0)
scene.render.filepath = OUT_PNG.replace('.png', '_front.png'); bpy.ops.render.render(write_still=True)
cam.location = (0.55, -0.75, 0.30); cam.rotation_euler = (math.radians(70), 0, math.radians(36)); cam.data.lens = 70
scene.render.filepath = OUT_PNG.replace('.png', '_shoes.png'); bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(os.path.dirname(OUT_PNG), 'haneul_chibi_rig.blend'))
print('PNG ->', OUT_PNG)
