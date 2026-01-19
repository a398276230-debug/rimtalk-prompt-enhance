using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;

namespace RimTalkHealthEnhance
{
    public static class AnnouncementBuilder
    {
        /// <summary>
        /// 构建殖民地状态上下文（用于 {{colony_status}} 变量）
        /// 不包含 AI 史官总结（那部分由 colony_history 提供）
        /// </summary>
        public static string BuildAnnouncementContext()
        {
            var settings = RimTalkHealthEnhanceMod.Settings;
            if (!settings.ShowColonyAnnouncements) return null;
            
            var manager = Current.Game.GetComponent<ColonyAnnouncementManager>();
            if (manager?.Data == null) return null;
            
            // 检查当前地图是否属于玩家殖民地
            var map = Find.CurrentMap;
            bool isPlayerHome = map != null && map.IsPlayerHome;
            
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("## Colony Status");
            
            bool hasContent = false;
            
            // === 以下内容仅在主地图显示 ===
            if (isPlayerHome)
            {
                // 注意：派系关系已移至 {{colony_factions}} 胡子变量
                // 注意：地图布局已移至 {{colony_layout}} 胡子变量

                // === 1. 自由文本概况（Colony Overview） ===
                if (settings.ShowColonyOverview && !string.IsNullOrWhiteSpace(manager.Data.ColonyOverview))
                {
                    string overview = manager.Data.ColonyOverview;
                    if (overview.Length > settings.MaxOverviewLength)
                    {
                        overview = overview.Substring(0, settings.MaxOverviewLength) + "...";
                    }
                    sb.AppendLine("### Overview");
                    sb.AppendLine(overview);
                    sb.AppendLine();
                    hasContent = true;
                }
                
                // 注意：AI 史官总结已移至 colony_history 变量，不再在此处添加
            }
            
            // === 2. 结构化公告（支持全局通告） ===
            if (settings.ShowStructuredTasks)
            {
                var activeAnnouncements = manager.Data.Announcements
                    .Where(t =>
                    {
                        // 如果不在主地图，只显示全局通告
                        if (!isPlayerHome && !t.IsGlobal)
                        {
                            return false;
                        }
                        
                        if (t.Status == AnnouncementStatus.Active) return true;
                        
                        // 如果是最近完成的，也包含进来
                        if (settings.OnlyShowActiveTasks && t.Status == AnnouncementStatus.Completed && t.CompletedTick > 0)
                        {
                            // 自动捕获的事件完成后立即不再注入到 Context
                            if (t.IsAutoCaptured)
                            {
                                return false;
                            }
                            
                            // 手动创建的任务保持原有逻辑（保留指定天数）
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
                            sb.AppendLine($"### {categoryName}");
                            foreach (var item in items)
                            {
                                sb.AppendLine(FormatAnnouncement(item));
                            }
                            sb.AppendLine();
                            hasContent = true;
                        }
                    }
                }
            }
            
            if (hasContent)
            {
                return sb.ToString().TrimEnd();
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
