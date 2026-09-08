"""21차-3: 픽업 스프라이트에 굵은 이중 테두리(짙은 남색 + 흰 외곽)를 굽는다.
셰이더 테두리는 텍셀 단위라 멀리서 사라진다 → 그림 자체에 크기 비례 테두리를 넣어 원거리에서도 또렷하게.
사용: python bold_outline.py <png...>   (원본은 Tools/Art/_keybackup_r21/ 에 보관)
"""
import sys, os, shutil
from PIL import Image, ImageFilter, ImageEnhance
import numpy as np
from scipy import ndimage as ndi

DARK = (14, 12, 26, 255)
WHITE = (255, 255, 255, 255)
DARK_PCT = 0.022   # 22차-1: 테두리는 살짝 얇게(3.2→2.2%)
WHITE_PCT = 0.014

def dilate(mask, r):
    if r <= 0: return mask
    a = np.array(mask) > 127
    d = ndi.distance_transform_edt(~a) <= r
    return Image.fromarray((d * 255).astype(np.uint8))

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
    mask = Image.fromarray((ndi.binary_closing(np.array(mask) > 127, iterations=2) * 255).astype(np.uint8))
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
    body_a = np.array(body).astype(np.float32); body_a[:, :, 3] = np.array(mask.filter(ImageFilter.GaussianBlur(0.6)))
    # 22차-1: 3D 입체감 — 가장자리 안쪽 띠에 빛 방향(왼쪽 위) 기준으로 그림자/하이라이트를 넣는다.
    band_w = max(4, int(base * 0.09))
    m = np.array(mask).astype(np.float32) / 255.0
    dist = ndi.distance_transform_edt(m > 0.5)
    band = np.clip(1.0 - dist / band_w, 0, 1) * (m > 0.5)   # 1 = 가장자리, 안쪽으로 갈수록 0
    band = band * band
    soft = ndi.gaussian_filter(m, band_w)
    gy, gx = np.gradient(soft)                          # 안쪽으로 향하는 기울기
    n = np.sqrt(gx * gx + gy * gy) + 1e-6; nx, ny = gx / n, gy / n
    light = (-0.55, -0.83)                              # 빛: 왼쪽 위(화면 y는 아래로 증가)
    lam = nx * light[0] + ny * light[1]                 # +: 빛을 받는 가장자리, −: 그늘
    shade = band * np.clip(-lam, 0, 1) * 0.42           # 오른쪽 아래 가장자리 어둡게
    hilite = band * np.clip(lam, 0, 1) * 0.30           # 왼쪽 위 가장자리 밝게
    for c in range(3):
        ch = body_a[:, :, c]
        ch = ch * (1 - shade) + (255 - (255 - ch) * (1 - hilite)) * 0 + ch * 0  # placeholder to keep shape
        body_a[:, :, c] = np.clip(body_a[:, :, c] * (1 - shade) * (1 - hilite) + 255 * hilite, 0, 255)
    # 바닥 쪽 전체에 은은한 그라데이션(위 밝고 아래 살짝 어두움)으로 볼륨감
    h_ = body_a.shape[0]; grad = np.linspace(1.0, 0.86, h_)[:, None]
    ys, xs = np.nonzero(m > 0.5)
    if len(ys):
        top, bot = ys.min(), ys.max(); span = max(1, bot - top)
        gcol = np.ones(h_); gcol[top:bot + 1] = np.linspace(1.04, 0.84, span + 1)
        body_a[:, :, :3] = np.clip(body_a[:, :, :3] * gcol[:, None, None], 0, 255)
    body = Image.fromarray(body_a.astype(np.uint8))
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
