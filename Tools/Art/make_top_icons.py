
from PIL import Image, ImageDraw, ImageFont, ImageFilter
import math, os
OUT=r"C:\dev\game\Assets\Resources\CoastRun"
S=256
NAVY=(30,42,92); NAVY2=(52,74,150); RIM=(120,226,255); RIM2=(60,150,220); WHITE=(255,255,255); YEL=(255,214,70); PINK=(255,120,170)
def base():
    im=Image.new("RGBA",(S,S),(0,0,0,0)); d=ImageDraw.Draw(im)
    # 그림자
    sh=Image.new("RGBA",(S,S),(0,0,0,0)); ImageDraw.Draw(sh).ellipse((14,20,S-14,S-4),fill=(0,0,0,110)); sh=sh.filter(ImageFilter.GaussianBlur(8)); im.alpha_composite(sh)
    # 림(하늘) + 안쪽 남색 라디얼
    d.ellipse((8,8,S-8,S-8),fill=RIM2); d.ellipse((13,13,S-13,S-13),fill=RIM)
    disc=Image.new("RGBA",(S,S),(0,0,0,0)); dd=ImageDraw.Draw(disc)
    for r in range(105,0,-1):
        t=r/105; c=tuple(int(NAVY2[i]*(1-t)+NAVY[i]*t) for i in range(3))
        dd.ellipse((128-r,128-r,128+r,128+r),fill=c+(255,))
    im.alpha_composite(disc)
    # 상단 하이라이트
    hl=Image.new("RGBA",(S,S),(0,0,0,0)); ImageDraw.Draw(hl).ellipse((50,30,206,110),fill=(255,255,255,55)); hl=hl.filter(ImageFilter.GaussianBlur(6)); im.alpha_composite(hl)
    return im
def glyph_layer(): return Image.new("RGBA",(S,S),(0,0,0,0))
def finish(im,g,name):
    sh=g.copy(); px=sh.load()
    for y in range(S):
        for x in range(S):
            p=px[x,y]
            if p[3]: px[x,y]=(0,0,0,int(p[3]*0.5))
    sh=sh.filter(ImageFilter.GaussianBlur(3))
    off=Image.new("RGBA",(S,S),(0,0,0,0)); off.alpha_composite(sh,(2,4)); im.alpha_composite(off); im.alpha_composite(g)
    im.save(os.path.join(OUT,name+".png")); print(name)
def font(sz):
    for f in [r"C:\Windows\Fonts\malgunbd.ttf", r"C:\Windows\Fonts\arialbd.ttf"]:
        if os.path.exists(f): return ImageFont.truetype(f,sz)
    return ImageFont.load_default()
# 1 도움 ?
im=base(); g=glyph_layer(); d=ImageDraw.Draw(g); f=font(150)
d.text((128,128),"?",font=f,fill=WHITE,anchor="mm"); finish(im,g,"Icon_Top_Help")
# 2 Lv 왕관(노랑)
im=base(); g=glyph_layer(); d=ImageDraw.Draw(g)
pts=[(48,168),(40,90),(84,126),(128,64),(172,126),(216,90),(208,168)]
d.polygon(pts,fill=YEL); d.rectangle((48,168,208,190),fill=(255,190,40))
for cx in (40,128,216): d.ellipse((cx-13,(90 if cx!=128 else 64)-13,cx+13,(90 if cx!=128 else 64)+13),fill=YEL)
for cx in (80,128,176): d.ellipse((cx-9,144-9,cx+9,144+9),fill=(255,90,120))
finish(im,g,"Icon_Top_Lv")
# 3 마이룸 집
im=base(); g=glyph_layer(); d=ImageDraw.Draw(g)
d.polygon([(128,52),(40,132),(216,132)],fill=WHITE); d.rectangle((62,130,194,196),fill=WHITE)
d.rectangle((112,150,144,196),fill=NAVY); d.rectangle((160,116,182,150),fill=WHITE)
finish(im,g,"Icon_Top_Room")
# 4 상점 가게(차양)
im=base(); g=glyph_layer(); d=ImageDraw.Draw(g)
d.rectangle((52,120,204,196),fill=WHITE)
for i in range(5):
    x0=44+i*34; d.rectangle((x0,84,x0+34,120),fill=WHITE if i%2==0 else PINK)
    d.ellipse((x0,106,x0+34,134),fill=WHITE if i%2==0 else PINK)
d.rectangle((44,70,212,88),fill=WHITE); d.rectangle((110,150,146,196),fill=NAVY); d.rectangle((70,140,98,166),fill=NAVY)
finish(im,g,"Icon_Top_Shop")
# 5 가방 선물상자
im=base(); g=glyph_layer(); d=ImageDraw.Draw(g)
d.rectangle((56,110,200,196),fill=WHITE); d.rectangle((48,84,208,116),fill=WHITE)
d.rectangle((118,84,138,196),fill=PINK); d.rectangle((48,94,208,108),fill=PINK)
d.ellipse((92,52,132,92),outline=PINK,width=10); d.ellipse((124,52,164,92),outline=PINK,width=10)
finish(im,g,"Icon_Top_Bag")
# 6 뽑기 캡슐머신
im=base(); g=glyph_layer(); d=ImageDraw.Draw(g)
d.ellipse((58,46,198,186),fill=WHITE)   # 유리돔
cols=[(255,110,140),(90,200,255),(255,210,70),(120,220,120),(190,140,255),(255,160,90)]
pos=[(96,96),(136,86),(160,124),(120,130),(90,140),(150,158)]
for (x,y),c in zip(pos,cols): d.ellipse((x-16,y-16,x+16,y+16),fill=c)
d.rectangle((74,168,182,204),fill=(255,80,120)); d.rectangle((60,196,196,214),fill=(255,80,120))
d.rectangle((108,178,148,196),fill=NAVY)
finish(im,g,"Icon_Top_Gacha")
