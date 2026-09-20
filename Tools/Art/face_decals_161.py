# 161차: 마을 얼굴 데칼(주인공 + NPC 6) — 첨부 레퍼런스(큰 갈색 눈·속눈썹·작은 미소·볼터치) 스타일을 PIL 로 그림. 살색 배경은 투명.
import os, math
from PIL import Image, ImageDraw, ImageFilter
OUT = r"C:\dev\game\Assets\Resources\CoastRun\Textures\Village"
S = 512
def ell(d, cx, cy, rx, ry, fill, outline=None, w=0):
    d.ellipse([cx-rx, cy-ry, cx+rx, cy+ry], fill=fill, outline=outline, width=w)
def soft(img, cx, cy, rx, ry, color, blur=18):
    layer = Image.new("RGBA", img.size, (0,0,0,0)); d = ImageDraw.Draw(layer)
    ell(d, cx, cy, rx, ry, color); layer = layer.filter(ImageFilter.GaussianBlur(blur))
    img.alpha_composite(layer)
def eye(img, cx, cy, r, iris, dark, lash=True, sparkle=2, sleepy=False):
    d = ImageDraw.Draw(img)
    # 흰자
    ell(d, cx, cy, r*1.06, r*1.12, (255,255,255,255))
    # 홍채(위가 진하고 아래가 밝은 갈색) + 동공
    ell(d, cx, cy+r*0.06, r*0.92, r*1.0, dark)
    ell(d, cx, cy+r*0.22, r*0.72, r*0.72, iris)
    ell(d, cx, cy+r*0.05, r*0.42, r*0.50, dark)
    # 하이라이트
    ell(d, cx-r*0.36, cy-r*0.36, r*0.30, r*0.30, (255,255,255,255))
    if sparkle >= 2: ell(d, cx+r*0.34, cy+r*0.30, r*0.14, r*0.14, (255,255,255,255))
    if sparkle >= 3: ell(d, cx-r*0.05, cy+r*0.55, r*0.08, r*0.08, (255,255,255,230))
    # 위 속눈썹 선(두꺼운 아치) + 바깥 속눈썹 2개
    if lash:
        lw = int(r*0.30)
        d.arc([cx-r*1.12, cy-r*1.22, cx+r*1.12, cy+r*1.0], start=195, end=345, fill=(45,25,15,255), width=lw)
        side = -1 if cx < S/2 else 1
        for k, ang in enumerate((-28, -8)):
            a = math.radians(ang); x0 = cx + side*r*1.02*math.cos(a*0.3); y0 = cy - r*0.78 + k*r*0.28
            d.line([(x0, y0), (x0 + side*r*0.42, y0 - r*0.30)], fill=(45,25,15,255), width=int(r*0.16))
    if sleepy:   # 위 눈꺼풀 살색 덮개
        ell(d, cx, cy-r*0.95, r*1.2, r*0.7, (255,220,190,255))
def mouth(img, cx, cy, w, kind, skin_dark=(120,60,40,255)):
    d = ImageDraw.Draw(img)
    if kind == "smile":     # 작은 다문 미소 ︶
        d.arc([cx-w, cy-w*0.9, cx+w, cy+w*0.5], start=15, end=165, fill=skin_dark, width=max(4, int(w*0.13)))   # 164차: 레퍼런스처럼 가는 선
    elif kind == "open":    # 벌린 미소(혀 없음)
        d.chord([cx-w, cy-w*0.55, cx+w, cy+w*0.9], start=0, end=180, fill=(200,70,80,255), outline=skin_dark, width=int(w*0.14))
        d.chord([cx-w*0.6, cy+w*0.25, cx+w*0.6, cy+w*0.85], start=0, end=180, fill=(240,120,130,255))
    elif kind == "grin":    # 씩 웃음(ω)
        d.arc([cx-w, cy-w*0.6, cx, cy+w*0.4], start=0, end=180, fill=skin_dark, width=int(w*0.2))
        d.arc([cx, cy-w*0.6, cx+w, cy+w*0.4], start=0, end=180, fill=skin_dark, width=int(w*0.2))
    elif kind == "line":    # 무표정
        d.line([(cx-w*0.7, cy), (cx+w*0.7, cy)], fill=skin_dark, width=int(w*0.2))
def face(name, iris=(150,88,40,255), dark=(58,30,16,255), eye_r=78, eye_y=210, eye_dx=138, mouth_kind="smile", mouth_y=330, blush=(255,120,130,150), sparkle=2, sleepy=False, nose=True, blush_pos=None, mouth_w=40):
    img = Image.new("RGBA", (S, S), (0,0,0,0))
    # 볼터치(먼저, 눈 밑)
    if blush:
        bx, by, brx, bry = blush_pos if blush_pos else (eye_dx+50, 305, 80, 48)
        soft(img, S/2-bx, by, brx, bry, blush, 20); soft(img, S/2+bx, by, brx, bry, blush, 20)
    eye(img, S/2-eye_dx, eye_y, eye_r, iris, dark, sparkle=sparkle, sleepy=sleepy)
    eye(img, S/2+eye_dx, eye_y, eye_r, iris, dark, sparkle=sparkle, sleepy=sleepy)
    d = ImageDraw.Draw(img)
    if nose: ell(d, S/2, 262, 9, 7, (238,150,120,230))
    mouth(img, S/2, mouth_y, mouth_w, mouth_kind)
    img.save(os.path.join(OUT, name + ".png")); print("saved", name)
if __name__ == "__main__":
    old = os.path.join(OUT, "UI_Face_Haneul.png"); bak = r"C:\dev\game\Tools\Art\_keybackup\UI_Face_Haneul_155.png"
    os.makedirs(os.path.dirname(bak), exist_ok=True)
    if os.path.exists(old) and not os.path.exists(bak): Image.open(old).save(bak)
    face("UI_Face_Haneul", eye_r=74, eye_dx=97, eye_y=222, mouth_y=326, mouth_w=48, blush=None, sparkle=2)   # 167차(사용자): 볼 분홍 없음   # 164차: 레퍼런스 실측 비율(눈 간격 face 폭의 37%·눈 지름 27%·볼 y·입 폭)
    face("UI_Face_Npc_Haenyeo", iris=(90,60,40,255), eye_r=62, mouth_kind="smile", sparkle=1, sleepy=True, blush=None)
    face("UI_Face_Npc_Keeper", iris=(80,55,35,255), eye_r=64, mouth_kind="smile", sparkle=1, blush=None)   # 167차: 무표정→미소
    face("UI_Face_Npc_Florist", iris=(160,95,45,255), eye_r=78, mouth_kind="open", sparkle=3, blush=None)
    face("UI_Face_Npc_FisherBoy", iris=(120,75,35,255), eye_r=74, mouth_kind="grin", sparkle=2, blush=None)
    face("UI_Face_Npc_Cafe", iris=(70,110,190,255), dark=(40,55,110,255), eye_r=74, mouth_kind="smile", sparkle=3, blush=None)
    face("UI_Face_Npc_Surfer", iris=(60,45,35,255), eye_r=70, mouth_kind="grin", sparkle=2, blush=None)
