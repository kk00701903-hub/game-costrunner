# -*- coding: utf-8 -*-
"""Coast Run 147차 — 집 디테일 업그레이드 (Blender 4.x/5.x, Z-up).
평평한 상자 집 → 널판/벽돌 입체 벽(Bevel + Subdivision + Displace + Brick 셰이더), 기와 지붕(Array 로 기왓장 반복),
창틀(Inset)·덧문·창가 화단, 문 패널·손잡이, 굴뚝, 모서리 트림.

사용 1) Blender 안에서 Run Script → 'House_Detail' 컬렉션. 선택 메시가 있으면 --from-selection 처럼 그 치수를 씀.
사용 2) 헤드리스: blender -b -P house_detail.py -- --out "House_Detail.fbx" [--w 4.6 --d 4.0 --h 2.7 --wall #DDF3EA --roof #F2A7A0 --brick 0|1]
"""
import bpy, bmesh, sys, math
from mathutils import Vector

ARGS = {"w": 4.6, "d": 4.0, "h": 2.7, "wall": "#DDF3EA", "roof": "#F2A7A0", "trim": "#FFFFFF", "door": "#8C5A3C", "brick": 0, "out": None, "from_selection": 0, "tile": 0.22}
if "--" in sys.argv:
    it = iter(sys.argv[sys.argv.index("--") + 1:])
    for a in it:
        k = a.lstrip("-").replace("-", "_")
        if k in ARGS:
            v = next(it); ARGS[k] = type(ARGS[k])(v) if ARGS[k] is not None else v

def hexc(h):
    h = h.lstrip("#"); return tuple(int(h[i:i+2], 16) / 255.0 for i in (0, 2, 4)) + (1.0,)

def mat(name, col, rough=0.85, spec=0.2):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True; bsdf = m.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = col; bsdf.inputs["Roughness"].default_value = rough
        if "Specular IOR Level" in bsdf.inputs: bsdf.inputs["Specular IOR Level"].default_value = spec
    m.diffuse_color = col; return m

COL = bpy.data.collections.get("House_Detail") or bpy.data.collections.new("House_Detail")
if COL.name not in bpy.context.scene.collection.children: bpy.context.scene.collection.children.link(COL)

def link(o):
    for c in o.users_collection: c.objects.unlink(o)
    COL.objects.link(o); return o

def box(name, loc, size, m, rz=0.0):
    """loc=(x, y, z_up), size=(sx, sy, sz)"""
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc, rotation=(0, 0, rz))
    o = bpy.context.active_object; o.name = name; o.scale = size; o.data.materials.append(m); return link(o)

def sphere(name, loc, r, m):
    bpy.ops.mesh.primitive_uv_sphere_add(radius=r, location=loc, segments=16, ring_count=8)
    o = bpy.context.active_object; o.name = name; o.data.materials.append(m); return link(o)

def bevel(o, w=0.03, seg=3):
    md = o.modifiers.new("Bevel", "BEVEL"); md.width = w; md.segments = seg; md.limit_method = "ANGLE"; md.harden_normals = True
    for p in o.data.polygons: p.use_smooth = True
    try: bpy.context.view_layer.objects.active = o; bpy.ops.object.shade_smooth_by_angle(angle=math.radians(35))
    except Exception: pass

def wall_detail(o, brick):
    """벽: Subdivision(Simple 4단) + Displace(노이즈, 살짝 울퉁불퉁) + Brick Texture 셰이더(색·범프)로 벽돌/널판 줄."""
    sd = o.modifiers.new("Subd", "SUBSURF"); sd.subdivision_type = "SIMPLE"; sd.levels = 3; sd.render_levels = 3
    tex = bpy.data.textures.get("WallNoise") or bpy.data.textures.new("WallNoise", "CLOUDS"); tex.noise_scale = 0.35
    dp = o.modifiers.new("Displace", "DISPLACE"); dp.texture = tex; dp.strength = 0.012; dp.mid_level = 0.5; dp.texture_coords = "GLOBAL"
    m = o.data.materials[0]; nt = m.node_tree; bsdf = nt.nodes["Principled BSDF"]
    base = tuple(bsdf.inputs["Base Color"].default_value)
    br = nt.nodes.new("ShaderNodeTexBrick"); br.location = (-600, 200)
    br.inputs["Scale"].default_value = 3.2; br.inputs["Mortar Size"].default_value = 0.015 if brick else 0.02; br.inputs["Bias"].default_value = -0.1
    br.inputs["Color1"].default_value = base
    br.inputs["Color2"].default_value = (base[0] * 0.94, base[1] * 0.94, base[2] * 0.96, 1)
    br.inputs["Mortar"].default_value = (base[0] * 0.72, base[1] * 0.72, base[2] * 0.76, 1)
    if not brick:   # 널판(사이딩): 가로 줄만 — 오프셋 0, 벽돌 폭 크게
        br.offset = 0.0
        if "Brick Width" in br.inputs: br.inputs["Brick Width"].default_value = 40.0
        if "Row Height" in br.inputs: br.inputs["Row Height"].default_value = 0.36
    tc = nt.nodes.new("ShaderNodeTexCoord"); tc.location = (-800, 200)
    nt.links.new(tc.outputs["Object"], br.inputs["Vector"]); nt.links.new(br.outputs["Color"], bsdf.inputs["Base Color"])
    bump = nt.nodes.new("ShaderNodeBump"); bump.location = (-300, -150); bump.inputs["Strength"].default_value = 0.35
    nt.links.new(br.outputs["Fac"], bump.inputs["Height"]); nt.links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])

