using System.Collections.Generic;
using NonVRMod.Input;
using UnityEngine;

namespace NonVRMod.Dodge
{
    /// <summary>
    /// Moves the fake head out of walls. Polls ObstacleControllers ~15x/s, finds the nearest
    /// side-step / duck pose that clears every wall inside the lookahead distance, and eases toward it.
    /// Uses renderer world bounds so it does not depend on obstacle internals.
    /// </summary>
    internal class AutoDodge : MonoBehaviour
    {
        private const float HeadHalfW = 0.28f, HeadHalfH = 0.22f, HeadHalfD = 0.30f;
        private const float PollInterval = 0.066f;

        private readonly List<Bounds> _threats = new List<Bounds>();
        private Vector3 _target;
        private float _nextPoll, _nextLog;

        private void Update()
        {
            var cfg = PluginConfig.Instance;
            if (UnityEngine.Input.GetKeyDown(cfg.AutoDodgeKey) || XInput.Down(XInput.Back))
            {
                cfg.AutoDodge = !cfg.AutoDodge;
                Plugin.Log.Info($"Auto-dodge {(cfg.AutoDodge ? "ON" : "OFF")}");
            }

            if (PoseProvider.ManualSideHeld) return; // LB/RB is overriding the side-step (B only ducks; it does not block this)

            if (!cfg.AutoDodge) _target = Vector3.zero;
            else if (Time.unscaledTime >= _nextPoll)
            {
                _nextPoll = Time.unscaledTime + PollInterval;
                _target = Solve(cfg);
            }

            // Auto-dodge only owns the side-step. Height belongs to the player (hold B), see PoseProvider.UpdateManualDodge.
            PoseProvider.HeadOffset.x = Mathf.MoveTowards(PoseProvider.HeadOffset.x, _target.x, cfg.DodgeSpeed * Time.unscaledDeltaTime);
        }

        private Vector3 Solve(PluginConfig cfg)
        {
            _threats.Clear();
            foreach (var o in FindObjectsOfType<ObstacleController>())
            {
                if (o == null || !o.gameObject.activeInHierarchy) continue;
                if (!WorldBounds(o, out Bounds b)) continue;
                // Wall overlaps the head's depth slab soon, and has not fully passed.
                if (b.min.z <= cfg.LookaheadMeters && b.max.z >= -HeadHalfD) _threats.Add(b);
            }
            if (cfg.DebugLog && Time.unscaledTime >= _nextLog)
            {
                _nextLog = Time.unscaledTime + 0.5f;
                Plugin.Log.Info($"AutoDodge threats={_threats.Count} head=({PoseProvider.HeadOffset.x:0.00},{PoseProvider.HeadOffset.y:0.00})" +
                                string.Join("", _threats.ConvertAll(t => $" [x {t.min.x:0.0}..{t.max.x:0.0} y {t.min.y:0.0}..{t.max.y:0.0} z {t.min.z:0.0}..{t.max.z:0.0}]")));
            }
            if (_threats.Count == 0) return Vector3.zero;

            // Evaluate at the player's current height, so a manual duck (B) under a ceiling wall counts as clear.
            float headY = cfg.StandingHeadHeight + PoseProvider.HeadOffset.y;
            Vector3 best = Vector3.zero;
            float bestCost = float.MaxValue;
            for (float x = -cfg.MaxSideStep; x <= cfg.MaxSideStep + 1e-4f; x += 0.1f)
            {
                int hits = CountHits(x, headY);
                // Fewest walls first (none when possible), then small moves, and avoid flipping sides.
                float cost = hits * 100f + Mathf.Abs(x) + Mathf.Abs(x - PoseProvider.HeadOffset.x) * 0.25f;
                if (cost < bestCost) { bestCost = cost; best = new Vector3(x, 0f, 0f); }
            }
            return best;
        }

        // World-space box of a wall: union of all its child renderers (the frame and the core are separate meshes, and the
        // first one found is not always the full wall). Falls back to the controller's own bounds.
        private static readonly List<Renderer> RendererBuf = new List<Renderer>();
        private static bool WorldBounds(ObstacleController o, out Bounds b)
        {
            b = default;
            bool any = false;
            o.GetComponentsInChildren(true, RendererBuf);
            foreach (var r in RendererBuf)
            {
                if (r == null) continue;
                if (!any) { b = r.bounds; any = true; }
                else b.Encapsulate(r.bounds);
            }
            RendererBuf.Clear();
            if (any && b.size.sqrMagnitude > 1e-4f) return true;
            b = o.bounds;
            return b.size.sqrMagnitude > 1e-4f;
        }

        private int CountHits(float x, float y)
        {
            int n = 0;
            foreach (var b in _threats)
            {
                bool xOverlap = x + HeadHalfW > b.min.x && x - HeadHalfW < b.max.x;
                bool yOverlap = y + HeadHalfH > b.min.y && y - HeadHalfH < b.max.y;
                if (xOverlap && yOverlap) n++;
            }
            return n;
        }
    }
}
