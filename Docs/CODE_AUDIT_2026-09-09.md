# 『우리의 송전탑』(Coast Run) 전체 코드베이스 점검 리포트

- 일자: 2026-09-09
- 대상: `C:\dev\game` — `Assets/_CoastRun/Scripts` 200개 C# 파일, 약 40,000줄 + 에디터 스크립트 + 문서
- 방법: 정적 패턴 검사(grep) → 영역별 전수 정독(Core/Player/Economy · UI/Raising/Story/Meta · World/Visual/Rendering/Audio · 문서/툴링) → 상위 발견 사항 코드 재확인
- 환경 확인: Unity `6000.5.10f1`, URP, 세로 720×1280 기준. git 팩 크기 **896 MB**, `Assets/Resources/CoastRun` **386 MB(845개 파일이 git 추적, LFS 없음)**

---

## 0. 한눈에 보기

| 등급 | 건수 | 대표 |
|---|---|---|
| 치명 | 5 | 머티리얼 무한 누수(2건), 재도전 시 장애물 겹침, 세이브 무경고 초기화, 스크롤 영역 입력 소실 |
| 높음 | 12 | 코인 이중 적립·회차마다 공짜 500, 코인마다 디스크 쓰기, 골인 연출 사망, 세그먼트 스트리밍 GC 스파이크, 바다 메시 매 프레임 할당, 모바일 과부하 렌더 설정, IAP 복원 오동작, VN 중복 재생 소프트락 등 |
| 중간 | 20+ | 결정성 깨짐, 핫패스 탐색/할당, Sprite 누수, PlayerPrefs 스프롤, 한국어 하드코딩 산재, HitStop이 일시정지 해제 등 |
| 낮음 | 15+ | 매직넘버 중복, 죽은 분기, 로케일 의존 날짜 등 |
| 문서 | — | 루트 README는 다른 게임(『347』) 지시서. `_Guide` 6개 문서가 존재하지 않는 코드 참조 |
| 툴링/보안 | 3 | `VideoFetch.cs`가 도메인 리로드마다 텍스트 파일의 .bat를 무조건 실행, 무인증 TCP 원격 서버, 확인 없는 파괴적 삭제 |

**가장 먼저 손댈 것 (권장 순서)**
1. `CoastMaterials.CreateLit/Unlit` 색상 캐시 + 세그먼트/픽업 파괴 시 머티리얼 해제 (치명 1·2, 메모리)
2. `ObstacleSpawner.ResetForStage`에서 기존 행 전부 제거 + `CoinSpawner` 리셋 추가 (치명 3, 높음 4)
3. `SaveManager` 버전 마이그레이션·손상 시 백업·temp→rename 쓰기 (치명 4)
4. `CoastUiCanvas` RaycastWatchdog에 ScrollRect 예외 (치명 5)
5. 코인 원장 단일화 + `CoinWallet.Persist` 호출 시점 이동 (높음 3·7)
6. `Editor/VideoFetch.cs` 자동 실행 제거 (툴링)
7. 루트 README 재작성, `_Guide` 347 문서 아카이브 (문서)

---

## 1. 아키텍처 요약

**부팅·씬 흐름.** `BootLoader` → `GameDirector`(DDOL; `StageManager`·`SceneFlowController`·`ProgressionManager`·`UIRoot` 보유) → `SceneFlowController.GoTo()` 코루틴 상태기계가 `00_Boot → 01_Title → (03_Cutscene additive) → 02_Run → 05_Raising / 04_Ending`을 전환한다. 02_Run은 타이틀 뒤에서 additive로 프리로드된다. 씬은 전부 빈 껍데기이고 `*SceneDriver`(RuntimeInitializeOnLoad)가 컨트롤러를 붙인다.

**런 씬 조립.** `SceneDriverInstaller`가 `CoastRunBootstrap`을 붙이고, 이것이 월드·플레이어·카메라를 코드로 생성한 뒤 `GameSession.InitializeFromBootstrap()`이 20여 개 컴포넌트를 `GetComponent ?? AddComponent` + `Bind()`로 배선한다(591줄, 갓클래스).

