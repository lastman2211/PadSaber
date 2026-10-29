# PadSaber: how to play Beat Saber without VR (step by step)

This takes about 5 minutes. You need **Beat Saber on Windows** and an **Xbox-style gamepad** (an Xbox controller, or any controller that works as "XInput").

## 1. Get the right game version
The mod works with two Beat Saber versions: **1.40.8** and **1.45.3**.

- Most people have **1.45.3** (the current Steam version). Nothing to do.
- Need 1.40.8? In Steam, right-click Beat Saber > **Properties** > **Betas**, then pick `legacy1.40.8_unity_v2021.3.16f1`.

Close Beat Saber before the next step.

## 2. Download and unzip the mod
1. Go to the **Releases** page of this project and download `PadSaber-0.9.1.zip`.
2. Right-click the zip > **Extract All**. Put the folder anywhere, for example your Desktop.

## 3. Install it
1. Open the extracted folder and double-click **`Install.bat`**.
   (If Windows says "Windows protected your PC", click **More info** > **Run anyway**.)
2. It looks for your game and asks where Beat Saber is:
   - If it found it, it shows `Detected: ...`. Just press **Enter**.
   - If not, paste the folder that contains `Beat Saber.exe`, or type **B** and pick the folder in a window.
3. Wait for **"Done."** The first time it downloads the mod loader, so you need an internet connection.

If it says your game version "is not supported", go back to step 1.
If it says to **start the game once and close it**, do that, then run `Install.bat` again.

## 4. Start the game
1. **Plug in your controller first.**
2. Double-click **`Launch.bat`** (or press Play in Steam as usual).
3. You'll see a hint line at the top of the screen. If it says no gamepad was detected, check that the controller is plugged in and restart the game.

## 5. Using the menus
- **Turn the view:** right stick. A small laser points at the middle of the screen, like a crosshair. Turn the view until the laser is on a button.
- **Click:** **A** (or RT).
- **Scroll a list:** left stick.
- **Pick a song and press Play**, same as normal.

## 6. Playing a song
| What you want | Press |
|---|---|
| Swing the **left** saber | Left stick (flick it, like a wrist swing) |
| Swing the **right** saber | Right stick |
| Look around | D-pad |
| Recenter the view | Click a stick (L3 / R3) |
| Dodge **left / right** | LB / RB |
| **Duck** | Hold B |
| Pause | Start |

Good to know:
- **Diagonal blocks:** push the stick toward a corner, and the saber reaches the corner blocks.
- **Auto-dodge is on by default.** It steps aside and ducks for walls by itself. You can still dodge by hand any time. Press **Back** to turn it off.
- **Aim assist:** when you push a stick toward a block, the saber is gently pulled to the next block of its color. You still have to swing. Press **LT + Y** to turn it off.
- **Too fast or too slow?** Press **Y** to switch the sensitivity between Soft, Normal and Fast.
- **Forgot a button?** Press **X** to show all controls on screen.
- **Hide the hint line:** press **F9**.
- Your controller rumbles when you cut blocks.

## If something goes wrong
- **The mod doesn't seem to load:** make sure you started the game *after* running `Install.bat`, and that the game version is 1.40.8 or 1.45.3.
- **Controller not detected:** use an Xbox-style controller, and plug it in *before* starting the game. A PlayStation controller needs a tool that makes it look like an Xbox one (for example Steam Input or DS4Windows).
- **I updated the game and the mod disappeared:** the mod loader moves old mods away when the game updates. Start the game once, close it, and run `Install.bat` again.
- **Still stuck:** open `Logs\_latest.log` in the game folder and look for lines containing `NonVRMod`. Include them if you ask for help.

## Tweaking and removing
- Settings live in `UserData\NonVRMod.json` in the game folder (the game creates it on first run). The README lists every setting.
- To remove the mod, delete `NonVRMod.dll` from the game's `Plugins` folder.

Enjoy! If you like it, there's an optional donation address in the README. The mod is free.
