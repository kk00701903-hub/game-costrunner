# 47차 — 펫 4종 입체(Blender 절차 조형, Kling 정면 그림 색·비율 기준): 참새·기러기·흑돼지·오토바이 깡패(곰돌이 라이더).
#   통통한 장난감(치비) 조형: 큰 머리·둥근 몸·큰 눈(흰자+눈동자+하이라이트). 몸통 전방 = +Y(Unity +Z), 위 = +Z.
#   날개 WingL/WingR(원점=어깨, Z축 펄럭), 바퀴 WheelF/WheelB(로컬 Z = 차축) — Unity PetCompanion 이 이름으로 찾는다.
#   실행: Tools\blender\build_pets.bat  또는 Unity 메뉴 Coast Run/Build Pets 3D (Blender)
#   "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" -b --python pet_kit.py
import bpy, math, os

EXPORT_DIR = r"C:\dev\game\Assets\Resources\CoastRun"
os.makedirs(EXPORT_DIR, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)


def mat(name, rgb, rough=0.6, spec=0.3):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    try:
        m.use_nodes = True
        bsdf = m.node_tree.nodes.get("Principled BSDF")
        if bsdf:
            bsdf.inputs["Base Color"].default_value = (*rgb, 1.0)
            if "Roughness" in bsdf.inputs: bsdf.inputs["Roughness"].default_value = rough
            for key, val in (("Specular IOR Level", spec), ("Specular", spec)):
                if key in bsdf.inputs: bsdf.inputs[key].default_value = val
    except Exception:
        pass
    m.diffuse_color = (*rgb, 1.0)
    return m


def _finish(ob, name, scale, m, smooth=True):
    ob.name = name
    ob.scale = scale
    bpy.ops.object.transform_apply(scale=True)
    if m: ob.data.materials.append(m)
    if smooth: bpy.ops.object.shade_smooth()
    return ob


def sphere(name, loc, scale, m, seg=24, rings=16):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=seg, ring_count=rings, radius=0.5, location=loc)
    return _finish(bpy.context.active_object, name, scale, m)


def cyl(name, loc, rot, scale, m, verts=20):
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=0.5, depth=1.0, location=loc, rotation=rot)
    return _finish(bpy.context.active_object, name, scale, m)


def cone(name, loc, rot, scale, m, verts=12):
    bpy.ops.mesh.primitive_cone_add(vertices=verts, radius1=0.5, radius2=0.0, depth=1.0, location=loc, rotation=rot)
    return _finish(bpy.context.active_object, name, scale, m)


def box(name, loc, rot, scale, m, bevel=0.06):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=loc, rotation=rot)
    ob = _finish(bpy.context.active_object, name, scale, m, smooth=False)
    if bevel > 0:
        b = ob.modifiers.new("Bevel", 'BEVEL'); b.width = bevel; b.segments = 3
        bpy.ops.object.shade_smooth()
    return ob


def torus(name, loc, rot, major, minor, m):
    bpy.ops.mesh.primitive_torus_add(major_radius=major, minor_radius=minor, location=loc, rotation=rot,
                                     major_segments=24, minor_segments=10)
    ob = bpy.context.active_object
    ob.name = name
    ob.data.materials.append(m)
    bpy.ops.object.shade_smooth()
    return ob


def parent_all(root, parts):
    for p in parts:
        if p is None: continue
        p.parent = root
        p.matrix_parent_inverse = root.matrix_world.inverted()


def eye_pair(parent, y, z, spread, size, m_white, m_dark, m_shine, tilt=0.0):
    out = []
    for side, sx in (("L", -1), ("R", +1)):
        wh = sphere(f"Eye{side}", (sx * spread, y, z), (size, size * 0.9, size * 1.05), m_white, 14, 10)
        pu = sphere(f"Pupil{side}", (sx * spread * 0.98, y + size * 0.36, z + size * 0.02),
                    (size * 0.66, size * 0.5, size * 0.72), m_dark, 12, 8)
        hi = sphere(f"Shine{side}", (sx * spread - sx * size * 0.14, y + size * 0.58, z + size * 0.3),
                    (size * 0.22, size * 0.16, size * 0.22), m_shine, 8, 6)
        out += [wh, pu, hi]
    parent_all(parent, out)
    return out


