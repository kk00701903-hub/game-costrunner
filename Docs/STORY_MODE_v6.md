# 스토리 모드 v6 — 모드 분리 · 체력 게이트 · 이벤트 컷씬 (2026-09-09, 26차)

## 두 모드
| 모드 | 진입 | 내용 |
|---|---|---|
| 스토리 모드 | 타이틀 **이어하기 / 새로하기** | 육성(05_Raising) → 챕터 경계 이벤트 컷씬 → 체력 게이트 → 러닝(02_Run) → 클로즈 컷씬 → 다음 챕터. 목적은 러닝에 필요한 체력·아이템 키우기 |
| K-POP 러닝모드 | 타이틀 맨 아래 **K-POP 러닝모드 ♪** 바 | 스토리 없는 무한 러닝(`ArcadeRun.StartKpop`). 세이브가 있으면 육성 스탯(체력·순발력·매력·펫·러닝/보드)을 그대로 적용, 없으면 기본값. BGM은 `Resources/CoastRun/BGM/BGM_KPOP_1.ogg, _2 …`를 스테이지마다 순서대로(없으면 챕터 스템 폴백) |

## 체력 게이트 (`Meta/StoryGate.cs`)
- 요구 체력 = `24 + 6 × 챕터` → CH1 30(시작값과 같아 자동 통과) · CH5 54 · CH10 84 · CH15 114 · CH20 144. (StatMax 200)
- 판정 시점: 챕터 마지막 주가 끝날 때(`RaisingUI.ExecuteWeek` forced 분기), 스토리 카드/스토리 버튼으로 미리 돌입할 때.
- 통과 → 러닝. 불통과 → `GameManager.GateFail()`: 챕터 마감 `weekEnd`를 1주 연장, `ChapterRecord.gateFails++`, 육성 계속. 52주를 넘겨도 진행(계절은 겨울 고정). 다음 챕터는 `AfterChapterContinue`에서 늘어난 주차를 이어받는다(시간이 되돌아가지 않음).
- 대본 조건 태그: `[게이트>=0]`(통과) / `[게이트<0]`(불통과) / `[필요체력>=N]`.

## 이벤트 컷씬
- 챕터 마지막 주 → `ChapterVN.PlayChapterOpening(챕터)`가 육성 화면 위에 이벤트로 뜬다(처음 한 번). 컷씬이 끝나면 게이트 판정.
- 러닝 진입(`GameManager.StartStoryRun`)에서는 더 이상 오프닝을 틀지 않는다(프롤로그 PRO만 회차 첫 돌입 때).
- 송전탑 도착 후 클로즈(`CHnn_Close`)는 기존대로 `SceneFlowController`가 튼다.

## 대본 (기존 컷씬 전부 제거)
- 옛 대본 58씬은 `Docs/_archive_cutscenes_old/*.txt`로 보관(코드에서는 빠짐). `Docs/STORY_SCRIPT_2026-09-08.md`도 옛 대본의 요약본.
- 새 대본 자리: `Tools/Story/script/NN_<씬ID>.txt` 43개(PRO, CH01~20 Open/Close, END_A/B) — 지금은 플레이스홀더 한두 줄 + 게이트 분기 예시.
- 반영 방법: txt를 고친 뒤 `python Tools/Story/cutscene_txt.py import Tools/Story/script` → `ChapterScript.Data.cs / .En.cs / Loc.cs(챕터 제목)` 재생성 → Unity 컴파일.
- 챕터 제목은 각 `_Open.txt` 머리의 `제목: … | EN: …` 줄. 지금은 "N장 / Chapter N".
- 씬이 비어 있으면(파일 삭제) 그 컷씬은 건너뛴다(`ChapterVN.Play`가 즉시 onDone).
