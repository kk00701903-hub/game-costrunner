"""21차-3: 픽업 스프라이트에 굵은 이중 테두리(짙은 남색 + 흰 외곽)를 굽는다.
셰이더 테두리는 텍셀 단위라 멀리서 사라진다 → 그림 자체에 크기 비례 테두리를 넣어 원거리에서도 또렷하게.
사용: python bold_outline.py <png...>   (원본은 Tools/Art/_keybackup_r21/ 에 보관)
"""
import sys, os, shutil
from PIL import Image, ImageFilter, ImageEnhance
import numpy as np

DARK = (14, 12, 26, 255)
WHITE = (255, 255, 255, 255)
DARK_PCT = 0.032   # 테두리 두께 = 짧은 변의 %
WHITE_PCT = 0.020

def dilate(mask, r):
    if r <= 0: return mask
    size = 2 * r + 1
    return mask.filter(ImageFilter.MaxFilter(size))

def process(path):
    im = Image.open(path).convert('RGBA')
    w, h = im.size
    base = min(w, h)
    d1 = max(3, int(base * DARK_PCT))
    d2 = max(2, int(base * WHITE_PCT))
    pad = d1 + d2 + 4
    canvas = Image.new('RGBA', (w + 2 * pad, h + 2 * pad), (0, 0, 0, 0))
    canvas.paste(im, (pad, pad), im)
    a = np.array(canvas)[:, :, 3]
    # 마젠타 프린지 제거: 반투명 가장자리의 색을 안쪽 색으로 밀어 넣는 대신 알파를 이진화(경계는 테두리가 덮는다)
    mask = Image.fromarray(((a > 110) * 255).astype(np.uint8))
    # 작은 구멍 메우기
    mask = mask.filter(ImageFilter.MaxFilter(5)).filter(ImageFilter.MinFilter(5))
    dark = dilate(mask, d1)
    white = dilate(dark, d2)
    # 살짝 부드럽게
    dark_s = dark.filter(ImageFilter.GaussianBlur(0.8))
    white_s = white.filter(ImageFilter.GaussianBlur(0.8))
    out = Image.new('RGBA', canvas.size, (0, 0, 0, 0))
    out.paste(Image.new('RGBA', canvas.size, WHITE), (0, 0), white_s)
    out.paste(Image.new('RGBA', canvas.size, DARK), (0, 0), dark_s)
    # 본체: 채도·대비 살짝 올리고 마스크로 잘라 붙인다
    body = ImageEnhance.Color(canvas).enhance(1.15)
    body = ImageEnhance.Contrast(body).enhance(1.08)
    body_a = np.array(body); body_a[:, :, 3] = np.array(mask.filter(ImageFilter.GaussianBlur(0.6)))
    body = Image.fromarray(body_a)
    out.alpha_composite(body)
    # 여백 크롭(흰 테두리 밖 4px)
    bbox = white.getbbox()
    out = out.crop((max(0, bbox[0] - 2), max(0, bbox[1] - 2), min(out.width, bbox[2] + 2), min(out.height, bbox[3] + 2)))
    return out

if __name__ == '__main__':
    bk = os.path.join(os.path.dirname(os.path.abspath(__file__)), '_keybackup_r21')
    os.makedirs(bk, exist_ok=True)
    for p in sys.argv[1:]:
        dst = os.path.join(bk, os.path.basename(p))
        if not os.path.exists(dst): shutil.copy2(p, dst)
        out = process(p)
        out.save(p, optimize=True)
        print(os.path.basename(p), out.size)
