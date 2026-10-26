using System.Collections.Generic;
using UnityEngine;

namespace NonVRMod.Input
{
    /// <summary>
    /// Pulls a saber's aim toward the next incoming block of its color, but only while the player pushes the stick that
    /// way. A centered stick gets no help, so letting go never makes the saber swing by itself, and the player still has
    /// to flick through the block (the game judges cut direction and speed).
    /// </summary>
    internal static class AimAssist
    {
        private const float RescanInterval = 0.1f;   // how often the list of live notes is refreshed
        private const float CutPlaneAhead = 0.6f;    // aim at the lane position this far in front of the hand
        private const float MinStick = 0.15f, FullStick = 0.6f;

        private static readonly List<NoteController> Notes = new List<NoteController>();
        private static float _nextScan;
        private static float _nextLog;

        /// <param name="color">0 = left (red) hand, 1 = right (blue) hand.</param>
        /// <param name="stick">Response-curved stick, -1..1.</param>
        public static void Apply(PluginConfig cfg, int color, Vector3 hand, Vector2 stick,
                                 float maxYaw, float maxPitch, float restPitch, ref float yaw, ref float pitch)
        {
            if (!cfg.AimAssistOn || cfg.AimAssist <= 0f || !PoseProvider.InGame) return;
            float mag = stick.magnitude;
            if (mag < MinStick) return;
            if (!TryNext(color, hand, cfg.AimAssistRange, out Vector3 note)) return;

            // Where the blade has to point to cover that block's lane and row at the cut plane.
            Vector3 d = new Vector3(note.x, note.y, hand.z + CutPlaneAhead) - hand;
            float ty = Mathf.Clamp(Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, -maxYaw * 1.2f, maxYaw * 1.2f);
            float tp = -Mathf.Atan2(d.y, Mathf.Sqrt(d.x * d.x + d.z * d.z)) * Mathf.Rad2Deg;

            // Same thing in stick space: is the player already reaching toward the block?
            Vector2 want = new Vector2(ty / maxYaw, -(tp - restPitch) / maxPitch);
            float dot = want.sqrMagnitude < 1e-4f ? 0f : Vector2.Dot(stick.normalized, want.normalized);
            float side = Mathf.Clamp01((dot - 0.2f) / 0.5f);
            float w = cfg.AimAssist * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(MinStick, FullStick, mag)) * side;
            if (w <= 0f) return;

            yaw = Mathf.Lerp(yaw, ty, w);
            pitch = Mathf.Lerp(pitch, tp, w);

            if (cfg.DebugLog && Time.unscaledTime >= _nextLog)
            {
                _nextLog = Time.unscaledTime + 0.5f;
                Plugin.Log.Info($"AimAssist hand={color} note=({note.x:0.00},{note.y:0.00},{note.z:0.00}) stick=({stick.x:0.00},{stick.y:0.00}) w={w:0.00} yaw->{ty:0} pitch->{tp:0}");
            }
        }

        private static bool TryNext(int color, Vector3 hand, float range, out Vector3 pos)
        {
            if (Time.unscaledTime >= _nextScan)
            {
                _nextScan = Time.unscaledTime + RescanInterval;
                Notes.Clear();
                Notes.AddRange(Object.FindObjectsOfType<NoteController>());
            }

            pos = default;
            float bestZ = float.MaxValue;
            foreach (var n in Notes)
            {
                if (n == null || n.noteTransform == null || !n.gameObject.activeInHierarchy) continue;
                var data = n.noteData;
                if (data == null || data.gameplayType == NoteData.GameplayType.Bomb) continue;
                if ((int)data.colorType != color) continue;
                Vector3 p = n.noteTransform.position;
                if (p.z < hand.z - 0.3f || p.z > range) continue;
                if (p.z < bestZ) { bestZ = p.z; pos = p; }
            }
            return bestZ < float.MaxValue;
        }
    }
}
