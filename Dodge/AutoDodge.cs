using System.Collections.Generic;
using NonVRMod.Input;
using UnityEngine;

namespace NonVRMod.Dodge
{
    /// <summary>
    /// Moves the fake head out of walls (side-step and duck). Polls ObstacleControllers ~15x/s, finds the nearest
    /// side-step / duck pose that clears every wall inside the lookahead distance, and eases toward it.
    /// Mirrors the game's own test (PlayerHeadAndObstacleInteraction): the head is a point, and a wall hurts when the point
    /// is inside ObstacleController.bounds (the collision box, in the wall's local space). DodgeMargin pads that point.
    /// </summary>
    internal class AutoDodge : MonoBehaviour
    {
        private const float HeadHalfD = 0.30f;
        private const float PollInterval = 0.066f;

        private readonly List<Bounds> _threats = new List<Bounds>();
        private readonly List<float> _heights = new List<float>();
        private Vector3 _target;
        private float _nextPoll, _nextLog, _nextLog2;

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
                if (o == null || !o.gameObject.activeInHierarchy || o.hasPassedAvoidedMark) continue; // the game ignores walls past the avoid mark too
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
                    int hits = CountHits(x, h, cfg.DodgeMargin);
                    // Fewest walls first (none when possible; if nothing clears, the least-bad pose rather than freezing),
                    // then small moves. Ducking costs a little more than stepping, and flipping sides is discouraged.
                    float cost = hits * 100f + Mathf.Abs(x) + (stand - h) * 1.2f + Mathf.Abs(x - PoseProvider.HeadOffset.x) * 0.25f;
                    if (cost < bestCost) { bestCost = cost; best = new Vector3(x, h - stand, 0f); }
                }
            }
            if (cfg.DebugLog && Time.unscaledTime >= _nextLog2)
            {
                _nextLog2 = Time.unscaledTime + 0.5f;
                var cam = PoseProvider.Cam;
                Plugin.Log.Info($"AutoDodge choice=({best.x:0.00},{best.y:0.00}) cost={bestCost:0.0} heights={_heights.Count} autoDuck={cfg.AutoDuck} manualDuck={PoseProvider.ManualDuckHeld} " +
                                $"camWorld={(cam != null ? cam.transform.position.ToString("0.00") : "none")} headOffset=({PoseProvider.HeadOffset.x:0.00},{PoseProvider.HeadOffset.y:0.00})");
            }
            return best;
        }

        // World-space box of a wall's collision bounds (ObstacleController.bounds is in the wall's local space; the game
        // transforms the head point into that space). The 8 corners are transformed so a rotated (360 / 90) wall still works.
        // The visual mesh is NOT used: it is larger than the collision box (a duck wall looked like it started at 0.8 m).
        // Falls back to the renderers if the controller reports empty bounds.
        private static readonly List<Renderer> RendererBuf = new List<Renderer>();
        private static bool WorldBounds(ObstacleController o, out Bounds b)
        {
            Bounds local = o.bounds;
            if (local.size.sqrMagnitude > 1e-4f)
            {
                Transform t = o.transform;
                Vector3 c = local.center, e = local.extents;
                b = new Bounds(t.TransformPoint(c + new Vector3(-e.x, -e.y, -e.z)), Vector3.zero);
                for (int i = 1; i < 8; i++)
                    b.Encapsulate(t.TransformPoint(c + new Vector3((i & 1) == 0 ? -e.x : e.x, (i & 2) == 0 ? -e.y : e.y, (i & 4) == 0 ? -e.z : e.z)));
                return true;
            }

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
            return any && b.size.sqrMagnitude > 1e-4f;
        }

        // The head is a point in the game; the margin covers the time it takes the view to ease into place.
        private int CountHits(float x, float y, float margin)
        {
            int n = 0;
            foreach (var b in _threats)
            {
                bool xOverlap = x + margin > b.min.x && x - margin < b.max.x;
                bool yOverlap = y + margin > b.min.y && y - margin < b.max.y;
                if (xOverlap && yOverlap) n++;
            }
            return n;
        }
    }
}
