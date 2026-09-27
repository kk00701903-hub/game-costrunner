# 207차(사용자: 「건물이나 에셋등에 부족한 부분 있으면 다 점검해서 파이어플라이등 활용해서 수정해줘」): 관광단지·시내·정류장 소품 키트.
#   Prop_StoneWall  제주 돌담(길이 6 m = Blender Y, 높이 0.85, 두께 0.45) — 네모 블록 → 둥근 현무암 쌓기(구멍 숭숭). 재질 이름 Stone 그대로.
#   VParasol_A/B/C  해변 파라솔(줄무늬 캔버스 + 흰 기둥)   VSunbed  나무 선베드
#   VGreenhouse     식물원 유리 온실 돔(지름 12 m, 흰 뼈대 + 유리)
#   VBusShelter     버스 정류장(파란 틀 · 둥근 지붕 · 유리 · Firefly 포스터 PosterJeju · 나무 벤치) — 앞 = Blender -Y
#   VFountain       광장 분수(팔각 수반 2단)
#   VHotel          리조트 호텔(10×7 m, 4층, 발코니 · 귤색 지붕)
#   VFallsPool      폭포 못 둘레 현무암 테(반지름 4.6 m, -X 쪽 비움)
# 실행(열린 Blender MCP): g={}; exec(open(this).read(), g); exec('build_…()', g)
import bpy, bmesh, math, os, random
from mathutils import Vector
_cave = r"C:\dev\game\Tools\blender\village_cave_kit.py"
exec(open(_cave, encoding="utf-8").read().split("CAVE_BUILDS = '''")[0], globals())   # 도우미(rock_bm·new_obj·bake_ao·export·join·prim_box·prim_cyl·unwrap)

STONE = mat("Stone", (0.30, 0.30, 0.32)); BASALT = mat("Basalt", (0.35, 0.35, 0.37))
PRED = mat("ParasolRed", (0.95, 0.40, 0.40)); PBLUE = mat("ParasolBlue", (0.35, 0.62, 0.95)); PYEL = mat("ParasolYellow", (1.0, 0.80, 0.30))
CANVAS = mat("CanvasWhite", (0.97, 0.96, 0.92)); WOODM = mat("Wood", (0.55, 0.38, 0.22)); PAINT = mat("PaintWhite", (0.96, 0.96, 0.94))
GLASS = mat("GlassTint", (0.75, 0.92, 1.0)); BUS = mat("BusBody", (0.20, 0.56, 0.82)); POSTER = mat("PosterJeju", (1, 1, 1))
FSTONE = mat("FountainStone", (0.90, 0.86, 0.78)); FWATER = mat("FountainWater", (0.55, 0.85, 1.0))
HWALL = mat("HotelWall", (0.97, 0.95, 0.90)); HROOF = mat("HotelRoof", (0.95, 0.55, 0.22)); HWIN = mat("Glass", (0.55, 0.78, 0.95)); AWN = mat("Awning_Orange", (0.98, 0.6, 0.2))

