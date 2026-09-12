"""49차 미니게임 3D 키트 v2 (Blender 5.x, blender-mcp execute_blender_code 로 실행)
   - MG_YutStick.fbx : 윷가락(반원기둥, 길이 1, 굴곡면 +Z)
   - MG_Token.fbx    : 윷 말(납작 원판 + 꼭지)
   - MG_Jar.fbx      : 투호 항아리(입 넓은 항아리, 높이 1, 입 반지름 0.32)
   - MG_TuhoArrow.fbx: 투호 화살(길이 1, +Y 방향, 꼬리 깃 2장)
   - MG_Ddakji.fbx   : 딱지(1×1, 두께 0.07) + 바람개비 접힘 무늬(Fold)
   임시 씬에서 만들고 export 뒤 씬을 지운다(열려 있는 씬은 건드리지 않음)."""
import bpy, bmesh, math, os
from mathutils import Vector
OUT = r"C:\dev\game\Assets\Resources\CoastRun"

def new_obj(scene, name, mesh):
    o = bpy.data.objects.new(name, mesh); scene.collection.objects.link(o); return o

def mesh_from_bm(bm, name, smooth=True):
    if smooth:
        for f in bm.faces: f.smooth = True
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free(); return me

def lathe(profile, segs=24):
    """profile: [(r, z), ...] 아래→위. 회전체."""
    bm = bmesh.new(); rings = []
    for r, z in profile:
        ring = []
        for i in range(segs):
            a = i / segs * math.pi * 2
            ring.append(bm.verts.new((math.cos(a) * r, math.sin(a) * r, z)))
        rings.append(ring)
    for k in range(len(rings) - 1):
        a, b = rings[k], rings[k + 1]
        for i in range(segs):
            bm.faces.new((a[i], a[(i + 1) % segs], b[(i + 1) % segs], b[i]))
    # caps
    if profile[0][0] > 1e-4: bm.faces.new(list(reversed(rings[0])))
    if profile[-1][0] > 1e-4: bm.faces.new(rings[-1])
    return bm

def build_yut(scene):
    bm = bmesh.new(); n = 12; L = 1.0; r = 0.11
    rows = []
    for k in range(2):
        y = -L / 2 if k == 0 else L / 2
        row = [bm.verts.new((math.cos(i / n * math.pi) * r, y, math.sin(i / n * math.pi) * r)) for i in range(n + 1)]
        rows.append(row)
    for i in range(n):
        f = bm.faces.new((rows[0][i], rows[0][i + 1], rows[1][i + 1], rows[1][i])); f.smooth = True
    bm.faces.new((rows[0][0], rows[1][0], rows[1][n], rows[0][n]))   # 평평한 바닥
    bm.faces.new(list(reversed(rows[0]))); bm.faces.new(rows[1])
    me = mesh_from_bm(bm, "Stick", smooth=False)
    for p in me.polygons: p.use_smooth = p.normal.z > 0.05 or abs(p.normal.y) < 0.5
    o = new_obj(scene, "MG_YutStick", me)
    bev = o.modifiers.new("Bevel", 'BEVEL'); bev.width = 0.015; bev.segments = 2
    return [o]

def build_token(scene):
    bm = lathe([(0.0, 0.0), (0.42, 0.0), (0.5, 0.08), (0.5, 0.18), (0.38, 0.24), (0.18, 0.24), (0.14, 0.34), (0.20, 0.48), (0.0, 0.56)], 28)
    o = new_obj(scene, "MG_Token", mesh_from_bm(bm, "Token"))
    return [o]

def build_jar(scene):
    prof = [(0.0, 0.0), (0.30, 0.0), (0.40, 0.08), (0.46, 0.30), (0.44, 0.55), (0.34, 0.74), (0.30, 0.86), (0.34, 0.96), (0.36, 1.0), (0.30, 1.0), (0.27, 0.94), (0.26, 0.70)]
    bm = lathe(prof, 32); jar = new_obj(scene, "Jar", mesh_from_bm(bm, "Jar"))
    # 귀(양옆 고리)
    ears = []
    for s in (-1, 1):
        # 귀(고리): bmesh 에 torus 연산자가 없어 lathe 로 도넛 단면을 돌린다
        ring = [(0.09 + 0.03 * math.cos(a), 0.03 * math.sin(a)) for a in [i / 10 * math.pi * 2 for i in range(11)]]
        e = new_obj(scene, "Ear", mesh_from_bm(lathe(ring, 16), "Ear")); e.location = (s * 0.44, 0, 0.62); e.rotation_euler = (0, math.pi / 2, 0); ears.append(e)
    root = bpy.data.objects.new("MG_Jar", None); scene.collection.objects.link(root)
    for o in [jar] + ears: o.parent = root
    return [root, jar] + ears

