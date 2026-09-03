# 『347』 — 빈 씬 세팅 가이드

지반이 뒤에서부터 무너지는 도시. 배달 라이더 서도하가 보드로 서쪽 집하장까지 내려간다. 세계관은 [Story.md](Story.md), 규칙과 숫자는 [Systems.md](Systems.md), 실행 절차는 [TestPlay.md](TestPlay.md).

길은 **90도로 꺾입니다.** 좌표는 월드 Z가 아니라 **경로 거리 + 현재 yaw** 기준입니다.

빈 씬에서 Play만 눌러도 `GameBootstrap` 이 아래를 전부 만듭니다. 이 문서는 **손으로 씬을 짤 때** 필요한 값입니다.

---

## 0. 입력

**Edit > Project Settings > Player > Other Settings > Active Input Handling** 을 `Input Manager (Old)` 또는 `Both` 로 둡니다. `Input System Package (New)` 만 켜져 있으면 키보드/스와이프가 먹지 않습니다.

## 1. 태그

**Edit > Project Settings > Tags and Layers**

| 태그 | 용도 |
|------|------|
| `Player` | 도하 |
| `Obstacle` | 정체 차량, 잔해. 충돌 시 HP `-1` |
| `Supply` | 수집물 8종. 트리거로 수집 |

## 2. 라이팅

씬에 거대한 Ground Plane은 두지 마세요. 바닥은 도로 타일입니다.

1. `Directional Light` Rotation `(50, -30, 0)`
2. `Main Camera` Clear Flags **Solid Color**. 색은 `DayNightCycle` 이 Play 중 덮어씁니다
3. Fog Mode `Exponential`. 밀도는 `ZoneDirector` 가 구역별로 조절합니다

**스카이박스**: `DayNightCycle` 이 `Resources/Sky/Sky_MistyGrey.hdr` (CC0) 를 `Skybox/Panoramic` 으로 물립니다. 갈아 끼울 때는 **등장방형(2:1)** 파노라마를 쓰세요. 없으면 `Concept/Skybox_CoastApoc.jpg` → 단색으로 폴백합니다.

**노면 텍스처**: `ArtLibrary` 가 `Resources/Textures` 의 아스팔트·콘크리트(CC0)를 캐시해 임시 타일에 물립니다. 재질은 프리팹 간에 공유되고 구역 색은 `MaterialPropertyBlock` 으로 덧입혀지므로 재질 인스턴스가 늘지 않습니다. GLB 타일은 자기 재질을 그대로 씁니다.

---

## 3. 도로

### 3-1. 직선 타일

`Assets/Resources/Tracks/` — `Track_Straight.glb`, `Track_Cracked.glb`, `Track_CoastEdge.glb`

Unity는 GLB를 기본으로 못 읽습니다. **Package Manager**에서 `glTFast` (`com.unity.cloud.gltfast`) 를 설치한 뒤 프로젝트를 다시 포커스하세요.

`RoadSpawner` 는 Track Prefabs가 비어 있으면 이 폴더를 자동으로 씁니다. 시작 두 칸은 `Track_Straight` 만 깔립니다.

**구역별 필터**: `ZoneDirector.AllowsTile` 이 파일명 토큰으로 후보를 좁힙니다. 새 타일은 이름에 토큰을 넣으면 코드 수정 없이 붙습니다.

| 구역 | 토큰 (우선순위 순) |
|------|-------------------|
| 1 상가 골목 | `Arcade`, `Straight`, `Cracked` |
| 2 고가도로 | `Overpass`, `CoastEdge`, `Edge`, `Straight` |
| 3 침수 지하상가 | `Flooded`, `Cracked` |
| 4 주거 타워 | `Tower`, `Straight` |
| 5 집하장 | `Depot`, `Straight`, `Cracked` |

### 3-2. 코너 타일

`Track_CornerL.glb` / `Track_CornerR.glb` 가 `Resources/Tracks` 에 있으면 자동으로 씁니다. 없으면 `TestCatalog` 가 CC0 아스팔트를 입힌 L자 큐브로 대신합니다 — 회전 테스트에는 지장이 없습니다.

`TrackSegment` 인스펙터:

