using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using RimTalk.Service;
using RimWorld;
using Verse;

namespace RimTalkHealthEnhance
{
    /// <summary>
    /// 构建增强的健康信息，包含严重程度、疼痛等级和详细描述
    /// </summary>
    public static class HealthInfoBuilder
    {
        private static readonly MethodInfo VisibleHediffsMethod = AccessTools.Method(typeof(HealthCardUtility), "VisibleHediffs");

        public static string BuildEnhancedHealthContext(Pawn pawn, PromptService.InfoLevel infoLevel)
        {
            if (pawn?.health?.hediffSet == null)
                return null;

            var hediffs = (IEnumerable<Hediff>)VisibleHediffsMethod.Invoke(null, new object[] { pawn, false });
            if (hediffs == null || !hediffs.Any())
                return null;

            // 根据信息级别过滤和排序
            var filteredHediffs = FilterAndSortHediffs(hediffs, infoLevel);
            if (!filteredHediffs.Any())
                return null;

            var sb = new StringBuilder();
            sb.Append("Health:");

            foreach (var hediff in filteredHediffs)
            {
                sb.Append("\n- ");
                sb.Append(FormatHediffInfo(hediff, pawn, infoLevel));
            }

            return sb.ToString();
        }

        private static IEnumerable<Hediff> FilterAndSortHediffs(IEnumerable<Hediff> hediffs, PromptService.InfoLevel infoLevel)
        {
            var sorted = hediffs
                .OrderByDescending(h => GetHediffPriority(h))
                .ThenByDescending(h => h.Severity)
                .ThenByDescending(h => h.ageTicks);

            // Short模式只显示前3个最重要的
            if (infoLevel == PromptService.InfoLevel.Short)
                return sorted.Take(3);

            return sorted;
        }

        private static int GetHediffPriority(Hediff hediff)
        {
            // 优先级：致命 > 疼痛 > 可见 > 其他
            int priority = 0;

            if (hediff.CurStage?.lifeThreatening == true || hediff.def.lethalSeverity > 0)
                priority += 1000;

            if (hediff.PainOffset > 0.1f)
                priority += 100;

            if (hediff.Visible)
                priority += 10;

            return priority;
        }

        private static string FormatHediffInfo(Hediff hediff, Pawn pawn, PromptService.InfoLevel infoLevel)
        {
            var settings = RimTalkHealthEnhanceMod.Settings;
            var parts = new List<string>();

            // 基本信息：名称和部位
            string baseName = hediff.LabelCap;
            if (hediff.Part != null)
                baseName += $"({hediff.Part.Label})";
            parts.Add(baseName);

            // 严重度
            if (settings.ShowSeverity && (hediff.def.lethalSeverity > 0 || hediff.Severity > 0.01f))
            {
                float severityPercent = hediff.Severity * 100f;
                if (hediff.def.lethalSeverity > 0)
                    severityPercent = (hediff.Severity / hediff.def.lethalSeverity) * 100f;
                
                parts.Add($"Severity:{severityPercent:F0}%");
            }

            // 疼痛等级
            if (settings.ShowPainLevel && hediff.PainOffset > settings.MinPainToShow)
            {
                string painLevel = GetPainLevel(hediff.PainOffset);
                parts.Add($"Pain:{painLevel}");
            }

            // 致命标记
            if (settings.ShowLethalMarker && 
                (hediff.CurStage?.lifeThreatening == true || 
                 (hediff.def.lethalSeverity > 0 && hediff.Severity >= hediff.def.lethalSeverity * settings.LethalThreshold)))
            {
                parts.Add("LETHAL");
            }

            // 详细描述（仅Full模式）
            if (settings.ShowDescription && infoLevel == PromptService.InfoLevel.Full)
            {
                string description = GetHediffDescription(hediff);
                if (!string.IsNullOrEmpty(description))
                    parts.Add($"Desc:{description}");
            }

            return string.Join(", ", parts);
        }

        private static string GetPainLevel(float painOffset)
        {
            if (painOffset >= 0.4f) return "Extreme";
            if (painOffset >= 0.2f) return "Severe";
            if (painOffset >= 0.1f) return "Moderate";
            return "Mild";
        }

        private static string GetHediffDescription(Hediff hediff)
        {
            var settings = RimTalkHealthEnhanceMod.Settings;
            
            // 尝试获取hediff的描述
            string description = hediff.def.description;
            
            if (string.IsNullOrEmpty(description) && hediff.CurStage != null)
                description = hediff.CurStage.label;

            // 清理描述文本，移除换行和多余空格
            if (!string.IsNullOrEmpty(description))
            {
                description = description.Replace("\n", " ").Replace("\r", "");
                description = System.Text.RegularExpressions.Regex.Replace(description, @"\s+", " ").Trim();
                
                // 限制长度
                int maxLength = settings.MaxDescriptionLength;
                if (description.Length > maxLength)
                    description = description.Substring(0, maxLength - 3) + "...";
            }

            return description;
        }
    }
}