def build_tuho_arrow(scene):
    bm = bmesh.new(); bmesh.ops.create_cone(bm, cap_ends=True, segments=10, radius1=0.022, radius2=0.022, depth=1.0)
    me = mesh_from_bm(bm, "Shaft"); shaft = new_obj(scene, "Shaft", me); shaft.rotation_euler = (math.pi / 2, 0, 0); shaft.location = (0, 0.5, 0)
    bm = bmesh.new(); bmesh.ops.create_cone(bm, cap_ends=True, segments=10, radius1=0.05, radius2=0.0, depth=0.12)
    me = mesh_from_bm(bm, "Tip"); tip = new_obj(scene, "Tip", me); tip.rotation_euler = (math.pi / 2, 0, 0); tip.location = (0, 1.0, 0)
    fins = []
    for k in range(2):
        bm = bmesh.new()
        pts = [(0.0, 0.0), (0.0, 0.22), (0.075, 0.18), (0.075, 0.02)]
        vs = [bm.verts.new((x, y, 0.0)) for x, y in pts]; bm.faces.new(vs)
        vs2 = [bm.verts.new((-x, y, 0.0)) for x, y in pts]; bm.faces.new(list(reversed(vs2)))
        me = mesh_from_bm(bm, "Fin", smooth=False); f = new_obj(scene, "Fin", me); f.location = (0, 0.02, 0); f.rotation_euler = (0, k * math.pi / 2, 0)
        sol = f.modifiers.new("Solid", 'SOLIDIFY'); sol.thickness = 0.012; fins.append(f)
    root = bpy.data.objects.new("MG_TuhoArrow", None); scene.collection.objects.link(root)
    for o in [shaft, tip] + fins: o.parent = root
    return [root, shaft, tip] + fins

def build_ddakji(scene):
    bm = bmesh.new(); bmesh.ops.create_cube(bm, size=1.0)
    for v in bm.verts: v.co.z *= 0.07
    me = mesh_from_bm(bm, "Base", smooth=False); base = new_obj(scene, "Base", me)
    bev = base.modifiers.new("Bevel", 'BEVEL'); bev.width = 0.02; bev.segments = 2
    # 바람개비 접힘: 중심에서 네 변으로 가는 4개 삼각형(살짝 떠 있음)
    bm = bmesh.new(); c = bm.verts.new((0, 0, 0.037))
    corners = [(-0.5, -0.5), (0.5, -0.5), (0.5, 0.5), (-0.5, 0.5)]
    mids = [(0.0, -0.5), (0.5, 0.0), (0.0, 0.5), (-0.5, 0.0)]
    for i in range(4):
        a = bm.verts.new((corners[i][0] * 0.98, corners[i][1] * 0.98, 0.037)); b = bm.verts.new((mids[i][0] * 0.98, mids[i][1] * 0.98, 0.037))
        bm.faces.new((c, a, b))
    me = mesh_from_bm(bm, "Fold", smooth=False); fold = new_obj(scene, "Fold", me)
    sol = fold.modifiers.new("Solid", 'SOLIDIFY'); sol.thickness = 0.012; sol.offset = 1
    root = bpy.data.objects.new("MG_Ddakji", None); scene.collection.objects.link(root)
    for o in (base, fold): o.parent = root
    return [root, base, fold]

def export(scene, objs, path):
    win = bpy.context.window; win.scene = scene
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL',
                             use_mesh_modifiers=True, mesh_smooth_type='FACE', axis_forward='-Z', axis_up='Y',
                             bake_anim=False, add_leaf_bones=False, path_mode='STRIP')

def main():
    prev = bpy.context.window.scene
    scene = bpy.data.scenes.new("MGKit2")
    try:
        for name, fn in (("MG_YutStick", build_yut), ("MG_Token", build_token), ("MG_Jar", build_jar), ("MG_TuhoArrow", build_tuho_arrow), ("MG_Ddakji", build_ddakji)):
            objs = fn(scene)
            export(scene, objs, os.path.join(OUT, name + ".fbx"))
            print("exported", name)
    finally:
        bpy.context.window.scene = prev
        bpy.data.scenes.remove(scene)

if __name__ == "__main__":
    main()
