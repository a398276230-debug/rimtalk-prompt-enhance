using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;

namespace RimTalkHealthEnhance
{
    /// <summary>
    /// 构建当前地图上派系关系信息
    /// </summary>
    public static class FactionInfoBuilder
    {
        /// <summary>
        /// 线程安全的公共方法 - 从缓存读取
        /// </summary>
        public static string BuildFactionContext()
        {
            var manager = ColonyAnnouncementManager.Instance;
            if (manager == null) return null;
            
            return manager.GetCachedFactionInfo();
        }
        
        /// <summary>
        /// 不安全的方法 - 仅在主线程调用（由 Manager 定期更新缓存）
        /// </summary>
        public static string BuildFactionContextUnsafe()
        {
            var settings = RimTalkHealthEnhanceMod.Settings;
            if (!settings.ShowFactionRelations) return null;
            
            var map = Find.CurrentMap;
            if (map == null || !map.IsPlayerHome) return null;
            
            // 获取当前地图上所有非玩家派系
            var factionsOnMap = GetFactionsOnMap(map);
            
            if (!factionsOnMap.Any()) return null;
            
            var sb = new StringBuilder();
            sb.AppendLine("## Factions on Map");
            
            // 全局身份摘要（如果启用）
            if (settings.ShowGlobalSummary)
            {
                int totalEnemies = factionsOnMap.Sum(f => f.ActiveEnemies);
                int totalPrisoners = factionsOnMap.Sum(f => f.Prisoners);
                int totalTraders = factionsOnMap.Sum(f => f.Traders);
                int totalVisitors = factionsOnMap.Sum(f => f.Visitors);
                
                if (totalEnemies > 0 || totalPrisoners > 0 || totalTraders > 0 || totalVisitors > 0)
                {
                    sb.AppendLine("### Population Summary");
                    
                    if (totalEnemies > 0)
                        sb.AppendLine($"- Threats: {totalEnemies} active enem{(totalEnemies > 1 ? "ies" : "y")}");
                    
                    if (totalPrisoners > 0)
                        sb.AppendLine($"- Detained: {totalPrisoners} prisoner{(totalPrisoners > 1 ? "s" : "")}");
                    
                    if (totalTraders > 0 || totalVisitors > 0)
                    {
                        var visitorParts = new List<string>();
                        if (totalVisitors > 0)
                            visitorParts.Add($"{totalVisitors} visitor{(totalVisitors > 1 ? "s" : "")}");
                        if (totalTraders > 0)
                            visitorParts.Add($"{totalTraders} trader{(totalTraders > 1 ? "s" : "")}");
                        sb.AppendLine($"- Visitors: {string.Join(", ", visitorParts)}");
                    }
                    
                    sb.AppendLine();
                }
            }
            
            // 按游戏实际关系分组
            var allies = new List<FactionInfo>();
            var neutrals = new List<FactionInfo>();
            var enemies = new List<FactionInfo>();
            
            foreach (var info in factionsOnMap)
            {
                int goodwill = info.Faction.GoodwillWith(Faction.OfPlayer);
                
                // 应用好感度过滤
                if (settings.FilterByGoodwill && goodwill < settings.MinGoodwillToShow)
                    continue;
                
                // 使用游戏的实际关系状态判断
                FactionRelationKind relationKind = info.Faction.RelationKindWith(Faction.OfPlayer);
                
                if (relationKind == FactionRelationKind.Ally)
                    allies.Add(info);
                else if (relationKind == FactionRelationKind.Hostile)
                    enemies.Add(info);
                else // FactionRelationKind.Neutral
                    neutrals.Add(info);
            }
            
            // 输出盟友
            if (allies.Any())
            {
                sb.AppendLine("### Allies");
                foreach (var info in allies.OrderByDescending(f => f.Faction.GoodwillWith(Faction.OfPlayer)))
                {
                    sb.AppendLine(FormatFactionInfo(info));
                }
                sb.AppendLine();
            }
            
            // 输出中立派系
            if (settings.ShowNeutralFactions && neutrals.Any())
            {
                sb.AppendLine("### Neutral");
                foreach (var info in neutrals.OrderByDescending(f => f.Faction.GoodwillWith(Faction.OfPlayer)))
                {
                    sb.AppendLine(FormatFactionInfo(info));
                }
                sb.AppendLine();
            }
            
            // 输出敌对派系
            if (enemies.Any())
            {
                sb.AppendLine("### Hostile");
                foreach (var info in enemies.OrderBy(f => f.Faction.GoodwillWith(Faction.OfPlayer)))
                {
                    sb.AppendLine(FormatFactionInfo(info));
                }
            }
            
            return sb.ToString().TrimEnd();
        }
        
