using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace RimTalkHealthEnhance
{
    public static class EventCaptureService
    {
        public static void ProcessEvent(IArchivable archivable)
        {
            var settings = RimTalkHealthEnhanceMod.Settings;
            if (!settings.EnableAutoEventCapture) return;

            // 检查类型是否启用
            string typeName = archivable.GetType().FullName;
            if (settings.EnabledEventTypes.ContainsKey(typeName) && !settings.EnabledEventTypes[typeName])
                return;

            // 确定类别
            var category = DetermineCategory(archivable);
            
            // 检查类别开关
            if (category == AnnouncementCategory.Quest && !settings.AutoCaptureQuests) return;
            if (category == AnnouncementCategory.Event && !settings.AutoCaptureEvents) return;
            if (category == AnnouncementCategory.Resource && !settings.AutoCaptureResources) return;

            // 提取信息
            string title = archivable.ArchivedLabel;
            string description = archivable.ArchivedTooltip.StripTags();
            
            // 确定优先级
            var priority = DeterminePriority(archivable);

            // 创建公告
            var announcement = new ColonyAnnouncement
            {
                Id = Guid.NewGuid().ToString(),
                Category = category,
                Title = title,
                Description = description,
                Priority = priority,
                Status = AnnouncementStatus.Active,
                CreatedTick = Find.TickManager.TicksGame,
                Progress = 0f,
                IsAutoCaptured = true  // 标记为自动捕获
            };

            // 关联任务ID
            if (archivable is ChoiceLetter { quest: not null } letter)
            {
                announcement.RelatedQuestId = letter.quest.id;
            }

            // 设置自动完成时间
            if (category == AnnouncementCategory.Quest)
            {
                if (settings.AutoCompleteDays > 0)
                    announcement.DeadlineTicks = Find.TickManager.TicksGame + (settings.AutoCompleteDays * 60000);
            }
            else
            {
                // 普通事件使用较短的过期时间
                if (settings.EventExpireDays > 0)
                    announcement.DeadlineTicks = Find.TickManager.TicksGame + (int)(settings.EventExpireDays * 60000);
            }

            // 添加到管理器
            MergeOrAdd(announcement);
        }

        private static AnnouncementCategory DetermineCategory(IArchivable archivable)
        {
            // Quest detection
            if (archivable is ChoiceLetter { quest: not null })
                return AnnouncementCategory.Quest;
            
            if (archivable.ArchivedLabel.IndexOf("Quest", StringComparison.OrdinalIgnoreCase) >= 0)
                return AnnouncementCategory.Quest;

            // Resource detection
            if (ContainsResourceKeywords(archivable.ArchivedTooltip))
                return AnnouncementCategory.Resource;

            // Default to Event
            return AnnouncementCategory.Event;
        }

        private static AnnouncementPriority DeterminePriority(IArchivable archivable)
        {
            if (archivable is Letter letter)
            {
                if (letter.def == LetterDefOf.ThreatBig) return AnnouncementPriority.Urgent;
                if (letter.def == LetterDefOf.ThreatSmall) return AnnouncementPriority.High;
                if (letter.def == LetterDefOf.NegativeEvent) return AnnouncementPriority.Normal;
                if (letter.def == LetterDefOf.PositiveEvent) return AnnouncementPriority.Normal;
            }

            return AnnouncementPriority.Normal;
        }

        private static bool ContainsResourceKeywords(string text)
        {
            string[] keywords = { "resource", "cargo pods", "meteorite", "chunk", "deposit" };
            return keywords.Any(k => text.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static void MergeOrAdd(ColonyAnnouncement newAnnouncement)
        {
            var manager = ColonyAnnouncementManager.Instance;
            if (manager == null) return;

            var settings = RimTalkHealthEnhanceMod.Settings;

            if (settings.MergeDuplicateEvents)
            {
                // 查找最近的相同标题的活动公告
                var existing = manager.Data.Announcements
                    .Where(a => a.Status == AnnouncementStatus.Active && 
                                a.Category == newAnnouncement.Category &&
                                a.Title == newAnnouncement.Title)
                    .OrderByDescending(a => a.CreatedTick)
                    .FirstOrDefault();

                if (existing != null)
                {
                    // 更新现有公告
                    // 检查是否已经有计数器
                    if (existing.Title.Contains("(x"))
                    {
                        int lastOpen = existing.Title.LastIndexOf("(x");
                        int lastClose = existing.Title.LastIndexOf(")");
                        if (lastOpen != -1 && lastClose > lastOpen)
                        {
                            string countStr = existing.Title.Substring(lastOpen + 2, lastClose - lastOpen - 2);
                            if (int.TryParse(countStr, out int count))
                            {
                                existing.Title = existing.Title.Substring(0, lastOpen).Trim() + $" (x{count + 1})";
                            }
                        }
                    }
                    else
                    {
                        existing.Title += " (x2)";
                    }

                    // 更新描述为最新的
                    existing.Description = newAnnouncement.Description;
                    existing.CreatedTick = newAnnouncement.CreatedTick; // 更新时间戳
                    
                    // 刷新截止时间
                    if (newAnnouncement.DeadlineTicks > 0)
                        existing.DeadlineTicks = newAnnouncement.DeadlineTicks;

                    return;
                }
            }

            manager.AddAnnouncement(newAnnouncement);
        }
    }
}