| 필드 | 값 |
|------|-----|
| Kind | `Straight` / `CornerLeft` / `CornerRight`. 비우면 이름의 `CornerL` / `CornerR` 로 자동 판정 |
| Length | 직선 타일 길이 `30`. 코너는 무시 |
| Corner Arm | 진입→코너 중심, 코너 중심→출구 거리 `15`. 코너 경로 길이는 `30` |
| Road Half Width | `5` |

코너 로컬 규격: 진입 원점 `+Z`, 코너 중심 `(0, 0, 15)`, 출구 `(±15, 0, 15)`, yaw 델타 `±90`. 콜라이더가 없으면 L자 박스 2개를 자동으로 붙입니다.

### 3-3. 장애물

`Assets/Resources/Hazards/` — `Wreck_Car`, `Wreck_Van`, `Debris`, `Barrier_Low`, `FallenPole`, `TireStack`

Obstacle Prefabs를 비우면 폴더를 통째로 씁니다. 태그 `Obstacle` 은 스폰 시 붙습니다. 이름에 `Barrier` 가 들어간 것만 슬라이드로 무피해 통과이고, 나머지는 HP `-1` 입니다.

### 3-4. 수집물

`Assets/Resources/Items/` — 파일명이 곧 종류입니다 ([Pickup.KindFromName](../Scripts/Pickup.cs)).

| 이름 패턴 | `PickupKind` |
|-----------|--------------|
| `Item_Coin`, `Supply_Can`, `Supply_Bottle` | `Coin` |
| `Item_Tag`, `SupplyCrate_Med` | `Tag` |
| `Item_BoosterCell`, `SupplyCrate_Food` | `BoosterCell` |
| `Item_Shield` | `Shield` |
| `Item_ReverseScan` | `ReverseScan` |
| `Item_DeckTape` | `DeckTape` |
| `Item_DeckPiece` | `DeckPiece` |
| `Item_Letter` | `Letter` |

GLB가 없으면 `TestCatalog` 가 8종 프리미티브를 만듭니다. 태그 `Supply` + Is Trigger + `Pickup` 은 스폰 시 붙습니다.

### 3-5. 노변 장식

`Assets/Resources/Props/` — 타일마다 차선(폭 10 m) **밖**에만 깔리고 콜라이더는 꺼집니다.

---

## 4. 플레이어

포즈 PNG는 `Assets/Resources/Character/`. `PlayerSpriteView` 가 `Doha_*` 를 먼저 찾고 없으면 `Girl_*` 로 폴백합니다.

1. **3D Object > Capsule**, 이름 `Player`, **Tag = Player**
2. Position `(0, 1.15, 2)`. Capsule Collider **Remove**. **Character Controller**: Height `2`, Radius `0.4`, Center `(0, 0, 0)`
3. **Player Controller** + **Player Vitals** + **Player Sprite View** 추가

`PlayerController`:

| 필드 | 값 |
|------|-----|
| Base Speed / Max Speed | `9` / `20` |
| Speed Per Metre | `0.0035` |
| Lane Offset | `2.5` |
| Jump Force / Gravity | `8` / `-20` |
| Slide Duration / Height / Radius | `0.7` / `0.5` / `0.22` |
| Turn Grace | `3` |
| Hurt Stun Seconds | `0.3` |
| Swipe Threshold | `50` |

`PlayerVitals`:

| 필드 | 값 |
|------|-----|
| Max Hp | `3` |
| Invuln Seconds | `1.2` |
| Hit Speed Factor | `0.7` |
| Speed Recover Seconds | `1.5` |
| Revive Invuln Seconds | `3` |

`PlayerSpriteView` 는 `Deck_Ok` / `Deck_Cracked` / `Deck_Broken` 을 HP에 따라 전환하고, 무적 프레임에는 흰색으로 깜빡입니다. 슬롯은 비워 두면 Resources에서 자동 로드합니다.

---

## 5. 카메라

`Main Camera` 에 **Camera Follow**:

- Target `Player`
- Offset `(0, 4.5, -9)` — 카메라 yaw 로컬로 적용
- X Damping `0.25` · Yaw Damping `0.15` (코너 스윙)
- Base Fov / Fast Fov `60` / `72`
- Fall Pitch `34`
- Look At Player 끄기

