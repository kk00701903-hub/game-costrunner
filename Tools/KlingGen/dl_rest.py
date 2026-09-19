
import json, urllib.request, os
from PIL import Image
d=r"C:\dev\game\Tools\KlingGen\out\rest"
m=json.load(open(os.path.join(d,"_dl.json")))
ims=[]
for name,b in m.items():
    out=os.path.join(d,name+"_raw.png")
    url="https://s15-kling.klingai.com/kimg/"+b+".origin?x-kcdn-pid=112372"
    req=urllib.request.Request(url, headers={"Referer":"https://app.klingai.com/","User-Agent":"Mozilla/5.0"})
    data=urllib.request.urlopen(req,timeout=90).read(); open(out,"wb").write(data)
    im=Image.open(out).convert("RGB"); print(name, im.size); ims.append(im)
sheet=Image.new("RGB",(600*2+8,450),(20,20,20))
for i,im in enumerate(ims): sheet.paste(im.resize((600,450)),(i*604,0))
sheet.save(os.path.join(d,"_sheet.jpg"),quality=86)