def ring_cone(name, r, h, seg, mats_cycle, z0):
    """꼭짓점이 위인 원뿔 캔버스 — 조각마다 재질을 번갈아(줄무늬), 가장자리는 살짝 늘어진 물결."""
    bm = bmesh.new(); tip = bm.verts.new((0, 0, z0 + h)); tip2 = bm.verts.new((0, 0, z0 + h - 0.02)); ring = []; ring2 = []
    for i in range(seg):
        a = i / seg * math.pi * 2; sag = 0.06 if i % 2 else 0.0
        ring.append(bm.verts.new((math.cos(a) * r, math.sin(a) * r, z0 - sag)))
        ring2.append(bm.verts.new((math.cos(a) * r * 0.99, math.sin(a) * r * 0.99, z0 - sag - 0.02)))
    for i in range(seg):
        f = bm.faces.new((ring[i], ring[(i + 1) % seg], tip)); f.material_index = (i // 2) % len(mats_cycle); f.smooth = False
        f2 = bm.faces.new((ring2[(i + 1) % seg], ring2[i], tip2)); f2.material_index = f.material_index; f2.smooth = False   # 아래에서 보아도 보이게(안쪽 면)
    return new_obj(name, bm, mats_cycle)

# ── 돌담 ─────────────────────────────────────────
def build_batdam(name="Prop_StoneWall", length=6.0, height=0.85, thick=0.45, seed=207):
    random.seed(seed); obs = []; rows = 3
    for r in range(rows):
        n = 11 if r < 2 else 10; step = length / n
        for c in range(n):
            if r == 2 and random.random() < 0.18: continue          # 윗줄은 군데군데 빈 틈
            sy = step * (0.52 + random.random() * 0.12); sz = height / rows * (0.62 + random.random() * 0.18)
            bm = rock_bm(seed * 10 + r * 40 + c, thick * 0.5 * (0.9 + random.random() * 0.2), sy, sz, 2, 0.2, moss=False)
            o = new_obj("s", bm, [STONE])
            dy = -length / 2 + (c + 0.5) * step + (step * 0.5 if r == 1 else 0) + (random.random() - 0.5) * 0.08
            if dy > length / 2 - step * 0.3: dy -= length
            dz = r * height / rows * 0.95 + (random.random() - 0.5) * 0.03
            for v in o.data.vertices: v.co += Vector(((random.random() - 0.5) * 0.06, dy, dz))
            obs.append(o)
    ob = join(obs, name); unwrap(ob, 1.2); bake_ao_keep_uv(ob); export(ob, name)

def bake_ao_keep_uv(ob):
    # bake_ao 는 unwrap 을 다시 하므로(같은 결과) 그대로 쓴다
    bake_ao(ob)

# ── 해변 ─────────────────────────────────────────
def build_parasol(name, col):
    pole = prim_cyl(0.045, 2.5, (0, 0, 1.25), PAINT, verts=8)
    knob = prim_cyl(0.07, 0.12, (0, 0, 2.95), PAINT, verts=8)
    can = ring_cone(name + "_c", 1.45, 0.5, 16, [col, CANVAS], 2.4)
    ob = join([pole, knob, can], name); bake_ao(ob); export(ob, name)

def build_sunbed():
    obs = [prim_box("base", (0.66, 1.25, 0.08), (0, -0.2, 0.32), WOODM)]
    for sx in (-0.28, 0.28):
        for sy in (-0.75, 0.35): obs.append(prim_box("leg", (0.06, 0.06, 0.3), (sx, sy, 0.15), WOODM))
    obs.append(prim_box("back", (0.66, 0.7, 0.08), (0, 0.62, 0.55), WOODM, rot=(math.radians(-38), 0, 0)))
    obs.append(prim_box("cush", (0.58, 1.15, 0.06), (0, -0.2, 0.39), PBLUE))
    obs.append(prim_box("cush2", (0.58, 0.62, 0.06), (0, 0.6, 0.6), PBLUE, rot=(math.radians(-38), 0, 0)))
    ob = join(obs, "VSunbed"); bake_ao(ob); export(ob, "VSunbed")

# ── 식물원 ───────────────────────────────────────
def build_greenhouse():
    R = 5.6; obs = []
    base = prim_cyl(R + 0.15, 0.7, (0, 0, 0.35), PAINT, verts=16); obs.append(base)
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=8, radius=R, location=(0, 0, 0.7))
    dome = bpy.context.active_object
    bm = bmesh.new(); bm.from_mesh(dome.data)
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if v.co.z < -0.01], context='VERTS'); bm.to_mesh(dome.data); bm.free()
    dome.data.materials.append(GLASS); obs.append(dome)
    for i in range(8):   # 자오선 뼈대
        a = i / 8 * math.pi * 2
        for k in range(8):
            t0 = k / 8 * math.pi / 2; t1 = (k + 1) / 8 * math.pi / 2
            p0 = Vector((math.cos(a) * math.cos(t0) * R, math.sin(a) * math.cos(t0) * R, 0.7 + math.sin(t0) * R))
            p1 = Vector((math.cos(a) * math.cos(t1) * R, math.sin(a) * math.cos(t1) * R, 0.7 + math.sin(t1) * R))
            mid = (p0 + p1) / 2; d = p1 - p0
            bpy.ops.mesh.primitive_cylinder_add(vertices=6, radius=0.07, depth=d.length * 1.05, location=mid)
            c = bpy.context.active_object; c.rotation_mode = 'QUATERNION'; c.rotation_quaternion = Vector((0, 0, 1)).rotation_difference(d)
            bpy.ops.object.transform_apply(rotation=True); c.data.materials.append(PAINT); obs.append(c)
    for zt in (0.35, 0.7):   # 가로 테
        z = 0.7 + math.sin(zt * math.pi / 2) * R; rr = math.cos(zt * math.pi / 2) * R
        bpy.ops.mesh.primitive_torus_add(major_radius=rr, minor_radius=0.06, major_segments=24, minor_segments=5, location=(0, 0, z))
        t = bpy.context.active_object; t.data.materials.append(PAINT); obs.append(t)
    obs.append(prim_cyl(0.5, 0.35, (0, 0, 0.7 + R + 0.1), PAINT, verts=8))       # 꼭대기 환기창
    # 앞(-Y) 현관
    obs.append(prim_box("porch", (2.2, 1.6, 2.4), (0, -R - 0.4, 1.2), PAINT))
    obs.append(prim_box("door", (1.3, 0.05, 1.9), (0, -R - 1.21, 0.95), GLASS))
    obs.append(prim_box("porchRoof", (2.5, 1.9, 0.18), (0, -R - 0.4, 2.46), PAINT))
    ob = join(obs, "VGreenhouse"); export(ob, "VGreenhouse")   # 유리는 AO 없이

