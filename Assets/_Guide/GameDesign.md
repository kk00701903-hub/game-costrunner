# Coast Run — 시스템 설계서

> 엔진: Unity 6 / URP · 타겟: iOS / Android 세로  
> 톤: 화창한 여름, 해안 언덕길을 내려가는 소녀 · 지브리 / 신카이풍 셀 셰이딩  
> 코드 루트: `Assets/_CoastRun/` (기존 『347』 코드와 분리)  
> **시각 표준 영상:** [`Reference/style_reference.mp4`](Reference/style_reference.mp4) · 바이블: [`StyleBible.md`](StyleBible.md)

---

## 0. 레퍼런스 영상 (게임 표준)

다운로드본 `이런_느낌의_d_게임을_커서_요청해줘 (1).mp4` 를 프로젝트에 복사해 두었다.

| 항목 | 값 |
|------|-----|
| 경로 | `Assets/_Guide/Reference/style_reference.mp4` |
| 키프레임 | `style_frame_1.jpg` … `style_frame_5.jpg` |
| 해상도 | 720×1280 세로, ~20초 |

영상에서 고정한 규칙 요약:

- **낮은 백뷰 카메라** + 하늘·적란운이 화면 상단을 지배
- **왼쪽 = 해안 마을·전신주·전선·NPC** / **오른쪽 = 방파제·바다·파도**
- 주인공은 **하단 1/3**, 올리브 배낭 + 오렌지 휠 보드
- 셀 셰이딩, 날카로운 쿨톤 그림자, 높은 채도의 청량한 여름색

세부 Lock은 **StyleBible.md** 만 본다.

---

## 1. 핵심 게임플레이 루프 & 메커니즘

### 1.1 한 줄 컨셉

**「화창한 여름날, 배낭 멘 소녀가 바다가 보이는 언덕길을 스케이트보드로 시원하게 내려간다.」**

긴장·추격이 아니라 **바람·속도·풍경**이 주인공이다. 실패해도 다시 타고 싶은 캐주얼 러닝.

### 1.2 코어 루프

```
홈 → 출발(언덕 정상) → 내려가며 회피·수집 → 골(해변/전망대)
      → 거리·콤보·수집 점수 정산 → 코스메틱/보드 파츠 → 다시 출발
```

| 단계 | 플레이어가 느끼는 것 | 시스템 |
|------|----------------------|--------|
| 출발 | 바람, 바다 원경 | 카메라 FOV·바람 VFX |
| 주행 | 좌우 레인, 점프/웅크리기 | `PlayerController` |
| 위기 | 장애물 한 번에 피함 | 짧은 감속, HP 없음(캐주얼) 또는 Soft HP 1~2 |
| 보상 | 코인·사진·시크릿 루트 | `ScoreService` / `PickupSpawner` |
| 결말 | 해변에서 보드 정지 | `RunSession` 종료 |

### 1.3 모바일 조작 (권장)

| 입력 | 동작 | 비고 |
|------|------|------|
| **좌/우 스와이프** | 3레인 이동 (L / C / R) | 차선 간격 ~2.2 m, 0.15~0.2초 보간 |
| **위 스와이프** | 점프 | 낮은 난간·상자 넘기 |
| **아래 스와이프** | 웅크리기(슬라이드) | 낮은 간판·나뭇가지 |
| **탭** | (선택) 트릭 / 사진 셔터 | 콤보용, 필수는 아님 |
| **터치 앤 홀드** | **자세 낮추기(턱)** → 가속 보너스 | 내리막 속도감, 홀드 해제 시 복귀 |
| **두 손가락 탭** | 일시정지 | 시스템 |

입력은 `IInputReader` 뒤로 숨긴다. 에디터는 WASD/화살표 폴백.

### 1.4 핵심 재미 설계

**장애물 회피**

- 타입: `LaneBlock`(한 레인), `LowBar`(슬라이드), `HighGap`(점프), `Gap`(점프 또는 감속)
- 패턴은 시드 기반 웨이브로 생성 → 매일/매 런 조금씩 다르게
- 히트 시: **HP 깎지 않고** 속도만 급감 + 짧은 흔들림 (캐주얼 톤 유지). 선택적으로 Soft HP 2칸.

**속도**

- 기본은 **내리막 중력 느낌**의 자동 가속 (`baseSpeed → maxSpeed`)
- 홀드(턱) 시 `tuckMultiplier` (예: ×1.15)
- 콤보(연속 회피·트릭) 시 짧은 속도 버스트
- 속도는 **카메라 FOV·모션 블러(약)·바람 파티클**에만 강하게 반영 (숫자 HUD 최소화)

**점수**

