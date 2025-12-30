using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimTalkHealthEnhance.Models;

namespace RimTalkHealthEnhance
{
    public class ColonyAnnouncementManager : GameComponent
    {
        public static ColonyAnnouncementManager Instance => Current.Game?.GetComponent<ColonyAnnouncementManager>();
        
        public ColonyAnnouncementData Data = new ColonyAnnouncementData();
        
        // 自定义命名区域列表
        public List<CustomNamedArea> CustomAreas = new List<CustomNamedArea>();
        
        // 用于UI缓存刷新的版本号
        public int DataVersion { get; private set; } = 0;
        
        private bool initialized = false;
        
        // 派系信息缓存（线程安全）
        private string _cachedFactionInfo = null;
        private int _lastFactionUpdateTick = 0;

        // 袭击检测延迟标记
        private int _pendingRaidCheckTick = -1;
        
        // 上次检测时的敌人数量（用于检测变化）
        private int _lastHostileCount = 0;
        
        // 延迟初始化袭击追踪的队列
        private List<(ColonyAnnouncement Event, int TargetTick)> _pendingRaidInitializations = new List<(ColonyAnnouncement, int)>();
        
        // 商队检测延迟标记
        private int _pendingCaravanCheckTick = -1;

        public ColonyAnnouncementManager(Game game)
        {
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();
            
            if (!initialized)
            {
                initialized = true;
                // 如果是首次加载（无历史快照），拍摄基准快照
                if (Data.LastSnapshot == null)
                {
                    Data.LastSnapshot = SnapshotService.TakeSnapshot();
                    // 设置上次总结时间为当前，避免立即触发（除非跨天）
                    // 如果是新游戏，DaysPassed 为 0，LastSynthesisDay 为 0，明天触发。
                    // 如果是旧存档，DaysPassed 为 N，LastSynthesisDay 为 N，明天触发。
                    Data.LastSynthesisDay = GenDate.DaysPassed;
                    Log.Message($"[RimTalk Enhance] Initialized baseline snapshot. Day: {Data.LastSynthesisDay}");
                }
                
                // 订阅当前地图的 Lord 事件
                var map = Find.CurrentMap;
                if (map != null)
                {
                    LordMonitorService.SubscribeToMap(map);
                }
            }
            
            int currentTick = Find.TickManager.TicksGame;
            
            // 定期更新派系信息缓存（线程安全）
            var settings = RimTalkHealthEnhanceMod.Settings;
            if (settings != null && settings.ShowFactionRelations)
            {
                int updateInterval = (int)(settings.FactionCacheUpdateInterval * 60); // 转换为 ticks
                if (currentTick - _lastFactionUpdateTick >= updateInterval)
                {
                    UpdateFactionCache();
                    _lastFactionUpdateTick = currentTick;
                }
            }
            
            // 每2000 ticks (约30秒) 检查一次
            if (currentTick % 2000 == 0)
            {
                CheckAutoCompletion();
            }

            // 检查延迟的袭击检测（改进版：使用 tick 比较，确保快进时也能正确检测）
            if (_pendingRaidCheckTick > 0 && currentTick >= _pendingRaidCheckTick)
            {
                Log.Message($"[RimTalk Enhance] Raid check delay elapsed. CurrentTick: {currentTick}, ScheduledTick: {_pendingRaidCheckTick}");
                CheckRaidCompletion();
                _pendingRaidCheckTick = -1; // 重置
            }
            
            // 额外保障：每2000 ticks 主动检查一次是否还有敌对单位
            // 这样即使错过了延迟检测，也能在30秒内自动完成
            if (currentTick % 2000 == 0)
            {
                CheckRaidCompletionFallback();
            }
            
            // 检查延迟的商队检测
            if (_pendingCaravanCheckTick > 0 && currentTick >= _pendingCaravanCheckTick)
            {
                Log.Message($"[RimTalk Enhance] Caravan check delay elapsed. CurrentTick: {currentTick}");
                CaravanTrackingService.CheckCaravanDepartures();
                _pendingCaravanCheckTick = -1;
            }
            
            // 每2000 ticks 也检查一次商队离开状态
            if (currentTick % 2000 == 0)
            {
                CaravanTrackingService.CheckCaravanDepartures();
            }
            
            // 处理延迟的袭击初始化
            ProcessPendingRaidInitializations(currentTick);
            
            // 定期更新 Lord 监控的初始计数（每60 ticks = 1秒检查一次）
            // 这样可以持续追踪空投/边缘生成的敌人
            if (currentTick % 60 == 0)
            {
                LordMonitorService.PeriodicUpdate();
            }
            
            // 每日 0 点触发 AI 总结
            // 使用天数判断，避免跳过时间导致错过触发
            int currentDay = GenDate.DaysPassed;
            
            // Debug log every hour to check status
            if (currentTick % 2500 == 0)
            {
                // Log.Message($"[RimTalk Debug] Tick: {currentTick}, Day: {currentDay}, LastSynthesisDay: {Data.LastSynthesisDay}");
            }

            if (currentDay > Data.LastSynthesisDay)
            {
                // 检查当前地图是否属于玩家殖民地
                var map = Find.CurrentMap;
                if (map != null && map.IsPlayerHome)
                {
                    Log.Message($"[RimTalk Enhance] Triggering daily synthesis. GameDay: {currentDay}, LastSynthesisDay: {Data.LastSynthesisDay}, Offset: {Data.SnapshotDayOffset}");
                    
                    // 更新 LastSynthesisDay 为当前游戏天数
                    Data.LastSynthesisDay = currentDay;
                    
                    // 更新所有工程的自动进度
                    BlueprintProgressService.UpdateAllAutoProjects();
                    
                    // 执行AI总结（MidnightSynthesisService 会使用 currentDay + SnapshotDayOffset 计算显示日期）
                    _ = MidnightSynthesisService.PerformSynthesis();
                }
                else
                {
                    // 如果不在主殖民地，仍然更新日期，避免重复触发
                    Data.LastSynthesisDay = currentDay;
                    Log.Message($"[RimTalk Enhance] Skipping daily synthesis - not on player home map. Day: {currentDay}");
                }
            }
        }

