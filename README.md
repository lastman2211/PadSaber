# BSnonvrmod

Play Beat Saber **1.40.8** on a normal PC with an Xbox-style (XInput) gamepad. No headset or OpenXR runtime needed. Optional auto-dodge for walls.

You need your own Steam copy of the game. This repo contains only the mod; no game files.

## Install
1. Install Beat Saber 1.40.8 from Steam and close it.
2. Run `Install.bat`. It finds your game, installs the BSIPA mod loader if missing (downloaded from its official release), and copies the mod into `Plugins`.
   If the game is not found, run `Install.bat "D:\path\to\Beat Saber"`.
3. Plug in a controller and start the game with `Launch.bat` (or Steam's Play button). Do not use the `fpfc` launch option.

## Controls
Each stick sweeps its saber through an arc, so a flick is a swing and both hands move independently. When you let go, the saber returns to center slowly so the rebound does not cut anything.

| Action | Gamepad |
|---|---|
| Left / right saber | Left / right stick |
| Menu cursor | Right stick |
| Click | A or RT |
| Scroll lists | Left stick |
| Pause | Start |
| Toggle auto-dodge | Back (or F8) |
| Hide on-screen hints | F9 |

## Tuning
Edit `UserData\NonVRMod.json` in the game folder (the game creates it on first run) and restart the game.

| Setting | Meaning |
|---|---|
| `MaxYaw`, `MaxPitch` | How many degrees the saber swings at full stick |
| `FollowSpeed` | How fast the saber follows the stick (higher = snappier) |
| `ReturnSpeed` | How fast it recenters on release (lower = softer) |
| `HandHeight`, `HandSpread`, `HandDepth`, `HandShift` | Where the hands rest and how much they drift |
| `RestPitch` | Resting saber tilt |
| `CursorSpeed` | Menu cursor speed |
| `FieldOfView`, `HeadBack`, `Fullscreen` | The view |
| `AutoDodge`, `LookaheadMeters`, `MaxSideStep`, `DodgeSpeed` | Auto-dodge (offline play only) |

## Build from source
```
dotnet build -c Release -p:BeatSaberDir="C:\Program Files (x86)\Steam\steamapps\common\Beat Saber"
```
BSIPA must already be installed in that game folder. The DLL is copied to `Plugins` automatically; copy `bin\Release\net472\NonVRMod.dll` to `dist\` to update the installer.

## If it doesn't work
Open `Logs\_latest.log` in the game folder. Lines like `Patch target not found: X.Y` mean the game's class names differ from 1.40.8; the patches are in `Patches/`.
