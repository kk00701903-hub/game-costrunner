# Jeju seaside building & prop kit for 『우리의 송전탑』 (Coast Run).
# Run inside Blender 4.x/5.x:  Text editor → Open this file → Run Script (Alt+P).
# Builds low-poly 2.5D buildings/props with clean per-face UVs, then exports each
# as its own FBX into the Unity project (Assets/Resources/CoastRun/Models).
#
# Conventions (match Unity after the FBX axis swap: Blender +X → Unity +X,
# Blender +Y → Unity +Z, Blender +Z → Unity +Y):
#   • Front of a building faces +X (the road side in the game).
#   • Origin at the building's front-bottom-centre; 1 unit = 1 m.
#   • Front face UV 0–1 → a painted facade texture (Tex_Facade_*).
#   • Side/back faces tile a wall texture; roof faces tile a roof texture.
# Materials carry only names + base colours; Unity maps names to textures.

import bpy, bmesh, math, os, random
from mathutils import Vector

EXPORT_DIR = r"C:\dev\game\Assets\Resources\CoastRun\Models"
os.makedirs(EXPORT_DIR, exist_ok=True)
random.seed(7)

# ── scene reset ──────────────────────────────────────────────────────────────
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for coll in (bpy.data.meshes, bpy.data.materials):
    for item in list(coll):
        if item.users == 0:
            coll.remove(item)

# ── materials ────────────────────────────────────────────────────────────────
def mat(name, rgb):
    m = bpy.data.materials.get(name)
    if m is None:
        m = bpy.data.materials.new(name)
        m.use_nodes = True
        bsdf = m.node_tree.nodes.get("Principled BSDF")
        if bsdf:
            bsdf.inputs["Base Color"].default_value = (*rgb, 1.0)
            bsdf.inputs["Roughness"].default_value = 0.9
    return m

MATS = {
    "Wall":        mat("Wall", (0.93, 0.89, 0.80)),
    "WallCool":    mat("WallCool", (0.82, 0.88, 0.92)),
    "Roof_Terracotta": mat("Roof_Terracotta", (0.85, 0.42, 0.28)),
    "Roof_Slate":  mat("Roof_Slate", (0.36, 0.45, 0.55)),
    "Roof_Basalt": mat("Roof_Basalt", (0.16, 0.16, 0.18)),
    "Stone":       mat("Stone", (0.22, 0.22, 0.24)),
    "Wood":        mat("Wood", (0.55, 0.38, 0.22)),
    "Awning_Red":  mat("Awning_Red", (0.85, 0.30, 0.28)),
    "Awning_Blue": mat("Awning_Blue", (0.30, 0.50, 0.80)),
    "Awning_Orange": mat("Awning_Orange", (0.95, 0.60, 0.25)),
    "Glass":       mat("Glass", (0.55, 0.75, 0.85)),
    "Trunk":       mat("Trunk", (0.40, 0.28, 0.18)),
    "Leaf":        mat("Leaf", (0.25, 0.55, 0.28)),
    "Orange":      mat("Orange", (0.98, 0.60, 0.15)),
    "Sign":        mat("Sign", (0.98, 0.95, 0.88)),
    "Metal":       mat("Metal", (0.45, 0.47, 0.50)),
    "Concrete":    mat("Concrete", (0.72, 0.72, 0.70)),
}
for k in "ABCDEFGH":
    MATS["Facade_" + k] = mat("Facade_" + k, (0.9, 0.9, 0.9))


def new_object(name, verts, faces, face_mats, uvs=None):
    """verts: [(x,y,z)], faces: [[i..]], face_mats: [material name per face],
    uvs: optional [[(u,v) per loop] per face]."""
    me = bpy.data.meshes.new(name)
    ob = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(ob)
    bm = bmesh.new()
    bverts = [bm.verts.new(Vector(v)) for v in verts]
    bm.verts.ensure_lookup_table()
    uv_layer = bm.loops.layers.uv.new("UVMap")
    mat_index = {}
    for fi, f in enumerate(faces):
        try:
            bf = bm.faces.new([bverts[i] for i in f])
        except ValueError:
            continue
        mname = face_mats[fi]
        if mname not in mat_index:
            mat_index[mname] = len(ob.data.materials)
            ob.data.materials.append(MATS[mname])
        bf.material_index = mat_index[mname]
        if uvs is not None and uvs[fi] is not None:
            for loop, uv in zip(bf.loops, uvs[fi]):
                loop[uv_layer].uv = uv
    bm.normal_update()
    bm.to_mesh(me)
    bm.free()
    return ob


