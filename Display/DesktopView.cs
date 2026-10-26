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
                // Menu cursor driven by the right stick.
                var old = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, 0.95f);
                Box(PoseProvider.CursorN.x * Screen.width, Screen.height - PoseProvider.CursorN.y * Screen.height, 12);
                GUI.color = old;
            }

            string dodge = PluginConfig.Instance.AutoDodge ? "ON" : "OFF";
            string text = XInput.Connected
                ? $"L stick = left saber | R stick = right saber | menus: R stick = cursor, A/RT = click, L stick = scroll | Start = pause | Back = auto-dodge ({dodge}) | F9 hide"
                : "No gamepad detected. Plug in an Xbox-style controller (XInput).";
            GUI.Label(new Rect(10, 6, Screen.width - 20, 24), text, _label);
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
