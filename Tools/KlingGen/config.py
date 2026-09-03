"""Load Kling / shared config from project-root .env."""

from __future__ import annotations

import os
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]  # c:/dev/game
ENV_PATH = ROOT / ".env"
OUT_DIR = Path(__file__).resolve().parent / "out"
RAW_DIR = OUT_DIR / "raw"
APPROVED_DIR = OUT_DIR / "approved"
LOG_DIR = OUT_DIR / "log"
JOBS_DIR = Path(__file__).resolve().parent / "jobs"


def load_dotenv(path: Path = ENV_PATH) -> dict[str, str]:
    values: dict[str, str] = {}
    if not path.is_file():
        return values
    for raw in path.read_text(encoding="utf-8").splitlines():
        line = raw.strip()
        if not line or line.startswith("#") or "=" not in line:
            continue
        key, _, val = line.partition("=")
        key = key.strip()
        val = val.strip().strip('"').strip("'")
        values[key] = val
        # Also expose to os.environ for child tools (do not overwrite existing).
        os.environ.setdefault(key, val)
    return values


_ENV = load_dotenv()


def env(key: str, default: str = "") -> str:
    return (os.environ.get(key) or _ENV.get(key) or default).strip()


KLING_ACCESS_KEY = env("KLING_ACCESS_KEY")
KLING_SECRET_KEY = env("KLING_SECRET_KEY")
KLING_API_KEY = env("KLING_API_KEY")
KLING_BASE_URL = env("KLING_BASE_URL", "https://api.klingai.com")

STYLE_SUFFIX = (
    "painterly anime background art, Makoto Shinkai and Studio Ghibli influence, "
    "hand-painted brush texture, deep azure summer sky, towering white cumulus clouds, "
    "turquoise sea with white foam, warm cream stone pavement, "
    "sharp cool blue-violet shadows, high saturation, strong directional sunlight, "
    "visible utility poles with black power lines crossing the sky, "
    "vertical composition, mobile portrait 9:16"
)

NEGATIVE_PROMPT = (
    "photorealistic, 3D render, CGI, plastic shading, low contrast, muddy colors, "
    "text, letters, watermark, logo, signature, UI overlay, mouse cursor, "
    "blurry, distorted anatomy, extra limbs, western cartoon, chibi, oversaturated neon"
)


def ensure_dirs() -> None:
    for p in (OUT_DIR, RAW_DIR, APPROVED_DIR, LOG_DIR, JOBS_DIR):
        p.mkdir(parents=True, exist_ok=True)