| 소스 | 예시 |
|------|------|
| 거리 | `metres × 1` |
| 코인/조개 | 소량 |
| 콤보 | 연속 회피 N회 |
| 시크릿 뷰포인트 | 사진 스팟 통과 |

**세션 길이**

- 첫 플레이: 60~90초 골 도달 가능
- 숙련: 사이드 루트·고콤보로 2~3분

---

## 2. 프로젝트 아키텍처 & 폴더 구조

### 2.1 원칙

- **게임플레이 코드**는 `Assets/_CoastRun/` 아래에만 둔다.
- **에셋**은 `Assets/_CoastRun/Art|Audio|Prefabs`에 두고, 런타임 로드가 필요하면 `Resources` 또는 Addressables.
- 기존 『347』 폴더(`Assets/Scripts`, `Assets/_Project`)는 레거시로 두고, 마이그레이션 후 제거한다.

### 2.2 권장 디렉터리

```
Assets/
  _CoastRun/
    Art/
      Characters/          # 소녀, 보드, 배낭
      Environment/         # 도로 타일, 언덕, 바다, 나무, 구름
      Props/               # 장애물, 코인, 간판
      VFX/                 # 바람, 먼지, 물보라
      UI/                  # HUD, 폰트, 아이콘
      Shaders/             # Cel / Outline / 바다
      Materials/
      Textures/
    Audio/
      BGM/
      SFX/
    Prefabs/
      Player/
      Environment/
      Obstacles/
      UI/
    Scenes/
      Boot.unity
      Run.unity
      Home.unity
    Scripts/
      Core/                # GameSession, ServiceLocator, Config
      Input/               # IInputReader, MobileSwipeInput
      Player/              # PlayerController, SkateVisual, AnimBridge
      Camera/              # CameraController, CameraShake
      World/               # MapGenerator, TrackSegment, EnvironmentManager
      Gameplay/            # Score, Combo, Obstacle, Pickup
      UI/                  # HudPresenter, Pause
      Rendering/           # URP helpers, QualityTier
    Data/                  # ScriptableObjects (RunConfig, ZonePalette)
    Settings/              # URP Asset, Renderer, Volume profiles
  _Guide/
    GameDesign.md          # 이 문서
  Editor/
    CoastRun/              # 임포터, 검증 메뉴
```

### 2.3 런타임 흐름

```
Boot → GameSession.StartRun()
     → MapGenerator.WarmPool()
     → Player + Camera spawn
     → Update: Input → Player → Map recycle → Score → Camera
     → Finish / SoftFail → Results → Home
```

Managers는 God Object 하나로 모으지 않는다.  
`GameSession`이 수명만 소유하고, 나머지는 인터페이스로 주입.

---

## 3. 핵심 클래스 설계

### 3.1 인터페이스

```csharp
public interface IInputReader
{
    Vector2 SwipeDelta { get; }
    bool JumpPressed { get; }
    bool CrouchPressed { get; }
    bool TuckHeld { get; }
    int LaneDelta { get; }   // -1 / 0 / +1 (한 프레임 소비)
    void Tick();
}

public interface IRunProgress
{
    float DistanceMetres { get; }
    float NormalizedSpeed { get; }
    bool IsRunning { get; }
}

public interface IMapStream
{
    bool TryGetPose(float pathDistance, out Vector3 position, out float yaw);
    void SetPlayerDistance(float pathDistance);
}
```

### 3.2 Manager / 서비스

| 클래스 | 책임 |
|--------|------|
| `GameSession` | 런 시작/종료, 일시정지, SoftFail |
| `ScoreService` | 거리·수집·콤보 집계 |
| `ObjectPool` | 타일·장애물·픽업 풀 |
| `AudioDirector` | BGM 레이어(바람/멜로디), SFX |
| `QualityTier` | Low/Med/High URP 프리셋 전환 |

### 3.3 PlayerController

**역할:** 경로 거리(`pathDistance`) 전진 + 레인 오프셋 + 점프/웅크리기 + 턱 가속.  
물리 엔진 Rigidbody는 쓰지 않는다(모바일 결정성·성능). Transform + 커스텀 중력.

```
상태: Run | Air | Crouch | Tuck | SoftHit | Finish
```

- `IInputReader`로 입력
- `IMapStream.TryGetPose`로 도로 중심선 샘플
- `Animator` / `SkateVisual`에 Speed, Grounded, Crouch, Tuck 전달
-  collisio: 간단한 AABB 또는 Trigger 콜백 → SoftHit

### 3.4 CameraController

**역할:** 3인칭 백뷰, 속도감 극대화.

