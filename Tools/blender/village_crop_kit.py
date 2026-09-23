# 169차(사용자: 「식물별 육성 사진이 달라야 한다」): 텃밭 작물 5종 × 성장 4단계 모델.
#   이름 VCrop_<Tomato|Potato|Rice|Rose|Lavender>_<2..5>   (2 새싹 · 3 줄기 · 4 봉오리 · 5 다 자람)
#   원점 = 흙 표면, +Z(블렌더) = 위 → Unity +Y. 한 포기 0.15~0.80 m. VillageFarm 이 칸마다 4포기 심는다.
#   재질 이름 → JejuKit.MaterialFor:
#     CropStem / CropLeaf / CropLeafGray / CropStake / CropSoil /
#     CropTomato / CropPotato / CropRice / CropRose / CropLavender / CropBloomWhite / CropBloomYellow
# 실행: blender -b --python village_crop_kit.py
import bpy, math, os, random
OUT = r"C:\dev\game\Assets\Resources\CoastRun\Models"; os.makedirs(OUT, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
R = math.radians

def mat(n, rgb):
    m = bpy.data.materials.get(n) or bpy.data.materials.new(n); m.use_nodes = True
    b = m.node_tree.nodes.get("Principled BSDF")
    if b: b.inputs["Base Color"].default_value = (rgb[0], rgb[1], rgb[2], 1.0)
    return m

STEM  = mat("CropStem",        (0.35, 0.60, 0.26))
LEAF  = mat("CropLeaf",        (0.42, 0.72, 0.32))
GRAY  = mat("CropLeafGray",    (0.55, 0.68, 0.50))
STAKE = mat("CropStake",       (0.62, 0.46, 0.28))
SOIL  = mat("CropSoil",        (0.44, 0.31, 0.20))
TOM   = mat("CropTomato",      (0.93, 0.22, 0.18))
POT   = mat("CropPotato",      (0.80, 0.64, 0.38))
RICE  = mat("CropRice",        (0.92, 0.80, 0.32))
ROSE  = mat("CropRose",        (0.97, 0.42, 0.60))
LAV   = mat("CropLavender",    (0.70, 0.52, 0.92))
WHITE = mat("CropBloomWhite",  (0.97, 0.96, 0.92))
YEL   = mat("CropBloomYellow", (1.00, 0.86, 0.25))

parts = []
def add(ob, m):
    ob.data.materials.append(m); parts.append(ob); return ob
def cyl(r, h, loc, m, rot=(0,0,0), r2=None, verts=10):
    if r2 is None: bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=r, depth=h, location=loc, rotation=rot)
    else: bpy.ops.mesh.primitive_cone_add(vertices=verts, radius1=r, radius2=r2, depth=h, location=loc, rotation=rot)
    ob = bpy.context.active_object; bpy.ops.object.shade_smooth(); return add(ob, m)
def sph(r, loc, m, scale=(1,1,1), rot=(0,0,0), seg=12, ring=8):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=seg, ring_count=ring, radius=r, location=loc, rotation=rot)
    ob = bpy.context.active_object; ob.scale = scale
    bpy.ops.object.transform_apply(scale=True); bpy.ops.object.shade_smooth(); return add(ob, m)

def leaf(length, width, base, m, yaw=0.0, tilt=22.0, thick=0.010):
    """잎 한 장 — 납작한 타원. base 에서 yaw 방향으로 뻗고 tilt 만큼 위로 든다."""
    ya, ti = R(yaw), R(tilt)
    cx = base[0] + math.cos(ya) * math.cos(ti) * length * 0.5
    cy = base[1] + math.sin(ya) * math.cos(ti) * length * 0.5
    cz = base[2] + math.sin(ti) * length * 0.5
    return sph(0.5, (cx, cy, cz), m, scale=(length, width, thick), rot=(0, -ti, ya))

