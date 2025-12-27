using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using RimTalkHealthEnhance.Models;

namespace RimTalkHealthEnhance
{
    public static class SnapshotService
    {
        private static FieldInfo allRoomsField;
        private static FieldInfo reinstallationMapField;

        public static ColonySnapshot TakeSnapshot()
        {
            var snapshot = new ColonySnapshot
            {
                SnapshotTick = Find.TickManager.TicksGame
            };
            
            try
            {
                var map = Find.CurrentMap;
                if (map == null) 
                {
                    Log.Warning("[RimTalk Enhance] CurrentMap is null during snapshot.");
                    return snapshot;
                }
                
                // 检查当前地图是否属于玩家殖民地
                if (!map.IsPlayerHome)
                {
                    Log.Warning("[RimTalk Enhance] Skipping snapshot - not on player home map.");
                    return snapshot;
                }
                
                // 统计建筑
                foreach (var building in map.listerBuildings.allBuildingsColonist)
                {
                    if (building.def == null) continue;
                    
                    string key = building.def.defName;
                    if (snapshot.BuildingCounts.ContainsKey(key))
                        snapshot.BuildingCounts[key]++;
                    else
                        snapshot.BuildingCounts[key] = 1;
                }
                
                // 统计房间 (使用反射访问私有字段 allRooms)
                if (allRoomsField == null)
                {
                    allRoomsField = AccessTools.Field(typeof(RegionGrid), "allRooms");
                }
                
                var allRooms = allRoomsField.GetValue(map.regionGrid) as List<Room>;
                if (allRooms != null)
                {
                    foreach (var room in allRooms)
                    {
                        if (room.Role == null || room.PsychologicallyOutdoors) 
                            continue;
                            
                        var roomSnap = new RoomSnapshot
                        {
                            RoomRole = room.Role.defName,
                            CellCount = room.CellCount,
                            KeyBuildings = room.ContainedAndAdjacentThings
                                .OfType<Building>()
                                .Where(b => IsKeyBuilding(b.def))
                                .Select(b => b.def.label)
                                .Distinct()
                                .ToList()
                        };
                        snapshot.Rooms.Add(roomSnap);
                    }
                }
                
                // 准备工程区域缓存
                var manager = ColonyAnnouncementManager.Instance;
                var projectAreas = new Dictionary<string, CustomNamedArea>();
                
                if (manager?.Data?.Announcements != null && manager.CustomAreas != null)
                {
                    foreach (var announcement in manager.Data.Announcements)
                    {
                        if (announcement.Category == AnnouncementCategory.Project && 
                            announcement.Status != AnnouncementStatus.Completed &&
                            !string.IsNullOrEmpty(announcement.BlueprintAreaId))
                        {
                            var area = manager.CustomAreas.FirstOrDefault(a => a.Id == announcement.BlueprintAreaId);
                            if (area != null)
                            {
                                projectAreas[announcement.Title] = area;
                            }
                        }
                    }
                }

                // 统计蓝图
                foreach (var blueprint in map.listerThings.ThingsInGroup(ThingRequestGroup.Blueprint))
                {
                    if (blueprint.Faction == Faction.OfPlayer && blueprint.def.entityDefToBuild != null)
                    {
                        string key = blueprint.def.entityDefToBuild.defName;
                        if (snapshot.BlueprintCounts.ContainsKey(key))
                            snapshot.BlueprintCounts[key]++;
                        else
                            snapshot.BlueprintCounts[key] = 1;
                            
                        // 检查所属工程
                        CheckBlueprintProject(blueprint, key, projectAreas, snapshot);
                    }
                }
                
                // 统计框架 (Frame) - 正在建造中的建筑
                foreach (var frame in map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingFrame))
                {
                    if (frame.Faction == Faction.OfPlayer && frame.def.entityDefToBuild != null)
                    {
                        string key = frame.def.entityDefToBuild.defName;
                        if (snapshot.BlueprintCounts.ContainsKey(key))
                            snapshot.BlueprintCounts[key]++;
                        else
                            snapshot.BlueprintCounts[key] = 1;
                            
                        // 检查所属工程
                        CheckBlueprintProject(frame, key, projectAreas, snapshot);
                    }
                }
                
                // 统计正在重新安装的建筑 (使用反射访问 ListerBuildings.reinstallationMap)
                if (reinstallationMapField == null)
                {
                    reinstallationMapField = AccessTools.Field(typeof(ListerBuildings), "reinstallationMap");
                }
                
                if (reinstallationMapField != null)
                {
                    var reinstallMap = reinstallationMapField.GetValue(map.listerBuildings) as System.Collections.IDictionary;
                    if (reinstallMap != null)
                    {
                        foreach (var key in reinstallMap.Keys)
                        {
                            var building = key as Thing;
                            if (building?.def != null)
                            {
                                string defName = building.def.defName;
                                if (snapshot.ReinstallingCounts.ContainsKey(defName))
                                    snapshot.ReinstallingCounts[defName]++;
                                else
                                    snapshot.ReinstallingCounts[defName] = 1;
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"[RimTalk Enhance] Error taking snapshot: {ex}");
            }
            
            return snapshot;
        }
        
        private static bool IsKeyBuilding(ThingDef def)
        {
            if (def == null) return false;
            
            // 过滤掉墙、门、地板等
            return def.building != null && 
                   !def.IsFrame && 
                   !def.IsBlueprint &&
                   def.category == ThingCategory.Building &&
                   def.building.isNaturalRock == false &&
                   def.passability != Traversability.Standable; // 通常关键建筑不可通行
        }

        private static void CheckBlueprintProject(Thing thing, string defName, Dictionary<string, CustomNamedArea> projectAreas, ColonySnapshot snapshot)
        {
            if (projectAreas == null || projectAreas.Count == 0) return;
            
            foreach (var kvp in projectAreas)
            {
                string projectName = kvp.Key;
                CustomNamedArea area = kvp.Value;
                
                if (area.ActiveCells.Contains(thing.Position))
                {
                    if (!snapshot.BlueprintToProjects.ContainsKey(defName))
                    {
                        snapshot.BlueprintToProjects[defName] = new ProjectListWrapper();
                    }
                    
                    if (!snapshot.BlueprintToProjects[defName].Projects.Contains(projectName))
                    {
                        snapshot.BlueprintToProjects[defName].Projects.Add(projectName);
                    }
                }
            }
        }
    }
}
