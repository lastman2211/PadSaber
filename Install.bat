@echo off
rem Installs the mod into your Beat Saber folder (and the BSIPA mod loader if it is missing).
rem It finds Steam and Oculus installs by itself and asks you to confirm or pick a folder.
rem Optional, no question asked: Install.bat "D:\Games\Beat Saber"
powershell -NoProfile -ExecutionPolicy Bypass -STA -File "%~dp0Install.ps1" %*
echo.
pause
