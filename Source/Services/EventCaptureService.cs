using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace RimTalkHealthEnhance
{
    public static class EventCaptureService
    {
        // 缓存已被 GameCondition 捕获的状况名称（避免重复捕获信件）
        private static HashSet<string> _capturedGameConditionLabels = new HashSet<string>();
        
        /// <summary>
        /// 标记某个 GameCondition 已被捕获（由 GameConditionCapturePatch 调用）
        /// </summary>
        public static void MarkGameConditionCaptured(string label)
        {
            if (!string.IsNullOrEmpty(label))
            {
                _capturedGameConditionLabels.Add(label.ToLower());
            }
        }
        
        /// <summary>
        /// 清理过期的标记（可选，在游戏加载时调用）
        /// </summary>
        public static void ClearCapturedLabels()
        {
            _capturedGameConditionLabels.Clear();
        }
        
        /// <summary>
        /// 检查标题是否匹配已捕获的 GameCondition
        /// </summary>
        private static bool IsGameConditionAlreadyCaptured(string title)
        {
            if (string.IsNullOrEmpty(title)) return false;
            
            string lowerTitle = title.ToLower();
            
            // 检查是否在已捕获列表中
            foreach (var label in _capturedGameConditionLabels)
            {
                if (lowerTitle.Contains(label) || label.Contains(lowerTitle))
                {
                    return true;
                }
            }
            
            return false;
        }
        
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
            
            // 如果是事件类别，检查是否已被 GameCondition 系统捕获（避免重复）
            if (category == AnnouncementCategory.Event && settings.AutoCaptureGameConditions)
            {
                if (IsGameConditionAlreadyCaptured(title))
                {
                    Log.Message($"[RimTalk Enhance] Skipping event '{title}' - already captured by GameCondition system");
                    return;
                }
            }
            
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
            
            // 如果是袭击事件，标记并安排延迟初始化
            // 注意：事件信件发送时，敌人可能还未完全生成
            // 所以我们先标记事件，使用 GameComponent 的 tick 来延迟初始化
            if (category == AnnouncementCategory.Event && RaidTrackingService.IsRaidEvent(title))
            {
                Log.Message($"[RimTalk Enhance] Raid event detected: '{title}'. Setting up tracking...");
                
                // 先标记为袭击事件
                announcement.IsRaidEvent = true;
                
                // 检查是否已有同一派系的活跃袭击事件
                var existingRaid = RaidTrackingService.GetActiveRaidEvent();
                if (existingRaid != null && existingRaid.Title == announcement.Title)
                {
                    // 已存在同名袭击事件，不需要重新初始化，但需要更新初始敌人计数
                    Log.Message($"[RimTalk Enhance] Raid event '{title}' already exists (ID: {existingRaid.Id}). Updating enemy count instead of creating new.");
                    
                    // 安排重新计数（可能有新敌人到达）
                    var manager = ColonyAnnouncementManager.Instance;
                    manager?.ScheduleRaidRecount(existingRaid, 60);
                }
                else
                {
                    var manager = ColonyAnnouncementManager.Instance;
                    
                    // 订阅当前地图的 Lord 事件（用于持续追踪初始敌人数）
                    var map = Find.CurrentMap;
                    if (map != null)
                    {
                        LordMonitorService.SubscribeToMap(map);
                        LordMonitorService.ResetMonitoring();
                    }
                    
                    // 区分动物袭击和派系袭击
                    if (RaidTrackingService.IsAnimalRaidEvent(title))
                    {
                        // 动物袭击：不使用 Lord 系统，使用短延迟直接计数
                        Log.Message($"[RimTalk Enhance] Animal raid detected. Using short delay (60 ticks = 1 second)...");
                        manager?.ScheduleRaidInitialization(announcement, 60);
                    }
                    else
                    {
                        // 派系袭击（海盗、机械族等）：使用 Lord 监控持续追踪
                        // 先进行初始计数，后续由 LordMonitorService 持续更新
                        Log.Message($"[RimTalk Enhance] Faction raid detected. Using Lord monitoring with initial delay (120 ticks = 2 seconds)...");
                        manager?.ScheduleRaidInitialization(announcement, 120);
                    }
                }
            }
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
