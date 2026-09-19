# -*- coding: utf-8 -*-
"""꼬마(주황 우비) 치비 3D 모델 — 138차: 마을에서 주인공 옆을 따라 걷는 동행. 하늘 치비와 같은 Humanoid 뼈(Mixamo 이름)라
러닝 클립(Anim_Run …)을 그대로 리타겟한다. Run:  blender -b --python kid_chibi_rig.py -- <out_fbx> <preview_png>
"""
import bpy, bmesh, math, sys, os
from mathutils import Vector

argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
OUT_FBX = argv[0] if argv else r'C:\dev\game\Assets\Resources\CoastRun\Rig\KidChibi.fbx'
OUT_PNG = argv[1] if len(argv) > 1 else r'C:\dev\game\Tools\blender\kid_chibi_preview.png'
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

# ---------------------------------------------------------------- 비율 (m) — 하늘보다 작고 통통하게
Z_HIP = 0.27
LEG_EXT = 0.0
HC = Vector((0, 0.0, 0.72)); HR = 0.255          # 머리 중심/반지름 (더 큰 머리)
BODY_C = Vector((0, 0, 0.42))
SH = 0.50                                        # 어깨 높이

# ---------------------------------------------------------------- 머리·얼굴 (Head)
head = sphere('Head', HC, HR, scale=(1.0, 0.95, 0.98), seg=64, rings=32); finish(head, SKIN, 'Head')
for sx in (1, -1):
    ew = sphere('EyeW_%d' % sx, (sx * 0.095, -0.215, HC.z - 0.005), 0.056, scale=(1.0, 0.5, 1.25), seg=24, rings=14); finish(ew, EYEW, 'Head')
    ir = sphere('Iris_%d' % sx, (sx * 0.095, -0.246, HC.z - 0.01), 0.040, scale=(1.0, 0.45, 1.2), seg=20, rings=12); finish(ir, IRIS, 'Head')
    pu = sphere('Pupil_%d' % sx, (sx * 0.095, -0.262, HC.z - 0.014), 0.024, scale=(1.0, 0.4, 1.15), seg=16, rings=10); finish(pu, BLACK, 'Head')
    h1 = sphere('EyeHi_%d' % sx, (sx * 0.080, -0.272, HC.z + 0.012), 0.012, seg=10, rings=6); finish(h1, WHITE, 'Head')
    br = box('Brow_%d' % sx, (sx * 0.095, -0.232, HC.z + 0.075), (0.07, 0.012, 0.012), rot=(0, 0, math.radians(sx * 6)), bevel=0.004, segs=2); finish(br, HAIR, 'Head', smooth=False)
    bl = sphere('Blush_%d' % sx, (sx * 0.165, -0.19, HC.z - 0.07), 0.042, scale=(1.0, 0.35, 0.7), seg=16, rings=10); finish(bl, BLUSH, 'Head')
nose = sphere('Nose', (0, -0.252, HC.z - 0.045), 0.012, seg=12, rings=8); finish(nose, SKIN, 'Head')
mouth = sphere('Mouth', (0, -0.245, HC.z - 0.10), 0.034, scale=(1.3, 0.35, 0.5), seg=16, rings=10); finish(mouth, MOUTH, 'Head')
# 앞머리(우비 모자 아래로 보이는 갈색 단발)
bangs = sphere('Bangs', HC + Vector((0, 0.0, 0.02)), HR + 0.03, seg=64, rings=32)
bisect(bangs, HC + Vector((0, 0, 0.06)), Vector((0.06, 0, 1.0)).normalized())     # 눈썹 위부터
bisect(bangs, HC + Vector((0, 0, 0.19)), (0, 0, -1))
bisect(bangs, HC + Vector((0, -0.03, 0)), (0, -1, 0))                               # 앞쪽만
finish(bangs, HAIR, 'Head')
for sx in (1, -1):   # 옆머리 살짝
    sd = sphere('HairSide_%d' % sx, (sx * 0.23, -0.02, HC.z - 0.06), 0.08, scale=(0.5, 0.9, 1.3), seg=20, rings=12); finish(sd, HAIR, 'Head')