**런 루프.** DDOL `StageManager.Update`가 진행률·노을 시계·클리어를 판정하고 `OnStageStart/Clear` 이벤트로 `GameSession`이 스포너·HP·펫을 리셋한다. 단일 씬에서 20스테이지를 타일 스트리밍으로 이어 달린다. `PlayerController`는 물리 없는 경로거리 + 레인 오프셋 + 수직 속도 모델이고 충돌만 키네마틱 Rigidbody + 트리거로 받는다. `ObstacleSpawner/JellySpawner`(시드 `System.Random`)·`CoinSpawner`(`UnityEngine.Random`)가 앞 80~90 m를 채운다.

**월드·렌더.** `DownhillPath`는 직선 z축이고 시각적 커브는 `CurveDirector`가 `_CoastCurve` 글로벌 벡터로 버텍스 셰이더에서 굽힌다. `MapGenerator`가 30 m 타일을 앞 6·뒤 2개 유지하며 `PromenadeSegmentBuilder.Build`로 매번 새로 만들고 범위 밖은 `Destroy`한다(풀링 없음). 색은 `CoastPaletteConfig`(SO) → `CoastMaterials`가 만든 머티리얼을 `TrackedMats`에 등록해 라이브 갱신. 포스트는 `CoastPostStack`(Bloom·ColorAdjust·Vignette) + 챕터 볼륨 크로스페이드.

**UI·메타.** `GameManager`(DDOL)가 회차 `SaveData`를 들고 육성→런→정산→엔딩을 결정, `SaveManager`가 `save_0.json` + `profile.json`을 JsonUtility로 통째로 쓴다. 육성 화면은 `RaisingUI`(1,892줄 + Popups 448줄)가 전부 코드로 빌드한다. 스토리는 v5 `ChapterVN` + `ChapterScript.Data/En`과 레거시 `CutsceneController/StoryManager`가 공존한다. 결제는 `IapBridge`(COASTRUN_IAP define).

**정적 지표.** `UnityEngine.Random` 사용 19파일(가장 많은 곳 `ChapterVN` 8, `CoinSpawner` 8) / `FindObjectOfType` 계열 73곳(`SceneFlowController` 14, `CoastRunBootstrap` 10) / `Destroy(` 다수(`RaisingUI.Popups` 15, `JellyPickup` 14, `OncomingCar` 10) / `Resources.Load` 42 / PlayerPrefs 83 / `Debug.Log` 직접 39 / `DontDestroyOnLoad` 24 / EditMode 테스트 0개.

---

## 2. 치명

### 2-1. 스폰마다 Material 생성 → 정적 리스트에 영구 보관 (메모리 무한 증가)
- `Rendering/CoastMaterials.cs:77,100,314-319` — `CreateLit/Unlit`이 매번 `new Material(...)` 후 `Track()`으로 `static List<Tracked> TrackedMats`에 추가. 제거 조건은 `t.Material == null`(285행)뿐인데 GameObject `Destroy`는 Material을 파괴하지 않고 `Resources.UnloadUnusedAssets`도 호출되지 않는다(전 스크립트 0건).
- 호출처: 세그먼트(`World/PromenadeSegmentBuilder.cs:641,605` — 타일당 40~55개), 픽업/하자드(`Economy/JellyPickup.cs:124,134`, `ObstacleHazard.cs:175,184`, `OncomingCar.cs:145-148`, `DuckHazard.cs:63,75`), `Content/PropCatalog.cs:321-353`, `World/FinishRibbon.cs:27-47`(스폰마다 14개).
- 결과: 30 m마다 ~50개 + 젤리 스테이지당 1,000개 이상 → 수천 개 머티리얼이 씬 재로드 후에도 해제 불가. `RefreshTracked`도 O(n) 증가.
- 수정: 색 → Material 캐시(`Dictionary<Color,Material>`) 또는 공유 머티리얼 1개 + `MaterialPropertyBlock`. `Track`은 캐시된 것만 등록. 픽업/세그먼트 `OnDestroy`에서 소유 머티리얼 `Destroy`.

