# 233차: raw(흰 배경) → 배경 투명 → 원본 그림의 캔버스·알파 영역에 맞춰 fit/ 에 저장
import sys, os
from PIL import Image, ImageDraw, ImageFilter
G=os.path.expanduser('~/mnt/game'); H=G+'/Tools/FireflyArt/hero233'; BAK=G+'/Tools/_xfer/bak219/imp233'
def cut(im):
    import numpy as np
    from scipy import ndimage as nd
    rgb=np.asarray(im.convert('RGB')).astype(np.int16); h,w,_=rgb.shape
    mx=rgb.max(2); mn=rgb.min(2)
    allowed=((mx-mn)<16)&(mn>150)          # 흰 배경·회색 바닥 그림자(무채색 밝은 색)
    lab,n=nd.label(allowed)
    edge=set(np.unique(np.concatenate([lab[0],lab[-1],lab[:,0],lab[:,-1]])))-{0}
    bg=np.isin(lab,list(edge))
    # 떨어진 조각 중 밝고 연한 것(점프 그림자 등)·아주 작은 티끌은 지운다
    fl,fn=nd.label(~bg)
    if fn>1:
        sizes=nd.sum(np.ones_like(fl),fl,range(1,fn+1)); lightf=nd.mean((mn>150).astype(float),fl,range(1,fn+1))
        big=sizes.max()
        drop=[k+1 for k in range(fn) if sizes[k]<big*0.002 or (lightf[k]>0.85 and sizes[k]<big*0.3)]
        if drop: bg|=np.isin(fl,drop)
    al=Image.fromarray(np.where(bg,0,255).astype('uint8'),'L').filter(ImageFilter.MinFilter(3)).filter(ImageFilter.GaussianBlur(0.7))
    out=im.convert('RGBA'); out.putalpha(al); return out
def fit(name):
    cp=f'{H}/cut/{name}.png'
    if os.path.exists(cp): c=Image.open(cp).convert('RGBA')
    else: c=cut(Image.open(f'{H}/raw/{name}.png'))
    bb=c.getchannel('A').point(lambda v:255 if v>20 else 0).getbbox(); c=c.crop(bb)
    org=Image.open(f'{BAK}/{name}.png'); W,Hh=org.size; ob=org.getchannel('A').getbbox()
    bw,bh=ob[2]-ob[0],ob[3]-ob[1]; s=min(bw/c.width,bh/c.height)
    c=c.resize((max(1,round(c.width*s)),max(1,round(c.height*s))),Image.LANCZOS)
    can=Image.new('RGBA',(W,Hh),(0,0,0,0)); x=ob[0]+(bw-c.width)//2; y=ob[3]-c.height
    can.paste(c,(x,y),c); can.save(f'{H}/fit/{name}.png'); return name, (W,Hh), c.size
for n in sys.argv[1:]: print(fit(n))