def blade(length, base, m, yaw=0.0, tilt=62.0, w=0.026):
    """벼·라벤더의 가는 잎 — 위로 솟았다 살짝 휘는 가늘고 긴 잎."""
    ya, ti = R(yaw), R(tilt)
    cx = base[0] + math.cos(ya) * math.cos(ti) * length * 0.5
    cy = base[1] + math.sin(ya) * math.cos(ti) * length * 0.5
    cz = base[2] + math.sin(ti) * length * 0.5
    return sph(0.5, (cx, cy, cz), m, scale=(length, w, 0.008), rot=(0, -ti, ya))

def export(name):
    global parts
    bpy.ops.object.select_all(action='DESELECT')
    for p in parts: p.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join(); ob = bpy.context.active_object; ob.name = name
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, name + ".fbx"), use_selection=True, apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_ALL', bake_space_transform=True, axis_forward='-Z', axis_up='Y',
        object_types={'MESH'}, mesh_smooth_type='FACE', use_mesh_modifiers=True, add_leaf_bones=False,
        bake_anim=False, path_mode='STRIP')
    print("exported", name)
    bpy.data.objects.remove(ob, do_unlink=True); parts = []

def sprout(m=LEAF, h=0.09, lf=0.10):
    """어느 작물이나 같은 떡잎 두 장 — 종 구분은 3단계부터."""
    cyl(0.011, h, (0, 0, h * 0.5), STEM)
    leaf(lf, lf * 0.62, (0, 0, h), m, yaw=0,   tilt=26)
    leaf(lf, lf * 0.62, (0, 0, h), m, yaw=180, tilt=26)

# ── 토마토: 지지대 막대 + 곧은 줄기 + 세 갈래 잎 + 노란 꽃 → 늘어진 방울토마토 송이 ─────
def tomato(stage):
    if stage == 2:
        sprout(); return
    h = {3: 0.32, 4: 0.46, 5: 0.54}[stage]
    cyl(0.012, h + 0.10, (0.105, 0, (h + 0.10) * 0.5), STAKE, verts=8)        # 지지대
    cyl(0.019, h, (0, 0, h * 0.5), STEM)
    cyl(0.005, 0.12, (0.052, 0, h * 0.66), STAKE, rot=(0, R(90), 0), verts=6)  # 묶은 자리
    nodes = {3: 3, 4: 4, 5: 5}[stage]
    for i in range(nodes):
        z = 0.08 + (h - 0.10) * (i / max(1, nodes - 1.0))
        for k, (dy, ln) in enumerate(((0, 0.16), (34, 0.115), (-34, 0.115))):  # 세 갈래 잔잎
            leaf(ln, ln * 0.56, (0, 0, z), LEAF, yaw=i * 72 + dy, tilt=20 - k * 5)
    if stage >= 4:
        for i in range(3):
            a = R(30 + i * 120)
            sph(0.025, (math.cos(a) * 0.115, math.sin(a) * 0.115, h * 0.80), YEL, scale=(1, 1, 0.6))
    if stage == 5:
        for a, z0 in ((40, 0.36), (205, 0.24)):                               # 송이 2개
            ar = R(a); bx, by = math.cos(ar) * 0.10, math.sin(ar) * 0.10
            cyl(0.006, 0.11, (bx * 0.55, by * 0.55, z0 + 0.05), STEM, rot=(0, R(58), ar), verts=6)
            for k, (ox, oz) in enumerate(((0.0, 0.0), (0.055, -0.045), (-0.045, -0.055))):
                fx = bx + math.cos(ar) * ox - math.sin(ar) * ox * 0.3
                fy = by + math.sin(ar) * ox + math.cos(ar) * ox * 0.3
                sph(0.056, (fx, fy, z0 + oz), TOM, scale=(1, 1, 0.94))
                sph(0.026, (fx, fy, z0 + oz + 0.045), STEM, scale=(1, 1, 0.35))  # 꼭지

