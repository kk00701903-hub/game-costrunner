@echo off
cd /d %~dp0\..
del /q "Assets\_CoastRun\Scripts\Player\PlayerController-1.cs" "Assets\_CoastRun\Scripts\Player\PlayerController-1.cs.meta" "Assets\_CoastRun\Scripts\Player\SkaterRig-1.cs" "Assets\_CoastRun\Scripts\Player\SkaterRig-1.cs.meta" "Assets\_CoastRun\Scripts\UI\MainMenuController-1.cs" "Assets\_CoastRun\Scripts\UI\MainMenuController-1.cs.meta"
echo done
