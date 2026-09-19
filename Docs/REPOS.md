# 레포 규칙 (2026-09-18 확정)

이 프로젝트는 **원격 저장소가 두 개**다. 작업 전에 어느 쪽인지 먼저 정한다.

| 리모트 | URL | 무엇 |
|---|---|---|
| `origin` | https://github.com/kk00701903-hub/game-costrunner.git | **기준 레포.** 「너와 나의 주파수」 본편 — 스토리·육성·아케이드 전부. 따로 말이 없으면 항상 여기. |
| `jette` | https://github.com/kk00701903-hub/game-jette.git | **K-POP 러닝만 따로 떼어낸 APK** 「빨리가자 제때 런」. 작업본은 **worktree `C:\dev\game-jette`(브랜치 `jette`)**. 원격은 첫 푸시 전까지 비어 있다. |

## 판단 규칙 (사용자 지시, 118차 다음)

> 「앞으로 기존의 레포를 기준으로 하되 **jette 나 제때** 레포의 경우 해당 레포를 바라보도록 해줘」

- 기본값은 `origin`(game-costrunner). 본편 작업·커밋·푸시는 전부 이쪽.
- 사용자가 **「jette」 또는 「제때」**(같은 말의 한글 표기)라고 하면 → `jette`(game-jette) 를 가리키는 것으로 읽는다.
  「제때 레포에 올려줘」 · 「jette 쪽 빌드」 · 「제때 APK」 = game-jette.
- 애매하면 묻는다. 커밋·푸시는 늘 사용자가 직접 한다 — 에이전트는 리모트 추가·브랜치 준비까지만.

주의: 번들 ID `com.jette.coastrun` · `PlayerSettings.companyName = "jette"` 는 **회사(스튜디오) 이름**이지 이 레포 이야기가 아니다. 본편 APK 의 것이므로 건드리지 않는다.

## K-POP 분리 APK — 아직 안 만든 것

사용자 계획: 「해당 레포에는 kpop 러닝만으로 별도 분리하는 apk 만들꺼야」. 지금은 **리모트만 연결**해 둔 상태다.
실제로 떼어낼 때 손봐야 할 곳(조사만 해 둔 것, 코드는 그대로):

- `Assets/_CoastRun/Editor/BuildMenu.cs` — `Bundle = "com.jette.coastrun"` 상수, `productName = "너와 나의 주파수"`, 빌드 씬은 `EditorBuildSettings.scenes` 의 enabled 전부. 분리 빌드는 **다른 번들 ID · 다른 productName · K-POP 씬만** 담은 별도 메뉴 항목이 필요하다(기존 `Build(BuildKind)` 를 그대로 쓰고 앞에서 값만 갈아끼우는 쪽이 회귀가 적다).
- K-POP 쪽 코드: `Scripts/UI/KpopChapterSelect.cs` · `KpopNowPlaying.cs` · `KpopBarPulse.cs` · `Scripts/Audio/CoastBgmLibrary.cs` · `CoastAudioManager.cs`, 곡 데이터는 `Assets/Resources/CoastRun/BGM/`.
- 진입 흐름은 `MainMenuController` · `SceneFlowController` 를 거치므로, 분리판은 타이틀에서 곧장 K-POP 러닝으로 들어가게 잘라야 한다.

떼어내는 방식은 두 갈래 — 사용자에게 먼저 물을 것:
1. **같은 작업본 + 브랜치** (`git push jette <branch>`): 본편 수정이 그대로 따라온다. 간단하지만 두 앱이 한 소스를 공유한다.
2. **별도 클론/워크트리**: 완전히 갈라진다. 공통 수정은 두 번 해야 한다.

## 2026-09-18 결정: worktree 로 분리했다
`git worktree add -b jette C:\dev\game-jette main` — 본편 main 의 미커밋 작업(113~118차)에 손대지 않으려고 같은 작업본의 브랜치 대신 **별도 폴더**를 썼다(이미 `C:\dev\game-pages`(gh-pages)도 같은 방식). 유니티는 한 번에 하나만(브릿지 포트 47001 공유). 자세한 것은 `C:\dev\game-jette\Docs\HANDOVER_JETTE_2026-09-18.md`.
첫 푸시: `git -C C:\dev\game-jette push -u jette jette:main`.