피격·사망 시 `Shake(강도, 초)` 로 짧게 흔들립니다. 붕괴선이 가까우면 화면이 탈색됩니다.

---

## 6. 매니저

빈 오브젝트 `GameSystems` 하나에 아래를 모두 붙입니다.

### 6-1. GameManager

| 필드 | 값 |
|------|-----|
| Player | `Player` |
| Depot Distance | `5000` (테스트 `400`). 월드 Z가 아니라 **경로 거리** |
| Course Seed | `0` = 매번 새 시드. 값을 넣으면 코스가 고정됩니다 |
| Coin Score | `10` |
| Risk Score Per Second | `30` (붕괴선 `20 m` 이내) |
| Grind Score Per Second | `25` |
| Risk Band Metres | `20` |

`PlayerPrefs`: `r347_run_count` (회차) · `r347_best_score` · `r347_deck_pieces`.

### 6-2. ZoneDirector

| 필드 | 값 |
|------|-----|
| Blend Metres | `220` |
| Follow Target | `Player` |
| Emitter Offset / Box | `(0, 6, 8)` / `(14, 10, 22)` |
| 팔레트 5개 | Arcade / Overpass / Flooded / Tower / Depot |
| 파티클 텍스처 | 비우면 `Resources/FX` 의 Leaf / Mote / Snow / Petal 자동 |

구역당 `1600 m` ([Zone.MetresPerZone](../Scripts/Zone.cs)). 구역 진입 시 `OnZoneChanged` → 배너 + 무전 비트 + `ZoneCleared` 플래그.

### 6-3. DayNightCycle

- Day Length Seconds `180` · Start Hour `17`
- Sun = Directional Light · Target Camera = Main Camera · Drive Camera Background 켜기

`ZoneDirector.FilterEnvironment` 가 구역 색을 위에 덧입힙니다.

### 6-4. RoadSpawner

빈 오브젝트 `RoadSpawner` + **Road Spawner**

| 필드 | 값 |
|------|-----|
| Player | `Player` |
| Track Prefabs | 비우면 `Resources/Tracks` 자동 |
| Safe Prefab / Safe Initial Count | `Track_Straight` / `2` |
| Initial Spawn Count / Pool Size | `6` / `4` |
| Spawn Ahead Distance | `90` (경로 거리) |
| Recycle Distance | `40` (붕괴선이 뒤를 쓰므로 이보다 내리지 마세요) |
| Lane Offset | `2.5` |
| Obstacle / Supply Prefabs | 비우면 `Resources/Hazards` · `Resources/Items` 자동 |
| Obstacle Chance / Supply Chance | `0.85` / `0.45` |
| Min Straight Between Corners | `2` |
| Corner Chance Start / End | `0.14` / `0.42` |
| Goal Straight Margin | `60` (집하장이 코너에 박히지 않게) |

밀도는 고정값이 아니라 `SpawnDirector.ObstacleScale` 로 흔들립니다. `TrackManager` 는 더 이상 쓰지 않습니다.

**런타임 스위치** (코드에서만): `ForceStraight` · `SuppressHazards` — 왕전 아레나가 씁니다.

### 6-5. CollapseLine

빈 오브젝트 `CollapseLine` + **Collapse Line**. `GameBootstrap` 의 Spawn Collapse Line 이 켜져 있으면 자동 생성.

| 필드 | 값 |
|------|-----|
| Start Gap | `45` |
| Pace Factor | `0.96` (플레이어 속도 배수) |
| Hit Penalty / Counter Reward | `12` / `6` |
| Warn Gap / Kill Gap | `15` / `0.5` |
| Emitter Height | `3.5` |

경로 뒤에서 콘크리트 먼지와 아스팔트 파편을 앞으로 날립니다. 삼켜지면 `DeathCause.Collapsed`.

### 6-6. RetrievalDrones

빈 오브젝트 `RetrievalDrones` + **Retrieval Drones**.

| 필드 | 값 |
|------|-----|
| Drone Count | `3` |
| Idle Gap / Close Gap / Catch Gap | `34` / `6` / `1.2` |
| Lock On Tags / Full Lock Tags | `6` / `24` |
| Height | `4.2` |

