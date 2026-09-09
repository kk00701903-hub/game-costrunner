"""K-POP 러닝모드 바 v2 — 사용자 시안(파스텔 라벤더→핑크 유리 바, 라인아트 네온 헤드폰, 시안→라벤더 글자, 파형 이퀄라이저, 흩어진 음표)."""
import math, random
from PIL import Image, ImageDraw, ImageFilter, ImageFont, ImageChops

W, H = 1320, 260
BX0, BY0, BX1, BY1 = 34, 34, 1286, 226
R = (BY1 - BY0) // 2 - 22          # 시안은 모서리가 완전 반원이 아니라 큰 라운드
random.seed(11)
FONT = '/usr/share/fonts/opentype/noto/NotoSansCJK-Black.ttc'

def lerp(a, b, t): return tuple(int(round(a[i] + (b[i] - a[i]) * t)) for i in range(len(a)))
def multi(stops, t):
    for i in range(len(stops) - 1):
        t0, c0 = stops[i]; t1, c1 = stops[i + 1]
        if t <= t1: return lerp(c0, c1, (t - t0) / max(1e-6, t1 - t0))
    return stops[-1][1]

def hgrad(size, stops, alpha=255):
    w, h = size
    img = Image.new('RGBA', size); px = img.load()
    row = [multi(stops, x / (w - 1)) for x in range(w)]
    for y in range(h):
        for x in range(w):
            c = row[x]; px[x, y] = (c[0], c[1], c[2], alpha)
    return img

def rr_mask(size, box, r):
    m = Image.new('L', size, 0); ImageDraw.Draw(m).rounded_rectangle(box, r, fill=255); return m

def glow_layer(draw_fn, blur, color):
    l = Image.new('L', (W, H), 0); draw_fn(ImageDraw.Draw(l))
    l = l.filter(ImageFilter.GaussianBlur(blur))
    g = Image.new('RGBA', (W, H), color + (255,)); g.putalpha(l); return g

def text_layer(text, font, xy, fill_stops=None, fill=None, stroke=0, stroke_fill=None):
    m = Image.new('L', (W, H), 0)
    ImageDraw.Draw(m).text(xy, text, font=font, fill=255, stroke_width=stroke, stroke_fill=255 if stroke else None)
    if fill_stops:
        bbox = ImageDraw.Draw(m).textbbox(xy, text, font=font)
        g = Image.new('RGBA', (W, H)); gp = g.load()
        for y in range(bbox[1] - 4, bbox[3] + 4):
            t = min(1, max(0, (y - bbox[1]) / max(1, bbox[3] - bbox[1])))
            c = multi(fill_stops, t)
            for x in range(bbox[0] - 8, bbox[2] + 8): gp[x, y] = (c[0], c[1], c[2], 255)
    else:
        g = Image.new('RGBA', (W, H), fill + (255,))
    g.putalpha(m); return g

canvas = Image.new('RGBA', (W, H), (0, 0, 0, 0))

# 1. 바깥 글로우: 보라 → 핑크
canvas.alpha_composite(glow_layer(lambda d: d.rounded_rectangle((BX0 - 4, BY0 - 4, BX1 + 4, BY1 + 4), R + 4, fill=255), 16, (150, 90, 255)))
canvas.alpha_composite(glow_layer(lambda d: d.rounded_rectangle((BX0, BY0, BX1, BY1), R, fill=255), 6, (255, 120, 220)))

# 2. 본체: 라벤더 → 연보라 → 핑크 (가로), 위쪽 살짝 밝게
body = hgrad((BX1 - BX0, BY1 - BY0), [(0, (178, 170, 255)), (0.4, (198, 160, 250)), (0.72, (240, 155, 232)), (1, (255, 170, 225))], 185)
top = Image.new('RGBA', body.size, (255, 255, 255, 0)); tp = top.load()
for y in range(body.size[1]):
    a = int(70 * max(0, 1 - y / (body.size[1] * 0.55)))
    for x in range(body.size[0]): tp[x, y] = (255, 255, 255, a)
body = Image.alpha_composite(body, top)
# 유리 안쪽 어두운 보라 심(가운데 살짝 진하게 → 글자가 뜨게)
core = Image.new('RGBA', body.size, (0, 0, 0, 0)); cp = core.load()
for y in range(body.size[1]):
    for x in range(body.size[0]):
        cx = x / body.size[0]; cy = y / body.size[1]
        a = int(55 * max(0, 1 - ((cx - 0.5) / 0.42) ** 2) * max(0, 1 - ((cy - 0.55) / 0.5) ** 2))
        cp[x, y] = (110, 70, 190, int(a * 0.8))
