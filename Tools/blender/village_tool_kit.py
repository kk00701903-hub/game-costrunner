# 168차(사용자: 「블렌더로 도구들 다시 그려줘」): 마을 손 도구 5종 — 잠자리채·방망이·도끼·곡괭이·낚싯대.
#   원점 = 손잡이 아래 끝, +Z(블렌더) = 위 → Unity +Y, 전체 높이 ≈ 0.9~1.1 m. VillageHub.ApplyTool 이 손 뼈에 붙이고 k 로 스케일.
#   재질 이름 → JejuKit.MaterialFor: Wood / WoodDark / Metal / Rope / ToolRed / ToolNet(반투명) / ToolCork / ToolLine
# 실행: blender -b --python village_tool_kit.py
import bpy, bmesh, math, os
from mathutils import Vector
OUT = r"C:\dev\game\Assets\Resources\CoastRun\Models"; os.makedirs(OUT, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
def mat(n, rgb):
    m = bpy.data.materials.get(n) or bpy.data.materials.new(n); m.use_nodes = True
    b = m.node_tree.nodes.get("Principled BSDF"); b and b.inputs["Base Color"].default_value.__setitem__(slice(0,3), rgb); return m
WOOD=mat("Wood",(0.62,0.44,0.26)); WOODD=mat("WoodDark",(0.42,0.28,0.16)); METAL=mat("Metal",(0.72,0.75,0.80)); ROPE=mat("Rope",(0.80,0.70,0.45))
RED=mat("ToolRed",(0.90,0.32,0.30)); NET=mat("ToolNet",(0.85,0.95,1.0)); CORK=mat("ToolCork",(0.86,0.72,0.50)); LINE=mat("ToolLine",(0.95,0.95,0.95))
parts=[]
def add(ob, m):
    ob.data.materials.append(m); parts.append(ob); return ob
def cyl(r, h, loc, m, rot=(0,0,0), r2=None, verts=14):
    if r2 is None: bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=r, depth=h, location=loc, rotation=rot)
    else: bpy.ops.mesh.primitive_cone_add(vertices=verts, radius1=r, radius2=r2, depth=h, location=loc, rotation=rot)
    ob=bpy.context.active_object; bpy.ops.object.shade_smooth(); return add(ob, m)
def box(sx, sy, sz, loc, m, rot=(0,0,0), bevel=0.01):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc, rotation=rot); ob=bpy.context.active_object; ob.scale=(sx,sy,sz)
    bpy.ops.object.transform_apply(scale=True)
    md=ob.modifiers.new("b",'BEVEL'); md.width=bevel; md.segments=2; return add(ob, m)
def sph(r, loc, m, scale=(1,1,1)):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=10, radius=r, location=loc); ob=bpy.context.active_object; ob.scale=scale
    bpy.ops.object.transform_apply(scale=True); bpy.ops.object.shade_smooth(); return add(ob, m)
def torus(R, r, loc, m, rot=(0,0,0)):
    bpy.ops.mesh.primitive_torus_add(major_radius=R, minor_radius=r, major_segments=28, minor_segments=8, location=loc, rotation=rot); ob=bpy.context.active_object; bpy.ops.object.shade_smooth(); return add(ob, m)
def export(name):
    global parts
    bpy.ops.object.select_all(action='DESELECT')
    for p in parts: p.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]; bpy.ops.object.join(); ob=bpy.context.active_object; ob.name=name
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, name+".fbx"), use_selection=True, apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL',
        bake_space_transform=True, axis_forward='-Z', axis_up='Y', object_types={'MESH'}, mesh_smooth_type='FACE', use_mesh_modifiers=True, add_leaf_bones=False, bake_anim=False, path_mode='STRIP')
    print("exported", name); bpy.data.objects.remove(ob, do_unlink=True); parts=[]

# 잠자리채: 긴 자루 + 고리 + 그물(반구)
cyl(0.017, 0.80, (0,0,0.40), WOOD); cyl(0.022, 0.16, (0,0,0.08), WOODD)
torus(0.16, 0.012, (0,0,0.98), METAL, rot=(math.radians(90),0,0))
sph(0.155, (0,0.10,0.98), NET, scale=(1,0.7,1)); export("VTool_Net")
# 방망이: 아래 가늘고 위 굵은 콘 + 빨간 테이프 손잡이 + 끝 캡
cyl(0.028, 0.55, (0,0,0.48), WOOD, r2=0.055); cyl(0.030, 0.22, (0,0,0.11), RED); sph(0.055, (0,0,0.755), WOOD); sph(0.034, (0,0,0.0), WOODD); export("VTool_Bat")
# 도끼: 자루 + 날(쐐기) + 쇠 고리
cyl(0.020, 0.70, (0,0,0.35), WOOD); cyl(0.026, 0.18, (0,0,0.09), WOODD)
box(0.20, 0.03, 0.16, (0.09,0,0.62), METAL, bevel=0.012); box(0.06, 0.05, 0.10, (0.0,0,0.62), WOODD, bevel=0.008); export("VTool_Axe")
# 곡괭이: 자루 + 양끝 뾰족 머리(두 콘)
cyl(0.020, 0.72, (0,0,0.36), WOOD); cyl(0.026, 0.18, (0,0,0.09), WOODD)
cyl(0.04, 0.24, (0.12,0,0.70), METAL, rot=(0,math.radians(90),0), r2=0.006); cyl(0.04, 0.24, (-0.12,0,0.70), METAL, rot=(0,math.radians(-90),0), r2=0.006)
box(0.07, 0.07, 0.07, (0,0,0.70), METAL, bevel=0.015); export("VTool_Pick")
# 낚싯대: 긴 가는 대(테이퍼) + 코르크 손잡이 + 릴 + 가이드 고리 + 줄·찌
cyl(0.014, 1.05, (0,0,0.525), WOODD, r2=0.005, verts=10); cyl(0.024, 0.22, (0,0,0.11), CORK)
torus(0.035, 0.010, (0.045,0,0.26), METAL, rot=(math.radians(90),0,0)); sph(0.018, (0.045,0,0.26), RED)
for z in (0.55, 0.80, 1.02): torus(0.014, 0.004, (0.02,0,z), METAL, rot=(math.radians(90),0,0))
cyl(0.003, 0.70, (0.06,0,0.72), LINE, rot=(0,math.radians(6),0)); sph(0.02, (0.10,0,0.37), RED, scale=(1,1,1.6)); export("VTool_Rod")
print("done")