### 2-2. 세그먼트 스트리밍이 Instantiate/Destroy 풀 재생성
- `World/MapGenerator.cs:33-34,46-48` — 타일마다 `PromenadeSegmentBuilder.Build` 새로 생성 / `Destroy`. 타일 하나 = 차선 점선 26 + 난간 기둥 12 + 만국기 33(`StreetDressing.cs:73-93`) + 수국 + 키트 Instantiate(`JejuKit.cs:53`, 렌더러마다 `new Material[]`) + 잉크 셸(`BuildingOutline.cs:24-31`) ≈ 250~350 렌더러/타일, 상주 9타일 ≈ 2,500 렌더러. `CreatePrimitive` 후 콜라이더 제거 비용도 매번.
- 수정: 타일 3~4종 프리팹 풀 + `CombineMeshes`로 점선·기둥·만국기를 1 메시로.

### 2-3. 재도전 시 이전 장애물이 남아 두 겹으로 깔림
- `Economy/ObstacleSpawner.cs:84-93` `ResetForStage`는 `_rng/_nextSpawnZ/_prevOpen`을 리셋하고 `_car`만 파괴한다. `_root` 자식(기존 행)은 남는다. `StageManager.RetryCurrent()`(Core/StageManager.cs:201)가 플레이어를 원점으로 되돌리면 옛 행은 `< z - 45f` 스윕(213행)에 안 걸리고, 새 시드로 다시 행이 깔려 "한 레인은 열림" 보장이 깨진다. `_rowsUntilPad/_padsSinceLine`(364,383행)도 리셋 안 됨.
- 수정: `JellySpawner.ClearAll()`처럼 `_root` 자식 전부 회수 + 패드 카운터 리셋.

### 2-4. 세이브 손상·형식 변경 시 무경고 초기화
- `Meta/SaveManager.cs:59-60` `if (s == null || s.chapters == null || s.chapters.Length != Timeline.Chapters) return null;` → `Meta/GameManager.cs:93` `if (Save == null) { NewGame(...) }`. `SaveData.version = 2`(`SaveData.cs:86`)를 읽는 곳이 없어 마이그레이션이 없고, 파싱 실패/챕터 수 변경/파일 손상 시 확인 없이 새 회차를 덮어쓴다. `Write()`도 temp→rename 없이 `File.WriteAllText`.
- 수정: `version` 스위치 마이그레이션, 손상 시 `.bak`로 옮기고 타이틀 안내, 임시파일 후 `File.Replace`.

### 2-5. 투명 스크롤 영역이 1초 뒤 입력을 잃음 (RaycastWatchdog)
- `UI/CoastUiCanvas.cs:504-510` — 알파 ≤ 0.02·부모에 Selectable 없음·화면 60% 이상인 Image의 `raycastTarget`을 끈다. `UI/CollectionUI.cs:555`의 ScrollRect 수신 이미지는 알파 0.01 → 워치독이 꺼버려 팬아트·트로피 탭 스크롤이 죽는다(RaisingUI 스크롤은 60% 미만이라 우연히 살아남음).
- 수정: 워치독 예외에 `GetComponentInParent<ScrollRect>()` 추가, 또는 투명 수신 이미지 알파를 0.03 이상으로 통일.

---

## 3. 높음

