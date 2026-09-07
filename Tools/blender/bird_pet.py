# 8차 — 펫 새 2종(참새·기러기)을 절차 생성해 FBX 로. 날개는 'WingL'/'WingR' 별도 오브젝트(원점 = 어깨),
# Unity PetCompanion 이 이름으로 찾아 Z축 회전으로 날갯짓한다. 몸통은 +Y(=Unity +Z) 를 향한다.
#   blender -b --python bird_pet.py
import bpy, math, os

EXPORT_DIR = r"C:\dev\game\Assets\Resources\CoastRun"
os.makedirs(EXPORT_DIR, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

def mat(name, rgb):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (*rgb, 1.0)
        bsdf.inputs["Roughness"].default_value = 0.85
    m.diffuse_color = (*rgb, 1.0)
    return m

def sphere(name, loc, scale, m, seg=16, rings=10):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=seg, ring_count=rings, radius=0.5, location=loc)
    ob = bpy.context.active_object
    ob.name = name
    ob.scale = scale
    ob.data.materials.append(m)
    bpy.ops.object.shade_smooth()
    return ob

def cone(name, loc, rot, scale, m):
    bpy.ops.mesh.primitive_cone_add(vertices=8, radius1=0.5, radius2=0.0, depth=1.0, location=loc, rotation=rot)
    ob = bpy.context.active_object
    ob.name = name; ob.scale = scale; ob.data.materials.append(m)
    return ob

def wing(name, side, root, length, chord, m, sweep=0.0):
    """평평한 타원 날개. 원점 = 어깨(root). side=+1 오른쪽(+X), -1 왼쪽."""
    bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=6, radius=0.5, location=(0, 0, 0))
    ob = bpy.context.active_object
    ob.name = name
    ob.scale = (length, chord, 0.06)
    bpy.ops.object.transform_apply(scale=True)
    # 날개를 어깨에서 바깥으로 뻗게 이동(원점은 어깨)
    for v in ob.data.vertices:
        v.co.x = v.co.x * 1.0 + side * length * 0.5
        v.co.y += -abs(v.co.x) * sweep
    ob.location = root
    ob.data.materials.append(m)
    bpy.ops.object.shade_smooth()
    return ob

def build_bird(kind):
    if kind == "Sparrow":
        back = mat("Sparrow_Back", (0.55, 0.36, 0.22)); belly = mat("Sparrow_Belly", (0.93, 0.85, 0.70))
        dark = mat("Bird_Dark", (0.08, 0.06, 0.05)); beak = mat("Bird_Beak", (0.95, 0.72, 0.30))
        body = sphere("Body", (0, 0, 0), (0.42, 0.56, 0.38), back)
        bell = sphere("Belly", (0, -0.02, -0.08), (0.36, 0.44, 0.28), belly)
        head = sphere("Head", (0, 0.26, 0.16), (0.30, 0.30, 0.28), back)
        cheek = sphere("Cheek", (0, 0.30, 0.08), (0.24, 0.22, 0.18), belly)
        eyeL = sphere("EyeL", (-0.11, 0.38, 0.20), (0.06, 0.05, 0.06), dark, 8, 6)
        eyeR = sphere("EyeR", (0.11, 0.38, 0.20), (0.06, 0.05, 0.06), dark, 8, 6)
        bk = cone("Beak", (0, 0.44, 0.13), (math.radians(90), 0, 0), (0.09, 0.08, 0.12), beak)
        tail = cone("Tail", (0, -0.34, 0.02), (math.radians(-100), 0, 0), (0.16, 0.05, 0.26), back)
        wl = wing("WingL", -1, (-0.14, 0.02, 0.06), 0.34, 0.26, back)
        wr = wing("WingR", +1, (0.14, 0.02, 0.06), 0.34, 0.26, back)
        parts = [bell, head, cheek, eyeL, eyeR, bk, tail]
    else:
        back = mat("Goose_Back", (0.52, 0.50, 0.48)); belly = mat("Goose_Belly", (0.93, 0.93, 0.90))
        dark = mat("Bird_Dark", (0.08, 0.06, 0.05)); beak = mat("Goose_Beak", (0.90, 0.55, 0.25))
        body = sphere("Body", (0, 0, 0), (0.48, 0.80, 0.42), back)
        bell = sphere("Belly", (0, -0.04, -0.10), (0.42, 0.66, 0.30), belly)
        neck = sphere("Neck", (0, 0.40, 0.22), (0.16, 0.22, 0.40), back)
        head = sphere("Head", (0, 0.50, 0.46), (0.22, 0.30, 0.22), back)
        chin = sphere("Chin", (0, 0.50, 0.40), (0.18, 0.22, 0.14), belly)
        eyeL = sphere("EyeL", (-0.09, 0.60, 0.50), (0.05, 0.04, 0.05), dark, 8, 6)
        eyeR = sphere("EyeR", (0.09, 0.60, 0.50), (0.05, 0.04, 0.05), dark, 8, 6)
        bk = cone("Beak", (0, 0.70, 0.44), (math.radians(90), 0, 0), (0.09, 0.07, 0.18), beak)
        tail = cone("Tail", (0, -0.48, 0.04), (math.radians(-100), 0, 0), (0.20, 0.05, 0.30), back)
        wl = wing("WingL", -1, (-0.16, 0.04, 0.10), 0.60, 0.30, back, sweep=0.15)
        wr = wing("WingR", +1, (0.16, 0.04, 0.10), 0.60, 0.30, back, sweep=0.15)
        parts = [bell, neck, head, chin, eyeL, eyeR, bk, tail]
    for p in parts:
        p.parent = body
        p.matrix_parent_inverse = body.matrix_world.inverted()
    for w in (wl, wr):
        w.parent = body
        w.matrix_parent_inverse = body.matrix_world.inverted()
    body.name = "Pet_" + kind
    return body

def export(root, filename):
    bpy.ops.object.select_all(action='DESELECT')
    root.select_set(True)
    for c in root.children_recursive:
        c.select_set(True)
    bpy.context.view_layer.objects.active = root
    path = os.path.join(EXPORT_DIR, filename + ".fbx")
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_ALL', bake_space_transform=False,
        axis_forward='-Z', axis_up='Y', object_types={'MESH', 'EMPTY'},
        mesh_smooth_type='FACE', use_mesh_modifiers=True, add_leaf_bones=False,
        bake_anim=False, path_mode='STRIP', embed_textures=False)
    print("exported", path)

for kind in ("Sparrow", "WildGoose"):
    root = build_bird(kind)
    export(root, "Pet_" + kind)
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
print("bird pets done")
