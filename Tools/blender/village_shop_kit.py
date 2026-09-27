# 211차(사용자: 「보강해줘」 — 가게 실내 소품이 상자, 서핑 숍이 상자): 실내·관광 소품 키트. 도우미는 village_tour_kit(→cave_kit) 에서.
#   VShopCounter VDiningTable VKitchen VShelfUnit VGoodsBox VPedestal VDisplayCase VTeddy VGacha VAnvil VForge VHayBale VPenFence VSurfShack
#   앞 = Blender -Y (Unity +z). 원점 = 바닥 가운데. 런타임(VillageHubEast.DressRoom)이 원래 상자 크기에 맞춰 늘린다.
import bpy, bmesh, math, os, random
from mathutils import Vector
exec(open(r"C:\dev\game\Tools\blender\village_tour_kit.py", encoding="utf-8").read().split("TOUR_BUILDS = '''")[0], globals())
WDARK = mat("WoodDark", (0.42, 0.28, 0.16)); METALM = mat("Metal", (0.55, 0.57, 0.62)); THATCH = mat("Thatch", (0.74, 0.60, 0.36)); ROPE = mat("Rope", (0.8, 0.7, 0.45))
GLOW = mat("LampGlow", (1.0, 0.82, 0.45)); STONEF = mat("FountainStone", (0.90, 0.86, 0.78)); ORANGE = mat("BuoyOrange", (0.98, 0.45, 0.15)); LEAF = mat("CropLeaf", (0.42, 0.72, 0.32))
FUR = mat("TeddyFur", (0.72, 0.50, 0.32)); FURL = mat("TeddyFurLight", (0.92, 0.78, 0.60)); EYE = mat("GhostEye", (0.12, 0.10, 0.20))
TEAL = mat("SurfTeal", (0.40, 0.75, 0.85))
def sph(r, loc, m, sc=(1, 1, 1), seg=12):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=seg, ring_count=seg // 2 + 1, radius=r, location=loc); o = bpy.context.active_object; o.scale = sc; bpy.ops.object.transform_apply(scale=True); o.data.materials.append(m); return o
def fin(obs, name, ao=True):
    ob = join(obs, name)
    if ao: bake_ao(ob)
    export(ob, name)

def build_counter():   # 3.2 × 0.8 × 0.8
    o = [prim_box("body", (3.0, 0.7, 0.72), (0, 0, 0.36), WOODM), prim_box("top", (3.3, 0.92, 0.08), (0, 0, 0.76), WDARK)]
    for i in range(7): o.append(prim_box("plank", (0.03, 0.02, 0.62), (-1.35 + i * 0.45, -0.36, 0.38), WDARK))
    o.append(prim_box("kick", (3.0, 0.04, 0.08), (0, -0.37, 0.04), WDARK))
    o += [prim_box("reg", (0.36, 0.3, 0.2), (0.9, 0.05, 0.9), METALM), prim_box("regTop", (0.3, 0.1, 0.12), (0.9, 0.12, 1.05), CANVAS, rot=(math.radians(-25), 0, 0))]
    o.append(prim_cyl(0.07, 0.05, (-0.9, -0.15, 0.82), METALM, verts=10, r2=0.02))
    o.append(prim_cyl(0.12, 0.1, (-0.3, 0.0, 0.85), PAINT, verts=12)); o.append(sph(0.1, (-0.3, 0.0, 1.0), LEAF))
    fin(o, "VShopCounter")

def build_table():   # 1.4 × 0.9, top at 0.76 + 2 stools (outside, ±z)
    o = [prim_box("top", (1.4, 0.9, 0.07), (0, 0, 0.74), WOODM), prim_box("apron", (1.25, 0.75, 0.1), (0, 0, 0.66), WDARK)]
    for sx in (-0.6, 0.6):
        for sy in (-0.35, 0.35): o.append(prim_box("leg", (0.07, 0.07, 0.66), (sx, sy, 0.33), WDARK))
    for sy in (-0.85, 0.85):
        o += [prim_cyl(0.2, 0.06, (0, sy, 0.46), ORANGE, verts=14), prim_cyl(0.035, 0.44, (0, sy, 0.22), METALM, verts=8), prim_cyl(0.16, 0.03, (0, sy, 0.02), METALM, verts=12)]
    o += [prim_cyl(0.12, 0.08, (-0.3, 0, 0.82), PAINT, verts=14), prim_cyl(0.1, 0.02, (-0.3, 0, 0.86), mat("Soup", (0.95, 0.80, 0.55)), verts=14), prim_box("chop", (0.02, 0.3, 0.02), (0.1, 0.05, 0.79), WOODM)]
    fin(o, "VDiningTable")

