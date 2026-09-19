# -*- coding: utf-8 -*-
"""140차: 마을 NPC 6종 치비 리그(첨부 캐릭터 시트 기준) — 해녀 할머니·등대지기·꽃집 언니·낚시 소년·카페 알바·서퍼.
같은 Humanoid 뼈(Mixamo 이름)라 러닝 클립을 그대로 리타겟. Run: blender -b --python npc_chibi_rig.py
"""
import bpy, bmesh, math, sys, os
from mathutils import Vector
OUT_DIR = r'C:\dev\game\Assets\Resources\CoastRun\Rig'
PREV_DIR = r'C:\dev\game\Tools\blender\npc_prev'
os.makedirs(PREV_DIR, exist_ok=True)

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

# ---------------------------------------------------------------- materials (KD_ 접두 — 하늘 재질과 구분)
SKIN   = mat('KD_Skin',   (1.00, 0.87, 0.76), 0.6)
HAIR   = mat('KD_Hair',   (0.30, 0.18, 0.10), 0.7)
COAT   = mat('KD_Coat',   (1.00, 0.52, 0.16), 0.55)
COATD  = mat('KD_CoatDark', (0.90, 0.40, 0.10), 0.55)
YELLOW = mat('KD_Yellow', (1.00, 0.82, 0.25), 0.5)
BOOT   = mat('KD_Boot',   (1.00, 0.80, 0.20), 0.35)
SOLE   = mat('KD_Sole',   (0.85, 0.66, 0.15), 0.5)
PANTS  = mat('KD_Pants',  (0.22, 0.30, 0.50), 0.7)
BLUSH  = mat('KD_Blush',  (1.00, 0.62, 0.62), 0.7)
EYEW   = mat('KD_EyeWhite', (0.99, 0.99, 0.99), 0.3)
IRIS   = mat('KD_Iris',   (0.35, 0.22, 0.10), 0.3)
BLACK  = mat('KD_Black',  (0.05, 0.04, 0.04), 0.3)
WHITE  = mat('KD_White',  (1.0, 1.0, 1.0), 0.3)
MOUTH  = mat('KD_Mouth',  (0.85, 0.35, 0.40), 0.5)
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


def cone(name, center, r1, r2, depth, verts=40):
    bpy.ops.mesh.primitive_cone_add(vertices=verts, radius1=r1, radius2=r2, depth=depth, location=center)
    ob = bpy.context.object; ob.name = name; return ob

Z_HIP = 0.27; HC = Vector((0, 0.0, 0.72)); HR = 0.255; SH = 0.50

def face(SKIN, HAIR, EYEW, IRIS, BLACK, WHITE, MOUTH, BLUSH, iris_col=None, wrinkles=False, big_eyes=True):
    head = sphere('Head', HC, HR, scale=(1.0, 0.95, 0.98), seg=64, rings=32); finish(head, SKIN, 'Head')
    es = 1.0 if big_eyes else 0.85
    for sx in (1, -1):
        ew = sphere('EyeW_%d' % sx, (sx * 0.095, -0.215, HC.z - 0.005), 0.056 * es, scale=(1.0, 0.5, 1.25), seg=24, rings=14); finish(ew, EYEW, 'Head')
        ir = sphere('Iris_%d' % sx, (sx * 0.095, -0.246, HC.z - 0.01), 0.040 * es, scale=(1.0, 0.45, 1.2), seg=20, rings=12); finish(ir, IRIS, 'Head')
        pu = sphere('Pupil_%d' % sx, (sx * 0.095, -0.262, HC.z - 0.014), 0.024 * es, scale=(1.0, 0.4, 1.15), seg=16, rings=10); finish(pu, BLACK, 'Head')
        h1 = sphere('EyeHi_%d' % sx, (sx * 0.080, -0.272, HC.z + 0.012), 0.012, seg=10, rings=6); finish(h1, WHITE, 'Head')
        br = box('Brow_%d' % sx, (sx * 0.095, -0.232, HC.z + 0.075), (0.07, 0.012, 0.012), rot=(0, 0, math.radians(sx * 6)), bevel=0.004, segs=2); finish(br, HAIR, 'Head', smooth=False)
        bl = sphere('Blush_%d' % sx, (sx * 0.165, -0.19, HC.z - 0.07), 0.042, scale=(1.0, 0.35, 0.7), seg=16, rings=10); finish(bl, BLUSH, 'Head')
        if wrinkles:
            w = box('Wrinkle_%d' % sx, (sx * 0.20, -0.20, HC.z - 0.02), (0.03, 0.008, 0.008), rot=(0, 0, math.radians(sx * 30)), bevel=0.003, segs=2); finish(w, BLACK, 'Head', smooth=False)
    nose = sphere('Nose', (0, -0.252, HC.z - 0.045), 0.012, seg=12, rings=8); finish(nose, SKIN, 'Head')
    mouth = sphere('Mouth', (0, -0.245, HC.z - 0.10), 0.034, scale=(1.3, 0.35, 0.5), seg=16, rings=10); finish(mouth, MOUTH, 'Head')
    return head

