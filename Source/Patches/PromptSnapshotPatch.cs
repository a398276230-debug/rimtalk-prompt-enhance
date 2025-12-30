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
            int currentDay = GenDate.DaysPassed;
            float daysToInject = settings.SnapshotInjectDays;
            
            var recentSnapshots = manager.Data.DailySnapshots
                .Where(s => currentDay - s.Day <= daysToInject && !string.IsNullOrEmpty(s.AISummary))
                .OrderByDescending(s => s.Day)
                .ToList();

            if (recentSnapshots.Any())
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("\n=== Recent Colony History ===");
                
                foreach (var snapshot in recentSnapshots)
                {
                    // 只显示游戏日期（年份/季节），节省tokens
                    string gameDateStr = GenDate.DateFullStringAt(snapshot.Tick, Vector2.zero);
                    sb.AppendLine($"[{gameDateStr}] {snapshot.AISummary}");
                }
                
                // 追加到 Prompt
                talkRequest.Prompt += sb.ToString();
            }
        }
    }
}