# ── 정류장 ───────────────────────────────────────
def build_shelter():
    obs = []
    for sx in (-1.75, 1.75):
        for sy in (-0.1, 0.75): obs.append(prim_box("post", (0.1, 0.1, 2.4), (sx, sy, 1.2), BUS))
    for k in range(7):   # 둥근 지붕(앞으로 살짝 내려옴)
        a0 = math.radians(-20 + k * 10); y = 0.35 - math.sin(a0) * 1.25; z = 2.35 + math.cos(a0) * 0.3 - 0.2
        obs.append(prim_box("roof", (3.9, 0.36, 0.08), (0, y - 0.35, z), BUS, rot=(a0, 0, 0)))
    obs.append(prim_box("trim", (3.95, 0.06, 0.16), (0, -0.62, 2.28), PAINT))
    obs.append(prim_box("back", (3.4, 0.05, 1.9), (0, 0.78, 1.25), GLASS))
    obs.append(prim_box("side", (0.05, 0.8, 1.9), (1.75, 0.35, 1.25), GLASS))
    # 포스터(왼쪽 옆판): 평면 UV 0..1
    obs.append(prim_box("pframe", (0.08, 0.9, 1.5), (-1.75, 0.35, 1.3), BUS))
    bpy.ops.mesh.primitive_plane_add(size=1, location=(-1.70, 0.35, 1.3), rotation=(math.radians(90), 0, math.radians(90)))
    pl = bpy.context.active_object; pl.scale = (0.78, 1.38, 1); bpy.ops.object.transform_apply(scale=True, rotation=True)
    pl.data.materials.append(POSTER); obs.append(pl)
    bpy.ops.mesh.primitive_plane_add(size=1, location=(-1.80, 0.35, 1.3), rotation=(math.radians(90), 0, math.radians(-90)))
    pl2 = bpy.context.active_object; pl2.scale = (0.78, 1.38, 1); bpy.ops.object.transform_apply(scale=True, rotation=True)
    pl2.data.materials.append(POSTER); obs.append(pl2)
    obs.append(prim_box("bench", (2.7, 0.42, 0.07), (0, 0.45, 0.48), WOODM))
    for sx in (-1.1, 1.1): obs.append(prim_box("bleg", (0.07, 0.35, 0.45), (sx, 0.45, 0.23), BUS))
    obs.append(prim_box("bback", (2.7, 0.05, 0.3), (0, 0.66, 0.78), WOODM))
    ob = join(obs, "VBusShelter"); export(ob, "VBusShelter")

