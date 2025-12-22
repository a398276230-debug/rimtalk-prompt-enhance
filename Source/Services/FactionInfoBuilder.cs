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
        public static string BuildFactionContext()
        {
            var settings = RimTalkHealthEnhanceMod.Settings;
            if (!settings.ShowFactionRelations) return null;
            
            var map = Find.CurrentMap;
            if (map == null || !map.IsPlayerHome) return null;
            
            // 获取当前地图上所有非玩家派系
            var factionsOnMap = GetFactionsOnMap(map);
            
            if (!factionsOnMap.Any()) return null;
            
            var sb = new StringBuilder();
            sb.AppendLine("=== Factions on Map ===");
            
            // 按关系分组
            var allies = new List<FactionInfo>();
            var neutrals = new List<FactionInfo>();
            var enemies = new List<FactionInfo>();
            
            foreach (var info in factionsOnMap)
            {
                int goodwill = info.Faction.GoodwillWith(Faction.OfPlayer);
                
                // 应用好感度过滤
                if (settings.FilterByGoodwill && goodwill < settings.MinGoodwillToShow)
                    continue;
                
                if (goodwill >= 75)
                    allies.Add(info);
                else if (goodwill <= -75 || info.Faction.HostileTo(Faction.OfPlayer))
                    enemies.Add(info);
                else
                    neutrals.Add(info);
            }
            
            // 输出盟友
            if (allies.Any())
            {
                sb.AppendLine("Allies:");
                foreach (var info in allies.OrderByDescending(f => f.Faction.GoodwillWith(Faction.OfPlayer)))
                {
                    sb.AppendLine(FormatFactionInfo(info));
                }
            }
            
            // 输出中立派系
            if (settings.ShowNeutralFactions && neutrals.Any())
            {
                sb.AppendLine("Neutral:");
                foreach (var info in neutrals.OrderByDescending(f => f.Faction.GoodwillWith(Faction.OfPlayer)))
                {
                    sb.AppendLine(FormatFactionInfo(info));
                }
            }
            
            // 输出敌对派系
            if (enemies.Any())
            {
                sb.AppendLine("Enemies:");
                foreach (var info in enemies.OrderBy(f => f.Faction.GoodwillWith(Faction.OfPlayer)))
                {
                    sb.AppendLine(FormatFactionInfo(info));
                }
            }
            
            return sb.ToString().TrimEnd();
        }
        
        private static List<FactionInfo> GetFactionsOnMap(Map map)
        {
            var factionDict = new Dictionary<Faction, FactionInfo>();
            
            // 遍历地图上所有 Pawn，统计派系
            foreach (var pawn in map.mapPawns.AllPawns)
            {
                if (pawn.Faction == null || pawn.Faction.IsPlayer) continue;
                if (pawn.Faction.def.hidden) continue; // 跳过隐藏派系
                
                if (!factionDict.ContainsKey(pawn.Faction))
                {
                    factionDict[pawn.Faction] = new FactionInfo
                    {
                        Faction = pawn.Faction,
                        PawnCount = 0,
                        IsHostile = pawn.HostileTo(Faction.OfPlayer)
                    };
                }
                
                factionDict[pawn.Faction].PawnCount++;
            }
            
            return factionDict.Values.ToList();
        }
        
        private static string FormatFactionInfo(FactionInfo info)
        {
            var settings = RimTalkHealthEnhanceMod.Settings;
            var sb = new StringBuilder();
            
            // 基本信息：派系名称
            sb.Append($"- {info.Faction.Name}");
            
            // 好感度
            if (settings.ShowFactionGoodwill)
            {
                int goodwill = info.Faction.GoodwillWith(Faction.OfPlayer);
                string relation = GetRelationLabel(goodwill, info.IsHostile);
                sb.Append($" ({relation}, Goodwill: {goodwill})");
            }
            
            // 成员数量
            if (settings.ShowFactionMemberCount && info.PawnCount > 0)
            {
                sb.Append($" - {info.PawnCount} member{(info.PawnCount > 1 ? "s" : "")} present");
            }
            
            return sb.ToString();
        }
        
        private static string GetRelationLabel(int goodwill, bool isHostile)
        {
            if (isHostile || goodwill < -75) return "Enemy";
            if (goodwill >= 75) return "Ally";
            if (goodwill >= 25) return "Friendly";
            if (goodwill >= -25) return "Neutral";
            return "Unfriendly";
        }
        
        private class FactionInfo
        {
            public Faction Faction;
            public int PawnCount;
            public bool IsHostile;
        }
    }
}
