
import sys, json, urllib.request, os
from PIL import Image
d=r"C:\dev\game\Tools\KlingGen\out\vista"
m=json.load(open(os.path.join(d,"_dl.json"),encoding="utf-8"))
for name,b in m.items():
    out=os.path.join(d,name+"_raw.png")
    if os.path.exists(out): continue
    url="https://s15-kling.klingai.com/kimg/"+b+".origin?x-kcdn-pid=112372"
    req=urllib.request.Request(url, headers={"Referer":"https://app.klingai.com/","User-Agent":"Mozilla/5.0"})
    data=urllib.request.urlopen(req,timeout=90).read(); open(out,"wb").write(data)
    print(name, Image.open(out).size)
names=[n for n in m]
ims=[Image.open(os.path.join(d,n+"_raw.png")).convert("RGB") for n in names]
tw,th=330,440; cols=min(6,len(ims)); rows=(len(ims)+cols-1)//cols
sheet=Image.new("RGB",(tw*cols+4*(cols-1),th*rows+4*(rows-1)),(20,20,20))
for i,im in enumerate(ims): sheet.paste(im.resize((tw,th),Image.LANCZOS),((i%cols)*(tw+4),(i//cols)*(th+4)))
sheet.save(os.path.join(d,"_sheet.jpg"),quality=85)
