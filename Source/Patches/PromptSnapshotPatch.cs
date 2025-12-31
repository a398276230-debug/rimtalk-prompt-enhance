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
            // 使用快照的逻辑 Day 值来过滤，参考点是最新快照的 Day
            // 这样无论用户如何调整日期，过滤都会正确工作
            var snapshotsWithSummary = manager.Data.DailySnapshots
                .Where(s => !string.IsNullOrEmpty(s.AISummary))
                .ToList();
            
            if (snapshotsWithSummary.Count == 0)
                return;
            
            int maxDay = snapshotsWithSummary.Max(s => s.Day);
            float daysToInject = settings.SnapshotInjectDays;
            
            var recentSnapshots = snapshotsWithSummary
                .Where(s => maxDay - s.Day < daysToInject)
                .OrderByDescending(s => s.Day)
                .ToList();

            if (recentSnapshots.Any())
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("\n=== Recent Colony History ===");
                
                foreach (var snapshot in recentSnapshots)
                {
                    // 只显示游戏日期（年份/季节），节省tokens
                    // 应用全局 Tick 偏移量
                    int displayTick = snapshot.Tick + manager.Data.SnapshotTickOffset;
                    string gameDateStr = GenDate.DateFullStringAt(displayTick, Vector2.zero);
                    sb.AppendLine($"[{gameDateStr}] {snapshot.AISummary}");
                }
                
                // 追加到 Prompt
                talkRequest.Prompt += sb.ToString();
            }
        }
    }
}
