"""22차-9: 커플 사진 → 2:3 포토카드(600×900). 폴라로이드(크림 종이 테두리)면 안쪽 사진만 잘라 쓰고,
사진을 세로 4:5로 가운데 크롭한 뒤 크림 카드 위에 캡션·하트를 얹는다(샘플 폴라로이드와 같은 언어).
사용: python make_photocard.py <in> <out.png> "JEJU • 2024.07 • HAMDEOK BEACH" [focus_x 0..1]
"""
import sys
from PIL import Image, ImageDraw, ImageFont, ImageOps
import numpy as np

CARD_W, CARD_H = 600, 900
PAPER = (247, 245, 240)

def inner_photo(im):
    """폴라로이드 종이(밝고 채도 낮음)를 걷어내고 사진 영역 bbox를 찾는다."""
    a = np.array(im.convert('RGB')).astype(np.int16)
    mx = a.max(axis=2); mn = a.min(axis=2)
    paper = (mn > 215) & ((mx - mn) < 22)
    notpaper = ~paper
    def run(frac):
        # 사진 줄 = 양옆에 종이가 남아 비율이 0.35~0.93 사이. 바깥 회색 배경 줄(≈1.0)은 제외. 가장 긴 연속 구간.
        ok = (frac > 0.35) & (frac < 0.93)
        best = (0, 0); start = None
        for i, v in enumerate(list(ok) + [False]):
            if v and start is None: start = i
            if not v and start is not None:
                if i - start > best[1] - best[0]: best = (start, i - 1)
                start = None
        return best
    y0, y1 = run(notpaper.mean(axis=1)); x0, x1 = run(notpaper.mean(axis=0))
    if y1 - y0 < 10 or x1 - x0 < 10: return im
    # 캡션 줄이 붙어 들어오지 않게: 아래쪽 가장자리에서 종이가 다시 시작되는 곳까지
    if (x1 - x0) < im.width * 0.5 or (y1 - y0) < im.height * 0.4: return im
    return im.crop((x0 + 2, y0 + 2, x1 - 1, y1 - 1))

def font(size):
    for f in ['/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf', 'C:/Windows/Fonts/arial.ttf']:
        try: return ImageFont.truetype(f, size)
        except Exception: pass
    return ImageFont.load_default()

def make(src, dst, caption, focus_x=0.5):
    im = Image.open(src).convert('RGB')
    im = inner_photo(im)
    # 세로 4:5 크롭(가로 사진이면 focus_x 중심으로)
    tw, th = 520, 650
    ratio = tw / th
    w, h = im.size
    if w / h > ratio:
        cw = int(h * ratio); cx = int(w * focus_x)
        x0 = max(0, min(w - cw, cx - cw // 2)); im = im.crop((x0, 0, x0 + cw, h))
    else:
        ch = int(w / ratio); y0 = max(0, (h - ch) // 3); im = im.crop((0, y0, w, y0 + ch))
    im = im.resize((tw, th), Image.LANCZOS)
    card = Image.new('RGB', (CARD_W, CARD_H), PAPER)
    # 종이 질감(아주 옅은 노이즈)
    noise = np.random.default_rng(7).integers(-3, 4, (CARD_H, CARD_W, 1))
    card = Image.fromarray(np.clip(np.array(card).astype(np.int16) + noise, 0, 255).astype(np.uint8))
    x = (CARD_W - tw) // 2; y = 40
    card.paste(im, (x, y))
    d = ImageDraw.Draw(card)
    # 사진 테두리 살짝
    d.rectangle((x - 1, y - 1, x + tw, y + th), outline=(225, 222, 214))
    f = font(19)
    cap = caption.upper()
    tw_ = d.textlength(cap, font=f)
    d.text(((CARD_W - tw_) / 2, y + th + 62), cap, fill=(70, 66, 62), font=f)
    # 작은 하트
    hx, hy = CARD_W // 2, y + th + 130
    d.polygon([(hx, hy + 10), (hx - 10, hy), (hx - 5, hy - 7), (hx, hy - 2), (hx + 5, hy - 7), (hx + 10, hy)], fill=(170, 165, 160))
    f2 = font(12)
    import re, os
    m = re.search(r'(\d+)', os.path.basename(dst))
    d.text((44, CARD_H - 40), "NO.%03d  •  FILM  •  2024.07" % (int(m.group(1)) if m else 0), fill=(150, 146, 140), font=f2)
    card.save(dst, optimize=True)
    print(dst, card.size)

if __name__ == '__main__':
    make(sys.argv[1], sys.argv[2], sys.argv[3], float(sys.argv[4]) if len(sys.argv) > 4 else 0.5)