def thick_wing(name, side, shoulder, length, chord, thick, m, sweep=0.12):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=10, radius=0.5, location=(0, 0, 0))
    ob = bpy.context.active_object
    ob.name = name
    ob.scale = (length, chord, thick)
    bpy.ops.object.transform_apply(scale=True)
    for v in ob.data.vertices:
        x = v.co.x
        v.co.x = x + side * length * 0.48
        v.co.y -= abs(x + side * length * 0.2) * sweep
        t = abs(v.co.x) / max(1e-3, length)
        v.co.z *= 1.0 - 0.25 * t
    ob.data.materials.append(m)
    bpy.ops.object.shade_smooth()
    tip = sphere(name + "_Tip", (side * length * 0.92, -length * sweep * 0.6, 0.0),
                 (chord * 0.55, chord * 0.7, thick * 0.7), m, 12, 8)
    parent_all(ob, [tip])
    ob.location = shoulder
    return ob


# ── 공용 재질 ────────────────────────────────────────────────────────────
DARK = mat("Pet_Dark", (0.06, 0.05, 0.05), 0.35, 0.5)
WHITE = mat("Pet_White", (0.98, 0.98, 0.97), 0.3, 0.6)
SHINE = mat("Pet_Shine", (1.0, 1.0, 1.0), 0.1, 0.9)
ORANGE = mat("Pet_Orange", (0.98, 0.62, 0.20), 0.45, 0.5)
PINK = mat("Pet_Pink", (0.98, 0.55, 0.72), 0.5, 0.4)


def build_sparrow():
    """Kling 참새: 갈색 등 + 크림 배·얼굴, 한 덩어리 둥근 몸, 주황 부리·발."""
    back = mat("Sparrow_Back", (0.55, 0.36, 0.22), 0.62, 0.28)
    belly = mat("Sparrow_Belly", (0.96, 0.88, 0.74), 0.5, 0.4)
    cheek = mat("Sparrow_Cheek", (0.80, 0.52, 0.34), 0.55, 0.3)
    body = sphere("Body", (0, 0, 0), (0.70, 0.72, 0.68), back, 32, 20)          # 한 덩어리
    bell = sphere("Belly", (0, 0.10, -0.06), (0.60, 0.58, 0.52), belly, 28, 16)  # 배·얼굴 크림
    face = sphere("Face", (0, 0.24, -0.02), (0.44, 0.34, 0.34), belly, 22, 14)   # 낮게 — 위·뒤에서 보면 안 튀어나오게
    cheekL = sphere("CheekL", (-0.22, 0.28, -0.06), (0.16, 0.12, 0.13), cheek, 14, 10)
    cheekR = sphere("CheekR", (0.22, 0.28, -0.06), (0.16, 0.12, 0.13), cheek, 14, 10)
    crest = sphere("Crest", (0, 0.06, 0.36), (0.30, 0.34, 0.14), back, 14, 10)
    beak = cone("Beak", (0, 0.42, -0.02), (math.radians(90), 0, 0), (0.12, 0.10, 0.16), ORANGE)
    eyes = eye_pair(body, 0.31, 0.06, 0.15, 0.11, WHITE, DARK, SHINE)
    tails = [cone(f"Tail{i}", (math.sin(math.radians(a)) * 0.05, -0.36, 0.02),
                  (math.radians(-100), 0, math.radians(a)), (0.12, 0.05, 0.26), back) for i, a in enumerate((-16, 0, 16))]
    feet = [sphere("FootL", (-0.12, 0.06, -0.34), (0.11, 0.16, 0.05), ORANGE, 10, 6),
            sphere("FootR", (0.12, 0.06, -0.34), (0.11, 0.16, 0.05), ORANGE, 10, 6)]
    wl = thick_wing("WingL", -1, (-0.26, -0.02, 0.06), 0.36, 0.28, 0.12, back, 0.10)
    wr = thick_wing("WingR", +1, (0.26, -0.02, 0.06), 0.36, 0.28, 0.12, back, 0.10)
    parent_all(body, [bell, face, cheekL, cheekR, crest, beak] + tails + feet + [wl, wr])
    body.name = "Pet_Sparrow"
    return body


