import sys,io,base64
from PIL import Image
names=sys.argv[1:]; ims=[Image.open(f'/{n}' if n.startswith('/') else n).convert('RGB') for n in names]
for im in ims: im.thumbnail((330,730))
W=Image.new('RGB',(sum(i.width for i in ims)+6*(len(ims)-1),max(i.height for i in ims)),'white'); x=0
for im in ims: W.paste(im,(x,0)); x+=im.width+6
b=io.BytesIO(); W.save(b,'JPEG',quality=50); print(base64.b64encode(b.getvalue()).decode())
