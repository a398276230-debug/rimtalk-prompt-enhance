using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RimTalkHealthEnhance
{
    /// <summary>
    /// Patches GameConditionManager.RegisterCondition to capture game condition start events
    /// (Heat Wave, Cold Snap, Toxic Fallout, Eclipse, etc.)
    /// </summary>
    [HarmonyPatch(typeof(GameConditionManager), nameof(GameConditionManager.RegisterCondition))]
    public static class GameConditionRegisterPatch
    {
        public static void Postfix(GameConditionManager __instance, GameCondition cond)
        {
            if (cond == null || cond.def == null) return;

            try
            {
                var settings = RimTalkHealthEnhanceMod.Settings;
                if (!settings.EnableAutoEventCapture || !settings.AutoCaptureGameConditions) return;

                // 只捕获玩家地图上的状况
                Map map = __instance.ownerMap;
                if (map != null && map != Find.CurrentMap) return;

                // 跳过不显示在UI上的状况
                if (!cond.def.displayOnUI) return;

                // 创建状况事件
                string title = GetConditionTitle(cond, true);
                string description = GetConditionDescription(cond, map);

                var announcement = new ColonyAnnouncement
                {
                    Id = Guid.NewGuid().ToString(),
                    Category = AnnouncementCategory.Event,
                    Title = title,
                    Description = description,
                    Priority = GetConditionPriority(cond),
                    Status = AnnouncementStatus.Active,
                    CreatedTick = Find.TickManager.TicksGame,
                    Progress = 0f,
                    IsAutoCaptured = true,
                    IsGameConditionEvent = true,
                    GameConditionDefName = cond.def.defName
                };

                // 如果不是永久状况，设置预计结束时间
                // 注意：统一使用 TicksGame 体系，避免 cond.startTick 可能与 TicksGame 不一致的问题
                if (!cond.Permanent && cond.Duration > 0)
                {
                    announcement.DeadlineTicks = Find.TickManager.TicksGame + cond.Duration;
                }
                else if (settings.GameConditionExpireHours > 0)
                {
                    // 永久状况使用配置的过期时间
                    announcement.DeadlineTicks = Find.TickManager.TicksGame + 
                        (int)(settings.GameConditionExpireHours * 2500);
                }

                // 查找并完成之前同类型的状况事件
                CompletePreviousConditionEvents(cond.def.defName);

                // 标记此 GameCondition 已被捕获（避免 Archive 系统重复捕获信件）
                EventCaptureService.MarkGameConditionCaptured(cond.LabelCap);
                
                // 添加事件
                var manager = ColonyAnnouncementManager.Instance;
                manager?.AddAnnouncement(announcement);

                DebugLog.Log($"Game condition started: {cond.def.label}");
            }
            catch (Exception ex)
            {
                Log.Warning($"[RimTalk Health Enhance] Error capturing game condition: {ex.Message}");
            }
        }

        private static string GetConditionTitle(GameCondition cond, bool isStart)
        {
            string action = isStart ? "⚠️ " : "✓ ";
            return $"{action}{cond.LabelCap}";
        }

        private static string GetConditionDescription(GameCondition cond, Map map)
        {
            var sb = new System.Text.StringBuilder();
            
            // 状况描述
            if (!cond.Description.NullOrEmpty())
            {
                sb.AppendLine(cond.Description.StripTags());
            }
            
            // 持续时间信息
            if (cond.Permanent)
            {
                sb.AppendLine("Duration: Permanent");
            }
            else if (cond.Duration > 0)
            {
                string durationStr = cond.Duration.ToStringTicksToPeriod();
                sb.AppendLine($"Expected duration: {durationStr}");
            }
            
            // 温度影响（静态值，不需要实时更新）
            float tempOffset = cond.TemperatureOffset();
            if (Math.Abs(tempOffset) > 0.1f)
            {
                sb.AppendLine($"Temperature effect: {tempOffset:+0.#;-0.#}°C");
            }
            
            // 注意：不再显示当前室外温度，因为无法实时更新
            
            return sb.ToString().Trim();
        }

        private static AnnouncementPriority GetConditionPriority(GameCondition cond)
        {
            // 方法1: 检查温度影响（通用方法，不依赖defName穷举）
            float tempOffset = cond.TemperatureOffset();
            
            // 温度影响大于15度 - 紧急
            if (Math.Abs(tempOffset) > 15f)
            {
                return AnnouncementPriority.Urgent;
            }
            
            // 温度影响大于10度 - 高优先级
            if (Math.Abs(tempOffset) > 10f)
            {
                return AnnouncementPriority.High;
            }
            
            // 方法2: 检查conditionClass类型（更可靠的判断方法）
            Type condType = cond.def.conditionClass;
            if (condType != null)
            {
                string typeName = condType.Name.ToLower();
                
                // 心灵类事件通常是紧急的
                if (typeName.Contains("psychic") || typeName.Contains("emanation"))
                {
                    return AnnouncementPriority.Urgent;
                }
            }
            
            // 方法3: 基于defName/label关键词判断（覆盖广泛的情况）
            string defName = cond.def.defName.ToLower();
            string label = cond.def.label?.ToLower() ?? "";
            string combined = defName + " " + label;
            
            // 危险状况关键词 - 紧急优先级
            string[] urgentKeywords = {
                "toxic", "fallout", "flashstorm", "volcanic", "psychic",
                "mechanoid", "manhunter", "eclipse", "darkness", "unnatural",
                "creeping", "anomaly", "void", "death", "plague", "blight",
                "infestation", "attack", "raid", "shard", "corruption"
            };
            
            foreach (var keyword in urgentKeywords)
            {
                if (combined.Contains(keyword))
                {
                    return AnnouncementPriority.Urgent;
                }
            }
            
            // 极端天气关键词 - 高优先级
            string[] highKeywords = {
                "heatwave", "coldsnap", "blizzard", "storm", "fog", "mist",
                "aurora", "solar", "flare", "cold", "heat", "freeze"
            };
            
            foreach (var keyword in highKeywords)
            {
                if (combined.Contains(keyword))
                {
                    return AnnouncementPriority.High;
                }
            }
            
            // 如果有温度影响（任何程度），提升到普通优先级
            if (Math.Abs(tempOffset) > 0.1f)
            {
                return AnnouncementPriority.Normal;
            }
            
            // 其他状况 - 低优先级（但仍会被捕获！）
            return AnnouncementPriority.Low;
        }

        private static void CompletePreviousConditionEvents(string defName)
        {
            var manager = ColonyAnnouncementManager.Instance;
            if (manager == null) return;

            var previousEvents = manager.Data.Announcements
                .FindAll(a => a.IsGameConditionEvent && 
                             a.GameConditionDefName == defName && 
                             a.Status == AnnouncementStatus.Active);
            
            foreach (var evt in previousEvents)
            {
                evt.Status = AnnouncementStatus.Completed;
                evt.CompletedTick = Find.TickManager.TicksGame;
            }
        }
    }

    /// <summary>
    /// Patches GameCondition.End to capture game condition end events
    /// </summary>
    [HarmonyPatch(typeof(GameCondition), nameof(GameCondition.End))]
    public static class GameConditionEndPatch
    {
        public static void Prefix(GameCondition __instance)
        {
            if (__instance == null || __instance.def == null) return;

            try
            {
                var settings = RimTalkHealthEnhanceMod.Settings;
                if (!settings.EnableAutoEventCapture || !settings.AutoCaptureGameConditions) return;

                // 跳过不显示在UI上的状况
                if (!__instance.def.displayOnUI) return;

                // 查找并完成相关的活动事件
                var manager = ColonyAnnouncementManager.Instance;
                if (manager == null) return;

                var activeEvents = manager.Data.Announcements
                    .FindAll(a => a.IsGameConditionEvent && 
                                 a.GameConditionDefName == __instance.def.defName && 
                                 a.Status == AnnouncementStatus.Active);
                
                foreach (var evt in activeEvents)
                {
                    evt.Status = AnnouncementStatus.Completed;
                    evt.CompletedTick = Find.TickManager.TicksGame;
                    
                    // 更新标题表示已结束
                    if (!evt.Title.StartsWith("✓"))
                    {
                        evt.Title = evt.Title.Replace("⚠️ ", "✓ ");
                        if (!evt.Title.StartsWith("✓"))
                        {
                            evt.Title = "✓ " + evt.Title;
                        }
                    }
                    
                    // 添加结束信息
                    int duration = Find.TickManager.TicksGame - evt.CreatedTick;
                    evt.Description += $"\n\nEnded after {duration.ToStringTicksToPeriod()}";
                }

                if (activeEvents.Count > 0)
                {
                    DebugLog.Log($"Game condition ended: {__instance.def.label}");
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[RimTalk Health Enhance] Error capturing game condition end: {ex.Message}");
            }
        }
    }
}