def tile_roof(w, d, h, z0, m):
    """기와: 기왓장(가는 원통, 축 X) → Array(가로) → Array(경사 방향 계단식) 을 앞(+Y)·뒤(−Y) 경사면에."""
    hd = d * 0.5; slope = math.hypot(hd, h); ang = math.atan2(h, hd); t = ARGS["tile"]
    n_x = int((w + 0.6) / t) + 1; n_s = int(slope / (t * 0.85)) + 1
    for side in (1, -1):
        bpy.ops.mesh.primitive_cylinder_add(vertices=10, radius=t * 0.42, depth=t * 0.96, rotation=(0, math.radians(90), 0),
                                            location=(-(w + 0.6) * 0.5 + t * 0.5, side * (hd - 0.06), z0 + 0.08))
        o = bpy.context.active_object; o.name = "Tiles_%s" % ("F" if side > 0 else "B"); o.data.materials.append(m)
        a1 = o.modifiers.new("ArrayX", "ARRAY"); a1.count = n_x; a1.relative_offset_displace = (0, 0, 1.0)      # 로컬 Z(=월드 X) 로 반복
        a2 = o.modifiers.new("ArrayS", "ARRAY"); a2.count = n_s; a2.use_relative_offset = False; a2.use_constant_offset = True
        # 경사 위쪽 방향(월드): (0, -side*cos, sin) → 로컬(회전 (0,90°,0): 로컬X=월드-Z, 로컬Y=월드Y, 로컬Z=월드X)
        step = t * 0.85
        a2.constant_offset_displace = (-math.sin(ang) * step, -side * math.cos(ang) * step, 0)
        for p in o.data.polygons: p.use_smooth = True
        link(o)

def window(x, y, rz, m_frame, m_glass, m_pot):
    q = rz; c, s = math.cos(q), math.sin(q)
    def L(dx, dz, dy):  # dx: 창 가로, dz: 위, dy: 벽 바깥쪽
        return (x + dx * c - dy * s, y + dx * s + dy * c, 1.55 + dz)
    f = box("WinFrame", L(0, 0, 0.02), (1.0, 0.10, 1.0), m_frame, q); bevel(f, 0.02, 2)
    # 틀 안쪽 파기(Inset) — 바깥면
    bpy.context.view_layer.objects.active = f; bpy.ops.object.mode_set(mode="EDIT")
    bm = bmesh.from_edit_mesh(f.data); bm.faces.ensure_lookup_table()
    out_face = max(bm.faces, key=lambda fc: fc.normal.y)
    bmesh.ops.inset_individual(bm, faces=[out_face], thickness=0.08, depth=-0.02)
    bmesh.update_edit_mesh(f.data); bpy.ops.object.mode_set(mode="OBJECT")
    box("Win", L(0, 0, 0.05), (0.82, 0.06, 0.82), m_glass, q)
    for sx in (-0.66, 0.66): bevel(box("Shutter", L(sx, 0, 0.03), (0.26, 0.06, 1.0), m_frame, q), 0.02, 2)
    bevel(box("Pot", L(0, -0.66, 0.24), (1.05, 0.26, 0.24), m_pot, q), 0.02, 2)
    sphere("PotLeaf", L(0, -0.52, 0.24), 0.16, mat("Leaf", (0.40, 0.68, 0.32, 1))).scale = (3.2, 1.0, 0.8)
    cols = [(1, 0.55, 0.70, 1), (1, 0.92, 0.45, 1), (1, 1, 1, 1), (1, 0.62, 0.55, 1)]
    for k in range(5): sphere("PotFlower", L(-0.38 + k * 0.19, -0.44 + (k % 2) * 0.05, 0.24 + (k % 2) * 0.06), 0.075, mat("Fl%d" % (k % 4), cols[k % 4]))

