# -*- coding: utf-8 -*-
"""Coast Run 143차 — 에셋 통일화: 선택한(또는 전체) 메시에 Bevel 로 모서리를 둥글리고,
텍스처 노드를 걷어낸 뒤 부드러운 단색 파스텔 머티리얼로 바꾼다(동물의 숲풍).

사용 1) Blender 안에서: 오브젝트 선택 후 Text Editor 에서 Run Script (선택이 없으면 씬의 모든 메시).
사용 2) 헤드리스 배치:  blender -b -P cutify_assets.py -- --in "in.fbx" --out "out_soft.fbx" [--bevel 0.02] [--segments 3] [--pastel 0.35]
    (FBX 를 읽어 처리 후 다시 FBX 로 내보낸다. 텍스처는 버리고 원래 평균색만 파스텔로 옮긴다.)
"""
import bpy, sys, math, colorsys
from mathutils import Vector

ARGS = {"bevel": 0.02, "segments": 3, "angle": 30.0, "pastel": 0.35, "sat": 0.85, "rough": 0.85, "in": None, "out": None, "keep_tex_color": True}
if "--" in sys.argv:
    it = iter(sys.argv[sys.argv.index("--") + 1:])
    for a in it:
        k = a.lstrip("-")
        if k in ARGS:
            v = next(it)
            ARGS[k] = type(ARGS[k])(v) if ARGS[k] is not None and not isinstance(ARGS[k], bool) else (v if not isinstance(ARGS[k], bool) else v.lower() in ("1", "true", "y"))


def mean_image_color(img):
    """이미지 평균색(작게 샘플링). 없으면 None."""
    try:
        w, h = img.size
        if w == 0 or h == 0: return None
        px = img.pixels[:]
        step = max(1, (w * h) // 4096)
        r = g = b = 0.0; n = 0
        for i in range(0, w * h, step):
            r += px[i * 4]; g += px[i * 4 + 1]; b += px[i * 4 + 2]; n += 1
        return (r / n, g / n, b / n) if n else None
    except Exception:
        return None


def pastelize(rgb, mix=0.35, sat=0.85):
    """원래 색 → 흰색과 mix 만큼 섞고 채도를 sat 배 — 파스텔 톤."""
    r, g, b = [min(1.0, max(0.0, c)) for c in rgb]
    h, s, v = colorsys.rgb_to_hsv(r, g, b)
    s *= sat; v = min(1.0, v * 0.92 + 0.12)
    r, g, b = colorsys.hsv_to_rgb(h, s, v)
    return (r + (1 - r) * mix, g + (1 - g) * mix, b + (1 - b) * mix)


def flat_pastel_material(mat, mix, sat, rough):
    """머티리얼의 텍스처 노드를 지우고 Principled 단색으로."""
    if mat is None: return
    mat.use_nodes = True
    nt = mat.node_tree
    base = None
    for n in nt.nodes:
        if n.type == "BSDF_PRINCIPLED":
            base = n; break
    src = None
    if base is not None:
        inp = base.inputs.get("Base Color")
        if inp and inp.is_linked:
            up = inp.links[0].from_node
            if up.type == "TEX_IMAGE" and up.image is not None and ARGS["keep_tex_color"]:
                src = mean_image_color(up.image)
        if src is None:
            src = tuple(inp.default_value[:3]) if inp else (0.8, 0.8, 0.8)
    else:
        src = tuple(mat.diffuse_color[:3])
    # 노드 정리
    for n in list(nt.nodes):
        if n.type != "OUTPUT_MATERIAL":
            nt.nodes.remove(n)
    out = next((n for n in nt.nodes if n.type == "OUTPUT_MATERIAL"), None) or nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled"); bsdf.location = (-300, 0)
    c = pastelize(src, mix, sat)
    bsdf.inputs["Base Color"].default_value = (*c, 1.0)
    bsdf.inputs["Roughness"].default_value = rough
    for key in ("Specular IOR Level", "Specular"):
        if key in bsdf.inputs: bsdf.inputs[key].default_value = 0.2
    if "Coat Weight" in bsdf.inputs: bsdf.inputs["Coat Weight"].default_value = 0.0
    nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    mat.diffuse_color = (*c, 1.0)
    return c


def cutify(obj, bevel, segments, angle):
    if obj.type != "MESH": return False
    # 기존 Bevel 이 있으면 갱신, 없으면 추가(맨 앞이 아니라 마지막에 — 미러/어레이 뒤)
    mod = next((m for m in obj.modifiers if m.type == "BEVEL" and m.name == "CutifyBevel"), None)
    if mod is None:
        mod = obj.modifiers.new("CutifyBevel", "BEVEL")
    mod.width = bevel; mod.segments = segments; mod.limit_method = "ANGLE"; mod.angle_limit = math.radians(angle)
    mod.harden_normals = True; mod.miter_outer = "MITER_ARC"; mod.profile = 0.7
    # 스무스 셰이딩(각도 기준) — Blender 4.1+ 는 오퍼레이터, 이전은 auto_smooth
    for p in obj.data.polygons: p.use_smooth = True
    try:
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.shade_smooth_by_angle(angle=math.radians(angle))
    except Exception:
        try: obj.data.use_auto_smooth = True; obj.data.auto_smooth_angle = math.radians(angle)
        except Exception: pass
    return True


def run(objs):
    n_obj = 0; mats = {}
    for o in objs:
        if cutify(o, ARGS["bevel"], ARGS["segments"], ARGS["angle"]): n_obj += 1
        for slot in o.material_slots:
            if slot.material and slot.material.name not in mats:
                mats[slot.material.name] = flat_pastel_material(slot.material, ARGS["pastel"], ARGS["sat"], ARGS["rough"])
        if not o.material_slots and o.type == "MESH":
            m = bpy.data.materials.new("Pastel"); o.data.materials.append(m); mats[m.name] = flat_pastel_material(m, ARGS["pastel"], ARGS["sat"], ARGS["rough"])
    print("[cutify] objects:", n_obj, "materials:", len(mats))
    for k, v in mats.items(): print("   ", k, "→ #%02X%02X%02X" % tuple(int(c * 255) for c in v))


if ARGS["in"]:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=ARGS["in"])
    run([o for o in bpy.context.scene.objects if o.type == "MESH"])
    if ARGS["out"]:
        bpy.ops.object.select_all(action="SELECT")
        bpy.ops.export_scene.fbx(filepath=ARGS["out"], use_selection=True, apply_scale_options="FBX_SCALE_ALL", path_mode="COPY", embed_textures=False, mesh_smooth_type="FACE")
        print("[cutify] exported", ARGS["out"])
else:
    sel = [o for o in bpy.context.selected_objects if o.type == "MESH"] or [o for o in bpy.context.scene.objects if o.type == "MESH"]
    run(sel)