# ── 분수 ─────────────────────────────────────────
def build_fountain():
    obs = [prim_cyl(2.5, 0.55, (0, 0, 0.275), FSTONE, verts=8), prim_cyl(2.2, 0.06, (0, 0, 0.5), FWATER, verts=8),
           prim_cyl(0.32, 1.3, (0, 0, 0.9), FSTONE, verts=8), prim_cyl(0.95, 0.22, (0, 0, 1.55), FSTONE, verts=8, r2=1.1),
           prim_cyl(0.9, 0.04, (0, 0, 1.67), FWATER, verts=8), prim_cyl(0.16, 0.5, (0, 0, 1.9), FSTONE, verts=8),
           prim_cyl(0.28, 0.1, (0, 0, 2.18), FSTONE, verts=8)]
    for i in range(8):   # 수반 테두리 윗면 돌
        a = i / 8 * math.pi * 2 + math.pi / 8
        obs.append(prim_box("cap", (1.95, 0.42, 0.1), (math.cos(a) * 2.35, math.sin(a) * 2.35, 0.58), FSTONE, rot=(0, 0, a + math.pi / 2)))
    ob = join(obs, "VFountain"); bake_ao(ob); export(ob, "VFountain")

# ── 호텔 ─────────────────────────────────────────
def build_hotel():
    W, D, fl, fh = 10.0, 7.0, 4, 3.0; H = fl * fh; obs = [prim_box("body", (W, D, H), (0, 0, H / 2), HWALL)]
    for f in range(fl):
        z = f * fh
        if f == 0:
            obs.append(prim_box("lobby", (6.0, 0.05, 2.2), (0, -D / 2 - 0.03, 1.2), HWIN))
            obs.append(prim_box("awn", (7.0, 1.4, 0.12), (0, -D / 2 - 0.7, 2.6), AWN, rot=(math.radians(-10), 0, 0)))
            continue
        for k in range(4):
            x = -3.75 + k * 2.5
            obs.append(prim_box("win", (1.6, 0.05, 1.7), (x, -D / 2 - 0.03, z + 1.35), HWIN))
            obs.append(prim_box("slab", (2.1, 0.9, 0.12), (x, -D / 2 - 0.45, z + 0.3), HWALL))
            obs.append(prim_box("rail", (2.1, 0.05, 0.55), (x, -D / 2 - 0.88, z + 0.62), PAINT))
        for k in range(3): obs.append(prim_box("swin", (0.05, 1.3, 1.4), (W / 2 + 0.03, -2.0 + k * 2.0, z + 1.4), HWIN))
        for k in range(3): obs.append(prim_box("swin2", (0.05, 1.3, 1.4), (-W / 2 - 0.03, -2.0 + k * 2.0, z + 1.4), HWIN))
    obs.append(prim_box("cornice", (W + 0.4, D + 0.4, 0.3), (0, 0, H + 0.15), PAINT))
    # 모임지붕
    bm = bmesh.new(); hw, hd, rh = W / 2 + 0.3, D / 2 + 0.3, 2.2
    b = [bm.verts.new(p) for p in ((-hw, -hd, H + 0.3), (hw, -hd, H + 0.3), (hw, hd, H + 0.3), (-hw, hd, H + 0.3))]
    t = [bm.verts.new(p) for p in ((-hw + hd * 0.9, 0, H + 0.3 + rh), (hw - hd * 0.9, 0, H + 0.3 + rh))]
    for f in ((b[0], b[1], t[1], t[0]), (b[1], b[2], t[1]), (b[2], b[3], t[0], t[1]), (b[3], b[0], t[0])): bm.faces.new(f).smooth = False
    roof = new_obj("roof", bm, [HROOF]); obs.append(roof)
    ob = join(obs, "VHotel"); bake_ao(ob); export(ob, "VHotel")

