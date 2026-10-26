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
            Log.Info("NonVRMod started.");
        }

        [OnExit]
        public void OnExit() => _harmony?.UnpatchSelf();
    }
}
