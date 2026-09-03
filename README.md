# 『347』 — 개발 지시서 (Unity / C#)

이 폴더는 게임 프로젝트 전용이다. 상위 폴더의 웹소설 원고·문체 문서와는 아무 관련이 없다. 작업 시 상위 폴더의 파일을 참조하지 말 것.

스케이트보드 엔드리스 러너. 폐허가 된 도시에서 회수 드론에게 쫓기는 3레인 러너 + 사이클형 보스전 + 메타 성장/수집.

이 문서는 Cursor에게 주는 구현 지시서다. 기획 상세는 `Assets/_Guide/` 의 설계 문서와 아래 우선순위를 따른다.

| 문서 | 내용 |
|------|------|
| [Assets/_Guide/Story.md](Assets/_Guide/Story.md) | 세계관, 인물, 5구역 서사, 엔딩 |
| [Assets/_Guide/Systems.md](Assets/_Guide/Systems.md) | HP·아이템·상태머신·왕전 사이클·스토리엔진·난이도 |
| [Assets/_Guide/TestPlay.md](Assets/_Guide/TestPlay.md) | 실행·검증 체크리스트 |
| [Assets/_Guide/SceneSetup.md](Assets/_Guide/SceneSetup.md) | 씬/인스펙터 값 |
| [Assets/_Guide/Economy.md](Assets/_Guide/Economy.md) | 재화·강화·가차·도감·일일·광고 |
| [Assets/_Guide/Onboarding.md](Assets/_Guide/Onboarding.md) | 첫 90초·왕전 온보딩·첫 사망 로그 |

**충돌 시 우선순위: 이 README > 설계 문서 > 추측.**

---

## 0. 확정 사항

| 항목 | 값 |
|------|-----|
| 엔진 | Unity 2022.3 LTS 이상, C#, URP |
| 플랫폼 | 모바일 우선 (Android/iOS), 에디터에서 개발 |
| 화면 방향 | 세로 고정 (Portrait), **Galaxy S26 기준 1080×2340 (19.5:9)**, 4:3 ~ 21:9 대응 |
| 목표 | 60fps (중급기 기준), 저사양 30fps 폴백 |
| 외부 패키지 | **무료 패키지 허용** (유료/상용 금지). DOTween 등 애니메이션 트윈 라이브러리는 쓰지 않고 내장 보간으로 처리 |
| 물리 엔진 | 사용 안 함. 커스텀 이동 + AABB 오버랩 판정 |
| 수익 모델 | 광고 리워드만 (SDK는 M7에서 인터페이스만) |
| 경로 | 템플런 스타일 **90도 코너 유지**. 좌표계는 경로 거리 + yaw |

세로 화면인 이유: 3레인 러너는 좌우 폭보다 전방 시야 거리가 난이도를 결정한다. 세로가 앞을 더 멀리 보여준다. 왕이 전방 60~90m에 있는 구도도 세로가 유리하다. 한 손 플레이도 된다.

### 아직 미정 (구현 중 결정, 임시값 사용)

- 아트 스타일 → 전부 프리미티브 도형 + 단색 머티리얼로 진행 (GLB는 있으면 쓰고, 없어도 100% 동작)
- 사운드 → 무음. 단 **반격창 생성음만은 임시 사인파라도 반드시** 넣는다
- 서버/랭킹 → M8 이후. 지금은 전부 로컬 저장

---

## 1. 구현 원칙

1. 모든 수치는 코드가 아니라 **ScriptableObject**에. 밸런싱을 코드 수정 없이 한다
2. **결정론적으로.** 같은 시드 = 같은 코스 = 같은 결과. `System.Random` 인스턴스를 시드로 생성해 쓰고, `UnityEngine.Random`은 게임플레이에 금지(연출에만 허용)
3. 런타임 할당 0에 가깝게. 장애물·아이템·투척물은 전부 오브젝트 풀. 런 중 `Instantiate`/`Destroy` 금지
4. 텔레그래프 최소 **0.45초**는 어떤 상황에서도 보장. 속도가 올라가면 스폰 거리를 늘려서 시간을 맞춘다
5. HP는 어떤 경로로도 **3을 넘지 않는다**
6. `Update`에서 `GetComponent` 금지. 참조는 전부 캐싱
7. 씬은 3개만: **Boot / Meta / Run**

---

## 2. 프로젝트 구조

```
Assets/_Project/
  Scripts/
    Core/        GameLoop  ServiceLocator  GameConfig  ObjectPool
                 DeterministicRandom  FixedTimestep  GameLog
    Player/      PlayerController  PlayerStateMachine  PlayerStats  States/
    World/       ChunkSpawner  LaneSystem  CollapseLine  SpawnDirector  ZoneManager  Track*
    Combat/      KingController  KingCycleFSM  KingPhaseData  CounterLane  Projectile*
    Items/       ItemSlot  ItemDefinition  Pickup*
    Story/       StoryEngine  FlagStore  StoryEventData  RetrievalLog
    Economy/     (M7)
    Meta/        SaveSystem  SaveData  AdRewardService  RunCardSystem  (M7+)
    UI/          HUD  DeckHpView  ItemSlotView  RetrievalLogView  SubtitleView
    Camera/      RunCamera  CameraProfile  AxisFlipHandler
    Audio/       CounterCueTone
  Data/          ScriptableObject 에셋 (Scripts와 대칭)
  Prefabs/
  Scenes/        Boot.unity  Meta.unity  Run.unity
  Tests/EditMode/
```

레거시 `Assets/Scripts/` 는 `_Project` 이관이 끝나는 대로 비운다. 런타임 부트는 `GameBootstrap` / `GameLoop` 가 담당한다.

---

