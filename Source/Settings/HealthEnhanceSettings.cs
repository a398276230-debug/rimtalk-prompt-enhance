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
        Custom // OpenAI Compatible
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
        public bool ShowNeutralFactions = true;
        public bool FilterByGoodwill = false;
        public int MinGoodwillToShow = -100;

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
        
        // === AI Synthesis Settings ===
        public bool EnableAISynthesis = false;
        public bool InjectSnapshotToContext = true;      // 是否将快照注入到 AI context
        public float SnapshotInjectDays = 1.0f;          // 注入多少天的快照（0.5-7天）
        public AIProvider SynthesisProvider = AIProvider.OpenAI;
        public string CustomApiKey = "";
        public string CustomApiUrl = "";
        public string CustomModelName = "gpt-4o-mini";
        
        // 自定义提示词
        public string CustomOverviewSummaryPrompt = "";  // 概况总结提示词
        public string CustomDailySynthesisPrompt = "";   // 每日快照提示词
        
        // 存储每种事件类型的启用状态 (TypeName -> Enabled)
        public Dictionary<string, bool> EnabledEventTypes = new Dictionary<string, bool>();
        
        // 缓存发现的类型（不保存）
        public static List<string> DiscoveredEventTypes = new List<string>();

        // === Scroll Positions ===
        private Vector2 _healthScrollPosition = Vector2.zero;
        private Vector2 _itemScrollPosition = Vector2.zero;
        private Vector2 _factionScrollPosition = Vector2.zero;
        private Vector2 _announcementScrollPosition = Vector2.zero;
        private Vector2 _eventScrollPosition = Vector2.zero;
        private Vector2 _aiScrollPosition = Vector2.zero;

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
            Scribe_Values.Look(ref ShowNeutralFactions, "showNeutralFactions", true);
            Scribe_Values.Look(ref FilterByGoodwill, "filterByGoodwill", false);
            Scribe_Values.Look(ref MinGoodwillToShow, "minGoodwillToShow", -100);

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
            
            Scribe_Values.Look(ref EnableAISynthesis, "enableAISynthesis", false);
            Scribe_Values.Look(ref InjectSnapshotToContext, "injectSnapshotToContext", true);
            Scribe_Values.Look(ref SnapshotInjectDays, "snapshotInjectDays", 1.0f);
            Scribe_Values.Look(ref SynthesisProvider, "synthesisProvider", AIProvider.OpenAI);
            Scribe_Values.Look(ref CustomApiKey, "customApiKey", "");
            Scribe_Values.Look(ref CustomApiUrl, "customApiUrl", "");
            Scribe_Values.Look(ref CustomModelName, "customModelName", "gpt-4o-mini");
            Scribe_Values.Look(ref CustomOverviewSummaryPrompt, "customOverviewSummaryPrompt", "");
            Scribe_Values.Look(ref CustomDailySynthesisPrompt, "customDailySynthesisPrompt", "");
            
            Scribe_Collections.Look(ref EnabledEventTypes, "enabledEventTypes", LookMode.Value, LookMode.Value);
            if (EnabledEventTypes == null)
                EnabledEventTypes = new Dictionary<string, bool>();
        }

        public void DoHealthSettingsWindowContents(Rect inRect)
        {
            Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, 600f);
            Widgets.BeginScrollView(inRect, ref _healthScrollPosition, viewRect);

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(viewRect);

            // Header
            Text.Font = GameFont.Medium;
            Widgets.Label(listing.GetRect(30f), "RimTalk 增强健康信息设置");
            Text.Font = GameFont.Small;
            listing.Gap();

            // Display Options
            listing.CheckboxLabeled("显示严重度百分比", ref ShowSeverity, 
                "显示每个健康状况的严重程度百分比");
            listing.CheckboxLabeled("显示疼痛等级", ref ShowPainLevel, 
                "显示疼痛强度（轻微/中等/严重/极度）");
            listing.CheckboxLabeled("显示致命标记", ref ShowLethalMarker, 
                "标记可能致命的健康状况");
            listing.CheckboxLabeled("显示详细描述（仅完整模式）", ref ShowDescription, 
                "在完整信息级别下包含详细描述");

            listing.Gap();
            listing.GapLine();
            listing.Gap();

            // Thresholds
            Widgets.Label(listing.GetRect(22f), $"最小疼痛显示阈值: {MinPainToShow:F2}");
            MinPainToShow = listing.Slider(MinPainToShow, 0f, 0.5f);
            listing.Gap(4f);

            Widgets.Label(listing.GetRect(22f), $"致命标记阈值: {LethalThreshold:P0}");
            LethalThreshold = listing.Slider(LethalThreshold, 0.5f, 1f);
            listing.Gap(4f);

            Widgets.Label(listing.GetRect(22f), $"描述最大长度: {MaxDescriptionLength} 字符");
            MaxDescriptionLength = (int)listing.Slider(MaxDescriptionLength, 50, 200);

            listing.Gap();
            listing.GapLine();
            listing.Gap();

            // Info
            Text.Font = GameFont.Tiny;
            Widgets.Label(listing.GetRect(20f), "注意：需要在RimTalk设置中启用包含健康信息选项。");
            Widgets.Label(listing.GetRect(20f), "此mod增强RimTalk发送给AI的健康上下文信息。");
            Text.Font = GameFont.Small;

            listing.End();
            Widgets.EndScrollView();
        }

        public void DoItemSettingsWindowContents(Rect inRect)
        {
            Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, 1000f);
            Widgets.BeginScrollView(inRect, ref _itemScrollPosition, viewRect);

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(viewRect);

            Text.Font = GameFont.Medium;
            Widgets.Label(listing.GetRect(30f), "物品描述增强设置");
            Text.Font = GameFont.Small;
            listing.Gap();

            // === Display Options ===
            listing.CheckboxLabeled("显示装备描述（武器+服装）", ref ShowEquipmentDesc, "在装备列表中包含物品的详细描述");
            listing.CheckboxLabeled("显示携带物品描述", ref ShowCarriedItemDesc, "显示正在搬运或携带的物品描述");
            listing.CheckboxLabeled("显示背包物品列表", ref ShowInventoryItems, "列出背包中的物品（消耗tokens）");
            if (ShowInventoryItems)
            {
                listing.CheckboxLabeled("  └─ 显示背包物品描述", ref ShowInventoryDesc, "为背包物品添加详细描述（消耗更多tokens）");
            }
            
            listing.Gap();
            listing.GapLine();
            listing.Gap();
            
            // === Interaction Options ===
            Text.Font = GameFont.Medium;
            Widgets.Label(listing.GetRect(30f), "交互物品设置");
            Text.Font = GameFont.Small;
            listing.Gap();
            
            listing.CheckboxLabeled("显示正在交互的物品/建筑描述", ref ShowInteractionDesc, "例如：正在使用的研究台、工作台、床等");
            if (ShowInteractionDesc)
            {
                listing.CheckboxLabeled("  └─ 仅显示重要建筑", ref OnlyShowImportantBuildings, "只显示工作台、研究台等重要设施，忽略普通家具");
                
                Widgets.Label(listing.GetRect(22f), $"交互描述最大长度: {InteractionMaxDescLength} 字符");
                InteractionMaxDescLength = (int)listing.Slider(InteractionMaxDescLength, 50, 200);
            }
            
            listing.Gap();
            listing.GapLine();
            listing.Gap();

            // === Quality Threshold ===
            Rect qualityRect = listing.GetRect(30f);
            Widgets.Label(qualityRect.LeftHalf(), "显示描述的最低品质:");
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

            // === Token Control ===
            Widgets.Label(listing.GetRect(22f), $"物品描述最大长度: {ItemMaxDescriptionLength} 字符");
            ItemMaxDescriptionLength = (int)listing.Slider(ItemMaxDescriptionLength, 50, 200);
            listing.Gap(4f);

            Widgets.Label(listing.GetRect(22f), $"最多显示背包物品: {MaxInventoryItems} 件");
            MaxInventoryItems = (int)listing.Slider(MaxInventoryItems, 1, 10);
            listing.Gap(4f);

            Widgets.Label(listing.GetRect(22f), $"最多描述物品数: {MaxItemsWithDesc} 件");
            MaxItemsWithDesc = (int)listing.Slider(MaxItemsWithDesc, 1, 10);

            listing.Gap();
            listing.GapLine();
            listing.Gap();

            // === Smart Filtering ===
            listing.CheckboxLabeled("跳过常见物品", ref SkipCommonItems, "跳过原材料、食物、尸体等常见物品的描述");
            listing.CheckboxLabeled("跳过艺术描述（避免泰南语）", ref SkipArtDescription,
                "高品质物品的艺术描述通常是无意义的故事，勾选此项将只显示物品的功能性描述");

            listing.Gap();
            listing.GapLine();
            listing.Gap();

            // === Info ===
            Text.Font = GameFont.Tiny;
            Widgets.Label(listing.GetRect(40f), 
                "提示：品质等级从低到高为：Awful < Poor < Normal < Good < Excellent < Masterwork < Legendary\n" +
                "建议设置为Normal或Good以平衡信息量和token消耗");
            Text.Font = GameFont.Small;

            listing.End();
            Widgets.EndScrollView();
        }

        public void DoFactionSettingsWindowContents(Rect inRect)
        {
            Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, 600f);
            Widgets.BeginScrollView(inRect, ref _factionScrollPosition, viewRect);

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(viewRect);

            Text.Font = GameFont.Medium;
            Widgets.Label(listing.GetRect(30f), "派系关系设置");
            Text.Font = GameFont.Small;
            listing.Gap();

            listing.CheckboxLabeled("启用派系关系显示", ref ShowFactionRelations, 
                "显示当前地图上存在的其他派系及其与玩家的关系");
            
            if (ShowFactionRelations)
            {
                listing.Gap();
                
                listing.CheckboxLabeled("显示好感度数值", ref ShowFactionGoodwill, 
                    "显示每个派系对玩家的好感度（-100 到 100）");
                
                listing.CheckboxLabeled("显示派系成员数量", ref ShowFactionMemberCount, 
                    "显示该派系在当前地图上有多少成员");
                
                listing.CheckboxLabeled("显示中立派系", ref ShowNeutralFactions, 
                    "包含中立派系（好感度在 -75 到 75 之间）");
                
                listing.Gap();
                listing.GapLine();
                listing.Gap();
                
                listing.CheckboxLabeled("按好感度过滤", ref FilterByGoodwill, 
                    "只显示好感度高于指定阈值的派系");
                
                if (FilterByGoodwill)
                {
                    Widgets.Label(listing.GetRect(22f), $"  └─ 最低好感度: {MinGoodwillToShow}");
                    MinGoodwillToShow = (int)listing.Slider(MinGoodwillToShow, -100, 100);
                    
                    Text.Font = GameFont.Tiny;
                    GUI.color = Color.gray;
                    Widgets.Label(listing.GetRect(18f), $"      只显示好感度 ≥ {MinGoodwillToShow} 的派系");
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

            listing.End();
            Widgets.EndScrollView();
        }

        public void DoAnnouncementSettingsWindowContents(Rect inRect)
        {
            Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, 600f);
            Widgets.BeginScrollView(inRect, ref _announcementScrollPosition, viewRect);

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(viewRect);

            Text.Font = GameFont.Medium;
            Widgets.Label(listing.GetRect(30f), "通告系统设置");
            Text.Font = GameFont.Small;
            listing.Gap();

            listing.CheckboxLabeled("启用通告系统", ref ShowColonyAnnouncements, "允许AI读取殖民地通告板的内容");
            
            if (ShowColonyAnnouncements)
            {
                listing.Gap();
                
                listing.CheckboxLabeled("显示殖民地概况", ref ShowColonyOverview, "包含玩家编写的自由文本概况");
                if (ShowColonyOverview)
                {
                    Widgets.Label(listing.GetRect(22f), $"概况最大长度: {MaxOverviewLength} 字符");
                    MaxOverviewLength = (int)listing.Slider(MaxOverviewLength, 100, 2000);
                }
                
                listing.Gap();
                
                listing.CheckboxLabeled("显示结构化任务", ref ShowStructuredTasks, "包含任务列表中的任务");
                if (ShowStructuredTasks)
                {
                    listing.CheckboxLabeled("  └─ 仅显示进行中的任务", ref OnlyShowActiveTasks, "忽略已完成或暂停的任务");
                    if (OnlyShowActiveTasks)
                    {
                        Widgets.Label(listing.GetRect(22f), $"    └─ 已完成保留时间: {CompletedTaskShowDays:F1} 天");
                        CompletedTaskShowDays = listing.Slider(CompletedTaskShowDays, 0f, 7f);
                    }
                }
            }

            listing.Gap();
            listing.GapLine();
            listing.Gap();

            Text.Font = GameFont.Tiny;
            Widgets.Label(listing.GetRect(40f), "提示：通告板可以通过游戏底部的'通告'标签页打开。\n在这里你可以向AI传达任何关于殖民地的信息。");
            Text.Font = GameFont.Small;

            listing.End();
            Widgets.EndScrollView();
        }

        public void DoAISettingsWindowContents(Rect inRect)
        {
            Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, 600f);
            Widgets.BeginScrollView(inRect, ref _aiScrollPosition, viewRect);

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(viewRect);

            Text.Font = GameFont.Medium;
            Widgets.Label(listing.GetRect(30f), "AI 史官设置");
            Text.Font = GameFont.Small;
            listing.Gap();

            listing.CheckboxLabeled("启用 AI 每日总结", ref EnableAISynthesis, "每日 0 点自动生成殖民地发展快照和总结");
            
            if (EnableAISynthesis)
            {
                listing.Gap();
                
                // Provider Selection
                Rect providerRect = listing.GetRect(30f);
                Widgets.Label(providerRect.LeftHalf(), "AI 提供商:");
                if (Widgets.ButtonText(providerRect.RightHalf(), SynthesisProvider.ToString()))
                {
                    List<FloatMenuOption> options = new List<FloatMenuOption>
                    {
                        new FloatMenuOption("OpenAI", () => { SynthesisProvider = AIProvider.OpenAI; CustomModelName = "gpt-4o-mini"; }),
                        new FloatMenuOption("Google (Gemini)", () => { SynthesisProvider = AIProvider.Google; CustomModelName = "gemini-pro"; }),
                        new FloatMenuOption("DeepSeek", () => { SynthesisProvider = AIProvider.DeepSeek; CustomModelName = "deepseek-chat"; }),
                        new FloatMenuOption("Custom (OpenAI Compatible)", () => SynthesisProvider = AIProvider.Custom)
                    };
                    Find.WindowStack.Add(new FloatMenu(options));
                }
                listing.Gap();
                
                Widgets.Label(listing.GetRect(24f), "API 配置");
                
                Widgets.Label(listing.GetRect(22f), "API Key:");
                CustomApiKey = listing.TextEntry(CustomApiKey);
                
                string defaultUrl = GetDefaultUrl(SynthesisProvider);
                
                Widgets.Label(listing.GetRect(22f), $"API URL (可选，默认 {SynthesisProvider}):");
                CustomApiUrl = listing.TextEntry(CustomApiUrl);
                if (string.IsNullOrEmpty(CustomApiUrl) && !string.IsNullOrEmpty(defaultUrl))
                {
                    Text.Font = GameFont.Tiny;
                    GUI.color = Color.gray;
                    Widgets.Label(listing.GetRect(18f), $"默认: {defaultUrl}");
                    GUI.color = Color.white;
                    Text.Font = GameFont.Small;
                }
                
                Widgets.Label(listing.GetRect(22f), "模型名称:");
                CustomModelName = listing.TextEntry(CustomModelName);
                
                listing.Gap();
                
                if (listing.ButtonText("测试连接"))
                {
                    // 简单的测试调用
                    System.Threading.Tasks.Task.Run(async () => 
                    {
                        string result = await SimpleAIClient.CallAI("Hello, are you there?");
                        if (!string.IsNullOrEmpty(result))
                            Messages.Message("连接成功！AI 回复: " + result, MessageTypeDefOf.PositiveEvent, false);
                        else
                            Messages.Message("连接失败，请检查日志。", MessageTypeDefOf.NegativeEvent, false);
                    });
                }
                
                listing.Gap();
                listing.GapLine();
                listing.Gap();
                
                // Context Injection Settings
                Widgets.Label(listing.GetRect(24f), "上下文注入设置");
                
                listing.CheckboxLabeled("自动注入快照到 AI 对话", ref InjectSnapshotToContext, 
                    "启用后，AI 在对话时会自动看到最近的历史快照总结");
                
                if (InjectSnapshotToContext)
                {
                    Widgets.Label(listing.GetRect(22f), $"  └─ 注入天数: {SnapshotInjectDays:F1} 天");
                    SnapshotInjectDays = listing.Slider(SnapshotInjectDays, 0.5f, 7f);
                    
                    Text.Font = GameFont.Tiny;
                    GUI.color = Color.gray;
                    Widgets.Label(listing.GetRect(18f), $"      当前将注入最近 {SnapshotInjectDays:F1} 天的快照总结");
                    GUI.color = Color.white;
                    Text.Font = GameFont.Small;
                }
                
                listing.Gap();
                listing.GapLine();
                listing.Gap();
                
                Text.Font = GameFont.Tiny;
                Widgets.Label(listing.GetRect(80f), 
                    "说明：\n" +
                    "1. 每日 0 点系统会自动拍摄殖民地快照（建筑、房间、蓝图）。\n" +
                    "2. AI 将对比昨日快照，结合玩家操作日志和事件，生成一段简短的总结。\n" +
                    "3. 总结结果将显示在'每日快照'标签页中，不会直接修改概况。\n" +
                    "4. 如果启用'自动注入'，AI 在对话时会自动看到最近的历史记录（含日期）。");
                Text.Font = GameFont.Small;
            }

            listing.End();
            Widgets.EndScrollView();
        }

        private string GetDefaultUrl(AIProvider provider)
        {
            switch (provider)
            {
                case AIProvider.OpenAI: return "https://api.openai.com/v1/chat/completions";
                case AIProvider.Google: return "https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent";
                case AIProvider.DeepSeek: return "https://api.deepseek.com/v1/chat/completions";
                default: return "";
            }
        }

        public void DoEventSettingsWindowContents(Rect inRect)
        {
            Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, 1000f);
            Widgets.BeginScrollView(inRect, ref _eventScrollPosition, viewRect);

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(viewRect);

            Text.Font = GameFont.Medium;
            Widgets.Label(listing.GetRect(30f), "自动事件捕获设置");
            Text.Font = GameFont.Small;
            listing.Gap();

            listing.CheckboxLabeled("启用自动事件捕获", ref EnableAutoEventCapture, "自动将游戏事件添加到状况板");

            if (EnableAutoEventCapture)
            {
                listing.Gap();
                
                // General Options
                Widgets.Label(listing.GetRect(24f), "通用选项");
                listing.CheckboxLabeled("捕获任务 (Quests)", ref AutoCaptureQuests);
                listing.CheckboxLabeled("捕获事件 (Events)", ref AutoCaptureEvents);
                listing.CheckboxLabeled("捕获资源发现 (Resources)", ref AutoCaptureResources);
                
                listing.Gap();
                
                Widgets.Label(listing.GetRect(22f), $"任务自动过期: {AutoCompleteDays} 天");
                AutoCompleteDays = (int)listing.Slider(AutoCompleteDays, 1, 30);
                
                Widgets.Label(listing.GetRect(22f), $"普通事件过期: {EventExpireDays:F1} 天");
                EventExpireDays = listing.Slider(EventExpireDays, 0.1f, 7f);
                
                listing.Gap();
                
                Widgets.Label(listing.GetRect(22f), $"自动捕获事件完成后删除: {AutoCapturedDeleteDays:F1} 天");
                AutoCapturedDeleteDays = listing.Slider(AutoCapturedDeleteDays, 0f, 3f);
                
                Text.Font = GameFont.Tiny;
                GUI.color = Color.gray;
                if (AutoCapturedDeleteDays == 0f)
                {
                    Widgets.Label(listing.GetRect(18f), "    设为 0 天表示立即删除已完成的自动捕获事件");
                }
                else
                {
                    Widgets.Label(listing.GetRect(18f), $"    自动捕获的事件完成后将在 {AutoCapturedDeleteDays:F1} 天后自动删除");
                }
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
                
                listing.Gap();
                
                listing.CheckboxLabeled("合并重复事件", ref MergeDuplicateEvents, "如果标题相同，合并为一条并增加计数");
                listing.CheckboxLabeled("自动归档手动创建的已完成项", ref AutoArchiveCompleted, "定期清理手动创建的已完成条目（1天后）");

                listing.Gap();
                listing.GapLine();
                listing.Gap();

                // Event Types List
                if (DiscoveredEventTypes.Count > 0)
                {
                    Widgets.Label(listing.GetRect(24f), $"已发现的事件类型 ({DiscoveredEventTypes.Count})");
                    listing.Gap(6f);

                    // Group types by category
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
                        // Category header
                        Text.Font = GameFont.Small;
                        GUI.color = Color.yellow;
                        Widgets.Label(listing.GetRect(24f), $"━━ {group.Key} ({group.Count()}) ━━");
                        GUI.color = Color.white;
                        listing.Gap(4f);

                        foreach (var typeName in group.OrderBy(x => x))
                        {
                            bool isEnabled = !EnabledEventTypes.ContainsKey(typeName) || EnabledEventTypes[typeName];
                            bool newEnabled = isEnabled;
                            
                            // Simple name for display
                            string displayName = typeName.Contains(".") ? typeName.Substring(typeName.LastIndexOf('.') + 1) : typeName;
                            
                            // Highlight Verse.Message in red if enabled (usually spammy)
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
                    Widgets.Label(listing.GetRect(24f), "未发现事件类型。请进入游戏加载存档后刷新。");
                    GUI.color = Color.white;
                }

                listing.Gap(12f);
                if (listing.ButtonText("重置为默认"))
                {
                    foreach (var typeName in DiscoveredEventTypes)
                    {
                        // Enable by default for most types, but disable Verse.Message specifically
                        bool defaultEnabled = !typeName.Equals("Verse.Message", StringComparison.OrdinalIgnoreCase);
                        EnabledEventTypes[typeName] = defaultEnabled;
                    }
                }
            }

            listing.End();
            Widgets.EndScrollView();
        }
    }
}
