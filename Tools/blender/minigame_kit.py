"""48차-13 미니게임 3D 키트 (Blender 5.x, blender-mcp execute_blender_code 로 실행하거나
   blender --background --python Tools/blender/minigame_kit.py)
   - MG_Marble.fbx : 유리 구슬 = Glass(UV 스피어 r0.5) + Swirl(나선 리본, Solidify+Subsurf) + Core(작은 구 r0.2)
   - MG_Arrow.fbx  : 납작 조준 화살표(길이 1, +Y → Unity 에서는 -Z 를 향하므로 코드에서 180° 보정)
   Unity: Assets/Resources/CoastRun/*.fbx → MiniStage3D.Spawn("MG_Marble") 이 피벗으로 감싸 인스턴스. 머티리얼은 런타임(MiniStage3D.Lit).
"""
import bpy, bmesh, math, os
from mathutils import Vector

OUT = r"C:\dev\game\Assets\Resources\CoastRun"


def new_obj(scene, name, mesh):
    o = bpy.data.objects.new(name, mesh); scene.collection.objects.link(o); return o


def build_marble(scene):
    bm = bmesh.new(); bmesh.ops.create_uvsphere(bm, u_segments=32, v_segments=20, radius=0.5)
    for f in bm.faces: f.smooth = True
    me = bpy.data.meshes.new("Glass"); bm.to_mesh(me); bm.free(); glass = new_obj(scene, "Glass", me)

    bm = bmesh.new(); rows = []; n, w, r = 48, 0.16, 0.30
    for i in range(n + 1):
        a = i / n * math.pi * 2.0
        c = Vector((math.cos(a) * r * math.sin(a * 0.5 + 0.2), math.sin(a) * r * math.sin(a * 0.5 + 0.2), math.cos(a * 0.5) * 0.28))
        side = Vector((math.cos(a * 2.5), math.sin(a * 2.5), 0.0)).normalized() * w
        up = Vector((0, 0, 1)) * w * 0.35
        rows.append([bm.verts.new(c - side - up), bm.verts.new(c + side + up)])
    for i in range(n):
        a0, b0 = rows[i]; a1, b1 = rows[i + 1]; f = bm.faces.new((a0, b0, b1, a1)); f.smooth = True
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new("Swirl"); bm.to_mesh(me); bm.free(); swirl = new_obj(scene, "Swirl", me)
    sol = swirl.modifiers.new("Solid", 'SOLIDIFY'); sol.thickness = 0.05; sol.offset = 0
    sub = swirl.modifiers.new("Sub", 'SUBSURF'); sub.levels = 1; sub.render_levels = 1

    bm = bmesh.new(); bmesh.ops.create_uvsphere(bm, u_segments=20, v_segments=12, radius=0.20)
    for f in bm.faces: f.smooth = True
    me = bpy.data.meshes.new("Core"); bm.to_mesh(me); bm.free(); core = new_obj(scene, "Core", me)

    root = bpy.data.objects.new("MG_Marble", None); scene.collection.objects.link(root)
    for o in (glass, swirl, core): o.parent = root
    return [root, glass, swirl, core]


def build_arrow(scene):
    bm = bmesh.new()
    pts = [(-0.10, 0.0), (0.10, 0.0), (0.10, 0.62), (0.24, 0.62), (0.0, 1.0), (-0.24, 0.62), (-0.10, 0.62)]
    bm.faces.new([bm.verts.new((x, y, 0.0)) for x, y in pts])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new("Arrow"); bm.to_mesh(me); bm.free()
    arrow = new_obj(scene, "MG_Arrow", me)
    sol = arrow.modifiers.new("Solid", 'SOLIDIFY'); sol.thickness = 0.06; sol.offset = 1
    bev = arrow.modifiers.new("Bevel", 'BEVEL'); bev.width = 0.02; bev.segments = 2
    return [arrow]


def export(objs, path):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL',
                             use_mesh_modifiers=True, mesh_smooth_type='FACE', axis_forward='-Z', axis_up='Y',
                             bake_anim=False, add_leaf_bones=False, path_mode='STRIP')


def main():
    bpy.ops.wm.read_homefile(use_empty=True)
    scene = bpy.context.scene
    export(build_marble(scene), os.path.join(OUT, "MG_Marble.fbx"))
    export(build_arrow(scene), os.path.join(OUT, "MG_Arrow.fbx"))
    print("exported MG_Marble.fbx, MG_Arrow.fbx")


if __name__ == "__main__":
    main()
