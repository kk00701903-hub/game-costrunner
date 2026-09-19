# -*- coding: utf-8 -*-
"""치비 하늘 — UI 렌더(투명 PNG). 126차(사용자 시안): 떠 있는 흙섬 위에 무릎 안고 앉은 하늘 + 서 있는 포즈.

Run:  blender -b Tools/blender/haneul_chibi_rig.blend --python haneul_chibi_renders.py -- <out_dir>
Outputs:
  UI_RunOver_Sad.png   900×1000  — 흙섬 위 무릎 안고 앉은 하늘(러닝 실패 카드)
  UI_Haneul_Stand.png  700×1000  — 서 있는 하늘(우리 집 방)
  haneul_pose_preview.png        — 확인용(불투명)
"""
import bpy, math, sys, os, bmesh
from mathutils import Vector, Matrix, Euler

argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
OUT = argv[0] if argv else r'C:\dev\game\Tools\blender\out'
os.makedirs(OUT, exist_ok=True)
scene = bpy.context.scene
for _m in bpy.data.materials:
    if _m.name == 'HN_Skin' and _m.use_nodes:
        _m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (1.0, 0.80, 0.66, 1)   # 렌더 전용: 조명에 날아가지 않게
arm = bpy.data.objects['HaneulChibi']
mesh = bpy.data.objects['HaneulChibi_Mesh']

def rot_world(pb, axis, deg):
    ax = {'x': Vector((1, 0, 0)), 'y': Vector((0, 1, 0)), 'z': Vector((0, 0, 1))}[axis]
    Rw = Matrix.Rotation(math.radians(deg), 3, ax)
    Rr = (arm.matrix_world.to_3x3() @ pb.bone.matrix_local.to_3x3())
    Rl = Rr.inverted() @ Rw @ Rr
    pb.rotation_mode = 'QUATERNION'
    pb.rotation_quaternion = (pb.rotation_quaternion.to_matrix() @ Rl).to_quaternion()

def reset_pose():
    for pb in arm.pose.bones:
        pb.rotation_mode = 'QUATERNION'; pb.rotation_quaternion = (1, 0, 0, 0); pb.location = (0, 0, 0)

HIP_DROP = 0.19
def pose_sit():
    reset_pose()
    P = arm.pose.bones
    P['Hips'].location = (0, -HIP_DROP, 0.0)          # 뼈 로컬 Y = 위 → 엉덩이를 내린다
    for side in ('Left', 'Right'):
        s = 1 if side == 'Left' else -1
        rot_world(P[side + 'UpLeg'], 'x', -118)       # 허벅지 앞·위로(무릎 세움)
        rot_world(P[side + 'UpLeg'], 'z', s * 6)
        rot_world(P[side + 'Leg'], 'x', 128)          # 정강이 아래로
        rot_world(P[side + 'Foot'], 'x', -20)
        rot_world(P[side + 'Arm'], 'z', -s * 70)      # 팔 앞으로
        rot_world(P[side + 'Arm'], 'x', 10)
        rot_world(P[side + 'ForeArm'], 'z', -s * 70)  # 무릎을 감싸듯
        rot_world(P[side + 'ForeArm'], 'x', -6)
    rot_world(P['Spine1'], 'x', 8)
    rot_world(P['Head'], 'x', 6)
    rot_world(P['Head'], 'y', -7)                    # 갸웃

def pose_stand():
    reset_pose()
    P = arm.pose.bones
    for side in ('Left', 'Right'):
        s = 1 if side == 'Left' else -1
        rot_world(P[side + 'Arm'], 'y', s * 70)       # 팔 내리기(T→A 포즈)
        rot_world(P[side + 'ForeArm'], 'y', s * 8)
    rot_world(P['Head'], 'y', 4)

