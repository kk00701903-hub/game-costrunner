# 151차: 마을 소품 FBX → 각 정리(각도 스무딩) + AO 를 정점색으로 베이크(구석 진하게·모서리 밝게) → Models/Sharp/<name>.fbx
# 실행: blender -b --python sharpen_assets.py -- [이름...]
import bpy, sys, os, math
from mathutils import Vector
SRC = r"C:\dev\game\Assets\Resources\CoastRun\Models"
DST = os.path.join(SRC, "Sharp")
NAMES = ["Prop_OrangeTree","Prop_Bench","Prop_Hareubang","Prop_OrangeStall","Prop_Pavilion","Prop_UtilityPole",
         "Kerb_Onggi","Kerb_PlanterWood","Kerb_Buoys","Kerb_CrateStack","Prop_StoneWall","Kerb_PlanterBasalt","Kerb_StoneBed","Prop_CafeSet"]
if "--" in sys.argv and len(sys.argv) > sys.argv.index("--") + 1: NAMES = sys.argv[sys.argv.index("--") + 1:]
os.makedirs(DST, exist_ok=True)
log = open(os.path.join(os.path.dirname(os.path.abspath(__file__)) if "__file__" in globals() else SRC, "sharpen_log.txt"), "a", encoding="utf-8")
def L(x): print(x); log.write(x + "\n"); log.flush()

def process(name):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    src = os.path.join(SRC, name + ".fbx")
    if not os.path.exists(src): L(f"[skip] {name} 없음"); return
    bpy.ops.import_scene.fbx(filepath=src)
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    if not meshes: L(f"[skip] {name} 메시 없음"); return
    sc = bpy.context.scene
    sc.render.engine = 'CYCLES'; sc.cycles.samples = 24; sc.cycles.device = 'CPU'
    sc.render.bake.target = 'VERTEX_COLORS'; sc.render.bake.use_selected_to_active = False
    sc.world = bpy.data.worlds.new("W"); sc.world.use_nodes = True
    bg = sc.world.node_tree.nodes.get("Background"); bg.inputs[0].default_value = (1, 1, 1, 1)
    for o in meshes:
        bpy.ops.object.select_all(action='DESELECT'); o.select_set(True); bpy.context.view_layer.objects.active = o
        me = o.data
        # 각 정리: 40° 이상은 날카롭게(테두리가 또렷), 그 안은 부드럽게
        try: bpy.ops.object.shade_smooth_by_angle(angle=math.radians(40))
        except Exception:
            try: bpy.ops.object.shade_auto_smooth(angle=math.radians(40))
            except Exception: bpy.ops.object.shade_smooth()
        if not me.materials: me.materials.append(bpy.data.materials.new("M"))
        col = me.color_attributes.get("Col") or me.color_attributes.new(name="Col", type='BYTE_COLOR', domain='CORNER')
        me.color_attributes.active_color = col
        try:
            bpy.ops.object.bake(type='AO')
        except Exception as e:
            L(f"[warn] {name}/{o.name} bake 실패: {e}"); continue
        # AO → 스타일 정점색: 구석 0.70, 열린 면 1.0, 모서리(법선 꺾임) 살짝 밝게 1.06
        me.calc_loop_triangles()
        vn = [v.normal.copy() for v in me.vertices]
        for poly in me.polygons:
            for li in poly.loop_indices:
                lp = me.loops[li]; c = col.data[li].color
                ao = c[0]
                edge = 1.0 - max(0.0, vn[lp.vertex_index].dot(poly.normal))   # 정점 법선과 면 법선 차 = 모서리
                k = 0.70 + 0.30 * (ao ** 0.8) + 0.06 * min(1.0, edge * 2.0)
                col.data[li].color = (k, k * 0.985, k * 0.97, 1.0)   # 구석은 살짝 따뜻하게
    bpy.ops.object.select_all(action='SELECT')
    out = os.path.join(DST, name + ".fbx")
    bpy.ops.export_scene.fbx(filepath=out, use_selection=True, apply_scale_options='FBX_SCALE_ALL', colors_type='SRGB', mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False, path_mode='COPY')
    L(f"[ok] {name} → {out} ({len(meshes)} mesh)")

for n in NAMES:
    try: process(n)
    except Exception as e: L(f"[err] {n}: {e}")
L("done")
