using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RimTalkHealthEnhance
{
    /// <summary>
    /// Patches WeatherManager.TransitionTo to capture weather change events
    /// </summary>
    [HarmonyPatch(typeof(WeatherManager), nameof(WeatherManager.TransitionTo))]
    public static class WeatherTransitionPatch
    {
        public static void Postfix(WeatherManager __instance, WeatherDef newWeather)
        {
            if (newWeather == null || __instance?.map == null) return;

            try
            {
                var settings = RimTalkHealthEnhanceMod.Settings;
                if (!settings.EnableAutoEventCapture || !settings.AutoCaptureWeather) return;

                // 获取之前的天气（在 TransitionTo 中已经变成 lastWeather）
                WeatherDef previousWeather = __instance.lastWeather;
                
                // 如果天气没有变化，跳过
                if (previousWeather == newWeather) return;
                
                // 只捕获玩家所在地图的天气变化
                if (__instance.map != Find.CurrentMap) return;

                // 创建天气变化事件
                string title = GetWeatherChangeTitle(previousWeather, newWeather);
                string description = GetWeatherChangeDescription(previousWeather, newWeather, __instance.map);

                var announcement = new ColonyAnnouncement
                {
                    Id = Guid.NewGuid().ToString(),
                    Category = AnnouncementCategory.Event,
                    Title = title,
                    Description = description,
                    Priority = GetWeatherPriority(newWeather),
                    Status = AnnouncementStatus.Active,
                    CreatedTick = Find.TickManager.TicksGame,
                    Progress = 0f,
                    IsAutoCaptured = true,
                    IsWeatherEvent = true // 新增标记
                };

                // 设置过期时间（天气事件较短暂）
                if (settings.WeatherEventExpireHours > 0)
                {
                    announcement.DeadlineTicks = Find.TickManager.TicksGame + 
                        (int)(settings.WeatherEventExpireHours * 2500); // 1小时 = 2500 ticks
                }

                // 添加到管理器（会自动合并相同天气事件）
                AddWeatherEvent(announcement);

                Log.Message($"[RimTalk Enhance] Weather change captured: {previousWeather?.label ?? "none"} → {newWeather.label}");
            }
            catch (Exception ex)
            {
                Log.Warning($"[RimTalk Health Enhance] Error capturing weather event: {ex.Message}");
            }
        }

        private static string GetWeatherChangeTitle(WeatherDef previous, WeatherDef current)
        {
            if (previous == null)
                return $"Weather: {current.LabelCap}";
            
            return $"Weather: {previous.LabelCap} → {current.LabelCap}";
        }

        private static string GetWeatherChangeDescription(WeatherDef previous, WeatherDef current, Map map)
        {
            var sb = new System.Text.StringBuilder();
            
            // 当前天气描述
            if (!current.description.NullOrEmpty())
            {
                sb.AppendLine(current.description.StripTags());
            }
            
            // 天气属性
            if (current.rainRate > 0)
                sb.AppendLine($"Rain intensity: {(current.rainRate * 100):F0}%");
            if (current.snowRate > 0)
                sb.AppendLine($"Snowfall: {(current.snowRate * 100):F0}%");
            if (current.windSpeedFactor != 1f)
                sb.AppendLine($"Wind: {(current.windSpeedFactor * 100):F0}%");
            if (current.moveSpeedMultiplier != 1f)
                sb.AppendLine($"Movement speed: {(current.moveSpeedMultiplier * 100):F0}%");
            if (current.accuracyMultiplier != 1f)
                sb.AppendLine($"Accuracy: {(current.accuracyMultiplier * 100):F0}%");
            
            // 当前温度
            if (map != null)
            {
                float temp = map.mapTemperature.OutdoorTemp;
                sb.AppendLine($"Current outdoor temperature: {temp:F1}°C");
            }
            
            return sb.ToString().Trim();
        }

        private static AnnouncementPriority GetWeatherPriority(WeatherDef weather)
        {
            // 危险天气（暴风雪、毒雾等）设为高优先级
            if (weather.moveSpeedMultiplier < 0.7f || 
                weather.accuracyMultiplier < 0.7f ||
                weather.defName.IndexOf("fog", StringComparison.OrdinalIgnoreCase) >= 0 ||
                weather.defName.IndexOf("toxic", StringComparison.OrdinalIgnoreCase) >= 0 ||
                weather.defName.IndexOf("flashstorm", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return AnnouncementPriority.High;
            }
            
            // 恶劣天气设为普通优先级
            if (weather.rainRate > 0.5f || weather.snowRate > 0.5f)
            {
                return AnnouncementPriority.Normal;
            }
            
            // 普通天气设为低优先级
            return AnnouncementPriority.Low;
        }

        private static void AddWeatherEvent(ColonyAnnouncement announcement)
        {
            var manager = ColonyAnnouncementManager.Instance;
            if (manager == null) return;

            var settings = RimTalkHealthEnhanceMod.Settings;

            // 查找并完成之前的天气事件
            var previousWeatherEvents = manager.Data.Announcements
                .FindAll(a => a.IsWeatherEvent && a.Status == AnnouncementStatus.Active);
            
            foreach (var evt in previousWeatherEvents)
            {
                evt.Status = AnnouncementStatus.Completed;
                evt.CompletedTick = Find.TickManager.TicksGame;
            }

            // 添加新的天气事件
            manager.AddAnnouncement(announcement);
        }
    }
}