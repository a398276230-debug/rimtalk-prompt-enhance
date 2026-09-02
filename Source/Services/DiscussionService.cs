using System;
using System.Collections.Generic;
using System.Linq;
using RimTalk.Data;
using RimTalk.Service;
using RimTalk.Source.Data;
using RimTalk.UI;
using RimWorld;
using Verse;
using RTSettings = RimTalk.Settings;

namespace RimTalkHealthEnhance.Services
{
    /// <summary>
    /// 负责与RimTalk通信，发起关于通告条目的讨论。
    /// 直接引用 RimTalk v1.2.0 API（csproj 已引用 RimTalk.dll，About.xml 已声明依赖），
    /// 调用链与上游 CustomDialogueService.ExecuteDialogue 保持一致。
    /// </summary>
    public static class DiscussionService
    {
        /// <summary>
        /// 获取可参与讨论的殖民者列表
        /// </summary>
        public static List<Pawn> GetAvailableColonists()
        {
            var result = new List<Pawn>();

            if (Find.CurrentMap == null)
                return result;

            // 获取所有自由的殖民者
            foreach (var pawn in Find.CurrentMap.mapPawns.FreeColonistsSpawned)
            {
                if (pawn == null || pawn.Dead || !pawn.Spawned)
                    continue;

                // RimTalk 可生成对话检查（CanGenerateTalk 内部含 Awake 检查）
                if (Cache.Get(pawn)?.CanGenerateTalk() != true)
                    continue;

                // 基本检查：醒着且未被征召
                if (!pawn.Awake())
                    continue;

                result.Add(pawn);
            }

            return result;
        }

        /// <summary>
        /// 随机选择一个可用的殖民者
        /// </summary>
        public static Pawn SelectRandomColonist()
        {
            var colonists = GetAvailableColonists();
            if (!colonists.Any())
                return null;

            return colonists.RandomElement();
        }

        /// <summary>
        /// 发起关于通告条目的讨论（玩家对殖民者说话）。
        /// 调用链与上游 CustomDialogueService.ExecuteDialogue 的玩家路径一致：
        /// 会话号 -> AddTalkRequest(User) -> 回填ConversationId -> 抢占 -> AddUserHistory(带会话号) -> SpokenTick -> NotifyLogUpdated
        /// </summary>
        /// <param name="recipient">接收讨论的殖民者</param>
        /// <param name="item">要讨论的通告条目</param>
        /// <returns>是否成功发起讨论</returns>
        public static bool StartDiscussion(Pawn recipient, ColonyAnnouncement item)
        {
            if (recipient == null || item == null)
            {
                Log.Warning("[RimTalk Enhance] StartDiscussion: recipient or item is null");
                return false;
            }

            try
            {
                // 获取玩家Pawn
                var playerPawn = Cache.GetPlayer();
                if (playerPawn == null)
                {
                    Log.Warning("[RimTalk Enhance] Could not get player pawn");
                    Messages.Message("RTE_Announcement_Discuss_Failed".Translate(), MessageTypeDefOf.RejectInput, false);
                    return false;
                }

                // 获取接收者的PawnState（检查是否可以说话）
                var recipientState = Cache.Get(recipient);
                if (recipientState == null || !recipientState.CanDisplayTalk())
                {
                    Log.Warning($"[RimTalk Enhance] {recipient.LabelShort} cannot display talk");
                    Messages.Message("RTE_Announcement_Discuss_NoColonist".Translate(), MessageTypeDefOf.RejectInput, false);
                    return false;
                }

                // 构建讨论提示词
                string prompt = BuildDiscussionPrompt(item);

                // 会话链（RimTalk v1.2）：玩家路径领新会话号（与上游 ExecuteDialogue 一致）
                int conversationId = ApiHistory.NextConversationId();

                // 对接收者添加TalkRequest（玩家作为发起者；User 类型会 AddFirst 优先处理并进入 UserRequestPool）
                recipientState.AddTalkRequest(prompt, playerPawn, TalkType.User);
                if (recipientState.TalkRequests.First != null)
                {
                    recipientState.TalkRequests.First.Value.ConversationId = conversationId;
                }

                // AI 正在生成时抢占（玩家输入优先，与上游一致）
                if (AIService.IsBusy())
                {
                    AIService.CancelCurrent();
                }

                // 记录玩家输入历史（带会话号），标记已说出口并刷新对话悬浮层（与上游一致）
                ApiLog apiLog = ApiHistory.AddUserHistory(playerPawn, recipient, prompt, TalkType.User, null, conversationId);
                apiLog.SpokenTick = GenTicks.TicksGame;
                Overlay.NotifyLogUpdated();

                // 显示成功消息
                string playerName = GetPlayerName();
                Messages.Message("RTE_Announcement_Discuss_Started".Translate(playerName ?? "玩家", item.Title), MessageTypeDefOf.TaskCompletion, false);

                DebugLog.Log($"Discussion started: Player asking {recipient.LabelShort} about '{item.Title}'");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk Enhance] Failed to start discussion: {ex}");
                Messages.Message("RTE_Announcement_Discuss_Failed".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }
        }

