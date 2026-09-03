# 『우리의 송전탑』— 챕터 · 장면 · BGM · 분위기 일괄 스펙

> **목적:** Claude/에이전트가 **코드·오디오 연동을 구현할 때** 쓰는 단일 소스 오브 트루스.  
> **코드 구현은 이 문서 범위 밖** — 필요한 파일명·트리거·분위기·연출만 정의한다.  
> **기준 스토리:** `Story_OurPowerTower.md` + `StoryConfig` (`Assets/_CoastRun/Config/StoryConfig.asset`)  
> **비주얼 기준:** `StyleBible.md` + `Assets/_Guide/Reference/style_frame_1~5.jpg`

---

## 0. 한 줄 톤

> 여름 해안을 질주하는 **청량한 약속의 여행** → 시간이 지나며 **계절·추억이 겹치고** → 노을과 블루아워를 지나 **비밀 기지(송전탑)에서 만남**.  
> 음악은 항상 **무겁지 않게**, SoftHit조차도 **가볍게**. 슬픔보다 **그리움 + 설렘**.

---

## 1. 전체 타임라인 (챕터 = StoryAct)

| # | Act ID | 게임 이름 | 진행도 `progress` | 시각/조명 | BGM 슬롯 | 주요 UI |
|---|--------|-----------|-------------------|-----------|----------|---------|
| 0 | — | **MainMenu** | — | Scene_Frame_5 히어로 | `BGM_Menu` | 타이틀 · Start |
| 1 | `Prologue` | 프롤로그 | — (정지) | Scene_Frame_1~4 스틸 | `BGM_Prologue` | 풀스크린 스틸+캡션 |
| 2 | `Run` | 주행 · Chase | `0.00 → 0.55` | BrightNoon + 계절 루프 | `BGM_Run` | 미니멀 HUD · CHASE |
| 3 | `GoldenHour` | 노을 | `0.55 → 0.88` | GoldenHour 조명 | `BGM_Golden` | `GOLDEN HOUR` |
| 4 | `BlueHourApproach` | 블루아워 | `0.88 → 1.00` | BlueHour 조명 | `BGM_Blue` | `BLUE HOUR` |
| 5 | `Arrival` | 만남 | `≥ 1.00` | 송전탑·노을 잔광 | `BGM_Arrival` | 엔딩 카드 2장 |

**진행도 공식:** `progress = PathDistance / TowerDistance`  
**디폴트 임계값:** `goldenHourAt = 0.55`, `blueHourAt = 0.88` (`StoryConfig`)

**BGM 전환 규칙 (구현 시 필수):**
- Act 변경 시 **크로스페이드 1.5~3.0초** (Arrival만 페이드인 단독 가능).
- MainMenu → Prologue: 페이드아웃 0.8s → Prologue 페이드인.
- Prologue 씬4(핸드오프) → Run: **같은 에너지로 자연스럽게** (Prologue outro ≈ Run intro 키 맞춤 권장).
- SoftHit / NearMiss는 BGM을 끊지 않음 — SFX·응원 보이스만.

---

## 2. BGM 트랙 카탈로그 (제작·배치용)

권장 경로: `Assets/Resources/CoastRun/Audio/`  
포맷: **OGG Vorbis** (루프) / 엔딩만 WAV·OGG 논루프 가능.  
네이밍은 아래 **그대로** 쓸 것 (코드에서 `Resources.Load` 예정).

| 파일명 | 길이 목표 | BPM | 조성(권장) | 루프 | 악기·텍스처 | 한 줄 무드 |
|--------|-----------|-----|------------|------|-------------|------------|
| `BGM_Menu.ogg` | 60~90s | 92–100 | D major / A major | ✅ | 어쿠스틱 기타, soft pad, 먼 파도 | 타이틀·설렘·여행 시작 전 |
| `BGM_Prologue.ogg` | 70~100s | 78–88 | G major → E minor 살짝 | ✅ (씬별 duck) | 피아노·가벼운 스트링, UI 클릭과 안 겹치게 | 메시지·장애·결심의 서사 |
| `BGM_Run.ogg` | 120~180s | 110–118 | C / F major | ✅ | 기타 스트럼, 킥 라이트, 바람 하이햇 | 질주·청량·썸머 드라이브 |
| `BGM_Golden.ogg` | 90~140s | 100–108 | Bb / F major | ✅ | 워머 패드, soft 멜로디, 낮은 현 | 노을·서두름·아름다움 |
| `BGM_Blue.ogg` | 80~120s | 88–96 | A minor / D dorian | ✅ | 앰비언트, 느린 아르페지오, 저역 줄임 | 긴장·기대·거의 도착 |
| `BGM_Arrival.ogg` | 45~70s | 72–80 | C major 해결 | ❌ 또는 긴 테일 | 피아노 주제 회상 + soft 코러스 | 안도·만남·여운 |

