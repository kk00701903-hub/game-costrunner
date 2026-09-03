# 『347』 테스트 실행

**가장 빠른 방법 (Windows)**

```powershell
cd c:\dev\game
.\playtest.ps1          # Unity + Run 씬 열기
.\playtest.ps1 -Play    # 열고 바로 Play (러너)
.\playtest.ps1 -Arena   # 열고 바로 Play (왕전 아레나)
```

또는 `playtest.bat` 더블클릭.

Unity가 이미 열려 있으면:

| 단축키 | 메뉴 | 동작 |
|--------|------|------|
| `Ctrl+Shift+O` | Tools > 347 > **Open Run Test Scene** | 테스트 씬 열기 |
| `Ctrl+Shift+R` | Tools > 347 > **Play Runner** | 세로 Game View + 러너 Play |
| `Ctrl+Shift+K` | Tools > 347 > **Play King Arena** | 세로 Game View + 아레나 Play |

HUD/UI 확인 전: **Tools > 347 > Galaxy S26 Game View (1080×2340)**  
에셋 확인: **Tools > 347 > Verify UI Art Loaded**

---

이 폴더(`c:\dev\game`)를 Unity로 열고 **Run 씬에서 Play** 해도 됩니다. `GameBootstrap` 이 플레이어·카메라·UI·도로·붕괴선·드론·스토리 엔진을 다 만듭니다.

테스트 씬: `Assets/_Project/Scenes/Run.unity` (Build Settings 0번)

## 화면이 회색 + 캐릭터만 보일 때

| 증상 | 원인 | 해결 |
|------|------|------|
| ▶ Play **안 눌린** 상태 | Game 탭은 Play 전엔 거의 비어 보임 | **Ctrl+Shift+R** 또는 ▶ Play |
| Hierarchy에 **RoadSpawner 없음** | 아직 Play 안 함 / Untitled 씬 저장됨 | **File > New Scene** 저장 안 함 → **Ctrl+Shift+O** (Run 씬) → Play |
| Play 중인데 도로·하늘 없음 | URP 미연결 / GLB 셰이더 | **Tools > 347 > Setup Visual Pipeline** → 다시 Play |
| 프롤로그만 안 보임 | 정상 — 탭/키로 스킵 | 화면 탭 또는 아무 키 |

Play 중 Hierarchy에 **RoadSpawner · Canvas · CollapseLine** 이 생기면 정상입니다.

## 열기

1. Unity Hub → **Open** → `c:\dev\game`
2. 패키지 설치가 끝날 때까지 대기 (`glTFast`, `ugui`)
3. **Edit > Project Settings > Player > Active Input Handling** = `Both` 또는 `Input Manager (Old)`
4. **Tools > 347 > Play Runner** (`Ctrl+Shift+R`) 또는 Run 씬에서 **Play**

GLB가 없어도 `TestCatalog` 프리미티브로 전 규칙이 돌아갑니다.

## 3D 캐릭터 모드

| 항목 | 확인 |
|------|------|
| 기본 | `GameConfig.use3DCharacter = true` → 3D placeholder 또는 `Resources/Character/Doha/DohaModel` |
| 2D 폴백 | `Resources/347/GameConfig` 에서 `use3DCharacter` 끄기 → `Girl_*.png` 스프라이트 |
| 그림자 | 발 아래 blob shadow (CharacterShadow) |
| 애니 | FSM과 Speed/Grounded/Slide/Jump/Lean 동기 (Animator 또는 procedural) |
| Store 캐릭터 | Humanoid Import 후 `DohaModel.prefab` 으로 배치 → **Validate Art** |

비주얼 파이프라인: **Tools > 347 > Setup Visual Pipeline (URP + Materials + Volume)**  
무료 CC0 에셋: **Tools > 347 > Import Free CC0 Assets** (Kenney → Resources 매핑)  
가이드: [`Visual.md`](Visual.md) · [`FreeAssetMap.json`](FreeAssetMap.json)

## 두 가지 모드

| 진입 | 모드 | 무엇이 남는가 |
|------|------|--------------|
| Play, 또는 `F3`, 또는 `Tools > 347 > Play Runner` | **러너** | 전부 |
| `F2`, 또는 `Tools > 347 > Play King Arena` | **왕전 아레나** | 수거자 규칙만. 직선 강제, 장애물 꺼짐, 속도 `14` 고정 |

보스 타이밍을 만질 때는 아레나로 들어가세요. 코너와 장애물이 사라져서 반격 창만 남습니다.

## 테스트에서 달라진 점

- 집하장까지 **400 m** (본편 5000 m). 엔딩을 빨리 봅니다. 구역은 여전히 1600 m 단위라 테스트에서는 1구역만 봅니다 — 구역 전환을 보려면 `GameBootstrap` 의 `Test Depot Distance` 를 `9000` 으로 올리세요
- 프롤로그 ~3초(또는 **탭하여 시작**) 후 출발
- HUD는 **Galaxy S26 — 1080×2340 (19.5:9)** 기준: 상단 중앙 금색 점수, 데크 3핍, 좌하 아이템 슬롯, 하단 무전 자막. Safe Area(펀치홀·제스처) 자동 여백
- UI/컨셉 이미지는 `Assets/Resources/UI` · `Concept` 에 있음. **Pexels API** — `.env` 키 설정 후 **Tools > 347 > Fetch Pexels Images** 또는 [`PexelsImagePack.json`](PexelsImagePack.json) 검색어로 **같은 경로에 덮어쓰기** (코드 수정 불필요)
## UI 확인 포인트

