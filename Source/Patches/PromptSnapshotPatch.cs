using HarmonyLib;
using RimTalk.Service;
using RimTalk.Data;
using Verse;
using System.Linq;
using System.Text;
using UnityEngine;
using RimWorld;

namespace RimTalkHealthEnhance
{
    [HarmonyPatch(typeof(PromptService), "DecoratePrompt")]
    public static class PromptSnapshotPatch
    {
        static void Postfix(TalkRequest talkRequest)
        {
            var settings = RimTalkHealthEnhanceMod.Settings;
            if (!settings.EnableAISynthesis || !settings.InjectSnapshotToContext 
                || settings.SnapshotInjectionTarget != SnapshotInjectionMode.Prompt)
                return;

            var manager = Current.Game.GetComponent<ColonyAnnouncementManager>();
            if (manager?.Data?.DailySnapshots == null || manager.Data.DailySnapshots.Count == 0)
                return;

            // 获取最近 N 天的快照
            // 使用 AbsTick 排序和过滤，更加可靠
            var snapshotsWithSummary = manager.Data.DailySnapshots
                .Where(s => !string.IsNullOrEmpty(s.AISummary))
                .OrderByDescending(s => s.AbsTick)
                .ToList();
            
            if (snapshotsWithSummary.Count == 0)
                return;
            
            // 计算时间范围：最近 N 天 = N * TicksPerDay
            long maxAbsTick = snapshotsWithSummary.Max(s => s.AbsTick);
            long ticksToInject = (long)(settings.SnapshotInjectDays * GenDate.TicksPerDay);
            
            var recentSnapshots = snapshotsWithSummary
                .Where(s => maxAbsTick - s.AbsTick < ticksToInject)
                .ToList();

            if (recentSnapshots.Any())
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("\n=== Recent Colony History ===");
                
                foreach (var snapshot in recentSnapshots)
                {
                    // 使用 DailySnapshot 的方法获取显示日期（带偏移量）
                    string gameDateStr = snapshot.GetDateStringWithOffset(manager.Data.DisplayTickOffset, Vector2.zero);
                    sb.AppendLine($"[{gameDateStr}] {snapshot.AISummary}");
                }
                
                // 追加到 Prompt
                talkRequest.Prompt += sb.ToString();
            }
        }
    }
}
