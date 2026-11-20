import sys,os,glob
from PIL import Image, ImageDraw
H=os.path.expanduser('~/mnt/game/Tools/FireflyArt/hero233'); BAK=os.path.expanduser('~/mnt/game/Tools/_xfer/bak219/imp233')
names=sys.argv[2:]; out=sys.argv[1]; tw,th=240,380
sheet=Image.new('RGB',(len(names)*tw*2 if len(names)<=5 else 10*tw, th*((len(names)*2+9)//10 if len(names)>5 else 1)),(150,190,140))
d=ImageDraw.Draw(sheet); i=0
for n in names:
    for src in (f'{BAK}/{n}.png',f'{H}/fit/{n}.png'):
        im=Image.open(src).convert('RGBA'); im.thumbnail((tw-6,th-20)); x,y=(i%10)*tw,(i//10)*th
        sheet.paste(im,(x+3,y+3),im); d.text((x+3,y+th-16),n.replace('Raise_Girl_','')+(' new' if 'fit' in src else ' old'),fill='black'); i+=1
sheet.save(H+'/'+out,quality=88)