        private void CheckAutoCompletion()
        {
            if (Data.Announcements == null) return;
            
            var settings = RimTalkHealthEnhanceMod.Settings;
            if (settings == null) return;

            int currentTick = Find.TickManager.TicksGame;
            var toRemove = new List<ColonyAnnouncement>();

            foreach (var announcement in Data.Announcements)
            {
                // 修复旧事件：如果是自动捕获的事件但没有设置过期时间，根据当前设置补充
                if (announcement.IsAutoCaptured && 
                    announcement.Status == AnnouncementStatus.Active && 
                    announcement.DeadlineTicks <= 0)
                {
                    if (announcement.Category == AnnouncementCategory.Quest && settings.AutoCompleteDays > 0)
                    {
                        announcement.DeadlineTicks = announcement.CreatedTick + (settings.AutoCompleteDays * 60000);
                    }
                    else if (announcement.Category != AnnouncementCategory.Quest && settings.EventExpireDays > 0)
                    {
                        announcement.DeadlineTicks = announcement.CreatedTick + (int)(settings.EventExpireDays * 60000);
                    }
                }
                
                // 检查自动完成 (时间)
                if (announcement.Status == AnnouncementStatus.Active && 
                    announcement.DeadlineTicks > 0 && 
                    currentTick > announcement.DeadlineTicks)
                {
                    announcement.Status = AnnouncementStatus.Completed;
                    announcement.CompletedTick = currentTick;
                }
                
                // 检查自动完成 (任务状态)
                if (announcement.Status == AnnouncementStatus.Active && 
                    announcement.RelatedQuestId != -1)
                {
                    var quest = Find.QuestManager.QuestsListForReading.FirstOrDefault(q => q.id == announcement.RelatedQuestId);
                    if (quest != null && (quest.State == QuestState.EndedSuccess || 
                                          quest.State == QuestState.EndedFailed || 
                                          quest.State == QuestState.EndedUnknownOutcome))
                    {
                        announcement.Status = AnnouncementStatus.Completed;
                        announcement.CompletedTick = currentTick;
                    }
                }

                // 检查自动归档（删除）
                if (announcement.Status == AnnouncementStatus.Completed && 
                    announcement.CompletedTick > 0)
                {
                    // 自动捕获的事件：使用可配置的删除时间（0-3天）
                    if (announcement.IsAutoCaptured)
                    {
                        int deleteTicks = (int)(settings.AutoCapturedDeleteDays * 60000);
                        if (deleteTicks == 0 || currentTick > announcement.CompletedTick + deleteTicks)
                        {
                            toRemove.Add(announcement);
                        }
                    }
                    // 手动创建的事件：使用原有的归档设置（1天）
                    else if (settings.AutoArchiveCompleted)
                    {
                        if (currentTick > announcement.CompletedTick + 60000)
                        {
                            toRemove.Add(announcement);
                        }
                    }
                }
            }

            foreach (var item in toRemove)
            {
                Data.Announcements.Remove(item);
            }
            
            if (toRemove.Count > 0)
                DataVersion++;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref Data, "colonyAnnouncementData");
            Scribe_Collections.Look(ref CustomAreas, "customAreas", LookMode.Deep);
            