# ── 폭포 못 ──────────────────────────────────────
def build_falls_pool():
    random.seed(208); obs = []; n = 16
    for i in range(n):
        a = i / n * math.pi * 2
        if abs(math.atan2(math.sin(a), math.cos(a)) - math.pi) < 0.45 or abs(math.atan2(math.sin(a), math.cos(a)) + math.pi) < 0.45: continue   # -X 쪽 = 폭포 떨어지는 곳
        s = 0.8 + random.random() * 0.5
        bm = rock_bm(300 + i, 0.9 * s, 0.7 * s, 0.55 * s, 1, 0.25, moss=True)
        o = new_obj("r", bm, [BASALT, mat("Moss", (0.42, 0.66, 0.30))])
        for v in o.data.vertices:
            p = v.co.copy(); ca, sa = math.cos(a + math.pi / 2), math.sin(a + math.pi / 2)
            v.co = Vector((p.x * ca - p.y * sa + math.cos(a) * 4.6, p.x * sa + p.y * ca + math.sin(a) * 4.6, p.z - 0.15))
        obs.append(o)
    ob = join(obs, "VFallsPool"); bake_ao(ob); export(ob, "VFallsPool")

# ── 전망 데크(주상절리) ─────────────────────────
def build_deck(W=8.0, D=6.0):
    obs = []; n = int(W / 0.4)
    for i in range(n): obs.append(prim_box("plank", (0.36, D, 0.12), (-W / 2 + 0.2 + i * 0.4, 0, 0.06 + (i % 2) * 0.004), WOODM))
    for sy in (-D / 2 + 0.2, D / 2 - 0.2): obs.append(prim_box("joist", (W, 0.2, 0.2), (0, sy, -0.1), WOODM))
    for x in (-W / 2 + 0.1, W / 2 - 0.1):
        for y in (-D / 2 + 0.1, 0, D / 2 - 0.1): obs.append(prim_box("post", (0.14, 0.14, 1.0), (x, y, 0.5), WOODM))
        obs.append(prim_box("railS", (0.1, D, 0.1), (x, 0, 0.95), WOODM)); obs.append(prim_box("railS2", (0.06, D, 0.06), (x, 0, 0.6), WOODM))
    for k in range(9): obs.append(prim_box("postF", (0.12, 0.12, 1.0), (-W / 2 + 0.1 + k * (W - 0.2) / 8, -D / 2 + 0.1, 0.5), WOODM))
    obs.append(prim_box("railF", (W, 0.1, 0.1), (0, -D / 2 + 0.1, 0.95), WOODM)); obs.append(prim_box("railF2", (W, 0.06, 0.06), (0, -D / 2 + 0.1, 0.6), WOODM))
    ob = join(obs, "VDeck"); bake_ao(ob); export(ob, "VDeck")

# ── 축사 곁 사일로(207-2차) ─────────────────────
BRED = mat("BarnRed", (0.80, 0.30, 0.26)); BROOF = mat("BarnRoof", (0.40, 0.42, 0.46))
def build_silo():
    obs = []
    for k in range(5):   # 빨강/흰 띠
        obs.append(prim_cyl(1.0, 0.9, (0, 0, 0.45 + k * 0.9), BRED if k % 2 == 0 else PAINT, verts=16))
    for k in range(1, 5): obs.append(prim_cyl(1.03, 0.08, (0, 0, k * 0.9), PAINT, verts=16))
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=8, radius=1.05, location=(0, 0, 4.5))
    d = bpy.context.active_object; bm = bmesh.new(); bm.from_mesh(d.data)
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if v.co.z < -0.01], context='VERTS'); bm.to_mesh(d.data); bm.free()
    d.data.materials.append(BROOF); obs.append(d)
    obs.append(prim_cyl(0.12, 0.3, (0, 0, 5.65), BROOF, verts=8))
    for z in [0.4 + i * 0.35 for i in range(12)]: obs.append(prim_box("rung", (0.34, 0.05, 0.05), (0, -1.1, z), PAINT))
    for sx in (-0.17, 0.17): obs.append(prim_box("rail", (0.05, 0.05, 4.4), (sx, -1.1, 2.3), PAINT))
    ob = join(obs, "VSilo"); bake_ao(ob); export(ob, "VSilo")

