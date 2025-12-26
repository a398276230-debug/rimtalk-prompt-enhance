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
            
            // 减少/拆除的建筑（遍历昨天的建筑，检查今天是否减少）
            var removedBuildings = new Dictionary<string, int>();
            foreach (var kvp in yesterday.BuildingCounts)
            {
                int newCount = 0;
                if (today.BuildingCounts.ContainsKey(kvp.Key))
                    newCount = today.BuildingCounts[kvp.Key];
                
                if (kvp.Value > newCount)
                    removedBuildings[kvp.Key] = kvp.Value - newCount;
            }
            
            if (removedBuildings.Count > 0)
            {
                sb.AppendLine("\n【减少/拆除的建筑】");
                foreach (var kvp in removedBuildings)
                {
                    string label = DefDatabase<ThingDef>.GetNamed(kvp.Key, false)?.label ?? kvp.Key;
                    sb.AppendLine($"  -{kvp.Value} {label}");
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
            
            // 消失/拆除的房间（遍历昨天的房间，检查今天是否还存在）
            var removedRooms = new List<RoomSnapshot>();
            foreach (var yRoom in yesterday.Rooms)
            {
                bool found = false;
                foreach (var room in today.Rooms)
                {
                    if (room.RoomRole == yRoom.RoomRole && Math.Abs(room.CellCount - yRoom.CellCount) < 10)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                    removedRooms.Add(yRoom);
            }
            
            if (removedRooms.Count > 0)
            {
                sb.AppendLine("\n【消失/拆除的房间】");
                foreach (var room in removedRooms)
                {
                    string roleLabel = DefDatabase<RoomRoleDef>.GetNamed(room.RoomRole, false)?.label ?? room.RoomRole;
                    sb.AppendLine($"  {roleLabel} ({room.CellCount} 格)");
                }
            }
            
            // 检测重新安装/迁移中的建筑
            // 特征：建筑数量不变，但有该类型的蓝图存在
            var reinstalling = new List<string>();
            foreach (var kvp in today.BlueprintCounts)
            {
                string defName = kvp.Key;
                int blueprintCount = kvp.Value;
                
                // 检查该建筑类型的数量是否在昨天和今天相同
                int yesterdayCount = yesterday.BuildingCounts.ContainsKey(defName)
                    ? yesterday.BuildingCounts[defName] : 0;
                int todayCount = today.BuildingCounts.ContainsKey(defName)
                    ? today.BuildingCounts[defName] : 0;
                
                // 如果建筑数量相同且大于0，同时有蓝图，说明可能是重新安装
                if (yesterdayCount == todayCount && todayCount > 0 && blueprintCount > 0)
                {
                    string label = DefDatabase<ThingDef>.GetNamed(defName, false)?.label ?? defName;
                    reinstalling.Add($"{label} x{blueprintCount}（正在重新安装/迁移）");
                }
            }
            
            if (reinstalling.Count > 0)
            {
                sb.AppendLine("\n【重新安装/迁移中】");
                foreach (var item in reinstalling)
                    sb.AppendLine($"  {item}");
            }
            
            // 进行中的蓝图（排除重新安装的）
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
                // 过滤掉已经在"重新安装"中报告的蓝图
                var newBlueprints = new Dictionary<string, int>();
                foreach (var kvp in today.BlueprintCounts)
                {
                    string defName = kvp.Key;
                    int yesterdayBuildingCount = yesterday.BuildingCounts.ContainsKey(defName)
                        ? yesterday.BuildingCounts[defName] : 0;
                    int todayBuildingCount = today.BuildingCounts.ContainsKey(defName)
                        ? today.BuildingCounts[defName] : 0;
                    
                    // 只有不是"重新安装"的情况才显示在蓝图列表中
                    bool isReinstalling = (yesterdayBuildingCount == todayBuildingCount && todayBuildingCount > 0);
                    if (!isReinstalling)
                    {
                        newBlueprints[defName] = kvp.Value;
                    }
                }
                
                if (newBlueprints.Count > 0)
                {
                    sb.AppendLine("\n【进行中的蓝图】");
                    foreach (var kvp in newBlueprints)
                    {
                        string label = DefDatabase<ThingDef>.GetNamed(kvp.Key, false)?.label ?? kvp.Key;
                        sb.AppendLine($"  {kvp.Value} 个 {label}");
                    }
                }
            }
            
            return sb.ToString();
        }
    }
}