        /// <summary>
        /// 获取玩家名称
        /// </summary>
        private static string GetPlayerName()
        {
            try
            {
                return RTSettings.Get()?.PlayerName ?? "Player";
            }
            catch
            {
                return "Player";
            }
        }

        /// <summary>
        /// 发起小人自己的公开讨论（小人作为公告者向周围广播，附近殖民者自动参与）。
        /// 底层使用上游原生 Announcement 模式：AddTalkRequest(Announcement) 会
        /// AddFirst 优先处理 + UserRequestPool 每秒消费 + 最多 8 人上下文。
        /// </summary>
        /// <param name="initiator">发起讨论的殖民者</param>
        /// <param name="item">要讨论的通告条目</param>
        /// <returns>是否成功发起讨论</returns>
        public static bool StartPawnSelfDiscussion(Pawn initiator, ColonyAnnouncement item)
        {
            if (initiator == null || item == null)
            {
                Log.Warning("[RimTalk Enhance] StartPawnSelfDiscussion: initiator or item is null");
                return false;
            }

            try
            {
                // 获取发起者的PawnState
                var initiatorState = Cache.Get(initiator);
                if (initiatorState == null || !initiatorState.CanDisplayTalk())
                {
                    Log.Warning($"[RimTalk Enhance] {initiator.LabelShort} cannot display talk");
                    Messages.Message("RTE_Announcement_Discuss_NoColonist".Translate(), MessageTypeDefOf.RejectInput, false);
                    return false;
                }

                // 构建小人自己发起讨论的提示词（与玩家发起的不同）
                string prompt = BuildPawnSelfDiscussionPrompt(item);

                // 会话链（RimTalk v1.2）：Announcement 非独白路径领新会话号（与上游 ExecuteDialogue 一致）
                int conversationId = ApiHistory.NextConversationId();

                // 通告模式广播：recipient 传发起者自身，与上游 gizmo 通告路径一致
                // (TalkRequest(message, pawn, pawn, TalkType.Announcement))
                initiatorState.AddTalkRequest(prompt, initiator, TalkType.Announcement);
                if (initiatorState.TalkRequests.First != null)
                {
                    initiatorState.TalkRequests.First.Value.ConversationId = conversationId;
                }

                // AI 正在生成时抢占（Announcement 优先，与上游一致）
                if (AIService.IsBusy())
                {
                    AIService.CancelCurrent();
                }

                // 记录历史（带会话号）并把发起者的话以气泡形式立即显示（与上游 ExecuteDialogue 非 player 分支一致）
                ApiLog apiLog = ApiHistory.AddUserHistory(initiator, initiator, prompt, TalkType.Announcement, null, conversationId);
                TalkResponse talkResponse = new TalkResponse(TalkType.Announcement, initiator.LabelShort, prompt)
                {
                    Id = apiLog.Id
                };
                initiatorState.TalkResponses.Insert(0, talkResponse);

                // 显示成功消息
                Messages.Message("RTE_Announcement_Discuss_PawnStarted".Translate(initiator.LabelShortCap, item.Title), MessageTypeDefOf.TaskCompletion, false);

                DebugLog.Log($"Pawn self-discussion started: {initiator.LabelShort} discussing '{item.Title}'");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk Enhance] Failed to start pawn self-discussion: {ex}");
                Messages.Message("RTE_Announcement_Discuss_Failed".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }
        }

        /// <summary>
        /// 构建小人自己发起讨论的提示词
        /// </summary>
        private static string BuildPawnSelfDiscussionPrompt(ColonyAnnouncement item)
        {
            string categoryLabel = GetCategoryLabel(item.Category);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[公开讨论] 关于{categoryLabel}「{item.Title}」");

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

            sb.AppendLine("请公开发表你对这件事的看法，与附近的同伴一起讨论。");

            return sb.ToString();
        }

        /// <summary>
        /// 构建讨论提示词（玩家发起）
        /// </summary>
        private static string BuildDiscussionPrompt(ColonyAnnouncement item)
        {
            string categoryLabel = GetCategoryLabel(item.Category);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[讨论请求] 关于{categoryLabel}「{item.Title}」");

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

            sb.AppendLine("请就此事发表看法或提供建议。");

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
