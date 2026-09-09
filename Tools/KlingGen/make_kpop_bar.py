"""K-POP 러닝모드 바 합성: 네온 글라스 바 + 클링 헤드폰 아이콘 + 그라데이션 텍스트 + 이퀄라이저 + 음표 + 반짝이."""
import math, random
from PIL import Image, ImageDraw, ImageFilter, ImageFont, ImageChops

W, H = 1320, 220
BX0, BY0, BX1, BY1 = 40, 35, 1280, 185      # 바 본체
R = (BY1 - BY0) // 2
random.seed(7)
FONT = '/usr/share/fonts/opentype/noto/NotoSansCJK-Black.ttc'
SRC = '/mnt/user-data/uploads/game/Tools/KlingGen/out/kpop_btn/'

def lerp(a, b, t): return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(len(a)))

def rrect_mask(size, box, r):
    m = Image.new('L', size, 0)
    ImageDraw.Draw(m).rounded_rectangle(box, r, fill=255)
    return m

def gradient(size, c0, c1, horizontal=False, c2=None):
    w, h = size
    img = Image.new('RGBA', size)
    px = img.load()
    for y in range(h):
        for x in range(w):
            t = x / (w - 1) if horizontal else y / (h - 1)
            if c2 is not None:
                c = lerp(c0, c1, t * 2) if t < 0.5 else lerp(c1, c2, (t - 0.5) * 2)
            else:
                c = lerp(c0, c1, t)
            px[x, y] = c
    return img

canvas = Image.new('RGBA', (W, H), (0, 0, 0, 0))

# 1. 바깥 글로우(마젠타 + 시안 두 겹)
glow = Image.new('RGBA', (W, H), (0, 0, 0, 0))
gd = ImageDraw.Draw(glow)
gd.rounded_rectangle((BX0 - 6, BY0 - 6, BX1 + 6, BY1 + 6), R + 6, fill=(255, 70, 200, 200))
glow = glow.filter(ImageFilter.GaussianBlur(18))
canvas.alpha_composite(glow)
glow2 = Image.new('RGBA', (W, H), (0, 0, 0, 0))
ImageDraw.Draw(glow2).rounded_rectangle((BX0, BY0, BX1, BY1), R, fill=(120, 60, 255, 160))
canvas.alpha_composite(glow2.filter(ImageFilter.GaussianBlur(10)))

# 2. 본체: 세로 그라데이션(위 밝은 보라 → 아래 진보라) × 가로 핑크 틴트
body = gradient((BX1 - BX0, BY1 - BY0), (110, 40, 190, 245), (60, 18, 120, 245), False)
tint = gradient((BX1 - BX0, BY1 - BY0), (255, 40, 160, 90), (90, 20, 200, 0), True, (255, 60, 180, 60))
body = Image.alpha_composite(body, tint)
bmask = rrect_mask(body.size, (0, 0, body.size[0] - 1, body.size[1] - 1), R)
body.putalpha(ImageChops.multiply(body.getchannel('A'), bmask))
canvas.alpha_composite(body, (BX0, BY0))

