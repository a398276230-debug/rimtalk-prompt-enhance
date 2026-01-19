using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace RimTalkHealthEnhance
{
    /// <summary>
    /// Settings for RimTalk Enhancement (Health & Items)
    /// 主设置类，聚合各个子设置模块
    /// </summary>
    public class HealthEnhanceSettings : ModSettings
    {
        // === 子设置模块 ===
        public HealthSettings Health = new HealthSettings();
        public ItemSettings Items = new ItemSettings();
        public FactionSettings Factions = new FactionSettings();
        public LocationSettings Location = new LocationSettings();
        public AnnouncementSettings Announcement = new AnnouncementSettings();
        public AutoCaptureSettings AutoCapture = new AutoCaptureSettings();
        public AIHistorianSettings AIHistorian = new AIHistorianSettings();
        public MiscSettings Misc = new MiscSettings();

        // === 向后兼容的属性代理 ===
        #region Health Settings Proxies
        public bool ShowSeverity { get => Health.ShowSeverity; set => Health.ShowSeverity = value; }
        public bool ShowPainLevel { get => Health.ShowPainLevel; set => Health.ShowPainLevel = value; }
        public bool ShowLethalMarker { get => Health.ShowLethalMarker; set => Health.ShowLethalMarker = value; }
        public bool ShowDescription { get => Health.ShowDescription; set => Health.ShowDescription = value; }
        public float MinPainToShow { get => Health.MinPainToShow; set => Health.MinPainToShow = value; }
        public float LethalThreshold { get => Health.LethalThreshold; set => Health.LethalThreshold = value; }
        public int MaxDescriptionLength { get => Health.MaxDescriptionLength; set => Health.MaxDescriptionLength = value; }
        public bool ShowBionics { get => Health.ShowBionics; set => Health.ShowBionics = value; }
        public bool ShowImplants { get => Health.ShowImplants; set => Health.ShowImplants = value; }
        public bool ShowInjuries { get => Health.ShowInjuries; set => Health.ShowInjuries = value; }
        public bool ShowMissingParts { get => Health.ShowMissingParts; set => Health.ShowMissingParts = value; }
        public bool ShowConditions { get => Health.ShowConditions; set => Health.ShowConditions = value; }
        public int MaxBionicsToShow { get => Health.MaxBionicsToShow; set => Health.MaxBionicsToShow = value; }
        public int MaxImplantsToShow { get => Health.MaxImplantsToShow; set => Health.MaxImplantsToShow = value; }
        public int MaxInjuriesToShow { get => Health.MaxInjuriesToShow; set => Health.MaxInjuriesToShow = value; }
        public int MaxConditionsToShow { get => Health.MaxConditionsToShow; set => Health.MaxConditionsToShow = value; }
        public bool EnableInjuryConsolidation { get => Health.EnableInjuryConsolidation; set => Health.EnableInjuryConsolidation = value; }
        public bool EnableBionicSummary { get => Health.EnableBionicSummary; set => Health.EnableBionicSummary = value; }
        public float MinorInjurySeverityThreshold { get => Health.MinorInjurySeverityThreshold; set => Health.MinorInjurySeverityThreshold = value; }
        #endregion

        #region Item Settings Proxies
        public bool ShowEquipmentDesc { get => Items.ShowEquipmentDesc; set => Items.ShowEquipmentDesc = value; }
        public bool ShowCarriedItemDesc { get => Items.ShowCarriedItemDesc; set => Items.ShowCarriedItemDesc = value; }
        public bool ShowInventoryItems { get => Items.ShowInventoryItems; set => Items.ShowInventoryItems = value; }
        public bool ShowInventoryDesc { get => Items.ShowInventoryDesc; set => Items.ShowInventoryDesc = value; }
        public QualityCategory MinQualityForDesc { get => Items.MinQualityForDesc; set => Items.MinQualityForDesc = value; }
        public int ItemMaxDescriptionLength { get => Items.ItemMaxDescriptionLength; set => Items.ItemMaxDescriptionLength = value; }
        public int MaxInventoryItems { get => Items.MaxInventoryItems; set => Items.MaxInventoryItems = value; }
        public int MaxItemsWithDesc { get => Items.MaxItemsWithDesc; set => Items.MaxItemsWithDesc = value; }
        public bool SkipCommonItems { get => Items.SkipCommonItems; set => Items.SkipCommonItems = value; }
        public bool SkipArtDescription { get => Items.SkipArtDescription; set => Items.SkipArtDescription = value; }
        public bool ShowInteractionDesc { get => Items.ShowInteractionDesc; set => Items.ShowInteractionDesc = value; }
        public bool OnlyShowImportantBuildings { get => Items.OnlyShowImportantBuildings; set => Items.OnlyShowImportantBuildings = value; }
        public int InteractionMaxDescLength { get => Items.InteractionMaxDescLength; set => Items.InteractionMaxDescLength = value; }
        #endregion

        #region Faction Settings Proxies
        public bool ShowFactionRelations { get => Factions.ShowFactionRelations; set => Factions.ShowFactionRelations = value; }
        public bool ShowFactionGoodwill { get => Factions.ShowFactionGoodwill; set => Factions.ShowFactionGoodwill = value; }
        public bool ShowFactionMemberCount { get => Factions.ShowFactionMemberCount; set => Factions.ShowFactionMemberCount = value; }
        public bool ShowIdentityBreakdown { get => Factions.ShowIdentityBreakdown; set => Factions.ShowIdentityBreakdown = value; }
        public bool ShowGlobalSummary { get => Factions.ShowGlobalSummary; set => Factions.ShowGlobalSummary = value; }
        public bool ShowNeutralFactions { get => Factions.ShowNeutralFactions; set => Factions.ShowNeutralFactions = value; }
        public bool FilterByGoodwill { get => Factions.FilterByGoodwill; set => Factions.FilterByGoodwill = value; }
        public int MinGoodwillToShow { get => Factions.MinGoodwillToShow; set => Factions.MinGoodwillToShow = value; }
        public float FactionCacheUpdateInterval { get => Factions.FactionCacheUpdateInterval; set => Factions.FactionCacheUpdateInterval = value; }
        #endregion

        #region Location Settings Proxies
        public bool ShowRelativeLocation { get => Location.ShowRelativeLocation; set => Location.ShowRelativeLocation = value; }
        public bool ShowAreaInfo { get => Location.ShowAreaInfo; set => Location.ShowAreaInfo = value; }
        public bool EnableTownCenterDetection { get => Location.EnableTownCenterDetection; set => Location.EnableTownCenterDetection = value; }
        public int TownCenterRadius { get => Location.TownCenterRadius; set => Location.TownCenterRadius = value; }
        public IntVec3 ColonyCenterOffset { get => Location.ColonyCenterOffset; set => Location.ColonyCenterOffset = value; }
        public bool EnableGlobalLayout { get => Location.EnableGlobalLayout; set => Location.EnableGlobalLayout = value; }
        public int MinRoomSize { get => Location.MinRoomSize; set => Location.MinRoomSize = value; }
        public int MaxLayoutDistance { get => Location.MaxLayoutDistance; set => Location.MaxLayoutDistance = value; }
        public bool IncludeCustomAreas { get => Location.IncludeCustomAreas; set => Location.IncludeCustomAreas = value; }
        public bool GroupByDirection { get => Location.GroupByDirection; set => Location.GroupByDirection = value; }
        public bool OnlyShowNamedRooms { get => Location.OnlyShowNamedRooms; set => Location.OnlyShowNamedRooms = value; }
        #endregion

        #region Announcement Settings Proxies
        public bool ShowColonyAnnouncements { get => Announcement.ShowColonyAnnouncements; set => Announcement.ShowColonyAnnouncements = value; }
        public bool ShowColonyOverview { get => Announcement.ShowColonyOverview; set => Announcement.ShowColonyOverview = value; }
        public bool ShowStructuredTasks { get => Announcement.ShowStructuredTasks; set => Announcement.ShowStructuredTasks = value; }
        public bool OnlyShowActiveTasks { get => Announcement.OnlyShowActiveTasks; set => Announcement.OnlyShowActiveTasks = value; }
        public float CompletedTaskShowDays { get => Announcement.CompletedTaskShowDays; set => Announcement.CompletedTaskShowDays = value; }
        public int MaxOverviewLength { get => Announcement.MaxOverviewLength; set => Announcement.MaxOverviewLength = value; }
        #endregion

        #region AutoCapture Settings Proxies
        public bool EnableAutoEventCapture { get => AutoCapture.EnableAutoEventCapture; set => AutoCapture.EnableAutoEventCapture = value; }
        public bool AutoCaptureQuests { get => AutoCapture.AutoCaptureQuests; set => AutoCapture.AutoCaptureQuests = value; }
        public bool AutoCaptureEvents { get => AutoCapture.AutoCaptureEvents; set => AutoCapture.AutoCaptureEvents = value; }
        public bool AutoCaptureResources { get => AutoCapture.AutoCaptureResources; set => AutoCapture.AutoCaptureResources = value; }
        public int AutoCompleteDays { get => AutoCapture.AutoCompleteDays; set => AutoCapture.AutoCompleteDays = value; }
        public float EventExpireDays { get => AutoCapture.EventExpireDays; set => AutoCapture.EventExpireDays = value; }
        public bool MergeDuplicateEvents { get => AutoCapture.MergeDuplicateEvents; set => AutoCapture.MergeDuplicateEvents = value; }
        public bool AutoArchiveCompleted { get => AutoCapture.AutoArchiveCompleted; set => AutoCapture.AutoArchiveCompleted = value; }
        public float AutoCapturedDeleteDays { get => AutoCapture.AutoCapturedDeleteDays; set => AutoCapture.AutoCapturedDeleteDays = value; }
        public bool AutoCompleteRaidEvents { get => AutoCapture.AutoCompleteRaidEvents; set => AutoCapture.AutoCompleteRaidEvents = value; }
        public float RaidCheckDelay { get => AutoCapture.RaidCheckDelay; set => AutoCapture.RaidCheckDelay = value; }
        public bool AutoCompleteCaravanEvents { get => AutoCapture.AutoCompleteCaravanEvents; set => AutoCapture.AutoCompleteCaravanEvents = value; }
        public bool AutoCaptureWeather { get => AutoCapture.AutoCaptureWeather; set => AutoCapture.AutoCaptureWeather = value; }
        public bool AutoCaptureGameConditions { get => AutoCapture.AutoCaptureGameConditions; set => AutoCapture.AutoCaptureGameConditions = value; }
        public float WeatherEventExpireHours { get => AutoCapture.WeatherEventExpireHours; set => AutoCapture.WeatherEventExpireHours = value; }
        public float GameConditionExpireHours { get => AutoCapture.GameConditionExpireHours; set => AutoCapture.GameConditionExpireHours = value; }
        public Dictionary<string, bool> EnabledEventTypes { get => AutoCapture.EnabledEventTypes; set => AutoCapture.EnabledEventTypes = value; }
        public static List<string> DiscoveredEventTypes { get => AutoCaptureSettings.DiscoveredEventTypes; set => AutoCaptureSettings.DiscoveredEventTypes = value; }
        #endregion

        #region AIHistorian Settings Proxies
        public bool EnableAISynthesis { get => AIHistorian.EnableAISynthesis; set => AIHistorian.EnableAISynthesis = value; }
        public bool InjectSnapshotToContext { get => AIHistorian.InjectSnapshotToContext; set => AIHistorian.InjectSnapshotToContext = value; }
        public float SnapshotInjectDays { get => AIHistorian.SnapshotInjectDays; set => AIHistorian.SnapshotInjectDays = value; }
        public bool IncludeProjectsInSnapshot { get => AIHistorian.IncludeProjectsInSnapshot; set => AIHistorian.IncludeProjectsInSnapshot = value; }
        public bool IncludeResearchInSnapshot { get => AIHistorian.IncludeResearchInSnapshot; set => AIHistorian.IncludeResearchInSnapshot = value; }
        public bool IncludeUnfinishedResearch { get => AIHistorian.IncludeUnfinishedResearch; set => AIHistorian.IncludeUnfinishedResearch = value; }
        public bool IncludePowerInSnapshot { get => AIHistorian.IncludePowerInSnapshot; set => AIHistorian.IncludePowerInSnapshot = value; }
        public AIProvider SynthesisProvider { get => AIHistorian.SynthesisProvider; set => AIHistorian.SynthesisProvider = value; }
        public string CustomApiKey { get => AIHistorian.CustomApiKey; set => AIHistorian.CustomApiKey = value; }
        public string CustomApiUrl { get => AIHistorian.CustomApiUrl; set => AIHistorian.CustomApiUrl = value; }
        public string CustomModelName { get => AIHistorian.CustomModelName; set => AIHistorian.CustomModelName = value; }
        public string CustomOverviewSummaryPrompt { get => AIHistorian.CustomOverviewSummaryPrompt; set => AIHistorian.CustomOverviewSummaryPrompt = value; }
        public string CustomDailySynthesisPrompt { get => AIHistorian.CustomDailySynthesisPrompt; set => AIHistorian.CustomDailySynthesisPrompt = value; }
        public string CustomProjectSummaryPrompt { get => AIHistorian.CustomProjectSummaryPrompt; set => AIHistorian.CustomProjectSummaryPrompt = value; }
        #endregion

        #region Misc Settings Proxies
        public bool UnlimitedRelations { get => Misc.UnlimitedRelations; set => Misc.UnlimitedRelations = value; }
        public bool UnlimitedTraits { get => Misc.UnlimitedTraits; set => Misc.UnlimitedTraits = value; }
        #endregion

        // === UI State (不保存) ===
        private Vector2 _contextEnhancementScrollPosition = Vector2.zero;
        private Vector2 _colonyStatusScrollPosition = Vector2.zero;
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
            
            // 保存/加载各个子设置模块
            Scribe_Deep.Look(ref Health, "healthSettings");
            Scribe_Deep.Look(ref Items, "itemSettings");
            Scribe_Deep.Look(ref Factions, "factionSettings");
            Scribe_Deep.Look(ref Location, "locationSettings");
            Scribe_Deep.Look(ref Announcement, "announcementSettings");
            Scribe_Deep.Look(ref AutoCapture, "autoCaptureSettings");
            Scribe_Deep.Look(ref AIHistorian, "aiHistorianSettings");
            Scribe_Deep.Look(ref Misc, "miscSettings");
            
            // 确保子模块不为 null
            Health ??= new HealthSettings();
            Items ??= new ItemSettings();
            Factions ??= new FactionSettings();
            Location ??= new LocationSettings();
            Announcement ??= new AnnouncementSettings();
            AutoCapture ??= new AutoCaptureSettings();
            AIHistorian ??= new AIHistorianSettings();
            Misc ??= new MiscSettings();
        }

        /// <summary>
        /// 绘制可折叠区域标题
        /// </summary>
        private bool DrawCollapsibleSection(Listing_Standard listing, string title, bool isExpanded)
        {
            Rect headerRect = listing.GetRect(40f);
            Widgets.DrawBoxSolid(headerRect, new Color(0.3f, 0.35f, 0.4f, 0.8f));
            
            Text.Font = GameFont.Medium;
            string displayIcon = isExpanded ? "▼" : "▶";
            Widgets.Label(headerRect.LeftPart(0.95f).ContractedBy(6f), $"{displayIcon} {title}");
            Text.Font = GameFont.Small;
            
            bool clicked = Widgets.ButtonInvisible(headerRect);
            if (Mouse.IsOver(headerRect))
            {
                Widgets.DrawHighlight(headerRect);
            }
            
            listing.Gap(6f);
            return clicked ? !isExpanded : isExpanded;
        }

        /// <summary>
        /// Page 1: Context Enhancement (信息增强)
        /// </summary>
        public void DoContextEnhancementWindowContents(Rect inRect)
        {
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

            // Health Section
            _healthSectionExpanded = DrawCollapsibleSection(listing, "🏥 " + "RTE_Settings_Health_Title".Translate(), _healthSectionExpanded);
            if (_healthSectionExpanded)
            {
                listing.Gap(4f);
                Health.DrawSettings(listing);
                listing.Gap(8f);
            }

            // Items Section
            _itemsSectionExpanded = DrawCollapsibleSection(listing, "⚔️ " + "RTE_Settings_Items_Title".Translate(), _itemsSectionExpanded);
            if (_itemsSectionExpanded)
            {
                listing.Gap(4f);
                Items.DrawSettings(listing);
                listing.Gap(8f);
            }

            // Factions Section
            _factionsSectionExpanded = DrawCollapsibleSection(listing, "🤝 " + "RTE_Settings_Factions_Title".Translate(), _factionsSectionExpanded);
            if (_factionsSectionExpanded)
            {
                listing.Gap(4f);
                Factions.DrawSettings(listing);
                listing.Gap(8f);
            }

            // Location Section
            _locationSectionExpanded = DrawCollapsibleSection(listing, "🎯 " + "RTE_Settings_Location_Title".Translate(), _locationSectionExpanded);
            if (_locationSectionExpanded)
            {
                listing.Gap(4f);
                Location.DrawSettings(listing);
                listing.Gap(8f);
            }

            // Misc Section
            _miscSectionExpanded = DrawCollapsibleSection(listing, "🔧 " + "RTE_Settings_Misc_Title".Translate(), _miscSectionExpanded);
            if (_miscSectionExpanded)
            {
                listing.Gap(4f);
                Misc.DrawSettings(listing);
                listing.Gap(8f);
            }

            listing.End();
            Widgets.EndScrollView();
        }

        /// <summary>
        /// Page 2: Colony Status (殖民地状况板)
        /// </summary>
        public void DoColonyStatusWindowContents(Rect inRect)
        {
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

            // Announcement Section
            _announcementSectionExpanded = DrawCollapsibleSection(listing, "📋 " + "RTE_Settings_Announcement_Title".Translate(), _announcementSectionExpanded);
            if (_announcementSectionExpanded)
            {
                listing.Gap(4f);
                Announcement.DrawSettings(listing);
                listing.Gap(8f);
            }

            // Auto Capture Section
            _autoCaptureSectionExpanded = DrawCollapsibleSection(listing, "⚡ " + "RTE_Settings_AutoCapture_Title".Translate(), _autoCaptureSectionExpanded);
            if (_autoCaptureSectionExpanded)
            {
                listing.Gap(4f);
                AutoCapture.DrawSettings(listing);
                listing.Gap(8f);
            }

            // AI Historian Section
            _aiHistorianSectionExpanded = DrawCollapsibleSection(listing, "🤖 " + "RTE_Settings_AI_Title".Translate(), _aiHistorianSectionExpanded);
            if (_aiHistorianSectionExpanded)
            {
                listing.Gap(4f);
                AIHistorian.DrawSettings(listing);
                listing.Gap(8f);
            }

            listing.End();
            Widgets.EndScrollView();
        }
    }
}
