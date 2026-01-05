using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace RimTalkHealthEnhance.Services
{
    /// <summary>
    /// 群体讨论服务 - 让多个殖民者同时参与讨论一个话题
    /// 通过反射调用RimTalk的TalkService实现多人对话生成
    /// </summary>
    public static class GroupDiscussionService
    {
        // 反射缓存
        private static bool _initialized = false;
        private static bool _available = false;
        
        // RimTalk核心类型
        private static Type _talkServiceType;
        private static Type _talkRequestType;
        private static Type _talkTypeEnum;
        private static Type _promptServiceType;
        private static Type _cacheType;
        private static Type _pawnStateType;
        private static Type _aiServiceType;
        
        // 方法缓存
        private static MethodInfo _generateTalkMethod;
        private static MethodInfo _generateTalkDebugMethod;
        private static MethodInfo _buildContextMethod;
        private static MethodInfo _decoratePromptMethod;
        private static MethodInfo _cacheGetMethod;
        private static MethodInfo _canGenerateTalkMethod;
        private static MethodInfo _canDisplayTalkMethod;
        private static MethodInfo _isBusyMethod;
        
        // TalkType枚举值
        private static object _talkTypeUser;
        private static object _talkTypeOther;
        private static object _talkTypeEvent;
        
        // 构造函数缓存
        private static ConstructorInfo _talkRequestConstructor;
        
        // 属性/字段缓存
        private static PropertyInfo _contextProperty;
        private static PropertyInfo _promptProperty;
        private static FieldInfo _isMonologueField;
        
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
                    Log.Warning("[RimTalk Enhance] RimTalk assembly not found. Group discussion feature disabled.");
                    return false;
                }
                
                // 获取TalkService类型
                _talkServiceType = rimTalkAssembly.GetType("RimTalk.Service.TalkService");
                if (_talkServiceType == null)
                {
                    Log.Warning("[RimTalk Enhance] RimTalk.Service.TalkService type not found.");
                    return false;
                }
                
                // 获取TalkRequest类型
                _talkRequestType = rimTalkAssembly.GetType("RimTalk.Data.TalkRequest");
                if (_talkRequestType == null)
                {
                    Log.Warning("[RimTalk Enhance] RimTalk.Data.TalkRequest type not found.");
                    return false;
                }
                
                // 获取TalkType枚举
                _talkTypeEnum = rimTalkAssembly.GetType("RimTalk.Source.Data.TalkType");
                if (_talkTypeEnum == null)
                {
                    Log.Warning("[RimTalk Enhance] RimTalk.Source.Data.TalkType enum not found.");
                    return false;
                }
                
                // 获取PromptService类型
                _promptServiceType = rimTalkAssembly.GetType("RimTalk.Service.PromptService");
                if (_promptServiceType == null)
                {
                    Log.Warning("[RimTalk Enhance] RimTalk.Service.PromptService type not found.");
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
                
                // 获取AIService类型
                _aiServiceType = rimTalkAssembly.GetType("RimTalk.Service.AIService");
                if (_aiServiceType == null)
                {
                    Log.Warning("[RimTalk Enhance] RimTalk.Service.AIService type not found.");
                    return false;
                }
                
                // 获取TalkService.GenerateTalk方法
                _generateTalkMethod = _talkServiceType.GetMethod("GenerateTalk", BindingFlags.Public | BindingFlags.Static);
                if (_generateTalkMethod == null)
                {
                    Log.Warning("[RimTalk Enhance] TalkService.GenerateTalk method not found.");
                    return false;
                }
                
                // 获取TalkService.GenerateTalkDebug方法（备用）
                _generateTalkDebugMethod = _talkServiceType.GetMethod("GenerateTalkDebug", BindingFlags.Public | BindingFlags.Static);
                
                // 获取PromptService.BuildContext方法
                _buildContextMethod = _promptServiceType.GetMethod("BuildContext", BindingFlags.Public | BindingFlags.Static);
                
                // 获取PromptService.DecoratePrompt方法
                _decoratePromptMethod = _promptServiceType.GetMethod("DecoratePrompt", BindingFlags.Public | BindingFlags.Static);
                
                // 获取Cache.Get方法
                _cacheGetMethod = _cacheType.GetMethod("Get", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(Pawn) }, null);
                
                // 获取AIService.IsBusy方法
                _isBusyMethod = _aiServiceType.GetMethod("IsBusy", BindingFlags.Public | BindingFlags.Static);
                
                // 获取PawnState方法
                if (_pawnStateType != null)
                {
                    _canGenerateTalkMethod = _pawnStateType.GetMethod("CanGenerateTalk", BindingFlags.Public | BindingFlags.Instance);
                    _canDisplayTalkMethod = _pawnStateType.GetMethod("CanDisplayTalk", BindingFlags.Public | BindingFlags.Instance);
                }
                
                // 获取TalkType枚举值
                _talkTypeUser = Enum.Parse(_talkTypeEnum, "User");
                _talkTypeOther = Enum.Parse(_talkTypeEnum, "Other");
                _talkTypeEvent = Enum.Parse(_talkTypeEnum, "Event");
                
                // 获取TalkRequest构造函数
                // TalkRequest(string prompt, Pawn initiator, Pawn recipient = null, TalkType talkType = TalkType.Other)
                _talkRequestConstructor = _talkRequestType.GetConstructor(new Type[] { typeof(string), typeof(Pawn), typeof(Pawn), _talkTypeEnum });
                if (_talkRequestConstructor == null)
                {
                    Log.Warning("[RimTalk Enhance] TalkRequest constructor not found.");
                    return false;
                }
                
                // 获取TalkRequest属性
                _contextProperty = _talkRequestType.GetProperty("Context");
                _promptProperty = _talkRequestType.GetProperty("Prompt");
                _isMonologueField = _talkRequestType.GetField("IsMonologue");
                
                _available = true;
                Log.Message("[RimTalk Enhance] GroupDiscussionService initialized successfully.");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk Enhance] Failed to initialize GroupDiscussionService: {ex}");
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
        /// 检查AI服务是否繁忙
        /// </summary>
        public static bool IsAIBusy()
        {
            if (!IsAvailable() || _isBusyMethod == null)
                return false;
            
            try
            {
                var result = _isBusyMethod.Invoke(null, null);
                return result != null && (bool)result;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// 获取可参与群体讨论的殖民者列表
        /// </summary>
        public static List<Pawn> GetAvailableColonists()
        {
            var result = new List<Pawn>();
            
            if (Find.CurrentMap == null)
                return result;
            
            foreach (var pawn in Find.CurrentMap.mapPawns.FreeColonistsSpawned)
            {
                if (pawn == null || pawn.Dead || !pawn.Spawned)
                    continue;
                
                // 基本检查
                if (!pawn.Awake())
                    continue;
                
                // 检查是否可以显示对话（如果RimTalk可用）
                if (_available && !CanPawnDisplayTalk(pawn))
                    continue;
                
                result.Add(pawn);
            }
            
            return result;
        }
        
        /// <summary>
        /// 检查Pawn是否可以显示对话
        /// </summary>
        private static bool CanPawnDisplayTalk(Pawn pawn)
        {
            if (!_available || _cacheGetMethod == null || _canDisplayTalkMethod == null)
                return true;
            
            try
            {
                var pawnState = _cacheGetMethod.Invoke(null, new object[] { pawn });
                if (pawnState == null)
                    return false;
                
                var result = _canDisplayTalkMethod.Invoke(pawnState, null);
                return result != null && (bool)result;
            }
            catch
            {
                return true;
            }
        }
        
        /// <summary>
        /// 检查Pawn是否可以生成对话
        /// </summary>
        private static bool CanPawnGenerateTalk(Pawn pawn)
        {
            if (!_available || _cacheGetMethod == null || _canGenerateTalkMethod == null)
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
                return true;
            }
        }
        
        
        /// <summary>
        /// 发起群体讨论
        /// </summary>
        /// <param name="item">要讨论的通告条目</param>
        /// <param name="leader">发起者/领导者（第一个发言的人）</param>
        /// <param name="participants">其他参与者</param>
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
            
            if (!IsAvailable())
            {
                Messages.Message("RTE_Announcement_Discuss_Failed".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }
            
            if (IsAIBusy())
            {
                Messages.Message("RTE_GroupDiscussion_AIBusy".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }
            
            try
            {
                // 构建讨论提示词
                string prompt = BuildGroupDiscussionPrompt(item, leader, participants);
                
                // 创建TalkRequest
                // TalkRequest(string prompt, Pawn initiator, Pawn recipient = null, TalkType talkType = TalkType.Other)
                // 使用 TalkType.Other 而不是 User，因为 User 会触发玩家对话模式
                // Other 会让 initiator (leader) 作为对话发起者
                var talkRequest = _talkRequestConstructor.Invoke(new object[] { prompt, leader, null, _talkTypeOther });
                
                if (talkRequest == null)
                {
                    Log.Warning("[RimTalk Enhance] Failed to create TalkRequest");
                    return false;
                }
                
                // 调用GenerateTalk
                var result = _generateTalkMethod.Invoke(null, new object[] { talkRequest });
                
                if (result == null || !(bool)result)
                {
                    // 如果GenerateTalk返回false，尝试使用GenerateTalkDebug
                    if (_generateTalkDebugMethod != null)
                    {
                        Log.Message("[RimTalk Enhance] GenerateTalk returned false, trying GenerateTalkDebug");
                        
                        // 需要先手动构建Context和装饰Prompt
                        var allPawns = new List<Pawn> { leader };
                        allPawns.AddRange(participants.Where(p => p != leader));
                        
                        if (_buildContextMethod != null && _contextProperty != null)
                        {
                            var context = _buildContextMethod.Invoke(null, new object[] { allPawns });
                            _contextProperty.SetValue(talkRequest, context);
                        }
                        
                        _generateTalkDebugMethod.Invoke(null, new object[] { talkRequest });
                        
                        Messages.Message("RTE_GroupDiscussion_Started".Translate(leader.LabelShortCap, item.Title), MessageTypeDefOf.TaskCompletion, false);
                        Log.Message($"[RimTalk Enhance] Group discussion started (debug mode): {leader.LabelShort} leading discussion about '{item.Title}' with {participants.Count} participants");
                        return true;
                    }
                    
                    Log.Warning("[RimTalk Enhance] GenerateTalk returned false");
                    Messages.Message("RTE_Announcement_Discuss_Failed".Translate(), MessageTypeDefOf.RejectInput, false);
                    return false;
                }
                
                // 显示成功消息
                Messages.Message("RTE_GroupDiscussion_Started".Translate(leader.LabelShortCap, item.Title), MessageTypeDefOf.TaskCompletion, false);
                
                Log.Message($"[RimTalk Enhance] Group discussion started: {leader.LabelShort} leading discussion about '{item.Title}' with {participants.Count} participants");
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