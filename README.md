# BSnonvrmod

Play Beat Saber **1.40.8** on a normal PC with an Xbox-style (XInput) gamepad. No headset or OpenXR runtime needed. Includes head look, manual and automatic dodging, aim assist toward the next block, and controller rumble.

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
| Look around (head yaw / pitch) | D-pad |
| Recenter view | Click either stick (L3 / R3) |
| Side-step left / right | LB / RB |
| Duck (always manual) | Hold B |
| Click / triggers | A or RT / LT |
| Menus: turn the view (the laser stays at screen center) | Right stick |
| Scroll lists | Left stick |
| Sensitivity preset (Soft / Normal / Fast) | Y |
| Toggle auto-dodge | Back (or F8) |
| Toggle aim assist | LT + Y (or F7) |
| Controls panel | X |
| Pause | Start |
| Hide on-screen hints | F9 |

The head only turns during songs; menus keep the view centered so the cursor lines up. Sabers stay in place when you look around, as they would in VR. Auto-dodge only steps sideways; ducking is always yours (hold B), and auto-dodge still side-steps while you duck. LB/RB override the automatic side-step while held. Rumble follows the game's own haptics (cuts and so on).

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
