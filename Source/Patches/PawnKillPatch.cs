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
                bool isAnimal = __instance.RaceProps?.Animal ?? false;
                bool isMechanoid = __instance.RaceProps?.IsMechanoid ?? false;
                bool isPlayerFaction = __instance.Faction == Faction.OfPlayer;
                
                // 使用 RimWorld 内置的 HostileTo 方法判断是否为敌对目标
                // 注意：倒地后再死亡的敌人可能不再被视为 HostileTo（因为他们无法战斗了）
                bool isHostileNow = __instance.HostileTo(Faction.OfPlayer);
                
                // 如果 Pawn 之前已被记录为倒地的敌人，那它肯定是敌人
                bool wasDownedEnemy = RaidTrackingService.WasEnemyDowned(__instance.thingIDNumber);
                
                // 如果 Pawn 之前被记录为活跃敌对目标，那它肯定是敌人
                // 这解决了发狂动物等直接死亡（一击必杀）时无法被识别的问题
                bool wasActiveHostile = RaidTrackingService.WasActiveHostile(__instance.thingIDNumber);
                
                // 判断是否为敌对派系（用于倒地后死亡的情况）
                bool isHostileFaction = __instance.Faction != null &&
                                        __instance.Faction != Faction.OfPlayer &&
                                        __instance.Faction.HostileTo(Faction.OfPlayer);
                
                // 综合判断：当前敌对 OR 之前倒地过 OR 曾是活跃敌对目标 OR 属于敌对派系
                bool isEnemy = isHostileNow || wasDownedEnemy || wasActiveHostile || isHostileFaction;
                
                // 识别威胁类型用于日志
                string raceType = isHumanlike ? "Humanlike" : (isAnimal ? "Animal" : (isMechanoid ? "Mechanoid" : "Other"));
                
                Log.Message($"[RimTalk Enhance] PawnKillPatch triggered for: {__instance.LabelShort}, Faction: {factionName}, Type: {raceType}, IsHostileNow: {isHostileNow}, WasDownedEnemy: {wasDownedEnemy}, WasActiveHostile: {wasActiveHostile}, IsHostileFaction: {isHostileFaction}");
                
                // 敌对目标死亡（包括人类、动物、机械族等）
                if (isEnemy)
                {
                    Log.Message($"[RimTalk Enhance] Enemy killed: {__instance.LabelShort} (Type: {raceType})");
                    
                    // 记录敌人死亡
                    RaidTrackingService.RecordEnemyKill(__instance);
                    
                    // 触发袭击结束检测
                    var manager = ColonyAnnouncementManager.Instance;
                    manager?.ScheduleRaidCheck();
                }
                // 殖民者死亡（玩家派系的人类）
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
                
                // 识别类型用于日志
                bool isHumanlike = pawn.RaceProps?.Humanlike ?? false;
                bool isAnimal = pawn.RaceProps?.Animal ?? false;
                bool isMechanoid = pawn.RaceProps?.IsMechanoid ?? false;
                string raceType = isHumanlike ? "Humanlike" : (isAnimal ? "Animal" : (isMechanoid ? "Mechanoid" : "Other"));
                
                Log.Message($"[RimTalk Enhance] PawnDownedPatch triggered for: {pawn.LabelShort} (Type: {raceType})");
                
                // 敌对目标倒地（包括人类、动物、机械族等）
                if (pawn.HostileTo(Faction.OfPlayer))
                {
                    Log.Message($"[RimTalk Enhance] Enemy downed: {pawn.LabelShort} (Type: {raceType})");
                    RaidTrackingService.RecordEnemyDowned(pawn);
                    
                    // 倒地也可能导致袭击结束，调度检测
                    var manager = ColonyAnnouncementManager.Instance;
                    manager?.ScheduleRaidCheck();
                }
                // 殖民者倒地（玩家派系的人类）
                else if (pawn.Faction == Faction.OfPlayer && isHumanlike)
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
