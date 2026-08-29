using System;
using System.Collections.Generic;
using System.Linq;
using RimTalk.Data;
using RimTalk.Service;
using RimTalk.Source.Data;
using RimWorld;
using Verse;

namespace RimTalkHealthEnhance.Services
{
    /// <summary>
    /// 群体讨论服务 - 让多个殖民者同时参与讨论一个话题。
    /// 直接引用 RimTalk v1.1.0 API，底层改用上游原生 Announcement 通告模式：
    /// AddTalkRequest(Announcement) 会 AddFirst 优先处理 + UserRequestPool 每秒消费，
    /// 附近最多 8 人自动进入上下文（TalkService.GetAllNearByPawns isAnnouncement 路径）。
    /// 与上游 gizmo 通告的差异：本入口从通告板条目发起，带话题上下文。
    /// </summary>
    public static class GroupDiscussionService
    {
        /// <summary>
        /// 获取可参与群体讨论的殖民者列表
        /// leader == null：领导者候选（全地图自由殖民者，广播者无需在场约束）
        /// leader != null：参与者候选，复用上游 Announcement 聚集规则保证与实际发言者一致
        /// </summary>
        public static List<Pawn> GetAvailableColonists(Pawn leader = null)
        {
            var result = new List<Pawn>();

            if (leader == null)
            {
                if (Find.CurrentMap == null)
                    return result;

                foreach (var pawn in Find.CurrentMap.mapPawns.FreeColonistsSpawned)
                {
                    if (pawn == null || pawn.Dead || !pawn.Spawned)
                        continue;

                    // 基本检查
                    if (!pawn.Awake())
                        continue;

                    // RimTalk 可显示对话检查
                    if (Cache.Get(pawn)?.CanDisplayTalk() != true)
                        continue;

                    result.Add(pawn);
                }

                return result;
            }

            // 上游聚集语义：同房间 + InHorDistOf(30f × 听者听力等级)，按距离升序取前 10 候选，
            // leader 本人已被上游排除；返回可能含访客/囚犯（Cache.Keys 全体），自由殖民者过滤负责剔除
            var freeColonists = leader.Map?.mapPawns.FreeColonistsSpawned;
            if (freeColonists == null)
                return result;

            foreach (var pawn in PawnSelector.GetAllNearByPawns(leader, isAnnouncement: true))
            {
                if (pawn == null || pawn.Dead || !pawn.Spawned)
                    continue;

                // 自由殖民者过滤（对齐领导者列表的 FreeColonistsSpawned 语义）
                if (!freeColonists.Contains(pawn))
                    continue;

                if (!pawn.Awake())
                    continue;

                if (Cache.Get(pawn)?.CanDisplayTalk() != true)
                    continue;

                result.Add(pawn);
            }

            return result;
        }

