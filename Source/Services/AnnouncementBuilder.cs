using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimTalkHealthEnhance
{
    public static class AnnouncementBuilder
    {
        public static string BuildAnnouncementContext()
        {
            var settings = RimTalkHealthEnhanceMod.Settings;
            if (!settings.ShowColonyAnnouncements) return null;
            
            var manager = Current.Game.GetComponent<ColonyAnnouncementManager>();
            if (manager?.Data == null) return null;
            
            List<string> parts = new List<string>();
            
            // === 1. 自由文本概况 ===
            if (settings.ShowColonyOverview && !string.IsNullOrWhiteSpace(manager.Data.ColonyOverview))
            {
                string overview = manager.Data.ColonyOverview;
                if (overview.Length > settings.MaxOverviewLength)
                {
                    overview = overview.Substring(0, settings.MaxOverviewLength) + "...";
                }
                parts.Add($"Colony Overview:\n{overview}");
            }
            
            // === 2. 结构化公告 ===
            if (settings.ShowStructuredTasks)
            {
                var activeAnnouncements = manager.Data.Announcements
                    .Where(t => 
                    {
                        if (t.Status == AnnouncementStatus.Active) return true;
                        
                        // 如果是最近完成的，也包含进来
                        if (settings.OnlyShowActiveTasks && t.Status == AnnouncementStatus.Completed && t.CompletedTick > 0)
                        {
                            int ticksSinceCompleted = Find.TickManager.TicksGame - t.CompletedTick;
                            return ticksSinceCompleted <= (int)(settings.CompletedTaskShowDays * 60000);
                        }
                        
                        return !settings.OnlyShowActiveTasks;
                    })
                    .OrderByDescending(t => t.Priority)
                    .ToList();
                
                if (activeAnnouncements.Any())
                {
                    // 按类别分组
                    var grouped = activeAnnouncements.GroupBy(a => a.Category);
                    
                    foreach (var group in grouped)
                    {
                        string categoryName = group.Key.ToString();
                        var items = group.ToList();
                        
                        if (items.Any())
                        {
                            var lines = items.Select(a => FormatAnnouncement(a));
                            parts.Add($"{categoryName}s:\n{string.Join("\n", lines)}");
                        }
                    }
                }
            }
            
            if (parts.Any())
            {
                return "=== Colony Status ===\n" + string.Join("\n\n", parts);
            }
            
            return null;
        }
        
        private static string FormatAnnouncement(ColonyAnnouncement a)
        {
            string baseInfo = $"- [{a.Priority}] {a.Title}";
            
            if (a.Status != AnnouncementStatus.Active)
                baseInfo += $" ({a.Status})";
                
            string details = "";
            if (!string.IsNullOrEmpty(a.Description))
                details += a.Description;
                
            if (a.Category == AnnouncementCategory.Project && a.Progress > 0)
                details += $" [Progress: {a.Progress:P0}]";
                
            if (!string.IsNullOrEmpty(a.AssignedPawnName))
                details += $" [Assigned: {a.AssignedPawnName}]";
                
            if (!string.IsNullOrEmpty(details))
                return $"{baseInfo}: {details}";
                
            return baseInfo;
        }
    }
}