def box(name, x0, x1, y0, y1, z0, z1, mats, tile=1.0, front_uv01=False):
    """Axis-aligned box. mats: dict with keys front(+X), back, left(-Y), right(+Y),
    top, bottom → material names. front_uv01 maps the +X face to UV 0..1."""
    v = [(x0,y0,z0),(x1,y0,z0),(x1,y1,z0),(x0,y1,z0),
         (x0,y0,z1),(x1,y0,z1),(x1,y1,z1),(x0,y1,z1)]
    faces = [[1,2,6,5],   # +X front
             [3,0,4,7],   # -X back
             [0,1,5,4],   # -Y left
             [2,3,7,6],   # +Y right
             [4,5,6,7],   # top
             [3,2,1,0]]   # bottom
    keys = ["front","back","left","right","top","bottom"]
    fm = [mats[k] for k in keys]
    w = (y1-y0); d = (x1-x0); h = (z1-z0)
    def rect_uv(a, b):  # loops in face order: bottom-left → bottom-right → top-right → top-left
        return [(0,0),(a,0),(a,b),(0,b)]
    uvs = [rect_uv(1,1) if front_uv01 else rect_uv(w*tile, h*tile),
           rect_uv(w*tile, h*tile), rect_uv(d*tile, h*tile), rect_uv(d*tile, h*tile),
           rect_uv(d*tile, w*tile), rect_uv(d*tile, w*tile)]
    return new_object(name, v, faces, fm, uvs)


def join(objs, name):
    for o in bpy.context.selected_objects:
        o.select_set(False)
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.join()
    ob = bpy.context.view_layer.objects.active
    ob.name = name
    return ob


def export(ob, filename):
    for o in bpy.context.selected_objects:
        o.select_set(False)
    ob.select_set(True)
    bpy.context.view_layer.objects.active = ob
    path = os.path.join(EXPORT_DIR, filename + ".fbx")
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_ALL', bake_space_transform=True,
        axis_forward='-Z', axis_up='Y', object_types={'MESH'},
        mesh_smooth_type='OFF', use_mesh_modifiers=True, add_leaf_bones=False,
        path_mode='STRIP', embed_textures=False)
    print("exported", path)


# ── roof shapes ──────────────────────────────────────────────────────────────
def gable_roof(name, x0, x1, y0, y1, z0, rise, mat_name, overhang=0.35):
    """Ridge runs along Y (parallel to the road). Slopes face ±X."""
    ox0, ox1 = x0-overhang, x1+overhang
    oy0, oy1 = y0-overhang, y1+overhang
    xm = (x0+x1)*0.5
    v = [(ox0,oy0,z0),(ox1,oy0,z0),(ox1,oy1,z0),(ox0,oy1,z0),
         (xm,oy0,z0+rise),(xm,oy1,z0+rise)]
    faces = [[1,2,5,4],   # +X slope
             [3,0,4,5],   # -X slope
             [0,1,4],     # -Y gable
             [2,3,5],     # +Y gable
             [3,2,1,0]]   # underside
    fm = [mat_name, mat_name, "Wall", "Wall", "Wall"]
    L = oy1-oy0; S = math.hypot((ox1-ox0)*0.5, rise)
    uvs = [[(0,0),(L,0),(L,S),(0,S)], [(0,0),(L,0),(L,S),(0,S)],
           [(0,0),(1,0),(0.5,1)], [(0,0),(1,0),(0.5,1)], [(0,0),(1,0),(1,1),(0,1)]]
    return new_object(name, v, faces, fm, uvs)


def hip_roof(name, x0, x1, y0, y1, z0, rise, mat_name, overhang=0.35):
    ox0, ox1 = x0-overhang, x1+overhang
    oy0, oy1 = y0-overhang, y1+overhang
    xm = (x0+x1)*0.5; inset = min(1.2, (oy1-oy0)*0.25)
    v = [(ox0,oy0,z0),(ox1,oy0,z0),(ox1,oy1,z0),(ox0,oy1,z0),
         (xm,oy0+inset,z0+rise),(xm,oy1-inset,z0+rise)]
    faces = [[1,2,5,4],[3,0,4,5],[0,1,4],[2,3,5],[3,2,1,0]]
    fm = [mat_name]*4 + ["Wall"]
    L = oy1-oy0; S = math.hypot((ox1-ox0)*0.5, rise)
    uvs = [[(0,0),(L,0),(L-inset,S),(inset,S)], [(0,0),(L,0),(L-inset,S),(inset,S)],
           [(0,0),(1,0),(0.5,1)], [(0,0),(1,0),(0.5,1)], [(0,0),(1,0),(1,1),(0,1)]]
    return new_object(name, v, faces, fm, uvs)


