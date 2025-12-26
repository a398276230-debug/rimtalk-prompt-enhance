using HarmonyLib;
using RimWorld;
using Verse;

namespace RimTalkHealthEnhance
{
    /// <summary>
    /// 拦截 Pawn 受伤事件，追踪战斗中的受伤情况
    /// 使用 HashSet 去重，每个 Pawn 只记录一次
    /// </summary>
    [HarmonyPatch(typeof(Pawn), "PostApplyDamage")]
    public static class PawnDamagePatch
    {
        static void Postfix(Pawn __instance, DamageInfo dinfo)
        {
            if (__instance == null) return;
            if (__instance.Dead) return; // 已死亡的不计入受伤
            
            // 只追踪人形生物
            if (!__instance.RaceProps.Humanlike) return;
            
            // 只追踪战斗伤害（排除环境伤害等）
            if (dinfo.Instigator == null) return;
            
            // 敌对人形生物受伤
            if (__instance.HostileTo(Faction.OfPlayer))
            {
                RaidTrackingService.RecordEnemyWounded(__instance);
            }
            // 殖民者受伤
            else if (__instance.Faction == Faction.OfPlayer)
            {
                RaidTrackingService.RecordColonistWounded(__instance);
            }
        }
    }
}