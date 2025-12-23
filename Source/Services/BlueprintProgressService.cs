using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimTalkHealthEnhance.Models;

namespace RimTalkHealthEnhance
{
    /// <summary>
    /// 蓝图进度服务 - 扫描施工区域内的蓝图并计算工程进度
    /// </summary>
    public static class BlueprintProgressService
    {
        /// <summary>
        /// 扫描指定区域内的所有蓝图
        /// </summary>
        public static int CountBlueprintsInArea(CustomNamedArea area)
        {
            if (area?.Cells == null) return 0;
            
            var map = Find.CurrentMap;
            if (map == null) return 0;
            
            // 使用 HashSet 避免重复计数同一个蓝图
            var countedThings = new HashSet<Thing>();
            
            // 遍历区域内的所有格子，查找蓝图（Frame 或 Blueprint）
            foreach (var cell in area.ActiveCells)
            {
                // 检查该格子上的所有物品
                var things = map.thingGrid.ThingsListAt(cell);
                foreach (var thing in things)
                {
                    // Frame = 建造中的蓝图
                    // Blueprint_Build/Blueprint_Install = 未开始建造的蓝图
                    if ((thing is Frame || thing.def.IsBlueprint) && !countedThings.Contains(thing))
                    {
                        countedThings.Add(thing);
                    }
                }
            }
            
            return countedThings.Count;
        }
        
        /// <summary>
        /// 获取区域内蓝图的详细信息（用于AI总结）
        /// </summary>
        public static List<BlueprintInfo> GetBlueprintDetailsInArea(CustomNamedArea area)
        {
            if (area?.Cells == null) return new List<BlueprintInfo>();
            
            var map = Find.CurrentMap;
            if (map == null) return new List<BlueprintInfo>();
            
            // Key: "BuildingName|Location"
            var buildingCounts = new Dictionary<string, BlueprintInfo>();
            // 避免重复计数同一个蓝图
            var countedThings = new HashSet<Thing>();
            
            // 遍历区域内的所有格子
            foreach (var cell in area.ActiveCells)
            {
                var things = map.thingGrid.ThingsListAt(cell);
                foreach (var thing in things)
                {
                    // 如果已经计数过这个蓝图，跳过
                    if (countedThings.Contains(thing))
                        continue;
                    
                    string buildingName = null;
                    
                    // Frame = 建造中的蓝图
                    if (thing is Frame frame)
                    {
                        buildingName = frame.def?.entityDefToBuild?.label ?? frame.Label;
                    }
                    // Blueprint = 未开始建造的蓝图
                    else if (thing.def.IsBlueprint)
                    {
                        buildingName = thing.def?.entityDefToBuild?.label ?? thing.Label;
                    }
                    
                    if (buildingName != null)
                    {
                        // 标记为已计数
                        countedThings.Add(thing);
                        
                        // 获取位置信息
                        string location = GetLocationName(cell, map);
                        string key = $"{buildingName}|{location}";
                        
                        if (buildingCounts.ContainsKey(key))
                        {
                            buildingCounts[key].Count++;
                        }
                        else
                        {
                            buildingCounts[key] = new BlueprintInfo
                            {
                                BuildingName = buildingName,
                                Location = location,
                                Count = 1
                            };
                        }
                    }
                }
            }
            
            // 转换为列表并排序
            return buildingCounts.Values.OrderByDescending(x => x.Count).ToList();
        }
        
        /// <summary>
        /// 获取格子的位置名称（房间名或"Outdoors"）
        /// </summary>
        private static string GetLocationName(IntVec3 cell, Map map)
        {
            var room = cell.GetRoom(map);
            
            // 室外
            if (room == null || room.PsychologicallyOutdoors)
                return "Outdoors";
            
            // 室内 - 尝试获取自定义房间名
            try
            {
                var labelCapProp = room.GetType().GetProperty("LabelCap");
                if (labelCapProp != null)
                {
                    string labelCap = labelCapProp.GetValue(room) as string;
                    if (!string.IsNullOrEmpty(labelCap))
                        return labelCap;
                }
            }
            catch
            {
                // 忽略反射错误
            }
            
            // 回退到角色标签
            return room.Role?.label ?? "Room";
        }
        
        /// <summary>
        /// 计算工程进度（0-1）
        /// </summary>
        public static float CalculateProgress(int initialCount, int currentCount)
        {
            if (initialCount <= 0) return 0f;
            
            int completed = initialCount - currentCount;
            float progress = (float)completed / initialCount;
            
            return UnityEngine.Mathf.Clamp01(progress);
        }
        
        /// <summary>
        /// 更新工程的自动进度
        /// </summary>
        public static void UpdateProjectProgress(ColonyAnnouncement project)
        {
            if (!project.AutoCalculateProgress) return;
            if (string.IsNullOrEmpty(project.BlueprintAreaId)) return;
            
            var manager = ColonyAnnouncementManager.Instance;
            if (manager == null) return;
            
            var area = manager.CustomAreas?.FirstOrDefault(a => a.Id == project.BlueprintAreaId);
            if (area == null) return;
            
            int currentCount = CountBlueprintsInArea(area);
            project.Progress = CalculateProgress(project.InitialBlueprintCount, currentCount);
            
            // 如果进度达到100%，自动标记为完成
            if (project.Progress >= 0.99f && project.Status == AnnouncementStatus.Active)
            {
                project.Status = AnnouncementStatus.Completed;
                project.CompletedTick = Find.TickManager.TicksGame;
            }
        }
        
        /// <summary>
        /// 批量更新所有启用自动计算的工程
        /// </summary>
        public static void UpdateAllAutoProjects()
        {
            var manager = ColonyAnnouncementManager.Instance;
            if (manager?.Data?.Announcements == null) return;
            
            foreach (var announcement in manager.Data.Announcements)
            {
                if (announcement.Category == AnnouncementCategory.Project && 
                    announcement.AutoCalculateProgress)
                {
                    UpdateProjectProgress(announcement);
                }
            }
        }
    }
    
    /// <summary>
    /// 蓝图信息（用于AI总结）
    /// </summary>
    public class BlueprintInfo
    {
        public string BuildingName;
        public string Location;
        public int Count;
        
        public override string ToString()
        {
            return $"{BuildingName} x{Count} ({Location})";
        }
    }
}
