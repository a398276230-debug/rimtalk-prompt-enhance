using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RimTalkHealthEnhance
{
    /// <summary>
    /// 拦截 Pawn 死亡事件，追踪袭击战斗结果
    /// 使用明确的参数类型匹配 Pawn.Kill(DamageInfo? dinfo, Hediff exactCulprit)
    /// </summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
    [HarmonyPatch(new Type[] { typeof(DamageInfo?), typeof(Hediff) })]
    public static class PawnKillPatch
    {
        static void Postfix(Pawn __instance, DamageInfo? dinfo, Hediff exactCulprit)
        {
            if (__instance == null) return;
            
            try
            {
                // 记录详细信息用于调试
                string factionName = __instance.Faction?.Name ?? "No Faction";
                bool isHumanlike = __instance.RaceProps?.Humanlike ?? false;
                bool isPlayerFaction = __instance.Faction == Faction.OfPlayer;
                
                // 判断是否为敌对：非玩家派系 且 人形生物
                // 注意：死亡时 HostileTo 可能不准确，所以直接用派系判断
                bool isEnemy = !isPlayerFaction && isHumanlike && __instance.Faction != null;
                
                Log.Message($"[RimTalk Enhance] PawnKillPatch triggered for: {__instance.LabelShort}, Faction: {factionName}, Humanlike: {isHumanlike}, IsEnemy: {isEnemy}");
                
                // 敌对人形生物死亡
                if (isEnemy)
                {
                    Log.Message($"[RimTalk Enhance] Enemy killed: {__instance.LabelShort}");
                    
                    // 记录敌人死亡
                    RaidTrackingService.RecordEnemyKill(__instance);
                    
                    // 触发袭击结束检测
                    var manager = ColonyAnnouncementManager.Instance;
                    manager?.ScheduleRaidCheck();
                }
                // 殖民者死亡
                else if (isPlayerFaction && isHumanlike)
                {
                    Log.Message($"[RimTalk Enhance] Colonist killed: {__instance.LabelShort}");
                    RaidTrackingService.RecordColonistDeath(__instance);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk Enhance] Error in PawnKillPatch: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
    
    /// <summary>
    /// 拦截 Pawn 倒地事件，追踪袭击战斗结果
    /// 使用 Pawn_HealthTracker.MakeDowned（私有方法，需要用字符串）
    /// </summary>
    [HarmonyPatch(typeof(Pawn_HealthTracker), "MakeDowned")]
    [HarmonyPatch(new Type[] { typeof(DamageInfo?), typeof(Hediff) })]
    public static class PawnDownedPatch
    {
        static void Postfix(Pawn_HealthTracker __instance)
        {
            try
            {
                // 使用反射获取 pawn 字段
                var pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
                if (pawn == null) return;
                
                Log.Message($"[RimTalk Enhance] PawnDownedPatch triggered for: {pawn.LabelShort}");
                
                // 敌对人形生物倒地
                if (pawn.HostileTo(Faction.OfPlayer) && pawn.RaceProps.Humanlike)
                {
                    Log.Message($"[RimTalk Enhance] Enemy downed: {pawn.LabelShort}");
                    RaidTrackingService.RecordEnemyDowned(pawn);
                    
                    // 倒地也可能导致袭击结束，调度检测
                    var manager = ColonyAnnouncementManager.Instance;
                    manager?.ScheduleRaidCheck();
                }
                // 殖民者倒地
                else if (pawn.Faction == Faction.OfPlayer && pawn.RaceProps.Humanlike)
                {
                    Log.Message($"[RimTalk Enhance] Colonist downed: {pawn.LabelShort}");
                    RaidTrackingService.RecordColonistDowned(pawn);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk Enhance] Error in PawnDownedPatch: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}
