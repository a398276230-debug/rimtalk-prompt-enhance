using HarmonyLib;
using RimWorld;
using Verse;

namespace RimTalkHealthEnhance
{
    /// <summary>
    /// 拦截 Pawn 受伤事件，追踪战斗中的受伤情况
    /// 使用 HashSet 去重，每个 Pawn 只记录一次
    /// 支持追踪所有敌对目标（人类、动物、机械族等）
    /// </summary>
    [HarmonyPatch(typeof(Pawn), "PostApplyDamage")]
    public static class PawnDamagePatch
    {
        static void Postfix(Pawn __instance, DamageInfo dinfo)
        {
            if (__instance == null) return;
            if (__instance.Dead) return; // 已死亡的不计入受伤
            
            // 只追踪战斗伤害（排除环境伤害等）
            if (dinfo.Instigator == null) return;
            
            // 敌对目标受伤（包括人类、动物、机械族等）
            if (__instance.HostileTo(Faction.OfPlayer))
            {
                RaidTrackingService.RecordEnemyWounded(__instance);
            }
            // 殖民者受伤（玩家派系的人类）
            else if (__instance.Faction == Faction.OfPlayer && (__instance.RaceProps?.Humanlike ?? false))
            {
                RaidTrackingService.RecordColonistWounded(__instance);
            }
        }
    }
}