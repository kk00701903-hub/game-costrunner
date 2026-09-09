@echo off
chcp 65001 >nul
REM 34차: 옛 컷씬 잔여물 삭제 — C:\dev\game 에서 실행. 코드는 이미 이 파일들을 참조하지 않는다(OpeningCinematic·ChapterVN·포토카드 폴백 모두 BG_* 사용).
cd /d %~dp0\..
echo [1/3] 옛 컷씬 그림 Cut_*.png
del /q "Assets\Resources\CoastRun\Cut_*.png" "Assets\Resources\CoastRun\Cut_*.png.meta"
echo [2/3] 옛 컷씬 영상 Video\VID_*.mp4
del /q "Assets\Resources\CoastRun\Video\VID_*.mp4" "Assets\Resources\CoastRun\Video\VID_*.mp4.meta"
echo [3/3] 옛 대본 문서(보관용 — 남기려면 이 줄들을 지우고 실행)
rmdir /s /q "Docs\_archive_cutscenes_old"
del /q "Docs\STORY_SCRIPT_2026-09-08.md" "Docs\컷씬_스토리_요약_대화.md" "Docs\_story_dump.txt"
echo 완료. Unity 로 돌아가면 자동 리임포트됩니다.
pause
