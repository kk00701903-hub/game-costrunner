# BGM 슬롯 폴더

여기에 `BGM_제작발주서.md`의 파일명 그대로 `.ogg`(권장) / `.wav` / `.mp3`를 넣으면
다음 Play부터 자동으로 재생됩니다. 파일이 없는 슬롯은 절차적 사운드가 대신합니다.

| 파일 | 어디서 |
|---|---|
| `BGM_Menu.ogg` / `BGM_Menu_Cleared.ogg` | 타이틀 (`TitleAudio`) |
| `BGM_CH1_a.ogg` `_b` `_c` … `BGM_CH4_c.ogg` | 주행 스템 — 스테이지 1→a, 2→a+b, 3+→a+b+c (`CoastAudioManager.SetChapterStage`) |
| `BGM_CH5_a/b/c/d.ogg` | 주행 — 역방향 소거, S20은 d만 |

Import 설정 권장: Load Type **Streaming**, Compression **Vorbis** 품질 70, Preload Audio Data 끔.
스템 a/b/c는 **길이·BPM·시작 지점이 완전히 같아야** 합니다(동시 시작 후 볼륨만 페이드).