# 3. 유리 하이라이트(위쪽 반)
hl = Image.new('RGBA', body.size, (0, 0, 0, 0))
hd = ImageDraw.Draw(hl)
hd.rounded_rectangle((8, 6, body.size[0] - 9, body.size[1] // 2 + 4), R - 6, fill=(255, 255, 255, 70))
hl = hl.filter(ImageFilter.GaussianBlur(3))
hl.putalpha(ImageChops.multiply(hl.getchannel('A'), bmask))
canvas.alpha_composite(hl, (BX0, BY0))

# 4. 네온 테두리(핑크→시안 가로 그라데이션 스트로크)
rim = Image.new('RGBA', (W, H), (0, 0, 0, 0))
rd = ImageDraw.Draw(rim)
rd.rounded_rectangle((BX0, BY0, BX1, BY1), R, outline=(255, 255, 255, 255), width=5)
rimcol = gradient((W, H), (255, 90, 220, 255), (120, 230, 255, 255), True, (255, 120, 240, 255))
rimcol.putalpha(rim.getchannel('A'))
canvas.alpha_composite(rimcol.filter(ImageFilter.GaussianBlur(1.2)))
canvas.alpha_composite(rimcol)

# 5. 헤드폰 아이콘(클링, 검정 배경 → 밝기 알파)
hp = Image.open(SRC + 'headphones.png').convert('RGBA').crop((110, 60, 940, 760))
px = hp.load()
for y in range(hp.size[1]):
    for x in range(hp.size[0]):
        r, g, b, a = px[x, y]
        lum = max(r, g, b)
        a = min(255, int(lum * 1.6)) if lum > 18 else 0
        px[x, y] = (r, g, b, a)
hp = hp.resize((150, int(150 * hp.size[1] / hp.size[0])), Image.LANCZOS)
hglow = Image.new('RGBA', (hp.size[0] + 60, hp.size[1] + 60), (0, 0, 0, 0))
hg = Image.new('RGBA', hp.size, (255, 90, 210, 255)); hg.putalpha(hp.getchannel('A'))
hglow.alpha_composite(hg, (30, 30)); hglow = hglow.filter(ImageFilter.GaussianBlur(12))
hx, hy = BX0 + 30, (BY0 + BY1) // 2 - hp.size[1] // 2 - 2
canvas.alpha_composite(hglow, (hx - 30, hy - 30))
canvas.alpha_composite(hp, (hx, hy))

# 6. 텍스트 "K-POP 러닝모드" — 흰→핑크 그라데이션 + 마젠타 글로우 + 얇은 진보라 외곽선
font = ImageFont.truetype(FONT, 78)
text = 'K-POP 러닝모드'
tw, th = ImageDraw.Draw(canvas).textbbox((0, 0), text, font=font)[2:]
tx = BX0 + 230
ty = (BY0 + BY1) // 2 - th // 2 - 10
tmask = Image.new('L', (W, H), 0)
ImageDraw.Draw(tmask).text((tx, ty), text, font=font, fill=255)
tglow = Image.new('RGBA', (W, H), (255, 60, 200, 0)); tglow.putalpha(tmask)
canvas.alpha_composite(tglow.filter(ImageFilter.GaussianBlur(10)))
outline = Image.new('L', (W, H), 0)
ImageDraw.Draw(outline).text((tx, ty), text, font=font, fill=255, stroke_width=5, stroke_fill=255)
ol = Image.new('RGBA', (W, H), (70, 10, 110, 255)); ol.putalpha(outline)
canvas.alpha_composite(ol)
tgrad = gradient((W, H), (255, 255, 255, 255), (255, 200, 240, 255), False, (255, 120, 220, 255))
# 텍스트 높이 구간에만 그라데이션이 걸리도록 y 재매핑
tg = Image.new('RGBA', (W, H)); tgp = tg.load(); src = tgrad.load()
for y in range(H):
    t = min(1, max(0, (y - ty - 8) / max(1, th - 8)))
    yy = int(t * (H - 1))
    for x in range(tx - 10, tx + tw + 10):
        tgp[x, y] = src[x, yy]
tg.putalpha(tmask)
canvas.alpha_composite(tg)

# 7. 이퀄라이저 바(텍스트 오른쪽)
ex = tx + tw + 40
eh = [28, 52, 40, 70, 46, 60, 34, 56, 42]
ed = ImageDraw.Draw(canvas)
cy = (BY0 + BY1) // 2
for i, h in enumerate(eh):
    x = ex + i * 16
    col = lerp((255, 90, 210, 255), (110, 230, 255, 255), i / (len(eh) - 1))
    ed.rounded_rectangle((x, cy - h // 2, x + 9, cy + h // 2), 4, fill=col)
eq_glow = Image.new('RGBA', (W, H), (0, 0, 0, 0))
egd = ImageDraw.Draw(eq_glow)
for i, h in enumerate(eh):
    x = ex + i * 16
    egd.rounded_rectangle((x - 2, cy - h // 2 - 2, x + 11, cy + h // 2 + 2), 5, fill=(255, 120, 230, 180))
canvas.alpha_composite(eq_glow.filter(ImageFilter.GaussianBlur(8)))

# 8. 음표(폰트 글리프, 네온 핑크)
nfont = ImageFont.truetype(FONT, 88)
nx = BX1 - 120; ny = cy - 58
nm = Image.new('L', (W, H), 0)
ImageDraw.Draw(nm).text((nx, ny), '♪', font=nfont, fill=255)
ng = Image.new('RGBA', (W, H), (255, 60, 200, 255)); ng.putalpha(nm)
canvas.alpha_composite(ng.filter(ImageFilter.GaussianBlur(9)))
nfill = Image.new('RGBA', (W, H), (255, 150, 230, 255)); nfill.putalpha(nm)
canvas.alpha_composite(nfill)
nin = Image.new('L', (W, H), 0)
ImageDraw.Draw(nin).text((nx + 3, ny + 3), '♪', font=ImageFont.truetype(FONT, 78), fill=255)
nfill2 = Image.new('RGBA', (W, H), (255, 240, 250, 255)); nfill2.putalpha(nin)
canvas.alpha_composite(nfill2)

# 9. 반짝이(4각 별) + 입자
def star(d, cx, cy, r, col):
    d.polygon([(cx, cy - r), (cx + r * 0.28, cy - r * 0.28), (cx + r, cy), (cx + r * 0.28, cy + r * 0.28),
               (cx, cy + r), (cx - r * 0.28, cy + r * 0.28), (cx - r, cy), (cx - r * 0.28, cy - r * 0.28)], fill=col)
sp = Image.new('RGBA', (W, H), (0, 0, 0, 0)); sd = ImageDraw.Draw(sp)
for (cx, cy2, r, col) in [(BX0 + 12, BY0 + 8, 16, (255, 255, 255, 240)), (BX1 - 20, BY1 - 4, 18, (255, 255, 255, 240)),
                          (BX0 + 200, BY1 + 10, 10, (255, 170, 240, 230)), (BX1 - 210, BY0 - 6, 11, (170, 240, 255, 230)),
                          (tx + tw // 2, BY0 - 4, 8, (255, 255, 255, 200)), (BX1 - 60, BY0 + 14, 7, (255, 200, 250, 220))]:
    star(sd, cx, cy2, r, col)
for _ in range(40):
    x = random.randint(BX0 - 20, BX1 + 20); y = random.randint(BY0 - 20, BY1 + 20)
    r = random.choice([1, 1, 2, 2, 3])
    col = random.choice([(255, 255, 255, 200), (255, 150, 240, 200), (150, 230, 255, 200)])
    sd.ellipse((x - r, y - r, x + r, y + r), fill=col)
canvas.alpha_composite(sp.filter(ImageFilter.GaussianBlur(0.6)))

canvas.save('/mnt/user-data/outputs/UI_KpopBar.png')
# 미리보기(검정 위)
prev = Image.new('RGBA', (W, H), (20, 20, 30, 255)); prev.alpha_composite(canvas)
prev.save('/tmp/claude-0/-home-claude/14515ed5-23ea-53a1-9a81-669202515295/scratchpad/kpopbar_prev.png')
print('ok', canvas.size)