# ── building kit ─────────────────────────────────────────────────────────────
def building(key, width, depth, storeys, roof, facade, wall="Wall",
             awning=None, sign=True, balcony=False, ac_units=0, storey_h=3.0):
    """Front at +X, x in [-depth, 0], y centred, z from 0."""
    parts = []
    h = storeys*storey_h
    y0, y1 = -width*0.5, width*0.5
    body = box(key+"_body", -depth, 0.0, y0, y1, 0.0, h,
               {"front": facade, "back": wall, "left": wall, "right": wall,
                "top": "Concrete", "bottom": "Concrete"}, tile=0.5, front_uv01=True)
    parts.append(body)
    # plinth (dark base) so the building sits on the pavement instead of floating
    parts.append(box(key+"_plinth", -depth-0.05, 0.06, y0-0.05, y1+0.05, 0.0, 0.18,
                     {k: "Stone" for k in ["front","back","left","right","top","bottom"]}, tile=1))
    if roof == "flat":
        parts.append(box(key+"_parapet", -depth-0.15, 0.15, y0-0.15, y1+0.15, h, h+0.45,
                         {k: wall for k in ["front","back","left","right","top","bottom"]}, tile=0.5))
        parts.append(box(key+"_rooftop", -depth, 0.0, y0, y1, h, h+0.05,
                         {k: "Roof_Slate" for k in ["front","back","left","right","top","bottom"]}, tile=0.5))
        # water tank + stair house, the Jeju rooftop signature
        parts.append(box(key+"_tank", -depth*0.6, -depth*0.6+0.9, y0+0.6, y0+1.5, h+0.45, h+1.35,
                         {k: "Awning_Blue" for k in ["front","back","left","right","top","bottom"]}, tile=1))
    elif roof == "gable":
        parts.append(gable_roof(key+"_roof", -depth, 0.0, y0, y1, h, 1.1 + storeys*0.15, "Roof_Terracotta"))
    elif roof == "basalt":
        parts.append(hip_roof(key+"_roof", -depth, 0.0, y0, y1, h, 0.9, "Roof_Basalt"))
    elif roof == "slate":
        parts.append(hip_roof(key+"_roof", -depth, 0.0, y0, y1, h, 1.0, "Roof_Slate"))
    if awning:
        aw_w = width*0.7
        parts.append(new_object(key+"_awning",
            [(0.0, -aw_w/2, 2.45), (1.1, -aw_w/2, 2.05), (1.1, aw_w/2, 2.05), (0.0, aw_w/2, 2.45),
             (1.1, -aw_w/2, 1.85), (1.1, aw_w/2, 1.85)],
            [[0,1,2,3],[3,2,1,0],[1,4,5,2]],
            [awning, awning, awning],
            [[(0,0),(1,0),(1,1),(0,1)],[(0,0),(1,0),(1,1),(0,1)],[(0,0),(0.2,0),(0.2,1),(0,1)]]))
    if sign:
        parts.append(box(key+"_sign", 0.02, 0.14, -width*0.36, width*0.36, 2.55, 3.05,
                         {"front": "Sign", "back": "Metal", "left": "Metal", "right": "Metal",
                          "top": "Metal", "bottom": "Metal"}, tile=1, front_uv01=True))
    if balcony:
        for s in range(1, storeys):
            z = s*storey_h + 0.05
            parts.append(box(key+f"_balc{s}", 0.0, 0.9, -width*0.42, width*0.42, z, z+0.12,
                             {k: "Concrete" for k in ["front","back","left","right","top","bottom"]}, tile=1))
            parts.append(box(key+f"_rail{s}", 0.86, 0.92, -width*0.42, width*0.42, z+0.12, z+1.0,
                             {k: "Metal" for k in ["front","back","left","right","top","bottom"]}, tile=1))
    for i in range(ac_units):
        yy = y0 + 0.8 + i*(width-1.6)/max(1, ac_units-1) if ac_units > 1 else 0.0
        parts.append(box(key+f"_ac{i}", 0.0, 0.35, yy-0.3, yy+0.3, h-1.2, h-0.65,
                         {k: "Metal" for k in ["front","back","left","right","top","bottom"]}, tile=1))
    ob = join(parts, key)
    return ob