# ── 귀신(209차) ─────────────────────────────────
GBODY = mat("GhostBody", (0.93, 0.91, 1.0)); GEYE = mat("GhostEye", (0.12, 0.10, 0.20)); GCHEEK = mat("GhostCheek", (1.0, 0.62, 0.75))
def build_ghost():
    """동글동글한 이불 귀신 — 둥근 머리 + 물결 치마 자락 + 짧은 팔 + 큰 눈 · 볼터치. 원점 = 몸 가운데(바닥에서 1 m 뜬 자리), 앞 = -Y."""
    bm = bmesh.new(); seg = 24; rings = 10; R = 0.5
    rows = []
    for j in range(rings + 1):   # 위 반구
        th = j / rings * math.pi / 2
        rows.append([bm.verts.new((math.cos(i / seg * 2 * math.pi) * R * math.sin(th), math.sin(i / seg * 2 * math.pi) * R * math.sin(th), 0.25 + R * math.cos(th))) for i in range(seg)])
    for j in range(1, 6):          # 치마: 아래로 벌어지며 물결
        t = j / 5
        rows.append([bm.verts.new((math.cos(i / seg * 2 * math.pi) * R * (1 + 0.18 * t), math.sin(i / seg * 2 * math.pi) * R * (1 + 0.18 * t), 0.25 - 0.62 * t - (0.12 * math.cos(i / seg * 2 * math.pi * 6) * t * t))) for i in range(seg)])
    for j in range(len(rows) - 1):
        for i in range(seg):
            a, b = rows[j][i], rows[j][(i + 1) % seg]; c, d = rows[j + 1][(i + 1) % seg], rows[j + 1][i]
            if j == 0: bm.faces.new((a, d, c)) if False else None
            try: f = bm.faces.new((a, b, c, d)); f.smooth = True
            except Exception: pass
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-4); bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    body = new_obj("VGhost", bm, [GBODY])
    sol = body.modifiers.new("s", 'SOLIDIFY'); sol.thickness = 0.03
    bpy.context.view_layer.objects.active = body; bpy.ops.object.modifier_apply(modifier="s")
    obs = [body]
    for sx in (-1, 1):
        bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=8, radius=0.09, location=(sx * 0.17, -0.44, 0.38)); e = bpy.context.active_object; e.scale = (1, 0.5, 1.35); bpy.ops.object.transform_apply(scale=True); e.data.materials.append(GEYE); obs.append(e)
        bpy.ops.mesh.primitive_uv_sphere_add(segments=10, ring_count=6, radius=0.03, location=(sx * 0.15, -0.49, 0.44)); h = bpy.context.active_object; h.data.materials.append(PAINT); obs.append(h)
        bpy.ops.mesh.primitive_uv_sphere_add(segments=10, ring_count=6, radius=0.07, location=(sx * 0.3, -0.38, 0.22)); c = bpy.context.active_object; c.scale = (1, 0.4, 0.6); bpy.ops.object.transform_apply(scale=True); c.data.materials.append(GCHEEK); obs.append(c)
        bpy.ops.mesh.primitive_uv_sphere_add(segments=10, ring_count=6, radius=0.12, location=(sx * 0.55, -0.1, 0.05)); a = bpy.context.active_object; a.scale = (1.3, 0.8, 0.7); bpy.ops.object.transform_apply(scale=True); a.data.materials.append(GBODY); obs.append(a)
    bpy.ops.mesh.primitive_uv_sphere_add(segments=10, ring_count=6, radius=0.06, location=(0, -0.47, 0.2)); m = bpy.context.active_object; m.scale = (1.2, 0.4, 0.8); bpy.ops.object.transform_apply(scale=True); m.data.materials.append(GEYE); obs.append(m)
    ob = join(obs, "VGhost"); export(ob, "VGhost")

TOUR_BUILDS = '''
build_batdam(); build_parasol("VParasol_A", PRED); build_parasol("VParasol_B", PBLUE); build_parasol("VParasol_C", PYEL)
build_sunbed(); build_greenhouse(); build_shelter(); build_fountain(); build_hotel(); build_falls_pool(); build_deck()
'''
if bpy.app.background:
    exec(TOUR_BUILDS); print("done tour kit")