def build_goose():
    """Kling 기러기: 흰 몸·긴 목, 주황 부리·물갈퀴, 작은 검은 눈."""
    white = mat("Goose_White", (0.96, 0.97, 0.95), 0.5, 0.4)
    wing = mat("Goose_Wing", (0.86, 0.90, 0.88), 0.5, 0.35)
    body = sphere("Body", (0, 0, 0), (0.62, 0.92, 0.58), white, 30, 18)
    chest = sphere("Chest", (0, 0.22, -0.02), (0.52, 0.52, 0.50), white, 22, 14)
    neck = cyl("Neck", (0, 0.36, 0.34), (math.radians(-12), 0, 0), (0.22, 0.22, 0.62), white, 18)
    head = sphere("Head", (0, 0.44, 0.70), (0.34, 0.40, 0.32), white, 24, 14)
    beak = cone("Beak", (0, 0.72, 0.66), (math.radians(90), 0, 0), (0.15, 0.10, 0.30), ORANGE)
    beak2 = sphere("BeakBase", (0, 0.60, 0.66), (0.16, 0.12, 0.10), ORANGE, 12, 8)
    eyes = eye_pair(body, 0.58, 0.75, 0.12, 0.07, WHITE, DARK, SHINE)
    tails = [cone(f"Tail{i}", (math.sin(math.radians(a)) * 0.06, -0.52, 0.10),
                  (math.radians(-110), 0, math.radians(a)), (0.16, 0.05, 0.30), wing) for i, a in enumerate((-12, 0, 12))]
    legs = [cyl("LegL", (-0.13, 0.04, -0.34), (0, 0, 0), (0.07, 0.07, 0.20), ORANGE, 10),
            cyl("LegR", (0.13, 0.04, -0.34), (0, 0, 0), (0.07, 0.07, 0.20), ORANGE, 10),
            sphere("FootL", (-0.13, 0.10, -0.44), (0.16, 0.22, 0.05), ORANGE, 10, 6),
            sphere("FootR", (0.13, 0.10, -0.44), (0.16, 0.22, 0.05), ORANGE, 10, 6)]
    wl = thick_wing("WingL", -1, (-0.22, 0.02, 0.14), 0.62, 0.34, 0.16, wing, 0.16)
    wr = thick_wing("WingR", +1, (0.22, 0.02, 0.14), 0.62, 0.34, 0.16, wing, 0.16)
    parent_all(body, [chest, neck, head, beak, beak2] + tails + legs + [wl, wr])
    body.name = "Pet_WildGoose"
    return body


def build_pig():
    """Kling 흑돼지: 검은 둥근 몸, 분홍 코, 세모 귀, 굵은 다리, 꼬인 꼬리."""
    black = mat("Pig_Black", (0.13, 0.13, 0.15), 0.55, 0.35)
    black2 = mat("Pig_Black2", (0.10, 0.10, 0.12), 0.55, 0.35)
    body = sphere("Body", (0, 0, 0.05), (0.96, 0.98, 0.86), black, 32, 20)
    head = sphere("Head", (0, 0.30, 0.16), (0.72, 0.62, 0.66), black, 28, 18)
    snout = sphere("Snout", (0, 0.60, 0.04), (0.34, 0.16, 0.26), PINK, 20, 12)
    nostrils = [sphere("NosL", (-0.09, 0.68, 0.05), (0.06, 0.03, 0.07), DARK, 8, 6),
                sphere("NosR", (0.09, 0.68, 0.05), (0.06, 0.03, 0.07), DARK, 8, 6)]
    ears = [cone("EarL", (-0.30, 0.22, 0.46), (math.radians(-25), math.radians(-20), 0), (0.22, 0.10, 0.30), black2),
            cone("EarR", (0.30, 0.22, 0.46), (math.radians(-25), math.radians(20), 0), (0.22, 0.10, 0.30), black2)]
    eyes = eye_pair(body, 0.56, 0.24, 0.20, 0.08, WHITE, DARK, SHINE)
    legs = [cyl(f"Leg{i}", (sx * 0.30, sy, -0.40), (0, 0, 0), (0.22, 0.22, 0.26), black2, 12)
            for i, (sx, sy) in enumerate(((-1, 0.26), (1, 0.26), (-1, -0.26), (1, -0.26)))]
    hooves = [sphere(f"Hoof{i}", (sx * 0.30, sy, -0.52), (0.24, 0.24, 0.08), DARK, 12, 6)
              for i, (sx, sy) in enumerate(((-1, 0.26), (1, 0.26), (-1, -0.26), (1, -0.26)))]
    tail = torus("Tail", (0, -0.50, 0.18), (math.radians(90), 0, 0), 0.08, 0.025, PINK)
    parent_all(body, [head, snout] + nostrils + ears + legs + hooves + [tail])
    body.name = "Pet_BlackPig"
    return body


