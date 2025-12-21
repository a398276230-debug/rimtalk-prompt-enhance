using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RimTalkHealthEnhance
{
    [HarmonyPatch(typeof(Archive), nameof(Archive.Add))]
    public static class ArchiveCapturePatch
    {
        public static void Postfix(IArchivable archivable)
        {
            if (archivable == null) return;

            try
            {
                // 1. 收集类型信息供设置界面使用
                string typeName = archivable.GetType().FullName;
                if (!HealthEnhanceSettings.DiscoveredEventTypes.Contains(typeName))
                {
                    HealthEnhanceSettings.DiscoveredEventTypes.Add(typeName);
                    HealthEnhanceSettings.DiscoveredEventTypes.Sort();
                }

                // 2. 处理事件捕获
                EventCaptureService.ProcessEvent(archivable);
            }
            catch (Exception ex)
            {
                Log.Warning($"[RimTalk Health Enhance] Error capturing event: {ex.Message}");
            }
        }
    }
}
