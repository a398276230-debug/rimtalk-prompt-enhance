using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace RimTalkHealthEnhance
{
    /// <summary>
    /// 事件捕获服务：将 Archive 归档条目转化为殖民地通告板记录。
    /// 分类判定全部使用语言无关信号（Quest 类型/quest 字段、LetterDef defName、ThingDef 分类），
    /// 不依赖 ArchivedLabel/ArchivedTooltip 的本地化文本。
    /// 过滤使用本项目自建的 EnabledEventTypes（类型名 + defName 双重过滤），与 RimTalk 的播报过滤完全解耦。
    /// </summary>
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
        /// 清理过期的标记（在游戏加载时调用）
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

            // 类型名 + defName 双重过滤（自建事件池，与 RimTalk 播报过滤解耦）
            if (!ArchivableTypeScanner.ShouldCapture(archivable, settings.EnabledEventTypes))
                return;

            // 确定类别（语言无关判定）
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
                    return;
                }
            }

            // 确定优先级（LetterDef defName 判定，语言无关）
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

            // 商队事件：设置追踪
            bool isCaravanEvent = category == AnnouncementCategory.Event &&
                                  CaravanTrackingService.IsCaravanEvent(title, (archivable as ChoiceLetter)?.def);
            if (isCaravanEvent)
            {
                CaravanTrackingService.SetActiveCaravanEvent(announcement);
            }

            // 袭击事件：标记并安排延迟初始化
            // 注意：事件信件发送时，敌人可能还未完全生成
            // 所以我们先标记事件，使用 GameComponent 的 tick 来延迟初始化
            bool isRaidEvent = !isCaravanEvent &&
                               category == AnnouncementCategory.Event &&
                               RaidTrackingService.IsRaidEvent(title, (archivable as ChoiceLetter)?.def);

            // 袭击子类型：语言无关判定（信件 LookTargets 的 pawn race），Unknown 时下游降级标题关键词
            announcement.RaidKind = isRaidEvent ? RaidTrackingService.ClassifyRaidKind(archivable) : RaidKindType.Unknown;

            // 在入库前检查是否已有同名活跃袭击（入库后查询会命中刚加入的条目，导致首次捕获误走 recount 路径）
            ColonyAnnouncement existingRaid = null;
            if (isRaidEvent)
            {
                announcement.IsRaidEvent = true;
                existingRaid = ColonyAnnouncementManager.Instance?.Data.Announcements
                    .Where(a => a.Status == AnnouncementStatus.Active && a.IsRaidEvent && a.Title == announcement.Title)
                    .OrderByDescending(a => a.CreatedTick)
                    .FirstOrDefault();
            }

            // 添加到管理器（重复条目会合并计数）
            MergeOrAdd(announcement);

            if (isRaidEvent)
            {
                var manager = ColonyAnnouncementManager.Instance;

                if (existingRaid != null)
                {
                    // 已存在同名袭击事件，合并计数后安排重新计数（可能有新敌人到达）
                    DebugLog.Log($"Raid event '{title}' already exists (ID: {existingRaid.Id}). Updating enemy count instead of creating new.");
                    manager?.ScheduleRaidRecount(existingRaid, 60);
                }
                else
                {
                    // 首次捕获：订阅当前地图的 Lord 事件（用于持续追踪初始敌人数）
                    var map = Find.CurrentMap;
                    if (map != null)
                    {
                        LordMonitorService.SubscribeToMap(map);
                        LordMonitorService.ResetMonitoring();
                    }

                    // 动物袭击不使用 Lord 系统，用短延迟直接计数；派系袭击用 Lord 监控持续追踪
                    if (announcement.RaidKind == RaidKindType.Animal ||
                        (announcement.RaidKind == RaidKindType.Unknown && RaidTrackingService.IsAnimalRaidEvent(title)))
                    {
                        manager?.ScheduleRaidInitialization(announcement, 60);
                    }
                    else
                    {
                        manager?.ScheduleRaidInitialization(announcement, 120);
                    }
                }
            }
        }

        /// <summary>
        /// 语言无关的类别判定：
        /// - Quest: Quest 归档条目本身，或 ChoiceLetter 带 quest 引用
        /// - Resource: LookTargets 中含资源类物品（基于 ThingDef 分类）
        /// - Event: 其余全部
        /// </summary>
        private static AnnouncementCategory DetermineCategory(IArchivable archivable)
        {
            // Quest detection（语言无关：类型 + quest 字段，不用标签文本）
            if (archivable is Quest)
                return AnnouncementCategory.Quest;

            if (archivable is ChoiceLetter { quest: not null })
                return AnnouncementCategory.Quest;

            // Resource detection（语言无关：LookTargets 指向资源类物品，基于 ThingDef）
            if (IsResourceEvent(archivable))
                return AnnouncementCategory.Resource;

            // Default to Event
            return AnnouncementCategory.Event;
        }

        /// <summary>
        /// 判断事件目标是否为资源类物品（货运舱坠落、资源空投等）。
        /// 语言无关：基于 ThingDef 的分类定义而非本地化文本。
        /// </summary>
        private static bool IsResourceEvent(IArchivable archivable)
        {
            if (archivable.LookTargets is not { Any: true } targets)
                return false;

            foreach (var target in targets.targets)
            {
                var thing = target.Thing;
                if (thing == null) continue;
                if (IsResourceThing(thing.def))
                    return true;
            }

            return false;
        }

        private static bool IsResourceThing(ThingDef def)
        {
            if (def == null) return false;

            if (def.thingCategories == null) return false;

            return def.thingCategories.Any(c =>
                c != null &&
                (c.defName == "Resources" || c.defName == "ResourceRaw" || c.defName == "StoneBlocks"));
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
