using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SpatialTracking;

namespace NonVRMod.Input
{
    /// <summary>
    /// Gamepad-only fake head + saber poses.
    /// Sticks sweep the sabers through arcs (see Pose). D-pad looks around, LB/RB side-step, B ducks, Y cycles the
    /// sensitivity preset, X toggles the help panel. In menus the right stick moves a cursor the laser points at.
    /// </summary>
    internal class PoseProvider : MonoBehaviour
    {
        public static PoseProvider Instance { get; private set; }

        public static readonly string[] PresetNames = { "Soft", "Normal", "Fast" };
        // angle, follow speed, return speed multipliers per preset
        private static readonly Vector3[] PresetMul = { new Vector3(0.8f, 0.7f, 0.8f), Vector3.one, new Vector3(1.2f, 1.4f, 1.5f) };

        // Head pose is written by AutoDodge/manual dodge (offset) and read by PosePatches.
        public static Vector3 HeadOffset;   // x = side step, y = height delta from standing
        public static Vector3 HeadPosition => new Vector3(HeadOffset.x, PluginConfig.Instance.StandingHeadHeight + HeadOffset.y, -PluginConfig.Instance.HeadBack);
        public static Quaternion HeadRotation { get; private set; } = Quaternion.identity;
        public static float HeadYaw { get; private set; }
        public static float HeadPitch { get; private set; }

        public static Vector3 LeftPos { get; private set; } = new Vector3(-0.3f, 1.15f, 0.7f);
        public static Vector3 RightPos { get; private set; } = new Vector3(0.3f, 1.15f, 0.7f);
        public static Quaternion LeftRot { get; private set; } = Quaternion.identity;
        public static Quaternion RightRot { get; private set; } = Quaternion.identity;

        public static float LeftTrigger { get; private set; }
        public static float RightTrigger { get; private set; }
        public static bool MenuDown { get; private set; }
        public static bool MenuHeld { get; private set; }

        public static bool InGame { get; private set; }
        public static Camera Cam { get; private set; }
        public static Vector2 CursorN { get; private set; } = new Vector2(0.5f, 0.5f); // menu cursor, 0..1 screen coords
        public static bool ShowHelp { get; private set; }
        public static float PresetShownUntil { get; private set; }

        /// <summary>True while a manual dodge button is held; AutoDodge stays out of the way.</summary>
        public static bool ManualDodgeHeld =>
            PluginConfig.Instance.ManualDodge && (XInput.Held(XInput.LB) || XInput.Held(XInput.RB) || XInput.Held(XInput.B));

        public static string PresetName => PresetNames[Mathf.Clamp(PluginConfig.Instance.Preset, 0, PresetNames.Length - 1)];

        private readonly List<TrackedPoseDriver> _drivers = new List<TrackedPoseDriver>();
        private float _nextFind;
        private Vector2 _sl, _sr; // smoothed stick values

        private void Awake() => Instance = this;

