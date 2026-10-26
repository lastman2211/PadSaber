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
        private float _nextPoll;

        private void Update()
        {
            var cfg = PluginConfig.Instance;
            if (UnityEngine.Input.GetKeyDown(cfg.AutoDodgeKey) || XInput.Down(XInput.Back))
            {
                cfg.AutoDodge = !cfg.AutoDodge;
                Plugin.Log.Info($"Auto-dodge {(cfg.AutoDodge ? "ON" : "OFF")}");
            }

            if (PoseProvider.ManualDodgeHeld) return; // manual lean/duck is overriding

            if (!cfg.AutoDodge) _target = Vector3.zero;
            else if (Time.unscaledTime >= _nextPoll)
            {
                _nextPoll = Time.unscaledTime + PollInterval;
                _target = Solve(cfg);
            }

            PoseProvider.HeadOffset = Vector3.MoveTowards(PoseProvider.HeadOffset, _target, cfg.DodgeSpeed * Time.unscaledDeltaTime);
        }

        private Vector3 Solve(PluginConfig cfg)
        {
            _threats.Clear();
            foreach (var o in FindObjectsOfType<ObstacleController>())
            {
                if (o == null || !o.gameObject.activeInHierarchy) continue;
                var r = o.GetComponentInChildren<Renderer>();
                if (r == null) continue;
                Bounds b = r.bounds;
                // Wall overlaps the head's depth slab soon, and has not fully passed.
                if (b.min.z <= cfg.LookaheadMeters && b.max.z >= -HeadHalfD) _threats.Add(b);
            }
            if (_threats.Count == 0) return Vector3.zero;

            float stand = cfg.StandingHeadHeight, duck = cfg.DuckHeadHeight - stand;
            Vector3 best = Vector3.zero;
            float bestCost = float.MaxValue;
            for (float x = -cfg.MaxSideStep; x <= cfg.MaxSideStep + 1e-4f; x += 0.1f)
            {
                foreach (float dy in new[] { 0f, duck })
                {
                    if (Hits(x, stand + dy)) continue;
                    // Prefer small moves, avoid ducking unless needed, and avoid flipping sides.
                    float cost = Mathf.Abs(x) + Mathf.Abs(dy) * 1.5f + Mathf.Abs(x - PoseProvider.HeadOffset.x) * 0.25f;
                    if (cost < bestCost) { bestCost = cost; best = new Vector3(x, dy, 0f); }
                }
            }
            return bestCost < float.MaxValue ? best : PoseProvider.HeadOffset; // no safe pose: hold position
        }

        private bool Hits(float x, float y)
        {
            foreach (var b in _threats)
            {
                bool xOverlap = x + HeadHalfW > b.min.x && x - HeadHalfW < b.max.x;
                bool yOverlap = y + HeadHalfH > b.min.y && y - HeadHalfH < b.max.y;
                if (xOverlap && yOverlap) return true;
            }
            return false;
        }
    }
}