def hair_cap(HAIR, extra=0.03, bangs=True):
    cap = sphere('HairCap', HC + Vector((0, 0.015, 0.02)), HR + extra, seg=64, rings=32)
    bisect(cap, HC + Vector((0, 0, 0.03)), Vector((0, 0.6, 1.0)).normalized())
    finish(cap, HAIR, 'Head')
    if bangs:
        bg = sphere('Bangs', HC + Vector((0, 0.0, 0.02)), HR + extra + 0.012, seg=64, rings=32)
        bisect(bg, HC + Vector((0, 0, 0.075)), Vector((0.08, 0, 1.0)).normalized())
        bisect(bg, HC + Vector((0, 0, 0.26)), (0, 0, -1)); bisect(bg, HC + Vector((0, -0.02, 0)), (0, -1, 0))
        finish(bg, HAIR, 'Head')
    return cap

def arms(mat_up, mat_fore, SKIN, cuff=None, hand_mat=None):
    for sx, side in ((1, 'Left'), (-1, 'Right')):
        x0 = sx * 0.15; x1 = sx * 0.25; x2 = sx * 0.34
        up = capsule('UpperArm_' + side, (x0, 0, SH), (x1, 0, SH), 0.056); finish(up, mat_up, side + 'Arm')
        fo = capsule('ForeArm_' + side, (x1, 0, SH), (x2, 0, SH), 0.052); finish(fo, mat_fore, side + 'ForeArm')
        if cuff is not None:
            c = torus('Cuff_' + side, (sx * 0.335, 0, SH), 0.053, 0.011, rot=(0, math.radians(90), 0)); finish(c, cuff, side + 'ForeArm')
        hand = sphere('Hand_' + side, (sx * 0.38, 0, SH), 0.05, scale=(1.1, 0.9, 0.95)); finish(hand, hand_mat or SKIN, side + 'Hand')

def legs(PANTS, SHOE, SOLE, bare=False, boots=None):
    for sx, side in ((1, 'Left'), (-1, 'Right')):
        x = sx * 0.08; kz = 0.16
        th = capsule('Thigh_' + side, (x, 0, Z_HIP), (x, 0, kz), 0.062); finish(th, PANTS, side + 'UpLeg')
        sh = capsule('Shin_' + side, (x, 0, kz), (x, 0, 0.08), 0.056); finish(sh, PANTS if not bare else SHOE, side + 'Leg')
        if boots is not None:
            bt = cylinder('Boot_' + side, (x, 0, 0.10), 0.064, 0.16, verts=32); finish(bt, boots, side + 'Leg')
        foot = capsule('Foot_' + side, (x, 0.03, 0.045), (x, -0.09, 0.04), 0.052)
        bisect(foot, (x, 0, 0.012), (0, 0, 1), fill=True); finish(foot, boots or SHOE, side + 'Foot')
        sole = cylinder('Sole_' + side, (x, -0.025, 0.009), 1.0, 0.018, verts=40, scale=(0.064, 0.118, 1.0)); finish(sole, SOLE, side + 'Foot')