def build_thug():
    """Kling 오토바이 깡패: 빨간 스쿠터 + 헬멧·선글라스·검은 재킷 곰돌이 라이더. 전방 +Y."""
    red = mat("Thug_Red", (0.82, 0.12, 0.14), 0.35, 0.6)
    cream = mat("Thug_Cream", (0.93, 0.90, 0.82), 0.5, 0.4)
    fur = mat("Thug_Fur", (0.72, 0.52, 0.32), 0.6, 0.3)
    jacket = mat("Thug_Jacket", (0.12, 0.11, 0.12), 0.45, 0.4)
    helmet = mat("Thug_Helmet", (0.16, 0.17, 0.19), 0.3, 0.6)
    tire = mat("Thug_Tire", (0.08, 0.08, 0.09), 0.7, 0.2)
    chrome = mat("Thug_Chrome", (0.80, 0.82, 0.85), 0.2, 0.8)
    glass = mat("Thug_Glass", (0.05, 0.05, 0.06), 0.15, 0.9)
    lamp = mat("Thug_Lamp", (1.0, 0.95, 0.75), 0.2, 0.8)

    root = sphere("Body", (0, 0, 0.30), (0.62, 1.10, 0.40), red, 24, 14)        # 스쿠터 몸통(뒤 좌석 덩어리)
    floor = box("Floor", (0, 0.10, 0.18), (0, 0, 0), (0.46, 0.70, 0.08), red, 0.03)
    seat = sphere("Seat", (0, -0.30, 0.50), (0.44, 0.60, 0.20), cream, 18, 10)
    front = sphere("Front", (0, 0.62, 0.42), (0.46, 0.44, 0.50), red, 20, 12)
    column = cyl("Column", (0, 0.66, 0.78), (math.radians(-15), 0, 0), (0.10, 0.10, 0.60), chrome, 12)
    bar = cyl("Handlebar", (0, 0.60, 1.02), (0, math.radians(90), 0), (0.08, 0.08, 0.86), chrome, 12)
    grips = [cyl("GripL", (-0.40, 0.60, 1.02), (0, math.radians(90), 0), (0.10, 0.10, 0.16), jacket, 12),
             cyl("GripR", (0.40, 0.60, 1.02), (0, math.radians(90), 0), (0.10, 0.10, 0.16), jacket, 12)]
    light = sphere("Headlight", (0, 0.88, 0.62), (0.22, 0.10, 0.22), lamp, 14, 10)
    ring = torus("LampRing", (0, 0.86, 0.62), (math.radians(90), 0, 0), 0.12, 0.025, chrome)
    fenderF = sphere("FenderF", (0, 0.86, 0.26), (0.30, 0.50, 0.24), red, 16, 10)
    # 바퀴: 로컬 Z = 차축(X 방향으로 눕히되 회전은 적용하지 않는다 → Unity 로컬 Y 스핀)
    wheels = []
    for name, y in (("WheelF", 0.86), ("WheelB", -0.62)):
        bpy.ops.mesh.primitive_cylinder_add(vertices=24, radius=0.5, depth=1.0, location=(0, y, 0.0))
        w = bpy.context.active_object; w.name = name
        w.scale = (0.44, 0.44, 0.20)
        bpy.ops.object.transform_apply(scale=True)
        w.rotation_euler = (0, math.radians(90), 0)
        w.data.materials.append(tire); bpy.ops.object.shade_smooth()
        hub = sphere(name + "_Hub", (0, 0, 0), (0.24, 0.24, 0.24), chrome, 12, 8)
        hub.parent = w; hub.matrix_parent_inverse = w.matrix_world.inverted()
        wheels.append(w)
    # 라이더(곰돌이): 검은 재킷 몸, 갈색 얼굴, 헬멧, 선글라스, 귀, 팔·다리
    torso = sphere("Torso", (0, -0.12, 0.86), (0.52, 0.46, 0.56), jacket, 22, 14)
    belly = sphere("Belly", (0, 0.06, 0.78), (0.34, 0.24, 0.34), cream, 16, 10)
    head = sphere("Head", (0, 0.00, 1.34), (0.58, 0.54, 0.54), fur, 26, 16)
    muzzle = sphere("Muzzle", (0, 0.24, 1.24), (0.30, 0.20, 0.22), cream, 16, 10)
    nose = sphere("Nose", (0, 0.36, 1.28), (0.10, 0.07, 0.08), DARK, 10, 6)
    ears = [sphere("EarL", (-0.26, -0.04, 1.58), (0.18, 0.12, 0.18), fur, 12, 8),
            sphere("EarR", (0.26, -0.04, 1.58), (0.18, 0.12, 0.18), fur, 12, 8)]
    hel = sphere("Helmet", (0, -0.04, 1.44), (0.66, 0.62, 0.56), helmet, 26, 16)
    hel.location.z += 0.02
    # 헬멧 아랫면 잘라내기(얼굴 보이게): 아래 절반 정점 위로 접기
    for v in hel.data.vertices:
        if v.co.z < -0.04: v.co.z = -0.04
    visor = sphere("Visor", (0, 0.22, 1.42), (0.56, 0.16, 0.16), helmet, 16, 8)
    glasses = [sphere("GlassL", (-0.15, 0.27, 1.38), (0.18, 0.06, 0.13), glass, 12, 8),
               sphere("GlassR", (0.15, 0.27, 1.38), (0.18, 0.06, 0.13), glass, 12, 8),
               cyl("Bridge", (0, 0.29, 1.38), (0, math.radians(90), 0), (0.03, 0.03, 0.12), DARK, 8)]
    arms = [cyl("ArmL", (-0.34, 0.22, 0.98), (math.radians(-55), math.radians(-18), 0), (0.16, 0.16, 0.52), jacket, 12),
            cyl("ArmR", (0.34, 0.22, 0.98), (math.radians(-55), math.radians(18), 0), (0.16, 0.16, 0.52), jacket, 12),
            sphere("HandL", (-0.42, 0.52, 1.04), (0.16, 0.16, 0.16), fur, 10, 8),
            sphere("HandR", (0.42, 0.52, 1.04), (0.16, 0.16, 0.16), fur, 10, 8)]
    legs = [cyl("LegL", (-0.24, 0.12, 0.50), (math.radians(-30), 0, 0), (0.18, 0.18, 0.44), jacket, 12),
            cyl("LegR", (0.24, 0.12, 0.50), (math.radians(-30), 0, 0), (0.18, 0.18, 0.44), jacket, 12),
            sphere("BootL", (-0.24, 0.24, 0.26), (0.18, 0.26, 0.12), DARK, 10, 6),
            sphere("BootR", (0.24, 0.24, 0.26), (0.18, 0.26, 0.12), DARK, 10, 6)]
    parent_all(root, [floor, seat, front, column, bar, light, ring, fenderF, torso, belly, head, muzzle, nose, hel, visor]
               + grips + wheels + ears + glasses + arms + legs)
    root.name = "Pet_BikerThug"
    return root


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


for kind, builder in (("Sparrow", build_sparrow), ("WildGoose", build_goose),
                      ("BlackPig", build_pig), ("BikerThug", build_thug)):
    root = builder()
    export(root, "Pet_" + kind)
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)

print("pet kit done (47차)")