# Six Jeju street buildings. Facade letters map to Firefly paintings in Unity:
#   A cream apartment (3f)  B mint block (2f)  C 감귤 shop (1f)  D 돌담 cafe (1f)
#   E 해녀의 집 seafood (1f, blue awning)  F guesthouse (2f, gable)  G convenience
#   store (1f, flat)  H slate-roof 민가 (1f)
KIT = [
    ("Bldg_A", dict(width=6.0, depth=6.0, storeys=3, roof="flat",   facade="Facade_A", awning="Awning_Blue", balcony=True, ac_units=2)),
    ("Bldg_B", dict(width=5.4, depth=6.0, storeys=2, roof="flat",   facade="Facade_B", wall="WallCool", awning="Awning_Orange", ac_units=1)),
    ("Bldg_C", dict(width=5.0, depth=5.0, storeys=1, roof="gable",  facade="Facade_C", awning="Awning_Orange", storey_h=3.4)),
    ("Bldg_D", dict(width=5.6, depth=5.0, storeys=1, roof="basalt", facade="Facade_D", awning=None, storey_h=3.3)),
    ("Bldg_E", dict(width=6.2, depth=5.0, storeys=1, roof="slate",  facade="Facade_E", awning="Awning_Blue", storey_h=3.3)),
    ("Bldg_F", dict(width=5.2, depth=5.6, storeys=2, roof="gable",  facade="Facade_F", awning=None, balcony=True)),
    ("Bldg_G", dict(width=6.4, depth=5.0, storeys=1, roof="flat",   facade="Facade_G", awning="Awning_Red", storey_h=3.4)),
    ("Bldg_H", dict(width=4.8, depth=4.6, storeys=1, roof="slate",  facade="Facade_H", awning=None, sign=False, storey_h=3.0)),
]

built = []
for name, kw in KIT:
    ob = building(name, **kw)
    built.append(ob)
    export(ob, name)


# ── props ────────────────────────────────────────────────────────────────────
def stone_wall(name, length=6.0, height=0.85, thick=0.45):
    """Jeju 돌담: a low wall of stacked basalt blocks, slightly irregular."""
    parts = []
    rows = 3; cols = int(length/0.55)
    for r in range(rows):
        for c in range(cols):
            w = 0.5 + random.uniform(-0.08, 0.1)
            h = height/rows + random.uniform(-0.05, 0.05)
            y0 = -length/2 + c*(length/cols) + random.uniform(-0.04, 0.04)
            z0 = r*(height/rows) + random.uniform(-0.03, 0.03)
            x0 = -thick/2 + random.uniform(-0.05, 0.05)
            parts.append(box(f"{name}_s{r}_{c}", x0, x0+thick, y0, y0+w, z0, z0+h,
                             {k: "Stone" for k in ["front","back","left","right","top","bottom"]}, tile=1))
    return join(parts, name)


def orange_tree(name, height=2.6):
    parts = []
    bpy.ops.mesh.primitive_cylinder_add(vertices=8, radius=0.12, depth=height*0.45,
                                        location=(0, 0, height*0.225))
    trunk = bpy.context.active_object; trunk.name = name+"_trunk"
    trunk.data.materials.append(MATS["Trunk"]); parts.append(trunk)
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=height*0.42,
                                          location=(0, 0, height*0.68))
    canopy = bpy.context.active_object; canopy.name = name+"_canopy"
    canopy.data.materials.append(MATS["Leaf"]); parts.append(canopy)
    for i in range(9):
        a = random.uniform(0, math.tau); e = random.uniform(-0.4, 0.9)
        r = height*0.42
        loc = (r*math.cos(a)*math.cos(e), r*math.sin(a)*math.cos(e), height*0.68 + r*math.sin(e))
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=0.09, location=loc)
        o = bpy.context.active_object; o.name = f"{name}_o{i}"
        o.data.materials.append(MATS["Orange"]); parts.append(o)
    return join(parts, name)