# ── 감자: 낮고 넓게 퍼진 잎 덤불 + 흰 꽃 → 흙 위로 드러난 갈색 덩이 ──────────────
def potato(stage):
    if stage == 2:
        sprout(h=0.07, lf=0.09); return
    h = {3: 0.16, 4: 0.22, 5: 0.24}[stage]
    cyl(0.016, h, (0, 0, h * 0.5), STEM)
    n = {3: 6, 4: 9, 5: 9}[stage]
    for i in range(n):
        z = 0.05 + (h - 0.03) * ((i % 3) / 3.0)
        leaf(0.20 - (i % 3) * 0.02, 0.13, (0, 0, z), LEAF, yaw=i * 77, tilt=10 + (i % 3) * 7)
    if stage >= 4:
        for i in range(3):
            a = R(60 + i * 115)
            sph(0.030, (math.cos(a) * 0.09, math.sin(a) * 0.09, h + 0.03), WHITE, scale=(1, 1, 0.5))
            sph(0.010, (math.cos(a) * 0.09, math.sin(a) * 0.09, h + 0.045), YEL)
    if stage == 5:
        sph(0.19, (0, 0, 0.01), SOIL, scale=(1, 1, 0.45))                     # 북주기 흙더미
        for i, a in enumerate((20, 150, 265)):
            ar = R(a)
            sph(0.072, (math.cos(ar) * 0.16, math.sin(ar) * 0.16, 0.055), POT, scale=(1.15, 0.95, 0.82), rot=(0, 0, ar))

# ── 벼: 가는 잎이 부채처럼 → 고개 숙인 황금 이삭 ────────────────────────────────
def rice(stage):
    if stage == 2:
        cyl(0.008, 0.06, (0, 0, 0.03), STEM)
        for i in range(3): blade(0.13, (0, 0, 0.03), LEAF, yaw=i * 120, tilt=70)
        return
    h = {3: 0.30, 4: 0.44, 5: 0.48}[stage]
    n = {3: 7, 4: 9, 5: 9}[stage]
    for i in range(n):
        blade(h * (0.82 + (i % 3) * 0.09), (0, 0, 0.02), LEAF if stage < 5 else GRAY, yaw=i * 51, tilt=64 - (i % 3) * 9)
    if stage == 4:
        for i in range(3):
            a = R(30 + i * 120)
            cyl(0.007, h * 0.95, (math.cos(a) * 0.035, math.sin(a) * 0.035, h * 0.48), STEM, verts=6)
            sph(0.018, (math.cos(a) * 0.035, math.sin(a) * 0.035, h * 0.97), LEAF, scale=(1, 1, 2.6))
    if stage == 5:
        for i in range(5):
            a = R(18 + i * 71); bx, by = math.cos(a) * 0.045, math.sin(a) * 0.045
            cyl(0.007, h, (bx, by, h * 0.5), GRAY, verts=6)
            tipx, tipy, tipz = bx + math.cos(a) * 0.08, by + math.sin(a) * 0.08, h + 0.03
            # 고개 숙인 이삭: 알갱이를 호를 그리며 늘어놓는다
            for k in range(7):
                t = k / 6.0
                gx = bx + (tipx - bx) * t
                gy = by + (tipy - by) * t
                gz = h + 0.05 * math.sin(t * 2.0) - t * t * 0.10
                sph(0.014, (gx, gy, gz), RICE, scale=(1, 1, 1.5))

