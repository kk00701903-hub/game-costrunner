"""14차-7: Obs_*.png 마젠타 키 → 진짜 알파 채널.
가장자리 색 번짐(디스필) + 투명 픽셀 색을 본체 색으로 팽창시켜 밉맵/필터링에서 분홍 테두리가 안 생기게 한다."""
import sys, glob, os, numpy as np, cv2
from PIL import Image

GENTLE = {"Obs_Jelly_Grape", "Obs_Jelly_Strawberry"}
STRONG = {"Obs_Coin_Gold", "Obs_Coin_Silver", "Obs_Star", "Obs_Jelly_Lemon", "Obs_Jelly_Lime", "Obs_Jelly_Soda"}   # 노랑/초록/파랑 본체만

def process(path, backup_dir):
    bk0 = os.path.join(backup_dir, os.path.basename(path))
    im = Image.open(bk0 if os.path.exists(bk0) else path).convert("RGBA")
    a = np.asarray(im).astype(np.float32) / 255.0
    rgb = a[..., :3]
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    d = np.sqrt((r - 1) ** 2 + g ** 2 + (b - 1) ** 2)
    pink = ((r > 0.75) & (g < 0.35) & (b > 0.75)) | ((r > g + 0.30) & (b > g + 0.22))
    name = os.path.splitext(os.path.basename(path))[0]
    keyed = (d < 0.38) | pink
    if name in GENTLE:   # 분홍/보라 본체: 키 거리로만 자른다
        keyed = d < 0.30
    if os.path.splitext(os.path.basename(path))[0] in STRONG:
        # 픽업 아이콘: 클링이 그린 분홍 글로우까지 지운다(마젠타 계열 색상 + 채도)
        hsv = cv2.cvtColor((rgb * 255).astype(np.uint8), cv2.COLOR_RGB2HSV).astype(np.float32)
        hue = hsv[..., 0] * 2.0; sat = hsv[..., 1] / 255.0
        keyed |= ((hue > 250) | (hue < 15)) & (sat > 0.08)
        keyed = cv2.dilate(keyed.astype(np.uint8), np.ones((3, 3), np.uint8)) > 0
    # 이미 알파가 있는 파일이면 그것도 존중
    keyed |= a[..., 3] < 0.5
    if keyed.mean() < 0.02 and (a[..., 3] < 0.5).mean() < 0.02:
        print("skip (no key)", path); return False
    alpha = (~keyed).astype(np.float32)
    # 부드러운 가장자리: 0.8px 블러 후 약간 안쪽으로
    alpha = cv2.GaussianBlur(alpha, (0, 0), 0.8)
    alpha = np.clip((alpha - 0.35) / 0.4, 0, 1)
    # 투명 픽셀 색 = 가장 가까운 본체 색(팽창) → 필터링 시 마젠타 번짐 방지
    opaque = (alpha > 0.5).astype(np.uint8)
    rgb8 = (rgb * 255).astype(np.uint8)
    # inpaint 대신 반복 팽창(빠름)
    col = rgb8.copy()
    mask = opaque.copy()
    k = np.ones((3, 3), np.uint8)
    for _ in range(24):
        dil = cv2.dilate(mask, k)
        ring = (dil > 0) & (mask == 0)
        if not ring.any(): break
        blur = cv2.blur(col * mask[..., None], (3, 3)).astype(np.float32)
        cnt = cv2.blur(mask.astype(np.float32), (3, 3))
        fill = (blur / np.maximum(cnt[..., None], 1e-3)).astype(np.uint8)
        col[ring] = fill[ring]
        mask = dil
    # 가장자리 디스필: 반투명 픽셀은 마젠타 성분을 줄인다(초록 채널 기준으로 R/B 클램프)
    edge = (alpha > 0) & (alpha < 1)
    cf = col.astype(np.float32)
    gch = cf[..., 1]
    cf[..., 0] = np.where(edge, np.minimum(cf[..., 0], gch + 90), cf[..., 0])
    cf[..., 2] = np.where(edge, np.minimum(cf[..., 2], gch + 90), cf[..., 2])
    out = np.dstack([cf.astype(np.uint8), (alpha * 255).astype(np.uint8)])
    os.makedirs(backup_dir, exist_ok=True)
    bk = os.path.join(backup_dir, os.path.basename(path))
    if not os.path.exists(bk): im.save(bk)
    Image.fromarray(out, "RGBA").save(path, optimize=True)
    print("ok", os.path.basename(path), f"keyed={keyed.mean():.2f}")
    return True

if __name__ == "__main__":
    root = sys.argv[1]
    files = sys.argv[2:] or sorted(glob.glob(os.path.join(root, "Obs_*.png")))
    for f in files:
        process(f, os.path.join(os.path.dirname(os.path.abspath(__file__)), "_keybackup"))