def torso(mat, skirt=None, skirt_r=(0.24, 0.165), band=None):
    body = sphere('Body', Vector((0, 0, 0.48)), 0.175, scale=(1.0, 0.85, 0.85), seg=48, rings=24); finish(body, mat, 'Spine1')
    if skirt is not None:
        sk = cone('Skirt', (0, 0, 0.40), skirt_r[0], skirt_r[1], 0.30); finish(sk, skirt, 'Spine1')
    else:
        low = sphere('BodyLow', Vector((0, 0, 0.36)), 0.17, scale=(1.0, 0.85, 0.7), seg=48, rings=24); finish(low, mat, 'Hips')
    if band is not None:
        hb = torus('Hem', (0, 0, 0.255), skirt_r[0] - 0.005, 0.018, scale=(1.0, 0.9, 0.6)); finish(hb, band, 'Hips')
    return body

def rig_export(name):
    bpy.ops.object.select_all(action='DESELECT')
    for ob, bone, blend in PARTS:
        bpy.context.view_layer.objects.active = ob
        for m in list(ob.modifiers):
            try: bpy.ops.object.modifier_apply(modifier=m.name)
            except Exception as e: print('modifier apply failed', ob.name, m.name, e)
        vg = ob.vertex_groups.new(name=bone); vg.add(list(range(len(ob.data.vertices))), 1.0, 'REPLACE')
    mesh = join([ob for ob, _, _ in PARTS], name + '_Mesh')
    bpy.ops.object.armature_add(enter_editmode=True, location=(0, 0, 0))
    arm = bpy.context.object; arm.name = name; arm.data.name = name + 'Armature'
    eb = arm.data.edit_bones
    for b in list(eb): eb.remove(b)
    def bone(n, head, tail, parent=None, connect=False):
        b = eb.new(n); b.head = head; b.tail = tail
        if parent: b.parent = eb[parent]; b.use_connect = connect
        return b
    bone('Hips', (0, 0, Z_HIP), (0, 0, 0.35)); bone('Spine', (0, 0, 0.35), (0, 0, 0.42), 'Hips', True)
    bone('Spine1', (0, 0, 0.42), (0, 0, 0.48), 'Spine', True); bone('Spine2', (0, 0, 0.48), (0, 0, 0.54), 'Spine1', True)
    bone('Neck', (0, 0, 0.54), (0, 0, 0.57), 'Spine2', True); bone('Head', (0, 0, 0.57), (0, 0, 0.90), 'Neck', True)
    bone('HeadTop_End', (0, 0, 0.90), (0, 0, 1.02), 'Head', True)
    for sx, side in ((1, 'Left'), (-1, 'Right')):
        bone(side + 'Shoulder', (sx * 0.05, 0, 0.52), (sx * 0.15, 0, SH), 'Spine2')
        bone(side + 'Arm', (sx * 0.15, 0, SH), (sx * 0.25, 0, SH), side + 'Shoulder', True)
        bone(side + 'ForeArm', (sx * 0.25, 0, SH), (sx * 0.34, 0, SH), side + 'Arm', True)
        bone(side + 'Hand', (sx * 0.34, 0, SH), (sx * 0.42, 0, SH), side + 'ForeArm', True)
        bone(side + 'UpLeg', (sx * 0.08, 0, Z_HIP), (sx * 0.08, 0.005, 0.16), 'Hips')
        bone(side + 'Leg', (sx * 0.08, 0.005, 0.16), (sx * 0.08, 0, 0.06), side + 'UpLeg', True)
        bone(side + 'Foot', (sx * 0.08, 0, 0.06), (sx * 0.08, -0.09, 0.02), side + 'Leg', True)
        bone(side + 'ToeBase', (sx * 0.08, -0.09, 0.02), (sx * 0.08, -0.15, 0.02), side + 'Foot', True)
    bpy.ops.object.mode_set(mode='OBJECT')
    mesh.parent = arm
    md = mesh.modifiers.new('Armature', 'ARMATURE'); md.object = arm
    bpy.ops.object.select_all(action='DESELECT'); arm.select_set(True); mesh.select_set(True); bpy.context.view_layer.objects.active = arm
    out = os.path.join(OUT_DIR, name + '.fbx')
    bpy.ops.export_scene.fbx(filepath=out, use_selection=True, object_types={'ARMATURE', 'MESH'},
        apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y', add_leaf_bones=False, bake_anim=False,
        use_mesh_modifiers=True, mesh_smooth_type='FACE', path_mode='COPY', embed_textures=False, armature_nodetype='NULL',
        primary_bone_axis='Y', secondary_bone_axis='X', use_armature_deform_only=True)
    print('FBX ->', out)
    # preview
    scene = bpy.context.scene
    bpy.ops.object.camera_add(location=(1.5, -2.3, 0.95), rotation=(math.radians(80), 0, math.radians(33)))
    cam = bpy.context.object; cam.data.lens = 60; scene.camera = cam
    bpy.ops.object.light_add(type='SUN', location=(2, -3, 5), rotation=(math.radians(45), math.radians(20), math.radians(30))); bpy.context.object.data.energy = 3.0
    scene.render.engine = 'BLENDER_WORKBENCH'; scene.display.shading.light = 'STUDIO'; scene.display.shading.color_type = 'MATERIAL'
    scene.display.shading.show_shadows = True; scene.display.shading.show_cavity = True
    scene.render.resolution_x = 540; scene.render.resolution_y = 720
    world = bpy.data.worlds.new('W'); scene.world = world; world.color = (0.9, 0.9, 0.92)
    scene.render.filepath = os.path.join(PREV_DIR, name + '.png'); bpy.ops.render.render(write_still=True)