def build_kitchen():   # 4.5 × 0.8 × 1.0
    o = [prim_box("body", (4.5, 0.8, 0.9), (0, 0, 0.45), CANVAS), prim_box("top", (4.6, 0.86, 0.06), (0, 0, 0.93), METALM)]
    for i in range(4): o.append(prim_box("door", (1.0, 0.02, 0.7), (-1.6 + i * 1.07, -0.41, 0.42), PAINT)); o.append(prim_box("knob", (0.2, 0.03, 0.03), (-1.6 + i * 1.07, -0.43, 0.7), METALM))
    for x in (-1.2, 0.2):
        o += [prim_cyl(0.28, 0.4, (x, 0, 1.16), METALM, verts=14), prim_cyl(0.3, 0.04, (x, 0, 1.37), METALM, verts=14)]
    o += [prim_box("board", (0.6, 0.4, 0.04), (1.4, 0, 0.98), WOODM), sph(0.09, (1.3, 0, 1.06), ORANGE), sph(0.07, (1.5, 0.05, 1.04), LEAF)]
    o.append(prim_box("hood", (1.6, 0.6, 0.3), (-0.5, 0.15, 2.1), METALM))
    fin(o, "VKitchen")

def build_shelf():   # 2.4 × 0.8 × 1.8, 3 levels with goods
    o = [prim_box("sideL", (0.06, 0.8, 1.8), (-1.2, 0, 0.9), WOODM), prim_box("sideR", (0.06, 0.8, 1.8), (1.2, 0, 0.9), WOODM), prim_box("back", (2.4, 0.04, 1.8), (0, 0.38, 0.9), WDARK)]
    random.seed(5); cols = [PRED, PBLUE, PYEL, LEAF, ORANGE, CANVAS]
    for lv in range(4):
        z = 0.08 + lv * 0.56; o.append(prim_box("board", (2.4, 0.8, 0.05), (0, 0, z), WOODM))
        if lv == 3: continue
        for k in range(6):
            m = cols[(lv * 6 + k) % len(cols)]; x = -0.95 + k * 0.38
            if k % 3 == 0: o.append(prim_cyl(0.12, 0.3, (x, -0.05, z + 0.17), m, verts=10))
            else: o.append(prim_box("g", (0.28, 0.3, 0.26 + random.random() * 0.12), (x, -0.05, z + 0.17), m))
    fin(o, "VShelfUnit")

def build_goodsbox():   # 0.8 × 0.4 × 0.5 small crate display
    o = [prim_box("crate", (0.8, 0.4, 0.3), (0, 0, 0.15), WOODM)]
    for i in range(3): o.append(prim_box("slat", (0.8, 0.02, 0.05), (0, -0.21, 0.06 + i * 0.1), WDARK))
    for k in range(4): o.append(sph(0.09, (-0.27 + k * 0.18, 0, 0.36), ORANGE if k % 2 == 0 else PYEL))
    fin(o, "VGoodsBox")

def build_pedestal():   # 0.7 × 0.7 × 0.9
    o = [prim_box("base", (0.7, 0.7, 0.1), (0, 0, 0.05), STONEF), prim_box("col", (0.55, 0.55, 0.7), (0, 0, 0.45), CANVAS), prim_box("cap", (0.7, 0.7, 0.08), (0, 0, 0.86), STONEF)]
    o.append(prim_box("plate", (0.3, 0.02, 0.1), (0, -0.28, 0.6), mat("Brass", (0.85, 0.70, 0.35))))
    fin(o, "VPedestal")