body = Image.alpha_composite(body, core)
bm = rr_mask(body.size, (0, 0, body.size[0] - 1, body.size[1] - 1), R)
body.putalpha(ImageChops.multiply(body.getchannel('A'), bm))
canvas.alpha_composite(body, (BX0, BY0))

# 3. 테두리: 흰-핑크 얇은 선 + 안쪽 연한 선
rim = Image.new('RGBA', (W, H), (0, 0, 0, 0)); rd = ImageDraw.Draw(rim)
rd.rounded_rectangle((BX0, BY0, BX1, BY1), R, outline=(255, 230, 250, 255), width=4)
rd.rounded_rectangle((BX0 + 6, BY0 + 6, BX1 - 6, BY1 - 6), R - 6, outline=(255, 255, 255, 70), width=2)
canvas.alpha_composite(rim.filter(ImageFilter.GaussianBlur(0.8)))

# 4. 헤드폰(라인아트 네온): 헤드밴드 아크 + 이어컵 둘
cx, cy = BX0 + 118, (BY0 + BY1) // 2 + 8
K = 1.28
def headphones(d, w_band=12, w_cup=7, col=255, pad=0):
    d.arc((cx - 64*K - pad, cy - 70*K - pad, cx + 64*K + pad, cy + 38*K + pad), 182, 358, fill=col, width=int(w_band * K) + 2 * pad)
    for sx in (-1, 1):
        x0 = cx + sx * 60 * K
        d.rounded_rectangle((x0 - 20*K - pad, cy - 14*K - pad, x0 + 20*K + pad, cy + 34*K + pad), int(12*K) + pad, outline=col, width=int(w_cup * K) + 2 * pad)
        d.rounded_rectangle((x0 - 11*K - pad, cy - 4*K - pad, x0 + 11*K + pad, cy + 24*K + pad), int(8*K), fill=col)
hp_glow = glow_layer(lambda d: headphones(d, pad=4), 10, (90, 200, 255))
canvas.alpha_composite(hp_glow)
# 진보라 외곽선(대비)
hp_o = Image.new('L', (W, H), 0); headphones(ImageDraw.Draw(hp_o), pad=3)
ho = Image.new('RGBA', (W, H), (95, 50, 170, 255)); ho.putalpha(hp_o); canvas.alpha_composite(ho)
hp = Image.new('L', (W, H), 0); headphones(ImageDraw.Draw(hp))
hp_col = hgrad((W, H), [(0, (120, 245, 255)), (0.10, (150, 240, 255)), (0.16, (200, 245, 255)), (1, (225, 235, 255))])
hp_col.putalpha(hp); canvas.alpha_composite(hp_col)
# 이어컵 안쪽 연한 채움
cups = Image.new('L', (W, H), 0); cd = ImageDraw.Draw(cups)
for sx in (-1, 1):
    x0 = cx + sx * 60 * K; cd.rounded_rectangle((x0 - 20*K, cy - 14*K, x0 + 20*K, cy + 34*K), int(12*K), fill=90)
cf = Image.new('RGBA', (W, H), (150, 235, 255, 255)); cf.putalpha(cups); canvas.alpha_composite(cf)

# 5. 글자: "K-POP"(시안→라벤더) + "러닝모드"(흰→라벤더), 글로우
f1 = ImageFont.truetype(FONT, 92); f2 = ImageFont.truetype(FONT, 88)
t1, t2 = 'K-POP', ' 러닝모드'
tmp = ImageDraw.Draw(canvas)
w1 = tmp.textbbox((0, 0), t1, font=f1)[2]; w2 = tmp.textbbox((0, 0), t2, font=f2)[2]
tx = BX0 + 272; ty = (BY0 + BY1) // 2 - 64
canvas.alpha_composite(glow_layer(lambda d: (d.text((tx, ty), t1, font=f1, fill=255), d.text((tx + w1, ty + 2), t2, font=f2, fill=255)), 12, (120, 150, 255)))
canvas.alpha_composite(text_layer(t1, f1, (tx, ty), fill=(110, 60, 190), stroke=3))
canvas.alpha_composite(text_layer(t2, f2, (tx + w1, ty + 2), fill=(110, 60, 190), stroke=3))
canvas.alpha_composite(text_layer(t1, f1, (tx, ty), fill_stops=[(0, (235, 255, 255)), (0.45, (140, 230, 255)), (1, (200, 160, 255))]))
canvas.alpha_composite(text_layer(t2, f2, (tx + w1, ty + 2), fill_stops=[(0, (255, 255, 255)), (0.6, (240, 235, 255)), (1, (215, 185, 255))]))

