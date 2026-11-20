import sys,io,base64
from PIL import Image
im=Image.open(sys.argv[1]).convert("RGB")
b=io.BytesIO(); im.save(b,"JPEG",quality=80); print(base64.b64encode(b.getvalue()).decode()); print("#"*52000)
