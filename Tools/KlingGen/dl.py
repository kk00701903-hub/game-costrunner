
import sys, urllib.request, os
url, out = sys.argv[1], sys.argv[2]
req = urllib.request.Request(url, headers={"Referer": "https://app.klingai.com/", "User-Agent": "Mozilla/5.0"})
data = urllib.request.urlopen(req, timeout=60).read()
open(out, "wb").write(data)
from PIL import Image
im = Image.open(out); print(im.size, im.mode, len(data))
