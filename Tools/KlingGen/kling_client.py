"""
Kling AI API client for Coast Run asset generation.
Auth: official JWT (AK/SK) or third-party Bearer (KLING_API_KEY).
"""

from __future__ import annotations

import base64
import json
import time
from pathlib import Path
from typing import Any, Optional

import requests

try:
    import jwt
except ImportError as exc:  # pragma: no cover
    raise SystemExit(
        "PyJWT required. Run: pip install -r Tools/KlingGen/requirements.txt"
    ) from exc

from config import (
    KLING_ACCESS_KEY,
    KLING_API_KEY,
    KLING_BASE_URL,
    KLING_SECRET_KEY,
    LOG_DIR,
    ensure_dirs,
)


class KlingClient:
    def __init__(
        self,
        access_key: str | None = None,
        secret_key: str | None = None,
        api_key: str | None = None,
        base_url: str | None = None,
    ) -> None:
        self.access_key = (access_key or KLING_ACCESS_KEY or "").strip()
        self.secret_key = (secret_key or KLING_SECRET_KEY or "").strip()
        self.api_key = (api_key or KLING_API_KEY or "").strip()
        self.base_url = (base_url or KLING_BASE_URL or "https://api.klingai.com").rstrip("/")
        self._token: str | None = None
        self._token_exp: float = 0.0
        self._session = requests.Session()
        ensure_dirs()

        if not self.api_key and not (self.access_key and self.secret_key):
            raise ValueError(
                "Missing Kling credentials. Set KLING_ACCESS_KEY + KLING_SECRET_KEY "
                "(official) or KLING_API_KEY (reseller) in project-root .env"
            )

    @property
    def auth_mode(self) -> str:
        if self.access_key and self.secret_key:
            return "jwt"
        return "bearer"

    def _token_jwt(self) -> str:
        now = time.time()
        # Refresh 5 minutes before 30-minute expiry.
        if self._token and now < self._token_exp - 300:
            return self._token

        headers = {"alg": "HS256", "typ": "JWT"}
        payload = {
            "iss": self.access_key,
            "exp": int(now) + 1800,
            "nbf": int(now) - 5,
        }
        token = jwt.encode(payload, self.secret_key, algorithm="HS256", headers=headers)
        if isinstance(token, bytes):
            token = token.decode("utf-8")
        self._token = token
        self._token_exp = now + 1800
        return token

    def _auth_header(self) -> dict[str, str]:
        if self.auth_mode == "jwt":
            return {"Authorization": f"Bearer {self._token_jwt()}"}
        return {"Authorization": f"Bearer {self.api_key}"}

    def _log(self, name: str, payload: Any) -> None:
        path = LOG_DIR / f"{int(time.time())}_{name}.json"
        try:
            path.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")
        except OSError:
            pass

    def _request(
        self,
        method: str,
        path: str,
        *,
        json_body: dict | None = None,
        params: dict | None = None,
        retries: int = 5,
    ) -> dict:
        url = f"{self.base_url}{path}"
        last_err: Exception | None = None
        for attempt in range(retries):
            headers = {
                "Content-Type": "application/json",
                **self._auth_header(),
            }
            try:
                resp = self._session.request(
                    method,
                    url,
                    headers=headers,
                    json=json_body,
                    params=params,
                    timeout=60,
                )
                self._log(
                    f"{method.lower()}_{path.strip('/').replace('/', '_')}",
                    {
                        "url": url,
                        "status": resp.status_code,
                        "request": json_body,
                        "response_text": resp.text[:4000],
                    },
                )
                if resp.status_code in (429, 500, 502, 503, 504):
                    time.sleep(min(2 ** attempt, 30))
                    continue
                if resp.status_code >= 400:
                    raise RuntimeError(f"Kling HTTP {resp.status_code}: {resp.text[:500]}")
                data = resp.json() if resp.content else {}
                return data if isinstance(data, dict) else {"data": data}
            except RuntimeError:
                raise
            except Exception as exc:  # network
                last_err = exc
                time.sleep(min(2 ** attempt, 30))
        raise RuntimeError(f"Kling request failed after retries: {last_err}")

    def ping(self) -> dict:
        """
        Lightweight connectivity check.
        Tries account costs endpoint; falls back to empty image generations list.
        """
        for path in (
            "/v1/account/costs",
            "/v1/images/generations?pageNum=1&pageSize=1",
        ):
            try:
                if "?" in path:
                    p, q = path.split("?", 1)
                    params = dict(part.split("=") for part in q.split("&"))
                    return self._request("GET", p, params=params)
                return self._request("GET", path)
            except RuntimeError as exc:
                last = exc
                continue
        raise RuntimeError(f"Connectivity check failed: {last}")

    def generate_image(
        self,
        prompt: str,
        negative_prompt: str | None = None,
        model: str = "kling-v1",
        aspect_ratio: str = "9:16",
        n: int = 1,
        reference_image: str | None = None,
        reference_type: str | None = None,
        image_fidelity: float | None = None,
    ) -> str:
        body: dict[str, Any] = {
            "model_name": model,
            "prompt": prompt,
            "n": n,
            "aspect_ratio": aspect_ratio,
        }
        if negative_prompt:
            body["negative_prompt"] = negative_prompt
        if reference_image:
            body["image"] = self._encode_image(reference_image)
            if reference_type:
                body["image_reference"] = reference_type
            if image_fidelity is not None:
                body["image_fidelity"] = image_fidelity

        data = self._request("POST", "/v1/images/generations", json_body=body)
        task_id = _extract_task_id(data)
        if not task_id:
            raise RuntimeError(f"No task_id in response: {data}")
        return task_id

    def wait(self, task_id: str, timeout: int = 600, interval: int = 5) -> list[str]:
        deadline = time.time() + timeout
        while time.time() < deadline:
            data = self._request("GET", f"/v1/images/generations/{task_id}")
            status = _extract_status(data)
            if status in ("succeed", "success", "completed"):
                return _extract_urls(data)
            if status in ("failed", "error"):
                raise RuntimeError(f"Task failed: {data}")
            time.sleep(interval)
        raise TimeoutError(f"Task {task_id} timed out after {timeout}s")

    def download(self, url: str, dest: Path) -> Path:
        dest.parent.mkdir(parents=True, exist_ok=True)
        resp = self._session.get(url, timeout=120)
        resp.raise_for_status()
        dest.write_bytes(resp.content)
        return dest

    @staticmethod
    def _encode_image(path_or_url: str) -> str:
        if path_or_url.startswith("http://") or path_or_url.startswith("https://"):
            return path_or_url
        raw = Path(path_or_url).read_bytes()
        b64 = base64.b64encode(raw).decode("ascii")
        return b64


def _extract_task_id(data: dict) -> Optional[str]:
    d = data.get("data") if isinstance(data.get("data"), dict) else data
    for key in ("task_id", "taskId", "id"):
        if isinstance(d, dict) and d.get(key):
            return str(d[key])
    return None


def _extract_status(data: dict) -> str:
    d = data.get("data") if isinstance(data.get("data"), dict) else data
    if not isinstance(d, dict):
        return ""
    for key in ("task_status", "status", "state"):
        if d.get(key):
            return str(d[key]).lower()
    return ""


def _extract_urls(data: dict) -> list[str]:
    d = data.get("data") if isinstance(data.get("data"), dict) else data
    urls: list[str] = []
    if not isinstance(d, dict):
        return urls
    task_result = d.get("task_result") or d.get("result") or {}
    images = []
    if isinstance(task_result, dict):
        images = task_result.get("images") or task_result.get("image") or []
    if isinstance(images, dict):
        images = [images]
    for item in images or []:
        if isinstance(item, str):
            urls.append(item)
        elif isinstance(item, dict):
            for k in ("url", "image_url", "image"):
                if item.get(k):
                    urls.append(str(item[k]))
                    break
    return urls
