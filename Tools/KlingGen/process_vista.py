
import os
from PIL import Image
d=r"C:\dev\game\Tools\KlingGen\out\vista"
out=r"C:\dev\game\Assets\Resources\CoastRun"
H={"Coast_NOON":0.925,"Coast_SPRING":0.76,"Coast_AUTUMN":0.72,"Coast_WINTER":0.88,
"Village_SPRING":0.90,"Village_NOON":0.93,"Village_AUTUMN":0.93,"Village_WINTER":0.93,
"Oreum_SPRING":0.86,"Oreum_NOON":0.95,"Oreum_AUTUMN":0.80,"Oreum_WINTER":0.92,
"Forest_SPRING":0.90,"Forest_NOON":0.85,"Forest_AUTUMN":0.82,"Forest_WINTER":0.86,
"Coast_NOON_2":0.92,"Coast_SPRING_2":0.78,"Coast_AUTUMN_2":0.86,"Village_NOON_2":0.86,
"Oreum_NOON_2":0.88,"Oreum_AUTUMN_2":0.85,"Cave_NOON":0.90,"Village_SPRING_2":0.92,"Forest_NOON_2":0.90,"Coast_WINTER_2":0.90}
for name,h in H.items():
    im=Image.open(os.path.join(d,name+"_raw.png")).convert("RGB")
    w,hh=im.size
    bot=min(h/0.90,0.955)
    im=im.crop((0,0,w,int(hh*bot)))
    nw=1536; nh=int(round(im.height*nw/im.width))
    im=im.resize((nw,nh),Image.LANCZOS)
    im.save(os.path.join(out,"Sky_"+name+".png"),optimize=True)
    print(name, im.size, round(bot,3))