| # | 위치 | 문제 | 수정 |
|---|---|---|---|
| 3-1 | `Economy/CoinWallet.cs:23-49` | 코인 1개마다 `PlayerPrefs.Save()` 동기 디스크 쓰기 → 코인 라인에서 프레임 스파이크 | `Add`는 메모리만, `Persist`는 클리어/일시정지/`OnApplicationPause` |
| 3-2 | `Economy/CoinSpawner.cs:16,58,77-78`, `Core/GameSession.cs:340-350` | 스테이지 리셋 없음(재도전 후 이전 도달점까지 코인 0) + `UnityEngine.Random` 비결정 | `ResetForStage` 추가, `System.Random(seed)` 통일 |
| 3-3 | `Economy/ObstacleSpawner.cs:127,205,282-283,342` | `RowGap/PlanCar`가 `player.Speed`를 곱해 "같은 시드=같은 코스" 주석이 거짓 | 진행률 기반 기대 속도 곡선 사용 |
| 3-4 | `Core/GameSession.cs:377-378`, `Player/PlayerController.cs:222-230` | 클리어 시 `player.enabled=false`가 먼저 실행돼 `FinishRun` 감속 연출이 안 돈다. `MobileSwipeInput`은 Update가 없고 `Tick()`을 직접 호출하므로 `input.enabled=false`(300,376,429행)는 무효 → RunOver 중 스와이프가 상태를 바꿈 | `IsRunning` 플래그로 입력 차단, `Tick()` 앞 활성 체크 |
| 3-5 | `CoinWallet`(PlayerPrefs) vs `Meta/GameManager.cs:255`(`stats.money +=`) vs `Meta/SaveManager.cs:45-47` | 코인이 두 원장에 이중 적립, 새 회차마다 레거시 키에서 최대 500 재흡수 → 회차마다 공짜 500 | `SaveData.money` 단일 원장, 레거시 키 흡수 후 `DeleteKey` |
| 3-6 | `World/CoastSea.cs:131-142` | 매 프레임 `mesh.vertices`(1,961 Vector3 새 배열 ≈ 23 KB) + `RecalculateNormals`, `MarkDynamic` 없음 | 작업 배열 재사용 + `SetVertices`, 이상적으로는 셰이더 파도 |
| 3-7 | `Visual/BlobShadow.cs:78,121,158`, `HazardRing.cs:41,55`, `PickupGlow.cs:41`, `PaintedProp.cs:45,110` | 인스턴스마다 머티리얼 + 매 프레임 `GetComponentInParent` / `SetColor` / `Shader.Find` | 셰이더 캐시, 공유 머티리얼 + MPB, 참조 1회 캐시 |
| 3-8 | `Visual/JuiceDirector.cs:291-296,478-484,590-598` | 픽업/팡마다 파티클·Quad Instantiate/Destroy(젤리 트레일 초당 10개) | 파티클 1개 `Emit(EmitParams)`, 링 쿼드 풀 |
| 3-9 | `Core/StageManager.cs:288` → `Env/DynamicEnvironmentManager.cs:80-124,154`, `World/CoastSky.cs:74,438` | 매 프레임 `SetTime` → 환경 전체 재적용, `Camera.main`, `renderer.material` 인스턴싱 getter | 변화 임계값 넘을 때만 적용, 참조 캐시 |
| 3-10 | `Rendering/CoastPostStack.cs:38,86-91`, `CoastUrpShadows.cs:12-19`, `World/JejuKit.cs:67` | Bloom 강제, 그림자 60 m·2캐스케이드, 키트 전부 ShadowCasting On. **URP/볼륨 에셋을 런타임에 직접 수정**해 에디터에서 에셋이 더러워짐 | 저사양 프로파일(Bloom off·캐스케이드 1·30 m), 런타임 값은 `Instantiate(profile)`에 |
| 3-11 | `Meta/IapBridge.cs:135,156-162` | `RestoreTransactions` 콜백이 `FetchPurchases` 완료 전 `done(false)` 호출 → "복원 실패"로 보임. `_pendingDone` 연타 시 이전 콜백 유실 | `OnPurchasesFetched`에서 restore 완료 처리 + 타임아웃 |
| 3-12 | `Story/ChapterVN.cs:26-27,170,176`, `Raising/RaisingUI.cs:1394-1395` | 중복 `Play()` 시 이전 `_canvas`(raycast 블로커)와 `_onDone`이 유실 → `while(!doneVn)` 영구 대기, `_busy` 고정 | `Play()`에서 이전 인스턴스 `Finish()` 먼저 호출 |
| 3-13 | `Raising/RaisingUI.Popups.cs:420-424` → `GameManager.cs:356-362` | 타임라인 "송전탑 다시 가보기"가 `ResolveEnding`을 재호출해 `endingsSeen/happyEndings` 반복 증가, 다음 회차 번호 부풀림 | 재생 전용 `ReplayEnding()` 분리 |
| 3-14 | `UI/UI_FinalDestinationController.cs:233-281`, `UI/RunHudChrome.cs:723-738` | 런 HUD가 매 프레임 문자열 생성 + `text` 대입 + 앵커 재설정 → 캔버스 리빌드·GC | 정수 캐시 후 변경 시에만 대입, 상수 레이아웃은 Build 시 1회 |

