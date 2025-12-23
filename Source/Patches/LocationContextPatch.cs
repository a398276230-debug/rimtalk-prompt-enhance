using HarmonyLib;
using System.Text;
using RimTalk.Service;
using RimTalk.Data;
using Verse;

namespace RimTalkHealthEnhance
{
    /// <summary>
    /// 拦截 ContextBuilder.BuildLocationContext，添加相对位置信息
    /// </summary>
    [HarmonyPatch(typeof(ContextBuilder), "BuildLocationContext")]
    public static class LocationContextPatch
    {
        static void Postfix(StringBuilder sb, ContextSettings contextSettings, Pawn mainPawn)
        {
            try
            {
                var settings = RimTalkHealthEnhanceMod.Settings;
                if (!settings.ShowRelativeLocation) return;

                string relativeLocation = LocationContextBuilder.GetRelativeLocation(mainPawn);
                if (!string.IsNullOrEmpty(relativeLocation))
                {
                    sb.Append($"\nRelative Position: {relativeLocation}");
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"[RimTalk Enhance] Error in LocationContextPatch: {ex}");
            }
        }
    }
}
