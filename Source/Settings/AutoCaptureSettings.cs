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
        // 存储每种事件类型的启用状态 (C# 类型名或 defName -> Enabled)
        // 与 RimTalk 的 EnabledArchivableTypes 播报过滤完全解耦（自建事件池）
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

                // === 事件类型过滤（层级树，与 RimTalk 播报过滤解耦） ===
                ArchivableTypeScanner.ScanIfNeeded();

                Text.Font = GameFont.Medium;
                GUI.color = new Color(0.6f, 0.9f, 1f);
                Widgets.Label(listing.GetRect(26f), "RTE_Settings_AutoCapture_TypeFilterTitle".Translate());
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
                listing.Gap(4f);

                Text.Font = GameFont.Tiny;
                GUI.color = Color.cyan;
                Widgets.Label(listing.GetRect(30f), "RTE_Settings_AutoCapture_TypeFilterTip".Translate());
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
                listing.Gap(4f);

                DrawTypeFilterTree(listing);

                listing.Gap(6f);

                if (listing.ButtonText("RTE_Settings_AutoCapture_ResetDefaults".Translate()))
                {
                    var messageTypes = ArchivableTypeScanner.GetMessageTypes();
                    foreach (var typeName in AutoCaptureSettings.DiscoveredEventTypes)
                    {
                        EnabledEventTypes[typeName] = !messageTypes.Contains(typeName);
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

        /// <summary>
        /// 绘制事件类型过滤层级树（父 = C# 类型名，子 = defName）。
        /// 机制参考上游 RimTalk Settings_EventFilter：父开关联动子项、子项开启强制父开启、
        /// Verse.Message 排最后、来源 mod 名标注。数据读写本项目自建的 EnabledEventTypes。
        /// </summary>
        private void DrawTypeFilterTree(Listing_Standard listing)
        {
            var hierarchy = ArchivableTypeScanner.TypeHierarchy;
            var sourceMap = ArchivableTypeScanner.SourceMap;

            if (hierarchy.Count == 0)
            {
                GUI.color = Color.yellow;
                Widgets.Label(listing.GetRect(24f), "RTE_Settings_AutoCapture_NoTypes".Translate());
                GUI.color = Color.white;
                return;
            }

            var sortedParents = hierarchy.Keys
                .OrderBy(k => k.Equals(ArchivableTypeScanner.VerseMessage, StringComparison.OrdinalIgnoreCase) ? 1 : 0)
                .ThenByDescending(k => hierarchy[k].Count)
                .ThenBy(k => k)
                .ToList();

            foreach (var parentKey in sortedParents)
            {
                var children = hierarchy[parentKey];
                bool hasChildren = children.Any();
                bool showExpander = hasChildren && children.Count > 1;
                bool isExpanded = ArchivableTypeScanner.ExpandedParents.Contains(parentKey);

                // --- 父行 ---
                Rect parentRect = listing.GetRect(24f);
                float xOffset = 0f;

                // 1. 展开按钮
                if (showExpander)
                {
                    Rect expanderRect = new Rect(parentRect.x, parentRect.y, 24f, 24f);
                    string label = isExpanded ? "[-]" : "[+]";
                    if (Widgets.ButtonText(expanderRect, label, drawBackground: false))
                    {
                        if (isExpanded) ArchivableTypeScanner.ExpandedParents.Remove(parentKey);
                        else ArchivableTypeScanner.ExpandedParents.Add(parentKey);
                    }
                }

                xOffset += 28f;

                // 2. 父复选框 + 标签
                bool isParentEnabled = EnabledEventTypes.TryGetValue(parentKey, out var pVal) && pVal;
                bool newParentEnabled = isParentEnabled;

                Rect checkboxRect = new Rect(parentRect.x + xOffset, parentRect.y, parentRect.width - xOffset, 24f);
                Widgets.CheckboxLabeled(checkboxRect, parentKey, ref newParentEnabled);

                // 3. 来源 mod 名标注
                DrawSourceTag(checkboxRect, parentKey, sourceMap);

                if (newParentEnabled != isParentEnabled)
                {
                    EnabledEventTypes[parentKey] = newParentEnabled;
                    // 父开关联动子项
                    if (hasChildren)
                    {
                        foreach (var child in children)
                            EnabledEventTypes[child] = newParentEnabled;
                    }
                }

                // --- 子行 ---
                if (!showExpander || !isExpanded) continue;

                foreach (var childKey in children)
                {
                    Rect childRect = listing.GetRect(24f);
                    childRect.xMin += 40f; // 缩进

                    bool isChildEnabled = EnabledEventTypes.TryGetValue(childKey, out var cVal) && cVal;
                    bool newChildEnabled = isChildEnabled;

                    Widgets.CheckboxLabeled(childRect, childKey, ref newChildEnabled);

                    DrawSourceTag(childRect, childKey, sourceMap);

                    if (newChildEnabled != isChildEnabled)
                    {
                        EnabledEventTypes[childKey] = newChildEnabled;
                        // 子项开启 -> 强制父开启
                        if (newChildEnabled && !EnabledEventTypes[parentKey])
                        {
                            EnabledEventTypes[parentKey] = true;
                        }
                    }
                }
            }
        }

        /// <summary>在复选框行右侧绘制灰色小字来源标注（Core 不显示）</summary>
        private static void DrawSourceTag(Rect rowRect, string key, IReadOnlyDictionary<string, string> sourceMap)
        {
            if (sourceMap.TryGetValue(key, out var source) &&
                !string.IsNullOrEmpty(source) &&
                source != ArchivableTypeScanner.Core)
            {
                float nameWidth = Text.CalcSize(key).x;
                Text.Font = GameFont.Tiny;
                GUI.color = Color.gray;
                Rect sourceRect = new Rect(rowRect.x + nameWidth + 10f, rowRect.y + 2f, 300f, 24f);
                Widgets.Label(sourceRect, $"({source})");
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
            }
        }
    }
}