| 파라미터 | 의도 |
|----------|------|
| `offset` (0, 4.2, -9.5) | 소녀가 화면 하단 1/3 |
| `lookAhead` | 진행 방향 전방 주시 |
| `fovBase` → `fovFast` | 속도에 따른 FOV |
| `yawDamping` / `lateralDamping` | 코너·레인 스무스 |
| `dutchMax` | 고속에서 아주 약한 Z 롤 (선택) |

플레이어 위치 직추적 금지 → **경로 중심 + 지연된 레인 오프셋**.

### 3.5 MapGenerator / EnvironmentManager

**MapGenerator**

- 세그먼트 큐: Straight / SoftCurveL / SoftCurveR / ScenicOverlook
- 플레이어 `pathDistance + spawnAhead`까지 스폰, 뒤로 `recycleDistance` 풀 반환
- 곡선은 Bezier/아크로 샘플 (`TryGetPose`) — Temple Run식 부드러운 코너

**EnvironmentManager**

- 바다 평면·하늘·구름 스크롤 (플레이어 따라가기)
- 구역 팔레트: Morning / Noon / GoldenHour (거리 또는 세션 타임)
- LOD: 먼 나무/바위는 빌보드 또는 저폴리
- 바람 파티클·갈매기 스폰 밀도 조절

---

## 4. 모바일 렌더링 & 최적화 (URP Cel)

### 4.1 URP 권장

| 항목 | Mid (권장 기본) | Low |
|------|-----------------|-----|
| Render Scale | 0.85~1.0 | 0.7 |
| MSAA | 2x / Off | Off |
| Shadows | Soft 1 cascade, 거리 짧음 | Off 또는 Hard |
| Soft Shadow | Off | Off |
| HDR | Off (모바일) | Off |
| Post | Bloom 약 + Color Adjustments | Color만 |
| Additional Lights | Per Object 0~2 | 0 |

- **Forward+** 또는 Forward, Additional Lights 최소화  
- 메인 Directional 1 + 바다 반사용 가짜 스펙큘러(머티리얼)

### 4.2 셀 셰이딩

- 커스텀 **Toon Lit** (2~3단 램프 + 림라이트 약하게)
- 아웃라인: **Inverted Hull**(캐릭터만) 또는 화면공간(비추천, 비쌈)
- 캐릭터/보드만 아웃라인, 환경은 컬러 블록으로 읽히게
- 셰이더 키워드 최소화, GPU Instancing ON

### 4.3 바다·하늘 (청량감의 핵심)

- 바다는 **단일 플레인 + 스크롤 노멀/프레넬** (반사 프로브 실시간 X)
- 하늘: Gradient sky 또는 저해상도 cubemap, 구름은 스크롤 쿼드
- Depth Fog로 원경 부드럽게 (Exponential, density 낮게)

### 4.4 프레임 예산 (60fps 목표)

- Draw Call: 캐릭터 분리 머티리얼 ≤ 3, 환경 아틀라스
- 타일당 트라이 ≤ 2k~4k (모바일)
- 파티클: 바람 1 시스템, 최대 80~120
- 업데이트: 경로 샘플·스폰만 플레이어 근처, AI 없음
- `Application.targetFrameRate = 60`, VSync off (플랫폼별 조절)

### 4.5 QualityTier 자동

기동 시 GPU/메모리 휴리스틱 또는 유저 설정:

```
High → Mid → Low
Bloom / Outline / Sea detail / Shadow cascade
```

---

## 5. 초기 코드 위치

| 파일 | 경로 |
|------|------|
| `PlayerController.cs` | `Assets/_CoastRun/Scripts/Player/` |
| `CameraController.cs` | `Assets/_CoastRun/Scripts/Camera/` |
| `IInputReader.cs` | `Assets/_CoastRun/Scripts/Input/` |
| `MobileSwipeInput.cs` | `Assets/_CoastRun/Scripts/Input/` |
| `RunConfig.cs` | `Assets/_CoastRun/Scripts/Core/` |
| `MapGenerator.cs` (stub) | `Assets/_CoastRun/Scripts/World/` |
| `EnvironmentManager.cs` (stub) | `Assets/_CoastRun/Scripts/World/` |

다음 스텝: `Run.unity`에 Player + Camera 프리팹 배치 → `MapGenerator` 직선 타일 풀 → Cel 머티리얼 1종.

---

## 6. 레거시 정리 메모

- 기존 『347』 스토리·에셋은 삭제됨.
- `Assets/Scripts/*`, `Assets/_Project/Scripts/*`는 당분간 컴파일만 유지하거나, Coast Run 플레이어블이 올라온 뒤 일괄 제거한다.
- 새 플레이 진입점은 `Assets/_CoastRun/Scenes/Run.unity`로 통일한다.