# ================================================================ 캐릭터들
def common_mats(skin=(1.0, 0.87, 0.76)):
    return dict(SKIN=mat('N_Skin', skin, 0.6), EYEW=mat('N_EyeWhite', (0.99, 0.99, 0.99), 0.3), BLACK=mat('N_Black', (0.05, 0.04, 0.04), 0.3),
                WHITE=mat('N_White', (1, 1, 1), 0.3), MOUTH=mat('N_Mouth', (0.85, 0.35, 0.40), 0.5), BLUSH=mat('N_Blush', (1.0, 0.62, 0.62), 0.7))

def reset():
    global PARTS
    bpy.ops.wm.read_factory_settings(use_empty=True)
    MATS.clear(); PARTS.clear()

def basket(parent_bone, center, r=0.11, h=0.09, fill=None):
    body = cylinder('Basket', center, r, h, verts=32, scale=(1.0, 0.8, 1.0)); finish(body, BASK, parent_bone)
    rim = torus('BasketRim', (center[0], center[1], center[2] + h * 0.5), r, 0.012, scale=(1.0, 0.8, 1.0)); finish(rim, BASKD, parent_bone)
    if fill is not None:
        for i in range(6):
            a = i * 1.05; f = sphere('Fill%d' % i, (center[0] + math.cos(a) * r * 0.5, center[1] + math.sin(a) * r * 0.4, center[2] + h * 0.55), 0.035, seg=12, rings=8); finish(f, fill, parent_bone)