# ---------------------------------------------------------------- 흙섬
def island(z_top):
    bpy.ops.mesh.primitive_cylinder_add(vertices=48, radius=0.66, depth=0.22, location=(0, 0.04, z_top - 0.11))
    isl = bpy.context.object; isl.name = 'Island'; isl.scale = (1.0, 0.72, 1.0)
    bpy.ops.object.transform_apply(scale=True)
    b = isl.modifiers.new('Bevel', 'BEVEL'); b.width = 0.07; b.segments = 5
    # 아래쪽 울퉁불퉁: 바닥 정점을 랜덤하게 내린다
    import random; random.seed(3)
    for v in isl.data.vertices:
        if v.co.z < z_top - 0.15:
            v.co.z -= random.random() * 0.09
            v.co.x *= 0.92; v.co.y *= 0.92
    m = bpy.data.materials.new('IslandDirt'); m.use_nodes = True
    bsdf = m.node_tree.nodes['Principled BSDF']; bsdf.inputs['Base Color'].default_value = (0.42, 0.26, 0.15, 1); bsdf.inputs['Roughness'].default_value = 0.95
    isl.data.materials.append(m)
    for p in isl.data.polygons: p.use_smooth = True
    # 윗면(흙 마당) — 살짝 밝은 원반
    bpy.ops.mesh.primitive_cylinder_add(vertices=48, radius=0.62, depth=0.02, location=(0, 0.04, z_top + 0.005))
    top = bpy.context.object; top.name = 'IslandTop'; top.scale = (1.0, 0.72, 1.0); bpy.ops.object.transform_apply(scale=True)
    mt = bpy.data.materials.new('IslandTop'); mt.use_nodes = True
    bt = mt.node_tree.nodes['Principled BSDF']; bt.inputs['Base Color'].default_value = (0.60, 0.42, 0.26, 1); bt.inputs['Roughness'].default_value = 0.9
    top.data.materials.append(mt)
    # 풀 몇 포기
    mg = bpy.data.materials.new('Grass'); mg.use_nodes = True
    bg = mg.node_tree.nodes['Principled BSDF']; bg.inputs['Base Color'].default_value = (0.45, 0.70, 0.30, 1)
    random.seed(5)
    for i in range(9):
        a = random.random() * 6.283; r = 0.40 + random.random() * 0.20
        x, y = math.cos(a) * r, math.sin(a) * r * 0.72 + 0.02
        for j in range(3):
            bpy.ops.mesh.primitive_cone_add(vertices=6, radius1=0.014, radius2=0.0, depth=0.07 + random.random() * 0.04,
                                            location=(x + (j - 1) * 0.02, y, z_top + 0.045), rotation=(math.radians((j - 1) * 22), 0, 0))
            c = bpy.context.object; c.name = 'Grass'; c.data.materials.append(mg)
    # 작은 별 몇 개(노랑) — 섬 위·주변
    ms = bpy.data.materials.new('StarY'); ms.use_nodes = True
    bs = ms.node_tree.nodes['Principled BSDF']; bs.inputs['Base Color'].default_value = (1.0, 0.85, 0.25, 1); bs.inputs['Emission Color'].default_value = (1.0, 0.8, 0.2, 1); bs.inputs['Emission Strength'].default_value = 1.2
    for (sx, sy, sz, sr) in ((0.62, -0.30, z_top + 0.03, 0.05), (-0.55, -0.34, z_top + 0.02, 0.04), (0.30, -0.44, z_top + 0.015, 0.03)):
        bm = bmesh.new(); vs = []
        for k in range(10):
            ang = math.pi / 2 + k * math.pi / 5; rr = sr if k % 2 == 0 else sr * 0.45
            vs.append(bm.verts.new((math.cos(ang) * rr, math.sin(ang) * rr, 0)))
        f = bm.faces.new(vs); res = bmesh.ops.extrude_face_region(bm, geom=[f])
        bmesh.ops.translate(bm, verts=[e for e in res['geom'] if isinstance(e, bmesh.types.BMVert)], vec=(0, 0, 0.012))
        me = bpy.data.meshes.new('Star'); bm.to_mesh(me); bm.free()
        ob = bpy.data.objects.new('Star', me); bpy.context.collection.objects.link(ob); ob.location = (sx, sy, sz); ob.data.materials.append(ms)

# ---------------------------------------------------------------- render setup
def setup(w, h, transparent=True):
    for eng in ('BLENDER_EEVEE', 'BLENDER_EEVEE_NEXT'):
        try: scene.render.engine = eng; break
        except Exception: continue
    try: scene.eevee.taa_render_samples = 48
    except Exception: pass
    scene.render.resolution_x = w; scene.render.resolution_y = h; scene.render.resolution_percentage = 100
    scene.render.film_transparent = transparent
    scene.render.image_settings.file_format = 'PNG'; scene.render.image_settings.color_mode = 'RGBA'
    scene.view_settings.view_transform = 'Standard'
    for o in [o for o in bpy.data.objects if o.type in ('LIGHT', 'CAMERA')]:
        bpy.data.objects.remove(o, do_unlink=True)
    bpy.ops.object.light_add(type='SUN', location=(1.5, -2.5, 4)); sun = bpy.context.object; sun.data.energy = 2.3; sun.data.angle = math.radians(14)
    sun.rotation_euler = Euler((math.radians(48), math.radians(8), math.radians(22)))
    bpy.ops.object.light_add(type='AREA', location=(-2.2, -2.6, 1.6)); fill = bpy.context.object; fill.data.energy = 70; fill.data.size = 3; fill.data.color = (0.9, 0.88, 1.0)
    fill.rotation_euler = Euler((math.radians(70), 0, math.radians(-40)))
    bpy.ops.object.light_add(type='AREA', location=(0.5, 2.5, 2.2)); rim = bpy.context.object; rim.data.energy = 120; rim.data.size = 2; rim.data.color = (0.85, 0.75, 1.0)
    rim.rotation_euler = Euler((math.radians(-60), 0, 0))
    world = scene.world or bpy.data.worlds.new('W'); scene.world = world; world.use_nodes = True
    bg = world.node_tree.nodes.get('Background')
    if bg: bg.inputs[0].default_value = (0.75, 0.70, 0.95, 1); bg.inputs[1].default_value = 0.3

def camera(loc, look_at, lens=55):
    bpy.ops.object.camera_add(location=loc); cam = bpy.context.object
    cam.rotation_euler = (Vector(look_at) - Vector(loc)).to_track_quat('-Z', 'Y').to_euler(); cam.data.lens = lens
    scene.camera = cam; return cam

def render(path):
    scene.render.filepath = path; bpy.ops.render.render(write_still=True); print('PNG ->', path)

# 1) 앉은 하늘 + 흙섬
setup(900, 1000)
pose_sit(); bpy.context.view_layer.update()
island(0.10)
cam = camera((0.50, -2.2, 0.90), (0.0, 0.0, 0.40), lens=60)
render(os.path.join(OUT, 'UI_RunOver_Sad.png'))
scene.render.film_transparent = False
render(os.path.join(OUT, 'haneul_pose_preview.png'))
bpy.data.objects.remove(cam, do_unlink=True)
for o in [o for o in bpy.data.objects if o.name.startswith(('Island', 'Grass', 'Star'))]: bpy.data.objects.remove(o, do_unlink=True)

# 2) 서 있는 하늘
setup(700, 1000)
pose_stand(); bpy.context.view_layer.update()
cam = camera((0.35, -2.6, 0.62), (0.0, 0.0, 0.56), lens=62)
render(os.path.join(OUT, 'UI_Haneul_Stand.png'))