# 우비 모자(후드): 머리를 감싸는 주황 구 — 얼굴 앞은 잘라 낸다 + 얼굴 둘레 챙
hood = sphere('Hood', HC + Vector((0, 0.03, 0.03)), HR + 0.06, seg=64, rings=32)
bisect(hood, HC + Vector((0, -0.14, 0)), Vector((0, 1.0, 0.0)))                      # 얼굴 앞(-Y) 제거
bisect(hood, HC + Vector((0, 0, -0.16)), (0, 0, 1), fill=True)                       # 턱 아래 정리
finish(hood, COAT, 'Head')
rim = torus('HoodRim', HC + Vector((0, -0.135, 0.01)), HR + 0.02, 0.028, rot=(math.radians(90), 0, 0), scale=(1.0, 1.05, 1.0)); finish(rim, COATD, 'Head')
tip = sphere('HoodTip', HC + Vector((0, 0.12, 0.30)), 0.05, scale=(0.8, 1.2, 0.9), seg=16, rings=10); finish(tip, COAT, 'Head')

# ---------------------------------------------------------------- 몸통 (Spine1) — 주황 우비(아래로 퍼짐) + 단추 + 주머니
body = sphere('Body', BODY_C + Vector((0, 0, 0.06)), 0.175, scale=(1.0, 0.85, 0.85), seg=48, rings=24); finish(body, COAT, 'Spine1')
skirt = cone('CoatSkirt', (0, 0, 0.40), 0.24, 0.165, 0.30); finish(skirt, COAT, 'Spine1')
hem = torus('CoatHem', (0, 0, 0.255), 0.235, 0.018, scale=(1.0, 0.9, 0.6)); finish(hem, COATD, 'Hips')
for i, z in enumerate((0.50, 0.44, 0.38)):
    b = sphere('Button_%d' % i, (0, -0.19 - (0.235 - 0.165) * (0.50 - z) / 0.3 * 0.85, z), 0.02, scale=(1, 0.5, 1), seg=12, rings=8); finish(b, YELLOW, 'Spine1')
for sx in (1, -1):
    pk = box('Pocket_%d' % sx, (sx * 0.12, -0.185, 0.34), (0.075, 0.02, 0.06), bevel=0.012, segs=3); finish(pk, COATD, 'Spine1')

# ---------------------------------------------------------------- 팔 (T-포즈)
for sx, side in ((1, 'Left'), (-1, 'Right')):
    x0 = sx * 0.15; x1 = sx * 0.25; x2 = sx * 0.34
    up = capsule('UpperArm_' + side, (x0, 0, SH), (x1, 0, SH), 0.056); finish(up, COAT, side + 'Arm')
    fo = capsule('ForeArm_' + side, (x1, 0, SH), (x2, 0, SH), 0.052); finish(fo, COAT, side + 'ForeArm')
    cuff = torus('Cuff_' + side, (sx * 0.335, 0, SH), 0.053, 0.011, rot=(0, math.radians(90), 0)); finish(cuff, YELLOW, side + 'ForeArm')
    hand = sphere('Hand_' + side, (sx * 0.38, 0, SH), 0.05, scale=(1.1, 0.9, 0.95)); finish(hand, SKIN, side + 'Hand')

