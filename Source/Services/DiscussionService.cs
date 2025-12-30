using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;

namespace RimTalkHealthEnhance.Services
{
    /// <summary>
    /// 负责与RimTalk通信，发起关于通告条目的讨论
    /// </summary>
    public static class DiscussionService
    {
        // 反射缓存
        private static bool _initialized = false;
        private static bool _available = false;
        
        private static Type _cacheType;
        private static Type _pawnStateType;
        private static Type _talkTypeEnum;
        
        private static MethodInfo _cacheGetMethod;
        private static MethodInfo _cacheGetPlayerMethod;
        private static MethodInfo _addTalkRequestMethod;
        private static MethodInfo _canGenerateTalkMethod;
        private static MethodInfo _canDisplayTalkMethod;
        
        // ApiHistory 和 Overlay 相关
        private static Type _apiHistoryType;
        private static Type _overlayType;
        private static Type _settingsType;
        private static MethodInfo _addUserHistoryMethod;
        private static MethodInfo _notifyLogUpdatedMethod;
        private static MethodInfo _settingsGetMethod;
        private static PropertyInfo _playerNameProperty;
        
        private static object _talkTypeUser;
        
        /// <summary>
        /// 初始化反射缓存
        /// </summary>
        public static bool Initialize()
        {
            if (_initialized)
                return _available;
            
            _initialized = true;
            
            try
            {
                // 查找RimTalk程序集
                Assembly rimTalkAssembly = null;
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (assembly.GetName().Name == "RimTalk")
                    {
                        rimTalkAssembly = assembly;
                        break;
                    }
                }
                
                if (rimTalkAssembly == null)
                {
                    Log.Warning("[RimTalk Enhance] RimTalk assembly not found. Discussion feature disabled.");
                    return false;
                }
                
                // 获取Cache类型
                _cacheType = rimTalkAssembly.GetType("RimTalk.Data.Cache");
                if (_cacheType == null)
                {
                    Log.Warning("[RimTalk Enhance] RimTalk.Data.Cache type not found.");
                    return false;
                }
                
                // 获取PawnState类型
                _pawnStateType = rimTalkAssembly.GetType("RimTalk.Data.PawnState");
                if (_pawnStateType == null)
                {
                    Log.Warning("[RimTalk Enhance] RimTalk.Data.PawnState type not found.");
                    return false;
                }
                
                // 获取TalkType枚举
                _talkTypeEnum = rimTalkAssembly.GetType("RimTalk.Source.Data.TalkType");
                if (_talkTypeEnum == null)
                {
                    Log.Warning("[RimTalk Enhance] RimTalk.Source.Data.TalkType enum not found.");
                    return false;
                }
                
                // 获取Cache.Get方法
                _cacheGetMethod = _cacheType.GetMethod("Get", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(Pawn) }, null);
                if (_cacheGetMethod == null)
                {
                    Log.Warning("[RimTalk Enhance] Cache.Get method not found.");
                    return false;
                }
                
                // 获取Cache.GetPlayer方法（获取玩家Pawn）
                _cacheGetPlayerMethod = _cacheType.GetMethod("GetPlayer", BindingFlags.Public | BindingFlags.Static);
                if (_cacheGetPlayerMethod == null)
                {
                    Log.Warning("[RimTalk Enhance] Cache.GetPlayer method not found.");
                    return false;
                }
                
                // 获取PawnState.AddTalkRequest方法
                _addTalkRequestMethod = _pawnStateType.GetMethod("AddTalkRequest", BindingFlags.Public | BindingFlags.Instance);
                if (_addTalkRequestMethod == null)
                {
                    Log.Warning("[RimTalk Enhance] PawnState.AddTalkRequest method not found.");
                    return false;
                }
                
                // 获取PawnState.CanGenerateTalk方法
                _canGenerateTalkMethod = _pawnStateType.GetMethod("CanGenerateTalk", BindingFlags.Public | BindingFlags.Instance);
                if (_canGenerateTalkMethod == null)
                {
                    Log.Warning("[RimTalk Enhance] PawnState.CanGenerateTalk method not found.");
                    return false;
                }
                
                // 获取PawnState.CanDisplayTalk方法
                _canDisplayTalkMethod = _pawnStateType.GetMethod("CanDisplayTalk", BindingFlags.Public | BindingFlags.Instance);
                if (_canDisplayTalkMethod == null)
                {
                    Log.Warning("[RimTalk Enhance] PawnState.CanDisplayTalk method not found.");
                    return false;
                }
                
                // 获取ApiHistory类型和方法
                _apiHistoryType = rimTalkAssembly.GetType("RimTalk.Data.ApiHistory");
                if (_apiHistoryType != null)
                {
                    _addUserHistoryMethod = _apiHistoryType.GetMethod("AddUserHistory", BindingFlags.Public | BindingFlags.Static);
                }
                
                // 获取Overlay类型和方法
                _overlayType = rimTalkAssembly.GetType("RimTalk.UI.Overlay");
                if (_overlayType != null)
                {
                    _notifyLogUpdatedMethod = _overlayType.GetMethod("NotifyLogUpdated", BindingFlags.Public | BindingFlags.Static);
                }
                
                // 获取Settings类型和PlayerName属性
                _settingsType = rimTalkAssembly.GetType("RimTalk.Settings");
                if (_settingsType != null)
                {
                    _settingsGetMethod = _settingsType.GetMethod("Get", BindingFlags.Public | BindingFlags.Static);
                }
                
                // 获取TalkType.User枚举值
                _talkTypeUser = Enum.Parse(_talkTypeEnum, "User");
                