---

## 4. 중간

**결정성·게임 로직**
- `World/PromenadeSegmentBuilder.cs:258 static int _shopStreak`, `StreetDressing.cs:105 _signIx++` — 정적 상태가 타일 생성 순서에 따라 같은 인덱스 타일을 다르게 만든다. 시드 `System.Random(index*…)`로 잘 해 놓고 여기서 무너짐 → `index` 해시로 계산.
- `Meta/ScheduleJudge.cs:76-78` — 대성공 확률이 성공 확률과 독립(`roll < pGreat ? Great : roll < pSuccess ? Success : Fail`). pSuccess 5%·pGreat 30%면 실제 30% 성공인데 카드엔 5% 표시 → `pGreat = min(pGreat, pSuccess)`.
- `Visual/JuiceDirector.cs:631-645` HitStop이 `Time.timeScale = 1f`로 복원 → 니어미스 직후 일시정지(0)를 0.15초 뒤 풀어버림 → 저장값 복원 + 일시정지 중 중단.
- `Core/GameDirector.cs:76,91` `BuildChildren()` 2회 호출(`AddComponent` 시 Awake가 먼저) → `progression.Load`·`flow.Bind` 중복.
- `Core/SceneFlowController.cs:34-37,117-124` — `_runPreload` 대기 중 `GoTo(Title/Ending)`가 오면 Single 로드가 영원히 대기(주석이 스스로 인정한 검은 화면). `BusyGuard`는 플래그만 풀고 코루틴은 그대로 → `LoadSingle` 진입 시 프리로드 flush.
- `Camera/RunnerCameraRig.cs:251,354,363` — `dt = unscaledDeltaTime`인데 `SmoothDamp`는 내부 `Time.deltaTime` 사용 → 슬로모·히트스톱에서 롤/측면만 느려짐. `deltaTime:` 인자 명시.
- `Player/PetCompanion.cs:60` `Clamp(…, 0, 3)` → `BlackPig = 4` 선택 불가.
- `Player/CoastPlayerVisual.cs:89-91` vs `Player/SkaterRig.cs:465` — 리그 오프셋 (0,0.15,0.06)을 매 프레임 (0,bounce,0)으로 덮어써 발이 보드 위에 안 놓임.
- `Meta/RandomEventTable.cs:276` 폴백 `_all[0]`은 가을 전용 이벤트 → 계절 규칙 위반 가능.
- `Story/StoryCond.cs:74-84` 첫 `[…]`만 처리, `Value()`가 세이브 없을 때 999/0 혼용.

**핫패스 할당·탐색**
- `ObstacleSpawner.cs:230-231,410,424` 행마다 `int[]`/`List<int>` 새로 생성; `Economy/FeverMode.cs:78`·`PlayerController.cs:229,216`·`Visual/MonochromeWorld.cs:89` Update마다 `Find*ObjectByType`; `CoastPlayerVisual.cs:413`·`PickupReach.cs:34`·`PaintedProp.cs:129`·`CloudLayerScroller.cs:156-158`·`CoastSky.cs:238` 매 프레임 `Camera.main`; `Player/PetCompanion.cs:252-254` → `ObstacleHazard.cs:22 Breakable => GetComponentInParent<OncomingCar>()` 활성 하자드 수십 개 × 매 프레임; `SkaterRig.cs:267` 피격마다 `anim.parameters` 배열 복사.
- `Economy/JellyPickup.cs:46`, `JellySpawner.cs:158`, `ObstacleSpawner.cs:214` — 젤리·장애물 풀링 없음(코인만 풀링). 젤리는 스테이지당 1,000개 이상.
- `Core/GameSession.cs:577` + `PlayerController.cs:241` `map.SetPlayerDistance` 매 프레임 두 번.
- `World/JejuKit.cs:293-296,313`, `Content/SeasonLook.cs:89-99`, `PromenadeSegmentBuilder.cs:428-432` — 가장 큰 메시가 전부 MPB → SRP Batcher 이탈. 색 조합이 적으니(베이스 7 + 포인트 4) 색별 공유 머티리얼로.
- `UI/CoastUiCanvas.cs:496,504` RaycastWatchdog 자체가 매초 `FindObjectsByType` + 이미지마다 `GetComponentInParent` → 개발 빌드 전용으로.

