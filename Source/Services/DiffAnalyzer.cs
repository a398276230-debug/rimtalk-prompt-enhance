using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;

namespace RimTalkHealthEnhance
{
    public static class DiffAnalyzer
    {
        public static string GenerateDiffReport(
            ColonySnapshot yesterday, 
            ColonySnapshot today)
        {
            var sb = new StringBuilder();
            
            // 新增建筑
            var newBuildings = new Dictionary<string, int>();
            foreach (var kvp in today.BuildingCounts)
            {
                int oldCount = 0;
                if (yesterday.BuildingCounts.ContainsKey(kvp.Key))
                    oldCount = yesterday.BuildingCounts[kvp.Key];
                
                if (kvp.Value > oldCount)
                    newBuildings[kvp.Key] = kvp.Value - oldCount;
            }
            
            if (newBuildings.Count > 0)
            {
                sb.AppendLine("【新增建筑】");
                foreach (var kvp in newBuildings)
                {
                    string label = DefDatabase<ThingDef>.GetNamed(kvp.Key, false)?.label ?? kvp.Key;
                    sb.AppendLine($"  +{kvp.Value} {label}");
                }
            }
            
            // 新增房间
            // 简单逻辑：如果今天有某个角色的房间数量 > 昨天，或者房间大小有显著变化
            var newRooms = new List<RoomSnapshot>();
            foreach (var room in today.Rooms)
            {
                // 尝试在昨天找到匹配的房间（同角色，大小相近）
                bool found = false;
                foreach (var yRoom in yesterday.Rooms)
                {
                    if (yRoom.RoomRole == room.RoomRole && Math.Abs(yRoom.CellCount - room.CellCount) < 10)
                    {
                        found = true;
                        break;
                    }
                }
                
                if (!found)
                    newRooms.Add(room);
            }
            
            if (newRooms.Count > 0)
            {
                sb.AppendLine("\n【新增房间】");
                foreach (var room in newRooms)
                {
                    string roleLabel = DefDatabase<RoomRoleDef>.GetNamed(room.RoomRole, false)?.label ?? room.RoomRole;
                    sb.AppendLine($"  {roleLabel} ({room.CellCount} 格)");
                    if (room.KeyBuildings.Count > 0)
                        sb.AppendLine($"    包含: {string.Join(", ", room.KeyBuildings)}");
                }
            }
            
            // 进行中的蓝图
            // 只有在蓝图状态发生变化时才报告
            bool blueprintsChanged = false;
            if (today.BlueprintCounts.Count != yesterday.BlueprintCounts.Count)
            {
                blueprintsChanged = true;
            }
            else
            {
                foreach (var kvp in today.BlueprintCounts)
                {
                    if (!yesterday.BlueprintCounts.TryGetValue(kvp.Key, out int count) || count != kvp.Value)
                    {
                        blueprintsChanged = true;
                        break;
                    }
                }
            }

            if (blueprintsChanged && today.BlueprintCounts.Count > 0)
            {
                sb.AppendLine("\n【进行中的蓝图】");
                foreach (var kvp in today.BlueprintCounts)
                {
                    string label = DefDatabase<ThingDef>.GetNamed(kvp.Key, false)?.label ?? kvp.Key;
                    sb.AppendLine($"  {kvp.Value} 个 {label}");
                }
            }
            
            return sb.ToString();
        }
    }
}