def build_haenyeo():
    global BASK, BASKD
    reset(); M = common_mats((1.0, 0.88, 0.80))
    HAIR = mat('N_HairGray', (0.62, 0.62, 0.64), 0.7); IRIS = mat('N_Iris', (0.20, 0.14, 0.10), 0.3)
    SUIT = mat('N_Wetsuit', (0.16, 0.16, 0.18), 0.6); VEST = mat('N_Vest', (1.0, 0.50, 0.12), 0.55); VESTD = mat('N_VestDark', (0.85, 0.38, 0.08), 0.55)
    GLOVE = mat('N_Glove', (1.0, 0.55, 0.15), 0.5); BOOT = mat('N_BootBlk', (0.12, 0.12, 0.14), 0.4); SOLE = mat('N_Sole', (0.3, 0.3, 0.32), 0.5)
    BASK = mat('N_Basket', (0.72, 0.58, 0.32), 0.7); BASKD = mat('N_BasketD', (0.55, 0.42, 0.22), 0.7); SHELL = mat('N_Shell', (0.35, 0.45, 0.35), 0.5)
    face(M['SKIN'], mat('N_HairGrayD', (0.45, 0.45, 0.48), 0.7), M['EYEW'], IRIS, M['BLACK'], M['WHITE'], M['MOUTH'], M['BLUSH'], wrinkles=True, big_eyes=False)
    # 곱슬 회색 머리: 캡 + 방울 구 12개
    hair_cap(HAIR, extra=0.02, bangs=False)
    import random; rnd = random.Random(3)
    for i in range(14):
        a = i * 0.45 + 0.2; z = HC.z + 0.10 - (i % 3) * 0.10
        r = HR + 0.03
        c = (math.cos(a) * r * 0.95, math.sin(a) * r * 0.85 + 0.03, z)
        if c[1] < -0.16 and z < HC.z + 0.05: continue
        s = sphere('Curl%d' % i, c, 0.075 + rnd.random() * 0.02, seg=16, rings=10); finish(s, HAIR, 'Head')
    torso(SUIT)
    vest = sphere('Vest', Vector((0, 0, 0.45)), 0.19, scale=(1.02, 0.9, 0.82), seg=48, rings=24)
    bisect(vest, (0, 0, 0.56), (0, 0, -1)); bisect(vest, (0, 0, 0.33), (0, 0, 1), fill=True); finish(vest, VEST, 'Spine1')
    for sx in (1, -1):
        pk = box('VPocket_%d' % sx, (sx * 0.10, -0.175, 0.40), (0.07, 0.02, 0.06), bevel=0.01, segs=3); finish(pk, VESTD, 'Spine1')
    band = torus('VestBand', (0, 0, 0.36), 0.185, 0.012, scale=(1.0, 0.85, 0.7)); finish(band, SUIT, 'Hips')
    arms(SUIT, SUIT, M['SKIN'], cuff=GLOVE, hand_mat=GLOVE)
    legs(SUIT, BOOT, SOLE, boots=BOOT)
    basket('Spine1', (0, -0.27, 0.36), fill=SHELL)
    rig_export('Npc_Haenyeo')

def build_keeper():
    reset(); M = common_mats()
    HAIR = mat('N_HairBrown', (0.42, 0.28, 0.16), 0.7); IRIS = mat('N_Iris', (0.35, 0.22, 0.10), 0.3)
    NAVY = mat('N_Navy', (0.14, 0.20, 0.42), 0.55); NAVYD = mat('N_NavyD', (0.10, 0.14, 0.32), 0.55); RED = mat('N_Red', (0.85, 0.15, 0.18), 0.5)
    GOLD = mat('N_Gold', (0.95, 0.75, 0.20), 0.4); SCARF = mat('N_Scarf', (0.20, 0.32, 0.65), 0.6); SHOE = mat('N_ShoeNavy', (0.12, 0.16, 0.30), 0.4); SOLE = mat('N_Sole', (0.3, 0.3, 0.32), 0.5)
    GLASS = mat('N_Glass', (1.0, 0.85, 0.40), 0.2)
    face(M['SKIN'], HAIR, M['EYEW'], IRIS, M['BLACK'], M['WHITE'], M['MOUTH'], M['BLUSH'], big_eyes=False)
    hair_cap(HAIR, extra=0.025)
    beard = sphere('Beard', HC + Vector((0, -0.16, -0.15)), 0.15, scale=(1.25, 0.7, 0.55), seg=32, rings=16); finish(beard, HAIR, 'Head')
    capT = sphere('Cap', HC + Vector((0, 0.02, 0.12)), HR + 0.045, scale=(1.05, 1.0, 0.7), seg=48, rings=24)
    bisect(capT, HC + Vector((0, 0, 0.10)), (0, 0, 1), fill=True); finish(capT, RED, 'Head')
    visor = box('Visor', HC + Vector((0, -0.25, 0.11)), (0.34, 0.16, 0.03), bevel=0.012, segs=3); finish(visor, M['BLACK'], 'Head')
    anchor = sphere('Anchor', HC + Vector((0, -0.24, 0.22)), 0.03, scale=(1, 0.3, 1.2), seg=12, rings=8); finish(anchor, GOLD, 'Head')
    torso(NAVY, skirt=NAVY, skirt_r=(0.21, 0.165))
    for i, z in enumerate((0.47, 0.41, 0.35)):
        b = sphere('Button%d' % i, (0.0, -0.19, z), 0.017, scale=(1, 0.5, 1), seg=12, rings=8); finish(b, GOLD, 'Spine1')
    scarf = torus('Scarf', (0, -0.01, 0.55), 0.10, 0.035, scale=(1.05, 0.95, 0.7)); finish(scarf, SCARF, 'Spine1')
    tail = capsule('ScarfTail', (0.05, -0.17, 0.52), (0.07, -0.19, 0.36), 0.03); finish(tail, SCARF, 'Spine1')
    arms(NAVY, NAVY, M['SKIN'], cuff=NAVYD)
    for sx in (1, -1):
        st = box('Stripe_%d' % sx, (sx * 0.20, -0.045, SH + 0.02), (0.05, 0.02, 0.012), bevel=0.003, segs=2); finish(st, GOLD, ('Left' if sx > 0 else 'Right') + 'Arm', smooth=False)
    legs(NAVYD, SHOE, SOLE)
    # 랜턴(왼손 아래)
    lx = 0.40
    lb = box('LanternBase', (lx, 0, SH - 0.20), (0.11, 0.11, 0.03), bevel=0.008); finish(lb, GOLD, 'LeftHand')
    lg = box('LanternGlass', (lx, 0, SH - 0.13), (0.085, 0.085, 0.11)); finish(lg, GLASS, 'LeftHand')
    lt = box('LanternTop', (lx, 0, SH - 0.065), (0.11, 0.11, 0.025), bevel=0.008); finish(lt, GOLD, 'LeftHand')
    lh = torus('LanternHandle', (lx, 0, SH - 0.02), 0.04, 0.008, rot=(math.radians(90), 0, 0)); finish(lh, GOLD, 'LeftHand')
    rig_export('Npc_Keeper')

