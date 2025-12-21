using HarmonyLib;
using RimTalk;
using RimTalk.Service;
using Verse;

namespace RimTalkHealthEnhance
{
    /// <summary>
    /// Harmony patch to replace RimTalk's GetHealthContext method with enhanced version
    /// </summary>
    [HarmonyPatch(typeof(ContextBuilder), "GetHealthContext")]
    public static class HealthContextPatch
    {
        /// <summary>
        /// Prefix patch that replaces the original method entirely
        /// </summary>
        static bool Prefix(Pawn pawn, PromptService.InfoLevel infoLevel, ref string __result)
        {
            // Check if health context is enabled in settings
            var contextSettings = Settings.Get().Context;
            if (!contextSettings.IncludeHealth)
            {
                __result = null;
                return false; // Skip original method
            }

            // Use our enhanced health info builder
            __result = HealthInfoBuilder.BuildEnhancedHealthContext(pawn, infoLevel);
            
            // Debug logging in dev mode
            if (Prefs.DevMode && !string.IsNullOrEmpty(__result))
            {
                Log.Message($"[RimTalk增强] {pawn.LabelShort} 的健康信息:\n{__result}");
            }
            
            return false; // Skip original method
        }
    }
}
