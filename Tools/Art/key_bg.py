"""17차: 배경색을 모서리에서 샘플링해 키잉(색상 창 ±14° + 채도) — 테두리와 연결된 영역만 지운다(본체 안 분홍은 보존).
사용: python3 key_bg.py <out_dir> <Obs_X.png ...>  (원본은 Tools/Art/_keybackup/Obs_X.png)"""
import sys, os, numpy as np, cv2
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__))
def process(name, out_dir):
    src = os.path.join(HERE, '_keybackup', name)
    im = Image.open(src).convert('RGB'); rgb = np.asarray(im)
    H, W = rgb.shape[:2]
    corners = np.concatenate([rgb[:8, :8].reshape(-1, 3), rgb[:8, -8:].reshape(-1, 3), rgb[-8:, :8].reshape(-1, 3), rgb[-8:, -8:].reshape(-1, 3)])
    bg = np.median(corners, axis=0).astype(np.uint8)
    bh = cv2.cvtColor(bg.reshape(1, 1, 3), cv2.COLOR_RGB2HSV)[0, 0].astype(np.float32)
    hsv = cv2.cvtColor(rgb, cv2.COLOR_RGB2HSV).astype(np.float32)
    dh = np.abs(hsv[..., 0] - bh[0]); dh = np.minimum(dh, 180 - dh)   # OpenCV hue 0..180
    like = (dh < 8) & (hsv[..., 1] > 90) & (hsv[..., 2] > 90)
    dist = np.linalg.norm(rgb.astype(np.float32) - bg.astype(np.float32), axis=2)
    like |= dist < 40
    like = cv2.morphologyEx(like.astype(np.uint8), cv2.MORPH_CLOSE, np.ones((3, 3), np.uint8))
    n, lab = cv2.connectedComponents(like)
    border = set(np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]])))
    border.discard(0)
    keyed = np.isin(lab, list(border))
    keyed = cv2.dilate(keyed.astype(np.uint8), np.ones((3, 3), np.uint8)) > 0
    alpha = (~keyed).astype(np.float32)
    alpha = cv2.GaussianBlur(alpha, (0, 0), 0.8)
    alpha = np.clip((alpha - 0.35) / 0.4, 0, 1)
    opaque = (alpha > 0.5).astype(np.uint8)
    col = rgb.copy(); mask = opaque.copy(); k = np.ones((3, 3), np.uint8)
    for _ in range(24):
        dil = cv2.dilate(mask, k); ring = (dil > 0) & (mask == 0)
        if not ring.any(): break
        blur = cv2.blur(col * mask[..., None], (3, 3)).astype(np.float32)
        cnt = cv2.blur(mask.astype(np.float32), (3, 3))
        fill = (blur / np.maximum(cnt[..., None], 1e-3)).astype(np.uint8)
        col[ring] = fill[ring]; mask = dil
    # 가장자리 디스필: 반투명 픽셀은 배경색 성분을 줄인다
    edge = (alpha > 0) & (alpha < 1)
    cf = col.astype(np.float32)
    for c in (0, 2):
        cf[..., c] = np.where(edge, np.minimum(cf[..., c], cf[..., 1] + 90), cf[..., c])
    out = np.dstack([cf.astype(np.uint8), (alpha * 255).astype(np.uint8)])
    # 내용 bbox로 자르기(여백 4%) → 스프라이트 해상도 효율
    ys, xs = np.where(alpha > 0.05)
    if len(ys):
        m = int(0.04 * max(H, W)); y0, y1 = max(0, ys.min() - m), min(H, ys.max() + m); x0, x1 = max(0, xs.min() - m), min(W, xs.max() + m)
        out = out[y0:y1, x0:x1]
    Image.fromarray(out, 'RGBA').save(os.path.join(out_dir, name), optimize=True)
    print('ok', name, 'bg', bg.tolist(), 'keyed=%.2f' % keyed.mean(), out.shape[1], 'x', out.shape[0])
if __name__ == '__main__':
    for f in sys.argv[2:]: process(os.path.basename(f), sys.argv[1])
