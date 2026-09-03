# 우리의 송전탑 (Our Power Tower) — 스토리 시스템

## 진행 순서

```
프롤로그(약속→장애→결심→게임 전환) → 주행(NearMiss·업그레이드·추억) → 노을 → 블루아워 → 송전탑 · 만남
```

| 단계 | Act | 트리거 | 연출 |
|------|-----|--------|------|
| 1 | Prologue | 게임 시작 | 텍스트 4씬 + 씬4 카메라 핸드오프 |
| 2 | Run | 프롤로그 종료 | NearMiss 응원, 랜드마크 독백, 업그레이드 |
| 3 | GoldenHour | 진행 55%+ | 노을 조명, HUD `GOLDEN HOUR` |
| 4 | BlueHourApproach | 진행 88%+ | 블루아워, 송전탑 접근 |
| 5 | Arrival | 송전탑 도착 | 엔딩 카드 2장 (만남) |

## 프롤로그 4씬

1. **약속의 스마트폰** — 송전탑 약속 메시지
2. **예기치 못한 장애** — 버스·택시 불가
3. **소녀의 결심** — 보드에 올라탐
4. **게임플레이로의 전환** — 측면 → 추격 카메라

## 스크립트

| 클래스 | 역할 |
|--------|------|
| `StoryConfig` | 프롤로그·Act·랜드마크·NearMiss·도착 엔딩 |
| `StoryManager` | 프롤로그 재생 + 씬4 카메라 전환 |
| `StoryProgressDirector` | Act 순서 오케스트레이션 |
| `StoryEndingController` | 송전탑 도착 후 만남 엔딩 |
| `LandmarkManager` | 거리 비율로 추억 독백 |
| `DynamicEnvironmentManager` | 낮 → 노을 → 블루아워 |
| `UI_FinalDestinationController` | 진행바 · D-Day · Act 라벨 · 스토리/응원 |

## 연동

`GameSession`이 부트 시 Story 시스템을 붙이고, 프롤로그가 끝난 뒤에만 `IsRunning`을 켠다.  
`NearMissSystem.OnNearMissRewarded` → 목적지 UI 응원 텍스트.  
`DestinationGate` 도착 → `EndRun()` → `StoryEndingController.PlayArrivalEnding()`.  
진행도는 `player.PathDistance / upgrades.TowerDistance`.

## 챕터 · BGM · 분위기 상세

→ **`ChapterAudioAtmosphere.md`**  
(메인메뉴~도착 전 Act, BGM/SFX 파일명, 랜드마크 20·응원 라인, Claude 연동 훅, P0~P2 체크리스트)