**회수 태그 보유량이 곧 추적 정확도**입니다. 접촉하면 `DeathCause.Retrieved`.

### 6-7. ItemSlot

`GameSystems` 에 **Item Slot**. 1칸, 덮어쓰기.

| 필드 | 값 |
|------|-----|
| Booster Seconds / Speed Bonus / Gap Bonus | `4` / `+0.4` / `+30 m` |
| Shield Seconds | `8` |
| Scan Seconds | `6` |

발동은 `Space` / `LeftShift` / 화면 탭.

### 6-8. StoryEngine

`GameSystems` 에 **Story Engine**. `Resources/Story/events.json` 을 로드하고 [StoryScript](../Scripts/StoryScript.cs) 의 코드 이벤트를 합칩니다. 채널 우선순위 `Control` > `Radio` > `AmbientVoice`.

### 6-9. GameAudio

`Resources/Audio` 를 이름으로 자동 로드합니다. 엔진 소리는 없습니다. 붕괴선 간격이 좁아지면 럼블이 커지고, HP가 낮으면 데크 삐걱임이 상시로 깔립니다.

### 6-10. DepotGate

`GameManager` 가 Play 시 자동 생성합니다. `Resources/Depot/DepotGate` 를 먼저 찾고 없으면 `Resources/Tower/RadioTower` 로 폴백합니다. `TrackPath` 에 목표 구간이 생기는 순간 그 도로 위로 스스로 이동하고, 그 전까지는 렌더러와 오디오가 꺼져 있습니다. 꼭대기 `Beacon` 이 없으면 점광을 붙여 스캐너처럼 좌우로 훑습니다.

### 6-11. KingFight

씬에 미리 두지 않습니다. `F2` 또는 `Tools > 347 > Play King Arena` 로 아레나에서 생성됩니다.

| 필드 | 값 |
|------|-----|
| Aim / Throw / Counter Seconds | `0.6` / `0.5` / `1.4` |
| Min Recover / Stagger Seconds | `0.25` / `0.8` |
| Hp Per Phase | `3` (총 9) |
| Arena Speed | `14` 고정 |
| Base Distance / Distance Swing | `74` / `±14` |
| Stagger Close In | `30` |
| Reverse Rules | 5구역 역주행용. 기본 꺼짐 |

---

## 7. UI

Canvas (Screen Space Overlay). 텍스트는 **UI > Legacy > Text** (`UnityEngine.UI.Text`). TextMeshPro는 이 스크립트 타입과 맞지 않습니다.

### 7-1. ProloguePanel

반투명 디머 + 자식 `Card` 에 `UI_Panel_Dark` 9-slice. 자식 Text `PrologueText` 는 비워도 됩니다 — 스크립트가 편의점 오프닝을 넣고 **3초** 뒤 닫습니다.

### 7-2. HudRoot

| 위치 | 요소 | 예 |
|------|------|-----|
| 좌상단 | `ZoneText` | `구역 2 · 고가도로` |
| 좌상단 아래 | `HpText` (숫자 옵션) | `데크 3` |
| 상단 중앙 | `DistanceText` | 1~4구역 `주행 1,840 m` · 5구역 `집하장까지 320 m` |
| 우상단 | `ScoreText` | `점수 1,840   최고 3,102` |
| 우상단 아래 | `SuppliesText` | `태그 8 · 편지 2 · 조각 5` |
| 중앙 위 | `TurnHintText` | `◀ 왼쪽` / `오른쪽 ▶` |
| 중앙 아래 | `ItemText` · `ComboText` | `부스터 셀` · `×1.8` |
| 하단 | `SubtitleText` | `청소부  …너 전에도 여기서 물어봤던 것 같은데.` |
| 중앙 | `BannerText` | 구역 진입 배너, 2초 페이드 |
| 상단 | `KingText` | 왕전에서만 |

거리 표시가 **5구역에서만 목표로 바뀌는** 게 중요합니다. 도하가 동쪽으로 도망하다 서쪽으로 방향을 트는 설정이라, 목표가 그때 생겨야 맞습니다.