### 2.1 Suno / Udio 프롬프트 골자 (영문 키워드)

```
Menu: gentle summer acoustic, coastal breeze, hopeful, anime slice-of-life, no vocals, loopable
Prologue: soft piano storytelling, bittersweet texting, sparse, emotional but light, no vocals
Run: upbeat skateboarding coastal run, bright acoustic pop-instrumental, windy, energetic, loopable, no vocals
Golden: golden hour warm pads, nostalgic sunset ride, hopeful urgency, no vocals
Blue: twilight blue hour ambient, soft tension, approaching reunion, sparse melody, no vocals
Arrival: emotional piano reunion, soft resolve, cinematic fade, no vocals
```

### 2.2 볼륨·더킹 (구현 가이드)

| 상황 | BGM | Ambient(파도/바람) | SFX |
|------|-----|-------------------|-----|
| MainMenu | 0.55 | 0.25 | UI 0.7 |
| Prologue 스틸 | 0.40 | 0.10 | 페이지 0.5 |
| Run 정상 | 0.50 | 0.30 | 휠·NearMiss |
| SoftHit | 0.50 유지 | — | SoftHit 우선 |
| Landmark 독백 | **duck → 0.28** 2s | — | 독백 UI |
| Arrival | 0.45 | 0.15 | 거의 없음 |

---

## 3. 챕터별 상세 (장면 · 분위기 · 필요 에셋)

---

### 3.0 Main Menu

| 항목 | 내용 |
|------|------|
| **비주얼** | `Scene_Frame_5` / `UI_TitleBackground` — 소녀 등·하늘·구름 히어로 |
| **분위기** | “지금 떠나면 노을 전에 닿을 수 있을까” — 밝고 조용한 설렘 |
| **카피 톤** | 브랜드 `Coast Run` / 부제 『우리의 송전탑』 가능 |
| **BGM** | `BGM_Menu` |
| **SFX** | `UI_Click`, `UI_Start` (짧은 보드 킥오프 느낌 OK) |
| **필요** | 타이틀 로고(있으면), Start 버튼, Skip Prologue 토글(재플레이 시 `CoastRun_SkipPrologue`) |

---

### 3.1 Prologue — 4장면 (풀스크린 스틸)

공통: HUD 없음 · 터치/클릭 또는 `holdSeconds` 후 다음 · **BGM_Prologue** 유지.

| 씬 | 제목 | 스틸 | hold | 감정 아크 | 화면 연출 메모 | 추가 SFX(선택) |
|----|------|------|------|-----------|----------------|----------------|
| **P1** | 약속의 스마트폰 | `Scene_Frame_1` | 5.5s | 설렘·비밀 | 메시지 UI 느낌, 송전탑 사진 암시 | `SFX_PhoneVibrate` soft |
| **P2** | 예기치 못한 장애 | `Scene_Frame_2` | 5.0s | 초조·막힘 | 정류장·축제 혼잡, 채도 살짝 ↓ | `SFX_CrowdFar` duck |
| **P3** | 소녀의 결심 | `Scene_Frame_3` | 4.5s | 결의 | 보드 내려놓는 순간, 해 기울기 | `SFX_BoardDrop` |
| **P4** | 게임플레이 전환 | `Scene_Frame_4` → 인게임 | 2.5s | 출발 | **측면→추격 카메라 핸드오프** | `SFX_KickPush` → Run BGM |

**본문 카피 (StoryConfig 확정본):**