        private static List<FactionInfo> GetFactionsOnMap(Map map)
        {
            var settings = RimTalkHealthEnhanceMod.Settings;
            var factionDict = new Dictionary<Faction, FactionInfo>();
            
            // 遍历地图上所有 Pawn，统计派系和身份
            foreach (var pawn in map.mapPawns.AllPawns)
            {
                if (pawn.Faction == null || pawn.Faction.IsPlayer) continue;
                if (pawn.Faction.def.hidden) continue; // 跳过隐藏派系
                if (!pawn.RaceProps.Humanlike) continue; // 只统计人形生物
                
                if (!factionDict.ContainsKey(pawn.Faction))
                {
                    factionDict[pawn.Faction] = new FactionInfo
                    {
                        Faction = pawn.Faction,
                        PawnCount = 0,
                        ActiveEnemies = 0,
                        Prisoners = 0,
                        Traders = 0,
                        Visitors = 0
                    };
                }
                
                var info = factionDict[pawn.Faction];
                info.PawnCount++;
                
                // 身份识别（仅在启用身份细分时）
                if (settings.ShowIdentityBreakdown)
                {
                    if (pawn.IsPrisoner)
                    {
                        info.Prisoners++;
                    }
                    else if (pawn.HostileTo(Faction.OfPlayer) && !pawn.Downed)
                    {
                        info.ActiveEnemies++;
                    }
                    else if (pawn.trader != null)
                    {
                        // 是商队成员（有 trader 组件）
                        info.Traders++;
                    }
                    else if (!pawn.HostileTo(Faction.OfPlayer))
                    {
                        info.Visitors++;
                    }
                }
            }
            
            return factionDict.Values.ToList();
        }
        
        private static string FormatFactionInfo(FactionInfo info)
        {
            var settings = RimTalkHealthEnhanceMod.Settings;
            var sb = new StringBuilder();
            
            // 基本信息：派系名称
            sb.Append($"- {info.Faction.Name}");
            
            // 好感度和关系状态
            if (settings.ShowFactionGoodwill)
            {
                int goodwill = info.Faction.GoodwillWith(Faction.OfPlayer);
                FactionRelationKind relationKind = info.Faction.RelationKindWith(Faction.OfPlayer);
                string relation = GetRelationLabel(relationKind);
                sb.Append($" ({relation}, Goodwill: {goodwill})");
            }
            
            // 成员数量
            if (settings.ShowFactionMemberCount && info.PawnCount > 0)
            {
                sb.Append($" - {info.PawnCount} member{(info.PawnCount > 1 ? "s" : "")} present");
            }
            
            // 身份细分（如果启用）
            if (settings.ShowIdentityBreakdown && info.PawnCount > 0)
            {
                var identities = new List<string>();
                
                if (info.ActiveEnemies > 0)
                    identities.Add($"{info.ActiveEnemies} active enem{(info.ActiveEnemies > 1 ? "ies" : "y")}");
                
                if (info.Prisoners > 0)
                    identities.Add($"{info.Prisoners} prisoner{(info.Prisoners > 1 ? "s" : "")}");
                
                if (info.Traders > 0)
                    identities.Add($"{info.Traders} trader{(info.Traders > 1 ? "s" : "")}");
                
                if (info.Visitors > 0)
                    identities.Add($"{info.Visitors} visitor{(info.Visitors > 1 ? "s" : "")}");
                
                if (identities.Any())
                {
                    sb.AppendLine();
                    sb.Append($"  └─ {string.Join(", ", identities)}");
                }
            }
            
            return sb.ToString();
        }
        
        private static string GetRelationLabel(FactionRelationKind relationKind)
        {
            switch (relationKind)
            {
                case FactionRelationKind.Ally:
                    return "Ally";
                case FactionRelationKind.Hostile:
                    return "Hostile";
                case FactionRelationKind.Neutral:
                default:
                    return "Neutral";
            }
        }
        
        private class FactionInfo
        {
            public Faction Faction;
            public int PawnCount;
            
            // 身份细分统计
            public int ActiveEnemies;    // 活跃敌人（未倒地、非囚犯）
            public int Prisoners;        // 囚犯
            public int Traders;          // 商队成员
            public int Visitors;         // 普通访客
        }
    }
}
