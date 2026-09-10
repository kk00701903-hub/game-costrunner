# 40차 — 펫 새 2종(참새·기러기). 평면 타원 날개 → 통통한 입체 치비 조형.
# 날개 오브젝트 이름 WingL / WingR (원점=어깨) — Unity PetCompanion 이 Z축으로 펄럭.
# 몸통은 +Y(=Unity +Z) 전방.
#   "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" -b --python bird_pet.py
import bpy, math, os, bmesh
from mathutils import Vector, Matrix

EXPORT_DIR = r"C:\dev\game\Assets\Resources\CoastRun"
os.makedirs(EXPORT_DIR, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)


def mat(name, rgb, rough=0.55, spec=0.35):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (*rgb, 1.0)
        if "Roughness" in bsdf.inputs:
            bsdf.inputs["Roughness"].default_value = rough
        # Blender 4+/5 Principled socket names vary — set if present
        for key, val in (("Specular IOR Level", spec), ("Specular", spec),
                         ("Coat Weight", 0.08), ("Sheen Weight", 0.25),
                         ("Sheen Roughness", 0.4)):
            if key in bsdf.inputs:
                bsdf.inputs[key].default_value = val
    m.diffuse_color = (*rgb, 1.0)
    return m


def mesh_ob(name, verts, faces, m):
    me = bpy.data.meshes.new(name)
    me.from_pydata(verts, [], faces)
    me.update()
    ob = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(ob)
    bpy.context.view_layer.objects.active = ob
    ob.select_set(True)
    bpy.ops.object.shade_smooth()
    if m:
        ob.data.materials.append(m)
    ob.select_set(False)
    return ob


def sphere(name, loc, scale, m, seg=24, rings=16):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=seg, ring_count=rings, radius=0.5, location=loc)
    ob = bpy.context.active_object
    ob.name = name
    ob.scale = scale
    bpy.ops.object.transform_apply(scale=True)
    ob.data.materials.append(m)
    bpy.ops.object.shade_smooth()
    return ob


def cone(name, loc, rot, scale, m, verts=10):
    bpy.ops.mesh.primitive_cone_add(vertices=verts, radius1=0.5, radius2=0.0, depth=1.0,
                                    location=loc, rotation=rot)
    ob = bpy.context.active_object
    ob.name = name
    ob.scale = scale
    bpy.ops.object.transform_apply(scale=True)
    ob.data.materials.append(m)
    bpy.ops.object.shade_smooth()
    return ob


def thick_wing(name, side, shoulder, length, chord, thick, m, sweep=0.12):
    """통통한 날개: 타원 몸통 + 끝 깃털 볼륨. 원점=어깨."""
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=10, radius=0.5, location=(0, 0, 0))
    ob = bpy.context.active_object
    ob.name = name
    ob.scale = (length, chord, thick)
    bpy.ops.object.transform_apply(scale=True)
    # 어깨에서 바깥으로, 살짝 뒤로 쓸림
    for v in ob.data.vertices:
        x = v.co.x
        v.co.x = x + side * length * 0.48
        v.co.y -= abs(x + side * length * 0.2) * sweep
        # 끝으로 갈수록 조금 납작
        t = abs(v.co.x) / max(1e-3, length)
        v.co.z *= 1.0 - 0.25 * t
    # 깃털 tip
    tip = sphere(name + "_Tip", (side * length * 0.92, -length * sweep * 0.6, 0.0),
                 (chord * 0.55, chord * 0.7, thick * 0.7), m, 12, 8)
    tip.parent = ob
    tip.matrix_parent_inverse = ob.matrix_world.inverted()
    # 앞깃 한 겹
    cover = sphere(name + "_Cover", (side * length * 0.35, chord * 0.15, thick * 0.35),
                   (length * 0.45, chord * 0.55, thick * 0.55), m, 12, 8)
    cover.parent = ob
    cover.matrix_parent_inverse = ob.matrix_world.inverted()

    ob.location = shoulder
    ob.data.materials.append(m)
    bpy.ops.object.shade_smooth()
    return ob


def eye_pair(parent, y, z, spread, size, m_white, m_dark, m_shine):
    eyes = []
    for side, sx in (("L", -1), ("R", +1)):
        wh = sphere(f"Eye{side}", (sx * spread, y, z), (size, size * 0.92, size * 0.85), m_white, 12, 8)
        pu = sphere(f"Pupil{side}", (sx * spread, y + size * 0.35, z + size * 0.15),
                    (size * 0.55, size * 0.55, size * 0.5), m_dark, 10, 6)
        hi = sphere(f"Shine{side}", (sx * spread - size * 0.12, y + size * 0.55, z + size * 0.35),
                    (size * 0.18, size * 0.18, size * 0.15), m_shine, 8, 6)
        for ob in (wh, pu, hi):
            ob.parent = parent
            ob.matrix_parent_inverse = parent.matrix_world.inverted()
            eyes.append(ob)
    return eyes


def parent_all(body, parts):
    for p in parts:
        if p is None:
            continue
        p.parent = body
        p.matrix_parent_inverse = body.matrix_world.inverted()