1. 「노을 질 때, 우리 어릴 적 비밀 기지였던 그 송전탑 아래에서 만나자. 꼭 할 말이 있어.」 + 송전탑 사진  
2. 정류장 전광판 『정비 중 · 운행 중단』 / 축제 도로 정체 / 예약등만 켠 차들  
3. 해 기울기 · 배낭 보드 풀기 · 송전탑 바라보며 땅을 걷어찬다  
4. 카메라가 등 뒤로 · 해안 내리막 질주 시작  

**프롤로그 종료 조건:** 씬4 핸드오프 완료 → `PrologueComplete` → `IsRunning = true` → **BGM_Run**.

---

### 3.2 Run — 주행 (Bright Noon + 계절)

| 항목 | 내용 |
|------|------|
| **progress** | 0.00 ~ 0.55 |
| **HUD** | `CHASE` · 진행바(송전탑/그 사람) · D-Day 타이머(기본 10800s=3h 설계) |
| **나레이션** | “바람을 가르며… 추억이 길을 안내한다.” (Act 진입 1회) |
| **비주얼** | StyleBible 표준: 왼쪽 마을·전신주 / 오른쪽 바다 / 하늘 ⅓+ / 소녀 하단 ⅓ |
| **계절** | 거리 밴드로 Spring→Summer→Autumn→Winter 순환 (`SeasonPalettes`) — **추억 랜드마크와 동기** |
| **BGM** | `BGM_Run` |
| **루프 SFX** | `Amb_Sea`, `Amb_Wind`, `SFX_Wheels` (속도에 볼륨·피치) |
| **이벤트 SFX** | NearMiss 성공 `SFX_NearMiss`, SoftHit `SFX_SoftHit`, 코인 `SFX_Coin`, 업그레이드 `SFX_Upgrade` |
| **응원 라인** | NearMiss 시 `nearMissCheerLines[]` 중 랜덤 (HUD 짧게) |

**분위기 키워드:** 청량 · 속도감 · 그리운 풍경이 스쳐 감 · 아직 낮고 밝음.

---

### 3.3 Golden Hour — 노을

| 항목 | 내용 |
|------|------|
| **progress** | ≥ 0.55 |
| **HUD** | `GOLDEN HOUR` |
| **나레이션** | “하늘이 주황으로 물든다. 노을 전에 꼭 도착해야 해.” |
| **라이팅** | sky≈(0.95,0.55,0.28) · sun warm · pitch↓ · intensity 살짝↓ (`DynamicEnvironmentManager`) |
| **BGM** | `BGM_Golden` (Run에서 크로스페이드) |
| **Amb** | 파도 유지, 바람 톤을 **따뜻하게** (하이 주파수 살짝) |
| **분위기** | 아름다움 + **마감 시간 압박** — 서두르되 절박하지 않게 |

---

### 3.4 Blue Hour Approach — 블루아워

| 항목 | 내용 |
|------|------|
| **progress** | ≥ 0.88 |
| **HUD** | `BLUE HOUR` |
| **나레이션** | “보랏빛 송전탑… 조금만 더. 기다려줘.” |
| **라이팅** | deep blue sky · cool sun · intensity↓ · pitch 낮음/마이너스 |
| **원경** | 송전탑 실루엣 강조 (`TransmissionTower` / DestinationGate) |
| **BGM** | `BGM_Blue` |
| **분위기** | 숨 죽인 기대 · 보랏빛 · “거의 다 왔다” |

---

### 3.5 Arrival — 우리의 송전탑 (단일 엔딩)

| 항목 | 내용 |
|------|------|
| **트리거** | S20 clear → `SceneFlow` → `04_Ending` (`EndingController`) |
| **엔딩** | 단일 시퀀스 4분 — 재회/해설/분기 없음 (`엔딩_개정안_v2.md`) |
| **BGM** | `BGM_End_Arrival` → dig piano → `BGM_End_Letter` → `BGM_End_Descent` → credits silence → `BGM_Sting_Radio` |
| **연출** | 도착(캠 고정) · 지표 모호 2초 · 편지(손+종이) · 폰 3초(이름 잘림) · 하강 플레이어블 · 검은 스팅어 |
| **금지** | 도윤 현재 등장 · 나레이션 해설 · 진엔딩 · 회상 100% 보상 텍스트 |
| **종료** | `CoastRun_Cleared` → 타이틀(클리어 버전) |