def build_florist():
    global BASK, BASKD
    reset(); M = common_mats()
    HAIR = mat('N_HairBrownL', (0.50, 0.32, 0.18), 0.7); IRIS = mat('N_Iris', (0.38, 0.24, 0.12), 0.3)
    BLOUSE = mat('N_Blouse', (0.98, 0.98, 0.97), 0.6); APRON = mat('N_Apron', (0.45, 0.62, 0.35), 0.6); APROND = mat('N_ApronD', (0.35, 0.50, 0.27), 0.6)
    SHOE = mat('N_ShoeBrown', (0.42, 0.28, 0.18), 0.4); SOLE = mat('N_Sole', (0.3, 0.3, 0.32), 0.5)
    BASK = mat('N_Basket', (0.72, 0.58, 0.32), 0.7); BASKD = mat('N_BasketD', (0.55, 0.42, 0.22), 0.7); FLW = mat('N_FlowerO', (1.0, 0.55, 0.20), 0.5)
    face(M['SKIN'], HAIR, M['EYEW'], IRIS, M['BLACK'], M['WHITE'], M['MOUTH'], M['BLUSH'])
    hair_cap(HAIR, extra=0.03)
    back = sphere('HairLong', HC + Vector((0, 0.09, -0.22)), 0.24, scale=(1.15, 0.75, 1.6), seg=48, rings=24)
    bisect(back, HC + Vector((0, 0, 0.08)), (0, 0, -1)); bisect(back, HC + Vector((0, -0.06, 0)), (0, 1, 0)); finish(back, HAIR, 'Head')
    for sx in (1, -1):
        lock = capsule('Lock_%d' % sx, (sx * 0.24, -0.04, HC.z + 0.02), (sx * 0.27, -0.02, HC.z - 0.36), 0.065); finish(lock, HAIR, 'Head')
    torso(BLOUSE, skirt=APRON, skirt_r=(0.23, 0.165), band=APROND)
    bib = box('Bib', (0, -0.175, 0.50), (0.17, 0.02, 0.14), bevel=0.01, segs=3); finish(bib, APRON, 'Spine1')
    for sx in (1, -1):
        strap = capsule('Strap_%d' % sx, (sx * 0.07, -0.17, 0.57), (sx * 0.09, 0.05, 0.60), 0.014); finish(strap, APRON, 'Spine1')
    pk = box('ApronPocket', (0.0, -0.215, 0.36), (0.09, 0.02, 0.06), bevel=0.01, segs=3); finish(pk, APROND, 'Spine1')
    arms(BLOUSE, M['SKIN'], M['SKIN'])
    legs(M['SKIN'], SHOE, SOLE, bare=True)
    basket('RightHand', (-0.42, 0, SH - 0.17), fill=FLW)
    rig_export('Npc_Florist')

