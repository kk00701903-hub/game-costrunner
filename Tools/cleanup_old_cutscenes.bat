@echo off
chcp 65001 >nul
REM 34차/47차: 옛 컷씬·옛 원고 잔여물 삭제 — C:\dev\game 에서 실행(더블클릭). 코드는 이미 이 파일들을 참조하지 않는다.
REM   (OpeningCinematic·ChapterVN·포토카드 폴백은 BG_* 사용, StoryVideo.LegacyVideos=false, CutsceneController 옛 원고 삭제,
REM    CH10/CH15 뒤 옛 시네마틱 재생 제거 — 47차.)
REM   옛 오프닝 영상 중 「멀리 버스 가는 컷」(VID_CH01_Open)은 47차에 Resources\CoastRun\Title_Bus.mp4 로 복사해 타이틀 앞에 쓴다 — 그건 지우지 않는다.
cd /d %~dp0\..
echo [1/4] 옛 컷씬 그림 Cut_*.png (32장)
del /q "Assets\Resources\CoastRun\Cut_*.png" "Assets\Resources\CoastRun\Cut_*.png.meta"
echo [2/4] 옛 컷씬 영상 Video\VID_*.mp4 (10개)
del /q "Assets\Resources\CoastRun\Video\VID_*.mp4" "Assets\Resources\CoastRun\Video\VID_*.mp4.meta"
echo [3/4] 옛 오프닝 클립 StreamingAssets\Opening\open_*.mp4 (10개)
del /q "Assets\StreamingAssets\Opening\open_*.mp4" "Assets\StreamingAssets\Opening\open_*.mp4.meta"
rmdir /q "Assets\StreamingAssets\Opening" 2>nul
del /q "Assets\StreamingAssets\Opening.meta" 2>nul
echo [4/4] 옛 대본 문서(보관용 — 남기려면 이 줄들을 지우고 실행)
rmdir /s /q "Docs\_archive_cutscenes_old"
del /q "Docs\STORY_SCRIPT_2026-09-08.md" "Docs\컷씬_스토리_요약_대화.md" "Docs\_story_dump.txt"
echo 완료. Unity 로 돌아가면 자동 리임포트됩니다. 그 다음 git add -A 로 삭제까지 커밋.
pause
