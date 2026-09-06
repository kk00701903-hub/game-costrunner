"""절차 생성 달리기 사이클 — Mixamo 스켈레톤(Anim_Skate.fbx)에 Run 액션을 만들어 Anim_Run.fbx로 내보낸다.
   blender -b --python run_cycle.py
   Unity: Rig 폴더 Humanoid 임포트 → Coast Run/Art/Build Skater animator → RunnerAnimator.controller."""
import bpy, math, os, sys
from mathutils import Vector, Quaternion

ROOT = r"C:\dev\game"
SRC = os.path.join(ROOT, "Assets", "Resources", "CoastRun", "Rig", "Anim_Skate.fbx")
DST = os.path.join(ROOT, "Assets", "Resources", "CoastRun", "Rig", "Anim_Run.fbx")
FPS = 30
FRAMES = 20          # 한 사이클 0.67초
LOG = open(os.path.join(ROOT, "Tools", "blender", "run_cycle_log.txt"), "w", encoding="utf-8")

def log(*a):
    print(*a); LOG.write(" ".join(str(x) for x in a) + "\n"); LOG.flush()

bpy.ops.wm.read_factory_settings(use_empty=True)
try:
    bpy.ops.import_scene.fbx(filepath=SRC, use_anim=False, ignore_leaf_bones=True, automatic_bone_orientation=False)
except Exception as e:
    log("import_scene.fbx failed:", e)
    bpy.ops.wm.fbx_import(filepath=SRC)

arm = next((o for o in bpy.data.objects if o.type == "ARMATURE"), None)
if arm is None:
    log("no armature"); sys.exit(1)
# 메시는 버린다(애니메이션 전용 파일).
for o in list(bpy.data.objects):
    if o.type != "ARMATURE":
        bpy.data.objects.remove(o, do_unlink=True)
arm.animation_data_clear()
log("armature:", arm.name, "bones:", len(arm.data.bones), "scale:", tuple(arm.scale))

def bone(suffix):
    for b in arm.pose.bones:
        if b.name.endswith(suffix):
            return b
    return None

names = ["Hips", "Spine", "Spine1", "Spine2", "Neck", "Head", "LeftShoulder", "RightShoulder",
         "LeftArm", "RightArm", "LeftForeArm", "RightForeArm", "LeftUpLeg", "RightUpLeg",
         "LeftLeg", "RightLeg", "LeftFoot", "RightFoot", "LeftToeBase", "RightToeBase"]
B = {n: bone(":" + n) or bone(n) for n in names}
missing = [n for n, b in B.items() if b is None]
log("missing:", missing)

# 월드(아마추어) 축 — 앞 방향은 발끝 방향, 위는 +Z(Blender).
def head_ws(pb): return arm.matrix_world @ pb.bone.head_local
UP = Vector((0, 0, 1))
fwd = (head_ws(B["LeftToeBase"]) - head_ws(B["LeftFoot"])) if B["LeftToeBase"] and B["LeftFoot"] else Vector((0, -1, 0))
fwd.z = 0
if fwd.length < 1e-6: fwd = Vector((0, -1, 0))
FWD = fwd.normalized()
SIDE = FWD.cross(UP).normalized()      # +θ about SIDE = 다리가 앞으로
log("forward:", tuple(round(v, 2) for v in FWD), "side:", tuple(round(v, 2) for v in SIDE))
hip_h = head_ws(B["Hips"]).z
log("hip height (world):", hip_h)

from mathutils import Matrix
A3 = arm.matrix_world.to_3x3()
A3i = A3.inverted()
def axis_a(world_axis): return (A3i @ world_axis).normalized()

def rot(pb, *pairs):
    """월드 축 회전을 본 머리 기준으로 포즈 행렬에 직접 적용 (부모 포즈 반영). pairs 순서대로 적용."""
    bpy.context.view_layer.update()
    base = pb.matrix.copy()
    head = base.translation.copy()
    R = Matrix.Identity(4)
    for axis, deg in pairs:
        R = Matrix.Rotation(math.radians(deg), 4, axis_a(axis)) @ R
    pb.matrix = Matrix.Translation(head) @ R @ Matrix.Translation(-head) @ base

def move(pb, world_offset_m):
    bpy.context.view_layer.update()
    off = A3i @ world_offset_m
    pb.matrix = Matrix.Translation(off) @ pb.matrix.copy()

for pb in arm.pose.bones:
    pb.rotation_mode = "QUATERNION"
    pb.rotation_quaternion = Quaternion((1, 0, 0, 0))
    pb.location = Vector((0, 0, 0))

action = bpy.data.actions.new("Run")
arm.animation_data_create()
arm.animation_data.action = action
try:
    # Blender 4.4+ slotted actions
    if hasattr(action, "slots") and len(action.slots) == 0:
        slot = action.slots.new(id_type='OBJECT', name=arm.name)
        arm.animation_data.action_slot = slot
except Exception as e:
    log("slot:", e)

scene = bpy.context.scene
scene.render.fps = FPS
scene.frame_start = 1
scene.frame_end = FRAMES + 1     # 마지막 프레임 = 첫 프레임(루프)