# 6. 파형 이퀄라이저(가운데 높고 양끝 낮게, 시안→흰→핑크)
ex0 = tx + w1 + w2 + 34
n = 25; base_cy = (BY0 + BY1) // 2
eq = Image.new('RGBA', (W, H), (0, 0, 0, 0)); ed = ImageDraw.Draw(eq)
heights = [12, 22, 38, 28, 56, 44, 78, 62, 98, 80, 118, 92, 130, 96, 116, 76, 100, 60, 84, 46, 62, 30, 40, 20, 12]
for i in range(n):
    x = ex0 + i * 9; h = heights[i]
    c = multi([(0, (130, 235, 255)), (0.5, (255, 255, 255)), (1, (255, 150, 230))], i / (n - 1))
    ed.rounded_rectangle((x, base_cy - h // 2, x + 5, base_cy + h // 2), 2, fill=c + (255,))
canvas.alpha_composite(glow_layer(lambda d: [d.rounded_rectangle((ex0 + i * 9 - 1, base_cy - heights[i] // 2 - 1, ex0 + i * 9 + 6, base_cy + heights[i] // 2 + 1), 3, fill=255) for i in range(n)], 7, (170, 210, 255)))
canvas.alpha_composite(eq)

# 7. 음표들: 바 안 오른쪽에 작은 것 셋 + 바깥 큰 것 하나
nf = lambda s: ImageFont.truetype(FONT, s)
notes = [((BX0 + 470, BY0 + 6), 30, (255, 255, 255)), ((ex0 - 70, BY0 - 2), 34, (245, 235, 255)),
         ((ex0 + 225, BY0 + 8), 44, (255, 255, 255)), ((ex0 + 170, BY1 - 44), 30, (255, 200, 245)),
         ((BX1 - 34, BY0 + 10), 62, (255, 215, 248)), ((BX1 - 8, BY1 - 70), 40, (255, 200, 245)), ((ex0 + 250, BY0 - 14), 36, (235, 225, 255))]
for (pos, size, col) in notes:
    canvas.alpha_composite(glow_layer(lambda d, p=pos, s=size: d.text(p, '♪', font=nf(s), fill=255), 6, (255, 150, 240)))
    canvas.alpha_composite(text_layer('♪', nf(size), pos, fill=col))

# 8. 반짝이: 4각 별 + 점
def star(d, x, y, r, col):
    d.polygon([(x, y - r), (x + r * .3, y - r * .3), (x + r, y), (x + r * .3, y + r * .3), (x, y + r), (x - r * .3, y + r * .3), (x - r, y), (x - r * .3, y - r * .3)], fill=col)
sp = Image.new('RGBA', (W, H), (0, 0, 0, 0)); sd = ImageDraw.Draw(sp)
for (x, y, r) in [(BX0 + 12, BY0 + 6, 14), (BX1 - 8, BY1 - 2, 14), (tx + w1 + 40, BY0 + 8, 9), (BX0 + 320, BY1 - 10, 8), (ex0 + 110, BY1 + 4, 7), (BX0 + 60, BY1 + 6, 6)]:
    star(sd, x, y, r, (255, 255, 255, 235))
for _ in range(60):
    x = random.randint(BX0, BX1); y = random.randint(BY0 - 10, BY1 + 10); r = random.choice([1, 1, 1, 2, 2, 3])
    col = random.choice([(255, 255, 255, 210), (200, 240, 255, 200), (255, 190, 245, 200)])
    sd.ellipse((x - r, y - r, x + r, y + r), fill=col)
canvas.alpha_composite(sp.filter(ImageFilter.GaussianBlur(0.5)))

canvas.save('/mnt/user-data/outputs/UI_KpopBar.png')
prev = Image.new('RGBA', (W, H), (40, 30, 40, 255)); prev.alpha_composite(canvas)
prev.save('/tmp/claude-0/-home-claude/14515ed5-23ea-53a1-9a81-669202515295/scratchpad/kpopbar_prev2.png')
print('ok')
