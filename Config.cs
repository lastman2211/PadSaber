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
        public virtual bool ManualDodge { get; set; } = true;       // LB/RB side-step, hold B to duck
        public virtual bool AutoDuck { get; set; } = true;          // auto-dodge also lowers the view under low walls (B still ducks by hand)
        public virtual float AutoDuckMinHeight { get; set; } = 0.85f; // lowest the view may go for an automatic duck (meters)
        public virtual bool DebugLog { get; set; } = false;         // verbose auto-dodge / aim-assist logging

        // Display
        public virtual bool Fullscreen { get; set; } = true;
        public virtual float FieldOfView { get; set; } = 90f;
        public virtual float HeadBack { get; set; } = 0f;          // meters to move the view backwards
        public virtual bool ShowOverlay { get; set; } = true;       // saber markers + hints (F9)

        // Head look (D-pad)
        public virtual float LookSpeed { get; set; } = 90f;         // degrees per second
        public virtual float HeadMaxYaw { get; set; } = 60f;
        public virtual float HeadMaxPitch { get; set; } = 40f;
        public virtual float MenuLookSpeed { get; set; } = 110f;    // degrees per second the right stick turns the view in menus
        public virtual float MenuMaxYaw { get; set; } = 120f;       // how far the menu view can turn sideways (degrees) so side panels are reachable
        public virtual float MenuMaxPitch { get; set; } = 80f;      // how far it can look up/down in menus
        public virtual bool MenuCrosshair { get; set; } = false;    // tiny dot at the center of the screen in menus
        public virtual float HeadReturnSpeed { get; set; } = 0f;    // >0: view drifts back to center when D-pad is released

        // Gamepad controls
        public virtual bool SwapSticks { get; set; } = false;       // left-handed layout
        public virtual bool SquareStick { get; set; } = true;       // diagonal reaches full range on both axes (corner blocks)
        public virtual bool InvertY { get; set; } = false;
        public virtual float StickDeadzone { get; set; } = 0.18f;
        public virtual float ResponseCurve { get; set; } = 1.2f;    // 1 = linear, higher = finer control near center
        public virtual int Preset { get; set; } = 1;                // 0 Soft, 1 Normal, 2 Fast (Y cycles)
        public virtual bool AimAssistOn { get; set; } = true;       // pull a saber toward the nearest block of its color
        public virtual float AimAssist { get; set; } = 0.6f;        // 0..1 how strongly
        public virtual float AimAssistRange { get; set; } = 10f;    // meters ahead to look for blocks
        public virtual bool Rumble { get; set; } = true;
        public virtual float RumbleStrength { get; set; } = 1f;

        public virtual float MaxYaw { get; set; } = 70f;            // degrees the saber swings sideways at full stick
        public virtual float MaxPitch { get; set; } = 80f;          // degrees the saber swings up/down at full stick
        public virtual float RestPitch { get; set; } = -10f;        // resting saber tilt (negative = slightly up)
        public virtual float HandShift { get; set; } = 0.15f;       // meters the hand itself drifts with the stick
        public virtual float FollowSpeed { get; set; } = 45f;       // how fast the saber follows the stick (higher = snappier)
        public virtual float ReturnSpeed { get; set; } = 7f;        // how fast it recenters when you let go (lower = softer)
        public virtual float HandHeight { get; set; } = 1.2f;       // resting hand height
        public virtual float HandSpread { get; set; } = 0.25f;      // resting distance of each hand from the center line
        public virtual float HandDepth { get; set; } = 0.7f;        // how far in front of the head the sabers sit

        public virtual void OnReload() { }
        public virtual void CopyFrom(PluginConfig other) { }
    }
}
