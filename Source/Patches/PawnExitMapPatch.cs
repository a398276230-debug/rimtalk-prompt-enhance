using HarmonyLib;
using RimWorld;
using Verse;

namespace RimTalkHealthEnhance
{
    /// <summary>
    /// 拦截 Pawn 离开地图事件，追踪敌人撤退
    /// </summary>
    [HarmonyPatch(typeof(Pawn), "ExitMap")]
    public static class PawnExitMapPatch
    {
        static void Postfix(Pawn __instance, bool allowedToJoinOrCreateCaravan)
        {
            if (__instance == null) return;
            
            // 只追踪敌对人形生物的撤退（非死亡离开）
            if (__instance.HostileTo(Faction.OfPlayer) && 
                __instance.RaceProps.Humanlike &&
                !__instance.Dead)
            {
                RaidTrackingService.RecordEnemyFlee(__instance);
                
                // 撤退后也检查袭击是否结束
                var manager = ColonyAnnouncementManager.Instance;
                manager?.ScheduleRaidCheck();
            }
        }
    }
}