# ── 장미: 가시 줄기 + 톱니 잎 → 봉오리 → 겹꽃 ──────────────────────────────────
def rose(stage):
    if stage == 2:
        sprout(h=0.10, lf=0.09); return
    h = {3: 0.30, 4: 0.44, 5: 0.48}[stage]
    cyl(0.015, h, (0, 0, h * 0.5), STEM)
    for i in range(6):                                                        # 가시
        z = 0.08 + i * (h - 0.12) / 6.0
        cyl(0.011, 0.035, (0, 0, z), STEM, rot=(R(90), 0, R(i * 63)), r2=0.001, verts=5)
    n = {3: 5, 4: 6, 5: 6}[stage]
    for i in range(n):
        z = 0.09 + (h - 0.14) * (i / max(1, n - 1.0))
        leaf(0.13, 0.075, (0, 0, z), LEAF, yaw=i * 101, tilt=14)
    if stage == 3:
        sph(0.028, (0, 0, h + 0.02), LEAF, scale=(1, 1, 1.4))
    if stage == 4:
        for i, a in enumerate((0, 150)):
            ar = R(a); bx, by = math.cos(ar) * 0.05 * i, math.sin(ar) * 0.05 * i
            cyl(0.008, 0.07, (bx, by, h + 0.03), STEM, verts=6)
            sph(0.042, (bx, by, h + 0.09), ROSE, scale=(0.85, 0.85, 1.35))    # 봉오리
            for k in range(4):
                kk = R(k * 90)
                leaf(0.05, 0.03, (bx, by, h + 0.05), LEAF, yaw=k * 90 + 20, tilt=52)
    if stage == 5:
        for i, a in enumerate((10, 140, 255)):
            ar = R(a); rr = 0.0 if i == 0 else 0.095
            bx, by = math.cos(ar) * rr, math.sin(ar) * rr
            bz = h + (0.05 if i == 0 else 0.0)
            cyl(0.008, 0.08, (bx, by, bz - 0.01), STEM, verts=6)
            sph(0.030, (bx, by, bz + 0.05), ROSE, scale=(1, 1, 0.8))          # 꽃심
            for k in range(6):                                                # 꽃잎
                leaf(0.095, 0.068, (bx, by, bz + 0.05), ROSE, yaw=k * 60, tilt=30 + (k % 2) * 12, thick=0.012)
            for k in range(5):
                leaf(0.062, 0.045, (bx, by, bz + 0.035), ROSE, yaw=k * 72 + 36, tilt=52, thick=0.012)

# ── 라벤더: 회록 잎 다발 + 긴 꽃대 → 보라 이삭 ──────────────────────────────────
def lavender(stage):
    if stage == 2:
        cyl(0.009, 0.05, (0, 0, 0.025), STEM)
        for i in range(4): blade(0.09, (0, 0, 0.025), GRAY, yaw=i * 90, tilt=58, w=0.022)
        return
    h = {3: 0.22, 4: 0.34, 5: 0.40}[stage]
    for i in range(8):
        blade(0.16 + (i % 3) * 0.02, (0, 0, 0.02), GRAY, yaw=i * 45, tilt=52 - (i % 3) * 8, w=0.024)
    stalks = {3: 3, 4: 5, 5: 7}[stage]
    for i in range(stalks):
        a = R(i * (360.0 / stalks) + 12); rr = 0.03 + (i % 2) * 0.025
        bx, by = math.cos(a) * rr, math.sin(a) * rr
        lean = R(7 + (i % 3) * 5)
        cyl(0.006, h, (bx + math.sin(lean) * h * 0.25, by, h * 0.5), GRAY, rot=(0, lean, a), verts=6)
        tx, ty, tz = bx + math.sin(lean) * h * 0.5, by, h
        if stage == 3:
            sph(0.016, (tx, ty, tz + 0.02), GRAY, scale=(1, 1, 1.8))
        else:
            spike = 0.10 if stage == 4 else 0.14
            cyl(0.017, spike, (tx, ty, tz + spike * 0.5), LAV, r2=0.006, verts=8)
            for k in range(5):                                                # 이삭 알갱이
                sph(0.017, (tx + math.cos(R(k * 72)) * 0.010, ty + math.sin(R(k * 72)) * 0.010,
                            tz + 0.02 + k * (spike / 5.5)), LAV, scale=(1, 1, 0.75))

BUILD = {"Tomato": tomato, "Potato": potato, "Rice": rice, "Rose": rose, "Lavender": lavender}
for name, fn in BUILD.items():
    for st in (2, 3, 4, 5):
        random.seed(hash(name) + st)
        fn(st)
        export("VCrop_%s_%d" % (name, st))
print("done — 20 models")
