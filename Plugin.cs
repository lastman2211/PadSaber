using HarmonyLib;
using IPA;
using IPA.Config.Stores;
using UnityEngine;
using Conf = IPA.Config.Config;
using IPALogger = IPA.Logging.Logger;

namespace NonVRMod
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        internal static IPALogger Log { get; private set; }
        private Harmony _harmony;

        [Init]
        public Plugin(IPALogger logger, Conf conf)
        {
            Log = logger;
            PluginConfig.Instance = conf.Generated<PluginConfig>();
        }

        [OnStart]
        public void OnStart()
        {
            // Patches run before XR init where possible; each patch resolves its target by name
            // and logs a warning instead of throwing if the game version differs.
            _harmony = new Harmony("com.nonvrmod");
            Patches.XrBypassPatches.Apply(_harmony);
            Patches.XrBypassPatches.ApplyRuntimeNamePatch(_harmony);
            Patches.PosePatches.Apply(_harmony);

            var go = new GameObject("NonVRMod");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<Input.PoseProvider>();
            go.AddComponent<Dodge.AutoDodge>();
            go.AddComponent<Display.DesktopView>();
            DisableScoreSubmission();
            Log.Info("NonVRMod started.");
        }

        // Auto-dodge and aim assist change gameplay, so scores are never submitted while PadSaber is loaded.
        // Calls BS Utils by reflection so the mod still loads if BS Utils is missing (then it only warns).
        private static void DisableScoreSubmission()
        {
            try
            {
                System.Type type = null;
                foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm.GetName().Name == "BS_Utils") { type = asm.GetType("BS_Utils.Gameplay.ScoreSubmission"); break; }
                }
                var disable = type?.GetMethod("ProlongedDisableSubmission", new[] { typeof(string) });
                if (disable == null)
                {
                    Log.Warn("BS Utils not found: score submission is NOT disabled. Install BS Utils to keep assisted scores off leaderboards.");
                    return;
                }
                disable.Invoke(null, new object[] { "PadSaber" });
                Log.Info("Score submission disabled (PadSaber).");
            }
            catch (System.Exception e)
            {
                Log.Warn("Could not disable score submission: " + e.Message);
            }
        }

        [OnExit]
        public void OnExit() => _harmony?.UnpatchSelf();
    }
}
