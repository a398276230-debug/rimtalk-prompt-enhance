using HarmonyLib;
using RimWorld;
using Verse;

namespace RimTalkHealthEnhance
{
    /// <summary>
    /// 拦截 Pawn 离开地图事件，追踪敌人撤退
    /// 支持追踪所有敌对目标（人类、动物、机械族等）
    /// </summary>
    [HarmonyPatch(typeof(Pawn), "ExitMap")]
    public static class PawnExitMapPatch
    {
        static void Postfix(Pawn __instance, bool allowedToJoinOrCreateCaravan)
        {
            if (__instance == null) return;
            
            // 追踪所有敌对目标的撤退（非死亡离开）
            // 使用 HostileTo 自动处理派系敌对、发狂动物等
            if (__instance.HostileTo(Faction.OfPlayer) && !__instance.Dead)
            {
                // 识别类型用于日志
                bool isHumanlike = __instance.RaceProps?.Humanlike ?? false;
                bool isAnimal = __instance.RaceProps?.Animal ?? false;
                bool isMechanoid = __instance.RaceProps?.IsMechanoid ?? false;
                string raceType = isHumanlike ? "Humanlike" : (isAnimal ? "Animal" : (isMechanoid ? "Mechanoid" : "Other"));
                
                Log.Message($"[RimTalk Enhance] Enemy fled map: {__instance.LabelShort} (Type: {raceType})");
                
                RaidTrackingService.RecordEnemyFlee(__instance);
                
                // 撤退后也检查袭击是否结束
                var manager = ColonyAnnouncementManager.Instance;
                manager?.ScheduleRaidCheck();
            }
        }
    }
}