bob_m = hip_h * 0.035
sway_m = hip_h * 0.012

def key_all(frame):
    for pb in arm.pose.bones:
        pb.keyframe_insert("rotation_quaternion", frame=frame)
    B["Hips"].keyframe_insert("location", frame=frame)

for i in range(FRAMES + 1):
    f = i + 1
    for pb in arm.pose.bones:
        pb.rotation_quaternion = Quaternion((1, 0, 0, 0)); pb.location = Vector((0, 0, 0))
    ph = 2 * math.pi * (i % FRAMES) / FRAMES
    s = math.sin(ph); c = math.cos(ph)
    # 다리: 왼쪽 앞으로 = +s
    swingL, swingR = 42 * s, -42 * s
    kneeL = 22 + 62 * max(0.0, math.cos(ph - 0.35))         # 앞으로 스윙하는 중간에 최대 굽힘
    kneeR = 22 + 62 * max(0.0, math.cos(ph + math.pi - 0.35))
    # 몸통 먼저(부모), 그 다음 팔다리
    rot(B["Hips"], (SIDE, -6), (UP, 4 * s))
    rot(B["Spine"], (SIDE, -4), (UP, -3 * s))
    rot(B["Spine1"], (SIDE, -3), (UP, -3 * s))
    rot(B["Spine2"], (SIDE, -3), (UP, -3 * s))
    rot(B["Neck"], (SIDE, 5))
    rot(B["Head"], (SIDE, 5), (UP, 2 * s))
    rot(B["LeftUpLeg"], (SIDE, swingL - 6))
    rot(B["RightUpLeg"], (SIDE, swingR - 6))
    rot(B["LeftLeg"], (SIDE, -kneeL))
    rot(B["RightLeg"], (SIDE, -kneeR))
    # 발: 앞 스윙 끝(접지 직전)엔 발끝 올리고, 뒤로 찰 땐 발끝 내림
    rot(B["LeftFoot"], (SIDE, 10 * s + 8))
    rot(B["RightFoot"], (SIDE, -10 * s + 8))
    if B["LeftToeBase"]: rot(B["LeftToeBase"], (SIDE, -6 * max(0.0, -s)))
    if B["RightToeBase"]: rot(B["RightToeBase"], (SIDE, -6 * max(0.0, s)))
    # 팔: T포즈에서 몸 옆으로 68° 내리고(FWD 축), 다리와 반대로 스윙(SIDE 축), 팔꿈치 85° 굽힘(UP 축)
    armL, armR = -34 * s, 34 * s
    rot(B["LeftShoulder"], (FWD, -4))
    rot(B["RightShoulder"], (FWD, 4))
    rot(B["LeftArm"], (FWD, -66), (SIDE, armL - 12))     # 왼팔: FWD 축 -θ = 아래로
    rot(B["RightArm"], (FWD, 66), (SIDE, armR - 12))
    rot(B["LeftForeArm"], (SIDE, 88))                    # 팔꿈치: 앞으로 굽힘
    rot(B["RightForeArm"], (SIDE, 88))
    # 힙: 접지 때 낮게(두 번), 좌우 살짝
    move(B["Hips"], UP * (-bob_m * s * s) + SIDE * (sway_m * s))
    key_all(f)

# 팔 굽힘 방향 확인용 로그: 손 위치가 몸 앞에 있어야 한다.
scene.frame_set(1)
bpy.context.view_layer.update()
def pose_head(pb): return (arm.matrix_world @ pb.matrix).translation
hips = pose_head(B["Hips"])
for n in ["LeftArm","LeftForeArm","LeftHand","LeftFoot","RightFoot","Head"]:
    pb = B.get(n) or bone(":"+n)
    if pb:
        d = pose_head(pb) - hips
        log(f"check f1 {n}: fwd {d.dot(FWD):.3f} side {d.dot(SIDE):.3f} up {d.z:.3f}")
foot = pose_head(B["LeftFoot"]) - hips
log("check: LeftFoot at frame1 fwd comp:", round(foot.dot(FWD), 3), "(다리 앞뒤 스윙 기준점: 0 근처)")
scene.frame_set(6); bpy.context.view_layer.update()
foot = pose_head(B["LeftFoot"]) - hips
log("check: LeftFoot at frame6 fwd comp:", round(foot.dot(FWD), 3), "(양수면 왼발이 앞으로 나감 = 정상)")

bpy.ops.object.select_all(action="DESELECT")
arm.select_set(True)
bpy.context.view_layer.objects.active = arm
bpy.ops.export_scene.fbx(
    filepath=DST, use_selection=True, object_types={"ARMATURE"},
    add_leaf_bones=False, bake_anim=True, bake_anim_use_all_bones=True,
    bake_anim_use_nla_strips=False, bake_anim_use_all_actions=False,
    bake_anim_force_startend_keying=True, bake_anim_step=1.0, bake_anim_simplify_factor=0.0,
    armature_nodetype="NULL", use_armature_deform_only=False,
    apply_scale_options="FBX_SCALE_NONE", axis_forward="-Z", axis_up="Y",
)
log("exported:", DST, os.path.getsize(DST))
LOG.close()
