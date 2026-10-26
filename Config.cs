using System.Runtime.CompilerServices;
using IPA.Config.Stores;
using UnityEngine;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace NonVRMod
{
    internal class PluginConfig
    {
        public static PluginConfig Instance { get; set; }

        public virtual bool AutoDodge { get; set; } = true;
        public virtual KeyCode AutoDodgeKey { get; set; } = KeyCode.F8;
        public virtual float LookaheadMeters { get; set; } = 14f;
        public virtual float StandingHeadHeight { get; set; } = 1.7f;
        public virtual float DuckHeadHeight { get; set; } = 1.1f;
        public virtual float MaxSideStep { get; set; } = 1.3f;
        public virtual float DodgeSpeed { get; set; } = 6f;

        // Display
        public virtual bool Fullscreen { get; set; } = true;
        public virtual float FieldOfView { get; set; } = 90f;
        public virtual float HeadBack { get; set; } = 0f;          // meters to move the view backwards
        public virtual bool ShowOverlay { get; set; } = true;       // saber markers + hints (F9)

        // Gamepad controls
        public virtual float MaxYaw { get; set; } = 70f;            // degrees the saber swings sideways at full stick
        public virtual float MaxPitch { get; set; } = 80f;          // degrees the saber swings up/down at full stick
        public virtual float RestPitch { get; set; } = -10f;        // resting saber tilt (negative = slightly up)
        public virtual float HandShift { get; set; } = 0.15f;       // meters the hand itself drifts with the stick
        public virtual float FollowSpeed { get; set; } = 45f;       // how fast the saber follows the stick (higher = snappier)
        public virtual float ReturnSpeed { get; set; } = 7f;        // how fast it recenters when you let go (lower = softer)
        public virtual float HandHeight { get; set; } = 1.2f;       // resting hand height
        public virtual float HandSpread { get; set; } = 0.25f;       // resting distance of each hand from the center line
        public virtual float HandDepth { get; set; } = 0.7f;        // how far in front of the head the sabers sit
        public virtual float CursorSpeed { get; set; } = 0.9f;      // menu cursor speed (screens per second)

        public virtual void OnReload() { }
        public virtual void CopyFrom(PluginConfig other) { }
    }
}
