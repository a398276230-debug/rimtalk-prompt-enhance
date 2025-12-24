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
                    // 根据是否在主基地使用不同的前缀
                    string prefix = (mainPawn?.Map != null && mainPawn.Map.IsPlayerHome) 
                        ? "Relative Position" 
                        : "Current Map";
                    
                    sb.Append($"\n{prefix}: {relativeLocation}");
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"[RimTalk Enhance] Error in LocationContextPatch: {ex}");
            }
        }
    }
}
