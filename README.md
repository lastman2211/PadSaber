# BSnonvrmod

Play Beat Saber **1.40.8** on a normal PC with an Xbox-style (XInput) gamepad. No headset or OpenXR runtime needed. Includes head look, manual and automatic dodging, aim assist toward the next block, and controller rumble.

You need your own Steam copy of the game. This repo contains only the mod; no game files.

## Install
1. Install Beat Saber 1.40.8 (Steam, Oculus/Meta Store or any other copy) and close it.
2. Run `Install.bat`. It looks for your game (Steam libraries and Oculus folders) and asks where Beat Saber is:
   - press **Enter** to use the folder it found,
   - **paste** a folder path (quotes and a trailing `\` are fine), or
   - type **B** to pick the folder in a window.
   The folder must contain `Beat Saber.exe`. It then installs the BSIPA mod loader if missing (downloaded from its official release) and copies the mod into `Plugins`. Your choice is remembered for next time.
   To skip the question: `Install.bat "D:\Games\Beat Saber"`.
3. Plug in a controller and start the game with `Launch.bat` (it starts the folder you chose, or Steam if none was saved), or as usual from Steam / your launcher. Do not use the `fpfc` launch option.

## Controls
Each stick sweeps its saber through an arc, so a flick is a swing and both hands move independently. When you let go, the saber returns to center slowly so the rebound does not cut anything.

| Action | Gamepad |
|---|---|
| Left / right saber | Left / right stick |
| Look around (head yaw / pitch) | D-pad |
| Recenter view | Click either stick (L3 / R3) |
| Side-step left / right | LB / RB |
| Duck by hand (auto-dodge also ducks) | Hold B |
| Click / triggers | A or RT / LT |
| Menus: turn the view (the laser stays at screen center) | Right stick |
| Scroll lists | Left stick |
| Sensitivity preset (Soft / Normal / Fast) | Y |
| Toggle auto-dodge | Back (or F8) |
| Toggle aim assist | LT + Y (or F7) |
| Controls panel | X |
| Pause | Start |
| Hide on-screen hints | F9 |

In menus the right stick turns the view and the laser stays at screen center. Sabers stay in place when you look around, as they would in VR. Auto-dodge steps sideways and ducks under low walls by itself. Hold B to duck by hand and LB/RB to side-step by hand; whichever axis you control, the other stays automatic. Rumble follows the game's own haptics (cuts and so on).

## Tuning
Edit `UserData\NonVRMod.json` in the game folder (the game creates it on first run) and restart the game.

| Setting | Meaning |
|---|---|
| `MaxYaw`, `MaxPitch` | How many degrees the saber swings at full stick |
| `FollowSpeed` | How fast the saber follows the stick (higher = snappier) |
| `ReturnSpeed` | How fast it recenters on release (lower = softer) |
| `Preset` | 0 Soft, 1 Normal, 2 Fast (Y cycles it) |
| `SquareStick` | On: a diagonal stick push reaches full range on both axes, so corner blocks are reachable |
| `ResponseCurve`, `StickDeadzone` | Stick feel: higher curve = finer control near center |
| `SwapSticks`, `InvertY` | Left-handed layout, inverted vertical axis |
| `HandHeight`, `HandSpread`, `HandDepth`, `HandShift` | Where the hands rest and how much they drift |
| `RestPitch` | Resting saber tilt |
| `LookSpeed`, `HeadMaxYaw`, `HeadMaxPitch`, `HeadReturnSpeed` | Head look (return speed above 0 recenters on release) |
| `FieldOfView`, `HeadBack`, `Fullscreen` | The view |
| `Rumble`, `RumbleStrength` | Controller vibration |
| `ManualDodge` | LB/RB/B dodging on or off |
| `AutoDuck`, `AutoDuckMinHeight` | Auto-dodge also lowers the view under low walls, down to this height (meters) |
| `AutoDodge`, `LookaheadMeters`, `MaxSideStep`, `DodgeSpeed` | Auto-dodge (offline play only) |
| `MenuLookSpeed`, `MenuCrosshair` | How fast the right stick turns the view in menus; optional center dot |
| `AimAssistOn`, `AimAssist`, `AimAssistRange` | Pull each saber toward the next block of its color while the stick is pushed that way (0 to 1, meters ahead) |
| `DebugLog` | Log auto-dodge walls and aim-assist targets to `Logs\_latest.log` |

## Build from source
```
dotnet build -c Release -p:BeatSaberDir="C:\Program Files (x86)\Steam\steamapps\common\Beat Saber"
```
BSIPA must already be installed in that game folder. The DLL is copied to `Plugins` automatically; copy `bin\Release\net472\NonVRMod.dll` to `dist\` to update the installer.

## If it doesn't work
Open `Logs\_latest.log` in the game folder. Lines like `Patch target not found: X.Y` mean the game's class names differ from 1.40.8; the patches are in `Patches/`. If the on-screen hint says no gamepad was detected, the controller must be XInput-compatible (Xbox layout) and connected before the game starts.
