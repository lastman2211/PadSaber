@echo off
rem Developer tool: builds the mod for the Beat Saber install given as the first argument (or the default Steam folder).
if "%~1"=="" (powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Build.ps1") else (powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Build.ps1" -GameDir "%~1")
pause