        private void Update()
        {
            var cfg = PluginConfig.Instance;
            float dt = Time.unscaledDeltaTime;
            XInput.Deadzone = cfg.StickDeadzone;
            XInput.Poll();
            XInput.TickRumble(dt, cfg.Rumble ? cfg.RumbleStrength : 0f);

            if (Time.unscaledTime >= _nextFind)
            {
                _nextFind = Time.unscaledTime + 0.5f;
                _drivers.Clear();
                _drivers.AddRange(FindObjectsOfType<TrackedPoseDriver>());
                InGame = FindObjectOfType<SaberManager>() != null;
            }

            if (XInput.Down(XInput.Y))
            {
                cfg.Preset = (cfg.Preset + 1) % PresetNames.Length;
                PresetShownUntil = Time.unscaledTime + 2f;
            }
            if (XInput.Down(XInput.X)) ShowHelp = !ShowHelp;

            // Logical sticks/triggers (SwapSticks = left-handed layout).
            Vector2 ls = new Vector2(XInput.LX, XInput.LY), rs = new Vector2(XInput.RX, XInput.RY);
            float lt = XInput.LT, rt = XInput.RT;
            if (cfg.SwapSticks) { var t = ls; ls = rs; rs = t; var tt = lt; lt = rt; rt = tt; }
            if (cfg.InvertY) { ls.y = -ls.y; rs.y = -rs.y; }

            if (InGame) ApplyGameplayPoses(cfg, ls, rs, dt);
            else ApplyMenuPoses(cfg, rs, dt);

            UpdateHead(cfg, dt);
            UpdateManualDodge(cfg, dt);

            RightTrigger = Mathf.Max(rt, XInput.Held(XInput.A) ? 1f : 0f);
            LeftTrigger = lt;
            bool menuNow = XInput.Held(XInput.Start) || UnityEngine.Input.GetKey(KeyCode.Escape);
            MenuDown = menuNow && !MenuHeld;
            MenuHeld = menuNow;

            // No mouse needed: keep the OS cursor out of the picture.
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // ---- Sabers ---------------------------------------------------------------------------------------------

        private void ApplyGameplayPoses(PluginConfig cfg, Vector2 ls, Vector2 rs, float dt)
        {
            Vector3 m = PresetMul[Mathf.Clamp(cfg.Preset, 0, PresetMul.Length - 1)];
            _sl = Follow(_sl, ls, cfg.FollowSpeed * m.y, cfg.ReturnSpeed * m.z, dt);
            _sr = Follow(_sr, rs, cfg.FollowSpeed * m.y, cfg.ReturnSpeed * m.z, dt);
            Pose(cfg, m.x, -1f, _sl, out Vector3 pl, out Quaternion rl);
            Pose(cfg, m.x, +1f, _sr, out Vector3 pr, out Quaternion rr);
            LeftPos = pl; LeftRot = rl; RightPos = pr; RightRot = rr;
        }

        // Fast to follow the stick out, slow to return to center, so letting go doesn't produce a fast rebound
        // swing that the game would count as a wrong-way cut.
        private static Vector2 Follow(Vector2 cur, Vector2 target, float followRate, float returnRate, float dt)
        {
            float rate = target.sqrMagnitude >= cur.sqrMagnitude ? followRate : returnRate;
            return Vector2.Lerp(cur, target, 1f - Mathf.Exp(-rate * dt));
        }

        // Stick = where the saber points. The hand stays near its rest spot and pivots the saber, so a flick sweeps the
        // blade through an arc (like a wrist swing). Centered stick = saber pointing forward and slightly up.
        private static void Pose(PluginConfig cfg, float angleMul, float side, Vector2 s, out Vector3 pos, out Quaternion rot)
        {
            float cx = Curve(s.x, cfg.ResponseCurve), cy = Curve(s.y, cfg.ResponseCurve);
            float yaw = cx * cfg.MaxYaw * angleMul;
            float pitch = -cy * cfg.MaxPitch * angleMul + cfg.RestPitch; // stick up = saber tilts up (negative X euler = up)
            rot = Quaternion.Euler(pitch, yaw, 0f);
            pos = new Vector3(side * cfg.HandSpread + cx * cfg.HandShift,
                              cfg.HandHeight + cy * cfg.HandShift,
                              cfg.HandDepth - cfg.HeadBack);
        }

        private static float Curve(float v, float exp) => Mathf.Sign(v) * Mathf.Pow(Mathf.Abs(v), Mathf.Max(0.5f, exp));

        // Menus: park the left hand, move a cursor with the right stick, point the right laser through it.
        private void ApplyMenuPoses(PluginConfig cfg, Vector2 rs, float dt)
        {
            CursorN = new Vector2(Mathf.Clamp01(CursorN.x + rs.x * cfg.CursorSpeed * dt),
                                  Mathf.Clamp01(CursorN.y + rs.y * cfg.CursorSpeed * dt));
            Vector3 head = HeadPosition;
            LeftPos = head + new Vector3(-0.25f, -0.4f, 0.35f);
            RightPos = head + new Vector3(0.25f, -0.4f, 0.35f);
            LeftRot = Quaternion.identity;
            RightRot = Quaternion.identity;
            if (Cam == null) return;
            Ray ray = Cam.ScreenPointToRay(new Vector3(CursorN.x * Screen.width, CursorN.y * Screen.height, 0f));
            Vector3 dir = ray.GetPoint(8f) - RightPos;
            if (dir.sqrMagnitude > 1e-4f) RightRot = Quaternion.LookRotation(dir.normalized);
        }

        // ---- Head -----------------------------------------------------------------------------------------------

        // D-pad looks around (in songs only; menus keep the view centered so the cursor stays aligned).
        // Clicking either stick recenters.
        private void UpdateHead(PluginConfig cfg, float dt)
        {
            float dx = (XInput.Held(XInput.DRight) ? 1f : 0f) - (XInput.Held(XInput.DLeft) ? 1f : 0f);
            float dy = (XInput.Held(XInput.DUp) ? 1f : 0f) - (XInput.Held(XInput.DDown) ? 1f : 0f);
            if (cfg.InvertY) dy = -dy;

            if (!InGame || XInput.Down(XInput.L3) || XInput.Down(XInput.R3))
            {
                HeadYaw = Mathf.MoveTowards(HeadYaw, 0f, 400f * dt);
                HeadPitch = Mathf.MoveTowards(HeadPitch, 0f, 400f * dt);
            }
            else
            {
                if (dx != 0f || dy != 0f)
                {
                    HeadYaw = Mathf.Clamp(HeadYaw + dx * cfg.LookSpeed * dt, -cfg.HeadMaxYaw, cfg.HeadMaxYaw);
                    HeadPitch = Mathf.Clamp(HeadPitch - dy * cfg.LookSpeed * dt, -cfg.HeadMaxPitch, cfg.HeadMaxPitch);
                }
                else if (cfg.HeadReturnSpeed > 0f)
                {
                    HeadYaw = Mathf.MoveTowards(HeadYaw, 0f, cfg.HeadReturnSpeed * dt);
                    HeadPitch = Mathf.MoveTowards(HeadPitch, 0f, cfg.HeadReturnSpeed * dt);
                }
            }
            HeadRotation = Quaternion.Euler(HeadPitch, HeadYaw, 0f);
        }

        // LB / RB side-step, hold B to duck. While held it overrides auto-dodge.
        private void UpdateManualDodge(PluginConfig cfg, float dt)
        {
            if (!ManualDodgeHeld) return;
            float x = (XInput.Held(XInput.RB) ? 1f : 0f) - (XInput.Held(XInput.LB) ? 1f : 0f);
            float y = XInput.Held(XInput.B) ? cfg.DuckHeadHeight - cfg.StandingHeadHeight : 0f;
            Vector3 target = new Vector3(x * cfg.MaxSideStep, y, 0f);
            HeadOffset = Vector3.MoveTowards(HeadOffset, target, cfg.DodgeSpeed * dt);
        }

        private void LateUpdate()
        {
            var cfg = PluginConfig.Instance;
            Cam = null;
            foreach (var d in _drivers)
            {
                if (d == null) continue;
                d.enabled = false; // reads XR devices directly; with no headset it would pin the camera to the origin
                d.transform.localPosition = HeadPosition;
                d.transform.localRotation = HeadRotation;
                var cam = d.GetComponent<Camera>();
                if (cam == null) continue;
                // Same reset the game's own desktop mode (FirstPersonFlyingController) does.
                if (cam.stereoTargetEye != StereoTargetEyeMask.None) cam.stereoTargetEye = StereoTargetEyeMask.None;
                if (!Mathf.Approximately(cam.fieldOfView, cfg.FieldOfView)) { cam.fieldOfView = cfg.FieldOfView; cam.ResetAspect(); }
                if (Cam == null) Cam = cam;
            }
        }
    }
}
