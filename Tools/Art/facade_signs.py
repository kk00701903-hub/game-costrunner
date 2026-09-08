# 16차: Kling 파사드의 엉망 한글 간판을 지우고 Pretendard로 진짜 상호를 그린다.
# 원본: Tools/Art/_facade_src/Tex_Facade_K.png → 출력: Assets/Resources/CoastRun/Tex_Facade_K.png
from PIL import Image, ImageDraw, ImageFont
import os, sys
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SRC = os.path.join(ROOT, 'Tools/Art/_facade_src')
DST = os.path.join(ROOT, 'Assets/Resources/CoastRun')
FONT = os.path.join(ROOT, 'Assets/Resources/CoastRun/Fonts/Pretendard-ExtraBold.ttf')
# key: (text, board rect x0,y0,x1,y1 (fractions, y from top), board colour or None(=sample wall), text colour, corner radius frac)
SIGNS = {
 'M': ('해녀횟집',   (0.02,0.375,0.70,0.515), '#E23C3C', '#FFFFFF', 0.35),
 'N': ('바다민박',   (0.13,0.585,0.87,0.735), '#F7F1E4', '#1E1A18', 0.30),
 'O': ('카페',       (0.15,0.40,0.62,0.505), '#C9905A', '#3A2412', 0.25),
 'P': ('흑돼지',     (0.34,0.325,0.76,0.435), '#C9905A', '#3A2412', 0.25),
 'Q': ('감귤판매',   (0.14,0.24,0.46,0.44),   '#FAF4E4', '#2A2420', 0.30),
 'R': ('펜션',       (0.52,0.53,0.80,0.65),   '#FAF6EA', '#2A2420', 0.30),
 'S': ('편의점',     (0.0,0.50,0.66,0.73),   None,      '#FFFFFF', 0.0),
 'T': ('청과물',     (0.25,0.425,0.75,0.54),   '#FFFBEF', '#2A2420', 0.30),
 'U': ('게스트하우스',(0.03,0.40,0.47,0.52),  '#F0607F', '#FFFFFF', 0.30),
 'V': ('분식',       (0.63,0.44,0.99,0.55),   '#F0607F', '#FFFFFF', 0.30),
 'W': ('기념품',     (0.64,0.16,0.86,0.32),   '#FAF6EA', '#2A2420', 0.30),
 'X': ('서핑샵',     (0.55,0.51,0.95,0.60),   '#FFFBEF', '#2A2420', 0.30),
}
PATCHES = { 'S': [((0.85,0.895,1.0,0.975),(0.80,0.93))], 'W': [((0.0,0.0,0.075,0.33),(0.12,0.20))] }  # (rect, sample point)
def hexc(h): return tuple(int(h[i:i+2],16) for i in (1,3,5))
def sample(im, x, y):
    px = im.crop((x-6, y-6, x+6, y+6)).resize((1,1)); return px.getpixel((0,0))
def run(keys):
    for k in keys:
        text, (x0,y0,x1,y1), board, tcol, rad = SIGNS[k]
        im = Image.open(os.path.join(SRC, f'Tex_Facade_{k}.png')).convert('RGB')
        W, H = im.size
        bx0, by0, bx1, by1 = int(x0*W), int(y0*H), int(x1*W), int(y1*H)
        d = ImageDraw.Draw(im)
        for (px0,py0,px1,py1),(sx,sy) in PATCHES.get(k, []):
            d.rectangle((int(px0*W),int(py0*H),int(px1*W),int(py1*H)), fill=sample(im,int(sx*W),int(sy*H)))
        bh = by1 - by0
        if board is None:
            c = sample(im, int((x1*W + 30)), int((y0+y1)*0.5*H))
            d.rectangle((bx0, by0, bx1, by1), fill=c)
        else:
            c = hexc(board)
            d.rounded_rectangle((bx0, by0, bx1, by1), radius=int(bh*rad), fill=c)
            # 얇은 진한 테두리(만화 윤곽)
            d.rounded_rectangle((bx0, by0, bx1, by1), radius=int(bh*rad), outline=(40,32,30), width=max(2, bh//28))
        # 글자: 보드 안에 맞춤
        size = int(bh*0.68)
        f = ImageFont.truetype(FONT, size)
        while f.getlength(text) > (bx1-bx0)*0.86 and size > 8:
            size -= 2; f = ImageFont.truetype(FONT, size)
        tw = f.getlength(text); bb = f.getbbox(text)
        tx = (bx0+bx1)/2 - tw/2; ty = (by0+by1)/2 - (bb[1]+bb[3])/2
        if board is None:   # 벽에 직접 쓰는 흰 글자: 그림자 + 흰
            d.text((tx+3, ty+3), text, font=f, fill=(120,60,50))
        d.text((tx, ty), text, font=f, fill=hexc(tcol))
        im.save(os.path.join(DST, f'Tex_Facade_{k}.png'), optimize=True)
        print(k, text, size)
if __name__ == '__main__':
    run(sys.argv[1:] or list(SIGNS))
