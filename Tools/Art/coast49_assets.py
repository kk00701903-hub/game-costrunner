"""49차: 해안 드레싱 에셋 — Kling 원본(Tools/KlingGen/out/raw/coast49) → 키잉 스프라이트 3장 + 타일 텍스처 2장.
사용: python coast49_assets.py"""
import os, sys, shutil, uuid, numpy as np, cv2
from PIL import Image
ROOT = r"C:\dev\game"; RAW = os.path.join(ROOT, r"Tools\KlingGen\out\raw\coast49"); RES = os.path.join(ROOT, r"Assets\Resources\CoastRun")
ART = os.path.join(ROOT, r"Tools\Art"); sys.path.insert(0, ART)
BACKUP = os.path.join(ART, "_keybackup"); os.makedirs(BACKUP, exist_ok=True)
import key_bg

def meta_like(src_meta, dst_png):
    dst = dst_png + ".meta"
    if os.path.exists(dst): return
    s = open(src_meta, encoding="utf-8").read()
    import re
    s = re.sub(r"^guid: .*$", "guid: " + uuid.uuid4().hex, s, flags=re.M)
    open(dst, "w", encoding="utf-8").write(s)

def sprite(raw, name, global_key=False, cut_bottom=0.0):
    shutil.copy(raw, os.path.join(BACKUP, name))
    key_bg.process(name, RES)
    dst = os.path.join(RES, name)
    if global_key or cut_bottom > 0:
        # 식물 안쪽에 갇힌 분홍(테두리와 안 이어진 영역)까지 전부 키잉 — 본체에 분홍이 없는 그림만.
        rgba = np.asarray(Image.open(dst).convert("RGBA")).copy()
        rgb = rgba[..., :3]
        src = np.asarray(Image.open(raw).convert("RGB"))
        corners = np.concatenate([src[:8, :8].reshape(-1, 3), src[:8, -8:].reshape(-1, 3), src[-8:, :8].reshape(-1, 3), src[-8:, -8:].reshape(-1, 3)])
        bg = np.median(corners, axis=0).astype(np.uint8)
        bh = cv2.cvtColor(bg.reshape(1, 1, 3), cv2.COLOR_RGB2HSV)[0, 0].astype(np.float32)
        hsv = cv2.cvtColor(np.ascontiguousarray(rgb), cv2.COLOR_RGB2HSV).astype(np.float32)
        dh = np.abs(hsv[..., 0] - bh[0]); dh = np.minimum(dh, 180 - dh)
        if global_key:
            pink = (dh < 12) & (hsv[..., 1] > 60) & (hsv[..., 2] > 80)
            pink = cv2.morphologyEx(pink.astype(np.uint8), cv2.MORPH_OPEN, np.ones((2, 2), np.uint8)) > 0
            rgba[pink, 3] = 0
            # 반투명 가장자리의 분홍 물빼기
            edge = (rgba[..., 3] > 0) & (rgba[..., 3] < 255) & (dh < 20)
            rgba[edge, 0] = np.minimum(rgba[edge, 0], rgba[edge, 1] + 40); rgba[edge, 2] = np.minimum(rgba[edge, 2], rgba[edge, 1] + 40)
        if cut_bottom > 0:
            h = rgba.shape[0]; rgba[int(h * (1 - cut_bottom)):, 3] = 0
        Image.fromarray(rgba, "RGBA").save(dst, optimize=True)
    meta_like(os.path.join(RES, "Obs_HydrangeaPot.png.meta"), dst)

def seamless(img, blend=0.22):
    """가장자리 크로스페이드로 타일링 가능하게: 반씩 밀어(roll) 이음새를 가운데로 보내고 부드럽게 섞는다."""
    a = img.astype(np.float32); H, W = a.shape[:2]
    r = np.roll(np.roll(a, H // 2, 0), W // 2, 1)   # 이음새가 정중앙 십자로
    bw = int(W * blend); bh = int(H * blend)
    # 가로 방향 십자 블렌드
    x = np.abs(np.arange(W) - W / 2); wx = np.clip(1 - x / bw, 0, 1)[None, :, None]
    y = np.abs(np.arange(H) - H / 2); wy = np.clip(1 - y / bh, 0, 1)[:, None, None]
    w = np.maximum(wx, wy)
    # 이음새 위치에는 원본(밀기 전)이 자연스럽게 이어지므로 원본으로 덮는다 (원본은 중앙이 연속)
    out = r * (1 - w) + a * w
    return np.clip(out, 0, 255).astype(np.uint8)

def texture(raw, name, box, size=1024, inpaint_dark=False, basalt=False):
    im = Image.open(raw).convert("RGB").crop(box).resize((size, size), Image.LANCZOS)
    arr = np.asarray(im).copy()
    if inpaint_dark:
        # 풀밭 안의 돌(회색·어두움) 지우기
        hsv = cv2.cvtColor(arr, cv2.COLOR_RGB2HSV)
        m = (((hsv[..., 1] < 80) & (hsv[..., 2] < 170)) | (hsv[..., 2] < 95)).astype(np.uint8)
        m = cv2.dilate(m, np.ones((9, 9), np.uint8))
        arr = cv2.inpaint(arr, m, 7, cv2.INPAINT_TELEA)
    if basalt:
        # 갈색 바위 → 제주 현무암(검은 회색): 주황·갈색 계열만 채도 빼고 어둡게
        hsv = cv2.cvtColor(arr, cv2.COLOR_RGB2HSV).astype(np.float32)
        brown = (hsv[..., 0] < 30) | (hsv[..., 0] > 150)
        hsv[brown, 1] *= 0.15; hsv[brown, 2] *= 0.62
        arr = cv2.cvtColor(np.clip(hsv, 0, 255).astype(np.uint8), cv2.COLOR_HSV2RGB)
    out = seamless(arr)
    Image.fromarray(out).save(os.path.join(RES, name), optimize=True)
    meta_like(os.path.join(RES, "Tex_Stonewall_Jeju.png.meta"), os.path.join(RES, name))
    print("tex", name, out.shape)

sprite(os.path.join(RAW, r"silvergrass\kling_20260912_000131_1.png"), "Obs_SilverGrass.png", global_key=True)
sprite(os.path.join(RAW, r"canola\kling_20260912_000134_2.png"), "Obs_Canola.png", global_key=True, cut_bottom=0.09)
sprite(os.path.join(RAW, r"basalt\kling_20260912_000138_1.png"), "Obs_BasaltPile.png")
texture(os.path.join(RAW, r"grass\kling_20260912_000141_1.png"), "Tex_Grass_Meadow.png", (0, 130, 640, 770), inpaint_dark=True)
texture(os.path.join(RAW, r"shore\kling_20260912_000144_2.png"), "Tex_Shore_Basalt.png", (0, 440, 640, 1024), basalt=True)
print("done")
