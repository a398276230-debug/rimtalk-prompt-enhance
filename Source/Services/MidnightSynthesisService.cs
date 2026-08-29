using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimTalkHealthEnhance
{
    public static class MidnightSynthesisService
    {
        public static async Task PerformSynthesis()
        {
            DebugLog.Log("Starting Midnight Synthesis...");
            var manager = ColonyAnnouncementManager.Instance;
            var settings = RimTalkHealthEnhanceMod.Settings;
            
            if (!settings.EnableAISynthesis) 
            {
                DebugLog.Log("AI Synthesis disabled.");
                return;
            }
            
            // 1. 拍摄今日快照
            var todaySnapshot = SnapshotService.TakeSnapshot();
            DebugLog.Log($"Snapshot taken. Buildings: {todaySnapshot.BuildingCounts.Count}, Rooms: {todaySnapshot.Rooms.Count}");
            
            // 2. 生成差分报告
            var yesterdaySnapshot = manager.Data.LastSnapshot ?? new ColonySnapshot();
            string diffReport = DiffAnalyzer.GenerateDiffReport(yesterdaySnapshot, todaySnapshot);
            
            // 3. 收集当日事件（包含标题、描述和战斗统计）
            var todayEvents = manager.Data.Announcements
                .Where(a => a.Category == AnnouncementCategory.Event &&
                            a.CreatedTick > Find.TickManager.TicksGame - 60000)
                .Select(a => FormatEventInfo(a))
                .ToList();
            
            // 4. 收集工程信息（如果启用）
            List<string> projectInfo = new List<string>();
            if (settings.IncludeProjectsInSnapshot)
            {
                var activeProjects = manager.Data.Announcements
                    .Where(a => a.Category == AnnouncementCategory.Project)
                    .ToList();
                
                DebugLog.Log($"Found {activeProjects.Count} projects in status board");
                
                foreach (var project in activeProjects)
                {
                    string statusText = project.Status == AnnouncementStatus.Completed ? "[已完成]" : 
                                       project.Status == AnnouncementStatus.Paused ? "[暂停]" : "[进行中]";
                    string progressText = project.Progress > 0 ? $" ({project.Progress:P0})" : "";
                    string assignedText = !string.IsNullOrEmpty(project.AssignedPawnName) ? $" - 负责人: {project.AssignedPawnName}" : "";
                    string descText = !string.IsNullOrEmpty(project.Description) ? $" - {project.Description}" : "";
                    
                    // 收集该工程包含的蓝图
                    List<string> relatedBlueprints = new List<string>();
                    if (todaySnapshot.BlueprintToProjects != null)
                    {
                        foreach (var kvp in todaySnapshot.BlueprintToProjects)
                        {
                            if (kvp.Value.Projects.Contains(project.Title))
                            {
                                string defName = kvp.Key;
                                string label = DefDatabase<ThingDef>.GetNamedSilentFail(defName)?.label ?? defName;
                                int count = todaySnapshot.BlueprintCounts.ContainsKey(defName) ? todaySnapshot.BlueprintCounts[defName] : 0;
                                if (count > 0)
                                {
                                    relatedBlueprints.Add($"{label} x{count}");
                                }
                            }
                        }
                    }
                    
                    string blueprintsText = relatedBlueprints.Count > 0 
                        ? $"\n  包含蓝图: {string.Join(", ", relatedBlueprints)}" 
                        : "";
                    
                    string projectLine = $"{statusText} {project.Title}{progressText}{assignedText}{descText}{blueprintsText}";
                    projectInfo.Add(projectLine);
                    DebugLog.Log($"Project: {projectLine}");
                }
            }
            else
            {
                DebugLog.Log("Project tracking is disabled in settings");
            }
            
            // 5. 收集科技信息（如果启用）
            string researchInfo = null;
            if (settings.IncludeResearchInSnapshot)
            {
                researchInfo = ResearchInfoBuilder.BuildResearchContext();
                if (!string.IsNullOrEmpty(researchInfo))
                {
                    DebugLog.Log($"Research info collected: {researchInfo.Length} chars");
                }
            }
            else
            {
                DebugLog.Log("Research tracking is disabled in settings");
            }
            
            // 6. 收集电力信息（如果启用）
            string powerInfo = null;
            if (settings.IncludePowerInSnapshot)
            {
                powerInfo = PowerInfoBuilder.BuildPowerContext();
                if (!string.IsNullOrEmpty(powerInfo))
                {
                    DebugLog.Log($"Power info collected: {powerInfo.Length} chars");
                }
            }
            else
            {
                DebugLog.Log("Power tracking is disabled in settings");
            }
            
            // 6. 创建快照记录
            // 使用 TicksAbs 作为唯一标识，不再手动计算 Day
            long snapshotAbsTick = Find.TickManager.TicksAbs;
            
            var dailySnapshot = new DailySnapshot
            {
                AbsTick = snapshotAbsTick,  // 使用绝对时间戳
                Snapshot = todaySnapshot,
                PlayerActions = new List<string>(manager.Data.TodayActionLogs),
                Events = todayEvents,
                DiffReport = diffReport
            };
            
            // 7. 调用 AI 生成总结（可选）
            // 只有在有实质性变化时才调用AI
            bool hasChanges = !string.IsNullOrWhiteSpace(diffReport) || 
                              todayEvents.Count > 0 || 
                              manager.Data.TodayActionLogs.Count > 0 ||
                              projectInfo.Count > 0;

            DebugLog.Log($"Changes detected - Diff: {!string.IsNullOrWhiteSpace(diffReport)}, Events: {todayEvents.Count}, Actions: {manager.Data.TodayActionLogs.Count}, Projects: {projectInfo.Count}");

            if (!string.IsNullOrEmpty(settings.CustomApiKey))
            {
                if (hasChanges)
                {
                    try
                    {
                        string prompt = BuildSynthesisPrompt(diffReport, dailySnapshot, projectInfo, researchInfo, powerInfo);
                        DebugLog.Log($"Sending prompt to AI ({prompt.Length} chars)...");
                        dailySnapshot.AISummary = await SimpleAIClient.CallAI(prompt);
                        
                        if (string.IsNullOrEmpty(dailySnapshot.AISummary))
                        {
                            // AI 调用返回空结果，使用简单模板作为备选
                            Log.Warning("[RimTalk Enhance] AI returned empty response, using fallback template.");
                            dailySnapshot.AISummary = GenerateSimpleSummary(diffReport, todayEvents, projectInfo, researchInfo, powerInfo);
                            dailySnapshot.AISummary = "[AI 调用失败，以下为自动生成摘要]\n" + dailySnapshot.AISummary;
                        }
                        else
                        {
                            DebugLog.Log($"AI response received ({dailySnapshot.AISummary.Length} chars)");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"[RimTalk Enhance] AI Synthesis Failed: {ex.Message}");
                        // 使用简单模板作为备选，而不是只显示失败消息
                        dailySnapshot.AISummary = GenerateSimpleSummary(diffReport, todayEvents, projectInfo, researchInfo, powerInfo);
                        dailySnapshot.AISummary = "[AI 总结失败，以下为自动生成摘要]\n" + dailySnapshot.AISummary;
                    }
                }
                else
                {
                    dailySnapshot.AISummary = "（昨日无事发生，岁月静好）";
                }
            }
            else
            {
                // 无 API Key 时，使用简单模板
                dailySnapshot.AISummary = GenerateSimpleSummary(diffReport, todayEvents, projectInfo, researchInfo, powerInfo);
            }
            
            // 6. 保存快照
            manager.Data.DailySnapshots.Add(dailySnapshot);
            manager.Data.LastSnapshot = todaySnapshot;
            manager.Data.TodayActionLogs.Clear();
            
            // 7. 清理过期快照（使用 AbsTick 排序）
            if (manager.Data.DailySnapshots.Count > manager.Data.MaxSnapshotDays)
            {
                manager.Data.DailySnapshots = manager.Data.DailySnapshots
                    .OrderByDescending(s => s.AbsTick)
                    .Take(manager.Data.MaxSnapshotDays)
                    .ToList();
            }
            
            manager.NotifyDataChanged();
            
            DebugLog.Log($"Synthesis completed. Total snapshots: {manager.Data.DailySnapshots.Count}");
            
            // 在主线程显示消息（添加异常保护避免整个方法因小问题失败）
            try
            {
                // 使用当前地图的经纬度来获取正确的日期/季节显示（南半球会有不同季节）
                Vector2 location = Vector2.zero;
                var map = Find.CurrentMap;
                if (map != null)
                {
                    location = Find.WorldGrid.LongLatOf(map.Tile);
                }
                
                string displayDate = dailySnapshot.GetDateString(location);
                Messages.Message(
                    $"[AI 史官] {displayDate} 快照已生成",
                    MessageTypeDefOf.NeutralEvent
                );
            }
            catch (Exception ex)
            {
                // 消息显示失败不影响主流程，只记录警告
                Log.Warning($"[RimTalk Enhance] Failed to show synthesis message: {ex.Message}");
            }
        }
        
        public static string GetDefaultPromptTemplate()
        {
            return @"你是一个RimWorld（环世界）殖民地的史官。请根据提供的【昨日数据】，为殖民地撰写一段昨日的发展日志。

【参考风格与背景】
{overview}

【昨日数据】
1. 建筑与发展变化：
{diffReport}

2. 规划与决策记录（底层日志）：
{actions}

3. 工程项目状态：
{projects}

4. 科技研究状态：
{research}

5. 电力状态：
{power}

6. 发生事件：
{events}

---
【生成指令】
1. **核心原则**：将上述数据转化为一段连贯的叙事文本。不要写成清单或流水账。
2. **禁词**：绝对**禁止**出现""玩家""、""用户""、""系统""、""指令""等打破第四面墙的词汇。
   - 将""玩家部署蓝图""描述为""殖民地规划了...""、""大家决定建设...""或""新的蓝图被绘制出来""。
   - 将""建筑任务执行完毕""描述为""...终于建成了""、""...完工了""。
   - 将工程项目描述为殖民地的建设计划和进展。
   - 将科技研究描述为殖民地的知识积累和技术突破。
   - 将电力状态描述为殖民地的能源供需情况。
3. **建筑变化解读**：
   - 【新增建筑】：新建成的设施。
   - 【减少/拆除的建筑】：被拆除、损毁或卸载的设施。描述为""拆除了...""、""损失了...""或""卸载了...""。
   - 【重新安装/迁移中】：正在从一个位置移动到另一个位置的设施。描述为""正在迁移...""、""重新布置...""或""调整了...的位置""。
   - 【消失/拆除的房间】：不再存在的房间。描述为""拆除了...""或""改建了...""。
   - 【进行中的蓝图】：正在规划建造的新设施。
4. **内容融合**：
   - 结合【规划】、【建筑变化】和【工程项目】，描述殖民地的建设进程。
   - 结合【科技研究】，描述殖民地的技术发展。
   - 结合【电力状态】，如果电力不足或盈余较大，可以简要提及。
   - 结合【事件】，描述殖民地遭遇的挑战或机遇。
5. **篇幅**：控制在100-200字左右，精炼概括昨日重点。
6. **风格一致性**：如果【参考风格】是第一人称（我/我们），请保持；如果是第三人称，请保持。如果风格幽默，请保持幽默；如果严肃，请保持严肃。
7. 不要写开头和结尾的套话，直接输出内容。";
        }

        public static string BuildSynthesisPrompt(string diffReport, DailySnapshot snapshot, List<string> projectInfo, string researchInfo, string powerInfo = null)
        {
            var manager = ColonyAnnouncementManager.Instance;
            var settings = RimTalkHealthEnhanceMod.Settings;
            string existingOverview = manager.Data.ColonyOverview;
            bool hasOverview = !string.IsNullOrEmpty(existingOverview);

            // 检查是否有自定义提示词
            if (!string.IsNullOrEmpty(settings.CustomDailySynthesisPrompt))
            {
                // 使用自定义提示词并替换变量
                string overviewText = hasOverview ? existingOverview : "（新殖民地，暂无历史记录）";
                string actionsText = snapshot.PlayerActions.Count > 0
                    ? string.Join("\n", snapshot.PlayerActions)
                    : "（无新规划）";
                string eventsText = snapshot.Events.Count > 0
                    ? string.Join("\n", snapshot.Events.Select(e => $"- {e}"))
                    : "（无重大事件）";
                string projectsText = projectInfo != null && projectInfo.Count > 0
                    ? string.Join("\n", projectInfo.Select(p => $"- {p}"))
                    : "（无工程项目）";
                string researchText = !string.IsNullOrEmpty(researchInfo) ? researchInfo : "（无科技信息）";
                string powerText = !string.IsNullOrEmpty(powerInfo) ? powerInfo : "（无电力信息）";

                return settings.CustomDailySynthesisPrompt
                    .Replace("{overview}", overviewText)
                    .Replace("{diffReport}", string.IsNullOrWhiteSpace(diffReport) ? "（无明显建筑变化）" : diffReport)
                    .Replace("{actions}", actionsText)
                    .Replace("{events}", eventsText)
                    .Replace("{projects}", projectsText)
                    .Replace("{research}", researchText)
                    .Replace("{power}", powerText);
            }

            // 使用默认提示词
            var sb = new StringBuilder();
            sb.AppendLine("你是一个RimWorld（环世界）殖民地的史官。请根据提供的【昨日数据】，为殖民地撰写一段昨日的发展日志。");
            sb.AppendLine();
            
            if (hasOverview)
            {
                sb.AppendLine("【参考风格与背景】");
                sb.AppendLine("以下是玩家撰写的当前殖民地概况。请**务必模仿**这段文字的语言风格（语调、人称、用词习惯）来进行续写。");
                sb.AppendLine("--- 概况开始 ---");
                sb.AppendLine(existingOverview);
                sb.AppendLine("--- 概况结束 ---");
            }
            else
            {
                sb.AppendLine("【风格要求】");
                sb.AppendLine("由于暂无历史概况，请使用冷静、客观但具有叙事感的风格。");
            }

            sb.AppendLine();
            sb.AppendLine("【昨日数据】");
            
            sb.AppendLine("1. 建筑与发展变化：");
            sb.AppendLine(string.IsNullOrWhiteSpace(diffReport) ? "（无明显建筑变化）" : diffReport);
            
            sb.AppendLine("2. 规划与决策记录（底层日志）：");
            if (snapshot.PlayerActions.Count > 0)
                sb.AppendLine(string.Join("\n", snapshot.PlayerActions));
            else
                sb.AppendLine("（无新规划）");
            
            sb.AppendLine("3. 工程项目状态：");
            if (projectInfo != null && projectInfo.Count > 0)
                sb.AppendLine(string.Join("\n", projectInfo.Select(p => $"- {p}")));
            else
                sb.AppendLine("（无工程项目）");
            
            int sectionNum = 4;
            if (!string.IsNullOrEmpty(researchInfo))
            {
                sb.AppendLine($"{sectionNum}. 科技研究状态：");
                sb.AppendLine(researchInfo);
                sectionNum++;
            }
            
            if (!string.IsNullOrEmpty(powerInfo))
            {
                sb.AppendLine($"{sectionNum}. 电力状态：");
                sb.AppendLine(powerInfo);
                sectionNum++;
            }
            
            sb.AppendLine($"{sectionNum}. 发生事件：");
            if (snapshot.Events.Count > 0)
                sb.AppendLine(string.Join("\n", snapshot.Events.Select(e => $"- {e}")));
            else
                sb.AppendLine("（无重大事件）");

            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine("【生成指令】");
            sb.AppendLine("1. **核心原则**：将上述数据转化为一段连贯的叙事文本。不要写成清单或流水账。");
            sb.AppendLine("2. **禁词**：绝对**禁止**出现「玩家」、「用户」、「系统」、「指令」等打破第四面墙的词汇。");
            sb.AppendLine("   - 将「玩家部署蓝图」描述为「殖民地规划了...」、「大家决定建设...」或「新的蓝图被绘制出来」。");
            sb.AppendLine("   - 将「建筑任务执行完毕」描述为「...终于建成了」、「...完工了」。");
            sb.AppendLine("3. **建筑变化解读**：");
            sb.AppendLine("   - 【新增建筑】：新建成的设施。");
            sb.AppendLine("   - 【减少/拆除的建筑】：被拆除、损毁或卸载的设施。描述为「拆除了...」、「损失了...」或「卸载了...」。");
            sb.AppendLine("   - 【重新安装/迁移中】：正在从一个位置移动到另一个位置的设施。描述为「正在迁移...」、「重新布置...」或「调整了...的位置」。");
            sb.AppendLine("   - 【消失/拆除的房间】：不再存在的房间。描述为「拆除了...」或「改建了...」。");
            sb.AppendLine("   - 【进行中的蓝图】：正在规划建造的新设施。");
            sb.AppendLine("4. **内容融合**：");
            sb.AppendLine("   - 结合【规划】与【建筑变化】，描述殖民地的建设进程。");
            sb.AppendLine("   - 结合【事件】，描述殖民地遭遇的挑战或机遇。");
            sb.AppendLine("5. **篇幅**：控制在100-200字左右，精炼概括今日重点。");
            if (hasOverview)
            {
                sb.AppendLine("6. **风格一致性**：如果【参考风格】是第一人称（我/我们），请保持；如果是第三人称，请保持。如果风格幽默，请保持幽默；如果严肃，请保持严肃。");
            }
            sb.AppendLine("7. 不要写开头和结尾的套话，直接输出内容。");

            return sb.ToString();
        }
        
        /// <summary>
        /// 格式化事件信息，包含战斗统计
        /// </summary>
        private static string FormatEventInfo(ColonyAnnouncement announcement)
        {
            var sb = new StringBuilder();
            sb.Append(announcement.Title);
            
            // 如果是袭击事件，显示战斗统计（即使还在进行中）
            if (announcement.IsRaidEvent)
            {
                // 获取当前的受伤统计（从 RaidTrackingService 获取实时数据）
                int woundedEnemies = RaidTrackingService.GetWoundedEnemyCount();
                int woundedColonists = RaidTrackingService.GetWoundedColonistCount();
                
                // 根据袭击类型智能选择措辞（与 FinishRaidTracking 保持一致，优先 RaidKind）
                var (threatName, unitName) = RaidTrackingService.GetRaidTypeDisplayNames(announcement);
                
                // 如果有初始计数，显示详细统计
                if (announcement.RaidInitialCount > 0)
                {
                    sb.Append($" ({threatName}{announcement.RaidInitialCount}{unitName}");
                    
                    var stats = new List<string>();
                    if (announcement.RaidKillCount > 0)
                        stats.Add($"击杀{announcement.RaidKillCount}");
                    if (announcement.RaidDownedCount > 0)
                        stats.Add($"击倒{announcement.RaidDownedCount}");
                    if (announcement.RaidFleeCount > 0)
                        stats.Add($"逃跑{announcement.RaidFleeCount}");
                    if (woundedEnemies > 0 && announcement.Status == AnnouncementStatus.Active)
                        stats.Add($"受伤{woundedEnemies}");
                    
                    if (stats.Count > 0)
                        sb.Append($": {string.Join(", ", stats)}");
                    
                    // 殖民者伤亡
                    var colonistStats = new List<string>();
                    if (announcement.ColonistDeathCount > 0)
                        colonistStats.Add($"阵亡{announcement.ColonistDeathCount}");
                    if (announcement.ColonistDownedCount > 0)
                        colonistStats.Add($"倒地{announcement.ColonistDownedCount}");
                    if (woundedColonists > 0 && announcement.Status == AnnouncementStatus.Active)
                        colonistStats.Add($"受伤{woundedColonists}");
                    
                    if (colonistStats.Count > 0)
                        sb.Append($" | 殖民地: {string.Join(", ", colonistStats)}");
                    else
                        sb.Append(" | 殖民地无伤亡");
                    
                    sb.Append(")");
                }
                else
                {
                    // 没有初始计数，但仍然是袭击事件（可能是旧存档或检测时机问题），静默处理
                }
            }
            
            // 添加描述（如果不是战斗报告，因为战斗报告已经包含在上面了）
            if (!string.IsNullOrEmpty(announcement.Description) && !announcement.IsRaidEvent)
            {
                sb.Append($": {announcement.Description}");
            }
            
            // 添加状态
            if (announcement.Status == AnnouncementStatus.Completed)
            {
                sb.Append(" [已结束]");
            }
            else if (announcement.Status == AnnouncementStatus.Active && announcement.IsRaidEvent)
            {
                sb.Append(" [进行中]");
            }
            
            return sb.ToString();
        }
        
        private static string GenerateSimpleSummary(string diffReport, List<string> events, List<string> projectInfo, string researchInfo, string powerInfo = null)
        {
            // 无 AI 时的简单模板
            var sb = new StringBuilder();
            sb.AppendLine("昨日殖民地发生以下变化：");
            
            if (!string.IsNullOrEmpty(diffReport))
                sb.AppendLine(diffReport.Replace("【", "").Replace("】", ":"));
            
            if (projectInfo != null && projectInfo.Count > 0)
            {
                sb.AppendLine("\n工程项目：");
                foreach (var project in projectInfo)
                    sb.AppendLine($"- {project}");
            }
            
            if (!string.IsNullOrEmpty(researchInfo))
            {
                sb.AppendLine("\n科技状态：");
                sb.AppendLine(researchInfo);
            }
            
            if (!string.IsNullOrEmpty(powerInfo))
            {
                sb.AppendLine("\n电力状态：");
                sb.AppendLine(powerInfo);
            }
            
            if (events.Count > 0)
            {
                sb.AppendLine("\n重要事件：");
                foreach (var evt in events)
                    sb.AppendLine($"- {evt}");
            }
            
            return sb.ToString();
        }
    }
}
