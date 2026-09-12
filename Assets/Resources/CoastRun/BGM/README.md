# BGM 폴더

48차-15(사용자 지시): 챕터 스템·시네마·엔딩·메모리·메뉴·타이틀 BGM 은 모두 삭제. 49차: M8~M12 추가.

| 파일 | 어디서 |
|---|---|
| `BGM_M5.ogg` | 부팅·타이틀·메인 (`TitleAudio` / `CoastBgmLibrary.Menu`) |
| `BGM_M2 / M4 / M7 / M8 / M11 / M12` | K-POP 한 곡 달리기 (`ArcadeRun.KpopTracks`) |
| `BGM_M9 / M10` | 스토리 러닝 — 홀수 스테이지 M9, 짝수 스테이지 M10 (`CoastBgmLibrary.Story`) |
| `BGM_M1~M7` | 레코드(컬렉션) 화면 재생 |

M8~M12 는 원본 wav(48kHz) → -14 LUFS 통일, 50ms 페이드인, Vorbis 128k.

Import 설정 권장: Load Type **Streaming**, Compression **Vorbis** 품질 70, Preload Audio Data 끔.
