using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace RimTalkHealthEnhance
{
    /// <summary>
    /// 袭击事件追踪服务
    /// 追踪袭击过程中的敌人和殖民者伤亡情况
    /// </summary>
    public static class RaidTrackingService
    {
        // 袭击关键词（用于识别袭击事件）
        private static readonly string[] RaidKeywords = {
            "raid", "attack", "siege", "infestation", "manhunter", "ambush", "assault",
            "mechanoid", "mech cluster", "animal",
            "袭击", "进攻", "围攻", "虫害", "猎杀", "伏击", "突袭"
        };
        
        // 追踪受伤的 Pawn（去重用，每个 Pawn 只计数一次）
        private static HashSet<int> _woundedEnemyIds = new HashSet<int>();
        private static HashSet<int> _woundedColonistIds = new HashSet<int>();
        
        // 追踪已倒地的敌人 Pawn（用于在击杀时正确调整计数）
        private static HashSet<int> _downedEnemyIds = new HashSet<int>();
        
        // 动物袭击关键词（用于智能识别袭击类型）
        private static readonly string[] AnimalRaidKeywords = {
            "manhunter", "animal", "猎杀", "动物", "发狂"
        };
        
        // 虫群袭击关键词
        private static readonly string[] InfestationKeywords = {
            "infestation", "insect", "虫害", "虫群", "虫子"
        };
        
        // 机械族袭击关键词
        private static readonly string[] MechanoidKeywords = {
            "mechanoid", "mech", "cluster", "机械", "机械族", "机械集群"
        };
        
        // 当前活跃的袭击事件ID
        private static string _activeRaidEventId = null;
        
        /// <summary>
        /// 判断事件标题是否为袭击事件
        /// </summary>
        public static bool IsRaidEvent(string title)
        {
            if (string.IsNullOrEmpty(title)) return false;
            return RaidKeywords.Any(k => title.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0);
        }
        
        /// <summary>
        /// 判断是否为动物袭击事件
        /// </summary>
        public static bool IsAnimalRaidEvent(string title)
        {
            if (string.IsNullOrEmpty(title)) return false;
            return AnimalRaidKeywords.Any(k => title.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0);
        }
        
        /// <summary>
        /// 判断是否为虫群袭击事件
        /// </summary>
        public static bool IsInfestationEvent(string title)
        {
            if (string.IsNullOrEmpty(title)) return false;
            return InfestationKeywords.Any(k => title.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0);
        }
        
        /// <summary>
        /// 判断是否为机械族袭击事件
        /// </summary>
        public static bool IsMechanoidEvent(string title)
        {
            if (string.IsNullOrEmpty(title)) return false;
            return MechanoidKeywords.Any(k => title.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0);
        }
        
        /// <summary>
        /// 获取袭击类型的显示名称
        /// </summary>
        public static (string threatName, string unitName) GetRaidTypeDisplayNames(string title)
        {
            if (IsAnimalRaidEvent(title)) return ("发狂动物", "只");
            if (IsInfestationEvent(title)) return ("虫群", "只");
            if (IsMechanoidEvent(title)) return ("机械族", "个");
            return ("敌人", "人");
        }
        
        /// <summary>
        /// 设置当前活跃的袭击事件（由 EventCaptureService 调用）
        /// </summary>
        public static void SetActiveRaidEvent(ColonyAnnouncement raidEvent)
        {
            if (raidEvent == null) return;
            
            _activeRaidEventId = raidEvent.Id;
            
            // 清空追踪集合（新袭击开始）
            _woundedEnemyIds.Clear();
            _woundedColonistIds.Clear();
            _downedEnemyIds.Clear();
            
            Log.Message($"[RimTalk Enhance] Active raid event set to: '{raidEvent.Title}' (ID: {raidEvent.Id})");
        }
        
        /// <summary>
        /// 获取当前活跃的袭击事件
        /// </summary>
        public static ColonyAnnouncement GetActiveRaidEvent()
        {
            var manager = ColonyAnnouncementManager.Instance;
            if (manager?.Data?.Announcements == null)
            {
                Log.Warning("[RimTalk Enhance] GetActiveRaidEvent: Manager or Data is null!");
                return null;
            }
            
            // 如果有缓存的活跃袭击ID，先尝试使用
            if (!string.IsNullOrEmpty(_activeRaidEventId))
            {
                var cached = manager.Data.Announcements.FirstOrDefault(a => a.Id == _activeRaidEventId);
                if (cached != null && cached.Status == AnnouncementStatus.Active && cached.IsRaidEvent)
                {
                    return cached;
                }
                // 缓存失效，清除
                Log.Message($"[RimTalk Enhance] GetActiveRaidEvent: Cached raid ID '{_activeRaidEventId}' is no longer valid, clearing.");
                _activeRaidEventId = null;
            }
            
            // 查找最新的活跃袭击事件
            var result = manager.Data.Announcements
                .Where(a => a.Status == AnnouncementStatus.Active && a.IsRaidEvent)
                .OrderByDescending(a => a.CreatedTick)
                .FirstOrDefault();
                
            if (result == null)
            {
                // 尝试查找所有活跃的事件类公告，看看是否有未标记为袭击的
                var activeEvents = manager.Data.Announcements
                    .Where(a => a.Status == AnnouncementStatus.Active && a.Category == AnnouncementCategory.Event)
                    .ToList();
                    
                if (activeEvents.Count > 0)
                {
                    Log.Message($"[RimTalk Enhance] GetActiveRaidEvent: No active raid found, but {activeEvents.Count} active events exist. Titles: {string.Join(", ", activeEvents.Select(e => $"'{e.Title}' (IsRaid:{e.IsRaidEvent})"))}");
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// 初始化袭击追踪（在捕获袭击事件时调用）
        /// </summary>
        public static void InitializeRaidTracking(ColonyAnnouncement raidEvent)
        {
            if (raidEvent == null) return;
            
            raidEvent.IsRaidEvent = true;
            _activeRaidEventId = raidEvent.Id;
            
            // 清空追踪集合
            _woundedEnemyIds.Clear();
            _woundedColonistIds.Clear();
            _downedEnemyIds.Clear();
            
            // 计算初始敌人数量
            var map = Find.CurrentMap;
            if (map != null)
            {
                raidEvent.RaidInitialCount = CountHostileThreats(map);
                Log.Message($"[RimTalk Enhance] Raid tracking initialized for '{raidEvent.Title}' (ID: {raidEvent.Id}). Initial enemies: {raidEvent.RaidInitialCount}");
            }
            else
            {
                Log.Warning($"[RimTalk Enhance] Raid tracking initialized but no current map! Event: {raidEvent.Title}");
            }
        }
        
        /// <summary>
        /// 计算地图上的敌对威胁数量（只计算活着且站立的）
        /// 包括：人类敌人、机械族、发狂动物、虫群等所有敌对目标
        /// 使用 RimWorld 内置的 HostileTo 方法，自动处理派系敌对、精神状态（发狂）、掠食者等
        /// </summary>
        public static int CountHostileThreats(Map map)
        {
            if (map == null) return 0;
            
            int count = 0;
            var counted = new HashSet<int>(); // 防止重复计数
            
            foreach (var p in map.mapPawns.AllPawns)
            {
                if (p == null) continue;
                if (counted.Contains(p.thingIDNumber)) continue;
                
                // 跳过已死亡、未生成或已倒地的
                if (p.Dead || !p.Spawned || p.Downed) continue;
                
                // 跳过玩家派系的 Pawn
                if (p.Faction == Faction.OfPlayer) continue;
                
                // 使用 RimWorld 内置的 HostileTo 方法
                // 这会自动处理：派系敌对、发狂动物（MentalState）、狂暴状态、掠食者等
                bool isHostile = p.HostileTo(Faction.OfPlayer);
                
                if (isHostile)
                {
                    counted.Add(p.thingIDNumber);
                    count++;
                    
                    // 识别威胁类型用于日志
                    string raceType = p.RaceProps?.Humanlike == true ? "Humanlike" :
                                     (p.RaceProps?.Animal == true ? "Animal" :
                                     (p.RaceProps?.IsMechanoid == true ? "Mechanoid" : "Other"));
                    Log.Message($"[RimTalk Enhance] Counted hostile threat: {p.LabelShort} (ID: {p.thingIDNumber}, Type: {raceType}, Faction: {p.Faction?.Name ?? "None"})");
                }
            }
            
            Log.Message($"[RimTalk Enhance] Total hostile threats counted (alive & standing): {count}");
            return count;
        }
        
        /// <summary>
        /// 记录敌人死亡
        /// </summary>
        public static void RecordEnemyKill(Pawn enemy)
        {
            var raidEvent = GetActiveRaidEvent();
            if (raidEvent == null)
            {
                // 尝试查找任何活跃的袭击事件（即使未完全初始化）
                var manager = ColonyAnnouncementManager.Instance;
                if (manager?.Data?.Announcements != null)
                {
                    raidEvent = manager.Data.Announcements
                        .Where(a => a.Status == AnnouncementStatus.Active &&
                                   a.Category == AnnouncementCategory.Event &&
                                   a.IsRaidEvent)
                        .OrderByDescending(a => a.CreatedTick)
                        .FirstOrDefault();
                        
                    if (raidEvent != null)
                    {
                        // 找到了，设置为活跃
                        _activeRaidEventId = raidEvent.Id;
                        Log.Message($"[RimTalk Enhance] RecordEnemyKill: Found raid event '{raidEvent.Title}' via fallback search.");
                    }
                }
                
                if (raidEvent == null)
                {
                    Log.Warning($"[RimTalk Enhance] RecordEnemyKill: No active raid event found! Enemy: {enemy?.LabelShort ?? "Unknown"}");
                    return;
                }
            }
            
            // 通知 LordMonitorService 战斗已开始，锁定初始计数
            LordMonitorService.MarkCombatStarted();
            
            raidEvent.RaidKillCount++;
            
            // 如果敌人之前被记录为倒地，需要减去（击杀优先于击倒）
            if (enemy != null && _downedEnemyIds.Contains(enemy.thingIDNumber))
            {
                _downedEnemyIds.Remove(enemy.thingIDNumber);
                if (raidEvent.RaidDownedCount > 0)
                {
                    raidEvent.RaidDownedCount--;
                }
                Log.Message($"[RimTalk Enhance] Enemy killed (was downed): {enemy.LabelShort}. Adjusted downed count: {raidEvent.RaidDownedCount}");
            }
            
            Log.Message($"[RimTalk Enhance] Enemy killed: {enemy?.LabelShort ?? "Unknown"}. Total kills: {raidEvent.RaidKillCount}");
        }
        
        /// <summary>
        /// 记录敌人倒地（可能被俘虏）
        /// </summary>
        public static void RecordEnemyDowned(Pawn enemy)
        {
            if (enemy == null) return;
            
            var raidEvent = GetActiveRaidEvent();
            if (raidEvent == null) return;
            
            // 通知 LordMonitorService 战斗已开始，锁定初始计数
            LordMonitorService.MarkCombatStarted();
            
            // 防止重复记录同一个敌人的倒地
            if (_downedEnemyIds.Contains(enemy.thingIDNumber))
            {
                Log.Message($"[RimTalk Enhance] Enemy already recorded as downed: {enemy.LabelShort}");
                return;
            }
            
            _downedEnemyIds.Add(enemy.thingIDNumber);
            raidEvent.RaidDownedCount++;
            Log.Message($"[RimTalk Enhance] Enemy downed: {enemy.LabelShort} (ID: {enemy.thingIDNumber}). Total downed: {raidEvent.RaidDownedCount}");
        }
        
        /// <summary>
        /// 记录敌人撤退
        /// </summary>
        public static void RecordEnemyFlee(Pawn enemy)
        {
            var raidEvent = GetActiveRaidEvent();
            if (raidEvent == null) return;
            
            // 通知 LordMonitorService 战斗已开始，锁定初始计数
            LordMonitorService.MarkCombatStarted();
            
            raidEvent.RaidFleeCount++;
            Log.Message($"[RimTalk Enhance] Enemy fled: {enemy?.LabelShort ?? "Unknown"}. Total fled: {raidEvent.RaidFleeCount}");
        }
        
        /// <summary>
        /// 记录敌人受伤（去重）
        /// </summary>
        public static void RecordEnemyWounded(Pawn enemy)
        {
            if (enemy == null) return;
            
            var raidEvent = GetActiveRaidEvent();
            if (raidEvent == null) return;
            
            // 使用 thingIDNumber 作为唯一标识，避免重复计数
            if (!_woundedEnemyIds.Contains(enemy.thingIDNumber))
            {
                _woundedEnemyIds.Add(enemy.thingIDNumber);
                // 受伤数量可以通过 _woundedEnemyIds.Count 获取
            }
        }
        
        /// <summary>
        /// 记录殖民者死亡
        /// </summary>
        public static void RecordColonistDeath(Pawn colonist)
        {
            var raidEvent = GetActiveRaidEvent();
            if (raidEvent == null) return;
            
            raidEvent.ColonistDeathCount++;
            Log.Message($"[RimTalk Enhance] Colonist died: {colonist?.LabelShort ?? "Unknown"}. Total deaths: {raidEvent.ColonistDeathCount}");
        }
        
        /// <summary>
        /// 记录殖民者倒地
        /// </summary>
        public static void RecordColonistDowned(Pawn colonist)
        {
            var raidEvent = GetActiveRaidEvent();
            if (raidEvent == null) return;
            
            raidEvent.ColonistDownedCount++;
            Log.Message($"[RimTalk Enhance] Colonist downed: {colonist?.LabelShort ?? "Unknown"}. Total downed: {raidEvent.ColonistDownedCount}");
        }
        
        /// <summary>
        /// 记录殖民者受伤（去重）
        /// </summary>
        public static void RecordColonistWounded(Pawn colonist)
        {
            if (colonist == null) return;
            
            var raidEvent = GetActiveRaidEvent();
            if (raidEvent == null) return;
            
            if (!_woundedColonistIds.Contains(colonist.thingIDNumber))
            {
                _woundedColonistIds.Add(colonist.thingIDNumber);
            }
        }
        
        /// <summary>
        /// 检查 Pawn 是否曾被记录为倒地的敌人
        /// 用于判断倒地后死亡的敌人（此时 HostileTo 可能返回 false）
        /// </summary>
        public static bool WasEnemyDowned(int pawnId)
        {
            return _downedEnemyIds.Contains(pawnId);
        }
        
        /// <summary>
        /// 获取受伤敌人数量
        /// </summary>
        public static int GetWoundedEnemyCount()
        {
            return _woundedEnemyIds.Count;
        }
        
        /// <summary>
        /// 获取受伤殖民者数量
        /// </summary>
        public static int GetWoundedColonistCount()
        {
            return _woundedColonistIds.Count;
        }
        
        /// <summary>
        /// 完成袭击追踪，生成战斗报告
        /// </summary>
        public static string FinishRaidTracking(ColonyAnnouncement raidEvent)
        {
            if (raidEvent == null) return null;
            
            _activeRaidEventId = null;
            
            // 重置 LordMonitorService 的监控状态
            LordMonitorService.ResetMonitoring();
            
            int woundedEnemies = GetWoundedEnemyCount();
            int woundedColonists = GetWoundedColonistCount();
            
            // 清空追踪集合
            _woundedEnemyIds.Clear();
            _woundedColonistIds.Clear();
            _downedEnemyIds.Clear();
            
            // 生成战斗报告
            var report = new System.Text.StringBuilder();
            report.Append("[战斗结束] ");
            
            // 根据袭击类型智能选择措辞
            var (threatName, unitName) = GetRaidTypeDisplayNames(raidEvent.Title);
            
            // 敌方统计
            // 注意：倒地的敌人如果后来死了，RaidDownedCount 已经在 RecordEnemyKill 中减去
            // 所以这里的 RaidDownedCount 是最终存活但倒地的敌人（可能被俘虏或流血而死）
            var enemyStats = new List<string>();
            if (raidEvent.RaidKillCount > 0)
                enemyStats.Add($"击杀{raidEvent.RaidKillCount}{unitName}");
            if (raidEvent.RaidDownedCount > 0)
                enemyStats.Add($"击倒{raidEvent.RaidDownedCount}{unitName}");
            if (raidEvent.RaidFleeCount > 0)
                enemyStats.Add($"逃跑{raidEvent.RaidFleeCount}{unitName}");
            
            if (enemyStats.Count > 0)
            {
                report.Append($"{threatName}(共{raidEvent.RaidInitialCount}{unitName}): ");
                report.Append(string.Join(", ", enemyStats));
            }
            
            // 我方统计
            var colonistStats = new List<string>();
            if (raidEvent.ColonistDeathCount > 0)
                colonistStats.Add($"阵亡{raidEvent.ColonistDeathCount}");
            if (raidEvent.ColonistDownedCount > 0)
                colonistStats.Add($"倒地{raidEvent.ColonistDownedCount}");
            if (woundedColonists > 0)
                colonistStats.Add($"受伤{woundedColonists}");
            
            if (colonistStats.Count > 0)
            {
                report.Append($" | 殖民地: ");
                report.Append(string.Join(", ", colonistStats));
            }
            else if (enemyStats.Count > 0)
            {
                report.Append(" | 殖民地无伤亡");
            }
            
            return report.ToString();
        }
        
        /// <summary>
        /// 清除所有追踪数据（游戏加载时调用）
        /// </summary>
        public static void ClearTracking()
        {
            _activeRaidEventId = null;
            _woundedEnemyIds.Clear();
            _woundedColonistIds.Clear();
            _downedEnemyIds.Clear();
        }
    }
}