def build_fisherboy():
    reset(); M = common_mats()
    HAIR = mat('N_HairBrown', (0.42, 0.28, 0.16), 0.7); IRIS = mat('N_Iris', (0.35, 0.22, 0.10), 0.3)
    TEE = mat('N_TeeWhite', (0.98, 0.98, 0.98), 0.6); BLUE = mat('N_Blue', (0.22, 0.42, 0.78), 0.55); KHAKI = mat('N_Khaki', (0.82, 0.72, 0.52), 0.6)
    BAG = mat('N_Bag', (0.45, 0.50, 0.32), 0.6); SHOE = mat('N_ShoeBlue', (0.16, 0.28, 0.58), 0.4); SOLE = mat('N_Sole', (0.85, 0.85, 0.85), 0.5); WOOD = mat('N_Wood', (0.55, 0.36, 0.18), 0.6)
    face(M['SKIN'], HAIR, M['EYEW'], IRIS, M['BLACK'], M['WHITE'], M['MOUTH'], M['BLUSH'])
    hair_cap(HAIR, extra=0.025)
    capT = sphere('Cap', HC + Vector((0, 0.01, 0.10)), HR + 0.05, scale=(1.05, 1.0, 0.72), seg=48, rings=24)
    bisect(capT, HC + Vector((0, 0, 0.08)), (0, 0, 1), fill=True); finish(capT, BLUE, 'Head')
    visor = box('Visor', HC + Vector((0, -0.27, 0.10)), (0.30, 0.18, 0.03), bevel=0.012, segs=3); finish(visor, BLUE, 'Head')
    torso(TEE)
    for i, z in enumerate((0.55, 0.47, 0.39)):
        st = torus('Stripe%d' % i, (0, 0, z), 0.176 if i == 1 else 0.165, 0.016, scale=(1.0, 0.85, 0.7)); finish(st, BLUE, 'Spine1')
    shorts = cone('Shorts', (0, 0, 0.30), 0.19, 0.17, 0.10); finish(shorts, KHAKI, 'Hips')
    bag = box('Bag', (0.17, -0.10, 0.36), (0.11, 0.07, 0.09), bevel=0.02, segs=3); finish(bag, BAG, 'Spine1')
    strap = capsule('BagStrap', (0.14, -0.14, 0.42), (-0.10, -0.02, 0.60), 0.012); finish(strap, BAG, 'Spine1')
    arms(TEE, M['SKIN'], M['SKIN'], cuff=BLUE)
    legs(KHAKI, SHOE, SOLE)
    rod = capsule('Rod', (-0.40, -0.02, SH - 0.05), (-0.62, -0.12, SH + 0.62), 0.012); finish(rod, WOOD, 'RightHand')
    rig_export('Npc_FisherBoy')