**리소스 누수**
- `UI/CoastUiArt.cs:758-763 AsSprite` 매 호출 `Sprite.Create`, 캐시 없음 → `RaisingUI.Refresh()`마다 4~6개, `StageClearUI.cs:268`, `UI_MemoryPopup.cs:355,363`, `UI_FeedbackController.cs:212` 동일. `Dictionary<Texture2D,Sprite>` 캐시.
- `Audio/CoastAudioManager.cs:400-406` 리소스 없으면 호출마다 `CreateBlip` 새 AudioClip; `Audio/TitleAudio.cs:23-24` 메뉴 진입마다 1.4 MB 루프 생성. `CoastAudioManager.cs:436,490` `_sfx.pitch` 공유로 겹치는 원샷 피치 오염.
- `Story/EndingController.cs:978` 스톡당 `new Material`; `OpeningCinematic.cs:608` RenderTexture `Release`만.
- `Story/ChapterVN.cs:126-159` 파티클 매 프레임 35% 확률 `new GameObject`, `RaisingUI.RefreshCards`(801-802)·`CollectionUI.Refresh`(105) 열 때마다 전 카드 파괴·재생성 + 텍스처 재로드.

**데이터·저장**
- PlayerPrefs가 저장소: `ProgressionManager`, `UpgradeManager`(`SaveStat`마다 `Save()`, `SaveAll` 4회), `CoinWallet`, `Loc`, 디버그 플래그. 버전·마이그레이션·예외 처리 없음. `"CoastRun_AlbumOwned"`(Collection/IapBridge)가 `profile.albumOwned`와 이중 기록, `"CoastRun_ArcadeBoard"` 리터럴 중복, `"CoastRun_VN_"+id`는 쓰기만. → 키 상수 `CoastPrefs` 집중, 앨범 소유 profile 단일화, SaveManager(JSON)로 이관.
- `CollectionUI.cs:502-506` IAP 콜백이 닫힌 UI를 만짐(`_pendingDone` static) → `if (this == null) return`.
- `Meta/ArcadeRun.cs:66,186`, `ArcadeUI.cs:399` `DateTime.Now.ToString("yyyyMMdd")` 로케일 의존 → `InvariantCulture`.

**로컬라이즈·구조**
- 한국어 하드코딩이 `Loc` 밖에 산재: `RaisingUI.cs`(634,707,729,872,964,1010-1024,1046,1063-1066,1098,1246,1339,1366-1379,1451,1465,1811-1812), `RaisingUI.Popups.cs`(17-123), `Meta/RandomEventTable.cs:237-259`, `ScheduleTable.cs` `place`, `UI_FinalDestinationController.cs`, `RunHudChrome.cs:447,824-834`, `UI_PhoneOverlay.cs`, `EndingController.cs`, `OpeningCinematic.cs`. 같은 파일 안에서 `Loc.T`와 리터럴 혼용.
- 갓클래스: `RaisingUI`(2,340줄), `GameSession`(591), `JuiceDirector`(1,038), `MainMenuController`(1,023, 죽은 `firstLaunch` 분기와 레거시 3D 타이틀 경로 잔존), `EndingController`(1,022, 4분 시퀀스 스킵 없음). `OrnatePanel`이 `CoastOrnate.Panel`과 중복, `Place/Stretch/Label`이 4개 UI에 각각 재구현, `SeasonName`이 `Timeline`·`ArcadeRun` 중복.
- `GameObject.Find("CoastRunHUD")` 의존 4곳(`UI_PhoneOverlay.cs:80`, `UI_FinalDestinationController.cs:407`, `LandmarkManager.cs:436`, `EndingController.cs:991`).
- 매직넘버: `laneWidth = 2.2f`가 `ObstacleSpawner:26`·`CoinSpawner:14`·`JellySpawner:17`에 각각, `RunConfig.laneOffset`과 별개. `StageManager.cs:245,249`, `PlayerController.cs:270-271` 상수 → RunConfig.
- 이벤트 해제 누락: `UpgradeShopUI.cs:26-28`, `RunHudChrome.cs:105`.
- `Story/CutsceneController.cs:329-331` CineCamera에 AudioListener 추가 → 다중 리스너 경고. `MainMenuController.cs:873` `"v0.9"` 하드코딩 vs `Application.version`.