def build_sparrow():
    back = mat("Sparrow_Back", (0.58, 0.38, 0.24), 0.62, 0.28)
    belly = mat("Sparrow_Belly", (0.96, 0.90, 0.78), 0.5, 0.4)
    cheek = mat("Sparrow_Cheek", (0.72, 0.48, 0.32), 0.55, 0.3)
    dark = mat("Bird_Dark", (0.07, 0.05, 0.04), 0.35, 0.5)
    beak = mat("Bird_Beak", (0.98, 0.72, 0.28), 0.4, 0.55)
    white = mat("Bird_White", (0.98, 0.98, 0.96), 0.3, 0.6)
    shine = mat("Bird_Shine", (1.0, 1.0, 1.0), 0.1, 0.9)

    body = sphere("Body", (0, 0, 0), (0.52, 0.62, 0.50), back, 28, 18)
    # 볼륨감: 옆·등 보조 매스
    fluff = sphere("Fluff", (0, -0.02, 0.06), (0.48, 0.50, 0.42), back, 20, 12)
    bell = sphere("Belly", (0, 0.02, -0.10), (0.42, 0.50, 0.36), belly, 24, 14)
    head = sphere("Head", (0, 0.28, 0.22), (0.38, 0.38, 0.36), back, 24, 14)
    face = sphere("Face", (0, 0.34, 0.12), (0.30, 0.28, 0.24), belly, 18, 12)
    cheekL = sphere("CheekL", (-0.16, 0.32, 0.14), (0.14, 0.12, 0.12), cheek, 14, 10)
    cheekR = sphere("CheekR", (0.16, 0.32, 0.14), (0.14, 0.12, 0.12), cheek, 14, 10)
    crest = sphere("Crest", (0, 0.22, 0.40), (0.16, 0.20, 0.10), cheek, 12, 8)

    eyes = eye_pair(body, 0.42, 0.28, 0.13, 0.09, white, dark, shine)
    bk = cone("Beak", (0, 0.50, 0.18), (math.radians(90), 0, 0), (0.10, 0.09, 0.16), beak)
    # 꼬리: 부채 3장
    tails = []
    for i, ang in enumerate((-18, 0, 18)):
        t = cone(f"Tail{i}", (math.radians(ang) * 0.15, -0.40, 0.04),
                 (math.radians(-105), 0, math.radians(ang)), (0.12, 0.05, 0.28), back)
        tails.append(t)

    wl = thick_wing("WingL", -1, (-0.18, 0.02, 0.08), 0.42, 0.30, 0.14, back, 0.10)
    wr = thick_wing("WingR", +1, (0.18, 0.02, 0.08), 0.42, 0.30, 0.14, back, 0.10)

    parent_all(body, [fluff, bell, head, face, cheekL, cheekR, crest, bk] + tails + eyes)
    parent_all(body, [wl, wr])
    body.name = "Pet_Sparrow"
    return body


def build_goose():
    back = mat("Goose_Back", (0.48, 0.50, 0.54), 0.58, 0.3)
    belly = mat("Goose_Belly", (0.95, 0.95, 0.93), 0.48, 0.4)
    neck_c = mat("Goose_Neck", (0.42, 0.44, 0.48), 0.55, 0.28)
    dark = mat("Bird_Dark", (0.07, 0.05, 0.04), 0.35, 0.5)
    beak = mat("Goose_Beak", (0.95, 0.55, 0.22), 0.4, 0.55)
    white = mat("Bird_White", (0.98, 0.98, 0.96), 0.3, 0.6)
    shine = mat("Bird_Shine", (1.0, 1.0, 1.0), 0.1, 0.9)
    orange = mat("Goose_Feet", (0.95, 0.55, 0.22), 0.5, 0.4)

    body = sphere("Body", (0, 0, 0), (0.58, 0.88, 0.55), back, 28, 18)
    fluff = sphere("Fluff", (0, -0.04, 0.08), (0.52, 0.70, 0.46), back, 20, 12)
    bell = sphere("Belly", (0, 0.0, -0.12), (0.48, 0.72, 0.38), belly, 24, 14)
    neck = sphere("Neck", (0, 0.42, 0.28), (0.22, 0.28, 0.48), neck_c, 18, 12)
    head = sphere("Head", (0, 0.52, 0.55), (0.28, 0.36, 0.28), back, 22, 14)
    chin = sphere("Chin", (0, 0.55, 0.48), (0.22, 0.26, 0.18), belly, 16, 10)

    eyes = eye_pair(body, 0.64, 0.58, 0.11, 0.08, white, dark, shine)
    bk = cone("Beak", (0, 0.74, 0.52), (math.radians(90), 0, 0), (0.11, 0.08, 0.22), beak)
    tails = []
    for i, ang in enumerate((-14, 0, 14)):
        t = cone(f"Tail{i}", (math.sin(math.radians(ang)) * 0.08, -0.52, 0.06),
                 (math.radians(-105), 0, math.radians(ang)), (0.16, 0.05, 0.34), back)
        tails.append(t)

    wl = thick_wing("WingL", -1, (-0.20, 0.04, 0.12), 0.70, 0.34, 0.16, back, 0.16)
    wr = thick_wing("WingR", +1, (0.20, 0.04, 0.12), 0.70, 0.34, 0.16, back, 0.16)

    # 짧은 물갈퀴 발 — 지면 앵커 느낌
    footL = sphere("FootL", (-0.12, 0.10, -0.28), (0.10, 0.16, 0.06), orange, 10, 6)
    footR = sphere("FootR", (0.12, 0.10, -0.28), (0.10, 0.16, 0.06), orange, 10, 6)

    parent_all(body, [fluff, bell, neck, head, chin, bk, footL, footR] + tails + eyes)
    parent_all(body, [wl, wr])
    body.name = "Pet_WildGoose"
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


for kind, builder in (("Sparrow", build_sparrow), ("WildGoose", build_goose)):
    root = builder()
    export(root, "Pet_" + kind)
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)

print("bird pets done (chunky v40)")
