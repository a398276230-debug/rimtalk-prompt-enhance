using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimTalkHealthEnhance
{
    /// <summary>
    /// 自动事件捕获相关设置
    /// </summary>
    public class AutoCaptureSettings : IExposable
    {
        // === 基础捕获设置 ===
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
        
        // === 事件类型过滤 ===
        // 存储每种事件类型的启用状态 (TypeName -> Enabled)
        public Dictionary<string, bool> EnabledEventTypes = new Dictionary<string, bool>();
        
        // 缓存发现的类型（不保存）
        public static List<string> DiscoveredEventTypes = new List<string>();

        public void ExposeData()
        {
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
            
            Scribe_Values.Look(ref AutoCaptureWeather, "autoCaptureWeather", true);
            Scribe_Values.Look(ref AutoCaptureGameConditions, "autoCaptureGameConditions", true);
            Scribe_Values.Look(ref WeatherEventExpireHours, "weatherEventExpireHours", 6f);
            Scribe_Values.Look(ref GameConditionExpireHours, "gameConditionExpireHours", 24f);
            
            Scribe_Collections.Look(ref EnabledEventTypes, "enabledEventTypes", LookMode.Value, LookMode.Value);
            if (EnabledEventTypes == null)
                EnabledEventTypes = new Dictionary<string, bool>();
        }

        /// <summary>
        /// 绘制自动捕获设置 UI
        /// </summary>
        public void DrawSettings(Listing_Standard listing)
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
            
            // === 强制重置按钮（始终显示，不依赖 EnableAutoEventCapture） ===
            listing.Gap();
            listing.GapLine();
            listing.Gap();
            
            Text.Font = GameFont.Medium;
            GUI.color = new Color(1f, 0.7f, 0.3f);
            Widgets.Label(listing.GetRect(26f), "RTE_Settings_AutoCapture_MaintenanceTitle".Translate());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            listing.Gap(4f);
            
            Text.Font = GameFont.Tiny;
            GUI.color = Color.gray;
            Widgets.Label(listing.GetRect(36f), "RTE_Settings_AutoCapture_ForceReset_Desc".Translate());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            listing.Gap(4f);
            
            if (listing.ButtonText("RTE_Settings_AutoCapture_ForceReset".Translate()))
            {
                if (Current.Game != null)
                {
                    var manager = ColonyAnnouncementManager.Instance;
                    if (manager != null)
                    {
                        string report = manager.ForceResetEventDeadlines();
                        Messages.Message("RTE_Settings_AutoCapture_ForceReset_Result".Translate(report), MessageTypeDefOf.TaskCompletion, false);
                    }
                    else
                    {
                        Messages.Message("RTE_Settings_AutoCapture_ForceReset_NoManager".Translate(), MessageTypeDefOf.RejectInput, false);
                    }
                }
                else
                {
                    Messages.Message("RTE_Settings_AutoCapture_ForceReset_NoGame".Translate(), MessageTypeDefOf.RejectInput, false);
                }
            }
        }
    }
}