**장기 안정성**
- 원점 리베이스 없음: `Meta/ArcadeRun.cs:64` 무한 주행, `CurveDirector.cs:95`가 `(z-origin)²`를 셰이더에 → 모바일 half 정밀도에서 수 km부터 굽힘·그림자 떨림. 세그먼트 경계마다 월드 원점 시프트 권장.
- `Rendering/SkyTextureGenerator.cs:17-51` 폴백 512×896 절차 텍스처 메인 스레드 수백 ms.

---

## 5. 에디터 툴링·보안

- **`Assets/_CoastRun/Editor/VideoFetch.cs:11,20,28-37`** — `[InitializeOnLoad]` + `delayCall`로 **도메인 리로드마다 자동 실행**. `Tools/FireflyVideo/run.txt`에 적힌 .bat 절대경로를 확인 없이 실행하고, `fetch.txt`의 임의 URL을 내려받아 `Assets/Resources/CoastRun/Video`에 쓴다. 텍스트 파일 하나로 임의 코드 실행 가능 → 메뉴 수동 실행만, 도메인 화이트리스트.
- `Editor/CoastRemote.cs:22,64` — `[InitializeOnLoad]` TCP `127.0.0.1:47001`, 인증 없음. `menu <path>`로 아무 메뉴 실행. 루프백이라 외부 노출은 없지만 로컬 프로세스면 누구나 제어 가능(Unity MCP 브릿지가 이걸 쓴다 — 최소한 토큰 한 줄 추가 권장).
- 확인 대화상자 없는 파괴적 동작: `Editor/CoastTestBuild.cs:35`, `CoastRunWebGLBuild.cs:41` `Directory.Delete(outDir, true)`; `CoastVolumeMenu.cs:43` `AssetDatabase.DeleteAsset`; `MixamoImportSettings.cs:163`; `ThreeDAssetImporter.cs:66`. `SceneHotkeys.cs:32`, `BgmPipelineMenu.cs:59,101` `#F7/#F8` 단축키에 cmd.exe/.bat 실행이 묶여 있음.
- 비밀값: 코드에 하드코딩 없음. `PexelsImageImporter.cs`·`KlingGen`·`Telegram`은 `.env`에서 읽고 `.gitignore`(33-35행)가 `.env`를 제외 → OK. `Assets/Editor/ArtImportSettings.cs:45` `Obs_` 빌보드 밉맵 off → 원거리 시머링.
- `World/JejuKit.cs:95-98` 에디터에서 모델마다 `File.AppendAllText(kit_log.txt)` + `Debug.Log`.

---

## 6. 저장소·빌드 크기

- git 팩 **896 MB**, `.gitattributes`(LFS) 없음. `Assets/Resources/CoastRun` 386 MB(PNG 289 MB·OGG 74 MB·MP4 47 MB·FBX 34 MB)가 **845개 파일로 git에 직접 추적**된다. 클론·푸시가 느려지고 히스토리가 되돌릴 수 없이 커진다 → LFS 전환 또는 `git filter-repo` 검토.
- `Resources/` 폴더는 **빌드에 전부 포함**된다. `Skater.fbx` 29 MB, `Ch46_1001_Normal.png` 14 MB, `Cut_Open_*.png` 각 3.5~4 MB 등 원본이 그대로 들어가 있어 APK 크기와 `Resources.Load` 인덱스 비용에 직접 영향 → 런타임에 안 쓰는 원본은 `Art/`로, 컷씬 원화는 Addressables/StreamingAssets, 텍스처는 ASTC 압축·최대 크기 제한.
- EditMode 테스트 0개. `run-tests.ps1`은 테스트가 없어 `exit 1`로 끝난다. `CoastIntegrationTest.cs`는 리소스 존재·씬 부트만 확인하고 게임 로직 단언이 없다.

