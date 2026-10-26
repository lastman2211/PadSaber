@echo off
rem Installs the mod into your Beat Saber folder (and the BSIPA mod loader if it is missing).
rem Optional: Install.bat "D:\SteamLibrary\steamapps\common\Beat Saber"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install.ps1" %*
echo.
pause
