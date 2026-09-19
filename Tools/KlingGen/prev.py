
from PIL import Image
im=Image.open(r"C:\dev\game\Tools\KlingGen\out\vista\Coast_NOON_raw.png"); im.thumbnail((600,800)); im.convert("RGB").save(r"C:\dev\game\Tools\KlingGen\out\vista\_prev.jpg",quality=85)