def utility_pole(name, height=7.0):
    parts = []
    bpy.ops.mesh.primitive_cylinder_add(vertices=8, radius=0.13, depth=height, location=(0, 0, height/2))
    pole = bpy.context.active_object; pole.name = name+"_pole"
    pole.data.materials.append(MATS["Concrete"]); parts.append(pole)
    for i, z in enumerate((height-0.4, height-1.2)):
        parts.append(box(f"{name}_arm{i}", -0.06, 0.06, -0.9, 0.9, z, z+0.1,
                         {k: "Wood" for k in ["front","back","left","right","top","bottom"]}, tile=1))
        for y in (-0.75, -0.25, 0.25, 0.75):
            bpy.ops.mesh.primitive_cylinder_add(vertices=6, radius=0.05, depth=0.16, location=(0, y, z+0.18))
            ins = bpy.context.active_object; ins.data.materials.append(MATS["Sign"]); parts.append(ins)
    return join(parts, name)


def bench(name):
    parts = [box(name+"_seat", -0.25, 0.25, -0.8, 0.8, 0.42, 0.48,
                 {k: "Wood" for k in ["front","back","left","right","top","bottom"]}, tile=1),
             box(name+"_back", -0.30, -0.24, -0.8, 0.8, 0.50, 0.90,
                 {k: "Wood" for k in ["front","back","left","right","top","bottom"]}, tile=1)]
    for y in (-0.7, 0.7):
        parts.append(box(f"{name}_leg{y}", -0.2, 0.2, y-0.04, y+0.04, 0.0, 0.42,
                         {k: "Metal" for k in ["front","back","left","right","top","bottom"]}, tile=1))
    return join(parts, name)


def orange_stall(name):
    """감귤 노점: wooden cart, crates of oranges, striped awning."""
    parts = [box(name+"_cart", -0.5, 0.5, -0.9, 0.9, 0.35, 0.85,
                 {k: "Wood" for k in ["front","back","left","right","top","bottom"]}, tile=1)]
    for y in (-0.55, 0.0, 0.55):
        parts.append(box(f"{name}_crate{y}", -0.4, 0.4, y-0.25, y+0.25, 0.85, 1.05,
                         {k: "Wood" for k in ["front","back","left","right","top","bottom"]}, tile=1))
        for i in range(5):
            bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=0.09,
                location=(random.uniform(-0.28, 0.28), y+random.uniform(-0.15, 0.15), 1.1))
            o = bpy.context.active_object; o.data.materials.append(MATS["Orange"]); parts.append(o)
    for y in (-0.8, 0.8):
        parts.append(box(f"{name}_post{y}", -0.05, 0.05, y-0.04, y+0.04, 0.85, 2.1,
                         {k: "Wood" for k in ["front","back","left","right","top","bottom"]}, tile=1))
    parts.append(new_object(name+"_awning",
        [(-0.7,-1.0,2.1),(0.7,-1.0,1.95),(0.7,1.0,1.95),(-0.7,1.0,2.1)],
        [[0,1,2,3],[3,2,1,0]], ["Awning_Orange","Awning_Orange"],
        [[(0,0),(1,0),(1,1),(0,1)]]*2))
    for w in (-0.15, 0.15):
        bpy.ops.mesh.primitive_cylinder_add(vertices=10, radius=0.3, depth=0.1,
                                            location=(w*0+0.0, -1.0 if w < 0 else 1.0, 0.3), rotation=(math.pi/2, 0, 0))
        wheel = bpy.context.active_object; wheel.data.materials.append(MATS["Metal"]); parts.append(wheel)
    return join(parts, name)


PROPS = [
    ("Prop_StoneWall", lambda: stone_wall("Prop_StoneWall")),
    ("Prop_OrangeTree", lambda: orange_tree("Prop_OrangeTree")),
    ("Prop_UtilityPole", lambda: utility_pole("Prop_UtilityPole")),
    ("Prop_Bench", lambda: bench("Prop_Bench")),
    ("Prop_OrangeStall", lambda: orange_stall("Prop_OrangeStall")),
]
for name, fn in PROPS:
    ob = fn()
    built.append(ob)
    export(ob, name)

# Lay the kit out in a row for a quick look in the viewport.
for i, ob in enumerate(built):
    ob.location = (0, i*9.0, 0)
print("Jeju kit done:", len(built), "objects →", EXPORT_DIR)