---

## 4. 랜드마크 20선 — 추억 독백 × 계절 × 분위기

진행 중 `LandmarkManager`가 `progress`에 맞춰 **1회씩** 팝업.  
UI: `UI_Panel_Memory` · 제목+독백 · BGM duck.

| # | trigger | 제목 | 독백 | 권장 계절 톤 | 분위기 메모 |
|---|---------|------|------|--------------|-------------|
| 1 | 0.04 | 출발 · 정류장 | 버스가 안 오면, 보드가 답이지. | 여름 낮 | 프롤로그 잔향, 가벼운 웃음 |
| 2 | 0.08 | 봄 · 벚꽃 가로수 | 분홍 비 같던 그 봄… 같이 뛰었지. | **봄** | 분홍·설렘 |
| 3 | 0.12 | 추억 · 방파제 | 아이스크림 녹이던 방파제. 바람이 같아. | 여름 | 짠바람·장난 |
| 4 | 0.16 | 카페 골목 | 창가 자리… 네가 먼저 고르던 곳. | 봄/여름 | 달콤·일상 |
| 5 | 0.20 | 전신주 골목 | 『비밀 기지까지 누가 먼저』 내기. | 여름 | 동심·경쟁 |
| 6 | 0.25 | 여름 · 해수욕장 | 파도 소리에 말을 삼키던 여름. | **여름** | 파도 크게 |
| 7 | 0.30 | 서프숍 앞 | 빌려 탄 보드, 무릎 까짐, 웃음. | 여름 | 유쾌 |
| 8 | 0.35 | 축제 행렬 | 오늘도 길이 막혔어. 그때도. | 여름 | P2와 공명 |
| 9 | 0.40 | 자판기 코너 | 따뜻한 캔커피 나눠 마시던 밤. | 가을 초입 | 온기 |
| 10 | 0.45 | 가을 · 낙엽 | 낙엽 밟는 소리… 네가 좋아했지. | **가을** | 바스락 SFX 약하게 |
| 11 | 0.50 | 언덕 전망 | 여기서 송전탑이 처음으로 보여. | 가을 | **목표 가시화** |
| 12 | 0.55 | 비 오던 날 | 우산 하나. 어깨가 젖었어. | 가을/비 | Golden 진입과 겹칠 수 있음 |
| 13 | 0.60 | 등대 아래 | 등대 불빛에 약속했지. 꼭 돌아오자고. | 황혼 | 약속 테마 회상 |
| 14 | 0.65 | 겨울 · 첫눈 | 첫눈에 손 잡던 날. 손가락이 시렸어. | **겨울** | 고요·차갑 |
| 15 | 0.70 | 눈 쌓인 산책로 | 보드 바퀴가 하얘지던 겨울. | 겨울 | 휠 SFX 톤 다운 |
| 16 | 0.75 | 철길 건너 | 기적 소리에 말을 멈췄던 곳. | 겨울→ | 잠깐 정적 |
| 17 | 0.80 | 노을 직전 | 하늘이 주황으로 물들기 시작해. | Golden | BGM_Golden 구간 |
| 18 | 0.85 | 블루아워 | 보랏빛… 거의 다 왔어. | Blue 직전 | 긴장↑ |
| 19 | 0.90 | 송전탑 실루엣 | 저기다. 어릴 적 비밀 기지. | Blue | 타워 비주얼 강조 |
| 20 | 0.95 | 마지막 굽이 | 조금만 더… 기다려줘. | Blue | NearMiss 응원과 동일 감정 |

---

## 5. NearMiss 응원 라인 (스토리 SFX 레이어)

짧은 HUD 텍스트 — **보이스 없이도 성립**. 추후 VO 넣는다면 동일 카피.

| 라인 |
|------|
| 조금만 더 기다려줘! |
| 아직이야… 늦지 않을 거야. |
| 송전탑까지, 한 번에! |
| 노을 전에 꼭 갈게. |
| 기다려… 금방이야! |
| 비 와도 괜찮아. |
| 눈이라도… 갈게. |
| 봄·여름·가을·겨울, 다 달려서! |
| 바퀴야, 조금만 더! |
| 그 사람… 아직 있지? |

