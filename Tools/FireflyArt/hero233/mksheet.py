import glob,os
from PIL import Image, ImageDraw
fs=sorted(glob.glob('fit/*.png')); cols=11; tw,th=180,300
sh=Image.new('RGB',(cols*tw,((len(fs)+cols-1)//cols)*th),(150,190,140)); d=ImageDraw.Draw(sh)
for i,f in enumerate(fs):
    im=Image.open(f); im.thumbnail((tw-4,th-18)); x,y=(i%cols)*tw,(i//cols)*th
    sh.paste(im,(x+2,y+2),im); d.text((x+2,y+th-15),os.path.basename(f)[11:-4],fill='black')
sh.save('_new_sheet.jpg',quality=88)
