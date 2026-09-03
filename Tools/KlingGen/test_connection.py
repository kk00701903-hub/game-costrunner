"""
Verify Kling credentials from project-root .env.

Usage (from repo root):
  pip install -r Tools/KlingGen/requirements.txt
  python Tools/KlingGen/test_connection.py
"""

from __future__ import annotations

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from config import (  # noqa: E402
    ENV_PATH,
    KLING_ACCESS_KEY,
    KLING_API_KEY,
    KLING_BASE_URL,
    KLING_SECRET_KEY,
)
from kling_client import KlingClient  # noqa: E402


def main() -> int:
    print(f".env     : {ENV_PATH}  ({'found' if ENV_PATH.is_file() else 'MISSING'})")
    print(f"base_url : {KLING_BASE_URL}")
    print(f"access   : {'set' if KLING_ACCESS_KEY else 'empty'}")
    print(f"secret   : {'set' if KLING_SECRET_KEY else 'empty'}")
    print(f"api_key  : {'set' if KLING_API_KEY else 'empty'}")

    try:
        client = KlingClient()
    except ValueError as exc:
        print(f"\nFAIL — {exc}")
        print("Edit .env at the project root, then re-run.")
        return 1

    print(f"auth     : {client.auth_mode}")
    try:
        data = client.ping()
        code = data.get("code")
        msg = data.get("message") or data.get("msg") or ""
        print(f"\nOK — connected (code={code}, message={msg!r})")
        return 0
    except Exception as exc:
        print(f"\nFAIL — request error: {exc}")
        print("Check base URL, key type (official vs reseller), and account API access.")
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
