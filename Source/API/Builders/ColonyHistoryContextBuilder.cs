using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimTalkHealthEnhance.API
{
    /// <summary>
    /// 构建殖民地历史快照上下文（用于 {{colony_history}} 变量）
    /// </summary>
    internal static class ColonyHistoryContextBuilder
    {
        /// <summary>
        /// 构建殖民地历史快照上下文
        /// 使用 Markdown 格式
        /// </summary>
        public static string Build()
        {
            var settings = RimTalkHealthEnhanceMod.Settings;
            var manager = Current.Game?.GetComponent<ColonyAnnouncementManager>();

            if (manager?.Data?.DailySnapshots == null || manager.Data.DailySnapshots.Count == 0)
                return null;

            var snapshotsWithSummary = manager.Data.DailySnapshots
                .Where(s => !string.IsNullOrEmpty(s.AISummary))
                .OrderByDescending(s => s.AbsTick)
                .ToList();

            if (snapshotsWithSummary.Count == 0)
                return null;

            long maxAbsTick = snapshotsWithSummary.Max(s => s.AbsTick);
            long ticksToInject = (long)(settings.SnapshotInjectDays * GenDate.TicksPerDay);

            var recentSnapshots = snapshotsWithSummary
                .Where(s => maxAbsTick - s.AbsTick < ticksToInject)
                .ToList();

            if (!recentSnapshots.Any())
                return null;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("## Recent Colony History");

            foreach (var snapshot in recentSnapshots)
            {
                string gameDateStr = snapshot.GetDateStringWithOffset(manager.Data.DisplayTickOffset, Vector2.zero);
                sb.AppendLine($"[{gameDateStr}] {snapshot.AISummary}");
            }

            return sb.ToString().TrimEnd();
        }
    }
}