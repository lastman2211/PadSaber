using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace NonVRMod.Patches
{
    internal static class PatchUtil
    {
        /// <summary>Finds a type by simple name across loaded assemblies (game class names/namespaces shift between versions).</summary>
        public static Type FindType(string name) =>
            AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
                .FirstOrDefault(t => t.Name == name);

        /// <summary>Patches Type.method with a static method from patchType. Logs and skips if the target is missing.</summary>
        public static void Patch(Harmony h, string typeName, string method, Type patchType, string prefix = null, string postfix = null)
        {
            var type = FindType(typeName);
            var targets = type?.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .Where(m => m.Name == method).ToList();
            if (targets == null || targets.Count == 0)
            {
                Plugin.Log.Warn($"Patch target not found: {typeName}.{method} (game version mismatch?)");
                return;
            }
            foreach (var t in targets)
            {
                try
                {
                    h.Patch(t,
                        prefix: prefix == null ? null : new HarmonyMethod(patchType.GetMethod(prefix, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)),
                        postfix: postfix == null ? null : new HarmonyMethod(patchType.GetMethod(postfix, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)));
                    Plugin.Log.Info($"Patched {typeName}.{method}");
                }
                catch (Exception e) { Plugin.Log.Warn($"Failed patching {typeName}.{method}: {e.Message}"); }
            }
        }
    }
}
