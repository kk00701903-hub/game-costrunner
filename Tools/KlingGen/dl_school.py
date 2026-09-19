
import urllib.request, os
from PIL import Image
b="EMXN1y8qdwoGdXBsb2FkEg55bGFiLXN0dW50LXNncBpdc3BlY2lhbC1lZmZlY3Qvb3V0cHV0LzBkZDI3YWYzLTE4ZmUtNDg5Yy1hNGFlLWU3NDg4ZmM1NzVkOS8tODkxMTM5MDc0NTc2MTY2ODM4MC90ZW1wZ2c4NTEucG5n"
url="https://s15-kling.klingai.com/kimg/"+b+".origin?x-kcdn-pid=112372"
req=urllib.request.Request(url, headers={"Referer":"https://app.klingai.com/","User-Agent":"Mozilla/5.0"})
data=urllib.request.urlopen(req,timeout=90).read()
raw=r"C:\dev\game\Tools\KlingGen\out\school_raw.png"; open(raw,"wb").write(data)
im=Image.open(raw).convert("RGB"); w,h=im.size
ww,wh=int(w*0.16),int(h*0.06)
patch=im.crop((w-2*ww, h-wh, w-ww, h)).transpose(Image.FLIP_LEFT_RIGHT); im.paste(patch,(w-ww,h-wh))
im=im.resize((768,1024),Image.LANCZOS); im.save(r"C:\dev\game\Assets\Resources\CoastRun\UI_MG_Yard_School.png",optimize=True)
im.resize((384,512)).save(r"C:\dev\game\Tools\_shots\school_prev.jpg",quality=85); print(w,h)
