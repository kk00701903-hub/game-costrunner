# 말랑이 하트 — 통통한 캔디 3D (러닝 픽업용).
#   "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" -b --python make_heart_chunky.py
import bpy, bmesh, math, os
from mathutils import Vector

EXPORT = r"C:\dev\game\Assets\Resources\CoastRun\Models"
os.makedirs(EXPORT, exist_ok=True)

bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
for b in list(bpy.data.meshes):
    bpy.data.meshes.remove(b)
for b in list(bpy.data.materials):
    bpy.data.materials.remove(b)


def mat(name, rgb, rough=0.42, spec=0.55, coat=0.35):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (*rgb, 1.0)
        if "Roughness" in bsdf.inputs:
            bsdf.inputs["Roughness"].default_value = rough
        for key, val in (
            ("Specular IOR Level", spec),
            ("Specular", spec),
            ("Coat Weight", coat),
            ("Coat Roughness", 0.12),
            ("Sheen Weight", 0.15),
        ):
            if key in bsdf.inputs:
                bsdf.inputs[key].default_value = val
    m.diffuse_color = (*rgb, 1.0)
    return m


def heart_point(t, s=0.042):
    # 고전 하트 곡선 — 위가 lobed, 아래가 tip
    x = 16.0 * math.sin(t) ** 3
    y = (13.0 * math.cos(t)
         - 5.0 * math.cos(2.0 * t)
         - 2.0 * math.cos(3.0 * t)
         - math.cos(4.0 * t))
    return x * s, y * s


def build_heart_body():
    n = 64
    half_t = 0.22  # 두께(반)
    verts = []
    # 앞·뒤 두 링 + 살짝 부풀어 입체감
    for side in (-1.0, 1.0):
        z = side * half_t
        bulge = 1.0 + 0.08 * (1.0 - abs(side) * 0.0)  # keep
        for i in range(n):
            t = (i / n) * math.pi * 2.0
            x, y = heart_point(t)
            # 옆면으로 갈수록 살짝 안쪽으로(둥근 캔디)
            r = 1.0 - 0.06 * (abs(side))
            verts.append((x * r, y * r + 0.02, z))

    faces = []
    # 앞면 / 뒷면 팬
    for base in (0, n):
        for i in range(1, n - 1):
            if base == 0:
                faces.append((base, base + i, base + i + 1))
            else:
                faces.append((base, base + i + 1, base + i))
    # 옆면 쿼드
    for i in range(n):
        a = i
        b = (i + 1) % n
        c = n + (i + 1) % n
        d = n + i
        faces.append((a, b, c, d))

    me = bpy.data.meshes.new("Heart")
    me.from_pydata(verts, [], faces)
    me.update()
    ob = bpy.data.objects.new("Heart", me)
    bpy.context.collection.objects.link(ob)
    bpy.context.view_layer.objects.active = ob
    ob.select_set(True)

    # 스무스 + 서브디브로 통통하게
    bpy.ops.object.shade_smooth()
    bm = bmesh.new()
    bm.from_mesh(me)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(me)
    bm.free()

    sub = ob.modifiers.new("Subsurf", "SUBSURF")
    sub.levels = 2
    sub.render_levels = 2
    bev = ob.modifiers.new("Bevel", "BEVEL")
    bev.width = 0.018
    bev.segments = 2
    bev.limit_method = "ANGLE"
    bpy.ops.object.modifier_apply(modifier="Subsurf")
    bpy.ops.object.modifier_apply(modifier="Bevel")

    # 원점 = 바운드 중심, 바닥이 대략 y=0 근처가 되게 살짝 올림은 Unity 쪽에서
    bpy.ops.object.origin_set(type="ORIGIN_GEOMETRY", center="BOUNDS")
    ob.location = (0.0, 0.0, 0.0)

    pink = mat("HeartPink", (1.0, 0.42, 0.58), rough=0.38, coat=0.4)
    deep = mat("HeartDeep", (0.92, 0.28, 0.48), rough=0.5, coat=0.2)
    ob.data.materials.clear()
    ob.data.materials.append(pink)

    # 아래 tip 쪽 살짝 더 진한 톤 — 버텍스 페인트 대신 작은 딥 셸
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=10, radius=0.12, location=(0.0, -0.28, 0.0))
    tip = bpy.context.active_object
    tip.name = "HeartTip"
    tip.scale = (0.85, 0.7, 0.55)
    bpy.ops.object.transform_apply(scale=True)
    tip.data.materials.append(deep)
    bpy.ops.object.shade_smooth()

    # 흰 하이라이트(캔디 광택)
    gloss_m = mat("HeartGloss", (1.0, 0.96, 0.98), rough=0.15, coat=0.6)
    bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=8, radius=0.07, location=(-0.16, 0.22, -0.16))
    gloss = bpy.context.active_object
    gloss.name = "HeartGloss"
    gloss.scale = (1.1, 0.75, 0.55)
    bpy.ops.object.transform_apply(scale=True)
    gloss.data.materials.append(gloss_m)
    bpy.ops.object.shade_smooth()

    # 조인 → 단일 메시(하트 본체+딥+광택은 재질 슬롯 유지하려면 조인 전 슬롯 맞춤)
    # 재질 슬롯을 본체에 모은 뒤 조인
    bpy.ops.object.select_all(action="DESELECT")
    ob.select_set(True)
    bpy.context.view_layer.objects.active = ob
    if "HeartDeep" not in [m.name for m in ob.data.materials]:
        ob.data.materials.append(deep)
    if "HeartGloss" not in [m.name for m in ob.data.materials]:
        ob.data.materials.append(gloss_m)
    tip.data.materials.clear()
    tip.data.materials.append(ob.data.materials[1])
    gloss.data.materials.clear()
    gloss.data.materials.append(ob.data.materials[2])

    tip.select_set(True)
    gloss.select_set(True)
    bpy.ops.object.join()
    heart = bpy.context.active_object
    heart.name = "Heart"
    # 곡선이 XY(바닥)에 있어 세움: lobes → +Z(Blender up) → FBX 후 Unity +Y
    heart.rotation_euler = (math.radians(90.0), 0.0, 0.0)
    bpy.ops.object.transform_apply(rotation=True)
    bpy.ops.object.origin_set(type="ORIGIN_GEOMETRY", center="BOUNDS")
    heart.location = (0.0, 0.0, 0.0)

    dims = [round(x, 3) for x in heart.dimensions]
    print("Heart dims", dims)
    return heart


heart = build_heart_body()
path = os.path.join(EXPORT, "Heart.fbx")
bpy.ops.object.select_all(action="DESELECT")
heart.select_set(True)
bpy.context.view_layer.objects.active = heart
bpy.ops.export_scene.fbx(
    filepath=path,
    use_selection=True,
    apply_scale_options="FBX_SCALE_ALL",
    axis_forward="-Z",
    axis_up="Y",
    bake_space_transform=True,
    object_types={"MESH"},
    use_mesh_modifiers=True,
    add_leaf_bones=False,
    bake_anim=False,
)
print("exported", path)
