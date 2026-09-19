
from PIL import Image
import os, shutil
d=r"C:\dev\game\Tools\KlingGen\out\rest"; res=r"C:\dev\game\Assets\Resources\CoastRun"
for name in ["rest_home","rest_nap"]:
    dst=os.path.join(res,"Sched_"+name+".png")
    if os.path.exists(dst) and not os.path.exists(os.path.join(d,"old_Sched_"+name+".png")): shutil.copy2(dst, os.path.join(d,"old_Sched_"+name+".png"))
    im=Image.open(os.path.join(d,name+"_raw.png")).convert("RGB")
    w,h=im.size
    # 워터마크(우하단) 덮기: 바로 왼쪽 영역을 좌우반전해서 덮음
    ww,wh=int(w*0.16),int(h*0.07)
    patch=im.crop((w-2*ww, h-wh, w-ww, h)).transpose(Image.FLIP_LEFT_RIGHT)
    im.paste(patch,(w-ww,h-wh))
    im=im.resize((1024,768),Image.LANCZOS)
    im.save(dst,optimize=True); print(dst, im.size)