# ---------------------------------------------------------------- 다리·장화
for sx, side in ((1, 'Left'), (-1, 'Right')):
    x = sx * 0.08
    kz = 0.16
    th = capsule('Thigh_' + side, (x, 0, Z_HIP), (x, 0, kz), 0.062); finish(th, PANTS, side + 'UpLeg')
    sh = capsule('Shin_' + side, (x, 0, kz), (x, 0, 0.08), 0.056); finish(sh, PANTS, side + 'Leg')
    boot = cylinder('Boot_' + side, (x, 0, 0.10), 0.064, 0.16, verts=32); finish(boot, BOOT, side + 'Leg')
    bootTop = torus('BootTop_' + side, (x, 0, 0.178), 0.062, 0.010); finish(bootTop, SOLE, side + 'Leg')
    foot = capsule('Foot_' + side, (x, 0.03, 0.045), (x, -0.09, 0.04), 0.052)
    bisect(foot, (x, 0, 0.012), (0, 0, 1), fill=True); finish(foot, BOOT, side + 'Foot')
    sole = cylinder('Sole_' + side, (x, -0.025, 0.009), 1.0, 0.018, verts=40, scale=(0.064, 0.118, 1.0)); finish(sole, SOLE, side + 'Foot')

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
girl = join([ob for ob, _, _ in PARTS], 'KidChibi_Mesh')

# ---------------------------------------------------------------- 아마추어 (Mixamo 이름)
bpy.ops.object.armature_add(enter_editmode=True, location=(0, 0, 0))
arm = bpy.context.object; arm.name = 'KidChibi'; arm.data.name = 'KidChibiArmature'
eb = arm.data.edit_bones
for b in list(eb): eb.remove(b)
def bone(name, head, tail, parent=None, connect=False):
    b = eb.new(name); b.head = head; b.tail = tail
    if parent: b.parent = eb[parent]; b.use_connect = connect
    return b
bone('Hips',   (0, 0, Z_HIP), (0, 0, 0.35))
bone('Spine',  (0, 0, 0.35), (0, 0, 0.42), 'Hips', True)
bone('Spine1', (0, 0, 0.42), (0, 0, 0.48), 'Spine', True)
bone('Spine2', (0, 0, 0.48), (0, 0, 0.54), 'Spine1', True)
bone('Neck',   (0, 0, 0.54), (0, 0, 0.57), 'Spine2', True)
bone('Head',   (0, 0, 0.57), (0, 0, 0.90), 'Neck', True)
bone('HeadTop_End', (0, 0, 0.90), (0, 0, 1.02), 'Head', True)
# 132차: 머리카락 뼈(Humanoid 밖 — Unity SkaterRig 가 LateUpdate 에서 스프링으로 흔든다)
bone('HairBack', (0, 0.12, HC.z + 0.12), (0, 0.20, HC.z - 0.18), 'Head')
bone('HairL', (0.20, 0.05, HC.z + 0.05), (0.24, 0.05, HC.z - 0.20), 'Head')
bone('HairR', (-0.20, 0.05, HC.z + 0.05), (-0.24, 0.05, HC.z - 0.20), 'Head')
for sx, side in ((1, 'Left'), (-1, 'Right')):
    bone(side + 'Shoulder', (sx * 0.05, 0, 0.52), (sx * 0.15, 0, SH), 'Spine2')
    bone(side + 'Arm',      (sx * 0.15, 0, SH), (sx * 0.25, 0, SH), side + 'Shoulder', True)
    bone(side + 'ForeArm',  (sx * 0.25, 0, SH), (sx * 0.34, 0, SH), side + 'Arm', True)
    bone(side + 'Hand',     (sx * 0.34, 0, SH), (sx * 0.42, 0, SH), side + 'ForeArm', True)
    E = 0.0; kz = 0.16
    bone(side + 'UpLeg',    (sx * 0.08, 0, Z_HIP), (sx * 0.08, 0.005, kz), 'Hips')
    bone(side + 'Leg',      (sx * 0.08, 0.005, kz), (sx * 0.08, 0, 0.06), side + 'UpLeg', True)
    bone(side + 'Foot',     (sx * 0.08, 0, 0.06), (sx * 0.08, -0.09, 0.02), side + 'Leg', True)
    bone(side + 'ToeBase',  (sx * 0.08, -0.09, 0.02), (sx * 0.08, -0.15, 0.02), side + 'Foot', True)
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
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(os.path.dirname(OUT_PNG), 'kid_chibi_rig.blend'))
print('PNG ->', OUT_PNG)
