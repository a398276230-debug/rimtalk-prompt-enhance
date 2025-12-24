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

            // 检查延迟的袭击检测
            if (_pendingRaidCheckTick > 0 && currentTick >= _pendingRaidCheckTick)
            {
                CheckRaidCompletion();
                _pendingRaidCheckTick = -1; // 重置
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
                    Log.Message($"[RimTalk Enhance] Triggering daily synthesis. Day: {currentDay}, Last: {Data.LastSynthesisDay}");
                    Data.LastSynthesisDay = currentDay;
                    
                    // 更新所有工程的自动进度
                    BlueprintProgressService.UpdateAllAutoProjects();
                    
                    // 执行AI总结
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

        private void CheckRaidCompletion()
        {
            var map = Find.CurrentMap;
            if (map == null) return;
            
            // 检查是否还有敌对单位
            bool hasHostiles = map.mapPawns.AllPawns.Any(p => 
                p.HostileTo(Faction.OfPlayer) && 
                !p.Downed && 
                !p.Dead &&
                p.RaceProps.Humanlike);
            
            if (!hasHostiles)
            {
                // 自动完成所有活跃的袭击事件
                CompleteActiveRaidEvents();
            }
        }

        private void CompleteActiveRaidEvents()
        {
            if (Data.Announcements == null) return;
            
            int currentTick = Find.TickManager.TicksGame;
            var settings = RimTalkHealthEnhanceMod.Settings;
            
            // 袭击关键词
            string[] raidKeywords = { "raid", "attack", "siege", "infestation", "manhunter", "袭击", "进攻", "围攻", "虫害", "猎杀" };

            foreach (var announcement in Data.Announcements)
            {
                if (announcement.Status == AnnouncementStatus.Active && 
                    announcement.Category == AnnouncementCategory.Event)
                {
                    // 检查标题是否包含袭击关键词
                    bool isRaid = raidKeywords.Any(k => announcement.Title.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0);
                    
                    if (isRaid)
                    {
                        announcement.Status = AnnouncementStatus.Completed;
                        announcement.CompletedTick = currentTick;
                        Log.Message($"[RimTalk Enhance] Auto-completed raid event: {announcement.Title}");
                    }
                }
            }
            
            DataVersion++;
        }
    }
}