def build_cafe():
    reset(); M = common_mats()
    HAIR = mat('N_HairBlonde', (0.92, 0.78, 0.45), 0.7); IRIS = mat('N_IrisBlue', (0.25, 0.45, 0.80), 0.3)
    TEE = mat('N_TeeBeige', (0.88, 0.80, 0.66), 0.6); PINK = mat('N_Pink', (0.96, 0.45, 0.55), 0.55); PINKD = mat('N_PinkD', (0.85, 0.35, 0.45), 0.55)
    SHOE = mat('N_ShoeBrown', (0.42, 0.28, 0.18), 0.4); SOLE = mat('N_Sole', (0.3, 0.3, 0.32), 0.5); CUP = mat('N_Cup', (0.45, 0.28, 0.15), 0.3); LID = mat('N_Lid', (0.95, 0.95, 0.97), 0.3); STRAW = mat('N_Straw', (0.35, 0.75, 0.35), 0.4)
    face(M['SKIN'], HAIR, M['EYEW'], IRIS, M['BLACK'], M['WHITE'], M['MOUTH'], M['BLUSH'])
    hair_cap(HAIR, extra=0.03)
    for sx in (1, -1):
        tail = sphere('Tail_%d' % sx, (sx * 0.26, 0.02, HC.z - 0.12), 0.10, scale=(0.7, 0.9, 1.5), seg=24, rings=14); finish(tail, HAIR, 'Head')
        for k, dz in enumerate((0.02, -0.02)):
            bow = sphere('Bow_%d_%d' % (sx, k), (sx * 0.27, -0.02, HC.z + 0.02 + dz), 0.03, scale=(0.9, 0.6, 0.6), seg=12, rings=8); finish(bow, PINK, 'Head')
    torso(TEE, skirt=PINK, skirt_r=(0.24, 0.165), band=PINKD)
    bib = box('Bib', (0, -0.175, 0.49), (0.16, 0.02, 0.12), bevel=0.01, segs=3); finish(bib, PINK, 'Spine1')
    for sx in (1, -1):
        strap = capsule('Strap_%d' % sx, (sx * 0.07, -0.17, 0.55), (sx * 0.08, 0.05, 0.59), 0.014); finish(strap, PINK, 'Spine1')
    badge = sphere('Badge', (-0.08, -0.20, 0.50), 0.03, scale=(1, 0.3, 0.8), seg=12, rings=8); finish(badge, mat('N_Badge', (0.25, 0.35, 0.70), 0.4), 'Spine1')
    arms(TEE, M['SKIN'], M['SKIN'])
    legs(M['SKIN'], SHOE, SOLE, bare=True)
    cup = cylinder('Cup', (0.42, -0.02, SH - 0.12), 0.045, 0.12, verts=24); finish(cup, CUP, 'LeftHand')
    lid = cylinder('Lid', (0.42, -0.02, SH - 0.05), 0.05, 0.015, verts=24); finish(lid, LID, 'LeftHand')
    straw = cylinder('Straw', (0.43, -0.02, SH + 0.01), 0.008, 0.10, verts=8); finish(straw, STRAW, 'LeftHand')
    rig_export('Npc_Cafe')

def build_surfer():
    reset(); M = common_mats((0.78, 0.50, 0.30))
    HAIR = mat('N_HairBlack', (0.08, 0.08, 0.10), 0.7); IRIS = mat('N_Iris', (0.20, 0.14, 0.10), 0.3)
    MINT = mat('N_Mint', (0.40, 0.85, 0.72), 0.55); MINTD = mat('N_MintD', (0.30, 0.70, 0.58), 0.55); BOARD = mat('N_Board', (0.95, 0.85, 0.62), 0.4); BOARDS = mat('N_BoardStripe', (0.25, 0.55, 0.65), 0.4)
    SOLE = mat('N_Sole', (0.3, 0.3, 0.32), 0.5)
    face(M['SKIN'], HAIR, M['EYEW'], IRIS, M['BLACK'], M['WHITE'], M['MOUTH'], M['BLUSH'])
    hair_cap(HAIR, extra=0.03)
    pony = capsule('Pony', HC + Vector((0, 0.16, 0.22)), HC + Vector((0, 0.30, -0.40)), 0.075); finish(pony, HAIR, 'Head')
    scr = torus('Scrunchie', HC + Vector((0, 0.17, 0.20)), 0.075, 0.025, rot=(math.radians(60), 0, 0)); finish(scr, MINT, 'Head')
    torso(MINT)
    shorts = cone('Shorts', (0, 0, 0.30), 0.19, 0.17, 0.10); finish(shorts, MINTD, 'Hips')
    arms(MINT, MINT, M['SKIN'])
    legs(M['SKIN'], M['SKIN'], SOLE, bare=True)
    board = sphere('Board', (0.46, 0.0, 0.40), 1.0, scale=(0.04, 0.16, 0.50), seg=32, rings=20); finish(board, BOARD, 'LeftHand')
    stripe = sphere('BoardStripe', (0.475, 0.0, 0.40), 1.0, scale=(0.03, 0.05, 0.42), seg=24, rings=16); finish(stripe, BOARDS, 'LeftHand')
    rig_export('Npc_Surfer')

for fn in (build_haenyeo, build_keeper, build_florist, build_fisherboy, build_cafe, build_surfer):
    try: fn()
    except Exception as e:
        import traceback; traceback.print_exc(); print('FAILED', fn.__name__, e)
print('ALL DONE')
