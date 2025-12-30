using System;
using System.Collections.Generic;
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
        public int LastSynthesisDay = -1;    // 上次总结的天数（基于游戏真实天数）
        public int SnapshotDayOffset = 0;    // 快照日期偏移量（用于玩家手动调整日期）
        
        public void ExposeData()
        {
            Scribe_Values.Look(ref ColonyOverview, "colonyOverview", "");
            Scribe_Collections.Look(ref Announcements, "announcements", LookMode.Deep);
            Scribe_Collections.Look(ref DailySnapshots, "dailySnapshots", LookMode.Deep);
            Scribe_Values.Look(ref MaxSnapshotDays, "maxSnapshotDays", 7);
            Scribe_Collections.Look(ref TodayActionLogs, "todayActions", LookMode.Value);
            Scribe_Deep.Look(ref LastSnapshot, "lastSnapshot");
            Scribe_Values.Look(ref LastSynthesisDay, "lastSynthesisDay", -1);
            Scribe_Values.Look(ref SnapshotDayOffset, "snapshotDayOffset", 0);
            
            if (Announcements == null) Announcements = new List<ColonyAnnouncement>();
            if (DailySnapshots == null) DailySnapshots = new List<DailySnapshot>();
            if (TodayActionLogs == null) TodayActionLogs = new List<string>();
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