**연출:** 콤보↑일수록 글자 살짝 크게 / 파티클만 — BGM 유지.

---

## 6. SFX · Ambient 체크리스트 (파일명)

경로: `Assets/Resources/CoastRun/Audio/`

### Ambient (루프)
| 파일 | 용도 | 챕터 |
|------|------|------|
| `Amb_Sea.ogg` | 파도 | Run~Arrival |
| `Amb_Wind.ogg` | 바람 (속도 연동) | Run~Blue |
| `Amb_TownFar.ogg` | 먼 마을/축제 (P2·축제 랜드마크) | 선택 |
| `Amb_RainSoft.ogg` | 비 날씨 밴드 | WeatherFx 연동 시 |
| `Amb_SnowHush.ogg` | 눈·정적 | Winter 밴드 |

### One-shot
| 파일 | 트리거 |
|------|--------|
| `SFX_Wheels.ogg` | 이동 중 루프/원샷 레이어 |
| `SFX_KickPush.ogg` | 가속·프롤로그 P4 |
| `SFX_NearMiss.ogg` | NearMiss 성공 |
| `SFX_SoftHit.ogg` | SoftHit (가볍고 짧게) |
| `SFX_Coin.ogg` | 코인 |
| `SFX_Upgrade.ogg` | 업그레이드 구매 |
| `SFX_Landmark.ogg` | 추억 팝업 |
| `SFX_ActSting.ogg` | Golden/Blue Act 전환 스팅 (0.5~1s) |
| `SFX_ArrivalResolve.ogg` | 도착 |
| `UI_Click.ogg` / `UI_Start.ogg` | 메뉴 |

**현재 코드 상태:** `CoastAudioManager`는 **프로시저럴만** — 위 클립 배치 후 `CoastBgmDirector`(+SFX)로 교체하는 것이 구현 작업.

---

## 7. 장면 스틸 · UI 이미지 맵

| 리소스 | 용도 |
|--------|------|
| `Scene_Frame_1.jpg` | 프롤로그 약속 |
| `Scene_Frame_2.jpg` | 프롤로그 장애 |
| `Scene_Frame_3.jpg` | 프롤로그 결심 |
| `Scene_Frame_4.jpg` | 전환 (핸드오프 직전) |
| `Scene_Frame_5.jpg` | 메뉴 히어로 / 엔딩 여운 |
| `UI_Panel_Memory.png` | 랜드마크 독백 패널 |
| `Icon_Tower.png` / `Icon_Him.png` | 목적지 HUD |
| `UI_TitleBackground.png` | 메뉴 배경 |

경로: `Assets/Resources/CoastRun/Scene/` · `Assets/Resources/CoastRun/`

---

## 8. 구현 훅 (Claude용 — “어디에 붙일지”)

코드는 만들지 말고, **연동 지점만** 기록.

| 이벤트 | 기존 클래스 | BGM/오디오 액션 |
|--------|-------------|-----------------|
| 메인메뉴 열림 | `MainMenuController` / `MainMenuBootstrap` | Play `BGM_Menu` |
| 프롤로그 시작 | `StoryManager` | Stop Menu → `BGM_Prologue` |
| 프롤로그 씬 전환 | `StoryManager` beat index | (옵션) Prologue 내 duck / stinger |
| 프롤로그 종료·핸드오프 | `CameraController.PlayGameplayHandoff` | Crossfade → `BGM_Run` |
| Act → Run/Golden/Blue | `StoryProgressDirector.SetAct` | Crossfade to matching BGM |
| DayPhase 변경 | `DynamicEnvironmentManager` | Act와 동기 — **이중 트리거 주의** (한쪽만 소유권) |
| 랜드마크 팝업 | `LandmarkManager` | Duck BGM + `SFX_Landmark` |
| NearMiss | `NearMissSystem.OnNearMissRewarded` | `SFX_NearMiss` + cheer (기존 UI) |
| SoftHit | `PlayerController.SoftHit` | `SFX_SoftHit` |
| 도착 | `GameSession.EndRun` / `StoryEndingController` | `BGM_Arrival` |
| 메뉴 복귀 | 엔딩 후 | `BGM_Menu` |