def build_case():   # 1.0 × 0.7 × 0.9 base + glass box above(0.9~1.9)
    o = [prim_box("base", (1.0, 0.7, 0.9), (0, 0, 0.45), WOODM), prim_box("trim", (1.04, 0.74, 0.06), (0, 0, 0.9), WDARK),
         prim_box("glass", (0.96, 0.66, 0.95), (0, 0, 1.4), GLASS), prim_box("topT", (1.04, 0.74, 0.05), (0, 0, 1.9), WDARK)]
    fin(o, "VDisplayCase", ao=False)

def build_teddy():   # ~0.9 high sitting teddy
    o = [sph(0.26, (0, 0, 0.3), FUR, (1, 0.9, 1.05)), sph(0.15, (0, -0.2, 0.28), FURL, (1, 0.6, 1.1)), sph(0.21, (0, 0, 0.66), FUR)]
    for s in (-1, 1):
        o += [sph(0.08, (s * 0.15, 0, 0.84), FUR), sph(0.045, (s * 0.15, -0.03, 0.85), FURL), sph(0.03, (s * 0.08, -0.19, 0.7), EYE),
              sph(0.09, (s * 0.26, -0.05, 0.36), FUR, (0.8, 0.8, 1.3)), sph(0.1, (s * 0.14, -0.18, 0.08), FUR, (1, 1.3, 0.7))]
    o += [sph(0.08, (0, -0.19, 0.62), FURL, (1.2, 0.8, 0.8)), sph(0.03, (0, -0.25, 0.64), EYE)]
    fin(o, "VTeddy")

def build_gacha():   # 0.9 × 0.7, base 0.9 + dome → 1.65
    o = [prim_box("base", (0.9, 0.7, 0.9), (0, 0, 0.45), PRED), prim_box("panel", (0.6, 0.02, 0.35), (0, -0.36, 0.55), CANVAS),
         prim_cyl(0.12, 0.06, (0, -0.38, 0.55), METALM, rot=(math.radians(90), 0, 0), verts=12), prim_box("chute", (0.3, 0.04, 0.2), (0, -0.36, 0.2), WDARK)]
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=9, radius=0.42, location=(0, 0, 1.2)); d = bpy.context.active_object; d.data.materials.append(GLASS); o.append(d)
    random.seed(9)
    for k in range(9): o.append(sph(0.1, ((random.random() - 0.5) * 0.45, (random.random() - 0.5) * 0.35, 0.98 + random.random() * 0.25), [PRED, PBLUE, PYEL, LEAF][k % 4], seg=10))
    o.append(prim_cyl(0.1, 0.08, (0, 0, 1.64), PRED, verts=12))
    fin(o, "VGacha", ao=False)

def build_anvil():   # 1.0 × 0.5 × 0.75
    o = [prim_box("stump", (0.5, 0.45, 0.4), (0, 0, 0.2), WOODM), prim_box("waist", (0.35, 0.25, 0.15), (0, 0, 0.47), METALM),
         prim_box("face", (0.75, 0.32, 0.14), (0.05, 0, 0.61), METALM), prim_cyl(0.13, 0.3, (-0.42, 0, 0.62), METALM, rot=(0, math.radians(90), 0), verts=10, r2=0.02)]
    o += [prim_box("hammerH", (0.05, 0.05, 0.35), (0.2, 0.1, 0.8), WOODM, rot=(0, math.radians(70), 0)), prim_box("hammer", (0.14, 0.08, 0.08), (0.33, 0.1, 0.76), METALM)]
    fin(o, "VAnvil")

def build_forge():   # 1.6 × 1.0 × 1.6
    o = [prim_box("body", (1.6, 1.0, 1.1), (0, 0, 0.55), STONEF), prim_box("mouth", (0.9, 0.1, 0.5), (0, -0.48, 0.65), WDARK),
         prim_box("fire", (0.7, 0.04, 0.3), (0, -0.52, 0.58), GLOW), prim_box("ledge", (1.7, 1.1, 0.1), (0, 0, 1.12), STONEF)]
    o.append(prim_cyl(0.3, 0.9, (0, 0.15, 1.6), STONEF, verts=10, r2=0.22))
    for x in (-0.4, 0.0, 0.4): o.append(sph(0.1, (x, -0.52, 0.5), ORANGE, (1, 0.6, 0.8)))
    fin(o, "VForge")

