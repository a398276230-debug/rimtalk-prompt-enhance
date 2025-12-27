using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace RimTalkHealthEnhance
{
    /// <summary>
    /// Lord 监控服务
    /// 用于监控袭击 Lord 的创建和 Pawn 添加，以获取准确的初始敌人数量
    /// 这主要解决空投/边缘生成时敌人逐个出现导致初始计数不准的问题
    /// </summary>
    public static class LordMonitorService
    {
        // 当前订阅的地图
        private static Map _subscribedMap = null;
        
        // 已知的袭击 Lord 及其 Pawn 数量
        private static Dictionary<int, int> _raidLordPawnCounts = new Dictionary<int, int>();
        
        // 最后一次检测到的最大敌人数
        private static int _maxObservedHostileCount = 0;
        
        // 是否已经开始战斗（有敌人死亡/倒地/逃跑）
        private static bool _combatStarted = false;
        
        // 袭击类型 LordJob 名称关键词
        private static readonly string[] RaidLordJobKeywords = {
            "Assault", "Attack", "Siege", "Besiege", "Raid",
            "Sapper", "Breach", "Mechanoid", "Defend", "Stage"
        };
        
        /// <summary>
        /// 订阅地图的 Lord 事件
        /// </summary>
        public static void SubscribeToMap(Map map)
        {
            if (map == null) return;
            if (_subscribedMap == map) return;
            
            // 取消订阅旧地图
            UnsubscribeFromCurrentMap();
            
            // 订阅新地图
            _subscribedMap = map;
            map.events.LordAdded += OnLordAdded;
            
            Log.Message($"[RimTalk Enhance] LordMonitorService: Subscribed to map events.");
        }
        
        /// <summary>
        /// 取消订阅当前地图
        /// </summary>
        public static void UnsubscribeFromCurrentMap()
        {
            if (_subscribedMap != null)
            {
                _subscribedMap.events.LordAdded -= OnLordAdded;
                _subscribedMap = null;
            }
            
            // 清理状态
            _raidLordPawnCounts.Clear();
            _maxObservedHostileCount = 0;
            _combatStarted = false;
        }
        
        /// <summary>
        /// 重置监控状态（新袭击开始时调用）
        /// </summary>
        public static void ResetMonitoring()
        {
            _raidLordPawnCounts.Clear();
            _maxObservedHostileCount = 0;
            _combatStarted = false;
            Log.Message("[RimTalk Enhance] LordMonitorService: Monitoring reset.");
        }
        
        /// <summary>
        /// 标记战斗已开始（有敌人死亡/倒地/逃跑时调用）
        /// 一旦战斗开始，就锁定初始计数，不再更新
        /// </summary>
        public static void MarkCombatStarted()
        {
            if (!_combatStarted)
            {
                _combatStarted = true;
                Log.Message($"[RimTalk Enhance] LordMonitorService: Combat started. Initial count locked at {_maxObservedHostileCount}.");
            }
        }
        
        /// <summary>
        /// 检查战斗是否已开始
        /// </summary>
        public static bool IsCombatStarted => _combatStarted;
        
        /// <summary>
        /// 获取观察到的最大敌人数
        /// </summary>
        public static int MaxObservedHostileCount => _maxObservedHostileCount;
        
        /// <summary>
        /// Lord 被添加时的回调
        /// </summary>
        private static void OnLordAdded(Lord lord)
        {
            if (lord == null) return;
            
            // 检查是否为袭击类型的 Lord
            if (IsRaidLord(lord))
            {
                int pawnCount = lord.ownedPawns?.Count ?? 0;
                _raidLordPawnCounts[lord.loadID] = pawnCount;
                
                Log.Message($"[RimTalk Enhance] LordMonitorService: Raid Lord detected! " +
                           $"Job: {lord.LordJob?.GetType().Name}, " +
                           $"Faction: {lord.faction?.Name ?? "None"}, " +
                           $"Initial Pawns: {pawnCount}");
                
                // 更新袭击初始计数
                UpdateRaidInitialCount();
            }
            
            // 检查是否为商队类型的 Lord
            if (CaravanTrackingService.IsCaravanLord(lord))
            {
                CaravanTrackingService.OnLordAdded(lord);
            }
        }
        
        /// <summary>
        /// 判断 Lord 是否为袭击类型
        /// </summary>
        private static bool IsRaidLord(Lord lord)
        {
            if (lord?.LordJob == null) return false;
            
            // 检查派系是否敌对
            if (lord.faction != null && !lord.faction.HostileTo(Faction.OfPlayer))
            {
                return false;
            }
            
            // 检查 LordJob 类型名称
            string jobTypeName = lord.LordJob.GetType().Name;
            
            foreach (var keyword in RaidLordJobKeywords)
            {
                if (jobTypeName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 更新袭击初始计数
        /// 如果战斗已开始，则不再更新
        /// 只使用 CountHostileThreats 的结果，不再依赖 Lord.ownedPawns（它可能包含还未落地或已死亡的 Pawn）
        /// </summary>
        public static void UpdateRaidInitialCount()
        {
            if (_combatStarted)
            {
                return;
            }
            
            var raidEvent = RaidTrackingService.GetActiveRaidEvent();
            if (raidEvent == null)
            {
                return;
            }
            
            // 只使用 CountHostileThreats 来获取所有敌对单位
            // 这个方法已经检查了 !Dead && Spawned && !Downed
            int totalHostile = _subscribedMap != null ?
                RaidTrackingService.CountHostileThreats(_subscribedMap) : 0;
            
            // 只增加，不减少（使用最大值策略）
            if (totalHostile > _maxObservedHostileCount)
            {
                int oldMax = _maxObservedHostileCount;
                _maxObservedHostileCount = totalHostile;
                Log.Message($"[RimTalk Enhance] LordMonitorService: Max hostile count updated from {oldMax} to {_maxObservedHostileCount}");
            }
            
            // 更新事件的初始计数（使用最大观察值）
            if (_maxObservedHostileCount > raidEvent.RaidInitialCount)
            {
                int oldCount = raidEvent.RaidInitialCount;
                raidEvent.RaidInitialCount = _maxObservedHostileCount;
                Log.Message($"[RimTalk Enhance] LordMonitorService: Updated RaidInitialCount from {oldCount} to {_maxObservedHostileCount}");
            }
        }
        
        /// <summary>
        /// 定期检查并更新初始计数（由 GameComponent 的 Tick 调用）
        /// </summary>
        public static void PeriodicUpdate()
        {
            // 如果战斗已开始，不再更新
            if (_combatStarted) return;
            
            // 如果没有活跃的袭击事件，不需要更新
            var raidEvent = RaidTrackingService.GetActiveRaidEvent();
            if (raidEvent == null) return;
            
            // 动物袭击和其他袭击都需要更新初始计数
            // 之前跳过动物袭击是错误的，因为动物也可能逐个生成
            // 更新计数
            UpdateRaidInitialCount();
        }
        
        /// <summary>
        /// 获取当前所有袭击 Lord 的信息（用于调试）
        /// </summary>
        public static string GetDebugInfo()
        {
            if (_subscribedMap == null) return "No map subscribed.";
            
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Subscribed Map: {_subscribedMap.Index}");
            sb.AppendLine($"Combat Started: {_combatStarted}");
            sb.AppendLine($"Max Observed Count: {_maxObservedHostileCount}");
            sb.AppendLine("Raid Lords:");
            
            foreach (var lord in _subscribedMap.lordManager.lords)
            {
                if (IsRaidLord(lord))
                {
                    sb.AppendLine($"  - {lord.LordJob?.GetType().Name}: {lord.ownedPawns?.Count ?? 0} pawns");
                }
            }
            
            return sb.ToString();
        }
    }
}