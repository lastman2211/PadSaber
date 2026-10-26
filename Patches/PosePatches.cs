using HarmonyLib;
using NonVRMod.Input;
using UnityEngine;
using UnityEngine.XR;

namespace NonVRMod.Patches
{
    /// <summary>
    /// Replaces the IVRPlatformHelper input surface (verified against Beat Saber 1.40.8) with the fake poses/buttons.
    /// Both UnityXRHelper (the real OpenXR helper) and DevicelessVRHelper are patched; whichever the game instantiates is covered.
    /// </summary>
    internal static class PosePatches
    {
        private static readonly string[] Helpers = { "UnityXRHelper", "DevicelessVRHelper" };

        public static void Apply(Harmony h)
        {
            foreach (var t in Helpers)
            {
                PatchUtil.Patch(h, t, "GetNodePose", typeof(PosePatches), prefix: nameof(NodePose));
                PatchUtil.Patch(h, t, "GetTriggerValue", typeof(PosePatches), prefix: nameof(Trigger));
                PatchUtil.Patch(h, t, "GetThumbstickValue", typeof(PosePatches), prefix: nameof(Thumbstick));
                PatchUtil.Patch(h, t, "GetAnyJoystickMaxAxis", typeof(PosePatches), prefix: nameof(AnyStick));
                PatchUtil.Patch(h, t, "GetMenuButton", typeof(PosePatches), prefix: nameof(MenuHeld));
                PatchUtil.Patch(h, t, "GetMenuButtonDown", typeof(PosePatches), prefix: nameof(MenuDown));
                PatchUtil.Patch(h, t, "TryGetPoseOffsetForNode", typeof(PosePatches), prefix: nameof(PoseOffset));
                PatchUtil.Patch(h, t, "GetRootPositionOffsetForLegacyNodePose", typeof(PosePatches), prefix: nameof(RootOffset));
                PatchUtil.Patch(h, t, "TryGetLegacyPoseOffsetForNode", typeof(PosePatches), prefix: nameof(LegacyOffset));
                PatchUtil.Patch(h, t, "TriggerHapticPulse", typeof(PosePatches), prefix: nameof(Skip));
                PatchUtil.Patch(h, t, "StopHaptics", typeof(PosePatches), prefix: nameof(Skip));
                PatchUtil.Patch(h, t, "IsAdvancedHapticsSupported", typeof(PosePatches), prefix: nameof(False));
                PatchUtil.Patch(h, t, "get_hasInputFocus", typeof(PosePatches), prefix: nameof(True));
                PatchUtil.Patch(h, t, "get_hasVrFocus", typeof(PosePatches), prefix: nameof(True));
            }
            // UnityXRHelper.Start needs a live XRInputSubsystem (list[0]) and would throw without a runtime.
            PatchUtil.Patch(h, "UnityXRHelper", "Start", typeof(PosePatches), prefix: nameof(Skip));
            PatchUtil.Patch(h, "UnityXRHelper", "OnDestroy", typeof(PosePatches), prefix: nameof(Skip));
        }

        private static bool NodePose(XRNode nodeType, ref Vector3 pos, ref Quaternion rot, ref bool __result)
        {
            switch (nodeType)
            {
                case XRNode.LeftHand: pos = PoseProvider.LeftPos; rot = PoseProvider.LeftRot; break;
                case XRNode.RightHand: pos = PoseProvider.RightPos; rot = PoseProvider.RightRot; break;
                default: pos = PoseProvider.HeadPosition; rot = Quaternion.identity; break;
            }
            __result = true;
            return false;
        }

        private static bool Trigger(XRNode node, ref float __result)
        {
            __result = node == XRNode.LeftHand ? PoseProvider.LeftTrigger : node == XRNode.RightHand ? PoseProvider.RightTrigger : 0f;
            return false;
        }

        // Menu lists scroll with the thumbsticks; in songs these values are not used for sabers.
        private static bool Thumbstick(XRNode node, ref Vector2 __result)
        {
            __result = node == XRNode.LeftHand ? new Vector2(XInput.LX, XInput.LY)
                     : node == XRNode.RightHand ? new Vector2(XInput.RX, XInput.RY) : Vector2.zero;
            return false;
        }

        private static bool AnyStick(ref Vector2 __result)
        {
            __result = new Vector2(Mathf.Abs(XInput.LX) > Mathf.Abs(XInput.RX) ? XInput.LX : XInput.RX,
                                   Mathf.Abs(XInput.LY) > Mathf.Abs(XInput.RY) ? XInput.LY : XInput.RY);
            return false;
        }
        private static bool MenuDown(ref bool __result) { __result = PoseProvider.MenuDown; return false; }
        private static bool MenuHeld(ref bool __result) { __result = PoseProvider.MenuHeld; return false; }
        private static bool PoseOffset(ref Pose poseOffset, ref bool __result) { poseOffset = Pose.identity; __result = true; return false; }
        private static bool RootOffset(ref Pose __result) { __result = Pose.identity; return false; }
        private static bool LegacyOffset(ref Vector3 position, ref Vector3 rotation, ref bool __result)
        {
            position = Vector3.zero; rotation = Vector3.zero; __result = true; return false;
        }
        private static bool Skip() => false;
        private static bool False(ref bool __result) { __result = false; return false; }
        private static bool True(ref bool __result) { __result = true; return false; }
    }
}