### 7-3. 화면 효과

- `CollapseVeil` — 화면 전체 이미지. 붕괴선 간격 `15 m` 이내에서 알파가 오르며 탈색
- `HurtVignette` — 가장자리 붉은 비네트. **루트에 `CanvasGroup`** 을 두고 `UIManager` 가 알파를 조절합니다 (자식 이미지가 여러 장이라 `Image.color.a` 로는 안 됩니다)

### 7-4. GameOverPanel — 회수 로그

꺼 둔 상태로. 디머 + Card.

- `GameOverText` — 회수 로그 한 줄. `「A-0347 회수. 구역 2 · 고가도로, 거리 1,840 m. 기록 보관.」`
- `GameOverScore` — `회차 12 · 점수 1,840 · 최고 3,102 · 태그 8`
- 버튼 `다시 달리기` (`GameManager.Restart`)

### 7-5. EndingPanel

꺼 둔 상태로. 스크립트가 `GameManager.EndingCopy()` 로 3분기 문안을 넣습니다.

- `EndingScore` — `회차 12 · 점수 · 최고 · 편지 n`
- 버튼 `목록에 다시 오르기` (`UIManager.ContinueRun`) 와 `다시 달리기` (`GameManager.Restart`)

히든(347회차)에서는 계속 달리기 버튼이 숨습니다.

### 7-6. UI Manager 인스펙터

- Prologue Panel / Prologue Text / Prologue Seconds `3`
- Hud Root / Zone / Hp / Distance / Supplies / Score / Turn Hint / Item / Combo
- Subtitle Text / Banner Text
- Collapse Veil / Hurt Vignette
- King Text
- Game Over Panel / Text / Score
- Ending Panel / Text / Score / Continue Button

**File > Build Settings > Add Open Scenes** 를 해야 Restart의 LoadScene이 빌드에서 동작합니다.

---

## 8. 물리

- 장애물은 반드시 태그 `Obstacle`. Untagged면 부딪혀도 아무 일이 없습니다
- 이름에 `Barrier` 가 들어간 것만 슬라이드 판정입니다
- 수집물은 태그 `Supply` + Is Trigger + `Pickup`
- 도로 Collider가 있어야 점프가 됩니다
- 추락 사망 시 CharacterController를 끄고 Transform으로 떨어뜨립니다. `detectCollisions` 를 끄는 것만으로는 `Move()` 자기 충돌이 남습니다

---

## 9. 조작

| 입력 | 동작 |
|------|------|
| A / D / 좌우 스와이프 | 직선에서 3차선, **코너 타일 위에서는 회전** |
| W / 위 스와이프 | 점프 |
| S / 아래 스와이프 | 슬라이드 |
| Space / LeftShift / 탭 | 아이템 슬롯 |
| — | 반격은 **자동** |
| F2 / F3 | 왕전 아레나 / 러너 |

상태 우선순위는 `DEAD > COUNTER > INVULN > HURT > GRIND > WALLRUN > AIR > SLIDE > RUN` 입니다. 피격 스턴 중에도 회전과 차선 이동은 됩니다 — 코너에서 억울하게 죽지 않도록.

코너 규칙: 반대 방향 = 벽 즉사, 무입력 = 코너 중심 `3 m` 통과 후 추락. 회전은 yaw를 즉시 90도 스냅하고 기준 프레임을 코너 중심으로 옮깁니다.

---

## 10. 에디터 메뉴

`Tools > 347`

| 항목 | 단축키 | 하는 일 |
|------|--------|---------|
| Play Runner | `Ctrl+Shift+R` | 러너로 Play |
| Play King Arena | `Ctrl+Shift+K` | 왕전 아레나로 Play |
| Toggle Numeric HP | | 데크 그림 대신 숫자 HP |
| Toggle Slow Telegraphs | | 보스 텔레그래프 `×1.5` (읽기 검증용) |
| Reset Save Data | | 회차·최고 점수·데크 조각·영구 플래그 삭제 |

콘솔에 `RoadSpawner: assign track prefabs` 가 뜨면 Track Prefabs 슬롯이 비어 있고 `Resources/Tracks` 도 비어 있는 것입니다.
