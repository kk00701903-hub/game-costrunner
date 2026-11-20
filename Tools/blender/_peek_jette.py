
import bpy
for o in bpy.data.objects:
    print(o.type, o.name, o.parent.name if o.parent else None, [m.type for m in o.modifiers], tuple(round(v,2) for v in o.dimensions))
for a in bpy.data.armatures:
    print('ARM', a.name, [b.name for b in a.bones])
