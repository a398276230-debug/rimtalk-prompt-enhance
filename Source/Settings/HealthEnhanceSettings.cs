using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace RimTalkHealthEnhance
{
    public enum AIProvider
    {
        OpenAI,
        Google, // Gemini
        DeepSeek,
        Player2,
        Custom // OpenAI Compatible
    }

    public enum SnapshotInjectionMode
    {
        Context,  // 注入到 Context（系统上下文）
        Prompt    // 注入到 Prompt（对话提示词）
    }

    /// <summary>
    /// Settings for RimTalk Enhancement (Health & Items)
    /// </summary>
    public class HealthEnhanceSettings : ModSettings
    {
        // === Health Settings ===
        public bool ShowSeverity = true;
        public bool ShowPainLevel = true;
        public bool ShowLethalMarker = true;
        public bool ShowDescription = true;
        public float MinPainToShow = 0.01f;
        public float LethalThreshold = 0.8f;
        public int MaxDescriptionLength = 100;

        // === Health Filtering Settings ===
        public bool ShowBionics = true;           // 显示仿生体/义肢
        public bool ShowImplants = true;          // 显示其他植入物
        public bool ShowInjuries = true;          // 显示伤口
        public bool ShowMissingParts = true;      // 显示缺失部位
        public bool ShowConditions = true;        // 显示疾病/状态

        // 数量限制 (0 = 无限制)
        public int MaxBionicsToShow = 10;         // 最多显示仿生体数量
        public int MaxImplantsToShow = 10;        // 最多显示植入物数量
        public int MaxInjuriesToShow = 20;        // 最多显示伤口数量
        public int MaxConditionsToShow = 10;      // 最多显示状态数量

        // 智能整合选项（默认开启以节省tokens）
        public bool EnableInjuryConsolidation = true;  // 启用伤口整合
        public bool EnableBionicSummary = true;        // 启用仿生体摘要模式
        public float MinorInjurySeverityThreshold = 0.3f; // 轻伤阈值

        // === Item Settings ===
        public bool ShowEquipmentDesc = true;
        public bool ShowCarriedItemDesc = true;
        public bool ShowInventoryItems = false;
        public bool ShowInventoryDesc = false;
        public QualityCategory MinQualityForDesc = QualityCategory.Normal;
        public int ItemMaxDescriptionLength = 100;
        public int MaxInventoryItems = 3;
        public int MaxItemsWithDesc = 5;
        public bool SkipCommonItems = true;
        public bool SkipArtDescription = true;

        // === Interaction Settings ===
        public bool ShowInteractionDesc = true;           // Show description of item/building being used
        public bool OnlyShowImportantBuildings = true;    // Only show important buildings (workbenches, etc.)
        public int InteractionMaxDescLength = 100;        // Max length for interaction description

        // === Announcement Settings ===
        public bool ShowColonyAnnouncements = true;
        public bool ShowColonyOverview = true;        // 显示自由文本概况
        public bool ShowStructuredTasks = true;       // 显示结构化任务
        public bool OnlyShowActiveTasks = true;       // 只显示进行中的任务
        public float CompletedTaskShowDays = 1f;      // 已完成任务保留显示天数
        public int MaxOverviewLength = 500;           // 概况文本最大长度

        // === Faction Relations Settings ===
        public bool ShowFactionRelations = true;
        public bool ShowFactionGoodwill = true;
        public bool ShowFactionMemberCount = true;
        public bool ShowIdentityBreakdown = true;     // 显示身份细分（囚犯/敌人/访客）
        public bool ShowGlobalSummary = false;        // 显示全局身份摘要
        public bool ShowNeutralFactions = true;
        public bool FilterByGoodwill = false;
        public int MinGoodwillToShow = -100;
        public float FactionCacheUpdateInterval = 5f;  // 派系信息缓存更新间隔（秒）

        // === Auto Event Capture Settings ===
        public bool EnableAutoEventCapture = true;
        public bool AutoCaptureQuests = true;
        public bool AutoCaptureEvents = true;
        public bool AutoCaptureResources = false;
        public int AutoCompleteDays = 7;              // 任务自动完成时间
        public float EventExpireDays = 1f;            // 普通事件自动完成时间
        public bool MergeDuplicateEvents = true;      // 合并重复事件
        public bool AutoArchiveCompleted = false;     // 自动归档已完成的事件（手动创建的）
        public float AutoCapturedDeleteDays = 0.5f;   // 自动捕获事件完成后删除时间（0-3天，0表示立即）
        public bool AutoCompleteRaidEvents = true;    // 自动完成袭击事件（当敌对单位被消灭时）
        public float RaidCheckDelay = 3f;             // 袭击检测延迟时间（秒）
        public bool AutoCompleteCaravanEvents = true; // 自动完成商队事件（当商队离开时）
        
        // === 环境事件捕获 ===
        public bool AutoCaptureWeather = true;        // 自动捕获天气变化
        public bool AutoCaptureGameConditions = true; // 自动捕获游戏状况（热浪、寒潮、毒雾等）
        public float WeatherEventExpireHours = 6f;    // 天气事件过期时间（游戏小时）
        public float GameConditionExpireHours = 24f;  // 游戏状况过期时间（游戏小时，永久状况用）
        
        // === Location Context Settings ===
        public bool ShowRelativeLocation = true;         // 启用相对位置显示
        public bool ShowAreaInfo = true;                 // 显示 Area 信息
        public bool EnableTownCenterDetection = false;   // 启用城镇核心检测
        public int TownCenterRadius = 20;                // 城镇核心半径
        public IntVec3 ColonyCenterOffset = IntVec3.Zero; // 殖民地中心点偏移
        
        public bool EnableGlobalLayout = false;          // 启用全局布局信息
        public int MinRoomSize = 9;                      // 最小房间面积
        public int MaxLayoutDistance = 100;              // 最大距离
        public bool IncludeCustomAreas = true;           // 包含自定义区域
        public bool GroupByDirection = true;             // 按方位分组
        public bool OnlyShowNamedRooms = true;           // 只显示有名称的房间

        // === Misc Settings ===
        public bool UnlimitedRelations = false;          // 解除关系数量限制
        public bool UnlimitedTraits = false;             // 解除配角特质限制

        // === AI Synthesis Settings ===
        public bool EnableAISynthesis = false;
        public bool InjectSnapshotToContext = true;      // 是否将快照注入到 AI context
        public SnapshotInjectionMode SnapshotInjectionTarget = SnapshotInjectionMode.Context; // 注入位置
        public float SnapshotInjectDays = 1.0f;          // 注入多少天的快照（0.5-7天）
        public bool IncludeProjectsInSnapshot = true;    // 将状况板工程信息发给史官
        public bool IncludeResearchInSnapshot = false;   // 将科技状态发给史官（默认关闭）
        public bool IncludeUnfinishedResearch = false;   // 包含未完成的科技列表
        public bool IncludePowerInSnapshot = false;      // 将电力状态发给史官（默认关闭）
        public AIProvider SynthesisProvider = AIProvider.OpenAI;
        public string CustomApiKey = "";
        public string CustomApiUrl = "";
        public string CustomModelName = "gpt-4o-mini";
        
        // 自定义提示词
        public string CustomOverviewSummaryPrompt = "";  // 概况总结提示词
        public string CustomDailySynthesisPrompt = "";   // 每日快照提示词
        public string CustomProjectSummaryPrompt = "";   // 工程AI总结提示词
        
        // 存储每种事件类型的启用状态 (TypeName -> Enabled)
        public Dictionary<string, bool> EnabledEventTypes = new Dictionary<string, bool>();
        
        // 缓存发现的类型（不保存）
        public static List<string> DiscoveredEventTypes = new List<string>();

        // === Scroll Positions ===
        private Vector2 _contextEnhancementScrollPosition = Vector2.zero;
        private Vector2 _colonyStatusScrollPosition = Vector2.zero;
        
        // === Collapsible Section States ===
        private static bool _healthSectionExpanded = true;
        private static bool _itemsSectionExpanded = true;
        private static bool _factionsSectionExpanded = true;
        private static bool _locationSectionExpanded = true;
        private static bool _miscSectionExpanded = true;
        private static bool _announcementSectionExpanded = true;
        private static bool _autoCaptureSectionExpanded = true;
        private static bool _aiHistorianSectionExpanded = true;

        public override void ExposeData()
        {
            base.ExposeData();
            // Health
            Scribe_Values.Look(ref ShowSeverity, "showSeverity", true);
            Scribe_Values.Look(ref ShowPainLevel, "showPainLevel", true);
            Scribe_Values.Look(ref ShowLethalMarker, "showLethalMarker", true);
            Scribe_Values.Look(ref ShowDescription, "showDescription", true);
            Scribe_Values.Look(ref MinPainToShow, "minPainToShow", 0.01f);
            Scribe_Values.Look(ref LethalThreshold, "lethalThreshold", 0.8f);
            Scribe_Values.Look(ref MaxDescriptionLength, "maxDescriptionLength", 100);

            // Health Filtering
            Scribe_Values.Look(ref ShowBionics, "showBionics", true);
            Scribe_Values.Look(ref ShowImplants, "showImplants", true);
            Scribe_Values.Look(ref ShowInjuries, "showInjuries", true);
            Scribe_Values.Look(ref ShowMissingParts, "showMissingParts", true);
            Scribe_Values.Look(ref ShowConditions, "showConditions", true);
            Scribe_Values.Look(ref MaxBionicsToShow, "maxBionicsToShow", 10);
            Scribe_Values.Look(ref MaxImplantsToShow, "maxImplantsToShow", 10);
            Scribe_Values.Look(ref MaxInjuriesToShow, "maxInjuriesToShow", 20);
            Scribe_Values.Look(ref MaxConditionsToShow, "maxConditionsToShow", 10);
            Scribe_Values.Look(ref EnableInjuryConsolidation, "enableInjuryConsolidation", true);
            Scribe_Values.Look(ref EnableBionicSummary, "enableBionicSummary", true);
            Scribe_Values.Look(ref MinorInjurySeverityThreshold, "minorInjurySeverityThreshold", 0.3f);

            // Items
            Scribe_Values.Look(ref ShowEquipmentDesc, "showEquipmentDesc", true);
            Scribe_Values.Look(ref ShowCarriedItemDesc, "showCarriedItemDesc", true);
            Scribe_Values.Look(ref ShowInventoryItems, "showInventoryItems", false);
            Scribe_Values.Look(ref ShowInventoryDesc, "showInventoryDesc", false);
            Scribe_Values.Look(ref MinQualityForDesc, "minQualityForDesc", QualityCategory.Normal);
            Scribe_Values.Look(ref ItemMaxDescriptionLength, "itemMaxDescriptionLength", 100);
            Scribe_Values.Look(ref MaxInventoryItems, "maxInventoryItems", 3);
            Scribe_Values.Look(ref MaxItemsWithDesc, "maxItemsWithDesc", 5);
            Scribe_Values.Look(ref SkipCommonItems, "skipCommonItems", true);
            Scribe_Values.Look(ref SkipArtDescription, "skipArtDescription", true);
            
            // Interaction
            Scribe_Values.Look(ref ShowInteractionDesc, "showInteractionDesc", true);
            Scribe_Values.Look(ref OnlyShowImportantBuildings, "onlyShowImportantBuildings", true);
            Scribe_Values.Look(ref InteractionMaxDescLength, "interactionMaxDescLength", 100);

            // Faction Relations
            Scribe_Values.Look(ref ShowFactionRelations, "showFactionRelations", true);
            Scribe_Values.Look(ref ShowFactionGoodwill, "showFactionGoodwill", true);
            Scribe_Values.Look(ref ShowFactionMemberCount, "showFactionMemberCount", true);
            Scribe_Values.Look(ref ShowIdentityBreakdown, "showIdentityBreakdown", true);
            Scribe_Values.Look(ref ShowGlobalSummary, "showGlobalSummary", false);
            Scribe_Values.Look(ref ShowNeutralFactions, "showNeutralFactions", true);
            Scribe_Values.Look(ref FilterByGoodwill, "filterByGoodwill", false);
            Scribe_Values.Look(ref MinGoodwillToShow, "minGoodwillToShow", -100);
            Scribe_Values.Look(ref FactionCacheUpdateInterval, "factionCacheUpdateInterval", 5f);

            // Announcements
            Scribe_Values.Look(ref ShowColonyAnnouncements, "showColonyAnnouncements", true);
            Scribe_Values.Look(ref ShowColonyOverview, "showColonyOverview", true);
            Scribe_Values.Look(ref ShowStructuredTasks, "showStructuredTasks", true);
            Scribe_Values.Look(ref OnlyShowActiveTasks, "onlyShowActiveTasks", true);
            Scribe_Values.Look(ref CompletedTaskShowDays, "completedTaskShowDays", 1f);
            Scribe_Values.Look(ref MaxOverviewLength, "maxOverviewLength", 500);

            // Auto Event Capture
            Scribe_Values.Look(ref EnableAutoEventCapture, "enableAutoEventCapture", true);
            Scribe_Values.Look(ref AutoCaptureQuests, "autoCaptureQuests", true);
            Scribe_Values.Look(ref AutoCaptureEvents, "autoCaptureEvents", true);
            Scribe_Values.Look(ref AutoCaptureResources, "autoCaptureResources", false);
            Scribe_Values.Look(ref AutoCompleteDays, "autoCompleteDays", 7);
            Scribe_Values.Look(ref EventExpireDays, "eventExpireDays", 1f);
            Scribe_Values.Look(ref MergeDuplicateEvents, "mergeDuplicateEvents", true);
            Scribe_Values.Look(ref AutoArchiveCompleted, "autoArchiveCompleted", false);
            Scribe_Values.Look(ref AutoCapturedDeleteDays, "autoCapturedDeleteDays", 0.5f);
            Scribe_Values.Look(ref AutoCompleteRaidEvents, "autoCompleteRaidEvents", true);
            Scribe_Values.Look(ref RaidCheckDelay, "raidCheckDelay", 5f);
            Scribe_Values.Look(ref AutoCompleteCaravanEvents, "autoCompleteCaravanEvents", true);
            
            // Environment Events
            Scribe_Values.Look(ref AutoCaptureWeather, "autoCaptureWeather", true);
            Scribe_Values.Look(ref AutoCaptureGameConditions, "autoCaptureGameConditions", true);
            Scribe_Values.Look(ref WeatherEventExpireHours, "weatherEventExpireHours", 6f);
            Scribe_Values.Look(ref GameConditionExpireHours, "gameConditionExpireHours", 24f);
            
            // Location Context
            Scribe_Values.Look(ref ShowRelativeLocation, "showRelativeLocation", true);
            Scribe_Values.Look(ref ShowAreaInfo, "showAreaInfo", true);
            Scribe_Values.Look(ref EnableTownCenterDetection, "enableTownCenterDetection", false);
            Scribe_Values.Look(ref TownCenterRadius, "townCenterRadius", 20);
            Scribe_Values.Look(ref ColonyCenterOffset, "colonyCenterOffset", IntVec3.Zero);
            
            Scribe_Values.Look(ref EnableGlobalLayout, "enableGlobalLayout", false);
            Scribe_Values.Look(ref MinRoomSize, "minRoomSize", 9);
            Scribe_Values.Look(ref MaxLayoutDistance, "maxLayoutDistance", 100);
            Scribe_Values.Look(ref IncludeCustomAreas, "includeCustomAreas", true);
            Scribe_Values.Look(ref GroupByDirection, "groupByDirection", true);
            Scribe_Values.Look(ref OnlyShowNamedRooms, "onlyShowNamedRooms", true);

            Scribe_Values.Look(ref UnlimitedRelations, "unlimitedRelations", false);
            Scribe_Values.Look(ref UnlimitedTraits, "unlimitedTraits", false);

            Scribe_Values.Look(ref EnableAISynthesis, "enableAISynthesis", false);
            Scribe_Values.Look(ref InjectSnapshotToContext, "injectSnapshotToContext", true);
            Scribe_Values.Look(ref SnapshotInjectionTarget, "snapshotInjectionTarget", SnapshotInjectionMode.Context);
            Scribe_Values.Look(ref SnapshotInjectDays, "snapshotInjectDays", 1.0f);
            Scribe_Values.Look(ref IncludeProjectsInSnapshot, "includeProjectsInSnapshot", true);
            Scribe_Values.Look(ref IncludeResearchInSnapshot, "includeResearchInSnapshot", false);
            Scribe_Values.Look(ref IncludeUnfinishedResearch, "includeUnfinishedResearch", false);
            Scribe_Values.Look(ref IncludePowerInSnapshot, "includePowerInSnapshot", false);
            Scribe_Values.Look(ref SynthesisProvider, "synthesisProvider", AIProvider.OpenAI);
            Scribe_Values.Look(ref CustomApiKey, "customApiKey", "");
            Scribe_Values.Look(ref CustomApiUrl, "customApiUrl", "");
            Scribe_Values.Look(ref CustomModelName, "customModelName", "gpt-4o-mini");
            Scribe_Values.Look(ref CustomOverviewSummaryPrompt, "customOverviewSummaryPrompt", "");
            Scribe_Values.Look(ref CustomDailySynthesisPrompt, "customDailySynthesisPrompt", "");
            Scribe_Values.Look(ref CustomProjectSummaryPrompt, "customProjectSummaryPrompt", "");
            
            Scribe_Collections.Look(ref EnabledEventTypes, "enabledEventTypes", LookMode.Value, LookMode.Value);
            if (EnabledEventTypes == null)
                EnabledEventTypes = new Dictionary<string, bool>();
        }

        private string GetDefaultUrl(AIProvider provider)
        {
            switch (provider)
            {
                case AIProvider.OpenAI: return "https://api.openai.com/v1/chat/completions";
                case AIProvider.Google: return "https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent";
                case AIProvider.DeepSeek: return "https://api.deepseek.com/v1/chat/completions";
                case AIProvider.Player2: return "https://api.player2.game/v1/chat/completions";
                default: return "";
            }
        }

        // ========================================
        // NEW: Reorganized Settings Pages
        // ========================================

        /// <summary>
        /// Helper method to draw a collapsible section header
        /// </summary>
        private bool DrawCollapsibleSection(Listing_Standard listing, string title, bool isExpanded, string icon = "▶")
        {
            Rect headerRect = listing.GetRect(40f);  // 增加高度从32到40
            
            // Background - 使用更浅的颜色
            Widgets.DrawBoxSolid(headerRect, new Color(0.3f, 0.35f, 0.4f, 0.8f));
            
            // Icon and title
            Text.Font = GameFont.Medium;
            string displayIcon = isExpanded ? "▼" : "▶";
            Widgets.Label(headerRect.LeftPart(0.95f).ContractedBy(6f), $"{displayIcon} {title}");
            Text.Font = GameFont.Small;
            
            // Click to toggle
            bool clicked = Widgets.ButtonInvisible(headerRect);
            if (Mouse.IsOver(headerRect))
            {
                Widgets.DrawHighlight(headerRect);
            }
            
            listing.Gap(6f);  // 增加间距
            return clicked ? !isExpanded : isExpanded;
        }

        /// <summary>
        /// Page 1: Context Enhancement (信息增强)
        /// Combines: Health, Items, Factions, Location
        /// </summary>
        public void DoContextEnhancementWindowContents(Rect inRect)
        {
            // 使用足够大的静态高度确保所有内容都能显示
            Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, 3500f);
            Widgets.BeginScrollView(inRect, ref _contextEnhancementScrollPosition, viewRect);

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(viewRect);

            // Page Title
            Text.Font = GameFont.Medium;
            GUI.color = new Color(0.8f, 1f, 0.8f);
            Widgets.Label(listing.GetRect(35f), "RTE_Settings_ContextEnhancement_PageTitle".Translate());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            listing.Gap(8f);

            // ========== 1. Health Section ==========
            _healthSectionExpanded = DrawCollapsibleSection(listing, "🏥 " + "RTE_Settings_Health_Title".Translate(), _healthSectionExpanded);
            if (_healthSectionExpanded)
            {
                listing.Gap(4f);
                DrawHealthSection(listing);
                listing.Gap(8f);
            }

            // ========== 2. Items Section ==========
            _itemsSectionExpanded = DrawCollapsibleSection(listing, "⚔️ " + "RTE_Settings_Items_Title".Translate(), _itemsSectionExpanded);
            if (_itemsSectionExpanded)
            {
                listing.Gap(4f);
                DrawItemsSection(listing);
                listing.Gap(8f);
            }

            // ========== 3. Factions Section ==========
            _factionsSectionExpanded = DrawCollapsibleSection(listing, "🤝 " + "RTE_Settings_Factions_Title".Translate(), _factionsSectionExpanded);
            if (_factionsSectionExpanded)
            {
                listing.Gap(4f);
                DrawFactionsSection(listing);
                listing.Gap(8f);
            }

            // ========== 4. Location Section ==========
            _locationSectionExpanded = DrawCollapsibleSection(listing, "🎯 " + "RTE_Settings_Location_Title".Translate(), _locationSectionExpanded);
            if (_locationSectionExpanded)
            {
                listing.Gap(4f);
                DrawLocationSection(listing);
                listing.Gap(8f);
            }

            // ========== 5. Misc Section ==========
            _miscSectionExpanded = DrawCollapsibleSection(listing, "🔧 " + "RTE_Settings_Misc_Title".Translate(), _miscSectionExpanded);
            if (_miscSectionExpanded)
            {
                listing.Gap(4f);
                DrawMiscSection(listing);
                listing.Gap(8f);
            }

            listing.End();
            Widgets.EndScrollView();
        }

        /// <summary>
        /// Page 2: Colony Status (殖民地状况板)
        /// Combines: Announcement, AutoCapture, AIHistorian
        /// </summary>
        public void DoColonyStatusWindowContents(Rect inRect)
        {
            // 使用足够大的静态高度确保所有内容都能显示
            Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, 5000f);
            Widgets.BeginScrollView(inRect, ref _colonyStatusScrollPosition, viewRect);

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(viewRect);

            // Page Title
            Text.Font = GameFont.Medium;
            GUI.color = new Color(1f, 0.9f, 0.6f);
            Widgets.Label(listing.GetRect(35f), "RTE_Settings_ColonyStatus_PageTitle".Translate());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            listing.Gap(8f);

            // ========== 1. Announcement Section ==========
            _announcementSectionExpanded = DrawCollapsibleSection(listing, "📋 " + "RTE_Settings_Announcement_Title".Translate(), _announcementSectionExpanded);
            if (_announcementSectionExpanded)
            {
                listing.Gap(4f);
                DrawAnnouncementSection(listing);
                listing.Gap(8f);
            }

            // ========== 2. Auto Capture Section ==========
            _autoCaptureSectionExpanded = DrawCollapsibleSection(listing, "⚡ " + "RTE_Settings_AutoCapture_Title".Translate(), _autoCaptureSectionExpanded);
            if (_autoCaptureSectionExpanded)
            {
                listing.Gap(4f);
                DrawAutoCaptureSection(listing);
                listing.Gap(8f);
            }

            // ========== 3. AI Historian Section ==========
            _aiHistorianSectionExpanded = DrawCollapsibleSection(listing, "🤖 " + "RTE_Settings_AI_Title".Translate(), _aiHistorianSectionExpanded);
            if (_aiHistorianSectionExpanded)
            {
                listing.Gap(4f);
                DrawAIHistorianSection(listing);
                listing.Gap(8f);
            }

            listing.End();
            Widgets.EndScrollView();
        }

        // ========================================
        // Section Drawing Methods
        // ========================================

        private void DrawHealthSection(Listing_Standard listing)
        {
            listing.CheckboxLabeled("RTE_Settings_Health_ShowSeverity".Translate(), ref ShowSeverity,
                "RTE_Settings_Health_ShowSeverity_Desc".Translate());
            listing.CheckboxLabeled("RTE_Settings_Health_ShowPainLevel".Translate(), ref ShowPainLevel,
                "RTE_Settings_Health_ShowPainLevel_Desc".Translate());
            listing.CheckboxLabeled("RTE_Settings_Health_ShowLethalMarker".Translate(), ref ShowLethalMarker,
                "RTE_Settings_Health_ShowLethalMarker_Desc".Translate());
            listing.CheckboxLabeled("RTE_Settings_Health_ShowDescription".Translate(), ref ShowDescription,
                "RTE_Settings_Health_ShowDescription_Desc".Translate());

            listing.Gap();
            listing.GapLine();
            listing.Gap();

            Widgets.Label(listing.GetRect(22f), "RTE_Settings_Health_MinPainThreshold".Translate(MinPainToShow.ToString("F2")));
            MinPainToShow = listing.Slider(MinPainToShow, 0f, 0.5f);
            listing.Gap(4f);

            Widgets.Label(listing.GetRect(22f), "RTE_Settings_Health_LethalThreshold".Translate(LethalThreshold.ToStringPercent()));
            LethalThreshold = listing.Slider(LethalThreshold, 0.5f, 1f);
            listing.Gap(4f);

            Widgets.Label(listing.GetRect(22f), "RTE_Settings_Health_MaxDescLength".Translate(MaxDescriptionLength));
            MaxDescriptionLength = (int)listing.Slider(MaxDescriptionLength, 50, 200);

            listing.Gap();
            listing.GapLine();
            listing.Gap();

            // ========== 分类过滤 ==========
            Text.Font = GameFont.Medium;
            GUI.color = new Color(0.8f, 0.9f, 1f);
            Widgets.Label(listing.GetRect(26f), "RTE_Settings_Health_FilterSection".Translate());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            listing.Gap(4f);

            // 仿生体/义肢
            listing.CheckboxLabeled("RTE_Settings_Health_ShowBionics".Translate(), ref ShowBionics,
                "RTE_Settings_Health_ShowBionics_Desc".Translate());
            if (ShowBionics)
            {
                Widgets.Label(listing.GetRect(22f), "RTE_Settings_Health_MaxToShow".Translate(MaxBionicsToShow == 0 ? "∞" : MaxBionicsToShow.ToString()));
                MaxBionicsToShow = (int)listing.Slider(MaxBionicsToShow, 0, 20);
            }

            // 其他植入物
            listing.CheckboxLabeled("RTE_Settings_Health_ShowImplants".Translate(), ref ShowImplants,
                "RTE_Settings_Health_ShowImplants_Desc".Translate());
            if (ShowImplants)
            {
                Widgets.Label(listing.GetRect(22f), "RTE_Settings_Health_MaxToShow".Translate(MaxImplantsToShow == 0 ? "∞" : MaxImplantsToShow.ToString()));
                MaxImplantsToShow = (int)listing.Slider(MaxImplantsToShow, 0, 20);
            }

            // 伤口
            listing.CheckboxLabeled("RTE_Settings_Health_ShowInjuries".Translate(), ref ShowInjuries,
                "RTE_Settings_Health_ShowInjuries_Desc".Translate());
            if (ShowInjuries)
            {
                Widgets.Label(listing.GetRect(22f), "RTE_Settings_Health_MaxToShow".Translate(MaxInjuriesToShow == 0 ? "∞" : MaxInjuriesToShow.ToString()));
                MaxInjuriesToShow = (int)listing.Slider(MaxInjuriesToShow, 0, 30);
            }

            // 缺失部位
            listing.CheckboxLabeled("RTE_Settings_Health_ShowMissingParts".Translate(), ref ShowMissingParts,
                "RTE_Settings_Health_ShowMissingParts_Desc".Translate());

            // 疾病/状态
            listing.CheckboxLabeled("RTE_Settings_Health_ShowConditions".Translate(), ref ShowConditions,
                "RTE_Settings_Health_ShowConditions_Desc".Translate());
            if (ShowConditions)
            {
                Widgets.Label(listing.GetRect(22f), "RTE_Settings_Health_MaxToShow".Translate(MaxConditionsToShow == 0 ? "∞" : MaxConditionsToShow.ToString()));
                MaxConditionsToShow = (int)listing.Slider(MaxConditionsToShow, 0, 20);
            }

            listing.Gap();
            listing.GapLine();
            listing.Gap();

            // ========== 智能整合 ==========
            Text.Font = GameFont.Medium;
            GUI.color = new Color(0.8f, 0.9f, 1f);
            Widgets.Label(listing.GetRect(26f), "RTE_Settings_Health_ConsolidationSection".Translate());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            listing.Gap(4f);

            listing.CheckboxLabeled("RTE_Settings_Health_EnableInjuryConsolidation".Translate(), ref EnableInjuryConsolidation,
                "RTE_Settings_Health_EnableInjuryConsolidation_Desc".Translate());
            
            if (EnableInjuryConsolidation)
            {
                Widgets.Label(listing.GetRect(22f), "RTE_Settings_Health_MinorInjuryThreshold".Translate(MinorInjurySeverityThreshold.ToStringPercent()));
                MinorInjurySeverityThreshold = listing.Slider(MinorInjurySeverityThreshold, 0.1f, 0.5f);
                
                Text.Font = GameFont.Tiny;
                GUI.color = Color.gray;
                Widgets.Label(listing.GetRect(18f), "RTE_Settings_Health_MinorInjuryThreshold_Desc".Translate());
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
            }

            listing.CheckboxLabeled("RTE_Settings_Health_EnableBionicSummary".Translate(), ref EnableBionicSummary,
                "RTE_Settings_Health_EnableBionicSummary_Desc".Translate());

            listing.Gap();
            listing.GapLine();
            listing.Gap();

            Text.Font = GameFont.Tiny;
            Widgets.Label(listing.GetRect(20f), "RTE_Settings_Health_Note1".Translate());
            Widgets.Label(listing.GetRect(20f), "RTE_Settings_Health_Note2".Translate());
            Widgets.Label(listing.GetRect(20f), "RTE_Settings_Health_Note3".Translate());
            Text.Font = GameFont.Small;
        }

        private void DrawItemsSection(Listing_Standard listing)
        {
            listing.CheckboxLabeled("RTE_Settings_Items_ShowEquipmentDesc".Translate(), ref ShowEquipmentDesc, "RTE_Settings_Items_ShowEquipmentDesc_Desc".Translate());
            listing.CheckboxLabeled("RTE_Settings_Items_ShowCarriedDesc".Translate(), ref ShowCarriedItemDesc, "RTE_Settings_Items_ShowCarriedDesc_Desc".Translate());
            listing.CheckboxLabeled("RTE_Settings_Items_ShowInventory".Translate(), ref ShowInventoryItems, "RTE_Settings_Items_ShowInventory_Desc".Translate());
            if (ShowInventoryItems)
            {
                listing.CheckboxLabeled("RTE_Settings_Items_ShowInventoryDesc".Translate(), ref ShowInventoryDesc, "RTE_Settings_Items_ShowInventoryDesc_Desc".Translate());
            }
            
            listing.Gap();
            listing.GapLine();
            listing.Gap();
            
            Text.Font = GameFont.Medium;
            Widgets.Label(listing.GetRect(30f), "RTE_Settings_Items_InteractionTitle".Translate());
            Text.Font = GameFont.Small;
            listing.Gap();
            
            listing.CheckboxLabeled("RTE_Settings_Items_ShowInteraction".Translate(), ref ShowInteractionDesc, "RTE_Settings_Items_ShowInteraction_Desc".Translate());
            if (ShowInteractionDesc)
            {
                listing.CheckboxLabeled("RTE_Settings_Items_OnlyImportantBuildings".Translate(), ref OnlyShowImportantBuildings, "RTE_Settings_Items_OnlyImportantBuildings_Desc".Translate());
                
                Widgets.Label(listing.GetRect(22f), "RTE_Settings_Items_InteractionMaxLength".Translate(InteractionMaxDescLength));
                InteractionMaxDescLength = (int)listing.Slider(InteractionMaxDescLength, 50, 200);
            }
            
            listing.Gap();
            listing.GapLine();
            listing.Gap();

            Rect qualityRect = listing.GetRect(30f);
            Widgets.Label(qualityRect.LeftHalf(), "RTE_Settings_Items_MinQuality".Translate());
            if (Widgets.ButtonText(qualityRect.RightHalf(), MinQualityForDesc.GetLabel()))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (QualityCategory quality in Enum.GetValues(typeof(QualityCategory)))
                {
                    options.Add(new FloatMenuOption(quality.GetLabel(), () => 
                    {
                        MinQualityForDesc = quality;
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
            listing.Gap(4f);

            Widgets.Label(listing.GetRect(22f), "RTE_Settings_Items_MaxDescLength".Translate(ItemMaxDescriptionLength));
            ItemMaxDescriptionLength = (int)listing.Slider(ItemMaxDescriptionLength, 50, 200);
            listing.Gap(4f);

            Widgets.Label(listing.GetRect(22f), "RTE_Settings_Items_MaxInventoryItems".Translate(MaxInventoryItems));
            MaxInventoryItems = (int)listing.Slider(MaxInventoryItems, 1, 10);
            listing.Gap(4f);

            Widgets.Label(listing.GetRect(22f), "RTE_Settings_Items_MaxItemsWithDesc".Translate(MaxItemsWithDesc));
            MaxItemsWithDesc = (int)listing.Slider(MaxItemsWithDesc, 1, 10);

            listing.Gap();
            listing.GapLine();
            listing.Gap();

            listing.CheckboxLabeled("RTE_Settings_Items_SkipCommon".Translate(), ref SkipCommonItems, "RTE_Settings_Items_SkipCommon_Desc".Translate());
            listing.CheckboxLabeled("RTE_Settings_Items_SkipArt".Translate(), ref SkipArtDescription,
                "RTE_Settings_Items_SkipArt_Desc".Translate());

            listing.Gap();
            listing.GapLine();
            listing.Gap();

            Text.Font = GameFont.Tiny;
            Widgets.Label(listing.GetRect(40f), 
                "提示：品质等级从低到高为：Awful < Poor < Normal < Good < Excellent < Masterwork < Legendary\n" +
                "建议设置为Normal或Good以平衡信息量和token消耗");
            Text.Font = GameFont.Small;
        }

        private void DrawFactionsSection(Listing_Standard listing)
        {
            listing.CheckboxLabeled("RTE_Settings_Factions_Enable".Translate(), ref ShowFactionRelations, 
                "RTE_Settings_Factions_Enable_Desc".Translate());
            
            if (ShowFactionRelations)
            {
                listing.Gap();
                
                listing.CheckboxLabeled("RTE_Settings_Factions_ShowGoodwill".Translate(), ref ShowFactionGoodwill, 
                    "RTE_Settings_Factions_ShowGoodwill_Desc".Translate());
                
                listing.CheckboxLabeled("RTE_Settings_Factions_ShowMemberCount".Translate(), ref ShowFactionMemberCount, 
                    "RTE_Settings_Factions_ShowMemberCount_Desc".Translate());
                
                listing.CheckboxLabeled("RTE_Settings_Factions_ShowIdentity".Translate(), ref ShowIdentityBreakdown,
                    "RTE_Settings_Factions_ShowIdentity_Desc".Translate());
                
                listing.CheckboxLabeled("RTE_Settings_Factions_ShowSummary".Translate(), ref ShowGlobalSummary,
                    "RTE_Settings_Factions_ShowSummary_Desc".Translate());
                
                listing.CheckboxLabeled("RTE_Settings_Factions_ShowNeutral".Translate(), ref ShowNeutralFactions, 
                    "RTE_Settings_Factions_ShowNeutral_Desc".Translate());
                
                listing.Gap();
                
                Widgets.Label(listing.GetRect(22f), "RTE_Settings_Factions_CacheInterval".Translate(FactionCacheUpdateInterval.ToString("F1")));
                FactionCacheUpdateInterval = listing.Slider(FactionCacheUpdateInterval, 1f, 30f);
                
                Text.Font = GameFont.Tiny;
                GUI.color = Color.gray;
                Widgets.Label(listing.GetRect(18f), "RTE_Settings_Factions_CacheInterval_Desc".Translate());
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
                
                listing.Gap();
                listing.GapLine();
                listing.Gap();
                
                listing.CheckboxLabeled("RTE_Settings_Factions_FilterByGoodwill".Translate(), ref FilterByGoodwill,
                    "RTE_Settings_Factions_FilterByGoodwill_Desc".Translate());
                
                if (FilterByGoodwill)
                {
                    Widgets.Label(listing.GetRect(22f), "RTE_Settings_Factions_MinGoodwill".Translate(MinGoodwillToShow));
                    MinGoodwillToShow = (int)listing.Slider(MinGoodwillToShow, -100, 100);
                    
                    Text.Font = GameFont.Tiny;
                    GUI.color = Color.gray;
                    Widgets.Label(listing.GetRect(18f), "RTE_Settings_Factions_MinGoodwill_Desc".Translate(MinGoodwillToShow));
                    GUI.color = Color.white;
                    Text.Font = GameFont.Small;
                }
            }

            listing.Gap();
            listing.GapLine();
            listing.Gap();

            Text.Font = GameFont.Tiny;
            Widgets.Label(listing.GetRect(80f), 
                "说明：\n" +
                "1. 此功能只显示当前地图上实际存在的派系（有成员在场）。\n" +
                "2. 使用游戏原生的关系状态\n" +
                "3. 信息会自动注入到 AI 的上下文中，让 AI 了解当前的外交状况。\n" +
                "4. 当地图上没有其他派系时，不会显示任何信息。");
            Text.Font = GameFont.Small;
        }

        private void DrawMiscSection(Listing_Standard listing)
        {
            listing.CheckboxLabeled("RTE_Settings_Misc_UnlimitedRelations".Translate(), ref UnlimitedRelations, 
                "RTE_Settings_Misc_UnlimitedRelations_Desc".Translate());
            
            listing.CheckboxLabeled("RTE_Settings_Misc_UnlimitedTraits".Translate(), ref UnlimitedTraits, 
                "RTE_Settings_Misc_UnlimitedTraits_Desc".Translate());

            listing.Gap();
            listing.GapLine();
            listing.Gap();

            Text.Font = GameFont.Tiny;
            Widgets.Label(listing.GetRect(60f), 
                "说明：\n" +
                "1. 解除关系数量限制：显示所有关系，不再受 RimTalk 设置中的 MaxPawnContextCount 限制。\n" +
                "2. 解除配角特质限制：在 Short 模式下显示所有特质，不再只显示前 3 个。\n" +
                "注意：开启这些选项可能会增加 Token 消耗。");
            Text.Font = GameFont.Small;
        }

        private void DrawLocationSection(Listing_Standard listing)
        {
            listing.CheckboxLabeled("RTE_Settings_Location_Enable".Translate(), ref ShowRelativeLocation, 
                "RTE_Settings_Location_Enable_Desc".Translate());

            if (ShowRelativeLocation)
            {
                listing.Gap();
                
                listing.CheckboxLabeled("RTE_Settings_Location_ShowArea".Translate(), ref ShowAreaInfo,
                    "RTE_Settings_Location_ShowArea_Desc".Translate());
                
                listing.Gap();
                
                listing.CheckboxLabeled("RTE_Settings_Location_TownCenter".Translate(), ref EnableTownCenterDetection,
                    "RTE_Settings_Location_TownCenter_Desc".Translate());
                
                if (EnableTownCenterDetection)
                {
                    Widgets.Label(listing.GetRect(22f), "RTE_Settings_Location_CenterRadius".Translate(TownCenterRadius));
                    TownCenterRadius = (int)listing.Slider(TownCenterRadius, 10, 50);
                    
                    Text.Font = GameFont.Tiny;
                    GUI.color = Color.gray;
                    Widgets.Label(listing.GetRect(18f), "RTE_Settings_Location_CenterRadius_Desc".Translate(TownCenterRadius));
                    GUI.color = Color.white;
                    Text.Font = GameFont.Small;
                }
            }

            listing.Gap();
            listing.GapLine();
            listing.Gap();

            Text.Font = GameFont.Tiny;
            Widgets.Label(listing.GetRect(100f), 
                "说明：\n" +
                "1. 系统会自动计算殖民地中心（基于居住区）。\n" +
                "2. 提供8方位判断（东、南、西、北及四个斜向）。\n" +
                "3. 区域类型：Town Center（核心）、Town（城镇）、Town Edge（边缘）、Wilderness（野外）。\n" +
                "4. 自动检测种植区、储存区等游戏原生区域。\n" +
                "5. 信息会自动注入到 AI 的上下文中，让 AI 了解 Pawn 的位置。");
            Text.Font = GameFont.Small;

            listing.Gap();
            listing.GapLine();
            listing.Gap();

            listing.Gap();
            listing.GapLine();
            listing.Gap();

            listing.CheckboxLabeled("RTE_Settings_Location_EnableGlobalLayout".Translate(), ref EnableGlobalLayout,
                "RTE_Settings_Location_EnableGlobalLayout_Desc".Translate());

            if (EnableGlobalLayout)
            {
                listing.Gap();
                
                Widgets.Label(listing.GetRect(22f), "RTE_Settings_Location_MinRoomSize".Translate(MinRoomSize));
                MinRoomSize = (int)listing.Slider(MinRoomSize, 4, 50);
                
                Widgets.Label(listing.GetRect(22f), "RTE_Settings_Location_MaxDistance".Translate(MaxLayoutDistance));
                MaxLayoutDistance = (int)listing.Slider(MaxLayoutDistance, 0, 300);
                
                listing.CheckboxLabeled("RTE_Settings_Location_OnlyNamedRooms".Translate(), ref OnlyShowNamedRooms,
                    "RTE_Settings_Location_OnlyNamedRooms_Desc".Translate());
                
                listing.CheckboxLabeled("RTE_Settings_Location_IncludeCustomAreas".Translate(), ref IncludeCustomAreas,
                    "RTE_Settings_Location_IncludeCustomAreas_Desc".Translate());
                
                listing.CheckboxLabeled("RTE_Settings_Location_GroupByDirection".Translate(), ref GroupByDirection,
                    "RTE_Settings_Location_GroupByDirection_Desc".Translate());
                
                Text.Font = GameFont.Tiny;
                GUI.color = new Color(1f, 0.8f, 0.6f);
                Widgets.Label(listing.GetRect(40f), "RTE_Settings_Location_GlobalLayout_Warning".Translate());
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
            }

            listing.Gap();
            listing.GapLine();
            listing.Gap();

            Text.Font = GameFont.Medium;
            Widgets.Label(listing.GetRect(30f), "RTE_Settings_Location_ExampleTitle".Translate());
            Text.Font = GameFont.Small;
            listing.Gap();

            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.8f, 1f, 0.8f);
            Widgets.Label(listing.GetRect(20f), "• In Bedroom, Northeast of colony (Town)");
            Widgets.Label(listing.GetRect(20f), "• Outdoors in Growing Zone, South of colony (Town Edge)");
            Widgets.Label(listing.GetRect(20f), "• Outdoors, North of colony (Wilderness)");
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        private void DrawAnnouncementSection(Listing_Standard listing)
        {
            listing.CheckboxLabeled("RTE_Settings_Announcement_Enable".Translate(), ref ShowColonyAnnouncements, "RTE_Settings_Announcement_Enable_Desc".Translate());
            
            if (ShowColonyAnnouncements)
            {
                listing.Gap();
                
                listing.CheckboxLabeled("RTE_Settings_Announcement_ShowOverview".Translate(), ref ShowColonyOverview, "RTE_Settings_Announcement_ShowOverview_Desc".Translate());
                if (ShowColonyOverview)
                {
                    Widgets.Label(listing.GetRect(22f), "RTE_Settings_Announcement_OverviewMaxLength".Translate(MaxOverviewLength));
                    MaxOverviewLength = (int)listing.Slider(MaxOverviewLength, 100, 2000);
                }
                
                listing.Gap();
                
                listing.CheckboxLabeled("RTE_Settings_Announcement_ShowTasks".Translate(), ref ShowStructuredTasks, "RTE_Settings_Announcement_ShowTasks_Desc".Translate());
                if (ShowStructuredTasks)
                {
                    listing.CheckboxLabeled("RTE_Settings_Announcement_OnlyActive".Translate(), ref OnlyShowActiveTasks, "RTE_Settings_Announcement_OnlyActive_Desc".Translate());
                    if (OnlyShowActiveTasks)
                    {
                        Widgets.Label(listing.GetRect(22f), "RTE_Settings_Announcement_CompletedDays".Translate(CompletedTaskShowDays.ToString("F1")));
                        CompletedTaskShowDays = listing.Slider(CompletedTaskShowDays, 0f, 7f);
                    }
                }
            }

            listing.Gap();
            listing.GapLine();
            listing.Gap();

            Text.Font = GameFont.Tiny;
            Widgets.Label(listing.GetRect(40f), "RTE_Settings_Announcement_Tip".Translate());
            Text.Font = GameFont.Small;
        }

        private void DrawAutoCaptureSection(Listing_Standard listing)
        {
            listing.CheckboxLabeled("RTE_Settings_AutoCapture_Enable".Translate(), ref EnableAutoEventCapture, "RTE_Settings_AutoCapture_Enable_Desc".Translate());

            if (EnableAutoEventCapture)
            {
                listing.Gap();
                
                Widgets.Label(listing.GetRect(24f), "RTE_Settings_AutoCapture_GeneralOptions".Translate());
                listing.CheckboxLabeled("RTE_Settings_AutoCapture_Quests".Translate(), ref AutoCaptureQuests);
                listing.CheckboxLabeled("RTE_Settings_AutoCapture_Events".Translate(), ref AutoCaptureEvents);
                listing.CheckboxLabeled("RTE_Settings_AutoCapture_Resources".Translate(), ref AutoCaptureResources);
                
                listing.Gap();
                
                Widgets.Label(listing.GetRect(22f), "RTE_Settings_AutoCapture_QuestExpire".Translate(AutoCompleteDays));
                AutoCompleteDays = (int)listing.Slider(AutoCompleteDays, 1, 30);
                
                Widgets.Label(listing.GetRect(22f), "RTE_Settings_AutoCapture_EventExpire".Translate(EventExpireDays.ToString("F1")));
                EventExpireDays = listing.Slider(EventExpireDays, 0.1f, 7f);
                
                listing.Gap();
                
                Widgets.Label(listing.GetRect(22f), "RTE_Settings_AutoCapture_DeleteDays".Translate(AutoCapturedDeleteDays.ToString("F1")));
                AutoCapturedDeleteDays = listing.Slider(AutoCapturedDeleteDays, 0f, 3f);
                
                Text.Font = GameFont.Tiny;
                GUI.color = Color.gray;
                if (AutoCapturedDeleteDays == 0f)
                {
                    Widgets.Label(listing.GetRect(18f), "RTE_Settings_AutoCapture_DeleteDays_Zero".Translate());
                }
                else
                {
                    Widgets.Label(listing.GetRect(18f), "RTE_Settings_AutoCapture_DeleteDays_Desc".Translate(AutoCapturedDeleteDays));
                }
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
                
                listing.Gap();
                
                listing.CheckboxLabeled("RTE_Settings_AutoCapture_MergeDuplicates".Translate(), ref MergeDuplicateEvents, "RTE_Settings_AutoCapture_MergeDuplicates_Desc".Translate());
                listing.CheckboxLabeled("RTE_Settings_AutoCapture_AutoArchive".Translate(), ref AutoArchiveCompleted, "RTE_Settings_AutoCapture_AutoArchive_Desc".Translate());

                listing.Gap();
                
                listing.CheckboxLabeled("RTE_Settings_AutoCapture_AutoCompleteRaid".Translate(), ref AutoCompleteRaidEvents, "RTE_Settings_AutoCapture_AutoCompleteRaid_Desc".Translate());
                if (AutoCompleteRaidEvents)
                {
                    Widgets.Label(listing.GetRect(22f), "RTE_Settings_AutoCapture_RaidDelay".Translate(RaidCheckDelay.ToString("F1")));
                    RaidCheckDelay = listing.Slider(RaidCheckDelay, 1f, 30f);
                }
                
                listing.CheckboxLabeled("RTE_Settings_AutoCapture_AutoCompleteCaravan".Translate(), ref AutoCompleteCaravanEvents, "RTE_Settings_AutoCapture_AutoCompleteCaravan_Desc".Translate());

                listing.Gap();
                listing.GapLine();
                listing.Gap();
                
                // === 环境事件捕获 ===
                Text.Font = GameFont.Medium;
                GUI.color = new Color(0.6f, 0.9f, 1f);
                Widgets.Label(listing.GetRect(26f), "RTE_Settings_AutoCapture_EnvironmentTitle".Translate());
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
                listing.Gap(4f);
                
                listing.CheckboxLabeled("RTE_Settings_AutoCapture_Weather".Translate(), ref AutoCaptureWeather,
                    "RTE_Settings_AutoCapture_Weather_Desc".Translate());
                
                if (AutoCaptureWeather)
                {
                    Widgets.Label(listing.GetRect(22f), "RTE_Settings_AutoCapture_WeatherExpire".Translate(WeatherEventExpireHours.ToString("F1")));
                    WeatherEventExpireHours = listing.Slider(WeatherEventExpireHours, 1f, 24f);
                }
                
                listing.CheckboxLabeled("RTE_Settings_AutoCapture_GameConditions".Translate(), ref AutoCaptureGameConditions,
                    "RTE_Settings_AutoCapture_GameConditions_Desc".Translate());
                
                if (AutoCaptureGameConditions)
                {
                    Widgets.Label(listing.GetRect(22f), "RTE_Settings_AutoCapture_ConditionExpire".Translate(GameConditionExpireHours.ToString("F1")));
                    GameConditionExpireHours = listing.Slider(GameConditionExpireHours, 6f, 72f);
                    
                    Text.Font = GameFont.Tiny;
                    GUI.color = Color.gray;
                    Widgets.Label(listing.GetRect(18f), "RTE_Settings_AutoCapture_ConditionExpire_Desc".Translate());
                    GUI.color = Color.white;
                    Text.Font = GameFont.Small;
                }

                listing.Gap();
                listing.GapLine();
                listing.Gap();

                if (DiscoveredEventTypes.Count > 0)
                {
                    Widgets.Label(listing.GetRect(24f), "RTE_Settings_AutoCapture_DiscoveredTypes".Translate(DiscoveredEventTypes.Count));
                    listing.Gap(6f);

                    var groupedTypes = DiscoveredEventTypes
                        .GroupBy(typeName =>
                        {
                            string simpleName = typeName.Contains(".")
                                ? typeName.Substring(typeName.LastIndexOf('.') + 1)
                                : typeName;

                            if (simpleName.Contains("Letter")) return "Letters (信件)";
                            if (simpleName.Contains("Message")) return "Messages (消息)";
                            return "Other (其他)";
                        })
                        .OrderBy(g => g.Key.StartsWith("Letters") ? 0 : g.Key.StartsWith("Messages") ? 1 : 2)
                        .ToList();

                    foreach (var group in groupedTypes)
                    {
                        Text.Font = GameFont.Small;
                        GUI.color = Color.yellow;
                        Widgets.Label(listing.GetRect(24f), $"━━ {group.Key} ({group.Count()}) ━━");
                        GUI.color = Color.white;
                        listing.Gap(4f);

                        foreach (var typeName in group.OrderBy(x => x))
                        {
                            bool isEnabled = !EnabledEventTypes.ContainsKey(typeName) || EnabledEventTypes[typeName];
                            bool newEnabled = isEnabled;
                            
                            string displayName = typeName.Contains(".") ? typeName.Substring(typeName.LastIndexOf('.') + 1) : typeName;
                            
                            if (typeName.Equals("Verse.Message", StringComparison.OrdinalIgnoreCase) && isEnabled)
                            {
                                GUI.color = new Color(1f, 0.5f, 0.5f);
                            }

                            listing.CheckboxLabeled(displayName, ref newEnabled, typeName);
                            GUI.color = Color.white;
                            
                            if (newEnabled != isEnabled)
                            {
                                EnabledEventTypes[typeName] = newEnabled;
                            }
                        }
                        listing.Gap(8f);
                    }
                }
                else
                {
                    GUI.color = Color.yellow;
                    Widgets.Label(listing.GetRect(24f), "RTE_Settings_AutoCapture_NoTypes".Translate());
                    GUI.color = Color.white;
                }

                listing.Gap(12f);
                if (listing.ButtonText("RTE_Settings_AutoCapture_ResetDefaults".Translate()))
                {
                    foreach (var typeName in DiscoveredEventTypes)
                    {
                        bool defaultEnabled = !typeName.Equals("Verse.Message", StringComparison.OrdinalIgnoreCase);
                        EnabledEventTypes[typeName] = defaultEnabled;
                    }
                }
            }
        }

        private void DrawAIHistorianSection(Listing_Standard listing)
        {
            listing.CheckboxLabeled("RTE_Settings_AI_Enable".Translate(), ref EnableAISynthesis, "RTE_Settings_AI_Enable_Desc".Translate());
            
            if (EnableAISynthesis)
            {
                listing.Gap();
                
                Rect providerRect = listing.GetRect(30f);
                Widgets.Label(providerRect.LeftHalf(), "RTE_Settings_AI_Provider".Translate());
                if (Widgets.ButtonText(providerRect.RightHalf(), SynthesisProvider.ToString()))
                {
                    List<FloatMenuOption> options = new List<FloatMenuOption>
                    {
                        new FloatMenuOption("OpenAI", () => { SynthesisProvider = AIProvider.OpenAI; CustomModelName = "gpt-4o-mini"; }),
                        new FloatMenuOption("Google (Gemini)", () => { SynthesisProvider = AIProvider.Google; CustomModelName = "gemini-2.5-flash"; }),
                        new FloatMenuOption("DeepSeek", () => { SynthesisProvider = AIProvider.DeepSeek; CustomModelName = "deepseek-chat"; }),
                        new FloatMenuOption("Player2", () => { SynthesisProvider = AIProvider.Player2; CustomModelName = ""; CustomApiKey = ""; }),
                        new FloatMenuOption("Custom (OpenAI Compatible)", () => SynthesisProvider = AIProvider.Custom)
                    };
                    Find.WindowStack.Add(new FloatMenu(options));
                }
                listing.Gap();
                
                Widgets.Label(listing.GetRect(24f), "RTE_Settings_AI_APIConfig".Translate());
                
                Widgets.Label(listing.GetRect(22f), "RTE_Settings_AI_APIKey".Translate());
                CustomApiKey = listing.TextEntry(CustomApiKey);
                
                string defaultUrl = GetDefaultUrl(SynthesisProvider);
                
                Widgets.Label(listing.GetRect(22f), "RTE_Settings_AI_APIURL".Translate(SynthesisProvider));
                CustomApiUrl = listing.TextEntry(CustomApiUrl);
                if (string.IsNullOrEmpty(CustomApiUrl) && !string.IsNullOrEmpty(defaultUrl))
                {
                    Text.Font = GameFont.Tiny;
                    GUI.color = Color.gray;
                    Widgets.Label(listing.GetRect(18f), "RTE_Settings_AI_DefaultURL".Translate(defaultUrl));
                    GUI.color = Color.white;
                    Text.Font = GameFont.Small;
                }
                
                Widgets.Label(listing.GetRect(22f), "RTE_Settings_AI_ModelName".Translate());
                CustomModelName = listing.TextEntry(CustomModelName);
                
                listing.Gap();
                
                if (listing.ButtonText("RTE_Settings_AI_TestConnection".Translate()))
                {
                    System.Threading.Tasks.Task.Run(async () => 
                    {
                        string result = await SimpleAIClient.CallAI("Hello, are you there?");
                        if (!string.IsNullOrEmpty(result))
                            Messages.Message("RTE_Settings_AI_TestSuccess".Translate(result), MessageTypeDefOf.PositiveEvent, false);
                        else
                            Messages.Message("RTE_Settings_AI_TestFailed".Translate(), MessageTypeDefOf.NegativeEvent, false);
                    });
                }
                
                listing.Gap();
                listing.GapLine();
                listing.Gap();
                
                Widgets.Label(listing.GetRect(24f), "RTE_Settings_AI_SnapshotContent".Translate());
                
                listing.CheckboxLabeled("RTE_Settings_AI_IncludeProjects".Translate(), ref IncludeProjectsInSnapshot,
                    "RTE_Settings_AI_IncludeProjects_Desc".Translate());
                
                listing.CheckboxLabeled("RTE_Settings_AI_IncludeResearch".Translate(), ref IncludeResearchInSnapshot,
                    "RTE_Settings_AI_IncludeResearch_Desc".Translate());
                
                if (IncludeResearchInSnapshot)
                {
                    listing.CheckboxLabeled("RTE_Settings_AI_IncludeUnfinished".Translate(), ref IncludeUnfinishedResearch,
                        "RTE_Settings_AI_IncludeUnfinished_Desc".Translate());
                }
                
                listing.CheckboxLabeled("RTE_Settings_AI_IncludePower".Translate(), ref IncludePowerInSnapshot,
                    "RTE_Settings_AI_IncludePower_Desc".Translate());
                
                listing.Gap();
                listing.GapLine();
                listing.Gap();
                
                Widgets.Label(listing.GetRect(24f), "RTE_Settings_AI_ContextInjection".Translate());
                
                listing.CheckboxLabeled("RTE_Settings_AI_InjectSnapshot".Translate(), ref InjectSnapshotToContext, 
                    "RTE_Settings_AI_InjectSnapshot_Desc".Translate());
                
                if (InjectSnapshotToContext)
                {
                    // 注入位置选择
                    Rect modeRect = listing.GetRect(30f);
                    Widgets.Label(modeRect.LeftHalf(), "RTE_Settings_AI_InjectionMode".Translate());
                    if (Widgets.ButtonText(modeRect.RightHalf(), 
                        SnapshotInjectionTarget == SnapshotInjectionMode.Context ? "RTE_Settings_AI_InjectionMode_Context".Translate() : "RTE_Settings_AI_InjectionMode_Prompt".Translate()))
                    {
                        List<FloatMenuOption> options = new List<FloatMenuOption>
                        {
                            new FloatMenuOption("RTE_Settings_AI_InjectionMode_Context".Translate(), () => SnapshotInjectionTarget = SnapshotInjectionMode.Context),
                            new FloatMenuOption("RTE_Settings_AI_InjectionMode_Prompt".Translate(), () => SnapshotInjectionTarget = SnapshotInjectionMode.Prompt)
                        };
                        Find.WindowStack.Add(new FloatMenu(options));
                    }
                    
                    // 说明文字
                    Text.Font = GameFont.Tiny;
                    GUI.color = Color.gray;
                    if (SnapshotInjectionTarget == SnapshotInjectionMode.Context)
                    {
                        Widgets.Label(listing.GetRect(36f), "RTE_Settings_AI_InjectionMode_Context_Desc".Translate());
                    }
                    else
                    {
                        Widgets.Label(listing.GetRect(36f), "RTE_Settings_AI_InjectionMode_Prompt_Desc".Translate());
                    }
                    GUI.color = Color.white;
                    Text.Font = GameFont.Small;

                    Widgets.Label(listing.GetRect(22f), "RTE_Settings_AI_InjectDays".Translate(SnapshotInjectDays.ToString("F1")));
                    SnapshotInjectDays = listing.Slider(SnapshotInjectDays, 0.5f, 7f);
                    
                    Text.Font = GameFont.Tiny;
                    GUI.color = Color.gray;
                    Widgets.Label(listing.GetRect(18f), "RTE_Settings_AI_InjectDays_Desc".Translate(SnapshotInjectDays.ToString("F1")));
                    GUI.color = Color.white;
                    Text.Font = GameFont.Small;
                }
                
                listing.Gap();
                listing.GapLine();
                listing.Gap();
                
                Text.Font = GameFont.Tiny;
                Widgets.Label(listing.GetRect(120f), 
                    "说明：\n" +
                    "1. 每日 0 点系统会自动拍摄殖民地快照（建筑、房间、蓝图）。\n" +
                    "2. AI 将对比昨日快照，结合玩家操作日志、工程进度、科技状态和事件，生成一段简短的总结。\n" +
                    "3. 总结结果将显示在'每日快照'标签页中，不会直接修改概况。\n" +
                    "4. 如果启用'自动注入'，AI 在对话时会自动看到最近的历史记录（含日期）。\n" +
                    "5. 工程信息：从状况板读取进行中和已完成的工程项目。\n" +
                    "6. 科技状态：包含当前研究、已完成科技，可选包含未完成科技（默认关闭以节省token）。");
                Text.Font = GameFont.Small;
            }
        }
    }
}