def build():
    w, d, h = ARGS["w"], ARGS["d"], ARGS["h"]
    if ARGS["from_selection"]:
        sel = [o for o in bpy.context.selected_objects if o.type == "MESH"]
        if sel: w, d, h = sel[0].dimensions.x, sel[0].dimensions.y, sel[0].dimensions.z
    m_wall = mat("Wall", hexc(ARGS["wall"])); m_roof = mat("Roof", hexc(ARGS["roof"]), 0.7); m_trim = mat("Trim", hexc(ARGS["trim"]))
    m_door = mat("Door", hexc(ARGS["door"])); m_glass = mat("Glass", (0.72, 0.88, 1.0, 1), 0.2, 0.6); m_pot = mat("Pot", (0.72, 0.45, 0.30, 1))
    m_knob = mat("Knob", (1.0, 0.85, 0.35, 1), 0.3, 0.8); m_roofD = mat("RoofDark", tuple(c * 0.75 for c in hexc(ARGS["roof"])[:3]) + (1,), 0.7)
    wall = box("Wall", (0, 0, h * 0.5 + 0.05), (w, d, h), m_wall); bevel(wall, 0.04, 3); wall_detail(wall, bool(ARGS["brick"]))
    bevel(box("Plinth", (0, 0, 0.25), (w + 0.18, d + 0.18, 0.5), m_trim))
    for sx in (-1, 1):
        for sy in (-1, 1): bevel(box("CornerTrim", (sx * w * 0.5, sy * d * 0.5, h * 0.5 + 0.05), (0.16, 0.16, h), m_trim), 0.02, 2)
    # 박공 지붕 본체(삼각 프리즘, 용마루 = X) + 기와 + 용마루 + 처마널
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0, 0, h + 0.08 + 0.95)); g = bpy.context.active_object; g.name = "RoofCore"; g.scale = (w + 0.9, d + 0.9, 1.9)
    bm = bmesh.new(); bm.from_mesh(g.data)
    for v in bm.verts:
        if v.co.z > 0: v.co.y = 0.0
    bm.to_mesh(g.data); bm.free(); g.data.materials.append(m_roofD); link(g)
    tile_roof(w + 0.9, d + 0.9, 1.9, h + 0.08, m_roof)
    bevel(box("Ridge", (0, 0, h + 2.0), (w + 1.1, 0.34, 0.18), m_roofD), 0.05, 3)
    for sy in (-1, 1): bevel(box("Fascia", (0, sy * (d + 0.9) * 0.5, h + 0.06), (w + 1.0, 0.10, 0.16), m_trim), 0.02, 2)
    # 문(앞면 +Y): 틀·문짝·패널·손잡이·계단·매트
    fy = d * 0.5
    bevel(box("DoorFrame", (0, fy + 0.02, 1.05), (1.25, 0.10, 2.05), m_trim), 0.02, 2)
    bevel(box("Door", (0, fy + 0.06, 1.0), (1.0, 0.08, 1.9), m_door), 0.015, 2)
    for z, hh in ((1.35, 0.55), (0.6, 0.65)): bevel(box("DoorPanel", (0, fy + 0.105, z), (0.7, 0.02, hh), mat("DoorDark", tuple(c * 0.8 for c in hexc(ARGS["door"])[:3]) + (1,))), 0.008, 2)
    sphere("Knob", (0.32, fy + 0.12, 1.0), 0.06, m_knob)
    bevel(box("Step", (0, fy + 0.45, 0.12), (1.7, 0.8, 0.22), m_trim), 0.04, 3)
    box("Mat", (0, fy + 0.55, 0.24), (0.9, 0.5, 0.03), m_pot)
    # 창 4개(앞 2, 옆 2) + 굴뚝
    window(-w * 0.30, fy + 0.02, 0, m_trim, m_glass, m_pot); window(w * 0.30, fy + 0.02, 0, m_trim, m_glass, m_pot)
    window(w * 0.5 + 0.02, 0, math.radians(-90), m_trim, m_glass, m_pot); window(-w * 0.5 - 0.02, 0, math.radians(90), m_trim, m_glass, m_pot)
    ch = box("Chimney", (w * 0.28, -0.8, h + 1.55), (0.55, 0.55, 1.1), m_trim); bevel(ch, 0.03, 2); wall_detail(ch, True)
    bevel(box("ChimneyTop", (w * 0.28, -0.8, h + 2.12), (0.68, 0.68, 0.14), m_roofD), 0.02, 2)
    print("[house_detail] built", len(COL.objects), "objects")

build()
if ARGS["out"]:
    bpy.ops.object.select_all(action="DESELECT")
    for o in COL.objects: o.select_set(True)
    bpy.ops.export_scene.fbx(filepath=ARGS["out"], use_selection=True, apply_scale_options="FBX_SCALE_ALL", use_mesh_modifiers=True, mesh_smooth_type="FACE", path_mode="COPY", embed_textures=False)
    print("[house_detail] exported", ARGS["out"])