## 3. 핵심 데이터 모델

- `GameConfig` — HP·속도·붕괴선·입력·텔레그래프 하한 (싱글톤 SO)
- `KingPhaseData` — 페이즈별 사이클 타임·반격 레인 수·가짜 레인·미스 페널티
- `CameraProfile` — FOV·오프셋·축 전환·히트 쉐이크
- `SaveData` — JSON, `Application.persistentDataPath/save.json`

---

## 4. 구현 마일스톤

순서를 지킨다. 재미가 있는지 가장 빨리 확인하는 순서로 짜여 있다.

| 마일스톤 | 내용 | 완료 기준 |
|----------|------|-----------|
| **M1** 러너 기본기 | 3레인·점프·슬라이드·장애물 1종·속도 14 고정·버퍼 120ms·코요테 100ms | 60초 조작 끊김 없음, 레인 전환 ≤0.18초 |
| **M2** HP·붕괴선 | HP 3, 감속 70%→1.5s, 무적 1.2s, 붕괴선 45m/−12m, 15m 경고, 데크 균열 표시 | 두 번 맞으면 붕괴선이 화면 안으로 들어옴 |
| **M3** 왕전 사이클 ★ | 단독 검증. Aim→Throw→CounterWindow→Recover. 반격은 레인만. 온보딩 사이클 1~4 | 설명 없이 5명 중 3명 이상이 「던진 자리로 들어가면 된다」 |
| **M4** 아이템·스폰 디렉터 | 슬롯 1칸 + tension (UI 비노출) + **런 카드** | |
| **M5** 스토리엔진 | 이벤트 구독, 채널 우선순위, SILENCE 덕킹 | |
| **M6** 청크·구역 1~2 | ChunkDefinition + 봇 시뮬레이션 검증 | |
| **M7** 메타·경제·도감 | Wallet·강화·가차·일일·광고 인터페이스·PrestigeVisuals | |
| **M8** 회수 로그·회차 | GAME OVER 금지, 회차 개방, 기록 보관소 | |

**M3 통과 전에는 M5 이후를 만들지 않는다.**

---

## 5. 이번에 새로 추가되는 것

- **5-1** 런 한정 카드 (`RunCardSystem`) — M4. 순수 상향 금지, 구역 경계에서 3.5초 선택
- **5-2** PrestigeVisuals — M7. 성능 캡 25% 유지, 보이는 성장만
- **5-3** 고스트·기록 공유 — M8 이후
- **5-4** 초반 30분 훅 — M7 밸런싱 프론트로딩
- **5-5** 카메라 (`CameraProfile`) — M1부터. 정보 전달 장치

---

## 6. 성능 예산

Draw call ≤80 · 삼각형 ≤60k · 런 중 GC 0 B/frame · 로딩 ≤2s · 텍스처 ≤120MB · 10분 후 프레임 ≥90%.

프레임 드랍 시 스폰 거리를 늘려 텔레그래프 0.45초를 시간으로 보정한다.

---

## 7. 코딩 규칙

- private 필드 `_camelCase`, 프로퍼티/메서드 `PascalCase`
- `Update` 안에서 `GetComponent` / `Find` / LINQ / 문자열 결합 금지
- 이벤트는 C# `event` 또는 EventBus. `SendMessage` 금지
- 매직 넘버는 SO 또는 `const`
- 상태 전이는 `PlayerStateMachine` 을 거친다
- `Debug.Log` 는 `GameLog` 래퍼. 릴리즈에서 스트립
- `Tests/EditMode` 에 강화 비용·가차·성능 캡 25% 검증 필수 (M7)

---

## 현재 진행

- [x] 골격: `_Project` 폴더 · Core · GameConfig / KingPhaseData / CameraProfile · ServiceLocator · ObjectPool · DeterministicRandom · GameLoop
- [x] 세로 고정 (Portrait) · URP 패키지 추가 (`14.0.11`) · `Tools/347/Setup Default Data` · `Create Boot Meta Run Scenes`
- [x] M1 러너 기본기 — CharacterController 제거, 커스텀 이동 + AABB, 레인 전환 `MoveTowards` 0.18s, 버퍼/코요테, 템플런 코너 유지
- [x] M2 HP · 붕괴선 — GameConfig 연동, 데크 HP 상한 3, 붕괴선 수치 SO화
- [x] M3 왕전 사이클 — 온보딩 1~4, 반격창 사인파(`CounterCueTone`), 히트스톱 0.15s, 1프레임 백색, 금색 채도 = 남은 시간, 미스 페널티 ×1.30, 결정론 RNG
- [x] M7 경제·성장·수집 — Wallet / Upgrade / Vendor / Codex / Mission / AdReward(인터페이스) / PrestigeVisuals / save.json
- [x] 온보딩 — TutorialDirector 90초 · soft-fail · 스크립트 첫 피격 · 프리러너 · 첫 사망 타자기 로그
- [ ] M4 아이템·런카드 · M5~M6 · M8 — M3 5인 검증 후 본격화 (경제·온보딩 골격은 요청에 따라 선행)

### 에디터에서 한 번 실행

1. `Tools > 347 > Setup Default Data`
2. `Tools > 347 > Create Boot Meta Run Scenes`
3. `Tools > 347 > Validate Economy Numbers`
4. 빈 씬 Play 또는 `Tools > 347 > Play King Arena` (`Ctrl+Shift+K`)

레거시 스크립트는 아직 `Assets/Scripts/` 에 있고, 신규 Core/World/Combat/Audio/Meta/Economy 는 `Assets/_Project/Scripts/` 에 있습니다. 런타임 부트는 기존처럼 `GameBootstrap` + `EconomyBootstrap` 이 담당합니다.