            if (Data == null)
                Data = new ColonyAnnouncementData();
            
            if (CustomAreas == null)
                CustomAreas = new List<CustomNamedArea>();
            
            // 加载后重新关联地图
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                var map = Find.CurrentMap;
                if (map != null)
                {
                    foreach (var area in CustomAreas)
                    {
                        area.ReassignMap(map);
                    }
                }
            }
        }

        public void AddAnnouncement(ColonyAnnouncement announcement)
        {
            if (Data.Announcements == null)
                Data.Announcements = new List<ColonyAnnouncement>();
                
            Data.Announcements.Add(announcement);
            DataVersion++;
        }

        public void DeleteAnnouncement(string id)
        {
            if (Data.Announcements == null) return;
            
            var item = Data.Announcements.FirstOrDefault(t => t.Id == id);
            if (item != null)
            {
                // 如果工程关联了施工区域，同步删除区域
                if (!string.IsNullOrEmpty(item.BlueprintAreaId))
                {
                    DeleteCustomArea(item.BlueprintAreaId);
                }
                
                Data.Announcements.Remove(item);
                DataVersion++;
            }
        }
        
        public void NotifyDataChanged()
        {
            DataVersion++;
        }

        public List<ColonyAnnouncement> GetActiveAnnouncements()
        {
            if (Data.Announcements == null) return new List<ColonyAnnouncement>();
            return Data.Announcements.Where(t => t.Status == AnnouncementStatus.Active).ToList();
        }
        
        /// <summary>
        /// 更新派系信息缓存（在主线程调用）
        /// </summary>
        public void UpdateFactionCache()
        {
            _cachedFactionInfo = FactionInfoBuilder.BuildFactionContextUnsafe();
        }
        
        /// <summary>
        /// 获取缓存的派系信息（线程安全）
        /// </summary>
        public string GetCachedFactionInfo()
        {
            return _cachedFactionInfo;
        }
        
        /// <summary>
        /// 添加自定义区域
        /// </summary>
        public void AddCustomArea(CustomNamedArea area)
        {
            if (CustomAreas == null)
                CustomAreas = new List<CustomNamedArea>();
            
            CustomAreas.Add(area);
            DataVersion++;
            
            // 刷新布局缓存
            if (area.Map != null)
                ColonyLayoutBuilder.InvalidateCache(area.Map);
        }
        
        /// <summary>
        /// 删除自定义区域
        /// </summary>
        public void DeleteCustomArea(string id)
        {
            if (CustomAreas == null) return;
            
            var area = CustomAreas.FirstOrDefault(a => a.Id == id);
            if (area != null)
            {
                // 如果是施工区域，更新关联的工程状态
                if (area.IsConstructionArea)
                {
                    var project = Data.Announcements.FirstOrDefault(a => a.BlueprintAreaId == id);
                    if (project != null)
                    {
                        project.BlueprintAreaId = null;
                    }
                }

                var map = area.Map;
                CustomAreas.Remove(area);
                DataVersion++;
                
                // 刷新布局缓存
                if (map != null)
                    ColonyLayoutBuilder.InvalidateCache(map);
            }
        }
        
        /// <summary>
        /// 通知区域已修改（用于重命名、修改格子等）
        /// </summary>
        public void NotifyAreaModified(CustomNamedArea area)
        {
            DataVersion++;
            if (area?.Map != null)
                ColonyLayoutBuilder.InvalidateCache(area.Map);
        }
        
        /// <summary>
        /// 获取指定位置的自定义区域（优先级：最后创建的优先）
        /// </summary>
        public CustomNamedArea GetCustomAreaAt(IntVec3 position)
        {
            if (CustomAreas == null) return null;
            
            for (int i = CustomAreas.Count - 1; i >= 0; i--)
            {
                var area = CustomAreas[i];
                if (!area.IsActive) continue;
                if (area.Cells == null) continue;
                
                if (area[position])
                    return area;
            }
            
            return null;
        }

        /// <summary>
        /// 安排延迟初始化袭击追踪
        /// </summary>
        public void ScheduleRaidInitialization(ColonyAnnouncement raidEvent, int delayTicks)
        {
            int targetTick = Find.TickManager.TicksGame + delayTicks;
            _pendingRaidInitializations.Add((raidEvent, targetTick));
            Log.Message($"[RimTalk Enhance] Scheduled raid initialization for '{raidEvent.Title}' at tick {targetTick} (delay: {delayTicks} ticks)");
        }
        
        /// <summary>
        /// 安排重新计数敌人（用于同一袭击有新增敌人时）
        /// </summary>
        public void ScheduleRaidRecount(ColonyAnnouncement raidEvent, int delayTicks)
        {
            if (raidEvent == null) return;
            
            int targetTick = Find.TickManager.TicksGame + delayTicks;
            
            // 使用同样的队列，但会更新计数而非初始化
            _pendingRaidInitializations.Add((raidEvent, targetTick));
            Log.Message($"[RimTalk Enhance] Scheduled raid recount for '{raidEvent.Title}' at tick {targetTick}");
        }
        
        /// <summary>
        /// 处理延迟的袭击初始化
        /// </summary>
        private void ProcessPendingRaidInitializations(int currentTick)
        {
            if (_pendingRaidInitializations.Count == 0) return;
            
            var toRemove = new List<(ColonyAnnouncement, int)>();
            
            foreach (var item in _pendingRaidInitializations)
            {
                if (currentTick >= item.TargetTick)
                {
                    var raidEvent = item.Event;
                    var map = Find.CurrentMap;
                    
                    if (map != null)
                    {
                        int count = RaidTrackingService.CountHostileThreats(map);
                        
                        // 如果已有初始计数，说明是重新计数（有新增敌人），取较大值
                        if (raidEvent.RaidInitialCount > 0)
                        {
                            int newCount = Math.Max(raidEvent.RaidInitialCount, count);
                            Log.Message($"[RimTalk Enhance] Raid recount for '{raidEvent.Title}'. Previous: {raidEvent.RaidInitialCount}, Current: {count}, Updated to: {newCount}");
                            raidEvent.RaidInitialCount = newCount;
                        }
                        else
                        {
                            raidEvent.RaidInitialCount = count;
                            Log.Message($"[RimTalk Enhance] Raid tracking initialized (delayed) for '{raidEvent.Title}'. Initial enemies: {count}");
                        }
                        
                        RaidTrackingService.SetActiveRaidEvent(raidEvent);
                        
                        // 更新敌人计数
                        _lastHostileCount = count;
                    }
                    else
                    {
                        Log.Warning($"[RimTalk Enhance] ProcessPendingRaidInitializations: No current map for event '{raidEvent.Title}'");
                    }
                    
                    toRemove.Add(item);
                }
            }
            
            foreach (var item in toRemove)
            {
                _pendingRaidInitializations.Remove(item);
            }
        }
        
        /// <summary>
        /// 调度袭击检测（由 Pawn 死亡事件触发）
        /// </summary>
        public void ScheduleRaidCheck()
        {
            var settings = RimTalkHealthEnhanceMod.Settings;
            if (settings == null || !settings.AutoCompleteRaidEvents) return;

            // 每次敌人死亡时，重置延迟时间
            int delayTicks = (int)(settings.RaidCheckDelay * 60); // 秒转ticks
            _pendingRaidCheckTick = Find.TickManager.TicksGame + delayTicks;
        }
        
        /// <summary>
        /// 调度商队检测（由 Lord 移除事件触发）
        /// </summary>
        public void ScheduleCaravanCheck(int delayTicks)
        {
            var settings = RimTalkHealthEnhanceMod.Settings;
            if (settings == null || !settings.AutoCompleteCaravanEvents) return;
            
            _pendingCaravanCheckTick = Find.TickManager.TicksGame + delayTicks;
            Log.Message($"[RimTalk Enhance] Scheduled caravan check at tick {_pendingCaravanCheckTick}");
        }

        private void CheckRaidCompletion()
        {
            var map = Find.CurrentMap;
            if (map == null) return;
            
            // 使用 RaidTrackingService 的计数方法，确保逻辑一致
            int hostileCount = RaidTrackingService.CountHostileThreats(map);
            
            Log.Message($"[RimTalk Enhance] CheckRaidCompletion: Hostile count = {hostileCount}");
            
            if (hostileCount == 0)
            {
                // 自动完成所有活跃的袭击事件，并附加战斗报告
                CompleteActiveRaidEvents();
            }
            
            _lastHostileCount = hostileCount;
        }
        
        /// <summary>
        /// 备用的袭击检测（每2000 ticks调用一次）
        /// 用于在延迟检测失败时作为保障
        /// </summary>
        private void CheckRaidCompletionFallback()
        {
            var settings = RimTalkHealthEnhanceMod.Settings;
            if (settings == null || !settings.AutoCompleteRaidEvents) return;
            
            // 只有当有活跃的袭击事件时才检测
            if (Data.Announcements == null) return;
            bool hasActiveRaid = Data.Announcements.Any(a =>
                a.Status == AnnouncementStatus.Active &&
                a.Category == AnnouncementCategory.Event &&
                a.IsRaidEvent);
            
            if (!hasActiveRaid) return;
            
            var map = Find.CurrentMap;
            if (map == null) return;
            
            // 使用 RaidTrackingService 的计数方法，确保逻辑一致
            int hostileCount = RaidTrackingService.CountHostileThreats(map);
            
            // 如果敌对单位从有变成没有，触发完成检测
            if (_lastHostileCount > 0 && hostileCount == 0)
            {
                Log.Message($"[RimTalk Enhance] Fallback raid check triggered. Previous: {_lastHostileCount}, Current: {hostileCount}");
                CompleteActiveRaidEvents();
            }
            
            _lastHostileCount = hostileCount;
        }

        private void CompleteActiveRaidEvents()
        {
            if (Data.Announcements == null) return;
            
            int currentTick = Find.TickManager.TicksGame;
            var settings = RimTalkHealthEnhanceMod.Settings;

            foreach (var announcement in Data.Announcements)
            {
                if (announcement.Status == AnnouncementStatus.Active &&
                    announcement.Category == AnnouncementCategory.Event &&
                    announcement.IsRaidEvent)
                {
                    // 生成战斗报告并附加到描述
                    string battleReport = RaidTrackingService.FinishRaidTracking(announcement);
                    if (!string.IsNullOrEmpty(battleReport))
                    {
                        if (string.IsNullOrEmpty(announcement.Description))
                        {
                            announcement.Description = battleReport;
                        }
                        else
                        {
                            announcement.Description += "\n" + battleReport;
                        }
                    }
                    
                    announcement.Status = AnnouncementStatus.Completed;
                    announcement.CompletedTick = currentTick;
                    Log.Message($"[RimTalk Enhance] Auto-completed raid event: {announcement.Title}. {battleReport}");
                }
            }
            
            DataVersion++;
        }
    }
}
