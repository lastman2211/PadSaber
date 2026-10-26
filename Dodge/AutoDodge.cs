using System.Collections.Generic;
using NonVRMod.Input;
using UnityEngine;

namespace NonVRMod.Dodge
{
    /// <summary>
    /// Moves the fake head out of walls (side-step and duck). Polls ObstacleControllers ~15x/s, finds the nearest
    /// side-step / duck pose that clears every wall inside the lookahead distance, and eases toward it.
    /// Uses renderer world bounds so it does not depend on obstacle internals.
    /// </summary>
    internal class AutoDodge : MonoBehaviour
    {
        private const float HeadHalfW = 0.28f, HeadHalfH = 0.22f, HeadHalfD = 0.30f;
        private const float PollInterval = 0.066f;

        private readonly List<Bounds> _threats = new List<Bounds>();
        private readonly List<float> _heights = new List<float>();
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

            if (!cfg.AutoDodge) _target = Vector3.zero;
            else if (Time.unscaledTime >= _nextPoll)
            {
                _nextPoll = Time.unscaledTime + PollInterval;
                _target = Solve(cfg);
            }

            // x is skipped while LB/RB is held and y while B is held (the player is in charge of that axis).
            float step = cfg.DodgeSpeed * Time.unscaledDeltaTime;
            if (!PoseProvider.ManualSideHeld) PoseProvider.HeadOffset.x = Mathf.MoveTowards(PoseProvider.HeadOffset.x, _target.x, step);
            if (!PoseProvider.ManualDuckHeld) PoseProvider.HeadOffset.y = Mathf.MoveTowards(PoseProvider.HeadOffset.y, cfg.AutoDuck ? _target.y : 0f, step);
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

            // Heights to try: standing, then lower in 10 cm steps down to AutoDuckMinHeight. While B is held the player is
            // ducking by hand, so only that height is tried (the side-step still adapts to it).
            float stand = cfg.StandingHeadHeight;
            _heights.Clear();
            if (PoseProvider.ManualDuckHeld) _heights.Add(stand + PoseProvider.HeadOffset.y);
            else
            {
                _heights.Add(stand);
                if (cfg.AutoDuck)
                    for (float h = stand - 0.1f; h >= cfg.AutoDuckMinHeight - 1e-4f; h -= 0.1f) _heights.Add(h);
            }

            Vector3 best = Vector3.zero;
            float bestCost = float.MaxValue;
            for (float x = -cfg.MaxSideStep; x <= cfg.MaxSideStep + 1e-4f; x += 0.1f)
            {
                foreach (float h in _heights)
                {
                    int hits = CountHits(x, h);
                    // Fewest walls first (none when possible; if nothing clears, the least-bad pose rather than freezing),
                    // then small moves. Ducking costs a little more than stepping, and flipping sides is discouraged.
                    float cost = hits * 100f + Mathf.Abs(x) + (stand - h) * 1.2f + Mathf.Abs(x - PoseProvider.HeadOffset.x) * 0.25f;
                    if (cost < bestCost) { bestCost = cost; best = new Vector3(x, h - stand, 0f); }
                }
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
