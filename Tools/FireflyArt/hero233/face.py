# 235차: 동그란 얼굴 아이콘(UI_Face_Girl/Ring)을 새 하늘 얼굴로 — 원래 테두리 고리는 그대로 두고 안쪽만 바꾼다
import numpy as np, sys, os
from PIL import Image
G=os.path.expanduser('~/mnt/game'); H=G+'/Tools/FireflyArt/hero233'; BAK=G+'/Tools/_xfer/bak219/imp235'
src=Image.open(H+'/raw/UI_Face_Girl_src.png').convert('RGB')
a=np.asarray(src).astype(int); white=(a.min(2)>238)
for name in sys.argv[1:]:
    org=Image.open(f'{BAK}/{name}.png').convert('RGBA'); W,Hh=org.size; o=np.asarray(org).astype(int)
    al=o[...,3]; ys,xs=np.nonzero(al>128); cx,cy=(xs.min()+xs.max())/2,(ys.min()+ys.max())/2; R=min(xs.max()-xs.min(),ys.max()-ys.min())/2
    # 테두리 두께: 가운데 가로줄에서 바깥부터 어두운 픽셀이 끝나는 곳
    row=o[int(cy)]; lum=row[...,:3].mean(1); x0=int(cx-R); t=0
    while t<R*0.3 and (lum[x0+t]<110 or row[x0+t,3]<128): t+=1
    rin=R-t-1
    # 안쪽 배경색: 원래 안쪽 가장자리 근처 밝은 픽셀 평균
    yy,xx=np.mgrid[0:Hh,0:W]; d=np.hypot(xx-cx,yy-cy)
    band=(d>rin*0.85)&(d<rin*0.97)&(o[...,:3].mean(2)>150); bgc=o[band][:,:3].mean(0) if band.sum()>20 else np.array([250,232,200])
    p=a.copy(); p[white]=bgc  # 흰 배경 → 원래 안쪽 색
    pim=Image.fromarray(p.astype('uint8'))
    # 얼굴이 원 안에 오게: 원본 위쪽 8%~92% 정사각 크롭
    S=pim.width; crop=pim.crop((int(S*0.08),int(S*0.04),int(S*0.92),int(S*0.88))).resize((int(2*rin)+2,int(2*rin)+2),Image.LANCZOS)
    out=o.copy().astype(float); c=np.asarray(crop).astype(float)
    ox,oy=int(round(cx-rin)),int(round(cy-rin))
    inner=d<=rin
    for y in range(max(0,oy),min(Hh,oy+c.shape[0])):
        for x in range(max(0,ox),min(W,ox+c.shape[1])):
            if inner[y,x]:
                w=min(1.0,(rin-d[y,x])/1.5)  # 경계 1.5px 부드럽게
                out[y,x,:3]=c[y-oy,x-ox]*w+out[y,x,:3]*(1-w); out[y,x,3]=max(out[y,x,3],255*w)
    Image.fromarray(out.clip(0,255).astype('uint8'),'RGBA').save(f'{H}/fit/{name}.png'); print(name,(W,Hh),'R',R,'ring',t,'bg',bgc.astype(int))
