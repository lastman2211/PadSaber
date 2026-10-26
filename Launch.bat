@echo off
rem Starts Beat Saber with the mod loaded. Plug in an Xbox-style controller first.
rem Uses the folder chosen in Install.bat (so non-Steam installs work); otherwise starts it through Steam.
set "GAME="
if exist "%~dp0install_path.txt" set /p GAME=<"%~dp0install_path.txt"
if defined GAME if exist "%GAME%\Beat Saber.exe" (
    start "" /d "%GAME%" "%GAME%\Beat Saber.exe"
    exit /b
)
start "" "steam://rungameid/620980"
