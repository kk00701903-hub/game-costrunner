# 171차: 농장 안내 팝업 그림 4장(닭·토끼·달걀·고기) — 클링이 비회원 제출을 막아 둔 시간이라 PIL 로 임시 제작(나중에 클링으로 교체 가능)
#   → Assets/Resources/CoastRun/Textures/Village/UI_Farm_{Chicken,Rabbit,Egg,Meat}.png (256, 투명 배경, 굵은 윤곽)
from PIL import Image, ImageDraw
import os
D = r"C:\dev\game\Assets\Resources\CoastRun\Textures\Village"; os.makedirs(D, exist_ok=True)
INK = (40, 30, 35, 255)
def canvas(): return Image.new("RGBA", (512, 512), (0, 0, 0, 0))
def save(im, n): im.resize((256, 256), Image.LANCZOS).save(os.path.join(D, "UI_Farm_%s.png" % n)); print("saved", n)
def ell(d, box, fill, w=14): d.ellipse(box, fill=fill, outline=INK, width=w)

# 닭
im = canvas(); d = ImageDraw.Draw(im)
ell(d, (110, 190, 400, 420), (250, 247, 240, 255))                     # 몸
d.polygon([(380, 240), (450, 190), (430, 300)], fill=(240, 236, 226, 255), outline=INK, width=12)   # 꼬리
ell(d, (120, 120, 260, 260), (250, 247, 240, 255))                     # 머리
d.polygon([(150, 130), (175, 85), (200, 125), (225, 85), (245, 130)], fill=(225, 55, 60, 255), outline=INK, width=10)  # 볏
d.polygon([(118, 200), (60, 215), (118, 232)], fill=(245, 180, 50, 255), outline=INK, width=10)   # 부리
ell(d, (135, 230, 175, 275), (225, 55, 60, 255), 8)                     # 턱볏
ell(d, (165, 165, 195, 195), INK, 0)                                    # 눈
ell(d, (172, 170, 184, 182), (255, 255, 255, 255), 0)
d.polygon([(190, 300), (300, 260), (330, 340), (220, 370)], fill=(232, 226, 214, 255), outline=INK, width=10)  # 날개
for x in (230, 300): d.line([(x, 415), (x, 470)], fill=(245, 180, 50, 255), width=16); d.line([(x - 30, 480), (x + 30, 480)], fill=(245, 180, 50, 255), width=14)
save(im, "Chicken")

# 토끼
im = canvas(); d = ImageDraw.Draw(im)
for x in (185, 275): ell(d, (x, 40, x + 70, 250), (190, 185, 190, 255)); ell(d, (x + 18, 70, x + 52, 225), (245, 190, 200, 255), 6)   # 귀
ell(d, (110, 250, 400, 470), (200, 195, 200, 255))                     # 몸
ell(d, (140, 180, 320, 340), (205, 200, 205, 255))                     # 머리
ell(d, (185, 235, 215, 265), INK, 0); ell(d, (255, 235, 285, 265), INK, 0)    # 눈
ell(d, (192, 240, 202, 250), (255, 255, 255, 255), 0); ell(d, (262, 240, 272, 250), (255, 255, 255, 255), 0)
ell(d, (218, 275, 250, 298), (245, 160, 175, 255), 6)                  # 코
d.line([(234, 296), (234, 318)], fill=INK, width=8); d.arc((205, 300, 262, 340), 0, 180, fill=INK, width=8)   # 입
ell(d, (350, 330, 430, 410), (250, 247, 240, 255))                     # 꼬리
ell(d, (140, 400, 220, 460), (215, 210, 215, 255)); ell(d, (290, 400, 370, 460), (215, 210, 215, 255))     # 발
save(im, "Rabbit")

# 달걀(둥지 + 3개)
im = canvas(); d = ImageDraw.Draw(im)
ell(d, (60, 300, 452, 470), (200, 160, 90, 255))                       # 둥지
for i in range(14):
    x0 = 70 + i * 27; d.line([(x0, 330 + (i % 3) * 20), (x0 + 60, 420 - (i % 2) * 30)], fill=(150, 110, 55, 255), width=8)
for (cx, cy, s) in ((160, 250, 1.0), (300, 230, 1.05), (235, 300, 0.95)):
    w, h = int(95 * s), int(125 * s); ell(d, (cx - w // 2, cy - h // 2, cx + w // 2, cy + h // 2), (252, 248, 236, 255), 12)
    ell(d, (cx - w // 4, cy - h // 3, cx - w // 4 + 22, cy - h // 3 + 30), (255, 255, 255, 255), 0)
save(im, "Egg")

# 고기(뼈 붙은 살코기)
im = canvas(); d = ImageDraw.Draw(im)
d.line([(120, 400), (390, 140)], fill=(250, 248, 240, 255), width=44); d.line([(120, 400), (390, 140)], fill=INK, width=60)
d.line([(124, 396), (386, 144)], fill=(250, 248, 240, 255), width=40)  # 뼈
for (x, y) in ((110, 410), (400, 130)):
    ell(d, (x - 40, y - 30, x + 10, y + 20), (250, 248, 240, 255), 10); ell(d, (x - 10, y - 45, x + 40, y + 5), (250, 248, 240, 255), 10)
ell(d, (130, 130, 420, 400), (240, 110, 120, 255))                     # 살
ell(d, (170, 170, 380, 360), (250, 150, 155, 255), 0)
d.ellipse((215, 215, 335, 320), fill=(240, 110, 120, 255))
ell(d, (250, 200, 300, 250), (255, 235, 235, 255), 0)                  # 마블링
save(im, "Meat")
print("done")