def build_hay():   # 1.2 × 0.8 × 0.8
    o = [prim_box("bale", (1.2, 0.8, 0.8), (0, 0, 0.4), THATCH)]
    for x in (-0.35, 0.35): o.append(prim_box("band", (0.05, 0.82, 0.82), (x, 0, 0.4), ROPE))
    for i in range(10): o.append(prim_box("straw", (0.02, 0.2, 0.02), (-0.5 + i * 0.11, -0.2, 0.82), THATCH, rot=(math.radians(20 + i * 7), 0, 0)))
    fin(o, "VHayBale")

def build_pen():   # 0.1 × 2.2 × 1.0 (fence panel along Y)
    o = [prim_box("postA", (0.12, 0.12, 1.0), (0, -1.05, 0.5), WDARK), prim_box("postB", (0.12, 0.12, 1.0), (0, 1.05, 0.5), WDARK)]
    for z in (0.3, 0.65, 0.95): o.append(prim_box("rail", (0.06, 2.2, 0.1), (0, 0, z), WOODM))
    o.append(prim_box("x", (0.05, 2.3, 0.08), (0, 0, 0.5), WOODM, rot=(math.radians(18), 0, 0)))
    fin(o, "VPenFence")

def build_surfshack():   # 3.6 × 2.6, 2.8 high (+ roof overhang)
    o = [prim_box("floor", (4.0, 3.0, 0.2), (0, 0, 0.1), WOODM)]
    for sx in (-1.7, 1.7):
        for sy in (-1.2, 1.2): o.append(prim_box("post", (0.14, 0.14, 2.5), (sx, sy, 1.35), WOODM))
    o += [prim_box("back", (3.4, 0.08, 2.3), (0, 1.2, 1.35), TEAL), prim_box("sideL", (0.08, 2.4, 2.3), (-1.7, 0, 1.35), TEAL),
          prim_box("counter", (3.4, 0.5, 1.0), (0, -1.1, 0.7), TEAL), prim_box("ctop", (3.6, 0.7, 0.08), (0, -1.1, 1.24), WOODM)]
    for i in range(9): o.append(prim_box("stripe", (0.18, 0.02, 0.9), (-1.6 + i * 0.4, -1.36, 0.68), CANVAS if i % 2 else PYEL))
    # 초가(야자잎) 지붕: 두 겹 박공
    bm = bmesh.new(); hw, hd, h0, h1 = 2.4, 1.9, 2.55, 3.4
    v = [bm.verts.new(p) for p in ((-hw, -hd, h0), (hw, -hd, h0), (hw, hd, h0), (-hw, hd, h0), (-hw, 0, h1), (hw, 0, h1))]
    for f in ((v[0], v[1], v[5], v[4]), (v[2], v[3], v[4], v[5]), (v[1], v[2], v[5]), (v[3], v[0], v[4])): bm.faces.new(f).smooth = False
    roof = new_obj("roof", bm, [THATCH]); sol = roof.modifiers.new("s", 'SOLIDIFY'); sol.thickness = 0.14
    bpy.context.view_layer.objects.active = roof; bpy.ops.object.modifier_apply(modifier="s"); o.append(roof)
    for i in range(12): o.append(prim_box("fringe", (0.36, 0.05, 0.3), (-2.2 + i * 0.4, -hd - 0.02, h0 - 0.12), THATCH, rot=(0, 0, math.radians((i % 3 - 1) * 6))))
    for k, m in enumerate([PRED, PBLUE, PYEL, ORANGE]):   # 서핑보드 걸이(오른쪽 바깥)
        o.append(sph(0.28, (2.25, -0.9 + k * 0.55, 1.2), m, (0.35, 0.12, 2.2), seg=12))
    o.append(prim_box("rack", (0.12, 2.4, 0.1), (2.2, -0.1, 0.4), WDARK))
    fin(o, "VSurfShack")

SHOP_BUILDS = '''
build_counter(); build_table(); build_kitchen(); build_shelf(); build_goodsbox(); build_pedestal(); build_case(); build_teddy()
build_gacha(); build_anvil(); build_forge(); build_hay(); build_pen(); build_surfshack()
'''
if bpy.app.background:
    exec(SHOP_BUILDS); print("done shop kit")