---

## 7. 문서 정합성

- **루트 `README.md`는 다른 게임(『347』)의 지시서**다. `Assets/_Project/`, Boot/Meta/Run 3씬, `Tools > 347` 메뉴, `GameConfig/KingPhaseData/GameBootstrap/EconomyBootstrap` 등이 전부 존재하지 않는다(약 85% 불일치). 살아남는 건 0절 일부(세로·URP·모바일·DOTween 미사용)와 7절 네이밍 규칙뿐.
- `_Guide/Systems.md · SceneSetup.md · TestPlay.md · Economy.md · Onboarding.md · Visual.md` 6개 — 전부 347 전용, 참조 클래스·메뉴·`Story.md` 없음 → `_Guide/_archive_347/`로 이동.
- `_CoastRun/README.md`, `COMPLETION.md`, `Art/PIPELINE_STATUS.md`, `UnityHubTest.md` — `.\playtest.ps1 -CoastRun` 스위치가 없음(실제 `-Play/-Title/-CompileOnly`), `Tools → Coast Run → Create Run Scene/Play` 메뉴 없음(실제 `Coast Run/▶ PLAY 주행만 … %#c`), "4 seasons"는 `ContentIds.cs:17` `[Obsolete]`, "5-scene"은 6씬.
- `GameDesign.md` — 씬 `Boot/Run/Home`(실제 6씬), `IInputReader` 시그니처 불일치, "HP 없음/Soft HP"(실제 `HealthSystem` 스태미나 바), `ScoreService/RunSession/ObjectPool/AudioDirector` 미존재. `ChapterAudioAtmosphere.md` BGM 파일명 체계가 `Resources/CoastRun/BGM/README.md`와 다름. `README_CoreLoop.md` base 25 → 실제 30.
- 문서 간 모순: HP(3칸 / Soft HP / 스태미나), Unity 버전(2022.3 / 6 — 실제 `6000.5.10f1`), 해상도(1080×2340 / 720×1280 — 코드는 720×1280), 수익 모델(광고만 / IAP 존재), 물리(미사용 / 키네마틱 Rigidbody).
- 정리안: 루트 README를 송전탑 기준으로 재작성(6씬 흐름, 실제 메뉴·단축키, playtest 스위치, 코어 루프 + v2 육성/챕터/컷씬), `_CoastRun` 문서 3개를 하나로 병합, `GameDesign.md` 씬/입력/HP 절 수정, `run-tests.ps1` 헤더 갱신 또는 삭제, v2 육성 시스템(05_Raising·ScheduleTable·Timeline·SaveData 19챕터) 문서 신설.

---

## 8. 잘 되어 있는 것

- 스포너·세그먼트가 시드 `System.Random`을 쓰고 커브가 거리 함수라 재시도 시 동일 코스라는 설계 방향은 맞다(위 몇 군데 누수만 막으면 된다).
- 코인은 풀링돼 있고, `PuffStep`은 `Emit(EmitParams)` 방식으로 올바르게 돼 있다 — 젤리·링·트레일도 같은 방식으로 맞추면 된다.
- BGM은 Streaming 임포트, `OnAudioFilterRead` 사용 없음, 비밀값이 코드에 없고 `.env`가 gitignore에 있다.
- `ChapterScript.Data/En` ko·en 키/인덱스 불일치 없음(검증 완료). `Story_OurPowerTower.md`는 코드와 일치.
- 팔레트 라이브 갱신·챕터 볼륨 크로스페이드·SafeArea 캔버스 등 툴링 편의는 잘 갖춰져 있다.
