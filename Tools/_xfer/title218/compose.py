import cv2, numpy as np, io, base64, sys
from PIL import Image, ImageFilter, ImageDraw, ImageFont
SRC=sys.argv[1]; SH=int(sys.argv[2]); OUT=sys.argv[3]
def load_rgba(n):
    im=Image.open(f'Tools/_xfer/title218/{n}.png').convert('RGBA'); a=np.array(im)
    al=a[:,:,3]; al=cv2.erode(al,np.ones((3,3),np.uint8),iterations=2); al=cv2.GaussianBlur(al,(3,3),0); a[:,:,3]=al
    return Image.fromarray(a)
W,H=1080,1920
src=Image.open(SRC).convert('RGB').resize((W,H),Image.LANCZOS).filter(ImageFilter.UnsharpMask(radius=1.6,percent=60,threshold=2))
bg=Image.new('RGB',(W,H)); bg.paste(src.crop((0,SH,W,H)),(0,0))
if SH>0: bg.paste(src.crop((0,H-SH,W,H)).transpose(Image.FLIP_TOP_BOTTOM),(0,H-SH))
ov=Image.new('RGBA',(W,H),(0,0,0,0)); d=ImageDraw.Draw(ov)
for y in range(380): d.line([(0,y),(W,y)],fill=(40,20,40,int(80*(1-y/380)**1.6)))
for y in range(H-620,H): t=(y-(H-620))/620; d.line([(0,y),(W,y)],fill=(30,20,30,int(120*t**1.8)))
bg=Image.alpha_composite(bg.convert('RGBA'),ov)
tl=Image.new('RGBA',(W,H),(0,0,0,0)); td=ImageDraw.Draw(tl)
fj=ImageFont.truetype('Assets/Resources/CoastRun/Fonts/Jua-Regular.ttf',118); fp=ImageFont.truetype('Assets/Resources/CoastRun/Fonts/Pretendard-Bold.ttf',34)
td.text((W//2+4,182),'너와 나의 주파수',font=fj,fill=(60,25,40,150),stroke_width=10,stroke_fill=(60,25,40,150),anchor='mm')
tl=tl.filter(ImageFilter.GaussianBlur(6)); td=ImageDraw.Draw(tl)
td.text((W//2,176),'너와 나의 주파수',font=fj,fill=(255,248,236,255),stroke_width=7,stroke_fill=(92,44,58,255),anchor='mm')
td.text((W//2,262),'COAST RUN · JEJU',font=fp,fill=(255,240,222,255),stroke_width=3,stroke_fill=(92,44,58,230),anchor='mm')
bg=Image.alpha_composite(bg,tl)
for n,(x,y) in {'story':(22,300),'more':(22,560),'play':(190,1400)}.items(): bg.alpha_composite(load_rgba(n),(x,y))
out=bg.convert('RGB'); out.save('/tmp/t.png'); open(OUT,'wb').write(open('/tmp/t.png','rb').read())
