# 171차(사용자: 「농장 — 닭·토끼」): 가축 모델 2종. 원점 = 발밑, +Z 위 → Unity +Y. 닭 ≈0.45 m, 토끼 ≈0.35 m(다 크면 코드에서 1.5배).
#   재질 → JejuKit.MaterialFor: AnimWhite / AnimRed / AnimYellow / AnimGray / AnimPink / AnimDark
# 실행: blender -b --python village_animal_kit.py
import bpy, math, os
OUT = r"C:\dev\game\Assets\Resources\CoastRun\Models"; os.makedirs(OUT, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
R = math.radians
def mat(n, rgb):
    m = bpy.data.materials.get(n) or bpy.data.materials.new(n); m.use_nodes = True
    b = m.node_tree.nodes.get("Principled BSDF")
    if b: b.inputs["Base Color"].default_value = (rgb[0], rgb[1], rgb[2], 1.0)
    return m
WHITE = mat("AnimWhite", (0.97, 0.96, 0.92)); RED = mat("AnimRed", (0.92, 0.22, 0.22)); YEL = mat("AnimYellow", (0.98, 0.75, 0.25))
GRAY = mat("AnimGray", (0.72, 0.68, 0.66)); PINK = mat("AnimPink", (0.98, 0.78, 0.82)); DARK = mat("AnimDark", (0.12, 0.10, 0.12))
parts = []
def add(ob, m): ob.data.materials.append(m); parts.append(ob); return ob
def sph(r, loc, m, scale=(1,1,1), rot=(0,0,0)):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=14, ring_count=10, radius=r, location=loc, rotation=rot)
    ob = bpy.context.active_object; ob.scale = scale; bpy.ops.object.transform_apply(scale=True); bpy.ops.object.shade_smooth(); return add(ob, m)
def cyl(r, h, loc, m, rot=(0,0,0), r2=None):
    if r2 is None: bpy.ops.mesh.primitive_cylinder_add(vertices=8, radius=r, depth=h, location=loc, rotation=rot)
    else: bpy.ops.mesh.primitive_cone_add(vertices=8, radius1=r, radius2=r2, depth=h, location=loc, rotation=rot)
    ob = bpy.context.active_object; bpy.ops.object.shade_smooth(); return add(ob, m)
def export(name):
    global parts
    bpy.ops.object.select_all(action='DESELECT')
    for p in parts: p.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]; bpy.ops.object.join(); ob = bpy.context.active_object; ob.name = name
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, name + ".fbx"), use_selection=True, apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL',
        bake_space_transform=True, axis_forward='-Z', axis_up='Y', object_types={'MESH'}, mesh_smooth_type='FACE', use_mesh_modifiers=True, add_leaf_bones=False, bake_anim=False, path_mode='STRIP')
    print("exported", name); bpy.data.objects.remove(ob, do_unlink=True); parts = []

# 닭: 통통한 몸 + 머리 + 부리 + 볏 + 꼬리깃 + 다리 (앞 = +Y)
sph(0.17, (0, 0, 0.24), WHITE, scale=(1.0, 1.25, 0.95))
sph(0.10, (0, 0.20, 0.40), WHITE)
cyl(0.035, 0.09, (0, 0.31, 0.39), YEL, rot=(R(90), 0, 0), r2=0.004)          # 부리
sph(0.03, (0, 0.30, 0.34), RED, scale=(1, 1, 1.6))                            # 턱볏
for k, z in enumerate((0.47, 0.50, 0.47)): sph(0.028, (0, 0.24 - k * 0.035, z), RED, scale=(0.6, 1, 1.3))   # 볏
for k in (-1, 1): sph(0.018, (k * 0.07, 0.27, 0.43), DARK)                   # 눈
for k in range(3): sph(0.05, (0, -0.22, 0.34 + k * 0.03), WHITE, scale=(0.5, 1.4, 1.0), rot=(R(-35 - k * 12), 0, 0))   # 꼬리깃
for k in (-1, 1):
    cyl(0.018, 0.12, (k * 0.06, 0.02, 0.07), YEL)
    for t in (-1, 0, 1): cyl(0.012, 0.09, (k * 0.06 + t * 0.03, 0.05, 0.012), YEL, rot=(R(90), 0, R(t * 25)))   # 발가락
sph(0.08, (0, 0.04, 0.28), WHITE, scale=(1.6, 0.4, 0.8))                       # 날개 밑선
export("VAnimal_Chicken")

# 토끼: 몸 + 머리 + 긴 귀 2 + 코 + 꼬리 + 발 (앞 = +Y)
sph(0.15, (0, 0, 0.17), GRAY, scale=(1.0, 1.35, 0.9))
sph(0.11, (0, 0.20, 0.27), GRAY)
for k in (-1, 1):
    sph(0.035, (k * 0.05, 0.19, 0.44), GRAY, scale=(1, 0.5, 3.4), rot=(R(-12), R(k * 10), 0))   # 귀
    sph(0.02, (k * 0.05, 0.205, 0.44), PINK, scale=(0.9, 0.4, 3.0), rot=(R(-12), R(k * 10), 0))
    sph(0.016, (k * 0.06, 0.29, 0.30), DARK)                                                       # 눈
    sph(0.05, (k * 0.09, -0.08, 0.06), GRAY, scale=(1, 1.6, 0.9))                                  # 뒷발
    sph(0.035, (k * 0.07, 0.20, 0.05), GRAY, scale=(1, 1.4, 0.8))                                  # 앞발
sph(0.02, (0, 0.31, 0.26), PINK, scale=(1.2, 0.7, 0.8))                                            # 코
sph(0.05, (0, -0.19, 0.20), WHITE)                                                                 # 꼬리
export("VAnimal_Rabbit")
print("done")
