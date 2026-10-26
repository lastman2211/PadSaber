using HarmonyLib;
using UnityEngine.XR.Management;

namespace NonVRMod.Patches
{
    /// <summary>
    /// Stops the XR loader (OpenXR) from initializing so the game runs on the desktop without a runtime/headset.
    /// Method names verified against Unity.XR.Management in Beat Saber 1.40.8. The two static Attempt* methods are
    /// RuntimeInitializeOnLoad hooks that may fire before BSIPA loads plugins; if so, InitXRSDK/StartXRSDK still cover scene-time init.
    /// </summary>
    internal static class XrBypassPatches
    {
        public static void Apply(Harmony h)
        {
            foreach (var name in new[] { "AttemptInitializeXRSDKOnLoad", "AttemptStartXRSDKOnBeforeSplashScreen", "InitXRSDK", "StartXRSDK" })
            {
                var m = AccessTools.Method(typeof(XRGeneralSettings), name);
                if (m == null) { Plugin.Log.Warn($"XR bypass: XRGeneralSettings.{name} not found"); continue; }
                h.Patch(m, prefix: new HarmonyMethod(typeof(XrBypassPatches), nameof(Skip)));
                Plugin.Log.Info($"XR bypass: patched XRGeneralSettings.{name}");
            }
        }

        // PCAppInit.TransitionToNextScene sends the game to the "OpenXR Runtime was not found" error scene
        // when OpenXRRuntime.name is empty (and fpfc/editor mode are off). Report a runtime name so it proceeds.
        public static void ApplyRuntimeNamePatch(Harmony h)
        {
            var getter = AccessTools.PropertyGetter(typeof(UnityEngine.XR.OpenXR.OpenXRRuntime), "name");
            if (getter == null) { Plugin.Log.Warn("OpenXRRuntime.name getter not found"); return; }
            h.Patch(getter, prefix: new HarmonyMethod(typeof(XrBypassPatches), nameof(FakeRuntimeName)));
            Plugin.Log.Info("Patched OpenXRRuntime.name");
        }

        private static bool FakeRuntimeName(ref string __result) { __result = "NonVRMod"; return false; }

        private static bool Skip() => false; // skip original
    }
}
