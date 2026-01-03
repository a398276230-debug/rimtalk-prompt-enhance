using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimTalkHealthEnhance
{
    public class ColonyAnnouncementData : IExposable
    {
        // === 自由文本区域 ===
        public string ColonyOverview = "";  // 殖民地概况（玩家自由编辑）
        
        // === 结构化公告列表 ===
        public List<ColonyAnnouncement> Announcements = new List<ColonyAnnouncement>();
        
        // === 每日快照历史 ===
        public List<DailySnapshot> DailySnapshots = new List<DailySnapshot>();
        public int MaxSnapshotDays = 7;  // 保留最近 7 天
        
        // === 临时缓存（当日） ===
        public List<string> TodayActionLogs = new List<string>();
        public ColonySnapshot LastSnapshot;  // 昨日快照
        public int LastSynthesisDay = -1;    // 上次总结的天数（基于 GenDate.DaysPassed）
        
        // === 日期显示偏移量 ===
        // 用于玩家手动调整日期显示（只影响显示，不影响排序和过滤）
        // 存储为 AbsTick 偏移量
        public long DisplayTickOffset = 0;
        
        // === 兼容旧版本（已弃用）===
        [System.Obsolete("Use DisplayTickOffset instead")]
        public int SnapshotDayOffset = 0;
        [System.Obsolete("Use DisplayTickOffset instead")]
        public int SnapshotTickOffset = 0;
        
        public void ExposeData()
        {
            Scribe_Values.Look(ref ColonyOverview, "colonyOverview", "");
            Scribe_Collections.Look(ref Announcements, "announcements", LookMode.Deep);
            Scribe_Collections.Look(ref DailySnapshots, "dailySnapshots", LookMode.Deep);
            Scribe_Values.Look(ref MaxSnapshotDays, "maxSnapshotDays", 7);
            Scribe_Collections.Look(ref TodayActionLogs, "todayActions", LookMode.Value);
            Scribe_Deep.Look(ref LastSnapshot, "lastSnapshot");
            Scribe_Values.Look(ref LastSynthesisDay, "lastSynthesisDay", -1);
            
            // 新版本：使用 DisplayTickOffset
            Scribe_Values.Look(ref DisplayTickOffset, "displayTickOffset", 0L);
            
            // 兼容旧版本：读取旧字段并迁移
            #pragma warning disable CS0612
            Scribe_Values.Look(ref SnapshotDayOffset, "snapshotDayOffset", 0);
            Scribe_Values.Look(ref SnapshotTickOffset, "snapshotTickOffset", 0);
            
            // 如果是加载旧存档且新字段为0，从旧字段迁移
            if (Scribe.mode == LoadSaveMode.PostLoadInit && DisplayTickOffset == 0)
            {
                if (SnapshotTickOffset != 0)
                {
                    DisplayTickOffset = SnapshotTickOffset;
                }
                else if (SnapshotDayOffset != 0)
                {
                    DisplayTickOffset = SnapshotDayOffset * RimWorld.GenDate.TicksPerDay;
                }
            }
            #pragma warning restore CS0612
            
            if (Announcements == null) Announcements = new List<ColonyAnnouncement>();
            if (DailySnapshots == null) DailySnapshots = new List<DailySnapshot>();
            if (TodayActionLogs == null) TodayActionLogs = new List<string>();
            
            // 加载后处理：清理无效的快照
            if (Scribe.mode == LoadSaveMode.PostLoadInit && DailySnapshots != null)
            {
                int beforeCount = DailySnapshots.Count;
                
                // 移除无效的快照（AbsTick <= 0）
                DailySnapshots.RemoveAll(s => s == null || !s.IsValid);
                
                int removedCount = beforeCount - DailySnapshots.Count;
                if (removedCount > 0)
                {
                    Log.Warning($"[RimTalk Enhance] Removed {removedCount} invalid snapshots from old save. Remaining: {DailySnapshots.Count}");
                }
                
                // 检查并修复重复的 AbsTick
                var duplicateGroups = DailySnapshots.GroupBy(s => s.AbsTick).Where(g => g.Count() > 1).ToList();
                if (duplicateGroups.Any())
                {
                    Log.Warning($"[RimTalk Enhance] Found {duplicateGroups.Count} groups of duplicate AbsTick. Keeping only the first of each.");
                    var seenAbsTicks = new HashSet<long>();
                    DailySnapshots.RemoveAll(s => !seenAbsTicks.Add(s.AbsTick));
                }
                
                Log.Message($"[RimTalk Enhance] Loaded {DailySnapshots.Count} valid daily snapshots.");
            }
        }
    }

    public class ColonyAnnouncement : IExposable
    {
        public string Id;
        public AnnouncementCategory Category;
        public string Title;
        public string Description;
        public AnnouncementPriority Priority;
        public AnnouncementStatus Status;
        
        // === 可选字段 ===
        public float Progress = 0f;            // 进度 (0-1)
        public string AssignedPawnName;        // 负责人姓名
        public int DeadlineTicks = -1;         // 截止时间 (-1表示无)
        public int RelatedQuestId = -1;        // 关联的任务ID (用于自动追踪状态)
        public bool IsAutoCaptured = false;    // 是否为自动捕获的事件
        public bool IsGlobal = false;          // 是否全局生效（在所有地图，包括进攻其他派系时）
        
        // === 施工区域相关 ===
        public string BlueprintAreaId;         // 关联的施工区域ID
        public int InitialBlueprintCount = 0;  // 初始蓝图数量
        public bool AutoCalculateProgress = false; // 是否自动计算进度（(初始-剩余)/初始，0%→100%）
        
        // === 商队事件 ===
        public bool IsCaravanEvent = false;    // 是否为商队事件
        
        // === 袭击事件统计 ===
        public bool IsRaidEvent = false;       // 是否为袭击事件
        public int RaidInitialCount = 0;       // 初始敌人数量
        public int RaidKillCount = 0;          // 敌人死亡数量
        public int RaidFleeCount = 0;          // 敌人撤退数量
        public int RaidDownedCount = 0;        // 敌人倒地数量（被俘虏）
        public int ColonistDeathCount = 0;     // 殖民者死亡数量
        public int ColonistDownedCount = 0;    // 殖民者倒地数量
        
        // === 环境事件 ===
        public bool IsWeatherEvent = false;           // 是否为天气变化事件
        public bool IsGameConditionEvent = false;     // 是否为游戏状况事件（热浪、寒潮、毒雾等）
        public string GameConditionDefName;           // 游戏状况的defName（用于关联）
        
        public int CreatedTick;
        public int CompletedTick;
        
        public ColonyAnnouncement()
        {
            // Default constructor for Scribe
        }
        
        public void ExposeData()
        {
            Scribe_Values.Look(ref Id, "id");
            Scribe_Values.Look(ref Category, "category", AnnouncementCategory.Project);
            Scribe_Values.Look(ref Title, "title");
            Scribe_Values.Look(ref Description, "description");
            Scribe_Values.Look(ref Priority, "priority", AnnouncementPriority.Normal);
            Scribe_Values.Look(ref Status, "status", AnnouncementStatus.Active);
            
            Scribe_Values.Look(ref Progress, "progress", 0f);
            Scribe_Values.Look(ref AssignedPawnName, "assignedPawnName");
            Scribe_Values.Look(ref DeadlineTicks, "deadlineTicks", -1);
            Scribe_Values.Look(ref RelatedQuestId, "relatedQuestId", -1);
            Scribe_Values.Look(ref IsAutoCaptured, "isAutoCaptured", false);
            Scribe_Values.Look(ref IsGlobal, "isGlobal", false);
            
            Scribe_Values.Look(ref BlueprintAreaId, "blueprintAreaId");
            Scribe_Values.Look(ref InitialBlueprintCount, "initialBlueprintCount", 0);
            Scribe_Values.Look(ref AutoCalculateProgress, "autoCalculateProgress", false);
            
            // 商队事件
            Scribe_Values.Look(ref IsCaravanEvent, "isCaravanEvent", false);
            
            // 袭击统计
            Scribe_Values.Look(ref IsRaidEvent, "isRaidEvent", false);
            Scribe_Values.Look(ref RaidInitialCount, "raidInitialCount", 0);
            Scribe_Values.Look(ref RaidKillCount, "raidKillCount", 0);
            Scribe_Values.Look(ref RaidFleeCount, "raidFleeCount", 0);
            Scribe_Values.Look(ref RaidDownedCount, "raidDownedCount", 0);
            Scribe_Values.Look(ref ColonistDeathCount, "colonistDeathCount", 0);
            Scribe_Values.Look(ref ColonistDownedCount, "colonistDownedCount", 0);
            
            // 环境事件
            Scribe_Values.Look(ref IsWeatherEvent, "isWeatherEvent", false);
            Scribe_Values.Look(ref IsGameConditionEvent, "isGameConditionEvent", false);
            Scribe_Values.Look(ref GameConditionDefName, "gameConditionDefName");
            
            Scribe_Values.Look(ref CreatedTick, "createdTick");
            Scribe_Values.Look(ref CompletedTick, "completedTick");
        }
    }

    public enum AnnouncementCategory
    {
        Project,      // 工程
        Event,        // 事件
        Quest,        // 游戏任务
        Resource,     // 资源
        Personnel,    // 人员
        Custom        // 自定义
    }

    public enum AnnouncementStatus
    {
        Active,
        Completed,
        Paused
    }

    public enum AnnouncementPriority
    {
        Low,
        Normal,
        High,
        Urgent
    }
}