| 화면 | 보면 되는 것 |
|------|-------------|
| 프롤로그 | 하단 카드 + 펄스 CTA, 탭으로 즉시 스킵 |
| 주행 | 점수 카운트업, 구역/최고점 코너, 중앙은 비움 |
| 피격 | 데크 핍 색 변화(청→주→적), HP1 비네트 |
| 아이템 | 좌하 슬롯 펄스 / 발동 중 원형 게이지 |
| 사망 | 「회수 로그 · A-0347」 스탬프 + 다시 달리기 CTA |
## 조작

| 입력 | 동작 |
|------|------|
| A / D / 좌우 스와이프 | 직선에서 3차선, **코너 타일 위에서는 회전** |
| W / 위 스와이프 | 점프 |
| S / 아래 스와이프 | 슬라이드 |
| Space / LeftShift / 탭 | 아이템 슬롯 발동 |
| — | **반격은 자동.** 열린 차선에 들어가 있으면 됩니다 |

입력 버퍼 `0.12초` + 코요테 타임 `0.10초` 라 착지 직전에 눌러도 먹습니다.

## 코너 규칙

| 상황 | A / D 입력 |
|------|-----------|
| 직선 타일 | 3차선 이동 |
| 코너 타일 위 (`◀ 왼쪽` / `오른쪽 ▶` 힌트) | 회전 예약 |

- 힌트가 뜬 동안 **맞는 방향**을 누르면 코너 중심에서 자동으로 꺾입니다. 일찍 눌러도 됩니다
- **반대 방향** = 바깥 벽 즉사
- 무입력 = 코너 중심 `3 m` 통과 후 추락
- 코너에는 장애물이 없고 회전 안쪽에 코인 3개가 깔립니다
- 같은 방향 3연속 금지, 코너 사이 직선 2개 — 길이 스스로 겹치지 않습니다

## 데크 HP

즉사가 아니라 **3칸**입니다.

- 맞으면 HP `-1`, 속도 `×0.7` (1.5초 회복), 무적 `1.2초`
- 표시는 숫자가 아니라 데크 상태 + 붉은 비네트. 숫자를 보려면 `Tools > 347 > Toggle Numeric HP`
- HP 0에서 **편지가 있으면 태워서 부활** (무적 3초). 태운 편지는 트루 엔딩 카운트에서 빠집니다
- 큰 잔해·차·전봇대·타이어도 이제 즉사가 아니라 HP 1 감소입니다

## 두 종류의 추격

| 추격자 | 규칙 | 사망 |
|--------|------|------|
| 붕괴선 | 플레이어 속도 `×0.96`. 피격마다 `-12 m`, 반격마다 `+6 m` | `0.5 m` → 회수 로그 「지반 붕괴」 |
| 회수 드론 3기 | **회수 태그 보유량 = 추적 정확도.** 태그 6개부터 붙고 24개에서 완전 조준 | `1.2 m` → 「회수됨」 |

붕괴선은 속도로 도망칠 수 없고 **안 맞으면** 잡히지 않습니다. 드론은 반대로 **점수를 벌면** 붙습니다.
간격 `15 m` 안쪽이면 화면이 탈색되고 무전이 깨집니다.

## 확인할 것

1. 프롤로그가 편의점 문구로 뜨고 회수반 존댓말로 닫힘
2. 좌우 3차선, 점프, 슬라이드. 낮은 바는 슬라이드하면 무피해
3. 부딪히면 즉사가 아니라 HP가 줄고 데크에 금이 감. 3번 맞으면 회수 로그
4. 코너 회전 / 무시하면 추락 / 반대로 꺾으면 벽. 회전 후에도 도로가 어긋나지 않음
5. 붕괴선이 피격마다 확 가까워지고, `15 m` 안에서 화면이 탈색됨
6. 태그를 모으면 점수가 오르는 **동시에** 드론이 붙기 시작
7. 아이템을 주우면 슬롯이 갱신되고(1칸 덮어쓰기) Space로 발동됨
8. HP가 낮을 때 응급 테이프가 더 자주 나오고, 안전 차선이 하나 열림
9. 무전 자막이 화자별 색으로 뜨고, 배터리가 죽으면 청소부·붕어가 지글거림으로 바뀜
10. 구역이 바뀌면 배너 + 팔레트 + 파티클 + **도로 타일 후보**가 함께 전환
11. 사망 화면이 `GAME OVER` 가 아니라 회수 로그 형식이고, 회차가 1 오르며 재시작 후에도 누적
12. 집하장 도달 시 편지 개수에 따라 `회수 완료` / `목록 밖` 으로 갈림
13. 같은 시드로 재시작하면 같은 코스가 나옴

## 왕전 확인할 것

`F2` 로 들어가서:

1. `Aim(0.6s) → Throw(0.5s) → Counter(1.4s) → Recover(≥0.25s)` 가 눈으로 읽힘
2. 조준 차선에 마커가 뜨고, 텔레그래프가 `0.45초` 아래로 안 내려감
3. 반격 차선에 서 있으면 **버튼 없이** 반격이 나감
4. 페이즈당 3회 반격, 총 3페이즈 9회
5. 경직 `0.8초` 동안 수거자가 `30 m` 가까워짐
6. 페이즈가 바뀔 때마다 회복 아이템이 하나 떨어짐
7. 아레나에서 죽어도 들어올 때의 아이템 구성으로 복구됨

## 초기화

`Tools > 347 > Reset Save Data` — 회차·최고 점수·데크 조각·영구 플래그를 전부 지웁니다.

빈 씬에서도 **다시 달리기**는 런타임을 다시 만듭니다. 저장 씬이 있으면 그 씬을 다시 로드합니다.