**권장 단일 소유자:** `CoastBgmDirector`가 Act(+Menu)만 듣고, Ambient/SFX는 `CoastAudioManager` 확장.

**PlayerPrefs:** `CoastRun_SkipPrologue` — Skip 시 Menu→Run 직행, BGM도 Menu→Run 크로스페이드.

---

## 9. 감정 아크 그래프 (제작 가이드)

```
감정
설렘 ┤ ●Menu
     │   ╲
초조 ┤     ●P2
결의 ┤       ●P3──●P4
청량 ┤            ████ Run (계절 추억 물결)
그리움┤              ～～ landmarks ～～
황홀 ┤                    ██ Golden
긴장 ┤                         ██ Blue
안도 ┤                            ●Arrival
     └──────────────────────────────────→ 시간
```

음악 에너지: Menu(중) → Prologue(중하) → Run(고) → Golden(중고·따뜻) → Blue(중·공간감) → Arrival(저·해결).

---

## 10. 우선순위 체크리스트 (제작·연동)

### P0 — 플레이 체감 필수
- [ ] `BGM_Menu` / `BGM_Run` / `BGM_Golden` / `BGM_Blue` / `BGM_Arrival`
- [ ] Act 크로스페이드 연동 (`StoryProgressDirector`)
- [ ] `Amb_Sea` + `Amb_Wind` + `SFX_Wheels` (속도 연동)
- [ ] NearMiss / SoftHit / Coin SFX
- [ ] 프롤로그 4스틸 + 캡션 (이미 있음) + Prologue BGM

### P1 — 스토리 몰입
- [ ] `BGM_Prologue`
- [ ] Landmark duck + `SFX_Landmark`
- [ ] Act stinger (`SFX_ActSting`)
- [ ] Arrival resolve SFX
- [ ] TransmissionTower 비주얼 확정 (WARN 상태였음)

### P2 — 폴리시
- [ ] 계절 Ambient (비/눈)
- [ ] 축제/마을 far amb
- [ ] VO (응원·독백) — 없어도 텍스트로 완결
- [ ] 프롤로그 전용 미세 SFX (폰/보드)

---

## 11.  megl 금지 (톤 일관성)

- 공포·디스토피아·과한 EDM 드롭
- SoftHit 시 BGM 정지/무음
- 템플런식 과도한 실패 징벌음
- Arrival에서 코믹한 팡파르 (해결은 **조용한 피아노**)
- 가로 화면 기준 믹스 (항상 **세로 720×1280** 플레이 가정)

---

## 12. 관련 파일 인덱스

| 문서/코드 | 역할 |
|-----------|------|
| `Assets/_Guide/Story_OurPowerTower.md` | 스토리 진행 요약 |
| `Assets/_Guide/StyleBible.md` | 비주얼·사운드 톤 한 줄 |
| `Assets/_CoastRun/Scripts/Story/StoryConfig.cs` | 카피·임계값 소스 |
| `Assets/_CoastRun/Scripts/Story/StoryAct.cs` | Act enum |
| `Assets/_CoastRun/Scripts/Story/StoryProgressDirector.cs` | Act 오케스트레이션 |
| `Assets/_CoastRun/Scripts/Story/StoryManager.cs` | 프롤로그 |
| `Assets/_CoastRun/Scripts/Story/StoryEndingController.cs` | 도착 엔딩 |
| `Assets/_CoastRun/Scripts/Story/LandmarkManager.cs` | 추억 독백 |
| `Assets/_CoastRun/Scripts/Story/DynamicEnvironmentManager.cs` | 낮/노을/블루 |
| `Assets/_CoastRun/Scripts/Audio/CoastAudioManager.cs` | 현재 프로시저럴 (교체 대상) |
| `Assets/Resources/CoastRun/` | 런타임 로드 루트 |

---

*작성 기준일: 프로젝트 StoryConfig / StyleBible 동기화본. Claude 구현 시 이 문서의 파일명·트리거·볼륨 표를 우선하고, 카피 변경이 필요하면 StoryConfig와 이 문서를 함께 갱신할 것.*
