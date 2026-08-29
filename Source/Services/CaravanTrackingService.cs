using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace RimTalkHealthEnhance
{
    /// <summary>
    /// 商队事件追踪服务
    /// 追踪商队到达和离开地图，自动完成商队事件
    /// </summary>
    public static class CaravanTrackingService
    {
        // 商队关键词（用于识别商队事件，作为降级匹配）
        private static readonly string[] CaravanKeywords = {
            "caravan", "trader", "visitor", "traveler", "merchant",
            "商队", "商人", "访客", "旅行者", "贸易"
        };
        
        // 商队 LordJob 类型名称关键词
        private static readonly string[] CaravanLordJobKeywords = {
            "Trade", "Visit", "Defend", "Travel", "Assist"
        };
        
        // 当前追踪的商队 Lord ID -> 关联的事件 ID
        private static Dictionary<int, string> _trackedCaravanLords = new Dictionary<int, string>();
        
        // 当前活跃的商队事件
        private static HashSet<string> _activeCaravanEventIds = new HashSet<string>();
        
        /// <summary>
        /// 判断事件是否为商队事件（支持 LetterDef 和 Lord 判断）
        /// </summary>
        public static bool IsCaravanEvent(string title, LetterDef def = null)
        {
            // 优先检查地图上是否有商队 Lord（最准确的判断）
            var map = Find.CurrentMap;
            if (map != null)
            {
                bool hasCaravanLord = map.lordManager.lords.Any(l => IsCaravanLord(l));
                if (hasCaravanLord)
                {
                    // 再检查 LetterDef 是否是正面或中立事件（排除威胁类型）
                    if (def != null)
                    {
                        if (def == LetterDefOf.PositiveEvent || def == LetterDefOf.NeutralEvent)
                            return true;
                    }
                }
            }
            
            // 降级使用标题关键词匹配
            if (string.IsNullOrEmpty(title)) return false;
            return CaravanKeywords.Any(k => title.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        /// <summary>
        /// 兼容旧代码的方法重载
        /// </summary>
        public static bool IsCaravanEvent(string title)
        {
            return IsCaravanEvent(title, null);
        }
        
        /// <summary>
        /// 判断 Lord 是否为商队类型
        /// </summary>
        public static bool IsCaravanLord(Lord lord)
        {
            if (lord?.LordJob == null) return false;
            
            // 检查派系是否非敌对（商队应该是友好或中立的）
            if (lord.faction != null && lord.faction.HostileTo(Faction.OfPlayer))
            {
                return false;
            }
            
            // 检查 LordJob 类型名称
            string jobTypeName = lord.LordJob.GetType().Name;
            
            foreach (var keyword in CaravanLordJobKeywords)
            {
                if (jobTypeName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 设置商队事件为活跃（由 EventCaptureService 调用）
        /// </summary>
        public static void SetActiveCaravanEvent(ColonyAnnouncement caravanEvent)
        {
            if (caravanEvent == null) return;
            
            caravanEvent.IsCaravanEvent = true;
            _activeCaravanEventIds.Add(caravanEvent.Id);
            
            DebugLog.Log($"Caravan event set to active: '{caravanEvent.Title}' (ID: {caravanEvent.Id})");
            
            // 尝试关联当前地图上的商队 Lord
            TryAssociateLords(caravanEvent);
        }
        
        /// <summary>
        /// 尝试将事件与地图上的商队 Lord 关联
        /// </summary>
        private static void TryAssociateLords(ColonyAnnouncement caravanEvent)
        {
            var map = Find.CurrentMap;
            if (map == null) return;
            
            foreach (var lord in map.lordManager.lords)
            {
                if (IsCaravanLord(lord) && !_trackedCaravanLords.ContainsKey(lord.loadID))
                {
                    _trackedCaravanLords[lord.loadID] = caravanEvent.Id;
                    
                    // 记录商队成员数量
                    int memberCount = lord.ownedPawns?.Count ?? 0;
                    string factionName = lord.faction?.Name ?? "Unknown";
                    
                    DebugLog.Log($"Associated caravan Lord (ID: {lord.loadID}) with event '{caravanEvent.Title}'. " +
                               $"Faction: {factionName}, Members: {memberCount}");
                }
            }
        }
        
        /// <summary>
        /// 获取当前活跃的商队事件列表
        /// </summary>
        public static List<ColonyAnnouncement> GetActiveCaravanEvents()
        {
            var manager = ColonyAnnouncementManager.Instance;
            if (manager?.Data?.Announcements == null) return new List<ColonyAnnouncement>();
            
            return manager.Data.Announcements
                .Where(a => a.Status == AnnouncementStatus.Active && a.IsCaravanEvent)
                .ToList();
        }
        
        /// <summary>
        /// 检查商队是否已离开（定期调用）
        /// </summary>
        public static void CheckCaravanDepartures()
        {
            var settings = RimTalkHealthEnhanceMod.Settings;
            if (settings == null || !settings.AutoCompleteCaravanEvents) return;
            
            var map = Find.CurrentMap;
            if (map == null) return;
            
            // 获取当前地图上所有商队 Lord
            var currentCaravanLordIds = new HashSet<int>();
            foreach (var lord in map.lordManager.lords)
            {
                if (IsCaravanLord(lord))
                {
                    currentCaravanLordIds.Add(lord.loadID);
                }
            }
            
            // 检查已追踪的商队是否已离开
            var departedLords = _trackedCaravanLords.Keys
                .Where(id => !currentCaravanLordIds.Contains(id))
                .ToList();
            
            foreach (var lordId in departedLords)
            {
                string eventId = _trackedCaravanLords[lordId];
                _trackedCaravanLords.Remove(lordId);
                
                DebugLog.Log($"Caravan Lord (ID: {lordId}) has departed. Associated event ID: {eventId}");
            }
            
            // 如果所有关联的商队都离开了，完成事件
            var eventsToComplete = new List<string>();
            foreach (var eventId in _activeCaravanEventIds.ToList())
            {
                // 检查是否还有关联的商队 Lord
                bool hasAssociatedLord = _trackedCaravanLords.Values.Contains(eventId);
                
                if (!hasAssociatedLord)
                {
                    // 再检查地图上是否还有任何友好派系的商队
                    bool hasAnyCaravan = map.lordManager.lords.Any(l => IsCaravanLord(l));
                    
                    if (!hasAnyCaravan)
                    {
                        eventsToComplete.Add(eventId);
                    }
                }
            }
            
            // 完成事件
            foreach (var eventId in eventsToComplete)
            {
                CompleteCaravanEvent(eventId);
            }
        }
        
        /// <summary>
        /// 完成商队事件
        /// </summary>
        private static void CompleteCaravanEvent(string eventId)
        {
            var manager = ColonyAnnouncementManager.Instance;
            if (manager?.Data?.Announcements == null) return;
            
            var caravanEvent = manager.Data.Announcements.FirstOrDefault(a => a.Id == eventId);
            if (caravanEvent == null || caravanEvent.Status != AnnouncementStatus.Active) return;
            
            int currentTick = Find.TickManager.TicksGame;
            
            // 生成完成报告
            string report = GenerateCaravanReport(caravanEvent);
            if (!string.IsNullOrEmpty(report))
            {
                if (string.IsNullOrEmpty(caravanEvent.Description))
                {
                    caravanEvent.Description = report;
                }
                else
                {
                    caravanEvent.Description += "\n" + report;
                }
            }
            
            caravanEvent.Status = AnnouncementStatus.Completed;
            caravanEvent.CompletedTick = currentTick;
            
            _activeCaravanEventIds.Remove(eventId);
            
            DebugLog.Log($"Auto-completed caravan event: '{caravanEvent.Title}'. {report}");
            
            manager.NotifyDataChanged();
        }
        
        /// <summary>
        /// 生成商队报告
        /// </summary>
        private static string GenerateCaravanReport(ColonyAnnouncement caravanEvent)
        {
            return "[商队离开] 商队已安全离开殖民地。";
        }
        
        /// <summary>
        /// 当 Lord 被移除时调用（由 LordMonitorService 或 Patch 调用）
        /// </summary>
        public static void OnLordRemoved(Lord lord)
        {
            if (lord == null) return;
            
            if (_trackedCaravanLords.ContainsKey(lord.loadID))
            {
                string eventId = _trackedCaravanLords[lord.loadID];
                _trackedCaravanLords.Remove(lord.loadID);
                
                DebugLog.Log($"Caravan Lord (ID: {lord.loadID}) removed. Scheduling completion check...");
                
                // 延迟检查是否需要完成事件（给其他商队一些时间）
                var manager = ColonyAnnouncementManager.Instance;
                manager?.ScheduleCaravanCheck(60); // 1秒后检查
            }
        }
        
        /// <summary>
        /// 当新 Lord 被添加时调用（由 LordMonitorService 调用）
        /// </summary>
        public static void OnLordAdded(Lord lord)
        {
            if (lord == null || !IsCaravanLord(lord)) return;
            
            // 检查是否有活跃的商队事件可以关联
            var activeEvents = GetActiveCaravanEvents();
            if (activeEvents.Count == 0) return;
            
            // 关联到最新的商队事件
            var latestEvent = activeEvents.OrderByDescending(e => e.CreatedTick).First();
            
            if (!_trackedCaravanLords.ContainsKey(lord.loadID))
            {
                _trackedCaravanLords[lord.loadID] = latestEvent.Id;
                
                int memberCount = lord.ownedPawns?.Count ?? 0;
                string factionName = lord.faction?.Name ?? "Unknown";
                
                DebugLog.Log($"New caravan Lord detected (ID: {lord.loadID}). " +
                           $"Faction: {factionName}, Members: {memberCount}. Associated with event '{latestEvent.Title}'");
            }
        }
        
        /// <summary>
        /// 清除所有追踪数据（游戏加载时调用）
        /// </summary>
        public static void ClearTracking()
        {
            _trackedCaravanLords.Clear();
            _activeCaravanEventIds.Clear();
        }
        
        /// <summary>
        /// 获取调试信息
        /// </summary>
        public static string GetDebugInfo()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Active Caravan Events: {_activeCaravanEventIds.Count}");
            sb.AppendLine($"Tracked Caravan Lords: {_trackedCaravanLords.Count}");
            
            foreach (var kvp in _trackedCaravanLords)
            {
                sb.AppendLine($"  - Lord ID {kvp.Key} -> Event ID {kvp.Value}");
            }
            
            return sb.ToString();
        }
    }
}