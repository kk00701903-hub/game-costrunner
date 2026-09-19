
import json, urllib.request, os
from PIL import Image
d=r"C:\dev\game\Tools\KlingGen\out\vista"
m=json.load(open(os.path.join(d,"_dl.json"),encoding="utf-8"))
for name in ["Forest_NOON","Coast_NOON_2","Oreum_AUTUMN_2"]:
    out=os.path.join(d,name+"_raw.png")
    url="https://s15-kling.klingai.com/kimg/"+m[name]+".origin?x-kcdn-pid=112372"
    req=urllib.request.Request(url, headers={"Referer":"https://app.klingai.com/","User-Agent":"Mozilla/5.0"})
    data=urllib.request.urlopen(req,timeout=90).read(); open(out,"wb").write(data)
    print(name, Image.open(out).size)
ims=[Image.open(os.path.join(d,n+"_raw.png")).convert("RGB") for n in ["Forest_NOON","Coast_NOON_2","Oreum_AUTUMN_2"]]
sheet=Image.new("RGB",(330*3+8,440),(20,20,20))
for i,im in enumerate(ims): sheet.paste(im.resize((330,440),Image.LANCZOS),(i*334,0))
sheet.save(os.path.join(d,"_sheet2.jpg"),quality=85)
