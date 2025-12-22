using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace RimTalkHealthEnhance
{
    public static class MidnightSynthesisService
    {
        public static async Task PerformSynthesis()
        {
            Log.Message("[RimTalk Enhance] Starting Midnight Synthesis...");
            var manager = ColonyAnnouncementManager.Instance;
            var settings = RimTalkHealthEnhanceMod.Settings;
            
            if (!settings.EnableAISynthesis) 
            {
                Log.Message("[RimTalk Enhance] AI Synthesis disabled.");
                return;
            }
            
            // 1. 拍摄今日快照
            var todaySnapshot = SnapshotService.TakeSnapshot();
            Log.Message($"[RimTalk Enhance] Snapshot taken. Buildings: {todaySnapshot.BuildingCounts.Count}, Rooms: {todaySnapshot.Rooms.Count}");
            
            // 2. 生成差分报告
            var yesterdaySnapshot = manager.Data.LastSnapshot ?? new ColonySnapshot();
            string diffReport = DiffAnalyzer.GenerateDiffReport(yesterdaySnapshot, todaySnapshot);
            
            // 3. 收集当日事件（包含标题和描述）
            var todayEvents = manager.Data.Announcements
                .Where(a => a.Category == AnnouncementCategory.Event && 
                            a.CreatedTick > Find.TickManager.TicksGame - 60000)
                .Select(a => string.IsNullOrEmpty(a.Description) ? a.Title : $"{a.Title}: {a.Description}")
                .ToList();
            
            // 4. 创建快照记录
            // 注意：午夜触发时记录的是"昨天"的活动
            // 使用当前tick减去一整天(60000 ticks)来获取昨天的日期
            int yesterdayTick = Find.TickManager.TicksGame - 60000;
            
            var dailySnapshot = new DailySnapshot
            {
                Day = GenDate.DaysPassed,  // 保留用于排序
                Tick = yesterdayTick,  // 使用昨天的时间戳
                Snapshot = todaySnapshot,
                PlayerActions = new List<string>(manager.Data.TodayActionLogs),
                Events = todayEvents,
                DiffReport = diffReport
            };
            
            // 5. 调用 AI 生成总结（可选）
            // 只有在有实质性变化（diffReport不为空、有事件或有玩家操作）时才调用AI
            bool hasChanges = !string.IsNullOrWhiteSpace(diffReport) || 
                              todayEvents.Count > 0 || 
                              manager.Data.TodayActionLogs.Count > 0;

            if (!string.IsNullOrEmpty(settings.CustomApiKey))
            {
                if (hasChanges)
                {
                    try
                    {
                        string prompt = BuildSynthesisPrompt(diffReport, dailySnapshot);
                        dailySnapshot.AISummary = await SimpleAIClient.CallAI(prompt);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"[RimTalk Enhance] AI Synthesis Failed: {ex.Message}");
                        dailySnapshot.AISummary = "[AI 总结失败，请查看详细变化]";
                    }
                }
                else
                {
                    dailySnapshot.AISummary = "（今日无事发生，岁月静好）";
                }
            }
            else
            {
                // 无 API Key 时，使用简单模板
                dailySnapshot.AISummary = GenerateSimpleSummary(diffReport, todayEvents);
            }
            
            // 6. 保存快照
            manager.Data.DailySnapshots.Add(dailySnapshot);
            manager.Data.LastSnapshot = todaySnapshot;
            manager.Data.TodayActionLogs.Clear();
            
            // 7. 清理过期快照
            if (manager.Data.DailySnapshots.Count > manager.Data.MaxSnapshotDays)
            {
                manager.Data.DailySnapshots = manager.Data.DailySnapshots
                    .OrderByDescending(s => s.Day)
                    .Take(manager.Data.MaxSnapshotDays)
                    .ToList();
            }
            
            manager.NotifyDataChanged();
            
            Log.Message($"[RimTalk Enhance] Synthesis completed. Total snapshots: {manager.Data.DailySnapshots.Count}");
            
            Messages.Message(
                $"[AI 史官] 第 {dailySnapshot.Day} 天快照已生成",
                MessageTypeDefOf.NeutralEvent
            );
        }
        
        public static string GetDefaultPromptTemplate()
        {
            return @"你是一个RimWorld（环世界）殖民地的史官。请根据提供的【今日数据】，为殖民地撰写一段今日的发展日志。

【参考风格与背景】
{overview}

【今日数据】
1. 建筑与发展变化：
{diffReport}

2. 规划与决策记录（底层日志）：
{actions}

3. 发生事件：
{events}

---
【生成指令】
1. **核心原则**：将上述数据转化为一段连贯的叙事文本。不要写成清单或流水账。
2. **禁词**：绝对**禁止**出现""玩家""、""用户""、""系统""、""指令""等打破第四面墙的词汇。
   - 将""玩家部署蓝图""描述为""殖民地规划了...""、""大家决定建设...""或""新的蓝图被绘制出来""。
   - 将""建筑任务执行完毕""描述为""...终于建成了""、""...完工了""。
3. **内容融合**：
   - 结合【规划】与【建筑变化】，描述殖民地的建设进程。
   - 结合【事件】，描述殖民地遭遇的挑战或机遇。
4. **篇幅**：控制在100-200字左右，精炼概括今日重点。
5. **风格一致性**：如果【参考风格】是第一人称（我/我们），请保持；如果是第三人称，请保持。如果风格幽默，请保持幽默；如果严肃，请保持严肃。
6. 不要写开头和结尾的套话，直接输出内容。";
        }

        public static string BuildSynthesisPrompt(string diffReport, DailySnapshot snapshot)
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

                return settings.CustomDailySynthesisPrompt
                    .Replace("{overview}", overviewText)
                    .Replace("{diffReport}", string.IsNullOrWhiteSpace(diffReport) ? "（无明显建筑变化）" : diffReport)
                    .Replace("{actions}", actionsText)
                    .Replace("{events}", eventsText);
            }

            // 使用默认提示词
            var sb = new StringBuilder();
            sb.AppendLine("你是一个RimWorld（环世界）殖民地的史官。请根据提供的【今日数据】，为殖民地撰写一段今日的发展日志。");
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
            sb.AppendLine("【今日数据】");
            
            sb.AppendLine("1. 建筑与发展变化：");
            sb.AppendLine(string.IsNullOrWhiteSpace(diffReport) ? "（无明显建筑变化）" : diffReport);
            
            sb.AppendLine("2. 规划与决策记录（底层日志）：");
            if (snapshot.PlayerActions.Count > 0)
                sb.AppendLine(string.Join("\n", snapshot.PlayerActions));
            else
                sb.AppendLine("（无新规划）");
                
            sb.AppendLine("3. 发生事件：");
            if (snapshot.Events.Count > 0)
                sb.AppendLine(string.Join("\n", snapshot.Events.Select(e => $"- {e}")));
            else
                sb.AppendLine("（无重大事件）");

            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine("【生成指令】");
            sb.AppendLine("1. **核心原则**：将上述数据转化为一段连贯的叙事文本。不要写成清单或流水账。");
            sb.AppendLine("2. **禁词**：绝对**禁止**出现“玩家”、“用户”、“系统”、“指令”等打破第四面墙的词汇。");
            sb.AppendLine("   - 将“玩家部署蓝图”描述为“殖民地规划了...”、“大家决定建设...”或“新的蓝图被绘制出来”。");
            sb.AppendLine("   - 将“建筑任务执行完毕”描述为“...终于建成了”、“...完工了”。");
            sb.AppendLine("3. **内容融合**：");
            sb.AppendLine("   - 结合【规划】与【建筑变化】，描述殖民地的建设进程。");
            sb.AppendLine("   - 结合【事件】，描述殖民地遭遇的挑战或机遇。");
            sb.AppendLine("4. **篇幅**：控制在100-200字左右，精炼概括今日重点。");
            if (hasOverview)
            {
                sb.AppendLine("5. **风格一致性**：如果【参考风格】是第一人称（我/我们），请保持；如果是第三人称，请保持。如果风格幽默，请保持幽默；如果严肃，请保持严肃。");
            }
            sb.AppendLine("6. 不要写开头和结尾的套话，直接输出内容。");

            return sb.ToString();
        }
        
        private static string GenerateSimpleSummary(string diffReport, List<string> events)
        {
            // 无 AI 时的简单模板
            var sb = new StringBuilder();
            sb.AppendLine("今日殖民地发生以下变化：");
            
            if (!string.IsNullOrEmpty(diffReport))
                sb.AppendLine(diffReport.Replace("【", "").Replace("】", ":"));
            
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
