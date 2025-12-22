using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RimTalkHealthEnhance
{
    [HarmonyPatch(typeof(Designator_Build), "DesignateSingleCell")]
    public static class BlueprintPlacePatch
    {
        static void Postfix(IntVec3 c, Designator_Build __instance)
        {
            var manager = ColonyAnnouncementManager.Instance;
            if (manager == null) return;
            
            // 简单去重：如果同一tick内已经记录了相同的操作，则忽略
            // 这里我们只记录小时级别的日志，所以不需要太频繁
            
            string buildingName = __instance.PlacingDef.label;
            int hour = GenLocalDate.HourOfDay(Find.CurrentMap);
            
            string log = $"[{hour:D2}:00] 玩家部署了 {buildingName} 蓝图";
            
            // 避免刷屏：如果最后一条日志也是这个，就不加了
            if (manager.Data.TodayActionLogs.Count > 0 && 
                manager.Data.TodayActionLogs[manager.Data.TodayActionLogs.Count - 1] == log)
            {
                return;
            }
            
            manager.Data.TodayActionLogs.Add(log);
        }
    }

    [HarmonyPatch(typeof(Designator_Cancel), "DesignateThing")]
    public static class BlueprintCancelPatch
    {
        static void Postfix(Thing t)
        {
            if (t is Blueprint || t is Frame)
            {
                var manager = ColonyAnnouncementManager.Instance;
                if (manager == null) return;
                
                int hour = GenLocalDate.HourOfDay(Find.CurrentMap);
                string log = $"[{hour:D2}:00] 玩家取消了 {t.def.label} 蓝图";
                
                if (manager.Data.TodayActionLogs.Count > 0 && 
                    manager.Data.TodayActionLogs[manager.Data.TodayActionLogs.Count - 1] == log)
                {
                    return;
                }
                
                manager.Data.TodayActionLogs.Add(log);
            }
        }
    }
}
