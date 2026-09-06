"""Far_<Set>_key.png (마젠타 하늘 위 원경) → Resources/CoastRun/Far_<Set>.png (RGBA, 2048 폭, 아래 페이드).
   Far_Town_NOON과 같은 규격: 하늘 투명, 산 능선부터 아래로 불투명, 맨 아래 20%는 3D 바다로 녹아들게 페이드."""
import glob, os
import numpy as np
from PIL import Image, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "..", "..", "Assets", "Resources", "CoastRun")

def key_mask(a):
    key = np.median(np.concatenate([a[:8, :].reshape(-1, 3), a[:40, :8].reshape(-1, 3)]), axis=0)
    d = np.abs(a - key).sum(axis=2)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    pink = (r > 150) & (g < 110) & (b > 90) & (r - g > 90)
    return (d < 100) | pink   # True = sky(key)

for path in sorted(glob.glob(os.path.join(HERE, "Far_*_key.png"))):
    name = os.path.basename(path)[:-8]            # Far_Coast
    im = Image.open(path).convert("RGB")
    W, H = im.size
    a = np.asarray(im).astype(np.int16)
    sky = key_mask(a)
    # 열마다 첫 비-하늘 픽셀 = 능선. 그 위는 투명, 아래는 불투명.
    first = np.where((~sky).any(axis=0), (~sky).argmax(axis=0), H - 1)
    from scipy.ndimage import median_filter
    first = median_filter(first, size=31, mode="nearest")
    yy = np.arange(H)[:, None]
    alpha = np.clip((yy - first[None, :] + 6) / 12.0, 0, 1)
    # 능선 근처 키색 번짐 제거: 키 마스크인 픽셀은 알파 0
    alpha = np.where(sky & (yy < first[None, :] + 20), 0, alpha)
    fade = np.clip((0.99 - yy / H) / 0.20, 0, 1)
    alpha = alpha * fade
    alpha_img = Image.fromarray((alpha * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(1.2))
    rgb = a.astype(np.float32)
    # 마젠타 프린지 중화
    pr, pg, pb = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    fringe = ((pr - pg) > 60) & ((pb - pg) > 40)
    if fringe.any():
        blurred = np.asarray(im.filter(ImageFilter.MedianFilter(9))).astype(np.float32)
        rgb[fringe] = blurred[fringe]
    rgba = np.dstack([np.clip(rgb, 0, 255), np.asarray(alpha_img).astype(np.float32)])
    out = Image.fromarray(rgba.astype(np.uint8), "RGBA")
    al = np.asarray(out)[..., 3]
    rows = np.where(al.max(axis=1) > 8)[0]
    top = max(0, rows.min() - 8)
    out = out.crop((0, top, W, H))
    # 해안선 찾기: 능선 아래에서 열의 60% 이상이 '바다색'(청록·파랑, R 낮음)인 첫 줄. Far_Town 규격상 해안선 = 높이의 72%.
    o = np.asarray(out).astype(np.int16)
    r, g, b = o[..., 0], o[..., 1], o[..., 2]
    sea = ((b + g) // 2 - r > 25) & (o[..., 3] > 128) & (b > 90)
    frac = sea.mean(axis=1)
    h2 = out.size[1]
    shore = None
    for y in range(int(h2 * 0.3), h2 - 10):
        if (frac[y:y + 10] > 0.6).all():
            shore = y; break
    if shore is not None:
        keep = min(h2, int(shore / 0.72))
        out = out.crop((0, 0, W, keep))
        # 아래 20% 페이드를 새 높이 기준으로 다시 적용
        arr = np.asarray(out).astype(np.float32)
        yy2 = np.arange(out.size[1])[:, None] / out.size[1]
        arr[..., 3] *= np.clip((0.99 - yy2) / 0.20, 0, 1)
        out = Image.fromarray(arr.astype(np.uint8), "RGBA")
        print(" shoreline at", shore, "→ height", keep)
    # 해안선이 없는 세트(마을·오름·숲)는 위 820px만 남긴다 — 아래 근경은 3D 마을/길에 가려진다.
    if shore is None and out.size[1] > 820:
        out = out.crop((0, 0, W, 820))
        arr = np.asarray(out).astype(np.float32)
        yy2 = np.arange(820)[:, None] / 820
        arr[..., 3] *= np.clip((0.99 - yy2) / 0.20, 0, 1)
        out = Image.fromarray(arr.astype(np.uint8), "RGBA")
    if out.size[0] != 2048:
        out = out.resize((2048, int(out.size[1] * 2048 / out.size[0])), Image.LANCZOS)
    dst = os.path.join(OUT, name + ".png")
    out.save(dst, optimize=True)
    print("wrote", dst, out.size)
