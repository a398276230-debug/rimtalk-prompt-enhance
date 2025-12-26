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
    /// Hediff分类枚举
    /// </summary>
    public enum HediffCategory
    {
        Bionic,         // 仿生体/义肢 (Hediff_AddedPart)
        Implant,        // 其他植入物 (Hediff_Implant but not AddedPart)
        Injury,         // 伤口 (Hediff_Injury)
        MissingPart,    // 缺失部位 (Hediff_MissingPart)
        Condition       // 疾病/状态/buff (其他)
    }

    /// <summary>
    /// 构建增强的健康信息，包含严重程度、疼痛等级和详细描述
    /// 支持分类过滤和智能整合
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

            var settings = RimTalkHealthEnhanceMod.Settings;
            var sb = new StringBuilder();
            sb.Append("Health:");

            // 1. 分类并过滤hediffs
            var categorizedHediffs = CategorizeHediffs(hediffs);
            
            // 2. 处理仿生体（可能使用摘要模式）
            var bionics = GetFilteredHediffs(categorizedHediffs, HediffCategory.Bionic, 
                settings.ShowBionics, settings.MaxBionicsToShow);
            
            if (bionics.Any())
            {
                if (settings.EnableBionicSummary && bionics.Count() >= 3)
                {
                    // 摘要模式
                    sb.Append("\n- ");
                    sb.Append(GenerateBionicSummary(bionics));
                }
                else
                {
                    // 正常列出
                    foreach (var hediff in bionics)
                    {
                        sb.Append("\n- ");
                        sb.Append(FormatHediffInfo(hediff, pawn, infoLevel));
                    }
                }
            }

            // 3. 处理其他植入物
            var implants = GetFilteredHediffs(categorizedHediffs, HediffCategory.Implant,
                settings.ShowImplants, settings.MaxImplantsToShow);
            foreach (var hediff in implants)
            {
                sb.Append("\n- ");
                sb.Append(FormatHediffInfo(hediff, pawn, infoLevel));
            }

            // 4. 处理伤口（可能使用整合模式）
            var injuries = GetFilteredHediffs(categorizedHediffs, HediffCategory.Injury,
                settings.ShowInjuries, settings.MaxInjuriesToShow);
            
            if (injuries.Any())
            {
                if (settings.EnableInjuryConsolidation)
                {
                    // 整合模式
                    var consolidatedInjuries = ConsolidateInjuries(injuries.Cast<Hediff_Injury>().ToList(), 
                        pawn, infoLevel, settings.MinorInjurySeverityThreshold);
                    foreach (var line in consolidatedInjuries)
                    {
                        sb.Append("\n- ");
                        sb.Append(line);
                    }
                }
                else
                {
                    // 正常列出
                    foreach (var hediff in injuries)
                    {
                        sb.Append("\n- ");
                        sb.Append(FormatHediffInfo(hediff, pawn, infoLevel));
                    }
                }
            }

            // 5. 处理缺失部位
            if (settings.ShowMissingParts)
            {
                var missingParts = categorizedHediffs
                    .Where(kvp => kvp.Value == HediffCategory.MissingPart)
                    .Select(kvp => kvp.Key);
                foreach (var hediff in missingParts)
                {
                    sb.Append("\n- ");
                    sb.Append(FormatHediffInfo(hediff, pawn, infoLevel));
                }
            }

            // 6. 处理疾病/状态
            var conditions = GetFilteredHediffs(categorizedHediffs, HediffCategory.Condition,
                settings.ShowConditions, settings.MaxConditionsToShow);
            foreach (var hediff in conditions)
            {
                sb.Append("\n- ");
                sb.Append(FormatHediffInfo(hediff, pawn, infoLevel));
            }

            // 如果只有"Health:"没有任何内容，返回null
            if (sb.ToString() == "Health:")
                return null;

            return sb.ToString();
        }

        /// <summary>
        /// 将hediffs按类型分类
        /// </summary>
        private static Dictionary<Hediff, HediffCategory> CategorizeHediffs(IEnumerable<Hediff> hediffs)
        {
            var result = new Dictionary<Hediff, HediffCategory>();

            foreach (var hediff in hediffs)
            {
                HediffCategory category;

                if (hediff is Hediff_AddedPart)
                {
                    category = HediffCategory.Bionic;
                }
                else if (hediff is Hediff_Implant)
                {
                    category = HediffCategory.Implant;
                }
                else if (hediff is Hediff_Injury)
                {
                    category = HediffCategory.Injury;
                }
                else if (hediff is Hediff_MissingPart)
                {
                    category = HediffCategory.MissingPart;
                }
                else
                {
                    category = HediffCategory.Condition;
                }

                result[hediff] = category;
            }

            return result;
        }

        /// <summary>
        /// 获取过滤后的hediffs（按类别和数量限制）
        /// </summary>
        private static IEnumerable<Hediff> GetFilteredHediffs(
            Dictionary<Hediff, HediffCategory> categorized, 
            HediffCategory category, 
            bool isEnabled, 
            int maxCount)
        {
            if (!isEnabled)
                return Enumerable.Empty<Hediff>();

            var filtered = categorized
                .Where(kvp => kvp.Value == category)
                .Select(kvp => kvp.Key)
                .OrderByDescending(h => GetHediffPriority(h))
                .ThenByDescending(h => h.Severity);

            if (maxCount > 0)
                return filtered.Take(maxCount);

            return filtered;
        }

        /// <summary>
        /// 生成仿生体摘要
        /// </summary>
        private static string GenerateBionicSummary(IEnumerable<Hediff> bionics)
        {
            var bionicList = bionics.ToList();
            var count = bionicList.Count;
            
            // 收集部位信息
            var partLabels = new List<string>();
            var partGroups = bionicList
                .Where(h => h.Part != null)
                .GroupBy(h => GetBodyPartGroupLabel(h.Part))
                .OrderByDescending(g => g.Count());

            foreach (var group in partGroups.Take(4)) // 最多显示4个部位组
            {
                if (group.Count() > 1 && group.Key.StartsWith("both"))
                {
                    partLabels.Add(group.Key);
                }
                else
                {
                    partLabels.Add(group.First().Part?.Label ?? "unknown");
                }
            }

            var partsText = partLabels.Any() 
                ? string.Join(", ", partLabels) 
                : "various parts";

            return $"Extensively enhanced ({count} bionics: {partsText})";
        }

        /// <summary>
        /// 获取身体部位组标签（用于整合显示）
        /// </summary>
        private static string GetBodyPartGroupLabel(BodyPartRecord part)
        {
            if (part == null) return "unknown";

            // 检查是否是成对的部位
            var label = part.Label.ToLower();
            
            if (label.Contains("left") || label.Contains("right"))
            {
                // 尝试获取对称部位的统一名称
                var baseName = label.Replace("left ", "").Replace("right ", "").Trim();
                return $"both {baseName}s";
            }

            return part.Label;
        }

        /// <summary>
        /// 整合伤口信息
        /// </summary>
        private static List<string> ConsolidateInjuries(
            List<Hediff_Injury> injuries, 
            Pawn pawn, 
            PromptService.InfoLevel infoLevel,
            float minorThreshold)
        {
            var result = new List<string>();
            var processedInjuries = new HashSet<Hediff_Injury>();

            // 分离严重伤口和轻伤
            var seriousInjuries = injuries.Where(i => i.Severity >= minorThreshold).ToList();
            var minorInjuries = injuries.Where(i => i.Severity < minorThreshold).ToList();

            // 严重伤口单独显示
            foreach (var injury in seriousInjuries.OrderByDescending(i => i.Severity))
            {
                result.Add(FormatHediffInfo(injury, pawn, infoLevel));
                processedInjuries.Add(injury);
            }

            // 轻伤按父部位整合
            if (minorInjuries.Any())
            {
                var groupedByParent = minorInjuries
                    .GroupBy(i => GetParentPartLabel(i.Part))
                    .Where(g => g.Key != null);

                foreach (var group in groupedByParent)
                {
                    var injuries_in_group = group.ToList();
                    
                    if (injuries_in_group.Count >= 2)
                    {
                        // 整合显示
                        var injuryTypes = injuries_in_group
                            .GroupBy(i => i.def.label)
                            .Select(g => g.Count() > 1 ? $"{g.Count()} {g.Key}s" : g.Key);
                        
                        var typesText = string.Join(", ", injuryTypes);
                        result.Add($"{group.Key}: {typesText} (minor)");
                    }
                    else
                    {
                        // 单个轻伤也显示
                        foreach (var injury in injuries_in_group)
                        {
                            result.Add(FormatHediffInfo(injury, pawn, infoLevel));
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 获取父部位标签
        /// </summary>
        private static string GetParentPartLabel(BodyPartRecord part)
        {
            if (part == null) return null;

            // 如果是手指/脚趾等小部位，返回父部位（手/脚）
            if (part.parent != null)
            {
                var parentLabel = part.parent.Label.ToLower();
                var partLabel = part.Label.ToLower();
                
                // 如果部位名包含父部位名（如"left hand middle finger"包含"left hand"）
                if (partLabel.Contains(parentLabel) || 
                    parentLabel.Contains("hand") || 
                    parentLabel.Contains("foot"))
                {
                    return part.parent.Label;
                }
            }

            return part.Label;
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
