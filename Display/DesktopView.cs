using NonVRMod.Input;
using UnityEngine;

namespace NonVRMod.Display
{
    /// <summary>Fullscreen setup and an on-screen overlay showing where the sabers are (F9 toggles).</summary>
    internal class DesktopView : MonoBehaviour
    {
        private bool _overlay = true;
        private GUIStyle _label;

        private float _nextFs;

        private void Start() => _overlay = PluginConfig.Instance.ShowOverlay;

        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.F9)) _overlay = !_overlay;

            // The game's settings loader applies its own window mode shortly after startup, so keep asking for
            // borderless fullscreen for the first 30 seconds instead of only once.
            if (PluginConfig.Instance.Fullscreen && Time.realtimeSinceStartup < 30f && Time.realtimeSinceStartup >= _nextFs)
            {
                _nextFs = Time.realtimeSinceStartup + 1f;
                var d = UnityEngine.Display.main;
                if (Screen.fullScreenMode != FullScreenMode.FullScreenWindow || Screen.width != d.systemWidth)
                {
                    Screen.SetResolution(d.systemWidth, d.systemHeight, FullScreenMode.FullScreenWindow);
                    Plugin.Log.Info($"Display: requesting fullscreen {d.systemWidth}x{d.systemHeight}");
                }
            }
        }

        private void OnGUI()
        {
            if (!_overlay) return;
            var cam = PoseProvider.Cam;
            if (_label == null) _label = new GUIStyle(GUI.skin.label) { fontSize = 14 };

            if (cam != null && PoseProvider.InGame)
            {
                DrawHand(cam, PoseProvider.LeftPos, PoseProvider.LeftRot, new Color(1f, 0.25f, 0.25f, 0.9f));
                DrawHand(cam, PoseProvider.RightPos, PoseProvider.RightRot, new Color(0.25f, 0.6f, 1f, 0.9f));
            }
            else if (!PoseProvider.InGame)
            {
                // Menus: the right stick turns the view and the laser stays at screen center, so no pointer is drawn.
                if (PluginConfig.Instance.MenuCrosshair)
                {
                    var old = GUI.color;
                    GUI.color = new Color(1f, 1f, 1f, 0.8f);
                    Box(Screen.width / 2f, Screen.height / 2f, 5);
                    GUI.color = old;
                }
            }

            string dodge = PluginConfig.Instance.AutoDodge ? "ON" : "OFF";
            string text = XInput.Connected
                ? $"Preset: {PoseProvider.PresetName} (Y) | Look: D-pad (stick click recenters) | Dodge: LB/RB, duck: B (auto too) | Auto-dodge {dodge} (Back) | Aim assist {(PluginConfig.Instance.AimAssistOn ? "ON" : "OFF")} (LT+Y) | Help: X | Hide: F9"
                : "No gamepad detected. Plug in an Xbox-style controller (XInput).";
            GUI.Label(new Rect(10, 6, Screen.width - 20, 24), text, _label);

            if (Time.unscaledTime < PoseProvider.PresetShownUntil)
            {
                var big = new GUIStyle(GUI.skin.label) { fontSize = 30, alignment = TextAnchor.MiddleCenter };
                GUI.Label(new Rect(0, Screen.height * 0.12f, Screen.width, 50), $"Sensitivity: {PoseProvider.PresetName}", big);
            }

            if (Time.unscaledTime < PoseProvider.AimAssistShownUntil)
            {
                var big = new GUIStyle(GUI.skin.label) { fontSize = 30, alignment = TextAnchor.MiddleCenter };
                GUI.Label(new Rect(0, Screen.height * 0.18f, Screen.width, 50), $"Aim assist: {(PluginConfig.Instance.AimAssistOn ? "ON" : "OFF")}", big);
            }

            if (PoseProvider.ShowHelp) DrawHelp();
        }

        private void DrawHelp()
        {
            var cfg = PluginConfig.Instance;
            string text =
                "CONTROLS (X closes this)\n\n" +
                "Left / right stick      left / right saber (flick = swing)\n" +
                "D-pad                   look around (yaw / pitch)\n" +
                "L3 / R3 (stick click)   recenter view\n" +
                "LB / RB                 side-step left / right\n" +
                "B (hold)                duck by hand (auto-dodge ducks too)\n" +
                "A or RT / LT            click / triggers\n" +
                "Y                       sensitivity preset (Soft / Normal / Fast)\n" +
                "Back                    toggle auto-dodge\n" +
                "Start                   pause\n" +
                "F9                      hide on-screen hints\n\n" +
                $"Preset: {PoseProvider.PresetName}    Auto-dodge: {(cfg.AutoDodge ? "ON" : "OFF")}\n" +
                $"Head yaw {PoseProvider.HeadYaw:0}   pitch {PoseProvider.HeadPitch:0}";
            var style = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 18, padding = new RectOffset(20, 20, 16, 16) };
            float w = 640, h = 440;
            GUI.Box(new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h), text, style);
        }

        private static void DrawHand(Camera cam, Vector3 pos, Quaternion rot, Color c)
        {
            Vector3 a = cam.WorldToScreenPoint(pos);
            Vector3 b = cam.WorldToScreenPoint(pos + rot * Vector3.forward * 1.0f);
            if (a.z <= 0f) return;
            var old = GUI.color;
            GUI.color = c;
            Box(a.x, Screen.height - a.y, 10);
            if (b.z > 0f) Box(b.x, Screen.height - b.y, 16);
            GUI.color = old;
        }

        private static void Box(float x, float y, float size) =>
            GUI.DrawTexture(new Rect(x - size / 2f, y - size / 2f, size, size), Texture2D.whiteTexture);
    }
}