                _available = true;
                Log.Message("[RimTalk Enhance] DiscussionService initialized successfully.");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk Enhance] Failed to initialize DiscussionService: {ex}");
                return false;
            }
        }
        
        /// <summary>
        /// 检查服务是否可用
        /// </summary>
        public static bool IsAvailable()
        {
            if (!_initialized)
                Initialize();
            return _available;
        }
        
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
                
                // 检查是否可以生成对话（如果RimTalk可用）
                if (_available && !CanPawnGenerateTalk(pawn))
                    continue;
                
                // 基本检查：醒着且未被征召
                if (!pawn.Awake())
                    continue;
                
                result.Add(pawn);
            }
            
            return result;
        }
        
        /// <summary>
        /// 检查Pawn是否可以生成对话
        /// </summary>
        private static bool CanPawnGenerateTalk(Pawn pawn)
        {
            if (!_available)
                return true;
            
            try
            {
                var pawnState = _cacheGetMethod.Invoke(null, new object[] { pawn });
                if (pawnState == null)
                    return false;
                
                var result = _canGenerateTalkMethod.Invoke(pawnState, null);
                return result != null && (bool)result;
            }
            catch
            {
                return true; // 出错时假设可以
            }
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
        /// 发起关于通告条目的讨论（玩家对殖民者说话）
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
            
            if (!IsAvailable())
            {
                Messages.Message("RTE_Announcement_Discuss_Failed".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }
            
            try
            {
                // 获取玩家Pawn
                var playerPawn = _cacheGetPlayerMethod.Invoke(null, null) as Pawn;
                if (playerPawn == null)
                {
                    Log.Warning("[RimTalk Enhance] Could not get player pawn");
                    Messages.Message("RTE_Announcement_Discuss_Failed".Translate(), MessageTypeDefOf.RejectInput, false);
                    return false;
                }
                
                // 获取玩家的PawnState（检查是否可以说话）
                var playerState = _cacheGetMethod.Invoke(null, new object[] { playerPawn });
                if (playerState == null)
                {
                    Log.Warning("[RimTalk Enhance] Could not get PawnState for player");
                    Messages.Message("RTE_Announcement_Discuss_Failed".Translate(), MessageTypeDefOf.RejectInput, false);
                    return false;
                }
                
                // 检查玩家是否可以说话
                var canDisplayResult = _canDisplayTalkMethod.Invoke(playerState, null);
                if (canDisplayResult == null || !(bool)canDisplayResult)
                {
                    Log.Warning("[RimTalk Enhance] Player cannot display talk");
                    Messages.Message("RTE_Announcement_Discuss_Failed".Translate(), MessageTypeDefOf.RejectInput, false);
                    return false;
                }
                
                // 获取接收者的PawnState
                var recipientState = _cacheGetMethod.Invoke(null, new object[] { recipient });
                if (recipientState == null)
                {
                    Log.Warning($"[RimTalk Enhance] Could not get PawnState for {recipient.LabelShort}");
                    Messages.Message("RTE_Announcement_Discuss_Failed".Translate(), MessageTypeDefOf.RejectInput, false);
                    return false;
                }
                
                // 检查接收者是否可以说话
                var recipientCanDisplay = _canDisplayTalkMethod.Invoke(recipientState, null);
                if (recipientCanDisplay == null || !(bool)recipientCanDisplay)
                {
                    Log.Warning($"[RimTalk Enhance] {recipient.LabelShort} cannot display talk");
                    Messages.Message("RTE_Announcement_Discuss_NoColonist".Translate(), MessageTypeDefOf.RejectInput, false);
                    return false;
                }
                
                // 构建讨论提示词
                string prompt = BuildDiscussionPrompt(item);
                
                // 对接收者添加TalkRequest（玩家作为发起者）
                // 方法签名: AddTalkRequest(string prompt, Pawn recipient = null, TalkType talkType = TalkType.Other)
                // 注意：这里recipient参数是对话的另一方，即玩家
                _addTalkRequestMethod.Invoke(recipientState, new object[] { prompt, playerPawn, _talkTypeUser });
                
                // 获取玩家名称并记录到ApiHistory
                string playerName = GetPlayerName();
                if (_addUserHistoryMethod != null && !string.IsNullOrEmpty(playerName))
                {
                    try
                    {
                        var apiLog = _addUserHistoryMethod.Invoke(null, new object[] { playerName, prompt });
                        
                        // 设置SpokenTick
                        if (apiLog != null)
                        {
                            var spokenTickProp = apiLog.GetType().GetProperty("SpokenTick");
                            if (spokenTickProp != null)
                            {
                                spokenTickProp.SetValue(apiLog, GenTicks.TicksGame);
                            }
                        }
                        
                        // 通知Overlay更新
                        if (_notifyLogUpdatedMethod != null)
                        {
                            _notifyLogUpdatedMethod.Invoke(null, null);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"[RimTalk Enhance] Failed to add user history: {ex.Message}");
                    }
                }
                
                // 显示成功消息
                Messages.Message("RTE_Announcement_Discuss_Started".Translate(playerName ?? "玩家", item.Title), MessageTypeDefOf.TaskCompletion, false);
                
                Log.Message($"[RimTalk Enhance] Discussion started: Player asking {recipient.LabelShort} about '{item.Title}'");
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
                if (_settingsGetMethod == null)
                    return "Player";
                
                var settings = _settingsGetMethod.Invoke(null, null);
                if (settings == null)
                    return "Player";
                
                var playerNameProp = settings.GetType().GetProperty("PlayerName");
                if (playerNameProp == null)
                    return "Player";
                
                return playerNameProp.GetValue(settings) as string ?? "Player";
            }
            catch
            {
                return "Player";
            }
        }
        
        /// <summary>
        /// 构建讨论提示词
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