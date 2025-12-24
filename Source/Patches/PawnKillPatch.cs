using HarmonyLib;
using RimWorld;
using Verse;

namespace RimTalkHealthEnhance
{
    /// <summary>
    /// 拦截 Pawn 死亡事件，触发袭击检测
    /// </summary>
    [HarmonyPatch(typeof(Pawn), "Kill")]
    public static class PawnKillPatch
    {
        static void Postfix(Pawn __instance)
        {
            // 只关心敌对的人形生物
            if (__instance != null && 
                __instance.HostileTo(Faction.OfPlayer) && 
                __instance.RaceProps.Humanlike)
            {
                var manager = ColonyAnnouncementManager.Instance;
                manager?.ScheduleRaidCheck();
            }
        }
    }
}
