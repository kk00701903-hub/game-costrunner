# ACE-Step 1.5 로 BGM 만들기

`tracks.json`(발주서 34파일 → 24개 생성 단위) + `generate.py`(로컬 API 클라이언트).
결과는 `Assets/Resources/CoastRun/BGM/`에 떨어지고 Unity가 다음 Play에 자동으로 집어 씁니다.

## 0. 한 번만: ACE-Step 설치

Python 3.12가 이미 깔려 있으니 uv 없이 venv로 갑니다. **PowerShell**에서:

```powershell
cd C:\dev
git clone https://github.com/ACE-Step/ACE-Step-1.5.git
cd ACE-Step-1.5
python -m venv venv
venv\Scripts\activate
```

### A. AMD Radeon (이 PC) — ROCm 7.2
```powershell
# requirements-rocm.txt 상단 주석의 순서대로 (AMD 저장소에서 torch 휠 설치)
pip install -r requirements-rocm.txt
start_api_server_rocm.bat          # http://127.0.0.1:8001
```
`start_*_rocm.bat`이 `HSA_OVERRIDE_GFX_VERSION`·`ACESTEP_LM_BACKEND=pt`를 자동 설정합니다.
"No GPU detected"가 뜨면 `python scripts\check_gpu.py`로 확인. RX 7000대는 표의 값을 수동 지정:
RX 7900/9070 `11.0.0`, RX 7800/7700 `11.0.1`, RX 7600 `11.0.2`.

### B. GPU가 안 잡히면 — CPU 모드 (느리지만 됨)
```powershell
pip install -e .
set ACESTEP_INIT_LLM=false
set ACESTEP_DEVICE=cpu
python acestep\api_server.py
```
turbo 8스텝 기준 2분 30초짜리 한 곡에 수 분. 밤에 `--priority P0`만 돌려 두면 됩니다.

### C. NVIDIA
포터블 패키지(README 링크, CUDA 12.8) 풀고 `start_api_server.bat`.

첫 실행 때 모델 ~10GB를 내려받습니다. `http://127.0.0.1:8001/health`가 200이면 준비 끝.

## 1. 생성

다른 PowerShell 창에서(venv 활성화 상태로):

```powershell
cd C:\dev\game\Tools\AceStepGen
python generate.py --only BGM_Menu                 # 1) 파이프라인 검증 — 75초 한 곡
python generate.py --priority P0 --stems           # 2) 없으면 플레이 불가한 11곡 + 스템
python generate.py --group cine --takes 4          # 3) 컷씬 스코어, 후보 4개씩
python generate.py                                 # 4) 나머지 전부
```

옵션
- `--takes N` 후보 N개(최대 8). 전부 `takes/`에 남고 `--pick K`번째가 채택됩니다. 마음에 드는 테이크를 나중에 골라 `Assets/Resources/CoastRun/BGM/<name>.wav`로 복사해도 됩니다.
- `--thinking` LM 플래너 사용(품질↑, VRAM 6GB↑ 필요). CPU 모드에선 빼세요.
- `--stems` 챕터 곡을 Demucs로 6트랙 분리 후 `_a/_b/_c`로 묶음. 먼저 `pip install demucs`.
- `--no-loopfix` 루프 크로스페이드 끄기.

## 2. 스템이 어떻게 만들어지나

```
BGM_CH1.wav (전체 편성 1곡, seed 고정)
   └─ Demucs htdemucs_6s → drums / bass / other / vocals / guitar / piano
        a = other + guitar      (패드 + 코드)      ← S01
        b = drums + bass        (리듬)            ← S02 추가
        c = piano + vocals      (리드 멜로디)      ← S03 추가
```
세 파일은 샘플 단위로 같은 길이로 잘립니다(게임이 동시 시작 후 볼륨만 페이드).
CH5는 같은 구조 + `BGM_CH5_d`(드론)를 따로 생성. 게임은 S17→S20에서 c, b, a 순으로 빼고 d만 남깁니다.

## 3. 확인·교체

1. Unity: **Assets → Refresh** → Play. 타이틀에서 `BGM_Menu`, 주행에서 챕터 스템.
2. 각 wav 선택 → Inspector: Load Type **Streaming**, Compression **Vorbis**, Preload 끔.
3. 마음에 안 드는 곡은 `tracks.json`의 `seed`를 바꾸거나 `prompt`를 고치고 `--only`로 재생성.
4. **`BGM_Cine_CH4_Close`(무음 3초)와 `BGM_End_Arrival`(90초 무음)** 은 AI가 "무음"을 잘 못 만듭니다. 생성본을 Audacity에서 열어 해당 구간을 직접 잘라 무음으로 바꾸는 것이 확실합니다.

## 4. 저장소

`takes/`와 `demucs_out/`은 gitignore. 최종 wav(약 300MB)는 커밋 여부를 정하세요 — 용량이 부담이면 Git LFS 또는 `Assets/Resources/CoastRun/BGM/*.wav`를 ignore 하고 릴리스 아카이브에만 포함.

## 라이선스

ACE-Step 1.5 코드는 MIT, "저작권 없는 자료로만 학습" 명시 → 상업 게임 사용 가능. 산출물에 AI 생성 사실을 표기하는 것을 권장(크레딧 화면).