        /// <summary>
        /// 发起群体讨论：领导者作为公告者广播话题，附近殖民者自动参与。
        /// 调用链与上游 CustomDialogueService.ExecuteDialogue 的 Announcement 分支一致。
        /// </summary>
        /// <param name="item">要讨论的通告条目</param>
        /// <param name="leader">发起者/领导者（第一个发言的人）</param>
        /// <param name="participants">其他参与者（Announcement 模式下由 TalkService 按距离自动聚集，此参数仅用于提示词与人数校验）</param>
        /// <returns>是否成功发起讨论</returns>
        public static bool StartGroupDiscussion(ColonyAnnouncement item, Pawn leader, List<Pawn> participants)
        {
            if (item == null)
            {
                Log.Warning("[RimTalk Enhance] StartGroupDiscussion: item is null");
                return false;
            }

            if (leader == null)
            {
                Log.Warning("[RimTalk Enhance] StartGroupDiscussion: leader is null");
                Messages.Message("RTE_GroupDiscussion_NoLeader".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }

            if (participants == null || participants.Count == 0)
            {
                Log.Warning("[RimTalk Enhance] StartGroupDiscussion: no participants");
                Messages.Message("RTE_GroupDiscussion_NoParticipants".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }

            // AI 忙时不拦截：与 Player/PawnSelf 入口一致，经 UserRequestPool 入队等待、每秒重试
            try
            {
                // 领导者必须可显示对话
                var leaderState = Cache.Get(leader);
                if (leaderState == null || !leaderState.CanDisplayTalk())
                {
                    Log.Warning($"[RimTalk Enhance] {leader.LabelShort} cannot display talk");
                    Messages.Message("RTE_Announcement_Discuss_NoColonist".Translate(), MessageTypeDefOf.RejectInput, false);
                    return false;
                }

                // 构建讨论提示词
                string prompt = BuildGroupDiscussionPrompt(item, leader, participants);

                // Announcement 模式：AddFirst 优先 + UserRequestPool 每秒消费 + 最多 8 人上下文
                leaderState.AddTalkRequest(prompt, leader, TalkType.Announcement);

                // 记录历史并把领导者的话以气泡形式立即显示（与上游 ExecuteDialogue 非 player 分支一致）
                ApiLog apiLog = ApiHistory.AddUserHistory(leader, leader, prompt, TalkType.Announcement);
                TalkResponse talkResponse = new TalkResponse(TalkType.Announcement, leader.LabelShort, prompt)
                {
                    Id = apiLog.Id
                };
                leaderState.TalkResponses.Insert(0, talkResponse);

                // 显示成功消息
                Messages.Message("RTE_GroupDiscussion_Started".Translate(leader.LabelShortCap, item.Title), MessageTypeDefOf.TaskCompletion, false);

                DebugLog.Log($"Group discussion started: {leader.LabelShort} leading announcement about '{item.Title}' with {participants.Count} participants");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk Enhance] Failed to start group discussion: {ex}");
                Messages.Message("RTE_Announcement_Discuss_Failed".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }
        }

        /// <summary>
        /// 构建群体讨论提示词
        /// </summary>
        private static string BuildGroupDiscussionPrompt(ColonyAnnouncement item, Pawn leader, List<Pawn> participants)
        {
            string categoryLabel = GetCategoryLabel(item.Category);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[群体讨论] {leader.LabelShortCap}召集大家讨论{categoryLabel}「{item.Title}」");

            if (!string.IsNullOrWhiteSpace(item.Description))
            {
                sb.AppendLine($"详情: {item.Description}");
            }

            // 添加额外信息
            if (item.Category == AnnouncementCategory.Project && item.Progress > 0)
            {
                sb.AppendLine($"当前进度: {item.Progress:P0}");
            }

            if (!string.IsNullOrWhiteSpace(item.AssignedPawnName))
            {
                sb.AppendLine($"负责人: {item.AssignedPawnName}");
            }

            if (item.Status == AnnouncementStatus.Completed)
            {
                sb.AppendLine("状态: 已完成");
            }
            else if (item.Status == AnnouncementStatus.Paused)
            {
                sb.AppendLine("状态: 已暂停");
            }

            // 列出参与者
            var allNames = new List<string> { leader.LabelShortCap };
            allNames.AddRange(participants.Where(p => p != leader).Select(p => p.LabelShortCap));
            sb.AppendLine($"参与者: {string.Join(", ", allNames)}");

            sb.AppendLine("请各位就此事发表看法，展开讨论。");

            return sb.ToString();
        }

        /// <summary>
        /// 获取类别的显示标签
        /// </summary>
        private static string GetCategoryLabel(AnnouncementCategory cat)
        {
            return cat switch
            {
                AnnouncementCategory.Project => "工程",
                AnnouncementCategory.Event => "事件",
                AnnouncementCategory.Quest => "任务",
                AnnouncementCategory.Resource => "资源",
                AnnouncementCategory.Personnel => "人员",
                AnnouncementCategory.Custom => "事项",
                _ => "事项"
            };
        }
    }
}
