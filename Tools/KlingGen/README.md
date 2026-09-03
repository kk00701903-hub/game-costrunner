# KlingGen — Coast Run asset generation (outside Unity)

## Setup

1. Fill keys in project-root `.env` (see `.env.example`):
   - Official: `KLING_ACCESS_KEY` + `KLING_SECRET_KEY`
   - Reseller: `KLING_API_KEY` only
2. `pip install -r Tools/KlingGen/requirements.txt`
3. `python Tools/KlingGen/test_connection.py`

Console: https://app.klingai.com/global/dev

## Layout

| Path | Role |
|------|------|
| `kling_client.py` | JWT / Bearer client |
| `config.py` | Loads root `.env` |
| `test_connection.py` | Ping account |
| `jobs/` | Batch job JSON (later) |
| `out/` | Generated assets + logs |
