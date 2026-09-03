# 『347』 — 튜토리얼 & 온보딩

> 처음 90초와 왕전 첫 대면. 구현: `TutorialDirector`, `TutorialHints`, `PrerunnerTrigger`, `OnboardingMetrics`.

충돌 시: [README](../../README.md) > 이 문서.

---

## 원칙 (코드 반영)

1. 손이 먼저 — 강제 성공 → 선택 → 압박 (`TutorialDirector.Beat`)
2. 튜토리얼 중 soft-fail — HP 유지, 속도 40% (`PlayerController.ApplySoftFail`)
3. "TUTORIAL" 표기 없음. 안내 텍스트 최대 12자, 전체 게임 4개만
4. 유닛 5(첫 피격)는 **run_count 1에서만**, 스킵 불가
5. 왕전 온보딩에 안내 텍스트 없음

## 첫 90초

| 시간 | 유닛 | 코드 |
|------|------|------|
| 0–12s | 편의점 | 탭으로 닫기 (`UIManager.RunPrologue`) |
| ~16s | 좌우 | 힌트 `← → 피하기` |
| ~28s | 점프 | soft-fail 가능 |
| ~40s | 슬라이드 | soft-fail 가능 |
| ~53s | 조합 | 회수반 자막 |
| ~60s | **의도된 첫 피격** | 스크립트 히트 + 응급 테이프 드롭 |
| ~75s | 아이템 | 부스터 드롭, 3초 미사용 시 슬롯 Pulse |
| 90s | 본편 | 힌트 소멸 |

붕괴선은 첫 400m 동안 접근하지 않음 (`HoldCollapseLine`).

## 회차별 스킵

| attempts (RunCount+1) | 처리 |
|----------------------|------|
| 1 | 전체 |
| 2–4 | 강제 비트·힌트 생략 |
| 5+ | 튜토리얼 비활성, 1구역 일반 코스 |
| 언제나 | 유닛 5는 1회차만 |

## 왕전 프리러너 (2구역 끝)

`PrerunnerTrigger` → `KingFight.BeginPrerunner()`

- HP 3, 주기 5s, 반격창 2s
- 온보딩 사이클 1–4 (벽 좁히기 → 무해 금색 → 미스 페널티 → 자유)
- HP 0이면 **패배 화면 없이** 이탈 (`DismissPrerunner`)
- 힌트 무전은 사이클 4까지 반격 0회일 때만

## 첫 사망 로그

`run_count == 1`: 타자기 4.5초 + 비고 0.8초 지연, 스킵 불가.  
이후: 짧은 `RecoveryLog()`. 비고 문구는 회차 3/8/15/30/50/100/347에 개방.

## 계측 (`OnboardingMetrics`)

로컬 카운터: `prologue_shown`, `first_input`, `tutorial_complete`, `tutorial_first_hit`, `king_counter_c*`, `king_hint`, `first_death`.

## 아직

- 지형 강제 배치(진열대/셔터)는 청크 데이터(M6)와 함께
- 구역 2–5 리허설 30초 (그라인드/어둠/축전환/역주행) — 메커닉 구현 후
- 원터치 모드 반격 자동 진입
- 기록 보관소 UI
