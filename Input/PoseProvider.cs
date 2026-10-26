using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SpatialTracking;

namespace NonVRMod.Input
{
    /// <summary>
    /// Gamepad-only fake head + saber poses.
    /// In songs each stick maps directly to its saber's position (left stick = left saber, right stick = right saber),
    /// so a flick of the stick is a swing and the hands move independently. In menus the right stick moves a cursor
    /// that the laser points at; A or RT clicks, the left stick scrolls. Start = pause.
    /// </summary>
    internal class PoseProvider : MonoBehaviour
    {
        public static PoseProvider Instance { get; private set; }

        // Head pose is written by AutoDodge (offset) and read by PosePatches.
        public static Vector3 HeadOffset;   // x = side step, y = height delta from standing
        public static Vector3 HeadPosition => new Vector3(HeadOffset.x, PluginConfig.Instance.StandingHeadHeight + HeadOffset.y, -PluginConfig.Instance.HeadBack);

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

        private readonly List<TrackedPoseDriver> _drivers = new List<TrackedPoseDriver>();
        private float _nextFind;

        private void Awake() => Instance = this;

        private void Update()
        {
            var cfg = PluginConfig.Instance;
            float dt = Time.unscaledDeltaTime;
            XInput.Poll();

            if (Time.unscaledTime >= _nextFind)
            {
                _nextFind = Time.unscaledTime + 0.5f;
                _drivers.Clear();
                _drivers.AddRange(FindObjectsOfType<TrackedPoseDriver>());
                InGame = FindObjectOfType<SaberManager>() != null;
            }

            if (InGame) ApplyGameplayPoses(cfg, dt);
            else ApplyMenuPoses(cfg, dt);

            bool a = XInput.Held(XInput.A);
            RightTrigger = Mathf.Max(XInput.RT, a ? 1f : 0f);
            LeftTrigger = XInput.LT;
            bool menuNow = XInput.Held(XInput.Start) || UnityEngine.Input.GetKey(KeyCode.Escape);
            MenuDown = menuNow && !MenuHeld;
            MenuHeld = menuNow;

            // No mouse needed: keep the OS cursor out of the picture.
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Smoothed stick values. Fast to follow the stick out, slow to return to center, so letting go of a stick
        // doesn't produce a fast rebound swing that the game would count as a wrong-way cut.
        private Vector2 _sl, _sr;

        private void ApplyGameplayPoses(PluginConfig cfg, float dt)
        {
            _sl = Follow(_sl, new Vector2(XInput.LX, XInput.LY), cfg, dt);
            _sr = Follow(_sr, new Vector2(XInput.RX, XInput.RY), cfg, dt);
            Pose(cfg, -1f, _sl, out Vector3 pl, out Quaternion rl);
            Pose(cfg, +1f, _sr, out Vector3 pr, out Quaternion rr);
            LeftPos = pl; LeftRot = rl; RightPos = pr; RightRot = rr;
        }

        private static Vector2 Follow(Vector2 cur, Vector2 target, PluginConfig cfg, float dt)
        {
            bool outward = target.sqrMagnitude >= cur.sqrMagnitude;
            float rate = outward ? cfg.FollowSpeed : cfg.ReturnSpeed;
            return Vector2.Lerp(cur, target, 1f - Mathf.Exp(-rate * dt));
        }

        // Stick = where the saber points. The hand stays near its rest spot and pivots the saber, so a flick sweeps the
        // blade through an arc (like a wrist swing). Centered stick = saber pointing forward and slightly up.
        private static void Pose(PluginConfig cfg, float side, Vector2 s, out Vector3 pos, out Quaternion rot)
        {
            float cx = Curve(s.x), cy = Curve(s.y);
            float yaw = cx * cfg.MaxYaw;
            float pitch = -cy * cfg.MaxPitch + cfg.RestPitch; // stick up = saber tilts up (negative X euler = up)
            rot = Quaternion.Euler(pitch, yaw, 0f);
            pos = new Vector3(side * cfg.HandSpread + cx * cfg.HandShift,
                              cfg.HandHeight + cy * cfg.HandShift,
                              cfg.HandDepth - cfg.HeadBack);
        }

        private static float Curve(float v) => Mathf.Sign(v) * Mathf.Pow(Mathf.Abs(v), 1.2f);

        // Menus: park the left hand, move a cursor with the right stick, point the right laser through it.
        private void ApplyMenuPoses(PluginConfig cfg, float dt)
        {
            CursorN = new Vector2(Mathf.Clamp01(CursorN.x + XInput.RX * cfg.CursorSpeed * dt),
                                  Mathf.Clamp01(CursorN.y + XInput.RY * cfg.CursorSpeed * dt));
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

        private void LateUpdate()
        {
            var cfg = PluginConfig.Instance;
            Cam = null;
            foreach (var d in _drivers)
            {
                if (d == null) continue;
                d.enabled = false; // reads XR devices directly; with no headset it would pin the camera to the origin
                d.transform.localPosition = HeadPosition;
                d.transform.localRotation = Quaternion.identity;
                var cam = d.GetComponent<Camera>();
                if (cam == null) continue;
                // Same reset the game's own desktop mode (FirstPersonFlyingController) does.
                if (cam.stereoTargetEye != StereoTargetEyeMask.None) cam.stereoTargetEye = StereoTargetEyeMask.None;
                if (!Mathf.Approximately(cam.fieldOfView, cfg.FieldOfView)) { cam.fieldOfView = cfg.FieldOfView; cam.ResetAspect(); }
                if (Cam == null) Cam = cam;
            }
        }

        // Saber points forward and tilts toward the direction of motion so the game's velocity-based cut check sees a swing.
        private static Quaternion SwingRotation(Vector3 delta, Quaternion current, float dt)
        {
            Vector3 dir = Vector3.forward;
            if (dt > 0f && delta.sqrMagnitude > 1e-6f)
                dir = (Vector3.forward + Vector3.ClampMagnitude(delta / Mathf.Max(dt, 1e-3f), 6f) * 0.12f).normalized;
            return Quaternion.Slerp(current, Quaternion.LookRotation(dir), 0.5f);
        }
    }
}
